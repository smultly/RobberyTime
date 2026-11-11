using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Дверь в секретную комнату за книжным шкафом. Пока листки не собраны — заперта.
    /// После верного кода шкаф отодвигается (второй план комнаты) и проём открывается.
    /// </summary>
    public class SecretDoor : MonoBehaviour
    {
        public Level1Game game;
        public RoomView2D roomView;
        [Tooltip("Твёрдый коллайдер шкафа, который закрывает проём до ввода кода.")]
        public Collider2D cabinetBlocker;

        private float _messageCooldown;
        private bool _opened;

        public bool Opened { get { return _opened; } }

        private void Update()
        {
            if (_messageCooldown > 0f) _messageCooldown -= Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (game == null || !game.IsRunning || _opened) return;
            if (other.GetComponent<PlayerMover2D>() == null) return;

            if (game.CodeComplete)
            {
                game.OpenCodePanel();
            }
            else if (_messageCooldown <= 0f)
            {
                _messageCooldown = 3f;
                game.ShowToast("Дверь заперта. Нужно найти все листки в комодах");
            }
        }

        /// <summary>Отодвигает шкаф, включает второй план комнаты и открывает проход.</summary>
        public void OpenDoor()
        {
            if (_opened) return;

            _opened = true;

            if (cabinetBlocker != null) cabinetBlocker.enabled = false;
            if (roomView != null) roomView.ShowOpened();

            if (game != null)
            {
                game.ShowToast("Шкаф отодвинут! Дверь открыта — иди вверх", 4.5f);
            }
        }
    }
}
