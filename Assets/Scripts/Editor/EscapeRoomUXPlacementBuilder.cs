#if UNITY_EDITOR
using PuzzleRoom.Puzzles.Hanoi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class EscapeRoomUXPlacementBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string Marker = "UX_WorldHanoiCounter_v3";

        static EscapeRoomUXPlacementBuilder() => EditorApplication.delayCall += AutoBuild;

        private static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool ready = Find(scene, Marker) != null;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (!ready) Build(false);
        }

        [MenuItem("Tools/Puzzle Room/Art/Move Hanoi Counter Above Bookshelf")]
        public static void BuildMenu() => Build(true);

        private static void Build(bool select)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject hanoi = Find(scene, "HanoiPuzzle");
            GameObject counterObject = Find(scene, "MoveCounter");
            if (hanoi == null || counterObject == null)
            {
                Debug.LogError("HanoiPuzzle or MoveCounter was not found. No references were changed.");
                if (opened) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            GameObject worldCanvasObject = Find(scene, "HanoiMoveDisplayWorld");
            if (worldCanvasObject == null)
            {
                worldCanvasObject = new GameObject("HanoiMoveDisplayWorld", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(worldCanvasObject, scene);
                worldCanvasObject.transform.SetParent(hanoi.transform, false);
            }

            RectTransform canvasRect = worldCanvasObject.GetComponent<RectTransform>();
            // Keep the display below the bookshelf's upper trim while remaining
            // clearly above the four symbol buttons.
            canvasRect.position = new Vector3(5.34f, 2.48f, 1.5f);
            canvasRect.rotation = Quaternion.Euler(0f, 90f, 0f);
            canvasRect.localScale = Vector3.one * .0042f;
            canvasRect.sizeDelta = new Vector2(360f, 92f);
            Canvas canvas = worldCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;

            GameObject panel = FindChild(worldCanvasObject.transform, "CounterPanel");
            if (panel == null)
            {
                panel = new GameObject("CounterPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(worldCanvasObject.transform, false);
            }
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(.055f, .04f, .025f, .90f);

            counterObject.transform.SetParent(panel.transform, false);
            RectTransform counterRect = counterObject.GetComponent<RectTransform>();
            Stretch(counterRect);
            counterRect.offsetMin = new Vector2(12f, 6f);
            counterRect.offsetMax = new Vector2(-12f, -6f);
            Text text = counterObject.GetComponent<Text>();
            text.fontSize = 25;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, .84f, .48f);
            text.raycastTarget = false;
            Outline outline = counterObject.GetComponent<Outline>();
            if (outline == null) outline = counterObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject oldHud = Find(scene, "HanoiHUD");
            if (oldHud != null && oldHud != worldCanvasObject && oldHud.transform.childCount == 0)
                Object.DestroyImmediate(oldHud);

            if (Find(scene, Marker) == null)
            {
                GameObject marker = new GameObject(Marker);
                marker.transform.SetParent(hanoi.transform, false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (select) Selection.activeGameObject = worldCanvasObject;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Hanoi move counter moved from screen HUD to the display above the symbol buttons.");
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
            Transform child = parent.Find(name);
            return child != null ? child.gameObject : null;
        }
    }
}
#endif
