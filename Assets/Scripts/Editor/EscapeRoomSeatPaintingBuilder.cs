#if UNITY_EDITOR
using PuzzleRoom.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class EscapeRoomSeatPaintingBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string TexturePath = "Assets/Art/Textures/Academy_Cryptography_Painting.png";
        private const string MaterialPath = "Assets/Art/Materials/MAT_AcademyPainting.mat";
        private const string MarkerName = "ART_SeatPainting_v1";

        static EscapeRoomSeatPaintingBuilder() => EditorApplication.delayCall += AutoBuildOnce;

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool alreadyBuilt = Find(scene, MarkerName) != null;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (!alreadyBuilt) Build(false);
        }

        [MenuItem("Tools/Puzzle Room/Art/Build Seat + Academy Painting")]
        public static void BuildFromMenu() => Build(true);

        private static void Build(bool selectResult)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject environment = Find(scene, "Environment");
            GameObject chair = Find(scene, "WoodenChair");
            if (environment == null || chair == null)
            {
                Debug.LogError("Environment or WoodenChair was not found. Seat painting setup stopped safely.");
                if (opened) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            Material imageMaterial = GetImageMaterial();
            Material frameMaterial = GetMaterial("Assets/Art/Materials/MAT_DarkWoodTrim.mat", new Color(0.09f, 0.045f, 0.025f));
            Material innerFrameMaterial = GetMaterial("Assets/Art/Materials/MAT_AgedBrass.mat", new Color(0.38f, 0.25f, 0.09f));

            GameObject root = Find(scene, MarkerName);
            if (root == null)
            {
                root = new GameObject(MarkerName);
                root.transform.SetParent(environment.transform, false);
            }
            ClearChildren(root.transform);

            GameObject painting = new GameObject("AcademyPainting");
            painting.transform.SetParent(root.transform, false);
            painting.transform.position = new Vector3(3.35f, 1.62f, -4.93f);
            Part("Image", painting.transform, Vector3.zero, new Vector3(1.52f, 1.52f, 0.035f), imageMaterial);
            Part("FrameTop", painting.transform, new Vector3(0f, 0.84f, 0.01f), new Vector3(1.88f, 0.16f, 0.10f), frameMaterial);
            Part("FrameBottom", painting.transform, new Vector3(0f, -0.84f, 0.01f), new Vector3(1.88f, 0.16f, 0.10f), frameMaterial);
            Part("FrameLeft", painting.transform, new Vector3(-0.84f, 0f, 0.01f), new Vector3(0.16f, 1.52f, 0.10f), frameMaterial);
            Part("FrameRight", painting.transform, new Vector3(0.84f, 0f, 0.01f), new Vector3(0.16f, 1.52f, 0.10f), frameMaterial);
            Part("InnerTop", painting.transform, new Vector3(0f, 0.765f, 0.07f), new Vector3(1.58f, 0.035f, 0.025f), innerFrameMaterial);
            Part("InnerBottom", painting.transform, new Vector3(0f, -0.765f, 0.07f), new Vector3(1.58f, 0.035f, 0.025f), innerFrameMaterial);
            Part("InnerLeft", painting.transform, new Vector3(-0.765f, 0f, 0.07f), new Vector3(0.035f, 1.52f, 0.025f), innerFrameMaterial);
            Part("InnerRight", painting.transform, new Vector3(0.765f, 0f, 0.07f), new Vector3(0.035f, 1.52f, 0.025f), innerFrameMaterial);

            ConfigureChair(chair);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (selectResult) Selection.activeGameObject = painting;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Academy painting and reusable E-to-sit chair interaction configured.");
        }

        private static void ConfigureChair(GameObject chair)
        {
            BoxCollider collider = chair.GetComponent<BoxCollider>();
            if (collider == null) collider = chair.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.95f, 0f);
            collider.size = new Vector3(1.0f, 1.9f, 1.0f);

            Transform seat = GetAnchor(chair.transform, "SeatAnchor");
            seat.position = new Vector3(3.35f, -0.18f, -3.58f);
            seat.rotation = Quaternion.Euler(0f, 180f, 0f);

            Transform exit = GetAnchor(chair.transform, "ExitAnchor");
            exit.position = new Vector3(3.35f, 0f, -2.35f);
            exit.rotation = Quaternion.Euler(0f, 180f, 0f);

            SittableChair sittable = chair.GetComponent<SittableChair>();
            if (sittable == null) sittable = chair.AddComponent<SittableChair>();
            SerializedObject serialized = new SerializedObject(sittable);
            serialized.FindProperty("seatAnchor").objectReferenceValue = seat;
            serialized.FindProperty("exitAnchor").objectReferenceValue = exit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform GetAnchor(Transform parent, string name)
        {
            Transform anchor = parent.Find(name);
            if (anchor != null) return anchor;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Material GetImageMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Shader shader = Shader.Find("Unlit/Texture");
            if (material == null)
            {
                material = new Material(shader) { name = "MAT_AcademyPainting" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetMaterial(string path, Color fallback)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Standard")) { color = fallback };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
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
