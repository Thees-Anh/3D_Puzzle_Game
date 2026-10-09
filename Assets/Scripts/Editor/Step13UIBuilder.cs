#if UNITY_EDITOR
using PuzzleRoom.Core;
using PuzzleRoom.Player;
using PuzzleRoom.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class Step13UIBuilder
    {
        private const string GameplayPath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

        static Step13UIBuilder() => EditorApplication.delayCall += AutoBuildOnce;

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene gameplay = Open(GameplayPath, out bool closeGameplay);
            bool gameplayReady = Find(gameplay, "STEP13_PauseUI_v1") != null;
            if (closeGameplay) EditorSceneManager.CloseScene(gameplay, true);
            Scene menu = Open(MainMenuPath, out bool closeMenu);
            bool menuReady = Find(menu, "STEP13_MainMenu_v1") != null;
            if (closeMenu) EditorSceneManager.CloseScene(menu, true);
            if (!gameplayReady || !menuReady) Build(false);
        }

        [MenuItem("Tools/Puzzle Room/Setup Step 13 Main Menu + Pause")]
        public static void BuildFromMenu() => Build(true);

        private static void Build(bool selectPause)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            BuildMainMenu();
            BuildPauseMenu(selectPause);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Step 13 configured: Main Menu Play/Quit, Pause UI, ESC priority, restart, and scene navigation.");
        }

        private static void BuildMainMenu()
        {
            Scene scene = Open(MainMenuPath, out bool close);
            GameObject canvas = Find(scene, "MainMenuCanvas");
            if (canvas == null)
            {
                Debug.LogError("MainMenuCanvas was not found.");
                if (close) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            MainMenuController controller = canvas.GetComponent<MainMenuController>();
            if (controller == null) controller = canvas.AddComponent<MainMenuController>();
            Transform background = Find(scene, "Background")?.transform ?? canvas.transform;

            GameObject playObject = Find(scene, "Play");
            if (playObject != null)
                SetRect(playObject.GetComponent<RectTransform>(), new Vector2(0f, -55f), new Vector2(280f, 70f));

            GameObject existingQuit = Find(scene, "Quit");
            if (existingQuit == null)
            {
                Button quit = CreateButton("Quit", background, "QUIT", new Vector2(0f, -145f), new Vector2(280f, 70f));
                UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);
            }

            if (Find(scene, "STEP13_MainMenu_v1") == null)
            {
                GameObject marker = new GameObject("STEP13_MainMenu_v1");
                SceneManager.MoveGameObjectToScene(marker, scene);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (close) EditorSceneManager.CloseScene(scene, true);
        }

        private static void BuildPauseMenu(bool selectPause)
        {
            Scene scene = Open(GameplayPath, out bool close);
            GameObject player = Find(scene, "Player");
            GameObject flowObject = Find(scene, "GameFlow");
            if (player == null || flowObject == null)
            {
                Debug.LogError("Player or GameFlow was not found. Pause setup stopped safely.");
                if (close) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            GameObject canvasObject = Find(scene, "PauseCanvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("PauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PauseMenuController));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
            }
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panel = FindChild(canvasObject.transform, "PausePanel");
            if (panel == null)
            {
                panel = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(canvasObject.transform, false);
            }
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.015f, 0.02f, 0.025f, 0.92f);
            ClearChildren(panel.transform);

            PauseMenuController controller = canvasObject.GetComponent<PauseMenuController>();
            CreateText("Title", panel.transform, "PAUSED", new Vector2(0f, 185f), new Vector2(650f, 90f), 54, new Color(0.95f, 0.78f, 0.35f));
            Button resume = CreateButton("Resume", panel.transform, "RESUME", new Vector2(0f, 60f), new Vector2(330f, 72f));
            Button restart = CreateButton("Restart", panel.transform, "RESTART", new Vector2(0f, -35f), new Vector2(330f, 72f));
            Button menu = CreateButton("MainMenu", panel.transform, "MAIN MENU", new Vector2(0f, -130f), new Vector2(330f, 72f));
            UnityEventTools.AddPersistentListener(resume.onClick, controller.ResumeGame);
            UnityEventTools.AddPersistentListener(restart.onClick, controller.RestartGame);
            UnityEventTools.AddPersistentListener(menu.onClick, controller.ReturnToMainMenu);

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("pausePanel").objectReferenceValue = panel;
            serialized.FindProperty("playerMovement").objectReferenceValue = player.GetComponent<PlayerMovement>();
            serialized.FindProperty("playerLook").objectReferenceValue = player.GetComponentInChildren<PlayerLook>(true);
            serialized.FindProperty("playerInteraction").objectReferenceValue = player.GetComponentInChildren<PlayerInteraction>(true);
            serialized.FindProperty("keypadUI").objectReferenceValue = FindComponent<KeypadUI>(scene);
            serialized.FindProperty("finalPowerPuzzleUI").objectReferenceValue = FindComponent<FinalPowerPuzzleUI>(scene);
            serialized.FindProperty("gameFlowManager").objectReferenceValue = flowObject.GetComponent<GameFlowManager>();
            serialized.FindProperty("gameplaySceneName").stringValue = "PuzzleRoom";
            serialized.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);

            if (Find(scene, "STEP13_PauseUI_v1") == null)
            {
                GameObject marker = new GameObject("STEP13_PauseUI_v1");
                SceneManager.MoveGameObjectToScene(marker, scene);
            }
            if (FindComponent<EventSystem>(scene) == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (selectPause) Selection.activeGameObject = canvasObject;
            if (close) EditorSceneManager.CloseScene(scene, true);
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), position, size);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.21f, 0.98f);
            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.30f, 0.34f, 0.38f, 1f);
            colors.pressedColor = new Color(0.62f, 0.42f, 0.14f, 1f);
            button.colors = colors;
            CreateText("Label", go.transform, label, Vector2.zero, size, 25, Color.white);
            return button;
        }

        private static Text CreateText(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), position, size);
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static Scene Open(string path, out bool shouldClose)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            shouldClose = !scene.IsValid() || !scene.isLoaded;
            return shouldClose ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : scene;
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == name) return child.gameObject;
            return null;
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
            return null;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null) return result;
            }
            return null;
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == MainMenuPath)) scenes.Insert(0, new EditorBuildSettingsScene(MainMenuPath, true));
            if (!scenes.Exists(item => item.path == GameplayPath)) scenes.Add(new EditorBuildSettingsScene(GameplayPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
