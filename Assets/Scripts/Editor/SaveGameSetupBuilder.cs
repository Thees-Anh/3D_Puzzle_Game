#if UNITY_EDITOR
using System.Linq;
using PuzzleRoom.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    public static class SaveGameSetupBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";

        [MenuItem("Tools/Puzzle Room/Save System/Setup Pause Save Button")]
        public static void Setup()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PauseMenuController pause = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            if (pause == null) throw new System.InvalidOperationException("PauseMenuController not found.");

            SerializedObject pauseSo = new(pause);
            GameObject panel = pauseSo.FindProperty("pausePanel").objectReferenceValue as GameObject;
            if (panel == null) throw new System.InvalidOperationException("PausePanel reference is missing.");

            Button resume = FindButton(panel.transform, "Resume");
            Button restart = FindButton(panel.transform, "Restart");
            Button mainMenu = FindButton(panel.transform, "MainMenu");
            Button save = FindButton(panel.transform, "SaveGame");
            if (restart == null || resume == null || mainMenu == null) throw new System.InvalidOperationException("Pause buttons are incomplete.");

            if (save == null)
            {
                GameObject clone = Object.Instantiate(restart.gameObject, restart.transform.parent);
                clone.name = "SaveGame";
                save = clone.GetComponent<Button>();
                save.onClick = new Button.ButtonClickedEvent();
                Text label = clone.GetComponentsInChildren<Text>(true).FirstOrDefault();
                if (label != null) label.text = "SAVE GAME";
                UnityEventTools.AddPersistentListener(save.onClick, pause.SaveGame);
            }

            SetY(resume, 105f);
            SetY(save, 20f);
            SetY(restart, -65f);
            SetY(mainMenu, -150f);
            EditorUtility.SetDirty(pause);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SAVE_SYSTEM_V1 pause Save Game button configured.");
        }

        private static Button FindButton(Transform root, string name) =>
            root.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button.name == name);

        private static void SetY(Button button, float y)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, y);
        }
    }
}
#endif
