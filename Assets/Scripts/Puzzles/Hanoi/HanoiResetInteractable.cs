using PuzzleRoom.Interaction;
using UnityEngine;

namespace PuzzleRoom.Puzzles.Hanoi
{
    public sealed class HanoiResetInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private HanoiPuzzleManager puzzleManager;

        public string InteractionPrompt => "Reset Hanoi Puzzle";
        public bool CanInteract => puzzleManager != null && puzzleManager.CanReset;

        public void Interact(GameObject interactor)
        {
            puzzleManager.ResetPuzzle();
        }
    }
}
