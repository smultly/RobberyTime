using UnityEngine;

namespace LibraryEscape
{
    /// <summary>Триггер у открытой двери: после ввода кода вход в проём завершает уровень.</summary>
    public class DoorExit2D : MonoBehaviour
    {
        public SecretDoor door;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (door == null || !door.Opened) return;
            if (other.GetComponent<PlayerMover2D>() == null) return;
            if (door.game != null) door.game.Win();
        }
    }
}
