using UnityEngine;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Temporary interactable used to verify the shared interaction pipeline.
    /// </summary>
    public sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Interact";
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color interactedColor = Color.green;

        private Color initialColor;
        private bool hasInteracted;

        public string InteractionPrompt => interactionPrompt;

        public bool CanInteract => true;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer != null)
            {
                initialColor = targetRenderer.material.color;
            }
        }

        public void Interact(GameObject interactor)
        {
            hasInteracted = !hasInteracted;

            if (targetRenderer != null)
            {
                targetRenderer.material.color = hasInteracted ? interactedColor : initialColor;
            }

            Debug.Log($"{interactor.name} interacted with {name}.", this);
        }
    }
}
