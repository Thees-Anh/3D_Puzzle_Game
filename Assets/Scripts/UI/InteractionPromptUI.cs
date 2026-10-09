using System.Collections;
using PuzzleRoom.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleRoom.UI
{
    /// <summary>
    /// Small presentation component for the shared interaction prompt.
    /// </summary>
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField, Min(10)] private int fontSize = 28;
        [SerializeField] private Vector2 objectOffset = new Vector2(0f, 70f);
        [SerializeField] private Vector2 promptSize = new Vector2(350f, 82f);
        [SerializeField, Min(.1f)] private float holdDuration = .25f;

        private bool isVisible;
        private bool isShowingTemporaryMessage;
        private bool snapPosition;
        private Coroutine messageRoutine;
        private Camera worldCamera;
        private Vector3 worldPosition;
        private Vector2 smoothPosition;
        private Vector2 smoothVelocity;
        private RectTransform canvasRect;
        private RectTransform panelRect;
        private CanvasGroup canvasGroup;
        private Text label;
        private Image progress;
        private int ownerFingerId = int.MinValue;
        private float heldTime;
        private bool holdCompleted;

        private void Awake()
        {
            BuildPresentation();
            Hide();
        }

        private void OnEnable()
        {
            InteractionFeedback.MessageRequested += ShowTemporaryMessage;
        }

        private void OnDisable()
        {
            InteractionFeedback.MessageRequested -= ShowTemporaryMessage;

            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
                messageRoutine = null;
            }

            isShowingTemporaryMessage = false;
            isVisible = false;
            ResetHold();
        }

        public void Show(string actionText, Vector3 targetPosition, Camera sourceCamera)
        {
            if (isShowingTemporaryMessage)
            {
                return;
            }

            bool wasHidden = !isVisible;
            worldPosition = targetPosition;
            worldCamera = sourceCamera;
            label.text = actionText;
            isVisible = true;
            snapPosition |= wasHidden;
            SetPresentationVisible(true, true);
        }

        public void SetHoldDuration(float duration) => holdDuration = Mathf.Max(.1f, duration);

        public bool ConsumeHoldCompleted()
        {
            bool result = holdCompleted;
            holdCompleted = false;
            return result;
        }

        public void Hide()
        {
            if (!isShowingTemporaryMessage)
            {
                isVisible = false;
                SetPresentationVisible(false, false);
                ResetHold();
            }
        }

        private void ShowTemporaryMessage(string message, float duration)
        {
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(DisplayMessage(message, duration));
        }

        private IEnumerator DisplayMessage(string message, float duration)
        {
            isShowingTemporaryMessage = true;
            isVisible = true;
            label.text = message;
            panelRect.anchoredPosition = new Vector2(0f, 90f);
            SetPresentationVisible(true, false);

            yield return new WaitForSecondsRealtime(duration);

            isShowingTemporaryMessage = false;
            isVisible = false;
            SetPresentationVisible(false, false);
            ResetHold();
            messageRoutine = null;
        }

        private void Update()
        {
            HandleRawPointerFallback();
            if (ownerFingerId != int.MinValue && isVisible && !isShowingTemporaryMessage) AdvanceHold();
        }

        // Some mobile HUD canvases consume EventSystem events before this world prompt.
        // Reading the initial press as a fallback keeps mouse testing and touch reliable.
        private void HandleRawPointerFallback()
        {
            if (!isVisible || isShowingTemporaryMessage || panelRect == null) return;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && ownerFingerId == int.MinValue &&
                        RectTransformUtility.RectangleContainsScreenPoint(panelRect, touch.position))
                    {
                        BeginPointerHold(touch.fingerId);
                    }

                    if (touch.fingerId == ownerFingerId &&
                        (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                    {
                        EndPointerHold(touch.fingerId);
                    }
                }
                return;
            }

            if (Input.GetMouseButtonDown(0) && ownerFingerId == int.MinValue &&
                RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
            {
                BeginPointerHold(-1);
            }
            if (ownerFingerId == -1 && Input.GetMouseButtonUp(0)) EndPointerHold(-1);
        }

        private void LateUpdate()
        {
            if (!isVisible || isShowingTemporaryMessage || worldCamera == null || canvasRect == null) return;
            Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f)
            {
                SetPresentationVisible(false, false);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            Vector2 target = local + objectOffset;
            if (snapPosition)
            {
                smoothPosition = target;
                smoothVelocity = Vector2.zero;
                snapPosition = false;
            }
            else
            {
                smoothPosition = Vector2.SmoothDamp(smoothPosition, target, ref smoothVelocity, .055f, Mathf.Infinity, Time.unscaledDeltaTime);
            }
            panelRect.anchoredPosition = smoothPosition;
            SetPresentationVisible(true, true);
        }

        internal void BeginPointerHold(int pointerId)
        {
            if (!isVisible || isShowingTemporaryMessage || ownerFingerId != int.MinValue) return;
            ownerFingerId = pointerId;
            heldTime = 0f;
        }

        internal void EndPointerHold(int pointerId)
        {
            if (ownerFingerId == pointerId) ResetHold();
        }

        private void AdvanceHold()
        {
            heldTime += Time.unscaledDeltaTime;
            progress.fillAmount = Mathf.Clamp01(heldTime / holdDuration);
            if (heldTime < holdDuration) return;
            holdCompleted = true;
            ownerFingerId = int.MinValue;
            heldTime = 0f;
            progress.fillAmount = 0f;
        }

        private void ResetHold()
        {
            ownerFingerId = int.MinValue;
            heldTime = 0f;
            if (progress != null) progress.fillAmount = 0f;
        }

        private void SetPresentationVisible(bool visible, bool receivesInput)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible && receivesInput;
            canvasGroup.interactable = visible && receivesInput;
        }

        private void BuildPresentation()
        {
            // Older scene data may still contain the original narrow prompt size.
            // Keep enough room for inventory requirement messages on small screens.
            promptSize.x = Mathf.Max(promptSize.x, 390f);
            promptSize.y = Mathf.Max(promptSize.y, 78f);

            GameObject canvasObject = new GameObject("TouchInteractionPromptCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 140;
            canvasRect = canvasObject.GetComponent<RectTransform>();
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            GameObject panel = new GameObject("Prompt", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(HoldPromptPointerRelay));
            panel.transform.SetParent(canvasObject.transform, false);
            panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta = promptSize;
            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = CreateRoundedSprite(32, 12f, true);
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(.055f, .06f, .075f, .96f);
            panelImage.raycastTarget = true;
            canvasGroup = panel.GetComponent<CanvasGroup>();
            panel.GetComponent<HoldPromptPointerRelay>().Initialize(this);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(92f, 8f);
            textRect.offsetMax = new Vector2(-22f, -8f);
            label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.raycastTarget = false;

            GameObject progressObject = new GameObject("HoldProgress", typeof(RectTransform), typeof(Image));
            progressObject.transform.SetParent(panel.transform, false);
            RectTransform progressRect = progressObject.GetComponent<RectTransform>();
            progressRect.anchorMin = progressRect.anchorMax = new Vector2(0f, .5f);
            progressRect.pivot = new Vector2(.5f, .5f);
            progressRect.anchoredPosition = new Vector2(47f, 0f);
            progressRect.sizeDelta = new Vector2(58f, 58f);
            progress = progressObject.GetComponent<Image>();
            progress.sprite = CreateRoundedSprite(64, 31f, false);
            progress.color = new Color(1f, .67f, .18f);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Radial360;
            progress.fillOrigin = 2;
            progress.fillAmount = 0f;
            progress.raycastTarget = false;

            GameObject centerObject = new GameObject("HoldCenter", typeof(RectTransform), typeof(Image));
            centerObject.transform.SetParent(progressObject.transform, false);
            RectTransform centerRect = centerObject.GetComponent<RectTransform>();
            centerRect.anchorMin = Vector2.zero;
            centerRect.anchorMax = Vector2.one;
            centerRect.offsetMin = new Vector2(7f, 7f);
            centerRect.offsetMax = new Vector2(-7f, -7f);
            Image centerImage = centerObject.GetComponent<Image>();
            centerImage.sprite = CreateRoundedSprite(64, 31f, false);
            centerImage.color = new Color(.16f, .17f, .2f);
            centerImage.raycastTarget = false;
        }

        private static Sprite CreateRoundedSprite(int size, float radius, bool sliced)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRoundedPrompt",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[size * size];
            float half = size * .5f;
            Vector2 corner = new Vector2(half - radius, half - radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(Mathf.Abs(x + .5f - half), Mathf.Abs(y + .5f - half));
                    Vector2 outside = new Vector2(Mathf.Max(point.x - corner.x, 0f), Mathf.Max(point.y - corner.y, 0f));
                    float alpha = Mathf.Clamp01(radius + .5f - outside.magnitude);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);

            Vector4 border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = sliced ? "RuntimePromptPill" : "RuntimePromptCircle";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }

    internal sealed class HoldPromptPointerRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private InteractionPromptUI owner;
        internal void Initialize(InteractionPromptUI value) => owner = value;
        public void OnPointerDown(PointerEventData eventData) => owner?.BeginPointerHold(eventData.pointerId);
        public void OnPointerUp(PointerEventData eventData) => owner?.EndPointerHold(eventData.pointerId);
        public void OnPointerExit(PointerEventData eventData) { }
    }
}
