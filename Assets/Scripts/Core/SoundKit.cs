using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Генератор простых тональных звуков. Вынесен из игровых классов,
    /// чтобы не дублировать создание AudioClip на каждом уровне.
    /// </summary>
    public static class SoundKit
    {
        private const int SampleRate = 44100;

        public static AudioClip Tone(string name, float frequency, float duration, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float time = (float)i / SampleRate;
                float envelope = Mathf.Clamp01((samples - i) / (samples * 0.3f));
                envelope *= Mathf.Clamp01(i / (SampleRate * 0.008f));
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Тихий шаг: короткий шумовой всплеск с мягким низким ударом.
        /// Seed задаёт лёгкую разницу между вариантами, чтобы шаги не повторялись один в один.
        /// </summary>
        public static AudioClip Footstep(string name, int seed, float volume = 1f)
        {
            const float duration = 0.12f;
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[samples];

            System.Random random = new System.Random(seed);
            float frequency = 92f + seed % 5 * 7f;
            float smoothed = 0f;

            for (int i = 0; i < samples; i++)
            {
                float time = (float)i / SampleRate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                smoothed += (noise - smoothed) * 0.12f;

                float attack = Mathf.Clamp01(i / (SampleRate * 0.004f));
                float decay = Mathf.Exp(-time * 34f);
                float thump = Mathf.Sin(2f * Mathf.PI * frequency * time) * Mathf.Exp(-time * 42f);

                data[i] = (smoothed * 0.5f + thump * 0.5f) * attack * decay * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
