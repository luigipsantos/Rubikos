using System.Collections.Generic;
using NUnit.Framework;
using Rubikos.Core;

namespace Rubikos.Tests
{
    public class SoundSynthTests
    {
        private static IEnumerable<TestCaseData> AllSounds()
        {
            yield return new TestCaseData(SoundSynth.Thock()).SetName("Thock");
            yield return new TestCaseData(SoundSynth.Chime()).SetName("Chime");
            yield return new TestCaseData(SoundSynth.Buzz()).SetName("Buzz");
        }

        [TestCaseSource(nameof(AllSounds))]
        public void Sound_HasASensibleLength(float[] samples)
        {
            Assert.Greater(samples.Length, SoundSynth.SampleRate / 50, "longer than 20 ms");
            Assert.Less(samples.Length, SoundSynth.SampleRate * 2, "shorter than 2 s");
        }

        [TestCaseSource(nameof(AllSounds))]
        public void Sound_NeverClips(float[] samples)
        {
            foreach (float sample in samples)
                Assert.LessOrEqual(System.Math.Abs(sample), 1f);
        }

        [TestCaseSource(nameof(AllSounds))]
        public void Sound_IsNotSilent(float[] samples)
        {
            float max = 0f;
            foreach (float sample in samples) max = System.Math.Max(max, System.Math.Abs(sample));
            Assert.Greater(max, 0.1f);
        }

        [TestCaseSource(nameof(AllSounds))]
        public void Sound_StartsAndEndsAtSilence(float[] samples)
        {
            Assert.Less(System.Math.Abs(samples[0]), 0.01f, "a loud first sample would click");
            Assert.Less(System.Math.Abs(samples[samples.Length - 1]), 0.01f, "a loud last sample would click");
        }

        [Test]
        public void Sounds_AreTheSameEveryTime()
        {
            CollectionAssert.AreEqual(SoundSynth.Thock(), SoundSynth.Thock());
            CollectionAssert.AreEqual(SoundSynth.Chime(), SoundSynth.Chime());
        }
    }
}