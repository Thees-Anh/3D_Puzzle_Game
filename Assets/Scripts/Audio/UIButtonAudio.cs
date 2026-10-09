using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleRoom.Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonAudio : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private bool playClick = true;
        private Button button;
        private void Awake() => button = GetComponent<Button>();
        public void OnPointerClick(PointerEventData eventData)
        {
            if (playClick && button != null && button.IsInteractable()) GameAudio.Play(AudioCue.UIClick);
        }
    }
}
