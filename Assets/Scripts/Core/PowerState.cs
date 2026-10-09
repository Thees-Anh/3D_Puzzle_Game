using System;
using UnityEngine;

namespace PuzzleRoom.Core
{
    /// <summary>
    /// Shared power state and event channel. Puzzle code does not need to know
    /// which lights, doors, or future devices react to restored power.
    /// </summary>
    public static class PowerState
    {
        public static bool IsRestored { get; private set; }

        public static event Action PowerRestored;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            ResetForNewGame();
        }

        public static void ResetForNewGame()
        {
            IsRestored = false;
            PowerRestored = null;
        }

        public static void Restore()
        {
            if (IsRestored)
            {
                return;
            }

            IsRestored = true;
            PowerRestored?.Invoke();
        }
    }
}
