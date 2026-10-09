using UnityEngine;
using UnityEngine.EventSystems;

namespace PuzzleRoom.Mobile
{
    public sealed class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private int ownerFingerId = int.MinValue;
        private Vector2 accumulatedDelta;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ownerFingerId == int.MinValue) ownerFingerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == ownerFingerId) accumulatedDelta += eventData.delta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == ownerFingerId) ResetInput();
        }

        public Vector2 ConsumeDelta()
        {
            Vector2 result = accumulatedDelta;
            accumulatedDelta = Vector2.zero;
            return result;
        }

        private void OnDisable() => ResetInput();

        public void ResetInput()
        {
            ownerFingerId = int.MinValue;
            accumulatedDelta = Vector2.zero;
        }
    }
}
