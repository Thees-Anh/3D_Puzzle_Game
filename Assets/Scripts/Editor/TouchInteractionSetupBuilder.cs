#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleRoom.EditorTools
{
    public static class TouchInteractionSetupBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MarkerName = "TOUCH_INTERACTION_V1";

        [InitializeOnLoadMethod]
        private static void ScheduleAutomaticSetup()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += TryAutomaticSetup;
        }

        private static void TryAutomaticSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) return;
            bool configured = scene.GetRootGameObjects().Any(item => item.name == MarkerName);
            if (!configured) Setup();
        }

        [MenuItem("Tools/Puzzle Room/Mobile/Setup Touch Interaction")]
        public static void Setup()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(item => item.scene == scene).ToArray();
            foreach (GameObject item in objects)
            {
                if (item.name == "Crosshair" || item.name == "InteractButton") item.SetActive(false);
            }
            if (!scene.GetRootGameObjects().Any(item => item.name == MarkerName))
                new GameObject(MarkerName);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TOUCH_INTERACTION_V1 configured: crosshair and legacy Interact button disabled.");
        }
    }
}
#endif
