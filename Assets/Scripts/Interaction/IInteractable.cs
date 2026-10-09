using UnityEngine;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Common contract for objects the player can interact with.
    /// The player interaction system can depend on this interface instead of
    /// knowing about safes, books, doors, or other concrete object types.
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }

        bool CanInteract { get; }

        void Interact(GameObject interactor);
    }
}
