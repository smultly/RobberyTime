using System.Collections;
using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Фон комнаты. Хранит два плана (обычный и «шкаф отодвинут») и умеет
    /// плавно переключаться между ними, когда игрок вводит верный код.
    /// </summary>
    public class RoomView2D : MonoBehaviour
    {
        public SpriteRenderer target;
        public Sprite closedSprite;
        public Sprite openedSprite;
        public float fadeSeconds = 0.35f;

        private Coroutine _fade;
        private bool _opened;

        public bool Opened { get { return _opened; } }

        public void Configure(SpriteRenderer renderer, Sprite closed, Sprite opened)
        {
            target = renderer;
            closedSprite = closed;
            openedSprite = opened;

            if (target != null)
            {
                target.sprite = closedSprite != null ? closedSprite : openedSprite;
                Color color = target.color;
                color.a = 1f;
                target.color = color;
            }
        }

        public void ShowOpened()
        {
            if (_opened) return;
            _opened = true;

            if (target == null || openedSprite == null)
            {
                if (target != null && openedSprite != null) target.sprite = openedSprite;
                return;
            }

            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeSwap());
        }

        private IEnumerator FadeSwap()
        {
            Color color = target.color;
            float duration = Mathf.Max(0.01f, fadeSeconds);

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                color.a = Mathf.Lerp(1f, 0.15f, t / duration);
                target.color = color;
                yield return null;
            }

            target.sprite = openedSprite;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                color.a = Mathf.Lerp(0.15f, 1f, t / duration);
                target.color = color;
                yield return null;
            }

            color.a = 1f;
            target.color = color;
            _fade = null;
        }
    }
}
