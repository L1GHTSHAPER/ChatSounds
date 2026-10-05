using System;
using System.Text;

namespace ChatSounds
{
    /// <summary>
    /// Decodes RIFF/WAVE files (PCM 8/16/24/32-bit and IEEE float) straight from bytes, so WAV files load without
    /// going through file:// URLs. Plain .NET, no Unity types.
    /// </summary>
    internal static class WavReader
    {
        const int FormatPcm = 1;
        const int FormatFloat = 3;
        const int FormatExtensible = 0xFFFE;

        /// <summary>Interleaved samples in [-1, 1].</summary>
        public static bool TryRead(byte[] data, out float[] samples, out int channels, out int sampleRate, out string error)
        {
            samples = null;
            channels = 0;
            sampleRate = 0;
            error = null;
            if (data == null || data.Length < 12 || Tag(data, 0) != "RIFF" || Tag(data, 8) != "WAVE")
            {
                error = "not a WAV (RIFF/WAVE) file";
                return false;
            }

            int format = 0, bits = 0, dataOffset = -1, dataLength = 0;
            int position = 12;
            while (position + 8 <= data.Length)
            {
                string id = Tag(data, position);
                long size = BitConverter.ToUInt32(data, position + 4);
                int body = position + 8;
                // Truncated files and streamed files (size 0xFFFFFFFF) use whatever data is there.
                if (body + size > data.Length)
                    size = data.Length - body;

                if (id == "fmt " && size >= 16)
                {
                    format = BitConverter.ToUInt16(data, body);
                    channels = BitConverter.ToUInt16(data, body + 2);
                    sampleRate = BitConverter.ToInt32(data, body + 4);
                    bits = BitConverter.ToUInt16(data, body + 14);
                    // WAVE_FORMAT_EXTENSIBLE: the real format is the first field of the sub-format GUID.
                    if (format == FormatExtensible && size >= 26)
                        format = BitConverter.ToUInt16(data, body + 24);
                }
                else if (id == "data")
                {
                    dataOffset = body;
                    dataLength = (int)size;
                    if (format != 0)
                        break;
                }
                // Chunks are padded to an even size.
                position = body + (int)size + (int)(size & 1);
            }

            if (format == 0)
            {
                error = "no fmt chunk";
                return false;
            }
            if (dataOffset < 0)
            {
                error = "no data chunk";
                return false;
            }
            if (channels < 1 || channels > 8 || sampleRate < 1000 || sampleRate > 384000)
            {
                error = $"unsupported layout ({channels} channels, {sampleRate} Hz)";
                return false;
            }
            bool pcm = format == FormatPcm && (bits == 8 || bits == 16 || bits == 24 || bits == 32);
            bool ieee = format == FormatFloat && (bits == 32 || bits == 64);
            if (!pcm && !ieee)
            {
                error = $"unsupported encoding (format {format}, {bits} bit)";
                return false;
            }

            int bytesPerSample = bits / 8;
            int frames = dataLength / (bytesPerSample * channels);
            if (frames <= 0)
            {
                error = "no audio data";
                return false;
            }
            samples = new float[frames * channels];
            int offset = dataOffset;
            for (int i = 0; i < samples.Length; i++, offset += bytesPerSample)
                samples[i] = Decode(data, offset, bits, ieee);
            return true;
        }

        static float Decode(byte[] data, int offset, int bits, bool ieee)
        {
            if (ieee)
                return bits == 32 ? BitConverter.ToSingle(data, offset) : (float)BitConverter.ToDouble(data, offset);
            switch (bits)
            {
                case 8:
                    return (data[offset] - 128) / 128f;
                case 16:
                    return BitConverter.ToInt16(data, offset) / 32768f;
                case 24:
                    return (data[offset] | (data[offset + 1] << 8) | ((sbyte)data[offset + 2] << 16)) / 8388608f;
                default:
                    return BitConverter.ToInt32(data, offset) / 2147483648f;
            }
        }

        static string Tag(byte[] data, int offset)
        {
            return Encoding.ASCII.GetString(data, offset, 4);
        }
    }
}
