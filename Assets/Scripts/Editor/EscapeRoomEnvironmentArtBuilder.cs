#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleRoom.EditorTools
{
    /// <summary>Idempotent, visual-only environment and furniture art pass.</summary>
    [InitializeOnLoad]
    public static class EscapeRoomEnvironmentArtBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MaterialFolder = "Assets/Art/Materials";
        private const string PrefabFolder = "Assets/Art/Prefabs";

        static EscapeRoomEnvironmentArtBuilder()
        {
            EditorApplication.delayCall += AutoBuildOnce;
        }

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool alreadyBuilt = Find(scene, "ART_EnvironmentFurniture_v1") != null;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (!alreadyBuilt) BuildEnvironmentAndFurniture(false);
        }

        [MenuItem("Tools/Puzzle Room/Art/Build Environment + Furniture")]
        public static void BuildFromMenu() => BuildEnvironmentAndFurniture(true);

        public static void BuildEnvironmentAndFurniture(bool selectArtRoot)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before running the Environment Art Pass.");
                return;
            }

            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject environment = Find(scene, "Environment");
            if (environment == null)
            {
                Debug.LogError("Environment root was not found. Art pass stopped without changes.");
                if (opened) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            Material floorMat = MaterialAsset("MAT_DarkWoodFloor", new Color(0.16f, 0.085f, 0.04f), 0.05f, 0.22f);
            Material woodMat = MaterialAsset("MAT_DarkOak", new Color(0.20f, 0.105f, 0.055f), 0.02f, 0.2f);
            Material woodEdgeMat = MaterialAsset("MAT_DarkWoodTrim", new Color(0.09f, 0.045f, 0.025f), 0.02f, 0.16f);
            Material wallMat = MaterialAsset("MAT_MutedGreenWall", new Color(0.22f, 0.25f, 0.20f), 0f, 0.12f);
            Material ceilingMat = MaterialAsset("MAT_DarkCeiling", new Color(0.10f, 0.105f, 0.10f), 0f, 0.1f);
            Material metalMat = MaterialAsset("MAT_IndustrialMetal", new Color(0.12f, 0.13f, 0.14f), 0.65f, 0.28f);
            Material brassMat = MaterialAsset("MAT_AgedBrass", new Color(0.38f, 0.25f, 0.09f), 0.65f, 0.3f);
            Material warmMat = MaterialAsset("MAT_WarmLamp", new Color(1f, 0.58f, 0.16f), 0.05f, 0.45f);

            ApplyMaterial(Find(scene, "Floor"), floorMat);
            ApplyMaterial(Find(scene, "Ceiling"), ceilingMat);
            ApplyMaterial(Find(scene, "Wall_North"), wallMat);
            ApplyMaterial(Find(scene, "Wall_East"), wallMat);
            ApplyMaterial(Find(scene, "Wall_West"), wallMat);
            ApplyMaterial(Find(scene, "Wall_South_Left"), wallMat);
            ApplyMaterial(Find(scene, "Wall_South_Right"), wallMat);
            ApplyMaterial(Find(scene, "Wall_South_AboveExit"), wallMat);

            GameObject artRoot = Child(environment.transform, "ART_EnvironmentFurniture_v1");
            ClearGeneratedChildren(artRoot.transform);
            GameObject architecture = Child(artRoot.transform, "Architecture");
            BuildFloorDetails(architecture.transform, floorMat, woodEdgeMat);
            BuildBaseboards(architecture.transform, woodEdgeMat);
            BuildCeiling(architecture.transform, woodMat, metalMat, warmMat);
            BuildDoorFrame(architecture.transform, woodMat, metalMat);

            GameObject furniture = Child(artRoot.transform, "Furniture");
            BuildStandaloneFurniture(furniture.transform, woodMat, woodEdgeMat, metalMat);
            DressGameplayFurniture(scene, woodMat, woodEdgeMat, metalMat, brassMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (selectArtRoot)
            {
                Selection.activeGameObject = artRoot;
                EditorGUIUtility.PingObject(artRoot);
            }
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Environment + Furniture art pass built without changing gameplay roots or colliders.");
        }

        private static void BuildFloorDetails(Transform parent, Material floor, Material seam)
        {
            GameObject group = Child(parent, "DarkWoodFloorPlanks");
            for (int row = 0; row < 12; row++)
            {
                float z = -4.55f + row * 0.83f;
                Part($"Plank_{row + 1:00}", group.transform, new Vector3(0f, 0.012f, z), new Vector3(11.85f, 0.024f, 0.79f), floor);
                if (row < 11) Part($"Seam_{row + 1:00}", group.transform, new Vector3(0f, 0.027f, z + 0.415f), new Vector3(11.85f, 0.006f, 0.018f), seam);
            }
        }

        private static void BuildBaseboards(Transform parent, Material material)
        {
            GameObject group = Child(parent, "Baseboards");
            Part("North", group.transform, new Vector3(0f, 0.14f, 4.96f), new Vector3(11.8f, 0.28f, 0.12f), material);
            Part("West", group.transform, new Vector3(-5.96f, 0.14f, 0f), new Vector3(0.12f, 0.28f, 9.8f), material);
            Part("East", group.transform, new Vector3(5.96f, 0.14f, 0f), new Vector3(0.12f, 0.28f, 9.8f), material);
            Part("SouthLeft", group.transform, new Vector3(-3.45f, 0.14f, -4.96f), new Vector3(5.0f, 0.28f, 0.12f), material);
            Part("SouthRight", group.transform, new Vector3(3.45f, 0.14f, -4.96f), new Vector3(5.0f, 0.28f, 0.12f), material);
        }

        private static void BuildCeiling(Transform parent, Material wood, Material metal, Material warm)
        {
            GameObject beams = Child(parent, "CeilingBeams");
            Part("Beam_North", beams.transform, new Vector3(0f, 3.12f, 3.1f), new Vector3(11.7f, 0.22f, 0.25f), wood);
            Part("Beam_Center", beams.transform, new Vector3(0f, 3.12f, 0f), new Vector3(11.7f, 0.22f, 0.25f), wood);
            Part("Beam_South", beams.transform, new Vector3(0f, 3.12f, -3.1f), new Vector3(11.7f, 0.22f, 0.25f), wood);

            GameObject lamp = Child(parent, "CeilingLampVisual");
            Part("Mount", lamp.transform, new Vector3(0f, 3.03f, 0f), new Vector3(0.48f, 0.12f, 0.48f), metal, PrimitiveType.Cylinder);
            Part("Stem", lamp.transform, new Vector3(0f, 2.84f, 0f), new Vector3(0.09f, 0.34f, 0.09f), metal, PrimitiveType.Cylinder);
            GameObject shade = Part("Shade", lamp.transform, new Vector3(0f, 2.62f, 0f), new Vector3(0.62f, 0.22f, 0.62f), metal, PrimitiveType.Cylinder);
            shade.transform.localScale = new Vector3(0.62f, 0.12f, 0.62f);
            Part("WarmBulb", lamp.transform, new Vector3(0f, 2.48f, 0f), new Vector3(0.22f, 0.22f, 0.22f), warm, PrimitiveType.Sphere);
        }

        private static void BuildDoorFrame(Transform parent, Material wood, Material metal)
        {
            GameObject frame = Child(parent, "ExitDoorFrame");
            Part("LeftPost", frame.transform, new Vector3(-0.92f, 1.3f, -4.82f), new Vector3(0.22f, 2.65f, 0.22f), wood);
            Part("RightPost", frame.transform, new Vector3(0.92f, 1.3f, -4.82f), new Vector3(0.22f, 2.65f, 0.22f), wood);
            Part("Header", frame.transform, new Vector3(0f, 2.57f, -4.82f), new Vector3(2.05f, 0.24f, 0.22f), wood);
            Part("LeftBracket", frame.transform, new Vector3(-0.92f, 2.55f, -4.69f), new Vector3(0.3f, 0.16f, 0.08f), metal);
            Part("RightBracket", frame.transform, new Vector3(0.92f, 2.55f, -4.69f), new Vector3(0.3f, 0.16f, 0.08f), metal);
        }

        private static void BuildStandaloneFurniture(Transform parent, Material wood, Material trim, Material metal)
        {
            GameObject chair = Child(parent, "WoodenChair");
            chair.transform.localPosition = new Vector3(3.35f, 0f, -3.55f);
            Part("Seat", chair.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.9f, 0.14f, 0.9f), wood);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                Part($"Leg_{x}_{z}", chair.transform, new Vector3(x * 0.34f, 0.42f, z * 0.34f), new Vector3(0.12f, 0.84f, 0.12f), trim);
            Part("BackLeft", chair.transform, new Vector3(-0.35f, 1.42f, 0.37f), new Vector3(0.12f, 1.15f, 0.12f), trim);
            Part("BackRight", chair.transform, new Vector3(0.35f, 1.42f, 0.37f), new Vector3(0.12f, 1.15f, 0.12f), trim);
            Part("BackPanel", chair.transform, new Vector3(0f, 1.62f, 0.37f), new Vector3(0.78f, 0.42f, 0.1f), wood);
            SaveGeneratedPrefab(chair, "WoodenChair");

            GameObject table = Child(parent, "SmallSideTable");
            table.transform.localPosition = new Vector3(3.8f, 0f, 3.65f);
            Part("Top", table.transform, new Vector3(0f, 0.82f, 0f), new Vector3(1.25f, 0.16f, 1.0f), wood);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                Part($"Leg_{x}_{z}", table.transform, new Vector3(x * 0.48f, 0.4f, z * 0.35f), new Vector3(0.12f, 0.8f, 0.12f), trim);
            Part("MetalCup", table.transform, new Vector3(0.25f, 1.02f, 0f), new Vector3(0.16f, 0.22f, 0.16f), metal, PrimitiveType.Cylinder);
            SaveGeneratedPrefab(table, "SmallSideTable");

            GameObject shelves = Child(parent, "DecorativeShelves");
            shelves.transform.localPosition = new Vector3(-4.8f, 1.75f, -4.76f);
            Part("Back", shelves.transform, Vector3.zero, new Vector3(1.75f, 1.35f, 0.10f), trim);
            for (int i = 0; i < 3; i++) Part($"Shelf_{i + 1}", shelves.transform, new Vector3(0f, -0.55f + i * 0.55f, 0.18f), new Vector3(1.85f, 0.10f, 0.42f), wood);
            SaveGeneratedPrefab(shelves, "DecorativeShelf");
        }

        private static void DressGameplayFurniture(Scene scene, Material wood, Material trim, Material metal, Material brass)
        {
            GameObject hanoi = Find(scene, "HanoiPuzzle");
            if (hanoi != null)
            {
                ApplyMaterial(Find(scene, "BookshelfBody"), wood);
                GameObject visual = Child(hanoi.transform, "Visual");
                GameObject table = Child(visual.transform, "HanoiTableArt");
                Part("TableTop", table.transform, new Vector3(-0.48f, 0.02f, 0f), new Vector3(0.75f, 0.14f, 2.75f), wood);
                for (int z = -1; z <= 1; z += 2) Part($"Leg_{z}", table.transform, new Vector3(-0.48f, -0.32f, z * 1.05f), new Vector3(0.32f, 0.68f, 0.28f), trim);
                Part("FrontBrace", table.transform, new Vector3(-0.48f, -0.28f, 0f), new Vector3(0.22f, 0.2f, 2.15f), trim);
                GameObject shelf = Child(visual.transform, "BookshelfArt");
                Part("LeftFrame", shelf.transform, new Vector3(-0.32f, 1.2f, -1.42f), new Vector3(0.16f, 2.45f, 0.16f), trim);
                Part("RightFrame", shelf.transform, new Vector3(-0.32f, 1.2f, 1.42f), new Vector3(0.16f, 2.45f, 0.16f), trim);
                Part("TopFrame", shelf.transform, new Vector3(-0.32f, 2.38f, 0f), new Vector3(0.16f, 0.14f, 2.95f), trim);
                Part("ShelfUpper", shelf.transform, new Vector3(-0.32f, 1.58f, 0f), new Vector3(0.18f, 0.10f, 2.82f), wood);
            }

            GameObject safe = Find(scene, "Safe");
            if (safe != null)
            {
                ApplyMaterial(Find(scene, "SafeBody"), metal);
                ApplyMaterial(Find(scene, "SafeDoorVisual"), metal);
                GameObject visual = Child(safe.transform, "Visual");
                GameObject stand = Child(visual.transform, "SafeTableArt");
                Part("Top", stand.transform, new Vector3(0f, -0.02f, 0f), new Vector3(1.55f, 0.14f, 1.15f), wood);
                Part("Plinth", stand.transform, new Vector3(0f, -0.16f, 0f), new Vector3(1.35f, 0.22f, 0.95f), trim);
                Part("Dial", visual.transform, new Vector3(0.61f, 0.65f, 0f), new Vector3(0.16f, 0.34f, 0.34f), brass, PrimitiveType.Cylinder, new Vector3(0f, 0f, 90f));
            }

            GameObject cabinet = Find(scene, "Cabinet");
            if (cabinet != null)
            {
                string[] woodenParts = { "Back", "Top", "Bottom", "Side_Left", "Side_Right", "CabinetDoor" };
                foreach (string partName in woodenParts) ApplyMaterial(FindChild(cabinet.transform, partName), wood);
                GameObject visual = Child(cabinet.transform, "Visual");
                Part("Crown", visual.transform, new Vector3(0f, 0.96f, 0f), new Vector3(0.78f, 0.14f, 2.02f), trim);
                Part("Base", visual.transform, new Vector3(0f, -0.96f, 0f), new Vector3(0.78f, 0.14f, 2.02f), trim);
                Part("Handle", visual.transform, new Vector3(0.44f, 0f, 0.48f), new Vector3(0.08f, 0.22f, 0.09f), brass, PrimitiveType.Cylinder);
            }
        }

        private static GameObject Child(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null) return found.gameObject;
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject Part(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, PrimitiveType type = PrimitiveType.Cube, Vector3? localEuler = null)
        {
            Transform existing = parent.Find(name);
            GameObject part = existing != null ? existing.gameObject : GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEuler ?? Vector3.zero);
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return part;
        }

        private static void ClearGeneratedChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
                Object.DestroyImmediate(parent.GetChild(index).gameObject);
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            if (target == null) return;
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static Material MaterialAsset(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SaveGeneratedPrefab(GameObject instance, string prefabName)
        {
            string path = $"{PrefabFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                PrefabUtility.SaveAsPrefabAsset(instance, path);
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

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
