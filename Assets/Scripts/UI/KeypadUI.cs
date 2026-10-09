using PuzzleRoom.Player;
using PuzzleRoom.Puzzles;
using PuzzleRoom.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.UI
{
    /// <summary>
    /// Modal keypad presentation and input. The active SafePuzzle validates the code.
    /// </summary>
    public sealed class KeypadUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text codeDisplay;
        [SerializeField] private Text statusText;
        [SerializeField] private PlayerControlLock playerControlLock;

        private SafePuzzle activePuzzle;
        private string currentInput = string.Empty;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            foreach (char character in Input.inputString)
            {
                if (char.IsDigit(character))
                {
                    PressDigit(character.ToString());
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Clear();
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Submit();
            }
        }

        public void Open(SafePuzzle puzzle)
        {
            if (puzzle == null || panel == null)
            {
                return;
            }

            activePuzzle = puzzle;
            currentInput = string.Empty;
            panel.SetActive(true);
            GameAudio.Play(AudioCue.UIOpen);
            SetStatus(string.Empty);
            RefreshDisplay();

            if (playerControlLock != null)
            {
                playerControlLock.SetGameplayEnabled(false);
            }
        }

        public void PressDigit(string digit)
        {
            if (!IsOpen || activePuzzle == null || string.IsNullOrEmpty(digit))
            {
                return;
            }

            if (currentInput.Length >= activePuzzle.CodeLength)
            {
                return;
            }

            currentInput += digit[0];
            GameAudio.Play(AudioCue.SafeKey);
            SetStatus(string.Empty);
            RefreshDisplay();
        }

        public void Clear()
        {
            currentInput = string.Empty;
            GameAudio.Play(AudioCue.SafeClear);
            SetStatus(string.Empty);
            RefreshDisplay();
        }

        public void Submit()
        {
            if (!IsOpen || activePuzzle == null)
            {
                return;
            }

            GameAudio.Play(AudioCue.SafeSubmit);
            if (activePuzzle.TrySubmitCode(currentInput))
            {
                GameAudio.Play(AudioCue.SafeCorrect);
                Close();
                return;
            }

            currentInput = string.Empty;
            GameAudio.Play(AudioCue.SafeWrong);
            SetStatus("Incorrect Code");
            RefreshDisplay();
        }

        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
            GameAudio.Play(AudioCue.UIClose);

            activePuzzle = null;
            currentInput = string.Empty;

            if (playerControlLock != null)
            {
                playerControlLock.SetGameplayEnabled(true);
            }
        }

        private void RefreshDisplay()
        {
            if (codeDisplay != null)
            {
                codeDisplay.text = currentInput.PadRight(activePuzzle != null ? activePuzzle.CodeLength : 3, '_');
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
