#if UNITY_EDITOR
using System;
using System.Linq;
using PuzzleRoom.Audio;
using PuzzleRoom.Core;
using PuzzleRoom.Mobile;
using PuzzleRoom.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    public static class MainMenuSetupBuilder
    {
        private const string MenuPath = "Assets/Scenes/MainMenu.unity";
        private const string GameplayPath = "Assets/Scenes/PuzzleRoom.unity";
        private static readonly Color Gold = new(0.82f, 0.58f, 0.24f, 1f);
        private static readonly Color Cream = new(0.91f, 0.86f, 0.73f, 1f);
        private static readonly Color Panel = new(0.055f, 0.045f, 0.04f, 0.92f);
        private static readonly Color ButtonNormal = new(0.16f, 0.12f, 0.085f, 0.96f);
        private static Font font;

        [MenuItem("Tools/Puzzle Room/Main Menu/Build Complete Main Menu")]
        public static void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Scene gameplay = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            AudioManager sourceAudio = UnityEngine.Object.FindFirstObjectByType<AudioManager>();

            Scene menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(menu);
            BuildBackground();
            BuildInterface(out MainMenuController controller);
            if (sourceAudio != null) BuildPersistentAudio(sourceAudio);

            EditorSceneManager.CloseScene(gameplay, true);
            EditorSceneManager.SaveScene(menu, MenuPath);
            EnsureBuildSettings();
            Selection.activeGameObject = controller.gameObject;
            Debug.Log("MAIN_MENU_V2 built successfully. Existing gameplay scene was not modified.");
        }

        private static void BuildBackground()
        {
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0f, 2.8f, -7.8f), Quaternion.Euler(9f, 0f, 0f));
            camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.01f, 0.009f);

            Material floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/MAT_DarkWoodFloor.mat");
            Material wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/MAT_MutedGreenWall.mat");
            Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/MAT_DarkOak.mat");
            Material metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/MAT_BlackenedSteel.mat");
            Material brass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/MAT_AgedBrass.mat");
            Material pages = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/MAT_OldBookPages.mat");

            GameObject room = new("StaticRoomBackground");
            Cube("Floor", room.transform, new Vector3(0f, 0f, 3f), new Vector3(12f, .12f, 13f), floor);
            Cube("BackWall", room.transform, new Vector3(0f, 3.1f, 4.8f), new Vector3(12f, 6.2f, .16f), wall);
            Cube("RightWall", room.transform, new Vector3(5.8f, 3.1f, 1.5f), new Vector3(.16f, 6.2f, 6.8f), wall);
            Cube("TableTop", room.transform, new Vector3(2.3f, 1.2f, 2.5f), new Vector3(3.7f, .18f, 1.5f), wood);
            for (int i = 0; i < 4; i++)
                Cube($"TableLeg_{i}", room.transform, new Vector3(1f + (i % 2) * 2.6f, .58f, 2f + (i / 2) * 1f), new Vector3(.16f, 1.2f, .16f), wood);
            Cube("Safe", room.transform, new Vector3(3.4f, 2f, 3.5f), new Vector3(1.25f, 1.3f, .75f), metal);
            Cube("SafeDial", room.transform, new Vector3(3.4f, 2f, 3.08f), new Vector3(.34f, .34f, .08f), brass);
            Cube("PaintingFrame", room.transform, new Vector3(1.7f, 3.7f, 4.65f), new Vector3(2.2f, 1.45f, .12f), wood);
            Cube("Painting", room.transform, new Vector3(1.7f, 3.7f, 4.56f), new Vector3(1.85f, 1.12f, .05f), pages);
            for (int i = 0; i < 4; i++)
                Cube($"Book_{i}", room.transform, new Vector3(.9f + i * .25f, 1.43f, 2.4f), new Vector3(.18f, .48f + i * .04f, .5f), i % 2 == 0 ? wood : brass);

            Light lamp = new GameObject("Warm Lamp", typeof(Light)).GetComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, .62f, .28f);
            lamp.intensity = 2.2f;
            lamp.range = 7f;
            lamp.shadows = LightShadows.Hard;
            lamp.transform.position = new Vector3(2.2f, 3.6f, 1.6f);
            Cube("LampShade", room.transform, new Vector3(2.2f, 3.35f, 1.6f), new Vector3(.6f, .4f, .6f), brass);
        }

        private static void BuildInterface(out MainMenuController controller)
        {
            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            GameObject canvasGo = new("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = true;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            RectTransform safe = Rect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Image vignette = Image("Vignette", safe, new Color(0f, 0f, 0f, .42f));
            Stretch(vignette.rectTransform);
            vignette.raycastTarget = false;

            Text title = TextUI("Title", safe, "3D PUZZLE ROOM", 72, TextAnchor.MiddleCenter, Cream);
            SetRect(title.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -145f), new Vector2(1100f, 110f));
            Text subtitle = TextUI("Subtitle", safe, "ESCAPE ROOM", 25, TextAnchor.MiddleCenter, Gold);
            SetRect(subtitle.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -212f), new Vector2(700f, 55f));

            RectTransform main = Rect("MainPanel", safe, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -105f), new Vector2(520f, 570f));
            Image mainBg = main.gameObject.AddComponent<Image>(); mainBg.color = Panel;
            CanvasGroup mainGroup = main.gameObject.AddComponent<CanvasGroup>();
            VerticalLayoutGroup layout = main.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 28, 28); layout.spacing = 18f; layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = true;

            Button newGame = MenuButton("NewGameButton", main, "NEW GAME");
            Button continueButton = MenuButton("ContinueButton", main, "CONTINUE");
            Button settings = MenuButton("SettingsButton", main, "SETTINGS");
            Button quit = MenuButton("QuitButton", main, "QUIT");
            continueButton.interactable = false;

            RectTransform settingsPanel = Rect("SettingsPanel", safe, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1120f, 820f));
            settingsPanel.gameObject.AddComponent<Image>().color = Panel;
            CanvasGroup settingsGroup = settingsPanel.gameObject.AddComponent<CanvasGroup>();
            MainMenuSettingsUI settingsUI = settingsPanel.gameObject.AddComponent<MainMenuSettingsUI>();
            TextUI("Heading", settingsPanel, "SETTINGS", 48, TextAnchor.MiddleCenter, Cream).rectTransform.anchoredPosition = new Vector2(0f, 340f);

            Slider master = SettingSlider(settingsPanel, "Master", "MASTER VOLUME", 205f, 0f, 1f);
            Slider music = SettingSlider(settingsPanel, "Music", "MUSIC VOLUME", 90f, 0f, 1f);
            Slider sfx = SettingSlider(settingsPanel, "SFX", "SFX VOLUME", -25f, 0f, 1f);
            Slider sensitivity = SettingSlider(settingsPanel, "Sensitivity", "LOOK SENSITIVITY", -140f, .04f, .3f);
            Text sensitivityValue = ValueText("SensitivityValue", settingsPanel, -140f);
            Slider quality = SettingSlider(settingsPanel, "Quality", "GRAPHICS QUALITY", -255f, 0f, 2f);
            quality.wholeNumbers = true;
            Text qualityValue = ValueText("QualityValue", settingsPanel, -255f);

            Button apply = PositionedButton("ApplyButton", settingsPanel, "APPLY", new Vector2(-150f, -350f), new Vector2(250f, 72f));
            Button back = PositionedButton("BackButton", settingsPanel, "BACK", new Vector2(150f, -350f), new Vector2(250f, 72f));

            RectTransform dialogRoot = Rect("ConfirmationDialog", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup dialogGroup = dialogRoot.gameObject.AddComponent<CanvasGroup>();
            Image overlay = dialogRoot.gameObject.AddComponent<Image>(); overlay.color = new Color(0f, 0f, 0f, .78f);
            RectTransform dialogPanel = Rect("DialogPanel", dialogRoot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(700f, 390f));
            dialogPanel.gameObject.AddComponent<Image>().color = new Color(.11f, .085f, .06f, 1f);
            Text dialogTitle = TextUI("Title", dialogPanel, "QUIT GAME", 40, TextAnchor.MiddleCenter, Cream);
            SetRect(dialogTitle.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -70f), new Vector2(600f, 70f));
            Text dialogMessage = TextUI("Message", dialogPanel, "Quit the game?", 27, TextAnchor.MiddleCenter, Cream);
            SetRect(dialogMessage.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 28f), new Vector2(590f, 100f));
            Button confirm = PositionedButton("ConfirmButton", dialogPanel, "QUIT", new Vector2(-150f, -120f), new Vector2(250f, 72f));
            Button cancel = PositionedButton("CancelButton", dialogPanel, "CANCEL", new Vector2(150f, -120f), new Vector2(250f, 72f));
            Text confirmLabel = confirm.GetComponentInChildren<Text>();
            ConfirmationDialog dialog = dialogRoot.gameObject.AddComponent<ConfirmationDialog>();

            Text version = TextUI("VersionText", safe, "v1.0  •  ANDROID", 18, TextAnchor.LowerRight, new Color(.65f, .62f, .56f));
            SetRect(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 24f), new Vector2(360f, 40f));

            Image fadeImage = Image("FadeOverlay", canvasGo.transform, Color.black);
            Stretch(fadeImage.rectTransform);
            fadeImage.transform.SetAsLastSibling();
            CanvasGroup fadeGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();

            controller = canvasGo.AddComponent<MainMenuController>();
            Assign(settingsUI, "canvasGroup", settingsGroup);
            Assign(settingsUI, "masterSlider", master);
            Assign(settingsUI, "musicSlider", music);
            Assign(settingsUI, "sfxSlider", sfx);
            Assign(settingsUI, "sensitivitySlider", sensitivity);
            Assign(settingsUI, "sensitivityValue", sensitivityValue);
            Assign(settingsUI, "qualitySlider", quality);
            Assign(settingsUI, "qualityValue", qualityValue);
            Assign(dialog, "canvasGroup", dialogGroup);
            Assign(dialog, "titleText", dialogTitle);
            Assign(dialog, "messageText", dialogMessage);
            Assign(dialog, "confirmLabel", confirmLabel);
            Assign(controller, "mainPanel", mainGroup);
            Assign(controller, "settingsUI", settingsUI);
            Assign(controller, "confirmationDialog", dialog);
            Assign(controller, "fadeOverlay", fadeGroup);
            Assign(controller, "continueButton", continueButton);

            UnityEventTools.AddPersistentListener(newGame.onClick, controller.PlayGame);
            UnityEventTools.AddPersistentListener(continueButton.onClick, controller.ContinueGame);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.ShowQuitConfirmation);
            UnityEventTools.AddPersistentListener(apply.onClick, settingsUI.ApplyFromUI);
            UnityEventTools.AddPersistentListener(back.onClick, controller.CloseSettings);
            UnityEventTools.AddPersistentListener(confirm.onClick, dialog.Confirm);
            UnityEventTools.AddPersistentListener(cancel.onClick, dialog.Cancel);
            UnityEventTools.AddPersistentListener(sensitivity.onValueChanged, new UnityAction<float>(settingsUI.UpdateSensitivityLabel));
            UnityEventTools.AddPersistentListener(quality.onValueChanged, new UnityAction<float>(settingsUI.UpdateQualityLabel));

            settingsPanel.gameObject.SetActive(false);
            dialogRoot.gameObject.SetActive(false);
        }

        private static void BuildPersistentAudio(AudioManager source)
        {
            GameObject audioGo = new("AUDIO_System_v1");
            AudioManager target = audioGo.AddComponent<AudioManager>();
            EditorUtility.CopySerialized(source, target);
            SerializedObject so = new(target);
            SerializedProperty library = so.FindProperty("library");
            int gameplayIndex = -1;
            for (int i = 0; i < library.arraySize; i++)
                if (library.GetArrayElementAtIndex(i).FindPropertyRelative("cue").enumValueIndex == (int)AudioCue.GameplayMusic) gameplayIndex = i;
            if (gameplayIndex >= 0)
            {
                library.InsertArrayElementAtIndex(gameplayIndex);
                library.GetArrayElementAtIndex(gameplayIndex).FindPropertyRelative("cue").enumValueIndex = (int)AudioCue.MenuMusic;
                library.GetArrayElementAtIndex(gameplayIndex).FindPropertyRelative("volume").floatValue = .42f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button MenuButton(string name, Transform parent, string label)
        {
            Button button = ButtonBase(name, parent, label);
            button.gameObject.AddComponent<LayoutElement>().minHeight = 92f;
            return button;
        }

        private static Button PositionedButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            Button button = ButtonBase(name, parent, label);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            return button;
        }

        private static Button ButtonBase(string name, Transform parent, string label)
        {
            Image image = Image(name, parent, ButtonNormal);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonNormal; colors.highlightedColor = new Color(.25f, .18f, .1f); colors.pressedColor = Gold;
            colors.selectedColor = colors.highlightedColor; colors.disabledColor = new Color(.11f, .1f, .09f, .48f); colors.colorMultiplier = 1f;
            button.colors = colors;
            Text text = TextUI("Label", image.transform, label, 27, TextAnchor.MiddleCenter, Cream);
            Stretch(text.rectTransform);
            image.gameObject.AddComponent<UIButtonAudio>();
            image.gameObject.AddComponent<ButtonPressFeedback>();
            return button;
        }

        private static Slider SettingSlider(Transform parent, string name, string label, float y, float min, float max)
        {
            Text labelText = TextUI(name + "Label", parent, label, 22, TextAnchor.MiddleLeft, Cream);
            SetRect(labelText.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-375f, y), new Vector2(270f, 55f));
            RectTransform root = Rect(name + "Slider", parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(105f, y), new Vector2(540f, 52f));
            Image bg = Image("Background", root, new Color(.12f, .1f, .08f)); Stretch(bg.rectTransform); bg.rectTransform.offsetMin = new Vector2(0f, 17f); bg.rectTransform.offsetMax = new Vector2(0f, -17f);
            RectTransform fillArea = Rect("Fill Area", root, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-20f, 0f));
            Image fill = Image("Fill", fillArea, Gold); Stretch(fill.rectTransform); fill.rectTransform.offsetMin = new Vector2(0f, 17f); fill.rectTransform.offsetMax = new Vector2(0f, -17f);
            RectTransform handleArea = Rect("Handle Slide Area", root, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-20f, 0f));
            Image handle = Image("Handle", handleArea, Cream);
            SetRect(handle.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(34f, 44f));
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max; slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Text ValueText(string name, Transform parent, float y)
        {
            Text value = TextUI(name, parent, "0.00", 20, TextAnchor.MiddleRight, Gold);
            SetRect(value.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(445f, y), new Vector2(120f, 50f));
            return value;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private static Image Image(string name, Transform parent, Color color)
        {
            RectTransform rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(100f, 100f));
            Image image = rect.gameObject.AddComponent<Image>(); image.color = color; return image;
        }

        private static Text TextUI(string name, Transform parent, string content, int size, TextAnchor alignment, Color color)
        {
            RectTransform rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(300f, 60f));
            Text text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = content; text.fontSize = size; text.alignment = alignment; text.color = color; text.raycastTarget = false;
            text.alignByGeometry = true;
            text.fontStyle = size >= 27 ? FontStyle.Bold : FontStyle.Normal;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        { rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }

        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }

        private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject so = new(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureBuildSettings()
        {
            string[] paths = { MenuPath, GameplayPath };
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }
    }
}
#endif
