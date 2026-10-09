using UnityEngine;
using UnityEngine.EventSystems;

namespace PuzzleRoom.Mobile
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField, Range(0f, 0.9f)] private float deadZone = 0.12f;
        [SerializeField, Range(0.2f, 1f)] private float handleRange = 0.72f;

        private int ownerFingerId = int.MinValue;
        public Vector2 Value { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ownerFingerId != int.MinValue) return;
            ownerFingerId = eventData.pointerId;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == ownerFingerId) UpdateValue(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == ownerFingerId) ResetInput();
        }

        private void OnDisable() => ResetInput();

        public void ResetInput()
        {
            ownerFingerId = int.MinValue;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        public void Configure(RectTransform newBackground, RectTransform newHandle)
        {
            background = newBackground;
            handle = newHandle;
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (background == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

            Vector2 radius = background.rect.size * 0.5f;
            Vector2 normalized = new Vector2(
                radius.x > 0f ? localPoint.x / radius.x : 0f,
                radius.y > 0f ? localPoint.y / radius.y : 0f);
            normalized = Vector2.ClampMagnitude(normalized, 1f);
            float magnitude = normalized.magnitude;
            Value = magnitude <= deadZone
                ? Vector2.zero
                : normalized.normalized * Mathf.InverseLerp(deadZone, 1f, magnitude);

            if (handle != null)
                handle.anchoredPosition = Vector2.Scale(normalized, radius) * handleRange;
        }
    }
}
