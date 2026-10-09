using System;
using UnityEngine;

namespace PuzzleRoom.Core
{
    /// <summary>
    /// Shared read-only UV state observed by every UVRevealObject in the scene.
    /// </summary>
    public static class UVLightState
    {
        public static event Action<bool> Changed;

        public static bool IsActive { get; private set; }
        public static Light SourceLight { get; private set; }

        public static void SetSource(Light sourceLight)
        {
            SourceLight = sourceLight;
        }

        public static void SetActive(bool isActive)
        {
            if (IsActive == isActive)
            {
                return;
            }

            IsActive = isActive;
            Changed?.Invoke(isActive);
        }
    }
}
