using PuzzleRoom.Interaction;
using PuzzleRoom.Audio;
using UnityEngine;

namespace PuzzleRoom.Core
{
    /// <summary>
    /// Applies the global power event to room-level consumers.
    /// </summary>
    public sealed class RoomPowerController : MonoBehaviour
    {
        [SerializeField] private Light[] roomLights;
        [SerializeField] private GameObject[] poweredObjects;
        [SerializeField] private Door exitDoor;
        [SerializeField, Min(0f)] private float unpoweredIntensity = 1.65f;
        [SerializeField, Min(0f)] private float poweredIntensity = 2.8f;

        [Header("Lighting Palette")]
        [SerializeField] private Color unpoweredLightColor = new Color(1f, 0.78f, 0.48f);
        [SerializeField] private Color poweredLightColor = new Color(1f, 0.88f, 0.68f);
        [SerializeField] private bool controlAmbientLight = true;
        [SerializeField] private Color unpoweredAmbientColor = new Color(0.38f, 0.30f, 0.20f);
        [SerializeField] private Color poweredAmbientColor = new Color(0.62f, 0.52f, 0.38f);

        [Header("Powered Emission")]
        [SerializeField] private Renderer[] poweredEmissiveRenderers;
        [SerializeField] private Color unpoweredEmissionColor = new Color(0.18f, 0.09f, 0.015f);
        [SerializeField] private Color poweredEmissionColor = new Color(1.5f, 0.72f, 0.12f);

        [Header("Optional Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip powerRestoredSound;
        [SerializeField] private AudioClip exitUnlockSound;

        private void OnEnable()
        {
            PowerState.PowerRestored += HandlePowerRestored;
            ApplyPowerState(PowerState.IsRestored);
        }

        private void OnDisable()
        {
            PowerState.PowerRestored -= HandlePowerRestored;
        }

        private void HandlePowerRestored()
        {
            ApplyPowerState(true);
            GameAudio.PlayAt(AudioCue.ExitUnlock, exitDoor != null ? exitDoor.transform.position : transform.position);

            if (audioSource != null)
            {
                if (powerRestoredSound != null) audioSource.PlayOneShot(powerRestoredSound);
                if (exitUnlockSound != null) audioSource.PlayOneShot(exitUnlockSound);
            }
        }

        private void ApplyPowerState(bool restored)
        {
            if (roomLights != null)
            {
                foreach (Light roomLight in roomLights)
                {
                    if (roomLight != null)
                    {
                        roomLight.enabled = true;
                        roomLight.intensity = restored ? poweredIntensity : unpoweredIntensity;
                        roomLight.color = restored ? poweredLightColor : unpoweredLightColor;
                    }
                }
            }

            if (controlAmbientLight)
            {
                RenderSettings.ambientLight = restored
                    ? poweredAmbientColor
                    : unpoweredAmbientColor;
            }

            if (poweredEmissiveRenderers != null)
            {
                foreach (Renderer emissiveRenderer in poweredEmissiveRenderers)
                {
                    if (emissiveRenderer == null) continue;
                    Material material = emissiveRenderer.material;
                    material.EnableKeyword("_EMISSION");
                    material.SetColor(
                        "_EmissionColor",
                        restored ? poweredEmissionColor : unpoweredEmissionColor);
                }
            }

            if (poweredObjects != null)
            {
                foreach (GameObject poweredObject in poweredObjects)
                {
                    poweredObject?.SetActive(restored);
                }
            }

            if (exitDoor != null)
            {
                exitDoor.SetLocked(!restored);
            }
        }
    }
}
