using System.Collections;
using PuzzleRoom.Audio;
using PuzzleRoom.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.Core
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private enum MenuState { Main, Settings, Confirmation, Loading }

        [SerializeField] private string gameplaySceneName = "PuzzleRoom";
        [SerializeField] private CanvasGroup mainPanel;
        [SerializeField] private MainMenuSettingsUI settingsUI;
        [SerializeField] private ConfirmationDialog confirmationDialog;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private Button continueButton;
        [SerializeField, Range(.1f, 2f)] private float fadeDuration = .55f;

        private MenuState state;
        private bool transitionStarted;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            MainMenuTypography.Apply(transform);
            if (continueButton != null) continueButton.interactable = SaveGameSystem.HasValidSave;
            settingsUI?.ApplySavedSettings();
            ShowMainImmediately();
        }

        private void Start()
        {
            if (fadeOverlay != null) StartCoroutine(Fade(1f, 0f, fadeDuration, false));
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape) || state == MenuState.Loading) return;
            if (state == MenuState.Confirmation) confirmationDialog.Cancel();
            else if (state == MenuState.Settings) CloseSettings();
            else ShowQuitConfirmation();
        }

        public void PlayGame()
        {
            if (transitionStarted) return;
            if (SaveGameSystem.HasValidSave)
            {
                state = MenuState.Confirmation;
                SetMainPanelInteractable(false);
                confirmationDialog.Show(
                    "START NEW GAME",
                    "Start a new game?\n\nYour saved progress will be overwritten.",
                    ConfirmNewGame,
                    CancelConfirmation,
                    "START NEW GAME");
                return;
            }
            BeginNewGame();
        }

        public void ContinueGame()
        {
            if (transitionStarted || !SaveGameSystem.HasValidSave) return;
            SaveGameSystem.RequestContinue();
            BeginLoad(false);
        }

        public void OpenSettings()
        {
            if (state != MenuState.Main) return;
            state = MenuState.Settings;
            SetMainPanelVisible(false);
            settingsUI.Open();
            GameAudio.Play(AudioCue.UIOpen);
        }

        public void CloseSettings()
        {
            if (state != MenuState.Settings) return;
            settingsUI.Close();
            SetMainPanelVisible(true);
            state = MenuState.Main;
            GameAudio.Play(AudioCue.UIClose);
        }

        public void ShowQuitConfirmation()
        {
            if (state != MenuState.Main) return;
            state = MenuState.Confirmation;
            SetMainPanelInteractable(false);
            confirmationDialog.Show("QUIT GAME", "Quit the game?", QuitGame, CancelConfirmation, "QUIT");
        }

        public void QuitGame()
        {
            Debug.Log("Quit Game requested.");
            Application.Quit();
        }

        private void BeginNewGame()
        {
            SaveGameSystem.DeleteSave();
            BeginLoad(true);
        }

        private void ConfirmNewGame() => BeginNewGame();

        private void BeginLoad(bool resetProgress)
        {
            transitionStarted = true;
            state = MenuState.Loading;
            SetMainPanelInteractable(false);
            confirmationDialog.HideImmediate();
            Time.timeScale = 1f;
            if (resetProgress) PowerState.ResetForNewGame();
            StartCoroutine(LoadGameplay());
        }

        private IEnumerator LoadGameplay()
        {
            if (fadeOverlay != null) yield return Fade(0f, 1f, fadeDuration, true);
            SceneManager.LoadScene(gameplaySceneName);
        }

        private IEnumerator Fade(float from, float to, float duration, bool blockRaycasts)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.blocksRaycasts = blockRaycasts;
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                fadeOverlay.alpha = Mathf.Lerp(from, to, time / duration);
                yield return null;
            }
            fadeOverlay.alpha = to;
            if (to <= 0f)
            {
                fadeOverlay.blocksRaycasts = false;
                fadeOverlay.gameObject.SetActive(false);
            }
        }

        private void CancelConfirmation()
        {
            SetMainPanelInteractable(true);
            state = MenuState.Main;
        }

        private void ShowMainImmediately()
        {
            settingsUI?.CloseImmediate();
            confirmationDialog?.HideImmediate();
            SetMainPanelVisible(true);
            state = MenuState.Main;
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 1f;
                fadeOverlay.blocksRaycasts = true;
            }
        }

        private void SetMainPanelVisible(bool visible)
        {
            if (mainPanel == null) return;
            mainPanel.alpha = visible ? 1f : 0f;
            mainPanel.interactable = visible;
            mainPanel.blocksRaycasts = visible;
        }

        private void SetMainPanelInteractable(bool interactable)
        {
            if (mainPanel == null) return;
            mainPanel.interactable = interactable;
            mainPanel.blocksRaycasts = interactable;
        }
    }
}
