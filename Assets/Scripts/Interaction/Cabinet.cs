using System.Collections;
using PuzzleRoom.Core;
using PuzzleRoom.Player;
using PuzzleRoom.Audio;
using UnityEngine;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Locked container that checks the player's existing item inventory.
    /// The contained reward becomes available after the first successful open.
    /// </summary>
    public sealed class Cabinet : MonoBehaviour, IInteractable
    {
        [Header("Lock")]
        [SerializeField] private bool isLocked = true;
        [SerializeField] private ItemId requiredItem = ItemId.Key;
        [SerializeField] private string lockedMessage = "Requires Key";

        [Header("Door")]
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = -100f;
        [SerializeField, Min(1f)] private float openSpeed = 120f;

        [Header("Contents")]
        [SerializeField] private GameObject containedItem;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine rotationRoutine;
        private bool isOpen;
        private bool contentsRevealed;

        public string InteractionPrompt => isOpen ? "Close Cabinet" : "Open Cabinet";
        public bool CanInteract => rotationRoutine == null;
        public bool IsLocked => isLocked;
        public bool IsOpen => isOpen;
        public bool ContentsRevealed => contentsRevealed;

        private void Awake()
        {
            if (doorPivot == null)
            {
                doorPivot = transform;
            }

            closedRotation = doorPivot.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

            if (containedItem != null && !contentsRevealed)
            {
                containedItem.SetActive(false);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (isLocked)
            {
                PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
                if (inventory == null || !inventory.HasItem(requiredItem))
                {
                    GameAudio.PlayAt(AudioCue.CabinetLocked, transform.position);
                    InteractionFeedback.ShowMessage("YOU NEED KEY", 1.2f);
                    return;
                }
                if (!inventory.HasSelectedItem(requiredItem))
                {
                    GameAudio.PlayAt(AudioCue.CabinetLocked, transform.position);
                    InteractionFeedback.ShowMessage("SELECT KEY", 1.2f);
                    return;
                }

                inventory.TryRemoveItem(requiredItem);
                isLocked = false;
                GameAudio.PlayAt(AudioCue.CabinetUnlock, transform.position);
                InteractionFeedback.ShowMessage("Cabinet Unlocked", 1.2f);
            }

            if (rotationRoutine == null)
            {
                rotationRoutine = StartCoroutine(RotateDoor(!isOpen));
            }
        }

        private IEnumerator RotateDoor(bool shouldOpen)
        {
            GameAudio.PlayAt(shouldOpen ? AudioCue.CabinetOpen : AudioCue.CabinetClose, transform.position);
            Quaternion targetRotation = shouldOpen ? openRotation : closedRotation;

            while (Quaternion.Angle(doorPivot.localRotation, targetRotation) > 0.1f)
            {
                doorPivot.localRotation = Quaternion.RotateTowards(
                    doorPivot.localRotation,
                    targetRotation,
                    openSpeed * Time.deltaTime);
                yield return null;
            }

            doorPivot.localRotation = targetRotation;
            isOpen = shouldOpen;
            rotationRoutine = null;

            if (isOpen && !contentsRevealed)
            {
                contentsRevealed = true;
                if (containedItem != null)
                {
                    containedItem.SetActive(true);
                }
            }
        }

        private void OnDisable()
        {
            rotationRoutine = null;
        }

        public void RestoreState(bool locked, bool open, bool revealed)
        {
            if (rotationRoutine != null) StopCoroutine(rotationRoutine);
            rotationRoutine = null;
            isLocked = locked;
            isOpen = open;
            contentsRevealed = revealed;
            if (doorPivot != null) doorPivot.localRotation = open ? openRotation : closedRotation;
            if (containedItem != null) containedItem.SetActive(revealed);
        }
    }
}
