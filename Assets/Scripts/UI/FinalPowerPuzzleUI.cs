using PuzzleRoom.Player;
using PuzzleRoom.Puzzles.PowerGrid;
using PuzzleRoom.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.UI
{
    public sealed class FinalPowerPuzzleUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject gridPanel;
        [SerializeField] private GameObject activationPanel;
        [SerializeField] private Text phaseStatusText;
        [SerializeField] private Text activationProgressText;
        [SerializeField] private CircuitGridManager circuitGrid;
        [SerializeField] private PlayerControlLock playerControlLock;

        private FinalPowerPuzzleManager activePuzzle;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            panel?.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Open(FinalPowerPuzzleManager puzzle)
        {
            if (puzzle == null || puzzle.IsCompleted || panel == null)
            {
                return;
            }

            Unsubscribe();
            activePuzzle = puzzle;
            activePuzzle.StateChanged += Refresh;
            panel.SetActive(true);
            GameAudio.Play(AudioCue.UIOpen);
            playerControlLock?.SetGameplayEnabled(false);
            Refresh();
        }

        public void ResetCircuit()
        {
            if (activePuzzle != null && !activePuzzle.CircuitRoutingSolved)
            {
                circuitGrid?.ResetGrid();
                Refresh();
            }
        }

        public void ActivateLight() => SubmitActivation(PowerChannel.Light);
        public void ActivateControl() => SubmitActivation(PowerChannel.Control);
        public void ActivateExit() => SubmitActivation(PowerChannel.Exit);

        public void Close()
        {
            Unsubscribe();
            panel?.SetActive(false);
            GameAudio.Play(AudioCue.UIClose);
            playerControlLock?.SetGameplayEnabled(true);
        }

        private void SubmitActivation(PowerChannel channel)
        {
            activePuzzle?.SubmitActivation(channel);
        }

        private void Refresh()
        {
            if (activePuzzle == null)
            {
                return;
            }

            bool routingSolved = activePuzzle.CircuitRoutingSolved;
            gridPanel?.SetActive(!routingSolved);
            activationPanel?.SetActive(routingSolved && !activePuzzle.IsCompleted);

            if (phaseStatusText != null)
            {
                phaseStatusText.text = routingSolved
                    ? "POWER ROUTING COMPLETE\nACTIVATION SEQUENCE REQUIRED"
                    : "FIND THE LONG ROUTE THROUGH LIGHT, CONTROL, AND EXIT";
            }

            if (activationProgressText != null)
            {
                activationProgressText.text = $"Activation: {activePuzzle.ActivationProgress}/3";
            }

            if (activePuzzle.IsCompleted)
            {
                Close();
            }
        }

        private void Unsubscribe()
        {
            if (activePuzzle != null)
            {
                activePuzzle.StateChanged -= Refresh;
                activePuzzle = null;
            }
        }
    }
}
