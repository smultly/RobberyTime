using UnityEngine;

namespace LibraryEscape
{
    /// <summary>Листок с номером и цифрой кода. Подбирается один раз.</summary>
    public class PaperPickup : MonoBehaviour
    {
        public int ordinal = 1;
        public int digit = 0;
        public Level1Game game;

        public bool Collected { get; private set; }

        private Vector3 _basePosition;
        private float _phase;
        private float _hover;

        public void Configure(int paperOrdinal, int paperDigit, Level1Game owner)
        {
            ordinal = paperOrdinal;
            digit = paperDigit;
            game = owner;

            _basePosition = transform.localPosition;
            _phase = ordinal * 1.7f;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            TextMesh label = labelGo.AddComponent<TextMesh>();
            label.font = PlaceholderArt.UiFont;
            label.text = "№" + ordinal + "\n" + digit;
            label.fontSize = 64;
            label.characterSize = 0.075f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.16f, 0.13f, 0.10f);

            MeshRenderer renderer = labelGo.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = label.font.material;
                renderer.sortingOrder = 7;
            }
        }

        private void Update()
        {
            if (Collected) return;

            _hover += Time.deltaTime;
            float bob = Mathf.Sin(_hover * 2.2f + _phase) * 0.05f;
            transform.localPosition = _basePosition + new Vector3(0f, bob, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_hover * 1.3f + _phase) * 4f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Collected || game == null) return;
            if (other.GetComponent<PlayerMover2D>() == null) return;
            game.CollectPaper(this);
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true;
            gameObject.SetActive(false);
        }
    }
}
