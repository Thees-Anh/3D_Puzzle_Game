using PuzzleRoom.Interaction;
using UnityEngine;

namespace PuzzleRoom.Puzzles.Hanoi
{
    public sealed class HanoiBook : MonoBehaviour, IInteractable
    {
        [SerializeField] private BookSize size;
        [SerializeField] private HanoiPuzzleManager puzzleManager;
        [SerializeField, Min(0f)] private float selectedLift = 0.12f;

        private bool isSelected;

        public BookSize Size => size;
        public HanoiPeg CurrentPeg { get; internal set; }

        public string InteractionPrompt => puzzleManager != null && !puzzleManager.IsUnlocked
            ? "Books Locked"
            : isSelected
                ? "Cancel Selection"
                : $"Select {size} Book";

        public bool CanInteract => puzzleManager != null && puzzleManager.CanInteractWithBook(this);

        public void Interact(GameObject interactor)
        {
            puzzleManager.HandleBookInteraction(this);
        }

        public void SetSelectedVisual(bool selected)
        {
            if (isSelected == selected)
            {
                return;
            }

            transform.position += Vector3.up * (selected ? selectedLift : -selectedLift);
            isSelected = selected;
        }
    }
}
