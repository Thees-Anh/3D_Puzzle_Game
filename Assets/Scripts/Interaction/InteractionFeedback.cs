using System;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Lightweight message channel for short interaction feedback such as "Locked".
    /// Gameplay objects can publish feedback without referencing the player UI.
    /// </summary>
    public static class InteractionFeedback
    {
        public static event Action<string, float> MessageRequested;

        public static void ShowMessage(string message, float duration = 1.2f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            MessageRequested?.Invoke(message, duration);
        }
    }
}
