
// GaGa.
// A minimal radio player for the Windows Tray.


using System;
using System.Collections.Generic;
using System.IO;


namespace GaGa.NotifyIconPlayer
{
    /// <summary>
    /// Unwraps an Ogg FLAC stream into a native FLAC stream on the fly.
    /// The WPF MediaPlayer can't read Ogg, but it plays native FLAC (Windows 10+).
    /// FLAC frames are copied as they are: lossless, no re-encoding.
    ///
    /// Ogg FLAC mapping (https://xiph.org/flac/ogg_mapping.html):
    ///   first packet: 0x7F "FLAC" major minor nheaders(2) "fLaC" STREAMINFO block
    ///   next packets: other metadata blocks, then one FLAC frame per packet.
    /// Output: "fLaC" + STREAMINFO (marked as the last metadata block) + frames.
    /// </summary>
    internal class OggFlacRemuxer
    {
        private readonly Stream input;

        // everything read so far, kept until we know whether this is FLAC
        // (so a non-FLAC Ogg stream can still be passed through unchanged):
        private MemoryStream sniffed;

        private readonly List<Byte> packet = new List<Byte>();
        private Boolean packetValid;
        private readonly Queue<Byte[]> ready = new Queue<Byte[]>();
        private Byte[] firstPacket;

        public OggFlacRemuxer(Stream input)
        {
            this.input = input;
            sniffed = new MemoryStream();
        }

        ///
        /// Ogg reading
        ///

        private Boolean ReadFully(Byte[] buffer, Int32 offset, Int32 count)
        {
            while (count > 0)
            {
                Int32 read = input.Read(buffer, offset, count);
                if (read <= 0)
                    return false;

                if (sniffed != null)
                    sniffed.Write(buffer, offset, read);

                offset += read;
                count -= read;
            }
            return true;
        }

        /// <summary>
        /// Read the next Ogg page and queue the packets it completes.
        /// Resynchronizes on "OggS" if the stream starts mid-page.
        /// </summary>
        private Boolean ReadPage()
        {
            Byte[] one = new Byte[1];
            Int32 matched = 0;
            Byte[] magic = { (Byte) 'O', (Byte) 'g', (Byte) 'g', (Byte) 'S' };

            while (matched < 4)
            {
                if (!ReadFully(one, 0, 1))
                    return false;

                if (one[0] == magic[matched])
                    matched++;
                else
                    matched = (one[0] == magic[0]) ? 1 : 0;
            }

            // version(1) type(1) granule(8) serial(4) sequence(4) crc(4) segments(1):
            Byte[] header = new Byte[23];
            if (!ReadFully(header, 0, header.Length))
                return false;

            Boolean continued = (header[1] & 0x01) != 0;
            Int32 segments = header[22];

            Byte[] table = new Byte[segments];
            if (!ReadFully(table, 0, segments))
                return false;

            Int32 total = 0;
            foreach (Byte length in table)
                total += length;

            Byte[] data = new Byte[total];
            if (!ReadFully(data, 0, total))
                return false;

            // a continued packet whose start we never saw (we joined mid-stream) is dropped:
            if (!continued)
            {
                packet.Clear();
                packetValid = true;
            }

            Int32 position = 0;
            foreach (Byte length in table)
            {
                if (packetValid)
                {
                    for (Int32 i = 0; i < length; i++)
                        packet.Add(data[position + i]);
                }
                position += length;

                // a segment shorter than 255 ends the packet:
                if (length < 255)
                {
                    if (packetValid && packet.Count > 0)
                        ready.Enqueue(packet.ToArray());

                    packet.Clear();
                    packetValid = true;
                }
            }
            return true;
        }

        private Byte[] NextPacket()
        {
            while (ready.Count == 0)
            {
                if (!ReadPage())
                    return null;
            }
            return ready.Dequeue();
        }

        private static Boolean IsFlacHeader(Byte[] p)
        {
            return p.Length >= 13 + 4 + 34
                && p[0] == 0x7F && p[1] == 'F' && p[2] == 'L' && p[3] == 'A' && p[4] == 'C'
                && p[9] == 'f' && p[10] == 'L' && p[11] == 'a' && p[12] == 'C';
        }

        ///
        /// Public API
        ///

        /// <summary>
        /// Read the first packet and tell whether this is an Ogg FLAC stream.
        /// </summary>
        public Boolean Start()
        {
            firstPacket = NextPacket();
            return firstPacket != null && IsFlacHeader(firstPacket);
        }

        /// <summary>
        /// Write a native FLAC stream: "fLaC" + STREAMINFO, then audio frames.
        /// </summary>
        public void CopyFlacTo(Stream output, Func<Boolean> stopped, Action<String> onTitle)
        {
            sniffed = null;

            // "fLaC" + STREAMINFO block header (4) + STREAMINFO (34),
            // flagged as the last metadata block: we drop the others (tags, etc.):
            Byte[] head = new Byte[4 + 4 + 34];
            Array.Copy(firstPacket, 9, head, 0, head.Length);
            head[4] |= 0x80;
            output.Write(head, 0, head.Length);

            while (!stopped())
            {
                Byte[] p = NextPacket();
                if (p == null)
                    return;

                // FLAC frames start with the 0xFFF8/0xFFF9 sync code;
                // metadata packets and repeated stream headers are skipped:
                if (p.Length >= 2 && p[0] == 0xFF && (p[1] & 0xFE) == 0xF8)
                {
                    Byte[] frame = Renumber(p);
                    output.Write(frame, 0, frame.Length);
                }

                // VORBIS_COMMENT block (type 4): the tags of the current track,
                // sent again at the start of every chained stream:
                else if (p.Length > 4 && (p[0] & 0x7F) == 4 && onTitle != null)
                {
                    String title = ParseVorbisComment(p, 4);
                    if (title != null)
                        onTitle(title);
                }
            }
        }

        private static Int32 ReadInt32LE(Byte[] p, Int32 at)
        {
            return p[at] | (p[at + 1] << 8) | (p[at + 2] << 16) | (p[at + 3] << 24);
        }

        /// <summary>
        /// Build "Artist - Title" from a Vorbis comment block
        /// (vendor string, then "KEY=value" UTF-8 strings, lengths little-endian).
        /// </summary>
        internal static String ParseVorbisComment(Byte[] p, Int32 at)
        {
            try
            {
                Int32 vendorLength = ReadInt32LE(p, at);
                if (vendorLength < 0)
                    return null;

                at += 4 + vendorLength;
                Int32 count = ReadInt32LE(p, at);
                at += 4;

                String artist = null;
                String title = null;

                for (Int32 i = 0; i < count && at + 4 <= p.Length; i++)
                {
                    Int32 length = ReadInt32LE(p, at);
                    at += 4;
                    if (length < 0 || at + length > p.Length)
                        break;

                    String comment = System.Text.Encoding.UTF8.GetString(p, at, length);
                    at += length;

                    Int32 equals = comment.IndexOf('=');
                    if (equals <= 0)
                        continue;

                    String key = comment.Substring(0, equals);
                    String value = comment.Substring(equals + 1).Trim();
                    if (value.Length == 0)
                        continue;

                    if (artist == null && key.Equals("ARTIST", StringComparison.OrdinalIgnoreCase))
                        artist = value;
                    else if (title == null && key.Equals("TITLE", StringComparison.OrdinalIgnoreCase))
                        title = value;
                }

                if (artist != null && title != null)
                    return artist + " - " + title;

                return title ?? artist;
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        ///
        /// Frame renumbering
        ///
        /// Servers that chain Ogg streams (a new logical stream on every track,
        /// e.g. Liquidsoap/Icecast) restart FLAC frame numbers from zero. In one
        /// native FLAC stream that looks like time jumping backwards and the
        /// player stops. So every frame gets a continuous number; the header
        /// CRC-8 and the frame CRC-16 are recomputed. Audio data is untouched.
        ///

        private UInt64 nextFrame;     // fixed-blocksize streams: frame number
        private UInt64 nextSample;    // variable-blocksize streams: first sample number

        private static Int32 BlockSize(Int32 code, Byte[] p, Int32 extraAt)
        {
            if (code == 1) return 192;
            if (code >= 2 && code <= 5) return 576 << (code - 2);
            if (code == 6) return p[extraAt] + 1;
            if (code == 7) return ((p[extraAt] << 8) | p[extraAt + 1]) + 1;
            if (code >= 8) return 256 << (code - 8);
            return -1;
        }

        private static Byte[] EncodeNumber(UInt64 v)
        {
            if (v < 0x80) return new Byte[] { (Byte) v };

            Int32 length;
            if (v < 0x800) length = 2;
            else if (v < 0x10000) length = 3;
            else if (v < 0x200000) length = 4;
            else if (v < 0x4000000) length = 5;
            else if (v < 0x80000000) length = 6;
            else length = 7;

            Byte[] result = new Byte[length];
            for (Int32 i = length - 1; i > 0; i--)
            {
                result[i] = (Byte) (0x80 | (v & 0x3F));
                v >>= 6;
            }
            Byte mask = (Byte) (0xFF << (8 - length));
            result[0] = (Byte) (mask | (Byte) v);
            if (length == 7)
                result[0] = 0xFE;
            return result;
        }

        private static Byte Crc8(Byte[] data, Int32 count)
        {
            Byte crc = 0;
            for (Int32 i = 0; i < count; i++)
            {
                crc ^= data[i];
                for (Int32 bit = 0; bit < 8; bit++)
                    crc = (Byte) (((crc & 0x80) != 0) ? ((crc << 1) ^ 0x07) : (crc << 1));
            }
            return crc;
        }

        private static UInt16 Crc16(Byte[] data, Int32 count)
        {
            UInt16 crc = 0;
            for (Int32 i = 0; i < count; i++)
            {
                crc ^= (UInt16) (data[i] << 8);
                for (Int32 bit = 0; bit < 8; bit++)
                    crc = (UInt16) (((crc & 0x8000) != 0) ? ((crc << 1) ^ 0x8005) : (crc << 1));
            }
            return crc;
        }

        private Byte[] Renumber(Byte[] p)
        {
            try
            {
                Boolean variable = (p[1] & 0x01) != 0;
                Int32 blockCode = p[2] >> 4;
                Int32 rateCode = p[2] & 0x0F;

                // length of the old UTF-8-like coded number at offset 4:
                Int32 first = p[4], numberLength;
                if ((first & 0x80) == 0) numberLength = 1;
                else if ((first & 0xE0) == 0xC0) numberLength = 2;
                else if ((first & 0xF0) == 0xE0) numberLength = 3;
                else if ((first & 0xF8) == 0xF0) numberLength = 4;
                else if ((first & 0xFC) == 0xF8) numberLength = 5;
                else if ((first & 0xFE) == 0xFC) numberLength = 6;
                else if (first == 0xFE) numberLength = 7;
                else return p;

                Int32 extraAt = 4 + numberLength;
                Int32 extraLength = (blockCode == 6 ? 1 : blockCode == 7 ? 2 : 0)
                                  + (rateCode == 12 ? 1 : (rateCode == 13 || rateCode == 14) ? 2 : 0);

                Int32 blockSize = BlockSize(blockCode, p, extraAt);
                Int32 oldHeaderEnd = extraAt + extraLength;   // the CRC-8 byte
                if (blockSize <= 0 || oldHeaderEnd + 1 + 2 > p.Length)
                    return p;

                Byte[] number = EncodeNumber(variable ? nextSample : nextFrame);
                nextFrame++;
                nextSample += (UInt64) blockSize;

                Int32 headerLength = 4 + number.Length + extraLength;
                Int32 bodyLength = p.Length - (oldHeaderEnd + 1) - 2;   // between header CRC-8 and frame CRC-16
                Byte[] result = new Byte[headerLength + 1 + bodyLength + 2];

                Array.Copy(p, 0, result, 0, 4);
                Array.Copy(number, 0, result, 4, number.Length);
                Array.Copy(p, extraAt, result, 4 + number.Length, extraLength);
                result[headerLength] = Crc8(result, headerLength);
                Array.Copy(p, oldHeaderEnd + 1, result, headerLength + 1, bodyLength);

                UInt16 crc = Crc16(result, result.Length - 2);
                result[result.Length - 2] = (Byte) (crc >> 8);
                result[result.Length - 1] = (Byte) crc;
                return result;
            }
            catch (IndexOutOfRangeException)
            {
                return p;
            }
        }

        /// <summary>
        /// Not FLAC: pass the Ogg stream through unchanged.
        /// </summary>
        public void CopyRawTo(Stream output, Func<Boolean> stopped)
        {
            Byte[] already = sniffed.ToArray();
            sniffed = null;
            output.Write(already, 0, already.Length);

            Byte[] buffer = new Byte[16384];
            while (!stopped())
            {
                Int32 read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;

                output.Write(buffer, 0, read);
            }
        }
    }
}
