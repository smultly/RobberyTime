using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Процедурные спрайты банковского хранилища: бетонный пол, стопки денег,
    /// камера, ящик и вентиляционная решётка. Всё в пиксельном стиле, PPU 20.
    /// </summary>
    public static class VaultArt
    {
        private static Sprite _floor;
        private static Sprite _money;
        private static Sprite _cameraBody;
        private static Sprite _crate;
        private static Sprite _grate;

        public static Sprite Floor
        {
            get
            {
                if (_floor == null) _floor = CreateFloor();
                return _floor;
            }
        }

        public static Sprite Money
        {
            get
            {
                if (_money == null) _money = CreateMoney();
                return _money;
            }
        }

        public static Sprite CameraBody
        {
            get
            {
                if (_cameraBody == null) _cameraBody = CreateCamera();
                return _cameraBody;
            }
        }

        public static Sprite Crate
        {
            get
            {
                if (_crate == null) _crate = CreateCrate();
                return _crate;
            }
        }

        public static Sprite Grate
        {
            get
            {
                if (_grate == null) _grate = CreateGrate();
                return _grate;
            }
        }

        private static Sprite Make(string name, int width, int height, Color[] pixels, TextureWrapMode wrap)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = wrap;
            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 20f, 0,
                SpriteMeshType.FullRect);
        }

        private static Color[] Blank(int width, int height)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);
            return pixels;
        }

        private static void Set(Color[] pixels, int width, int x, int y, Color color)
        {
            pixels[y * width + x] = color;
        }

        private static void FillRect(Color[] pixels, int width, int x0, int y0, int w, int h, Color color)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    pixels[y * width + x] = color;
                }
            }
        }

        private static Sprite CreateFloor()
        {
            const int size = 40;
            Color[] pixels = Blank(size, size);

            Color baseColor = new Color(0.205f, 0.215f, 0.255f);
            Color lineColor = new Color(0.140f, 0.150f, 0.180f);
            Color bevelColor = new Color(0.240f, 0.252f, 0.295f);
            Color speckColor = new Color(0.175f, 0.185f, 0.220f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color color = baseColor;

                    if (x == 0 || y == 0) color = lineColor;
                    else if (x == 1 || y == 1) color = bevelColor;
                    else if ((x * 7 + y * 13) % 41 == 0) color = speckColor;
                    else if ((x * 11 + y * 5) % 67 == 0) color = bevelColor;

                    Set(pixels, size, x, y, color);
                }
            }

            return Make("Vault Floor", size, size, pixels, TextureWrapMode.Repeat);
        }

        private static Sprite CreateMoney()
        {
            const int w = 16;
            const int h = 12;
            Color[] pixels = Blank(w, h);

            Color dark = new Color(0.130f, 0.280f, 0.145f);
            Color mid = new Color(0.215f, 0.470f, 0.220f);
            Color light = new Color(0.330f, 0.640f, 0.300f);
            Color strap = new Color(0.855f, 0.800f, 0.450f);
            Color outline = new Color(0.070f, 0.150f, 0.080f);

            FillRect(pixels, w, 0, 0, w, h, outline);
            FillRect(pixels, w, 1, 1, w - 2, h - 2, dark);
            FillRect(pixels, w, 1, 4, w - 2, 4, mid);
            FillRect(pixels, w, 1, 8, w - 2, 3, light);

            // Банковская лента по центру.
            FillRect(pixels, w, 7, 1, 2, h - 2, strap);

            return Make("Vault Money", w, h, pixels, TextureWrapMode.Clamp);
        }

        private static Sprite CreateCamera()
        {
            const int w = 20;
            const int h = 12;
            Color[] pixels = Blank(w, h);

            Color dark = new Color(0.105f, 0.115f, 0.145f);
            Color body = new Color(0.235f, 0.255f, 0.310f);
            Color light = new Color(0.320f, 0.345f, 0.410f);
            Color lens = new Color(0.950f, 0.280f, 0.240f);

            // Крепление сверху.
            FillRect(pixels, w, 8, 10, 4, 2, dark);

            // Корпус.
            FillRect(pixels, w, 1, 1, w - 2, 9, dark);
            FillRect(pixels, w, 2, 2, w - 4, 7, body);
            FillRect(pixels, w, 2, 8, w - 4, 1, light);

            // Объектив снизу по центру.
            FillRect(pixels, w, 8, 0, 4, 3, dark);
            FillRect(pixels, w, 9, 1, 2, 2, lens);

            return Make("Vault Camera", w, h, pixels, TextureWrapMode.Clamp);
        }

        private static Sprite CreateCrate()
        {
            const int size = 24;
            Color[] pixels = Blank(size, size);

            Color dark = new Color(0.180f, 0.115f, 0.070f);
            Color wood = new Color(0.420f, 0.285f, 0.175f);
            Color light = new Color(0.520f, 0.365f, 0.230f);

            FillRect(pixels, size, 0, 0, size, size, dark);
            FillRect(pixels, size, 1, 1, size - 2, size - 2, wood);
            FillRect(pixels, size, 1, size - 4, size - 2, 3, light);
            FillRect(pixels, size, 1, 2, size - 2, 2, dark);

            // Крест-накрест.
            for (int i = 0; i < size - 4; i++)
            {
                Set(pixels, size, 2 + i, 3 + i, light);
                Set(pixels, size, size - 3 - i, 3 + i, light);
            }

            return Make("Vault Crate", size, size, pixels, TextureWrapMode.Clamp);
        }

        private static Sprite CreateGrate()
        {
            const int w = 24;
            const int h = 18;
            Color[] pixels = Blank(w, h);

            Color frame = new Color(0.230f, 0.250f, 0.300f);
            Color light = new Color(0.360f, 0.390f, 0.450f);
            Color slot = new Color(0.065f, 0.072f, 0.092f);

            FillRect(pixels, w, 0, 0, w, h, frame);
            FillRect(pixels, w, 2, 2, w - 4, h - 4, slot);

            for (int row = 0; row < 3; row++)
            {
                FillRect(pixels, w, 2, 3 + row * 5, w - 4, 2, light);
            }

            return Make("Vault Grate", w, h, pixels, TextureWrapMode.Clamp);
        }
    }
}
