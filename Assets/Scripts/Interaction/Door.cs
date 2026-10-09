using System.Collections;
using PuzzleRoom.Audio;
using UnityEngine;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Reusable hinged door for room doors and cabinet doors.
    /// External systems can call Lock or Unlock without knowing how the door animates.
    /// </summary>
    public sealed class Door : MonoBehaviour, IInteractable
    {
        [Header("Door State")]
        [SerializeField] private bool isLocked;
        [SerializeField] private bool isOpen;

        [Header("Rotation")]
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField, Min(1f)] private float openSpeed = 120f;

        [Header("Feedback")]
        [SerializeField] private string lockedMessage = "Locked";
        [SerializeField, Min(0.1f)] private float messageDuration = 1.2f;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine rotationRoutine;

        public string InteractionPrompt => isOpen ? "Close" : "Open";

        public bool CanInteract => rotationRoutine == null;

        public bool IsOpen => isOpen;

        public bool IsLocked => isLocked;

        private void Awake()
        {
            if (doorPivot == null)
            {
                doorPivot = transform;
            }

            closedRotation = doorPivot.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

            if (isOpen)
            {
                doorPivot.localRotation = openRotation;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (isLocked)
            {
                GameAudio.PlayAt(AudioCue.DoorLocked, transform.position);
                InteractionFeedback.ShowMessage(lockedMessage, messageDuration);
                return;
            }

            if (rotationRoutine == null)
            {
                rotationRoutine = StartCoroutine(RotateDoor(!isOpen));
            }
        }

        public void Lock()
        {
            isLocked = true;
        }

        public void Unlock()
        {
            if (isLocked) GameAudio.PlayAt(AudioCue.ExitUnlock, transform.position);
            isLocked = false;
        }

        public void SetLocked(bool shouldLock)
        {
            isLocked = shouldLock;
        }

        public void RestoreState(bool locked, bool open)
        {
            if (rotationRoutine != null) StopCoroutine(rotationRoutine);
            rotationRoutine = null;
            isLocked = locked;
            isOpen = open;
            if (doorPivot != null) doorPivot.localRotation = open ? openRotation : closedRotation;
        }

        private IEnumerator RotateDoor(bool shouldOpen)
        {
            GameAudio.PlayAt(shouldOpen ? AudioCue.DoorOpen : AudioCue.DoorClose, transform.position);
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
        }

        private void OnDisable()
        {
            rotationRoutine = null;
        }
    }
}
