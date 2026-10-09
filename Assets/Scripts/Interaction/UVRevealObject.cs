using PuzzleRoom.Core;
using UnityEngine;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Shows one or more renderers only while the player's UV light is active.
    /// </summary>
    public sealed class UVRevealObject : MonoBehaviour
    {
        [SerializeField] private Renderer[] revealRenderers;

        private void Awake()
        {
            if (revealRenderers == null || revealRenderers.Length == 0)
            {
                revealRenderers = GetComponentsInChildren<Renderer>(true);
            }

            RefreshVisibility();
        }

        private void OnEnable()
        {
            UVLightState.Changed += HandleUVChanged;
            RefreshVisibility();
        }

        private void OnDisable()
        {
            UVLightState.Changed -= HandleUVChanged;
            SetVisible(false);
        }

        private void Update()
        {
            if (UVLightState.IsActive)
            {
                RefreshVisibility();
            }
        }

        private void HandleUVChanged(bool isActive)
        {
            if (!isActive)
            {
                SetVisible(false);
                return;
            }

            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            Light source = UVLightState.SourceLight;
            if (!UVLightState.IsActive || source == null || !source.enabled)
            {
                SetVisible(false);
                return;
            }

            Vector3 toClue = transform.position - source.transform.position;
            float distance = toClue.magnitude;
            float angle = distance > 0.001f
                ? Vector3.Angle(source.transform.forward, toClue / distance)
                : 0f;
            bool insideBeam = distance <= source.range && angle <= source.spotAngle * 0.5f;
            SetVisible(insideBeam);
        }

        private void SetVisible(bool isVisible)
        {
            if (revealRenderers == null)
            {
                return;
            }

            foreach (Renderer revealRenderer in revealRenderers)
            {
                if (revealRenderer != null)
                {
                    revealRenderer.enabled = isVisible;
                }
            }
        }
    }
}
