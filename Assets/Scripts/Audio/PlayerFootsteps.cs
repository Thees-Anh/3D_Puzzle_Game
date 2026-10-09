using PuzzleRoom.Player;
using UnityEngine;

namespace PuzzleRoom.Audio
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float stepInterval = .48f;
        [SerializeField, Min(.01f)] private float movementThreshold = .15f;
        private CharacterController controller;
        private PlayerMovement movement;
        private float nextStepTime;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || movement == null || !movement.enabled ||
                controller == null || !controller.enabled) return;

            Vector2 movementInput = movement.CurrentMoveInput;
            if (movementInput.sqrMagnitude < movementThreshold * movementThreshold)
            {
                nextStepTime = Time.time + .1f;
                return;
            }
            if (Time.time < nextStepTime) return;
            // First-person footsteps are player-centric, so keep them non-spatial;
            // this avoids attenuation between the CharacterController feet and camera.
            GameAudio.Play(AudioCue.FootstepWood);
            nextStepTime = Time.time + stepInterval;
        }
    }
}
