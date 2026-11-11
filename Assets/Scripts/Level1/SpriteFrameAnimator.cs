using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Простая покадровая анимация спрайта без Animator: листает массив кадров
    /// с заданной частотой. Поддерживает цикл «туда-обратно» для ходьбы.
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [Tooltip("Спрайт, которому подставляются кадры.")]
        public SpriteRenderer target;
        public Sprite[] frames;
        [Tooltip("Кадров в секунду.")]
        public float framesPerSecond = 8f;
        [Tooltip("Проигрывать кадры 0→1→2→1→0… (иначе по кругу).")]
        public bool pingPong = true;
        [Tooltip("Если выключено — показывается первый кадр.")]
        public bool playing = true;

        private float _time;

        public void SetFrames(Sprite[] newFrames, float fps = -1f)
        {
            frames = newFrames;
            if (fps > 0f) framesPerSecond = Mathf.Max(0.01f, fps);
            _time = 0f;
            Apply(0);
        }

        public void Play()
        {
            playing = true;
        }

        public void Stop()
        {
            playing = false;
            _time = 0f;
            Apply(0);
        }

        private void Update()
        {
            if (!playing || frames == null || frames.Length == 0)
            {
                Apply(0);
                return;
            }

            if (frames.Length == 1) return;

            _time += Time.deltaTime;

            int index;
            if (pingPong)
            {
                index = Mathf.RoundToInt(Mathf.PingPong(_time * framesPerSecond, frames.Length - 1));
            }
            else
            {
                index = Mathf.FloorToInt(_time * framesPerSecond) % frames.Length;
            }

            Apply(index);
        }

        private void Apply(int index)
        {
            if (target == null || frames == null || frames.Length == 0) return;

            index = Mathf.Clamp(index, 0, frames.Length - 1);
            Sprite sprite = frames[index];
            if (sprite != null && target.sprite != sprite) target.sprite = sprite;
        }
    }
}
