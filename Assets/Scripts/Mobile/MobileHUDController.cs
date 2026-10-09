using PuzzleRoom.Player;
using PuzzleRoom.UI;
using PuzzleRoom.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.Mobile
{
    public sealed class MobileHUDController : MonoBehaviour
    {
        [SerializeField] private bool showOnMobile = true;
        [SerializeField] private bool showInEditorForTesting;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private PlayerInputProvider inputProvider;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private UVLightController uvLightController;
        [SerializeField] private PauseMenuController pauseMenu;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private Button interactButton;
        [SerializeField] private Button uvButton;
        [SerializeField] private Text uvLabel;
        [SerializeField] private CanvasGroup gameplayControls;

        private void Awake()
        {
            bool visible = (showOnMobile && Application.isMobilePlatform) ||
                           (Application.isEditor && showInEditorForTesting);
            if (interactButton != null) interactButton.gameObject.SetActive(false);
            if (uvButton != null) uvButton.gameObject.SetActive(false);
            gameObject.SetActive(visible);
        }

        private void Start()
        {
            if (!gameObject.activeSelf || !Application.isEditor) return;
            inputProvider?.SetEditorMobileSimulation(true);
            playerLook?.SetCursorLocked(false);
        }

        private void LateUpdate()
        {
            // PlayerLook.Start() and Pause resume may lock the cursor after this HUD
            // has initialized. Editor mobile simulation needs a free mouse pointer
            // so UGUI can receive joystick drags and button clicks.
            if (!Application.isEditor || !showInEditorForTesting || !gameObject.activeInHierarchy) return;
            playerLook?.SetCursorLocked(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDisable()
        {
            inputProvider?.SetEditorMobileSimulation(false);
        }

        private void Update()
        {
            if (gameFlowManager != null && gameFlowManager.IsVictory)
            {
                gameObject.SetActive(false);
                return;
            }
            bool gameplayAvailable = playerInteraction != null && playerInteraction.isActiveAndEnabled &&
                                     (pauseMenu == null || !pauseMenu.IsPaused);
            if (gameplayControls != null)
            {
                gameplayControls.interactable = gameplayAvailable;
                gameplayControls.blocksRaycasts = gameplayAvailable;
                gameplayControls.alpha = gameplayAvailable ? 1f : 0.35f;
            }

            if (uvButton != null)
            {
                bool ownsUV = uvLightController != null && uvLightController.HasUVLight;
                if (uvButton.gameObject.activeSelf != ownsUV) uvButton.gameObject.SetActive(ownsUV);
                uvButton.interactable = ownsUV && gameplayAvailable && uvLightController.CanToggle;
            }
            if (uvLabel != null && uvLightController != null)
                uvLabel.text = uvLightController.IsOn ? "UV ON" : "UV OFF";
        }

        public void Interact()
        {
            if (playerInteraction != null && playerInteraction.isActiveAndEnabled)
                inputProvider?.QueueInteract();
        }

        public void ToggleUV()
        {
            if (playerInteraction != null && playerInteraction.isActiveAndEnabled)
                uvLightController?.ToggleUV();
        }

        public void Pause()
        {
            if (pauseMenu != null && !pauseMenu.IsPaused) pauseMenu.RequestPause();
        }
    }
}
