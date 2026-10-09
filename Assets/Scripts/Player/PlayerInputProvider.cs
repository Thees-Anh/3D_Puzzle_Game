using PuzzleRoom.Mobile;
using UnityEngine;

namespace PuzzleRoom.Player
{
    /// <summary>
    /// Small input boundary shared by desktop and mobile controls. Gameplay reads
    /// intentions from here and does not need to know which device produced them.
    /// </summary>
    public sealed class PlayerInputProvider : MonoBehaviour
    {
        [Header("Desktop fallback")]
        [SerializeField] private bool enableDesktopInput = true;
        [SerializeField] private bool enableDesktopAlongsideMobile;

        [Header("Mobile sources")]
        [SerializeField] private VirtualJoystick movementJoystick;
        [SerializeField] private TouchLookArea touchLookArea;
        [SerializeField, Min(0.001f)] private float touchLookSensitivity = 0.12f;

        private bool mobileInteractQueued;
        private bool simulateMobileInEditor;

        private void Awake()
        {
            touchLookSensitivity = PlayerPrefs.GetFloat("Controls.TouchLookSensitivity", touchLookSensitivity);
        }

        public bool MobileLookActive => movementJoystick != null && touchLookArea != null &&
                                        touchLookArea.isActiveAndEnabled;

        private bool ReadDesktop => !simulateMobileInEditor && enableDesktopInput &&
            (!Application.isMobilePlatform || enableDesktopAlongsideMobile);

        public Vector2 ReadMove()
        {
            Vector2 mobile = movementJoystick != null && movementJoystick.isActiveAndEnabled
                ? movementJoystick.Value
                : Vector2.zero;
            if (!ReadDesktop) return Vector2.ClampMagnitude(mobile, 1f);

            Vector2 desktop = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            return Vector2.ClampMagnitude(mobile.sqrMagnitude > desktop.sqrMagnitude ? mobile : desktop, 1f);
        }

        public Vector2 ReadLook(float desktopSensitivity)
        {
            Vector2 mobile = touchLookArea != null && touchLookArea.isActiveAndEnabled
                ? touchLookArea.ConsumeDelta() * touchLookSensitivity
                : Vector2.zero;
            if (!ReadDesktop) return mobile;

            return mobile + new Vector2(
                Input.GetAxis("Mouse X") * desktopSensitivity,
                Input.GetAxis("Mouse Y") * desktopSensitivity);
        }

        public bool InteractPressed()
        {
            bool pressed = mobileInteractQueued || (ReadDesktop && Input.GetKeyDown(KeyCode.E));
            mobileInteractQueued = false;
            return pressed;
        }

        public void QueueInteract() => mobileInteractQueued = true;
        public bool UVPressed(KeyCode desktopKey) => ReadDesktop && Input.GetKeyDown(desktopKey);

        public void ResetMobileInput()
        {
            movementJoystick?.ResetInput();
            touchLookArea?.ResetInput();
            mobileInteractQueued = false;
        }

        public void SetMobileSources(VirtualJoystick joystick, TouchLookArea lookArea)
        {
            movementJoystick = joystick;
            touchLookArea = lookArea;
        }

        public void SetEditorMobileSimulation(bool enabled)
        {
            simulateMobileInEditor = Application.isEditor && enabled;
            ResetMobileInput();
        }
    }
}
