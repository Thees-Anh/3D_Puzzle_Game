using PuzzleRoom.Core;
using PuzzleRoom.Player;
using UnityEngine;

namespace PuzzleRoom.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class ExitTrigger : MonoBehaviour
    {
        [SerializeField] private GameFlowManager gameFlowManager;
        private bool hasTriggered;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered || !PowerState.IsRestored ||
                other.GetComponentInParent<PlayerMovement>() == null)
            {
                return;
            }

            hasTriggered = true;
            gameFlowManager?.TriggerVictory();
        }
    }
}
