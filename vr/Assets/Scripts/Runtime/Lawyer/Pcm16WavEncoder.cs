using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    public static class Pcm16WavEncoder
    {
        private const int HeaderSize = 44;

        public static byte[] Encode(float[] samples, int channels, int sampleRateHz)
        {
            if (samples == null)
                throw new ArgumentNullException(nameof(samples));
            if (channels <= 0)
                throw new ArgumentOutOfRangeException(nameof(channels));
            if (sampleRateHz <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRateHz));
            if (samples.Length % channels != 0)
                throw new ArgumentException(
                    "Sample count must be divisible by the channel count.", nameof(samples));

            int dataSize = checked(samples.Length * sizeof(short));
            using MemoryStream stream = new MemoryStream(HeaderSize + dataSize);
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRateHz);
            writer.Write(sampleRateHz * channels * sizeof(short));
            writer.Write((short)(channels * sizeof(short)));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (int index = 0; index < samples.Length; index++)
            {
                float clamped = Mathf.Clamp(samples[index], -1f, 1f);
                short pcm = clamped <= -1f
                    ? short.MinValue
                    : (short)Mathf.RoundToInt(clamped * short.MaxValue);
                writer.Write(pcm);
            }

            writer.Flush();
            return stream.ToArray();
        }

        public static float[] DownmixToMono(float[] interleavedSamples, int channels)
        {
            if (interleavedSamples == null)
                throw new ArgumentNullException(nameof(interleavedSamples));
            if (channels <= 0)
                throw new ArgumentOutOfRangeException(nameof(channels));
            if (interleavedSamples.Length % channels != 0)
                throw new ArgumentException(
                    "Sample count must be divisible by the channel count.",
                    nameof(interleavedSamples));
            if (channels == 1)
                return interleavedSamples;

            int frameCount = interleavedSamples.Length / channels;
            float[] mono = new float[frameCount];
            for (int frame = 0; frame < frameCount; frame++)
            {
                float sum = 0f;
                int offset = frame * channels;
                for (int channel = 0; channel < channels; channel++)
                    sum += interleavedSamples[offset + channel];
                mono[frame] = sum / channels;
            }
            return mono;
        }
    }
}
