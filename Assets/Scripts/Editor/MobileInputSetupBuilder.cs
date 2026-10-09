#if UNITY_EDITOR
using PuzzleRoom.Mobile;
using PuzzleRoom.Player;
using PuzzleRoom.UI;
using PuzzleRoom.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class MobileInputSetupBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MarkerName = "MOBILE_INPUT_V1";

        static MobileInputSetupBuilder() => EditorApplication.delayCall += AutoBuildOnce;

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene current = SceneManager.GetSceneByPath(ScenePath);
            bool wasLoaded = current.IsValid() && current.isLoaded;
            Scene scene = wasLoaded ? current : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool ready = Find(scene, MarkerName) != null;
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            if (!ready) Build(false);
        }

        [MenuItem("Tools/Puzzle Room/Setup Android Mobile Input")]
        public static void BuildFromMenu() => Build(true);

        public static void BuildBatch() => Build(false);

        private static void Build(bool selectResult)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = Find(scene, "Player");
            GameObject cameraObject = Find(scene, "PlayerCamera");
            PauseMenuController pause = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            if (player == null || cameraObject == null || pause == null)
            {
                Debug.LogError("Mobile input setup requires Player, PlayerCamera, and PauseMenuController.");
                return;
            }

            GameObject old = Find(scene, "MobileHUDCanvas");
            if (old != null) Object.DestroyImmediate(old);
            GameObject oldMarker;
            while ((oldMarker = Find(scene, MarkerName)) != null)
                Object.DestroyImmediate(oldMarker);

            PlayerInputProvider provider = player.GetComponent<PlayerInputProvider>();
            if (provider == null) provider = player.AddComponent<PlayerInputProvider>();
            PlayerInteraction interaction = cameraObject.GetComponent<PlayerInteraction>();
            UVLightController uv = player.GetComponentInChildren<UVLightController>(true);

            GameObject canvasObject = new GameObject("MobileHUDCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MobileHUDController));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Gameplay HUD stays below modal puzzle, pause, and victory canvases.
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            GameObject safeArea = CreateUI("SafeArea", canvasObject.transform, typeof(SafeAreaFitter));
            Stretch(safeArea.GetComponent<RectTransform>());
            GameObject controls = CreateUI("GameplayControls", safeArea.transform, typeof(CanvasGroup));
            Stretch(controls.GetComponent<RectTransform>());

            GameObject lookObject = CreateUI("TouchLookArea", controls.transform, typeof(Image), typeof(TouchLookArea));
            RectTransform lookRect = lookObject.GetComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(.5f, 0f);
            lookRect.anchorMax = Vector2.one;
            lookRect.offsetMin = lookRect.offsetMax = Vector2.zero;
            lookObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, .001f);

            GameObject joystickObject = CreateUI("MovementJoystick", controls.transform, typeof(Image), typeof(VirtualJoystick));
            RectTransform joystickRect = joystickObject.GetComponent<RectTransform>();
            SetAnchored(joystickRect, new Vector2(0f, 0f), new Vector2(220f, 220f),
                new Vector2(0f, 0f), new Vector2(150f, 150f));
            joystickObject.GetComponent<Image>().color = new Color(.08f, .1f, .12f, .58f);
            GameObject handleObject = CreateUI("Handle", joystickObject.transform, typeof(Image));
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            SetAnchored(handleRect, Vector2.zero, new Vector2(92f, 92f), new Vector2(.5f, .5f), Vector2.zero);
            handleObject.GetComponent<Image>().color = new Color(.85f, .88f, .92f, .78f);
            VirtualJoystick joystick = joystickObject.GetComponent<VirtualJoystick>();
            joystick.Configure(joystickRect, handleRect);

            Button interactButton = CreateButton("InteractButton", controls.transform, "INTERACT",
                new Vector2(1f, 0f), new Vector2(-150f, 145f), new Vector2(190f, 92f));
            Button uvButton = CreateButton("UVButton", controls.transform, "UV OFF",
                new Vector2(1f, 0f), new Vector2(-150f, 265f), new Vector2(190f, 92f));
            Button pauseButton = CreateButton("PauseButton", safeArea.transform, "II",
                new Vector2(1f, 1f), new Vector2(-75f, -70f), new Vector2(84f, 84f));

            GameObject crosshair = CreateUI("Crosshair", safeArea.transform, typeof(Image));
            RectTransform crosshairRect = crosshair.GetComponent<RectTransform>();
            SetAnchored(crosshairRect, Vector2.zero, new Vector2(8f, 8f), new Vector2(.5f, .5f), Vector2.zero);
            crosshair.GetComponent<Image>().color = Color.white;
            crosshair.GetComponent<Image>().raycastTarget = false;

            MobileHUDController hud = canvasObject.GetComponent<MobileHUDController>();
            UnityEventTools.AddPersistentListener(interactButton.onClick, hud.Interact);
            UnityEventTools.AddPersistentListener(uvButton.onClick, hud.ToggleUV);
            UnityEventTools.AddPersistentListener(pauseButton.onClick, hud.Pause);

            SerializedObject hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("playerInteraction").objectReferenceValue = interaction;
            hudSerialized.FindProperty("inputProvider").objectReferenceValue = provider;
            hudSerialized.FindProperty("playerLook").objectReferenceValue = cameraObject.GetComponent<PlayerLook>();
            hudSerialized.FindProperty("uvLightController").objectReferenceValue = uv;
            hudSerialized.FindProperty("pauseMenu").objectReferenceValue = pause;
            hudSerialized.FindProperty("gameFlowManager").objectReferenceValue =
                Object.FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
            hudSerialized.FindProperty("interactButton").objectReferenceValue = interactButton;
            hudSerialized.FindProperty("uvButton").objectReferenceValue = uvButton;
            hudSerialized.FindProperty("uvLabel").objectReferenceValue = uvButton.GetComponentInChildren<Text>();
            hudSerialized.FindProperty("gameplayControls").objectReferenceValue = controls.GetComponent<CanvasGroup>();
            hudSerialized.FindProperty("showInEditorForTesting").boolValue = true;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            provider.SetMobileSources(joystick, lookObject.GetComponent<TouchLookArea>());
            SerializedObject lockSerialized = new SerializedObject(player.GetComponent<PlayerControlLock>());
            lockSerialized.FindProperty("inputProvider").objectReferenceValue = provider;
            lockSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject marker = new GameObject(MarkerName);
            SceneManager.MoveGameObjectToScene(marker, scene);

            ConfigureAndroidPlayerSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (selectResult) Selection.activeGameObject = canvasObject;
            Debug.Log("Android mobile input configured. Enable Show In Editor For Testing on MobileHUDCanvas for mouse testing.");
        }

        private static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            string identifier = PlayerSettings.GetApplicationIdentifier(android);
            if (string.IsNullOrWhiteSpace(identifier) || identifier.StartsWith("com.DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(android, "com.defaultcompany.puzzleroom3d");
        }

        private static GameObject CreateUI(string name, Transform parent, params System.Type[] extraTypes)
        {
            System.Type[] types = new System.Type[extraTypes.Length + 1];
            types[0] = typeof(RectTransform);
            extraTypes.CopyTo(types, 1);
            GameObject go = new GameObject(name, types);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            GameObject go = CreateUI(name, parent, typeof(Image), typeof(Button));
            SetAnchored(go.GetComponent<RectTransform>(), Vector2.zero, size, anchor, position);
            go.GetComponent<Image>().color = new Color(.1f, .12f, .15f, .88f);
            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(.22f, .28f, .34f, 1f);
            colors.pressedColor = new Color(.72f, .48f, .15f, 1f);
            colors.disabledColor = new Color(.1f, .12f, .15f, .32f);
            button.colors = colors;

            GameObject textObject = CreateUI("Label", go.transform, typeof(Text));
            Stretch(textObject.GetComponent<RectTransform>());
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        private static void SetAnchored(RectTransform rect, Vector2 pivot, Vector2 size, Vector2 anchor, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot == Vector2.zero ? new Vector2(.5f, .5f) : pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == name) return child.gameObject;
            return null;
        }
    }
}
#endif
