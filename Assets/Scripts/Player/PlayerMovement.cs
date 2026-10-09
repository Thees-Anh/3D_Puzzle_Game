using UnityEngine;

namespace PuzzleRoom.Player
{
    /// <summary>
    /// Handles grounded first-person movement and gravity.
    /// Looking is intentionally handled by PlayerLook on the camera.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -20f;
        [SerializeField, Min(0f)] private float groundedForce = 2f;

        private CharacterController characterController;
        private PlayerInputProvider inputProvider;
        private float verticalVelocity;

        public Vector2 CurrentMoveInput { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputProvider = GetComponent<PlayerInputProvider>();
        }

        private void Update()
        {
            Move();
        }

        private void Move()
        {
            CurrentMoveInput = inputProvider != null
                ? inputProvider.ReadMove()
                : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            Vector3 inputDirection = new Vector3(CurrentMoveInput.x, 0f, CurrentMoveInput.y);
            inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);
            Vector3 horizontalMovement = transform.TransformDirection(inputDirection) * moveSpeed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -groundedForce;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = horizontalMovement + Vector3.up * verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);
        }

        private void OnValidate()
        {
            if (gravity > 0f)
            {
                gravity = -gravity;
            }
        }
    }
}
