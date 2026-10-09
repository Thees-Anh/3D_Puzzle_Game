using UnityEngine;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Prototype painting clue that reveals one part of the safe code.
    /// </summary>
    public sealed class PaintingClue : MonoBehaviour, IInteractable
    {
        [SerializeField] private string clueText = "5";
        [SerializeField, Min(0.1f)] private float displayDuration = 2f;

        public string InteractionPrompt => "Inspect";

        public bool CanInteract => true;

        public void Interact(GameObject interactor)
        {
            InteractionFeedback.ShowMessage(clueText, displayDuration);
        }
    }
}
