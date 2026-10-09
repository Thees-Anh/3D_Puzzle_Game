using PuzzleRoom.Player;
using PuzzleRoom.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.Interaction
{
    /// <summary>
    /// Reusable chair interaction. It temporarily positions the player at a seat,
    /// prevents walking, and lets the same E key leave the chair from any view angle.
    /// </summary>
    public sealed class SittableChair : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform exitAnchor;

        private GameObject seatedPlayer;
        private PlayerMovement playerMovement;
        private PlayerInteraction playerInteraction;
        private CharacterController characterController;
        private PlayerLook playerLook;
        private PlayerInputProvider inputProvider;
        private bool canExitWithInteractKey;
        private GameObject leaveCanvas;

        public string InteractionPrompt => "Sit";
        public bool CanInteract => seatedPlayer == null && seatAnchor != null;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract || interactor == null) return;

            seatedPlayer = interactor;
            playerMovement = interactor.GetComponent<PlayerMovement>();
            characterController = interactor.GetComponent<CharacterController>();
            playerInteraction = interactor.GetComponentInChildren<PlayerInteraction>(true);
            playerLook = interactor.GetComponentInChildren<PlayerLook>(true);
            inputProvider = interactor.GetComponent<PlayerInputProvider>();

            if (characterController != null) characterController.enabled = false;
            interactor.transform.SetPositionAndRotation(seatAnchor.position, seatAnchor.rotation);
            if (playerLook != null) playerLook.ResetView();
            if (playerMovement != null) playerMovement.enabled = false;
            if (playerInteraction != null) playerInteraction.enabled = false;
            GameAudio.PlayAt(AudioCue.ChairSit, transform.position);
            ShowLeaveButton(true);

            // The E press that entered the chair must be released before it may exit.
            canExitWithInteractKey = false;
        }

        private void Update()
        {
            if (seatedPlayer == null) return;

            if (!canExitWithInteractKey)
            {
                if (!Input.GetKey(KeyCode.E)) canExitWithInteractKey = true;
                return;
            }

            bool pressed = inputProvider != null
                ? inputProvider.InteractPressed()
                : Input.GetKeyDown(KeyCode.E);
            if (pressed) LeaveChair();
        }

        private void LeaveChair()
        {
            if (seatedPlayer == null) return;

            Transform destination = exitAnchor != null ? exitAnchor : transform;
            seatedPlayer.transform.SetPositionAndRotation(destination.position, destination.rotation);
            if (playerLook != null) playerLook.ResetView();
            if (characterController != null) characterController.enabled = true;
            if (playerMovement != null) playerMovement.enabled = true;
            if (playerInteraction != null) playerInteraction.enabled = true;
            GameAudio.PlayAt(AudioCue.ChairStand, transform.position);
            ShowLeaveButton(false);

            seatedPlayer = null;
            playerMovement = null;
            playerInteraction = null;
            characterController = null;
            playerLook = null;
            inputProvider = null;
            canExitWithInteractKey = false;
        }

        private void OnDisable()
        {
            if (seatedPlayer != null) LeaveChair();
            ShowLeaveButton(false);
        }

        private void ShowLeaveButton(bool visible)
        {
            if (visible && leaveCanvas == null) BuildLeaveButton();
            if (leaveCanvas != null) leaveCanvas.SetActive(visible);
        }

        private void BuildLeaveButton()
        {
            leaveCanvas = new GameObject("ChairExitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            leaveCanvas.transform.SetParent(transform, false);
            Canvas canvas = leaveCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 175;

            CanvasScaler scaler = leaveCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            GameObject buttonObject = new GameObject("LeaveChairButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(leaveCanvas.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(.5f, 0f);
            buttonRect.pivot = new Vector2(.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 125f);
            buttonRect.sizeDelta = new Vector2(310f, 78f);

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(.075f, .065f, .055f, .96f);
            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .86f, .62f);
            colors.pressedColor = new Color(.82f, .58f, .24f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(LeaveChair);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "LEAVE CHAIR";
            label.fontSize = 27;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.95f, .88f, .72f);
            label.raycastTarget = false;

            Shadow shadow = labelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .7f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }
    }
}
