#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PuzzleRoom.EditorTools
{
    /// <summary>
    /// Non-destructive helpers for the art pass. Nothing runs automatically and
    /// gameplay roots, components, colliders, and serialized references are preserved.
    /// </summary>
    public sealed class EscapeRoomArtTools : EditorWindow
    {
        private static readonly string[] ArtFolders =
        {
            "Assets/Art/Generated",
            "Assets/Art/Generated/Environment",
            "Assets/Art/Generated/Furniture",
            "Assets/Art/Generated/Interactive",
            "Assets/Art/Generated/Puzzles",
            "Assets/Art/Materials",
            "Assets/Art/Imported",
            "Assets/Art/Textures",
            "Assets/Art/Prefabs"
        };

        private Material materialToApply;

        [MenuItem("Tools/Puzzle Room/Escape Room Art Tools")]
        private static void Open()
        {
            GetWindow<EscapeRoomArtTools>("Escape Room Art Tools");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Stylized Low-Poly Mystery Room", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Safe preparation tools only. These actions never delete gameplay objects " +
                "or remove scripts/colliders.", MessageType.Info);

            if (GUILayout.Button("Ensure Art Folder Structure"))
            {
                EnsureArtFolders();
            }

            if (GUILayout.Button("Build / Refresh Environment + Furniture"))
            {
                EscapeRoomEnvironmentArtBuilder.BuildEnvironmentAndFurniture(true);
            }

            if (GUILayout.Button("Build / Refresh Interactive Objects"))
            {
                EscapeRoomInteractiveArtBuilder.BuildInteractiveObjects(true);
            }

            if (GUILayout.Button("Build / Refresh Puzzle Visuals"))
            {
                EscapeRoomPuzzleArtBuilder.BuildPuzzleVisuals(true);
            }

            if (GUILayout.Button("Finalize Materials + Lighting"))
            {
                EscapeRoomMaterialLightingBuilder.BuildMaterialsAndLighting(true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Gameplay Roots", EditorStyles.boldLabel);

            if (GUILayout.Button("Audit Selected Roots (Console)"))
            {
                AuditSelection();
            }

            if (GUILayout.Button("Create / Select Visual Child"))
            {
                CreateOrSelectVisualChildren();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material Preview", EditorStyles.boldLabel);
            materialToApply = (Material)EditorGUILayout.ObjectField(
                "Material", materialToApply, typeof(Material), false);

            using (new EditorGUI.DisabledScope(materialToApply == null))
            {
                if (GUILayout.Button("Apply Material To Selected Renderers"))
                {
                    ApplyMaterialToSelection();
                }
            }

            EditorGUILayout.HelpBox(
                "Create Visual Child is idempotent: if a direct child named Visual exists, " +
                "the tool selects it instead of creating a duplicate.", MessageType.None);
        }

        private static void EnsureArtFolders()
        {
            foreach (string folder in ArtFolders)
            {
                EnsureFolder(folder);
            }

            AssetDatabase.Refresh();
            Debug.Log("Escape Room Art folder structure is ready.");
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }

        private static void AuditSelection()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Select one or more gameplay roots to audit.");
                return;
            }

            foreach (GameObject root in Selection.gameObjects)
            {
                var report = new StringBuilder();
                report.AppendLine($"ART AUDIT — {root.name}");
                report.AppendLine($"Scene: {root.scene.path}");
                report.AppendLine("Scripts: " + string.Join(", ", root
                    .GetComponents<MonoBehaviour>()
                    .Where(component => component != null)
                    .Select(component => component.GetType().Name)));
                report.AppendLine($"Colliders in hierarchy: {root.GetComponentsInChildren<Collider>(true).Length}");
                report.AppendLine($"Renderers in hierarchy: {root.GetComponentsInChildren<Renderer>(true).Length}");
                report.AppendLine("Has direct Visual child: " + (root.transform.Find("Visual") != null));
                Debug.Log(report.ToString(), root);
            }
        }

        private static void CreateOrSelectVisualChildren()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Select one or more gameplay roots first.");
                return;
            }

            var visuals = new System.Collections.Generic.List<GameObject>();
            foreach (GameObject root in Selection.gameObjects)
            {
                Transform existing = root.transform.Find("Visual");
                if (existing != null)
                {
                    visuals.Add(existing.gameObject);
                    continue;
                }

                GameObject visual = new GameObject("Visual");
                Undo.RegisterCreatedObjectUndo(visual, "Create Visual Container");
                Undo.SetTransformParent(visual.transform, root.transform, "Parent Visual Container");
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                visuals.Add(visual);
            }

            Selection.objects = visuals.ToArray();
        }

        private void ApplyMaterialToSelection()
        {
            Renderer[] renderers = Selection.gameObjects
                .SelectMany(selected => selected.GetComponentsInChildren<Renderer>(true))
                .Distinct()
                .ToArray();

            if (renderers.Length == 0)
            {
                Debug.LogWarning("The selection contains no Renderer.");
                return;
            }

            Undo.RecordObjects(renderers, "Apply Art Material");
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterial = materialToApply;
                EditorUtility.SetDirty(renderer);
            }
        }
    }
}
#endif
