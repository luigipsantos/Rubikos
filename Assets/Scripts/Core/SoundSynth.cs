using System;

namespace Rubikos.Core
{
    /// <summary>
    /// Makes short sound effects as raw samples (numbers between -1 and 1), so the game needs no audio files.
    /// Pure C#: Unity turns the samples into an AudioClip elsewhere.
    /// </summary>
    public static class SoundSynth
    {
        public const int SampleRate = 44100;

        /// <summary>A short wooden "thock": a low tone whose pitch falls quickly, plus a tiny burst of noise.</summary>
        public static float[] Thock()
        {
            var random = new Random(1234);
            var samples = new float[Samples(0.09f)];

            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)SampleRate;
                double tone = Math.Sin(2 * Math.PI * (220 * t - 450 * t * t)); // pitch slides from 220 Hz downwards
                double noise = random.NextDouble() * 2 - 1;
                double body = Math.Exp(-45 * t);
                double click = Math.Exp(-200 * t);
                samples[i] = (float)((0.8 * tone + 0.25 * noise * click) * body);
            }
            return Finish(samples, 0.9f);
        }

        /// <summary>A rising four-note "ta-da" (C, E, G, C) for solving the cube.</summary>
        public static float[] Chime()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            const float spacing = 0.12f;  // seconds between the starts of the notes
            const float length = 0.45f;   // each note rings for this long, so they overlap

            var samples = new float[Samples(spacing * (notes.Length - 1)) + Samples(length)];
            for (int n = 0; n < notes.Length; n++)
                AddTone(samples, Samples(spacing * n), notes[n], length, 6f);

            return Finish(samples, 0.9f);
        }

        /// <summary>A low, short buzz for "not that move".</summary>
        public static float[] Buzz()
        {
            var samples = new float[Samples(0.18f)];
            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)SampleRate;
                double square = Math.Sign(Math.Sin(2 * Math.PI * 130 * t));
                double sub = Math.Sin(2 * Math.PI * 65 * t);
                samples[i] = (float)((0.6 * square + 0.4 * sub) * Math.Exp(-8 * t));
            }
            return Finish(samples, 0.5f);
        }

        // Adds a note (the tone plus a quieter octave above) into the buffer, starting at 'start'.
        private static void AddTone(float[] buffer, int start, float frequency, float seconds, float decay)
        {
            int length = Math.Min(Samples(seconds), buffer.Length - start);
            int fade = Samples(0.01f);

            for (int i = 0; i < length; i++)
            {
                double t = i / (double)SampleRate;
                double phase = 2 * Math.PI * frequency * t;
                double wave = Math.Sin(phase) + 0.3 * Math.Sin(2 * phase);
                double fadeOut = Math.Min(1.0, (length - 1 - i) / (double)fade); // no click when the note ends
                buffer[start + i] += (float)(wave * Math.Exp(-decay * t) * fadeOut);
            }
        }

        // Scales the sound to the given loudest value, and smooths the first and last milliseconds
        // so the sound never starts or ends with a "click".
        private static float[] Finish(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float sample in samples) max = Math.Max(max, Math.Abs(sample));
            float gain = max > 0f ? peak / max : 1f;

            int attack = Samples(0.002f);
            int fade = Samples(0.005f);

            for (int i = 0; i < samples.Length; i++)
            {
                float envelope = i < attack ? i / (float)attack : 1f;
                int fromEnd = samples.Length - 1 - i;
                if (fromEnd < fade) envelope = Math.Min(envelope, fromEnd / (float)fade);
                samples[i] *= gain * envelope;
            }
            return samples;
        }

        private static int Samples(float seconds) => (int)(seconds * SampleRate);
    }
}