using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Процедурные заглушки спрайтов и материалов, чтобы уровень запускался без готовых ассетов.
    /// </summary>
    public static class PlaceholderArt
    {
        private static Sprite _square;
        private static Sprite _disc;
        private static Sprite _paper;
        private static Sprite _heart;
        private static Sprite _shield;
        private static Font _font;

        public static Sprite Square
        {
            get
            {
                if (_square == null) _square = CreateSquare();
                return _square;
            }
        }

        public static Sprite Disc
        {
            get
            {
                if (_disc == null) _disc = CreateDisc(64, Color.white, new Color(0f, 0f, 0f, 0.55f));
                return _disc;
            }
        }

        public static Sprite Paper
        {
            get
            {
                if (_paper == null) _paper = CreatePaper(56, 72);
                return _paper;
            }
        }

        public static Sprite Heart
        {
            get
            {
                if (_heart == null) _heart = CreateHeart(48);
                return _heart;
            }
        }

        public static Sprite Shield
        {
            get
            {
                if (_shield == null) _shield = CreateRing(128, 0.80f);
                return _shield;
            }
        }

        public static Font UiFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }
                return _font;
            }
        }

        /// <summary>Создаёт объект со SpriteRenderer и растягивает спрайт до нужного размера в юнитах.</summary>
        public static SpriteRenderer AddSprite(string name, Transform parent, Sprite sprite, Color color,
            Vector2 localPosition, Vector2 worldSize, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            FitToSize(go.transform, sprite, worldSize);
            return sr;
        }

        public static void FitToSize(Transform target, Sprite sprite, Vector2 worldSize)
        {
            if (target == null || sprite == null) return;
            Vector3 size = sprite.bounds.size;
            target.localScale = new Vector3(
                worldSize.x / Mathf.Max(0.0001f, size.x),
                worldSize.y / Mathf.Max(0.0001f, size.y),
                1f);
        }

        public static Material CreateVisionMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material material = new Material(shader);
            material.name = "Vision Cone";
            material.color = color;
            return material;
        }

        /// <summary>Меш конуса зрения: вершина в (0,0), раскрытие вдоль оси +X.</summary>
        public static Mesh CreateConeMesh(float distance, float angleDegrees, int segments)
        {
            segments = Mathf.Clamp(segments, 3, 128);
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            float half = angleDegrees * 0.5f * Mathf.Deg2Rad;

            for (int i = 0; i <= segments; i++)
            {
                float angle = -half + 2f * half * i / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            Mesh mesh = new Mesh();
            mesh.name = "Vision Cone";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Sprite CreateSquare()
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.name = "Placeholder Square";
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f, 0,
                SpriteMeshType.FullRect);
        }

        private static Sprite CreateDisc(int size, Color fill, Color rim)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Placeholder Disc";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float radius = size * 0.5f - 0.5f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    Color color;
                    if (distance <= radius - 2f) color = fill;
                    else if (distance <= radius) color = rim;
                    else color = new Color(0f, 0f, 0f, 0f);
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0,
                SpriteMeshType.FullRect);
        }

        private static Sprite CreatePaper(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Placeholder Paper";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color paperColor = Color.white;
            Color borderColor = new Color(0.72f, 0.68f, 0.60f);
            Color sealColor = new Color(0.78f, 0.20f, 0.18f);

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool border = x < 2 || y < 2 || x >= width - 2 || y >= height - 2;
                    Color color = border ? borderColor : paperColor;

                    float dx = x - (width - 9f);
                    float dy = y - 9f;
                    if (dx * dx + dy * dy <= 12f) color = sealColor;

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height, 0,
                SpriteMeshType.FullRect);
        }

        private static Sprite CreateHeart(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Placeholder Heart";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size * 2f - 1f) * 1.25f;
                    // Ось Y в текстуре идёт снизу вверх, поэтому без минуса:
                    // острый кончик сердца должен смотреть вниз.
                    float ny = ((y + 0.5f) / size * 2f - 1f) * 1.25f + 0.18f;

                    float baseValue = nx * nx + ny * ny - 1f;
                    float f = baseValue * baseValue * baseValue - nx * nx * ny * ny * ny;
                    pixels[y * size + x] = f <= 0f ? Color.white : new Color(0f, 0f, 0f, 0f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0,
                SpriteMeshType.FullRect);
        }

        /// <summary>Кольцо для визуализации щита: центр прозрачный, края мягкие.</summary>
        private static Sprite CreateRing(int size, float innerRatio)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Placeholder Shield";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float outer = size * 0.5f - 1f;
            float inner = outer * Mathf.Clamp01(innerRatio);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f;
                    float dy = y + 0.5f - size * 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha;
                    if (distance > outer) alpha = 0f;
                    else if (distance > outer - 2f) alpha = Mathf.InverseLerp(outer, outer - 2f, distance);
                    else if (distance >= inner) alpha = 1f;
                    else if (distance > inner - 3f) alpha = Mathf.InverseLerp(inner - 3f, inner, distance) * 0.55f;
                    else alpha = 0f;

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0,
                SpriteMeshType.FullRect);
        }
    }
}
