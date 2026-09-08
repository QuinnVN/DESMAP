using System;
using System.Text;
using NCKH.VR.GameControl;
using NUnit.Framework;
using UnityEngine;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerDefenseRecordingTests
    {
        [Test]
        public void WavEncoderWritesPcm16HeaderAndClampedSamples()
        {
            float[] samples = { -2f, -0.5f, 0f, 0.5f, 2f };
            byte[] wav = Pcm16WavEncoder.Encode(samples, 1, 16000);

            Assert.That(Encoding.ASCII.GetString(wav, 0, 4), Is.EqualTo("RIFF"));
            Assert.That(Encoding.ASCII.GetString(wav, 8, 4), Is.EqualTo("WAVE"));
            Assert.That(BitConverter.ToInt32(wav, 24), Is.EqualTo(16000));
            Assert.That(BitConverter.ToInt16(wav, 22), Is.EqualTo(1));
            Assert.That(BitConverter.ToInt16(wav, 34), Is.EqualTo(16));
            Assert.That(BitConverter.ToInt32(wav, 40), Is.EqualTo(samples.Length * 2));
            Assert.That(wav, Has.Length.EqualTo(44 + samples.Length * 2));
            Assert.That(BitConverter.ToInt16(wav, 44), Is.EqualTo(short.MinValue));
            Assert.That(BitConverter.ToInt16(wav, 52), Is.EqualTo(short.MaxValue));
        }

        [Test]
        public void DownmixAveragesInterleavedChannels()
        {
            float[] mono = Pcm16WavEncoder.DownmixToMono(
                new[] { 1f, -1f, 0.5f, 0.25f }, 2);

            Assert.That(mono, Is.EqualTo(new[] { 0f, 0.375f }).Within(0.0001f));
        }

        [Test]
        public void TelemetrySerializesNestedAudioPayload()
        {
            GameplayTelemetryData data = new GameplayTelemetryData
            {
                roundId = "round-1",
                caseId = "case-1",
                audio = new AudioTelemetryData
                {
                    fileName = "defense.wav",
                    mimeType = "audio/wav",
                    encoding = "pcm_s16le",
                    sampleRateHz = 16000,
                    channels = 1,
                    durationSeconds = 12.5f,
                    endedEarly = true,
                    dataBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 })
                }
            };

            string json = JsonUtility.ToJson(data);
            Assert.That(json, Does.Contain("\"audio\""));
            Assert.That(json, Does.Contain("\"mimeType\":\"audio/wav\""));
            Assert.That(json, Does.Contain("\"sampleRateHz\":16000"));
            Assert.That(json, Does.Contain("\"endedEarly\":true"));
        }

        [Test]
        public void ReliableTelemetryUsesRequestedExponentialDelays()
        {
            Assert.That(GameController.ReliableRetryDelay(0), Is.EqualTo(1f));
            Assert.That(GameController.ReliableRetryDelay(1), Is.EqualTo(2f));
            Assert.That(GameController.ReliableRetryDelay(2), Is.EqualTo(4f));
            Assert.That(GameController.ReliableRetryDelay(3), Is.EqualTo(8f));
            Assert.That(GameController.ReliableRetryDelay(10), Is.EqualTo(8f));
        }
    }
}
