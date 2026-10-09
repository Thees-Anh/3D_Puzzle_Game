using PuzzleRoom.Core;
using PuzzleRoom.Player;
using PuzzleRoom.Puzzles.PowerGrid;
using PuzzleRoom.UI;
using UnityEngine;

namespace PuzzleRoom.Interaction
{
    public sealed class FuseBox : MonoBehaviour, IInteractable
    {
        [SerializeField] private FinalPowerPuzzleManager finalPuzzle;
        [SerializeField] private FinalPowerPuzzleUI circuitUI;
        [SerializeField] private GameObject insertedFuseVisual;

        private bool fuseInserted;
        public bool FuseInserted => fuseInserted;

        public string InteractionPrompt => fuseInserted ? "Open Circuit Panel" : "Insert Fuse";
        public bool CanInteract => finalPuzzle != null && !finalPuzzle.IsCompleted;

        public void Interact(GameObject interactor)
        {
            if (!fuseInserted)
            {
                PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
                if (inventory == null || !inventory.HasItem(ItemId.Fuse))
                {
                    InteractionFeedback.ShowMessage("YOU NEED POWER FUSE", 1.4f);
                    return;
                }
                if (!inventory.HasSelectedItem(ItemId.Fuse))
                {
                    InteractionFeedback.ShowMessage("SELECT POWER FUSE", 1.4f);
                    return;
                }

                inventory.TryRemoveItem(ItemId.Fuse);

                fuseInserted = true;
                finalPuzzle.InsertFuse();
                if (insertedFuseVisual != null)
                {
                    insertedFuseVisual.SetActive(true);
                }

                InteractionFeedback.ShowMessage("Fuse Inserted", 1f);
            }

            circuitUI?.Open(finalPuzzle);
        }

        public void RestoreState(bool inserted)
        {
            fuseInserted = inserted;
            if (insertedFuseVisual != null) insertedFuseVisual.SetActive(inserted);
        }
    }
}
