#if UNITY_EDITOR
using PuzzleRoom.Core;
using PuzzleRoom.Puzzles.PowerGrid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class EscapeRoomMaterialLightingBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MaterialFolder = "Assets/Art/Materials";

        static EscapeRoomMaterialLightingBuilder() => EditorApplication.delayCall += AutoBuildOnce;

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool built = Find(scene, "ART_MaterialsLighting_v3") != null;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (!built) BuildMaterialsAndLighting(false);
        }

        [MenuItem("Tools/Puzzle Room/Art/Finalize Materials + Lighting")]
        public static void BuildFromMenu() => BuildMaterialsAndLighting(true);

        public static void BuildMaterialsAndLighting(bool selectMarker)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before finalizing materials and lighting.");
                return;
            }

            EnsureFolder(MaterialFolder);
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Material floor = StandardMaterial("MAT_DarkWoodFloor", new Color(0.13f, 0.060f, 0.026f), 0.02f, 0.18f);
            Material wall = StandardMaterial("MAT_MutedGreenWall", new Color(0.19f, 0.205f, 0.17f), 0f, 0.10f);
            Material ceiling = StandardMaterial("MAT_DarkCeiling", new Color(0.075f, 0.078f, 0.075f), 0f, 0.08f);
            StandardMaterial("MAT_DarkOak", new Color(0.16f, 0.072f, 0.031f), 0.02f, 0.17f);
            StandardMaterial("MAT_DarkWoodTrim", new Color(0.065f, 0.025f, 0.012f), 0.03f, 0.14f);
            StandardMaterial("MAT_InteractiveDarkWood", new Color(0.15f, 0.058f, 0.024f), 0.02f, 0.18f);
            StandardMaterial("MAT_InteractiveWoodTrim", new Color(0.055f, 0.020f, 0.010f), 0.03f, 0.13f);
            StandardMaterial("MAT_WornSteel", new Color(0.13f, 0.145f, 0.15f), 0.68f, 0.22f);
            StandardMaterial("MAT_BlackenedSteel", new Color(0.045f, 0.052f, 0.055f), 0.72f, 0.18f);
            StandardMaterial("MAT_IndustrialMetal", new Color(0.10f, 0.11f, 0.115f), 0.68f, 0.23f);
            StandardMaterial("MAT_AntiqueBrass", new Color(0.43f, 0.26f, 0.07f), 0.70f, 0.28f);
            StandardMaterial("MAT_AgedBrass", new Color(0.34f, 0.215f, 0.065f), 0.66f, 0.25f);
            StandardMaterial("MAT_CopperContact", new Color(0.52f, 0.16f, 0.045f), 0.74f, 0.28f);
            StandardMaterial("MAT_PaintingFrame", new Color(0.095f, 0.037f, 0.014f), 0.03f, 0.20f);
            StandardMaterial("MAT_SymbolButtonBody", new Color(0.085f, 0.095f, 0.10f), 0.64f, 0.23f);

            Material glass = StandardMaterial("MAT_FuseGlass", new Color(0.25f, 0.47f, 0.50f, 0.58f), 0.10f, 0.72f);
            ConfigureTransparent(glass);

            Material uv = StandardMaterial("MAT_UVSymbol", new Color(0.42f, 0.055f, 0.82f), 0.03f, 0.42f);
            ConfigureEmission(uv, new Color(0.18f, 0.015f, 0.42f));
            Material uvElement = StandardMaterial("MAT_UVElement", new Color(0.26f, 0.035f, 0.66f), 0.04f, 0.48f);
            ConfigureEmission(uvElement, new Color(0.075f, 0.008f, 0.20f));

            string[] bookMaterials =
            {
                "MAT_BookLeather_Red", "MAT_BookLeather_Blue",
                "MAT_BookLeather_Green", "MAT_BookLeather_Brown"
            };
            Color[] bookColors =
            {
                new Color(0.27f, 0.045f, 0.035f), new Color(0.035f, 0.09f, 0.22f),
                new Color(0.035f, 0.17f, 0.085f), new Color(0.19f, 0.075f, 0.025f)
            };
            for (int i = 0; i < bookMaterials.Length; i++) StandardMaterial(bookMaterials[i], bookColors[i], 0f, 0.20f);
            StandardMaterial("MAT_OldBookPages", new Color(0.58f, 0.48f, 0.31f), 0f, 0.08f);

            Material circuitUI = UIMaterial("MAT_CircuitTileUI");
            Apply(Find(scene, "Floor"), floor);
            Apply(Find(scene, "Ceiling"), ceiling);
            string[] walls = { "Wall_North", "Wall_East", "Wall_West", "Wall_South_Left", "Wall_South_Right", "Wall_South_AboveExit" };
            foreach (string wallName in walls) Apply(Find(scene, wallName), wall);
            ConfigureCircuitUI(scene, circuitUI);
            ConfigureExitIndicator(scene);
            ConfigureLighting(scene);

            GameObject marker = Find(scene, "ART_MaterialsLighting_v3");
            if (marker == null)
            {
                marker = new GameObject("ART_MaterialsLighting_v3");
                SceneManager.MoveGameObjectToScene(marker, scene);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (selectMarker) Selection.activeGameObject = marker;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Materials + Lighting finalized using the existing PowerState and RoomPowerController.");
        }

        private static void ConfigureLighting(Scene scene)
        {
            GameObject lightingRoot = Find(scene, "Lighting");
            if (lightingRoot == null)
            {
                lightingRoot = new GameObject("Lighting");
                SceneManager.MoveGameObjectToScene(lightingRoot, scene);
            }
            GameObject art = Child(lightingRoot.transform, "ART_WarmRoomLighting");
            Light main = Find(scene, "Room Light")?.GetComponent<Light>();
            if (main != null)
            {
                main.type = LightType.Point;
                main.range = 13f;
                main.shadows = LightShadows.Soft;
                main.shadowStrength = 0.72f;
            }
            Light west = LightChild(art.transform, "WarmFill_West", new Vector3(-3.8f, 2.35f, 0.2f), 6f);
            Light east = LightChild(art.transform, "WarmFill_East", new Vector3(3.8f, 2.35f, -0.4f), 6f);

            RoomPowerController controller = Find(scene, "PowerSystem")?.GetComponent<RoomPowerController>();
            if (controller != null)
            {
                SerializedObject serialized = new SerializedObject(controller);
                SerializedProperty lights = serialized.FindProperty("roomLights");
                lights.arraySize = 3;
                lights.GetArrayElementAtIndex(0).objectReferenceValue = main;
                lights.GetArrayElementAtIndex(1).objectReferenceValue = west;
                lights.GetArrayElementAtIndex(2).objectReferenceValue = east;
                serialized.FindProperty("unpoweredIntensity").floatValue = 1.65f;
                serialized.FindProperty("poweredIntensity").floatValue = 2.8f;
                serialized.FindProperty("unpoweredLightColor").colorValue = new Color(1f, 0.78f, 0.48f);
                serialized.FindProperty("poweredLightColor").colorValue = new Color(1f, 0.88f, 0.68f);
                serialized.FindProperty("controlAmbientLight").boolValue = true;
                serialized.FindProperty("unpoweredAmbientColor").colorValue = new Color(0.38f, 0.30f, 0.20f);
                serialized.FindProperty("poweredAmbientColor").colorValue = new Color(0.62f, 0.52f, 0.38f);
                GameObject bulb = Find(scene, "WarmBulb");
                SerializedProperty emissiveRenderers = serialized.FindProperty("poweredEmissiveRenderers");
                emissiveRenderers.arraySize = bulb != null && bulb.GetComponent<Renderer>() != null ? 1 : 0;
                if (emissiveRenderers.arraySize == 1)
                    emissiveRenderers.GetArrayElementAtIndex(0).objectReferenceValue = bulb.GetComponent<Renderer>();
                serialized.FindProperty("unpoweredEmissionColor").colorValue = new Color(0.18f, 0.09f, 0.015f);
                serialized.FindProperty("poweredEmissionColor").colorValue = new Color(1.5f, 0.72f, 0.12f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.30f, 0.20f);
            RenderSettings.reflectionIntensity = 0.75f;
        }

        private static Light LightChild(Transform parent, string name, Vector3 position, float range)
        {
            GameObject go = Child(parent, name);
            go.transform.position = position;
            Light light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = 1.65f;
            light.color = new Color(1f, 0.78f, 0.48f);
            light.shadows = LightShadows.None;
            return light;
        }

        private static void ConfigureCircuitUI(Scene scene, Material uiMaterial)
        {
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    GameObject tileObject = Find(scene, $"Tile_R{row}C{col}");
                    CircuitTile tile = tileObject != null ? tileObject.GetComponent<CircuitTile>() : null;
                    Image image = tileObject != null ? tileObject.GetComponent<Image>() : null;
                    if (tile == null || image == null) continue;
                    image.material = uiMaterial;
                    SerializedObject serialized = new SerializedObject(tile);
                    serialized.FindProperty("unpoweredColor").colorValue = new Color(0.055f, 0.07f, 0.08f, 1f);
                    serialized.FindProperty("poweredColor").colorValue = new Color(0.95f, 0.48f, 0.07f, 1f);
                    serialized.FindProperty("unpoweredWireColor").colorValue = new Color(0.32f, 0.45f, 0.48f, 1f);
                    serialized.FindProperty("poweredWireColor").colorValue = new Color(1f, 0.70f, 0.12f, 1f);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ConfigureExitIndicator(Scene scene)
        {
            GameObject indicator = Find(scene, "ExitPowerIndicator");
            Renderer renderer = indicator != null ? indicator.GetComponent<Renderer>() : null;
            if (renderer == null || renderer.sharedMaterial == null) return;
            Material material = renderer.sharedMaterial;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.04f, 0.32f, 0.08f));
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureTransparent(Material material)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        private static void ConfigureEmission(Material material, Color color)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
            EditorUtility.SetDirty(material);
        }

        private static Material StandardMaterial(string name, Color color, float metallic, float smoothness)
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

        private static Material UIMaterial(string name)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("UI/Default")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        private static GameObject Child(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null) return found.gameObject;
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void Apply(GameObject target, Material material)
        {
            Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
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
