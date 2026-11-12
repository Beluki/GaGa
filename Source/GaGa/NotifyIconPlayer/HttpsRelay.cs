
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;


namespace GaGa.NotifyIconPlayer
{
    /// <summary>
    /// Fetches a radio stream itself and serves it to the WPF MediaPlayer
    /// as a plain http://127.0.0.1:port/... stream. Only reachable from this computer.
    ///
    /// The MediaPlayer can't open https:// (MediaPlayer.Open throws before connecting)
    /// and never shows the track titles that radio servers send. The relay:
    ///   - connects with TLS 1.2 for https:// streams,
    ///   - reads ICY titles (and removes them from the audio),
    ///   - unwraps Ogg FLAC into native FLAC (see OggFlacRemuxer),
    ///   - follows .m3u/.pls playlists to the first stream in them,
    ///   - understands old SHOUTcast servers that answer "ICY 200 OK".
    /// </summary>
    internal class HttpsRelay : IDisposable
    {
        private const String UserAgent = "GaGa/1.0 (Windows; radio player)";
        private const Int32 MaxRedirects = 5;

        private readonly Uri target;
        private readonly TcpListener listener;
        private readonly Thread acceptThread;
        private volatile Boolean stopped;
        private volatile String lastError;

        static HttpsRelay()
        {
            // .NET Framework defaults to SSL3/TLS 1.0, which modern servers refuse.
            // 3072 = Tls12, 768 = Tls11 (enum values exist on .NET 4.5+ runtimes).
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType) 3072 | (SecurityProtocolType) 768;
            }
            catch (NotSupportedException)
            {
            }
        }

        /// <summary>
        /// Start relaying a given http:// or https:// stream.
        /// </summary>
        public HttpsRelay(Uri target)
        {
            this.target = target;

            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            Int32 port = ((IPEndPoint) listener.LocalEndpoint).Port;

            // keep the original path, so the player still sees e.g. ".mp3":
            LocalUri = new Uri("http://127.0.0.1:" + port + target.PathAndQuery);

            acceptThread = new Thread(AcceptLoop);
            acceptThread.IsBackground = true;
            acceptThread.Name = "GaGa stream relay";
            acceptThread.Start();
        }

        /// <summary>
        /// Local http:// address to give to the MediaPlayer.
        /// </summary>
        public Uri LocalUri { get; private set; }

        /// <summary>
        /// Last error while fetching the remote stream, if any.
        /// </summary>
        public String LastError
        {
            get { return lastError; }
        }

        /// <summary>
        /// Raised when the stream announces the current track title
        /// (ICY StreamTitle for MP3/AAC, Vorbis comments for Ogg FLAC).
        /// Called from a relay thread, not the UI thread.
        /// </summary>
        public event Action<String> TitleChanged;

        private void ReportTitle(String title)
        {
            Action<String> handler = TitleChanged;
            if (handler != null && !stopped && !String.IsNullOrEmpty(title))
                handler(title);
        }

        private void AcceptLoop()
        {
            while (!stopped)
            {
                TcpClient client;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    // listener stopped:
                    return;
                }

                Thread worker = new Thread(() => Serve(client));
                worker.IsBackground = true;
                worker.Name = "GaGa stream relay connection";
                worker.Start();
            }
        }

        ///
        /// Small helpers
        ///

        /// <summary>
        /// Read one header line (up to \n, without \r\n). Null at end of stream.
        /// </summary>
        private static String ReadLine(Stream stream)
        {
            List<Byte> line = new List<Byte>();
            while (line.Count < 16384)
            {
                Int32 b = stream.ReadByte();
                if (b < 0)
                    return (line.Count > 0) ? Encoding.UTF8.GetString(line.ToArray()) : null;

                if (b == '\n')
                    break;

                if (b != '\r')
                    line.Add((Byte) b);
            }
            return Encoding.UTF8.GetString(line.ToArray());
        }

        /// <summary>
        /// Read the local request headers (and ignore them: it is always our stream).
        /// </summary>
        private static void ReadRequest(NetworkStream local)
        {
            String line;
            do
            {
                line = ReadLine(local);
            }
            while (!String.IsNullOrEmpty(line));
        }

        private static void WriteText(Stream stream, String text)
        {
            Byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        ///
        /// Remote connection
        ///

        /// <summary>
        /// An open remote stream and what we know about it.
        /// </summary>
        private class Remote : IDisposable
        {
            public Stream Body;
            public String ContentType;
            public Int32 MetaInterval;
            public IDisposable Owner;

            public void Dispose()
            {
                if (Body != null)
                    Body.Close();
                if (Owner != null)
                    Owner.Dispose();
            }
        }

        /// <summary>
        /// Connect with HttpWebRequest. Throws WebException.
        /// </summary>
        private static Remote OpenWithWebRequest(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
            request.Method = "GET";
            request.UserAgent = UserAgent;
            request.Accept = "*/*";
            request.AllowAutoRedirect = true;
            request.KeepAlive = false;
            request.Timeout = 15000;
            request.ReadWriteTimeout = 30000;

            // ask for in-band track titles:
            request.Headers.Add("Icy-MetaData", "1");

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            Remote remote = new Remote();
            remote.Owner = response;
            remote.Body = response.GetResponseStream();
            remote.ContentType = response.ContentType;
            remote.MetaInterval = ParseInterval(response.Headers["icy-metaint"]);
            return remote;
        }

        /// <summary>
        /// Minimal HTTP/1.0 client for servers that HttpWebRequest rejects,
        /// mainly SHOUTcast v1 answering "ICY 200 OK" instead of "HTTP/1.x 200 OK".
        /// </summary>
        private static Remote OpenRaw(Uri uri, Int32 redirects)
        {
            TcpClient client = new TcpClient();
            try
            {
                client.ReceiveTimeout = 30000;
                client.SendTimeout = 15000;

                IAsyncResult connecting = client.BeginConnect(uri.Host, uri.Port, null, null);
                if (!connecting.AsyncWaitHandle.WaitOne(15000))
                    throw new WebException("Connection timed out", WebExceptionStatus.Timeout);
                client.EndConnect(connecting);

                Stream stream = client.GetStream();

                if (uri.Scheme == Uri.UriSchemeHttps)
                {
                    SslStream ssl = new SslStream(stream, false);
                    // 3072 = Tls12, 768 = Tls11, 192 = Tls:
                    ssl.AuthenticateAsClient(uri.Host, null, (SslProtocols) (3072 | 768 | 192), true);
                    stream = ssl;
                }

                String host = uri.IsDefaultPort ? uri.Host : (uri.Host + ":" + uri.Port);
                WriteText(stream,
                    "GET " + uri.PathAndQuery + " HTTP/1.0\r\n" +
                    "Host: " + host + "\r\n" +
                    "User-Agent: " + UserAgent + "\r\n" +
                    "Accept: */*\r\n" +
                    "Icy-MetaData: 1\r\n" +
                    "Connection: close\r\n\r\n");

                // status line: "HTTP/1.0 200 OK" or "ICY 200 OK":
                String status = ReadLine(stream);
                String[] parts = (status ?? "").Split(new Char[] { ' ' }, 3);
                Int32 code;
                if (parts.Length < 2 || !Int32.TryParse(parts[1], out code))
                    throw new WebException("Invalid answer from the server: " + status);

                Dictionary<String, String> headers = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                String line;
                while (!String.IsNullOrEmpty(line = ReadLine(stream)))
                {
                    Int32 colon = line.IndexOf(':');
                    if (colon > 0)
                        headers[line.Substring(0, colon).Trim()] = line.Substring(colon + 1).Trim();
                }

                String location;
                if (code >= 300 && code < 400 && headers.TryGetValue("Location", out location) && redirects < MaxRedirects)
                {
                    client.Close();
                    return OpenRaw(new Uri(uri, location), redirects + 1);
                }

                if (code != 200)
                    throw new WebException("Server answered " + code + (parts.Length > 2 ? " " + parts[2] : ""));

                String contentType;
                String interval;
                headers.TryGetValue("Content-Type", out contentType);
                headers.TryGetValue("icy-metaint", out interval);

                Remote remote = new Remote();
                remote.Owner = client;
                remote.Body = stream;
                remote.ContentType = contentType;
                remote.MetaInterval = ParseInterval(interval);
                return remote;
            }
            catch
            {
                client.Close();
                throw;
            }
        }

        private static Int32 ParseInterval(String value)
        {
            Int32 interval;
            if (!Int32.TryParse(value, out interval) || interval <= 0)
                return 0;
            return interval;
        }

        /// <summary>
        /// Connect to a stream: HttpWebRequest first, the raw client when
        /// the server speaks something HttpWebRequest doesn't accept.
        /// </summary>
        private static Remote Open(Uri uri)
        {
            try
            {
                return OpenWithWebRequest(uri);
            }
            catch (WebException e)
            {
                if (e.Status != WebExceptionStatus.ServerProtocolViolation)
                    throw;
            }
            return OpenRaw(uri, 0);
        }

        ///
        /// Playlists
        ///

        private static Boolean IsPlaylist(Uri uri, String contentType)
        {
            String type = (contentType ?? "").ToLowerInvariant();
            if (type.Contains("mpegurl") || type.Contains("scpls") || type.Contains("pls+xml"))
                return true;

            // some servers send playlists as text/plain or octet-stream:
            String path = uri.AbsolutePath.ToLowerInvariant();
            return (path.EndsWith(".m3u") || path.EndsWith(".m3u8") || path.EndsWith(".pls"))
                && !type.StartsWith("audio/") && !type.Contains("ogg");
        }

        /// <summary>
        /// First http(s) stream address in an .m3u or .pls playlist, or null.
        /// HLS playlists (#EXT-X-...) are not radio streams we can relay: null.
        /// </summary>
        internal static Uri FirstPlaylistEntry(String text, Uri baseUri)
        {
            if (text.Contains("#EXT-X-"))
                return null;

            foreach (String raw in text.Split('\n'))
            {
                String line = raw.Trim();

                // .pls: File1=http://...
                if (line.StartsWith("File", StringComparison.OrdinalIgnoreCase) && line.Contains("="))
                    line = line.Substring(line.IndexOf('=') + 1).Trim();

                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("["))
                    continue;

                Uri entry;
                if (Uri.TryCreate(baseUri, line, out entry)
                    && (entry.Scheme == Uri.UriSchemeHttp || entry.Scheme == Uri.UriSchemeHttps))
                    return entry;
            }
            return null;
        }

        /// <summary>
        /// Open the stream, following playlists to the actual audio stream.
        /// Returns the playlist itself when it can't be followed (the player may still manage).
        /// </summary>
        private static Remote OpenFollowingPlaylists(Uri uri)
        {
            for (Int32 depth = 0; ; depth++)
            {
                Remote remote = Open(uri);
                if (depth >= 3 || !IsPlaylist(uri, remote.ContentType))
                    return remote;

                // playlists are small; streams are not, so cap what we read:
                MemoryStream body = new MemoryStream();
                Byte[] buffer = new Byte[4096];
                Int32 read;
                while (body.Length < 65536 && (read = remote.Body.Read(buffer, 0, buffer.Length)) > 0)
                    body.Write(buffer, 0, read);

                Uri entry = FirstPlaylistEntry(Encoding.UTF8.GetString(body.ToArray()), uri);
                if (entry == null)
                {
                    // give the player the playlist as it was:
                    remote.Body.Close();
                    remote.Body = new MemoryStream(body.ToArray());
                    remote.MetaInterval = 0;
                    return remote;
                }

                remote.Dispose();
                uri = entry;
            }
        }

        ///
        /// Serving
        ///

        private void Serve(TcpClient client)
        {
            Remote remote = null;

            try
            {
                client.NoDelay = true;
                NetworkStream local = client.GetStream();
                ReadRequest(local);

                try
                {
                    remote = OpenFollowingPlaylists(target);
                }
                catch (Exception e)
                {
                    WebException web = e as WebException;
                    HttpWebResponse r = (web != null) ? (web.Response as HttpWebResponse) : null;
                    lastError = (r != null)
                        ? ("Server answered " + (Int32) r.StatusCode + " " + r.StatusDescription)
                        : e.Message;

                    WriteText(local, "HTTP/1.0 502 Bad Gateway\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");
                    return;
                }

                String contentType = remote.ContentType;
                if (String.IsNullOrEmpty(contentType))
                    contentType = "application/octet-stream";

                lastError = null;

                // with "icy-metaint: N" the server inserts a metadata block after
                // every N audio bytes; they are removed before anything else sees the audio:
                Stream audio = (remote.MetaInterval > 0)
                    ? new IcyMetadataStream(remote.Body, remote.MetaInterval, ReportTitle)
                    : remote.Body;

                // Ogg streams: the MediaPlayer can't read Ogg at all, but it plays
                // native FLAC, so Ogg FLAC is unwrapped on the fly (lossless, no re-encoding).
                // Anything else (including other Ogg codecs) is passed through unchanged.
                if (contentType.IndexOf("ogg", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    OggFlacRemuxer remuxer = new OggFlacRemuxer(audio);
                    Boolean isFlac = remuxer.Start();

                    WriteText(local,
                        "HTTP/1.0 200 OK\r\n" +
                        "Content-Type: " + (isFlac ? "audio/flac" : contentType) + "\r\n" +
                        "Cache-Control: no-cache\r\n" +
                        "Connection: close\r\n\r\n");

                    if (isFlac)
                        remuxer.CopyFlacTo(local, () => stopped, ReportTitle);
                    else
                        remuxer.CopyRawTo(local, () => stopped);
                    return;
                }

                WriteText(local,
                    "HTTP/1.0 200 OK\r\n" +
                    "Content-Type: " + contentType + "\r\n" +
                    "Cache-Control: no-cache\r\n" +
                    "Connection: close\r\n\r\n");

                Byte[] buffer = new Byte[16384];
                while (!stopped)
                {
                    Int32 read = audio.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    local.Write(buffer, 0, read);
                }
            }
            catch (Exception e)
            {
                // the player closed the connection (stop/switch) or the network failed:
                if (!stopped)
                    lastError = e.Message;
            }
            finally
            {
                if (remote != null)
                    remote.Dispose();

                client.Close();
            }
        }

        ///
        /// ICY metadata
        ///

        /// <summary>
        /// Read-only stream that removes ICY metadata blocks from the audio
        /// and reports the StreamTitle they contain.
        /// </summary>
        private class IcyMetadataStream : Stream
        {
            private readonly Stream inner;
            private readonly Int32 interval;
            private readonly Action<String> onTitle;
            private Int32 untilMeta;

            public IcyMetadataStream(Stream inner, Int32 interval, Action<String> onTitle)
            {
                this.inner = inner;
                this.interval = interval;
                this.onTitle = onTitle;
                untilMeta = interval;
            }

            public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
            {
                if (untilMeta == 0)
                {
                    if (!ReadMetadata())
                        return 0;
                    untilMeta = interval;
                }

                Int32 read = inner.Read(buffer, offset, Math.Min(count, untilMeta));
                if (read > 0)
                    untilMeta -= read;
                return read;
            }

            /// <summary>
            /// One metadata block: a length byte (* 16), then text like
            /// "StreamTitle='Artist - Title';". False when the stream ended.
            /// </summary>
            private Boolean ReadMetadata()
            {
                Int32 length = inner.ReadByte();
                if (length < 0)
                    return false;

                length *= 16;
                if (length == 0)
                    return true;

                Byte[] data = new Byte[length];
                Int32 done = 0;
                while (done < length)
                {
                    Int32 read = inner.Read(data, done, length - done);
                    if (read <= 0)
                        return false;
                    done += read;
                }

                String title = ParseStreamTitle(data);
                if (title != null && onTitle != null)
                    onTitle(title);

                return true;
            }

            public override Boolean CanRead { get { return true; } }
            public override Boolean CanSeek { get { return false; } }
            public override Boolean CanWrite { get { return false; } }
            public override Int64 Length { get { throw new NotSupportedException(); } }
            public override Int64 Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }
            public override void Flush() { }
            public override Int64 Seek(Int64 offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(Int64 value) { throw new NotSupportedException(); }
            public override void Write(Byte[] buffer, Int32 offset, Int32 count) { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Extract StreamTitle from an ICY metadata block.
        /// UTF-8 when valid, Latin-1 otherwise (older servers).
        /// </summary>
        internal static String ParseStreamTitle(Byte[] data)
        {
            Int32 count = data.Length;
            while (count > 0 && data[count - 1] == 0)
                count--;

            String text;
            try
            {
                text = new UTF8Encoding(false, true).GetString(data, 0, count);
            }
            catch (ArgumentException)
            {
                text = Encoding.GetEncoding(28591).GetString(data, 0, count);
            }

            const String key = "StreamTitle='";
            Int32 start = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return null;

            start += key.Length;

            // the title itself may contain quotes, so look for the field end:
            Int32 end = text.IndexOf("';", start, StringComparison.Ordinal);
            if (end < 0)
                end = text.LastIndexOf('\'');
            if (end < start)
                return null;

            return text.Substring(start, end - start).Trim();
        }

        /// <summary>
        /// Stop relaying and close every connection.
        /// </summary>
        public void Dispose()
        {
            stopped = true;
            try
            {
                listener.Stop();
            }
            catch (Exception)
            {
            }
        }
    }
}
