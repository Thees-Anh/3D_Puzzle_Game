using PuzzleRoom.Interaction;
using PuzzleRoom.Puzzles.Hanoi;
using PuzzleRoom.Puzzles.SymbolUnlock;
using PuzzleRoom.UI;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleRoom.Player
{
    /// <summary>
    /// Mobile-first proximity interaction. Generic interactables expose a world-anchored
    /// hold prompt; Hanoi books and pegs are tapped directly using the touch position.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.2f;
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField] private InteractionPromptUI promptUI;

        private IInteractable currentInteractable;
        [SerializeField, Min(.1f)] private float holdDuration = .25f;
        private readonly Collider[] nearbyColliders = new Collider[48];
        private readonly HashSet<IInteractable> candidates = new();

        public bool HasInteractable => currentInteractable != null;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponent<Camera>();
            }
            holdDuration = .25f;
            if (promptUI != null) promptUI.SetHoldDuration(holdDuration);
        }

        private void Update()
        {
            FindProximityInteractable();
            HandleDirectHanoiTap();
            if (promptUI != null && promptUI.ConsumeHoldCompleted()) TryInteract();
        }

        public bool TryInteract()
        {
            if (!isActiveAndEnabled || currentInteractable == null || !currentInteractable.CanInteract)
                return false;
            currentInteractable.Interact(transform.root.gameObject);
            return true;
        }

        private void FindProximityInteractable()
        {
            currentInteractable = null;
            if (playerCamera == null) return;
            int count = Physics.OverlapSphereNonAlloc(
                transform.root.position,
                interactionDistance,
                nearbyColliders,
                interactionLayers,
                QueryTriggerInteraction.Collide);
            candidates.Clear();
            float bestDistance = float.MaxValue;
            Vector3 bestPosition = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Collider source = nearbyColliders[i];
                IInteractable interactable = source != null ? source.GetComponentInParent<IInteractable>() : null;
                if (interactable == null || !interactable.CanInteract || !candidates.Add(interactable)) continue;
                if (interactable is HanoiBook || interactable is HanoiPeg || interactable is SymbolButton) continue;
                Component component = interactable as Component;
                if (component == null) continue;
                Vector3 worldPosition = GetStablePromptPosition(component);
                Vector3 screen = playerCamera.WorldToScreenPoint(worldPosition);
                if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height) continue;
                float distance = Vector3.Distance(transform.root.position, worldPosition);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                currentInteractable = interactable;
                bestPosition = worldPosition;
            }
            if (promptUI == null) return;
            if (currentInteractable == null) promptUI.Hide();
            else promptUI.Show(currentInteractable.InteractionPrompt, bestPosition, playerCamera);
        }

        private static Vector3 GetStablePromptPosition(Component component)
        {
            Renderer[] renderers = component.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return component.transform.position + Vector3.up * .35f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center + Vector3.up * Mathf.Min(.35f, bounds.extents.y * .25f);
        }

        private void HandleDirectHanoiTap()
        {
            bool pressed = false;
            Vector2 position = default;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                pressed = touch.phase == TouchPhase.Began;
                position = touch.position;
            }
            else if (Application.isEditor || !Application.isMobilePlatform)
            {
                pressed = Input.GetMouseButtonDown(0);
                position = Input.mousePosition;
            }
            if (!pressed) return;
            if (!Physics.Raycast(playerCamera.ScreenPointToRay(position), out RaycastHit hit, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide)) return;
            IInteractable tapped = hit.collider.GetComponentInParent<IInteractable>();
            if ((tapped is HanoiBook || tapped is HanoiPeg || tapped is SymbolButton) && tapped.CanInteract)
            {
                tapped.Interact(transform.root.gameObject);
            }
        }

        private void OnDisable()
        {
            currentInteractable = null;

            if (promptUI != null)
            {
                promptUI.Hide();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Camera sourceCamera = playerCamera != null ? playerCamera : GetComponent<Camera>();

            if (sourceCamera == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(sourceCamera.transform.position, sourceCamera.transform.forward * interactionDistance);
        }
    }
}
