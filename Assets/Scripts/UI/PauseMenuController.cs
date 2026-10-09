using PuzzleRoom.Core;
using PuzzleRoom.Player;
using PuzzleRoom.Audio;
using PuzzleRoom.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleRoom.UI
{
    /// <summary>
    /// Owns gameplay pause and ESC priority. It runs before puzzle UI scripts so
    /// one ESC closes an open puzzle without also opening Pause in the same frame.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private KeypadUI keypadUI;
        [SerializeField] private FinalPowerPuzzleUI finalPowerPuzzleUI;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private string gameplaySceneName = "PuzzleRoom";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool movementWasEnabled;
        private bool lookWasEnabled;
        private bool interactionWasEnabled;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            Time.timeScale = 1f;
            pausePanel?.SetActive(false);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // Victory has absolute priority and never opens Pause.
            if (gameFlowManager != null && gameFlowManager.IsVictory) return;

            // Puzzle UIs process this same ESC later in the frame and close
            // themselves. Returning here prevents Pause from opening as well.
            if (!IsPaused && ((keypadUI != null && keypadUI.IsOpen) ||
                              (finalPowerPuzzleUI != null && finalPowerPuzzleUI.IsOpen)))
            {
                return;
            }

            if (IsPaused) ResumeGame();
            else PauseGame();
        }

        public void PauseGame()
        {
            if (IsPaused || (gameFlowManager != null && gameFlowManager.IsVictory)) return;

            movementWasEnabled = playerMovement != null && playerMovement.enabled;
            lookWasEnabled = playerLook != null && playerLook.enabled;
            interactionWasEnabled = playerInteraction != null && playerInteraction.enabled;

            if (playerMovement != null) playerMovement.enabled = false;
            if (playerLook != null) playerLook.enabled = false;
            if (playerInteraction != null) playerInteraction.enabled = false;

            IsPaused = true;
            GameAudio.Play(AudioCue.Pause);
            Time.timeScale = 0f;
            pausePanel?.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Shared pause request used by the mobile HUD and desktop shortcut priority.</summary>
        public void RequestPause()
        {
            if ((keypadUI != null && keypadUI.IsOpen) ||
                (finalPowerPuzzleUI != null && finalPowerPuzzleUI.IsOpen)) return;
            PauseGame();
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;

            Time.timeScale = 1f;
            pausePanel?.SetActive(false);
            IsPaused = false;
            GameAudio.Play(AudioCue.Resume);

            if (playerMovement != null) playerMovement.enabled = movementWasEnabled;
            if (playerInteraction != null) playerInteraction.enabled = interactionWasEnabled;
            if (playerLook != null)
            {
                playerLook.enabled = lookWasEnabled;
                playerLook.SetCursorLocked(lookWasEnabled);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            PowerState.ResetForNewGame();
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            PowerState.ResetForNewGame();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void SaveGame()
        {
            bool saved = SaveGameSystem.SaveCurrentGame();
            InteractionFeedback.ShowMessage(saved ? "GAME SAVED" : "SAVE FAILED", 1.5f);
            if (saved) GameAudio.Play(AudioCue.Confirm);
            else GameAudio.Play(AudioCue.Error);
        }

        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = 1f;
        }
    }
}
