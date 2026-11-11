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
    }
}
