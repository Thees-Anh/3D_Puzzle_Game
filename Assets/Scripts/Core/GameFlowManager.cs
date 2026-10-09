using PuzzleRoom.Player;
using PuzzleRoom.UI;
using PuzzleRoom.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleRoom.Core
{
    /// <summary>Owns the one-way transition from gameplay to victory.</summary>
    public sealed class GameFlowManager : MonoBehaviour
    {
        [SerializeField] private GameTimer gameTimer;
        [SerializeField] private PlayerControlLock playerControlLock;
        [SerializeField] private VictoryUI victoryUI;
        [SerializeField] private string gameplaySceneName = "PuzzleRoom";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Optional Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip victorySound;

        public bool IsVictory { get; private set; }

        public void TriggerVictory()
        {
            if (IsVictory || !PowerState.IsRestored)
            {
                return;
            }

            IsVictory = true;
            string completionTime = gameTimer != null ? gameTimer.StopTimer() : "00:00";
            playerControlLock?.SetGameplayEnabled(false);
            victoryUI?.Show(completionTime);
            AudioManager.Instance?.PlayVictoryAudio();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (audioSource != null && victorySound != null)
            {
                audioSource.PlayOneShot(victorySound);
            }
        }

        public void PlayAgain()
        {
            Time.timeScale = 1f;
            PowerState.ResetForNewGame();
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            PowerState.ResetForNewGame();

            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogWarning($"Main Menu scene '{mainMenuSceneName}' is not in Build Settings.");
            }
        }
    }
}
