using System;

namespace ChatSounds
{
    /// <summary>
    /// Renders the built-in notification sounds (additive synthesis), so the mod ships no audio files.
    /// Plain .NET, no Unity types.
    /// </summary>
    internal static class Synth
    {
        public static readonly string[] Names = { "Ping", "Chime", "Bubble", "Drop", "Marimba", "Bell", "Soft", "Alert" };

        const float PeakLevel = 0.85f;
        // Every note fades out over its last few milliseconds, so nothing ends with a click.
        const double ReleaseTime = 0.015;

        readonly struct Partial
        {
            public readonly float Ratio;
            public readonly float Amplitude;
            public readonly float Decay;

            public Partial(float ratio, float amplitude, float decay)
            {
                Ratio = ratio;
                Amplitude = amplitude;
                Decay = decay;
            }
        }

        sealed class Note
        {
            public float Start;
            public float Frequency;
            // Optional exponential glide from Frequency to EndFrequency over Glide seconds.
            public float EndFrequency;
            public float Glide;
            public float Attack = 0.003f;
            // Time constant of the exponential decay, in seconds (scaled per partial).
            public float Decay;
            public float Length;
            public float Gain = 1f;
            public Partial[] Partials;
        }

        static readonly Partial[] Sine = { new Partial(1f, 1f, 1f) };
        static readonly Partial[] Clear = { new Partial(1f, 1f, 1f), new Partial(2f, 0.22f, 0.55f), new Partial(3f, 0.07f, 0.35f) };
        static readonly Partial[] Round = { new Partial(1f, 1f, 1f), new Partial(2f, 0.12f, 0.5f) };
        static readonly Partial[] Warm = { new Partial(1f, 1f, 1f), new Partial(2f, 0.25f, 0.6f) };
        static readonly Partial[] Glass =
        {
            new Partial(1f, 1f, 1f), new Partial(2f, 0.35f, 0.6f), new Partial(3f, 0.12f, 0.4f), new Partial(4.2f, 0.05f, 0.25f)
        };
        // Marimba bars have strongly inharmonic overtones (about 1 : 3.9 : 9.2).
        static readonly Partial[] Wood = { new Partial(1f, 1f, 1f), new Partial(3.93f, 0.3f, 0.25f), new Partial(9.2f, 0.08f, 0.12f) };
        static readonly Partial[] Church =
        {
            new Partial(1f, 1f, 1f), new Partial(2f, 0.55f, 0.7f), new Partial(2.76f, 0.38f, 0.5f),
            new Partial(5.4f, 0.22f, 0.3f), new Partial(8.93f, 0.1f, 0.18f)
        };

        /// <summary>The built-in name matching <paramref name="name"/> regardless of case, or null.</summary>
        public static string Canonical(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            string trimmed = name.Trim();
            foreach (string known in Names)
            {
                if (string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase))
                    return known;
            }
            return null;
        }

        /// <summary>Mono samples in [-1, 1], or null for an unknown name.</summary>
        public static float[] Render(string name, int sampleRate)
        {
            string canonical = Canonical(name);
            Note[] notes = Preset(canonical);
            if (notes == null || sampleRate <= 0)
                return null;

            float length = 0f;
            foreach (Note note in notes)
                length = Math.Max(length, note.Start + note.Length);
            var buffer = new float[(int)Math.Ceiling(length * sampleRate) + 1];
            foreach (Note note in notes)
                RenderNote(note, buffer, sampleRate);
            Normalize(buffer, PeakLevel * Loudness(canonical));
            return buffer;
        }

        static void RenderNote(Note note, float[] buffer, int sampleRate)
        {
            int first = (int)Math.Round(note.Start * sampleRate);
            int count = Math.Min((int)Math.Round(note.Length * sampleRate), buffer.Length - first);
            double nyquist = sampleRate * 0.45;
            var phases = new double[note.Partials.Length];
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / sampleRate;
                double frequency = note.Frequency;
                if (note.Glide > 0f && note.EndFrequency > 0f)
                    frequency *= Math.Pow(note.EndFrequency / note.Frequency, Math.Min(t / note.Glide, 1.0));

                double envelope = t < note.Attack ? Math.Sin(0.5 * Math.PI * t / note.Attack) : 1.0;
                double remaining = (double)(count - 1 - i) / sampleRate;
                if (remaining < ReleaseTime)
                    envelope *= Math.Sin(0.5 * Math.PI * remaining / ReleaseTime);

                double sample = 0.0;
                for (int k = 0; k < phases.Length; k++)
                {
                    Partial partial = note.Partials[k];
                    double partialFrequency = frequency * partial.Ratio;
                    phases[k] += 2.0 * Math.PI * partialFrequency / sampleRate;
                    if (partialFrequency < nyquist)
                        sample += partial.Amplitude * Math.Exp(-t / (note.Decay * partial.Decay)) * Math.Sin(phases[k]);
                }
                buffer[first + i] += (float)(note.Gain * envelope * sample);
            }
        }

        static void Normalize(float[] buffer, float peak)
        {
            float max = 0f;
            foreach (float sample in buffer)
                max = Math.Max(max, Math.Abs(sample));
            if (max <= 0f)
                return;
            float gain = peak / max;
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] *= gain;
        }

        // Peak-normalised sounds still differ in perceived loudness: high, long or dense sounds are turned down a little.
        static float Loudness(string name)
        {
            switch (name)
            {
                case "Ping":
                    return 0.8f;
                case "Chime":
                    return 0.85f;
                case "Drop":
                    return 0.9f;
                case "Bell":
                    return 0.75f;
                case "Alert":
                    return 0.85f;
                case "Bubble":
                case "Marimba":
                    return 0.95f;
                default:
                    return 1f;
            }
        }

        static Note[] Preset(string name)
        {
            switch (name)
            {
                case "Ping":
                    return new[]
                    {
                        new Note { Frequency = 1318.51f, Attack = 0.004f, Decay = 0.16f, Length = 0.55f, Partials = Clear }
                    };
                case "Chime":
                    return new[]
                    {
                        new Note { Frequency = 783.99f, Decay = 0.3f, Length = 0.7f, Partials = Glass },
                        new Note { Start = 0.13f, Frequency = 1046.5f, Decay = 0.38f, Length = 0.85f, Partials = Glass }
                    };
                case "Bubble":
                    return new[]
                    {
                        new Note
                        {
                            Frequency = 320f, EndFrequency = 1100f, Glide = 0.07f, Attack = 0.002f, Decay = 0.055f,
                            Length = 0.16f, Partials = Round
                        },
                        new Note
                        {
                            Start = 0.085f, Frequency = 450f, EndFrequency = 1500f, Glide = 0.06f, Attack = 0.002f,
                            Decay = 0.05f, Length = 0.15f, Gain = 0.75f, Partials = Round
                        }
                    };
                case "Drop":
                    return new[]
                    {
                        new Note
                        {
                            Frequency = 1900f, EndFrequency = 650f, Glide = 0.11f, Attack = 0.002f, Decay = 0.07f,
                            Length = 0.24f, Partials = Round
                        }
                    };
                case "Marimba":
                    return new[]
                    {
                        new Note { Frequency = 659.25f, Attack = 0.0015f, Decay = 0.11f, Length = 0.42f, Partials = Wood },
                        new Note { Start = 0.11f, Frequency = 880f, Attack = 0.0015f, Decay = 0.13f, Length = 0.5f, Partials = Wood }
                    };
                case "Bell":
                    return new[]
                    {
                        new Note { Frequency = 880f, Attack = 0.002f, Decay = 0.55f, Length = 1.6f, Partials = Church }
                    };
                case "Soft":
                    return new[]
                    {
                        new Note { Frequency = 440f, Attack = 0.02f, Decay = 0.32f, Length = 0.9f, Partials = Warm },
                        new Note { Frequency = 659.25f, Attack = 0.02f, Decay = 0.3f, Length = 0.9f, Gain = 0.45f, Partials = Sine }
                    };
                case "Alert":
                    return new[]
                    {
                        new Note { Frequency = 1046.5f, Decay = 0.2f, Length = 0.45f, Partials = Clear },
                        new Note { Start = 0.09f, Frequency = 1318.51f, Decay = 0.2f, Length = 0.45f, Partials = Clear },
                        new Note { Start = 0.18f, Frequency = 1567.98f, Decay = 0.3f, Length = 0.7f, Partials = Clear }
                    };
                default:
                    return null;
            }
        }
    }
}
