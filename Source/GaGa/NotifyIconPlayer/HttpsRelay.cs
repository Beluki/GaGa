
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace GaGa.NotifyIconPlayer
{
    /// <summary>
    /// The WPF MediaPlayer that GaGa uses cannot open https:// streams
    /// (MediaPlayer.Open throws before connecting). This relay fetches the
    /// HTTPS stream itself (TLS 1.2) and serves it to the MediaPlayer as a
    /// plain http://127.0.0.1:port/... stream. Only reachable from this computer.
    /// </summary>
    internal class HttpsRelay : IDisposable
    {
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
        /// Start relaying a given https:// stream.
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
            acceptThread.Name = "GaGa HTTPS relay";
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
                worker.Name = "GaGa HTTPS relay connection";
                worker.Start();
            }
        }

        /// <summary>
        /// Read the local request headers (and ignore them: it is always our stream).
        /// </summary>
        private static void ReadRequest(NetworkStream local)
        {
            Byte[] buffer = new Byte[1];
            Int32 matched = 0;
            Int32 total = 0;

            // read until \r\n\r\n (or a sane limit):
            while (matched < 4 && total < 65536)
            {
                Int32 read = local.Read(buffer, 0, 1);
                if (read == 0)
                    return;

                total++;
                Byte b = buffer[0];
                if ((matched % 2 == 0 && b == '\r') || (matched % 2 == 1 && b == '\n'))
                    matched++;
                else
                    matched = (b == '\r') ? 1 : 0;
            }
        }

        private static void WriteText(Stream stream, String text)
        {
            Byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        private void Serve(TcpClient client)
        {
            HttpWebResponse response = null;

            try
            {
                client.NoDelay = true;
                NetworkStream local = client.GetStream();
                ReadRequest(local);

                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(target);
                request.Method = "GET";
                request.UserAgent = "GaGa/1.0 (Windows; radio player)";
                request.Accept = "*/*";
                request.AllowAutoRedirect = true;
                request.KeepAlive = false;
                request.Timeout = 15000;
                request.ReadWriteTimeout = 30000;

                try
                {
                    response = (HttpWebResponse) request.GetResponse();
                }
                catch (WebException e)
                {
                    HttpWebResponse r = e.Response as HttpWebResponse;
                    lastError = (r != null)
                        ? ("Server answered " + (Int32) r.StatusCode + " " + r.StatusDescription)
                        : e.Message;

                    WriteText(local, "HTTP/1.0 502 Bad Gateway\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");
                    return;
                }

                String contentType = response.ContentType;
                if (String.IsNullOrEmpty(contentType))
                    contentType = "application/octet-stream";

                lastError = null;

                using (Stream remote = response.GetResponseStream())
                {
                    // Ogg streams: the MediaPlayer can't read Ogg at all, but it plays
                    // native FLAC, so Ogg FLAC is unwrapped on the fly (lossless, no re-encoding).
                    // Anything else (including other Ogg codecs) is passed through unchanged.
                    if (contentType.IndexOf("ogg", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        OggFlacRemuxer remuxer = new OggFlacRemuxer(remote);
                        Boolean isFlac = remuxer.Start();

                        WriteText(local,
                            "HTTP/1.0 200 OK\r\n" +
                            "Content-Type: " + (isFlac ? "audio/flac" : contentType) + "\r\n" +
                            "Cache-Control: no-cache\r\n" +
                            "Connection: close\r\n\r\n");

                        if (isFlac)
                            remuxer.CopyFlacTo(local, () => stopped);
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
                        Int32 read = remote.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            break;

                        local.Write(buffer, 0, read);
                    }
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
                if (response != null)
                    response.Close();

                client.Close();
            }
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
