using System;
using PuzzleRoom.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.UI
{
    public sealed class ConfirmationDialog : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text confirmLabel;

        private Action confirmAction;
        private Action cancelAction;

        public void Show(string title, string message, Action onConfirm, Action onCancel, string confirmText)
        {
            titleText.text = title;
            messageText.text = message;
            confirmLabel.text = confirmText;
            confirmAction = onConfirm;
            cancelAction = onCancel;
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            GameAudio.Play(AudioCue.Confirm);
        }

        public void Confirm()
        {
            Action action = confirmAction;
            HideImmediate();
            action?.Invoke();
        }

        public void Cancel()
        {
            Action action = cancelAction;
            HideImmediate();
            GameAudio.Play(AudioCue.UIClose);
            action?.Invoke();
        }

        public void HideImmediate()
        {
            confirmAction = null;
            cancelAction = null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }
    }
}
