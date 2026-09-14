using NCKH.VR.GameControl;
using NUnit.Framework;
using UnityEngine;

namespace NCKH.VR.GameControl.Tests
{
    public sealed class SceneBgmPlayerTests
    {
        [Test]
        public void SetVolumeClampsAndUpdatesAudioSource()
        {
            var gameObject = new GameObject("Scene BGM Test");
            try
            {
                SceneBgmPlayer player = gameObject.AddComponent<SceneBgmPlayer>();
                AudioSource source = gameObject.GetComponent<AudioSource>();

                player.SetVolume(2f);

                Assert.That(player.Volume, Is.EqualTo(1f));
                Assert.That(source.volume, Is.EqualTo(1f));
                Assert.That(source.loop, Is.True);
                Assert.That(source.spatialBlend, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

    }
}
