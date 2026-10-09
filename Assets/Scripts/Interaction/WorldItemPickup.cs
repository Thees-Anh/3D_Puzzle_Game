using PuzzleRoom.Core;
using PuzzleRoom.Player;
using PuzzleRoom.Audio;
using UnityEngine;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Generic world pickup used by UV Light, Key, Fuse, and future unique items.
    /// </summary>
    public sealed class WorldItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemId itemId;
        [SerializeField] private string displayName = "Item";

        public string InteractionPrompt => $"Pick Up {displayName}";

        public bool CanInteract => gameObject.activeInHierarchy;
        public ItemId ItemId => itemId;

        public void Interact(GameObject interactor)
        {
            PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();

            if (inventory == null || !inventory.TryAddItem(itemId))
            {
                return;
            }

            InteractionFeedback.ShowMessage($"Picked up {displayName}", 1.5f);
            GameAudio.PlayAt(itemId switch
            {
                ItemId.UVLight => AudioCue.PickupUV,
                ItemId.Key => AudioCue.PickupKey,
                ItemId.Fuse => AudioCue.PickupFuse,
                _ => AudioCue.Confirm
            }, transform.position);
            gameObject.SetActive(false);
        }
    }
}
