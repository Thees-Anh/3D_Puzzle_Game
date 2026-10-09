using PuzzleRoom.Interaction;
using PuzzleRoom.Audio;
using PuzzleRoom.Puzzles.Hanoi;
using UnityEngine;

namespace PuzzleRoom.Puzzles.SymbolUnlock
{
    public sealed class SymbolSequencePuzzle : PuzzleBase
    {
        [SerializeField] private PuzzleSymbol[] correctSequence =
        {
            PuzzleSymbol.Moon,
            PuzzleSymbol.Triangle,
            PuzzleSymbol.Diamond,
            PuzzleSymbol.Sun
        };
        [SerializeField] private HanoiPuzzleManager hanoiPuzzle;
        [SerializeField] private GameObject unlockedIndicator;

        private PuzzleSymbol[] currentInput;
        private int currentInputIndex;

        public bool CanAcceptInput => !IsCompleted;
        public int CurrentInputIndex => currentInputIndex;

        public void RestoreState(bool completed)
        {
            RestoreCompletion(completed);
            currentInputIndex = 0;
            if (unlockedIndicator != null) unlockedIndicator.SetActive(completed);
            if (completed) hanoiPuzzle?.RestoreUnlockOnly();
        }

        private void Awake()
        {
            currentInput = new PuzzleSymbol[correctSequence != null ? correctSequence.Length : 0];

            if (unlockedIndicator != null)
            {
                unlockedIndicator.SetActive(false);
            }
        }

        public void RegisterSymbol(PuzzleSymbol symbol)
        {
            if (!CanAcceptInput || correctSequence == null || correctSequence.Length == 0)
            {
                return;
            }

            if (currentInput == null || currentInput.Length != correctSequence.Length)
            {
                currentInput = new PuzzleSymbol[correctSequence.Length];
            }

            currentInput[currentInputIndex] = symbol;
            currentInputIndex++;
            InteractionFeedback.ShowMessage($"Input {currentInputIndex}/{correctSequence.Length}", 0.8f);

            if (currentInputIndex < correctSequence.Length)
            {
                return;
            }

            bool isCorrect = true;
            for (int index = 0; index < correctSequence.Length; index++)
            {
                if (currentInput[index] != correctSequence[index])
                {
                    isCorrect = false;
                    break;
                }
            }

            if (!isCorrect)
            {
                currentInputIndex = 0;
                GameAudio.Play(AudioCue.SymbolWrong);
                InteractionFeedback.ShowMessage("Incorrect Sequence", 1.3f);
                return;
            }

            Complete();
            GameAudio.Play(AudioCue.SymbolComplete);
            if (unlockedIndicator != null)
            {
                unlockedIndicator.SetActive(true);
            }

            hanoiPuzzle?.UnlockPuzzle();
        }
    }
}
