using System;
using PuzzleRoom.Core;
using PuzzleRoom.Interaction;
using PuzzleRoom.Audio;
using UnityEngine;

namespace PuzzleRoom.Puzzles.PowerGrid
{
    public sealed class FinalPowerPuzzleManager : PuzzleBase
    {
        [SerializeField] private CircuitGridManager circuitGrid;
        [SerializeField] private PowerChannel[] activationSequence =
        {
            PowerChannel.Control,
            PowerChannel.Light,
            PowerChannel.Exit
        };

        [Header("Optional Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fuseInsertSound;
        [SerializeField] private AudioClip incorrectSequenceSound;
        [SerializeField] private AudioClip powerRestoredSound;

        private int activationProgress;

        public bool FuseInserted { get; private set; }
        public bool CircuitRoutingSolved { get; private set; }
        public int ActivationProgress => activationProgress;

        public event Action StateChanged;

        public void RestoreState(bool fuseInserted, bool routingSolved, int progress, bool completed)
        {
            FuseInserted = fuseInserted;
            CircuitRoutingSolved = routingSolved;
            activationProgress = Mathf.Clamp(progress, 0, activationSequence != null ? activationSequence.Length : 0);
            RestoreCompletion(completed);
            if (circuitGrid != null) circuitGrid.IsEnabled = fuseInserted;
            if (completed && !PowerState.IsRestored) PowerState.Restore();
            StateChanged?.Invoke();
        }

        private void OnEnable()
        {
            if (circuitGrid != null)
            {
                circuitGrid.RoutingSolved += HandleRoutingSolved;
            }
        }

        private void OnDisable()
        {
            if (circuitGrid != null)
            {
                circuitGrid.RoutingSolved -= HandleRoutingSolved;
            }
        }

        public void InsertFuse()
        {
            if (FuseInserted)
            {
                return;
            }

            FuseInserted = true;
            GameAudio.PlayAt(AudioCue.FuseInsert, transform.position);
            if (circuitGrid != null)
            {
                circuitGrid.IsEnabled = true;
                circuitGrid.EvaluateNetwork();
            }
            Play(fuseInsertSound);
            StateChanged?.Invoke();
        }

        public void SubmitActivation(PowerChannel channel)
        {
            if (!CircuitRoutingSolved || IsCompleted || activationSequence == null || activationSequence.Length == 0)
            {
                return;
            }

            if (channel != activationSequence[activationProgress])
            {
                GameAudio.Play(AudioCue.WrongActivation);
                activationProgress = 0;
                InteractionFeedback.ShowMessage("INVALID STARTUP SEQUENCE", 1.5f);
                Play(incorrectSequenceSound);
                StateChanged?.Invoke();
                return;
            }

            activationProgress++;
            GameAudio.Play(AudioCue.OutputActivate);
            StateChanged?.Invoke();

            if (activationProgress == activationSequence.Length)
            {
                Complete();
                PowerState.Restore();
                GameAudio.Play(AudioCue.PowerRestored);
                InteractionFeedback.ShowMessage("POWER RESTORED", 2.5f);
                Play(powerRestoredSound);
                StateChanged?.Invoke();
            }
        }

        private void HandleRoutingSolved()
        {
            if (CircuitRoutingSolved)
            {
                return;
            }

            CircuitRoutingSolved = true;
            GameAudio.Play(AudioCue.GridComplete);
            InteractionFeedback.ShowMessage("POWER ROUTING COMPLETE", 2f);
            StateChanged?.Invoke();
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
