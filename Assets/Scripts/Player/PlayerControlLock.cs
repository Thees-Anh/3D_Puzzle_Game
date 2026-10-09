using UnityEngine;

namespace PuzzleRoom.Player
{
    /// <summary>
    /// Enables or disables the gameplay controls while a modal UI is open.
    /// </summary>
    public sealed class PlayerControlLock : MonoBehaviour
    {
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private PlayerInputProvider inputProvider;

        public void SetGameplayEnabled(bool isEnabled)
        {
            if (!isEnabled)
            {
                if (inputProvider == null) inputProvider = GetComponent<PlayerInputProvider>();
                inputProvider?.ResetMobileInput();
            }
            if (playerMovement != null)
            {
                playerMovement.enabled = isEnabled;
            }

            if (playerInteraction != null)
            {
                playerInteraction.enabled = isEnabled;
            }

            if (playerLook != null)
            {
                playerLook.enabled = isEnabled;
                playerLook.SetCursorLocked(isEnabled);
            }
        }
    }
}
