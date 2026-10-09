using UnityEngine;

namespace PuzzleRoom.Player
{
    /// <summary>
    /// Rotates the player horizontally and the first-person camera vertically.
    /// Escape toggles whether the cursor is locked for convenient editor testing.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform playerBody;
        [SerializeField, Min(0f)] private float mouseSensitivity = 2f;
        [SerializeField, Range(1f, 90f)] private float maxLookAngle = 85f;

        private float verticalLookRotation;
        private bool isCursorLocked;
        private PlayerInputProvider inputProvider;
        public float VerticalLookRotation => verticalLookRotation;

        private void Awake()
        {
            if (playerBody == null)
            {
                playerBody = transform.parent;
            }
            inputProvider = GetComponentInParent<PlayerInputProvider>();
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            if ((!isCursorLocked && (inputProvider == null || !inputProvider.MobileLookActive)) || playerBody == null)
            {
                return;
            }

            Vector2 look = inputProvider != null
                ? inputProvider.ReadLook(mouseSensitivity)
                : new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity;
            float mouseX = look.x;
            float mouseY = look.y;

            verticalLookRotation -= mouseY;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -maxLookAngle, maxLookAngle);

            transform.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        public void SetCursorLocked(bool shouldLock)
        {
            isCursorLocked = shouldLock;
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }

        public void ResetView()
        {
            verticalLookRotation = 0f;
            transform.localRotation = Quaternion.identity;
        }

        public void RestoreView(float verticalAngle)
        {
            verticalLookRotation = Mathf.Clamp(verticalAngle, -maxLookAngle, maxLookAngle);
            transform.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
        }
    }
}
