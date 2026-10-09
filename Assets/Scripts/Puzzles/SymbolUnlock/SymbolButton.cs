using System.Collections;
using PuzzleRoom.Interaction;
using PuzzleRoom.Audio;
using UnityEngine;

namespace PuzzleRoom.Puzzles.SymbolUnlock
{
    public sealed class SymbolButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private PuzzleSymbol symbol;
        [SerializeField] private SymbolSequencePuzzle sequencePuzzle;
        [SerializeField] private Vector3 pressOffset = new Vector3(0.06f, 0f, 0f);
        [SerializeField, Min(0.01f)] private float pressSpeed = 0.5f;

        private Vector3 restingPosition;
        private Coroutine feedbackRoutine;

        public string InteractionPrompt => $"Press {symbol} Symbol";
        public bool CanInteract => sequencePuzzle != null && sequencePuzzle.CanAcceptInput && feedbackRoutine == null;

        private void Awake()
        {
            restingPosition = transform.localPosition;
        }

        public void Interact(GameObject interactor)
        {
            GameAudio.PlayAt(AudioCue.SymbolPress, transform.position);
            sequencePuzzle.RegisterSymbol(symbol);
            feedbackRoutine = StartCoroutine(PlayPressFeedback());
        }

        private IEnumerator PlayPressFeedback()
        {
            Vector3 pressedPosition = restingPosition + pressOffset;
            yield return MoveTo(pressedPosition);
            yield return MoveTo(restingPosition);
            feedbackRoutine = null;
        }

        private IEnumerator MoveTo(Vector3 target)
        {
            while ((transform.localPosition - target).sqrMagnitude > 0.000001f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    target,
                    pressSpeed * Time.deltaTime);
                yield return null;
            }

            transform.localPosition = target;
        }
    }
}
