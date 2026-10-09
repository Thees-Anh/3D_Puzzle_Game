using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleRoom.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(.8f, 1f)] private float pressedScale = .96f;
        private Vector3 normalScale;
        private Button button;

        private void Awake()
        {
            normalScale = transform.localScale;
            button = GetComponent<Button>();
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && button.IsInteractable()) transform.localScale = normalScale * pressedScale;
        }
        public void OnPointerUp(PointerEventData eventData) => transform.localScale = normalScale;
        public void OnPointerExit(PointerEventData eventData) => transform.localScale = normalScale;
        private void OnDisable() => transform.localScale = normalScale == Vector3.zero ? Vector3.one : normalScale;
    }
}
