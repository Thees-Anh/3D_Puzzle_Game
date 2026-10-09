using System;
using UnityEngine;

namespace PuzzleRoom.Puzzles
{
    /// <summary>
    /// Minimal shared lifecycle for every puzzle.
    /// Concrete puzzles own their rules and call Complete() when solved.
    /// Other systems can subscribe to Completed without referencing a
    /// concrete puzzle type.
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Unique identifier used to distinguish this puzzle from others.")]
        private string puzzleId;

        public string PuzzleId => puzzleId;

        public bool IsCompleted { get; private set; }

        public event Action<PuzzleBase> Completed;

        protected void Complete()
        {
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
            OnCompleted();
            Completed?.Invoke(this);
        }

        protected void RestoreCompletion(bool completed)
        {
            IsCompleted = completed;
        }

        /// <summary>
        /// Optional extension point for local visual or audio feedback.
        /// </summary>
        protected virtual void OnCompleted()
        {
        }
    }
}
