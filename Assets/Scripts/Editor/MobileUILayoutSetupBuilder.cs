#if UNITY_EDITOR
using PuzzleRoom.Mobile;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class MobileUILayoutSetupBuilder
    {
        private const string GameplayPath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MenuPath = "Assets/Scenes/MainMenu.unity";
        private const string Marker = "MOBILE_UI_LAYOUT_V1";

        static MobileUILayoutSetupBuilder() => EditorApplication.delayCall += AutoBuildOnce;

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene current = SceneManager.GetSceneByPath(GameplayPath);
            bool wasLoaded = current.IsValid() && current.isLoaded;
            Scene scene = wasLoaded ? current : EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Additive);
            bool ready = Find(scene, Marker) != null;
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            if (!ready) Build(false);
        }

        [MenuItem("Tools/Puzzle Room/Setup Android Mobile UI Layout")]
        public static void BuildFromMenu() => Build(true);

        public static void BuildBatch() => Build(false);

        private static void Build(bool selectResult)
        {
            Scene gameplay = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            ConfigureGameplay(gameplay);
            EditorSceneManager.MarkSceneDirty(gameplay);
            EditorSceneManager.SaveScene(gameplay);

            Scene menu = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            ConfigureCanvas(Find(menu, "MainMenuCanvas"));
            EnlargeNamedButton(menu, "Play", 300f, 84f);
            EnlargeNamedButton(menu, "Quit", 300f, 84f);
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);

            EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            if (selectResult) Selection.activeGameObject = Find(SceneManager.GetActiveScene(), "MobileHUDCanvas");
            Debug.Log("Android mobile UI layout configured for 16:9, 18:9, 19.5:9, and 20:9 landscape references.");
        }

        private static void ConfigureGameplay(Scene scene)
        {
            string[] canvases = { "KeypadCanvas", "FinalPowerCanvas", "PauseCanvas", "VictoryCanvas", "MobileHUDCanvas" };
            foreach (string canvasName in canvases) ConfigureCanvas(Find(scene, canvasName));

            EnsureSafeArea(Find(scene, "KeypadCanvas"));
            EnsureSafeArea(Find(scene, "FinalPowerCanvas"));
            EnsureSafeArea(Find(scene, "PauseCanvas"));
            EnsureSafeArea(Find(scene, "VictoryCanvas"));

            for (int digit = 0; digit <= 9; digit++)
                EnlargeNamedButton(scene, $"Button_{digit}", 104f, 74f);
            EnlargeNamedButton(scene, "Button_Clear", 104f, 74f);
            EnlargeNamedButton(scene, "Button_Enter", 104f, 74f);
            EnlargeNamedButton(scene, "Button_Close", 64f, 64f);

            for (int row = 0; row < 6; row++)
                for (int column = 0; column < 6; column++)
                    EnlargeNamedButton(scene, $"Tile_R{row}C{column}", 78f, 78f);

            EnlargeNamedButton(scene, "ResetCircuit", 210f, 68f);
            EnlargeNamedButton(scene, "Close", 150f, 64f);
            EnlargeNamedButton(scene, "ActivateControl", 210f, 84f);
            EnlargeNamedButton(scene, "ActivateLight", 210f, 84f);
            EnlargeNamedButton(scene, "ActivateExit", 210f, 84f);

            EnlargeNamedButton(scene, "Resume", 350f, 84f);
            EnlargeNamedButton(scene, "Restart", 350f, 84f);
            EnlargeNamedButtons(scene, "MainMenu", 270f, 84f);
            EnlargeNamedButton(scene, "PlayAgain", 270f, 84f);

            if (Find(scene, Marker) == null)
            {
                GameObject marker = new GameObject(Marker);
                SceneManager.MoveGameObjectToScene(marker, scene);
            }
        }

        private static void ConfigureCanvas(GameObject canvasObject)
        {
            if (canvasObject == null) return;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
        }

        private static void EnsureSafeArea(GameObject canvasObject)
        {
            if (canvasObject == null) return;
            Transform existing = canvasObject.transform.Find("MobileSafeArea");
            if (existing != null) return;

            GameObject safeObject = new GameObject("MobileSafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeObject.transform.SetParent(canvasObject.transform, false);
            RectTransform safeRect = safeObject.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;

            for (int i = canvasObject.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvasObject.transform.GetChild(i);
                if (child == safeObject.transform) continue;
                child.SetParent(safeObject.transform, false);
            }
        }

        private static void EnlargeNamedButtons(Scene scene, string name, float width, float height)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                    if (button.name == name) Enlarge(button, width, height);
        }

        private static void EnlargeNamedButton(Scene scene, string name, float width, float height)
        {
            GameObject target = Find(scene, name);
            Button button = target != null ? target.GetComponent<Button>() : null;
            if (button != null) Enlarge(button, width, height);
        }

        private static void Enlarge(Button button, float width, float height)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, width), Mathf.Max(rect.sizeDelta.y, height));
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
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
