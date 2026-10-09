using PuzzleRoom.Core;
using PuzzleRoom.Audio;
using UnityEngine;

namespace PuzzleRoom.Player
{
    /// <summary>
    /// Toggles the first-person UV light after the player owns the UVLight item.
    /// </summary>
    public sealed class UVLightController : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Light uvLight;
        [SerializeField] private GameObject equippedVisual;
        [SerializeField] private KeyCode toggleKey = KeyCode.F;

        public bool IsOn => uvLight != null && uvLight.enabled;
        public bool HasUVLight => inventory != null && inventory.HasItem(ItemId.UVLight);
        public bool CanToggle => inventory != null && inventory.HasSelectedItem(ItemId.UVLight);

        private PlayerInputProvider inputProvider;

        private void Awake()
        {
            UVLightState.SetSource(uvLight);
            inputProvider = GetComponentInParent<PlayerInputProvider>();
            SetUVEnabled(false);
        }

        private void Update()
        {
            if (IsOn && inventory != null && !inventory.HasSelectedItem(ItemId.UVLight))
            {
                SetUVEnabled(false);
            }

            bool pressed = inputProvider != null
                ? inputProvider.UVPressed(toggleKey)
                : Input.GetKeyDown(toggleKey);
            if (pressed) ToggleUV();
        }

        public void ToggleUV()
        {
            if (!isActiveAndEnabled || !CanToggle) return;
            bool turnOn = !IsOn;
            SetUVEnabled(turnOn);
            GameAudio.Play(turnOn ? AudioCue.UVOn : AudioCue.UVOff);
        }

        private void OnDisable()
        {
            SetUVEnabled(false);
        }

        private void SetUVEnabled(bool isEnabled)
        {
            bool canEnable = isEnabled && inventory != null && inventory.HasItem(ItemId.UVLight);

            if (uvLight != null)
            {
                uvLight.enabled = canEnable;
            }

            if (equippedVisual != null)
            {
                equippedVisual.SetActive(canEnable);
            }

            UVLightState.SetActive(canEnable);
        }
    }
}
