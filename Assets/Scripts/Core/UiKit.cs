using UnityEngine;
using UnityEngine.UI;

namespace LibraryEscape
{
    /// <summary>
    /// Фабрика UI-элементов uGUI. Одно место, где создаются RectTransform, Text и Image,
    /// чтобы экраны меню и уровней не дублировали разметку.
    /// </summary>
    public static class UiKit
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            RectTransform rect = Rect(name, parent, anchor, anchor, position, size);

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = PlaceholderArt.UiFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Image MakeImage(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 size, Color color, Sprite sprite)
        {
            RectTransform rect = Rect(name, parent, anchor, anchor, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = sprite != null;
            return image;
        }

        /// <summary>Полноэкранный слой (оверлей) с затемнением.</summary>
        public static RectTransform FullScreenLayer(string name, Transform parent, Color dimColor)
        {
            RectTransform rect = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image dim = rect.gameObject.AddComponent<Image>();
            dim.color = dimColor;
            dim.raycastTarget = false;
            return rect;
        }

        /// <summary>Канвас HUD: Screen Space Overlay, масштабирование под 1920×1080.</summary>
        public static Transform CreateHudCanvas(string name, Transform parent)
        {
            GameObject canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(parent, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas.transform;
        }

        public static bool Contains(RectTransform rect, Vector2 screenPoint)
        {
            return rect != null && rect.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }
    }
}
