using System.Collections;
using PuzzleRoom.Audio;
using PuzzleRoom.Interaction;
using PuzzleRoom.UI;
using UnityEngine;

namespace PuzzleRoom.Puzzles
{
    /// <summary>
    /// Owns the safe code, solved state, and door animation.
    /// KeypadUI is responsible only for collecting and displaying input.
    /// </summary>
    public sealed class SafePuzzle : PuzzleBase, IInteractable
    {
        [Header("Code")]
        [SerializeField] private string correctCode = "527";

        [Header("References")]
        [SerializeField] private KeypadUI keypadUI;
        [SerializeField] private Transform doorPivot;
        [SerializeField] private GameObject rewardObject;

        [Header("Door Animation")]
        [SerializeField] private float openAngle = 105f;
        [SerializeField, Min(1f)] private float openSpeed = 120f;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine openingRoutine;

        public string InteractionPrompt => "Use Keypad";

        public bool CanInteract => !IsCompleted && openingRoutine == null;

        public int CodeLength => Mathf.Max(1, correctCode.Length);

        public void RestoreState(bool completed)
        {
            RestoreCompletion(completed);
            if (openingRoutine != null) StopCoroutine(openingRoutine);
            openingRoutine = null;
            if (doorPivot != null) doorPivot.localRotation = completed ? openRotation : closedRotation;
            if (rewardObject != null) rewardObject.SetActive(completed);
        }

        private void Awake()
        {
            if (doorPivot != null)
            {
                closedRotation = doorPivot.localRotation;
                openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            }

            if (rewardObject != null)
            {
                rewardObject.SetActive(false);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!IsCompleted && keypadUI != null)
            {
                keypadUI.Open(this);
            }
        }

        public bool TrySubmitCode(string enteredCode)
        {
            if (IsCompleted || enteredCode != correctCode)
            {
                return false;
            }

            Complete();
            GameAudio.PlayAt(AudioCue.SafeUnlock, transform.position);

            if (doorPivot != null)
            {
                openingRoutine = StartCoroutine(OpenDoor());
            }
            else if (rewardObject != null)
            {
                rewardObject.SetActive(true);
            }

            return true;
        }

        private IEnumerator OpenDoor()
        {
            GameAudio.PlayAt(AudioCue.SafeDoor, doorPivot != null ? doorPivot.position : transform.position);
            while (Quaternion.Angle(doorPivot.localRotation, openRotation) > 0.1f)
            {
                doorPivot.localRotation = Quaternion.RotateTowards(
                    doorPivot.localRotation,
                    openRotation,
                    openSpeed * Time.deltaTime);
                yield return null;
            }

            doorPivot.localRotation = openRotation;

            if (rewardObject != null)
            {
                rewardObject.SetActive(true);
            }

            openingRoutine = null;
        }
    }
}
