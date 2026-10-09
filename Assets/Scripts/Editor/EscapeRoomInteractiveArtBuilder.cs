#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleRoom.Player;

namespace PuzzleRoom.EditorTools
{
    /// <summary>Adds visual-only low-poly models while preserving gameplay roots and pivots.</summary>
    [InitializeOnLoad]
    public static class EscapeRoomInteractiveArtBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MaterialFolder = "Assets/Art/Materials";
        private const string PrefabFolder = "Assets/Art/Prefabs";

        static EscapeRoomInteractiveArtBuilder()
        {
            EditorApplication.delayCall += AutoBuildOnce;
        }

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool built = Find(scene, "ART_InteractiveObjects_v2") != null;
            if (built)
            {
                EnsureUVVisualState(scene);
                EnsureExitDoorSeal(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (!built) BuildInteractiveObjects(false);
        }

        private static void EnsureUVVisualState(Scene scene)
        {
            GameObject effect = Find(scene, "UVLightEffect");
            GameObject equipped = effect != null ? FindChild(effect, "EquippedFlashlightVisual") : null;
            if (equipped == null) return;

            equipped.SetActive(false);
            UVLightController controller = effect.GetComponentInParent<UVLightController>();
            if (controller == null) return;
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("equippedVisual").objectReferenceValue = equipped;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureExitDoorSeal(Scene scene)
        {
            GameObject exit = Find(scene, "ExitDoor");
            GameObject leaf = FindChild(exit, "DoorVisual");
            if (leaf == null) return;

            // The wall opening is wider than the original 1.4 m leaf. Extend only
            // its width so both edges overlap the jambs when closed. Because the
            // leaf remains a child of the existing hinge root, the door pivot and
            // opening animation are unchanged.
            Vector3 scale = leaf.transform.localScale;
            scale.x = 1.64f;
            leaf.transform.localScale = scale;
        }

        [MenuItem("Tools/Puzzle Room/Art/Build Interactive Objects")]
        public static void BuildFromMenu() => BuildInteractiveObjects(true);

        public static void BuildInteractiveObjects(bool selectMarker)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before running the Interactive Art Pass.");
                return;
            }

            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Material steel = MaterialAsset("MAT_WornSteel", new Color(0.16f, 0.18f, 0.19f), 0.72f, 0.23f);
            Material steelDark = MaterialAsset("MAT_BlackenedSteel", new Color(0.055f, 0.065f, 0.07f), 0.75f, 0.2f);
            Material brass = MaterialAsset("MAT_AntiqueBrass", new Color(0.48f, 0.30f, 0.085f), 0.72f, 0.32f);
            Material copper = MaterialAsset("MAT_CopperContact", new Color(0.58f, 0.20f, 0.07f), 0.78f, 0.3f);
            Material rubber = MaterialAsset("MAT_FlashlightRubber", new Color(0.035f, 0.04f, 0.045f), 0f, 0.18f);
            Material uv = MaterialAsset("MAT_UVElement", new Color(0.30f, 0.045f, 0.85f), 0.08f, 0.55f);
            Material glass = MaterialAsset("MAT_FuseGlass", new Color(0.35f, 0.58f, 0.62f), 0.12f, 0.7f);
            Material wood = MaterialAsset("MAT_InteractiveDarkWood", new Color(0.17f, 0.075f, 0.035f), 0.02f, 0.2f);
            Material woodTrim = MaterialAsset("MAT_InteractiveWoodTrim", new Color(0.075f, 0.03f, 0.018f), 0.03f, 0.16f);
            Material red = MaterialAsset("MAT_LockIndicatorRed", new Color(0.75f, 0.035f, 0.025f), 0.05f, 0.4f);
            Material flashlightYellow = MaterialAsset("MAT_FlashlightYellow", new Color(0.92f, 0.58f, 0.06f), 0.18f, 0.32f);

            BuildSafe(scene, steel, steelDark, brass);
            BuildFlashlights(scene, flashlightYellow, rubber, steel, uv);
            BuildKey(scene, brass, steelDark);
            BuildCabinet(scene, wood, woodTrim, brass);
            BuildFuse(scene, glass, copper, steelDark);
            BuildFuseBox(scene, steel, steelDark, copper, red);
            BuildExitDoor(scene, wood, woodTrim, steel, brass, red);
            EnsureExitDoorSeal(scene);
            BuildSecretCompartment(scene, wood, woodTrim, brass);

            GameObject marker = Find(scene, "ART_InteractiveObjects_v2");
            if (marker == null)
            {
                marker = new GameObject("ART_InteractiveObjects_v2");
                SceneManager.MoveGameObjectToScene(marker, scene);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (selectMarker) Selection.activeGameObject = marker;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Interactive Objects art pass built. Gameplay components, colliders, pivots, and references were preserved.");
        }

        private static void BuildSafe(Scene scene, Material steel, Material dark, Material brass)
        {
            GameObject safe = Find(scene, "Safe");
            GameObject body = FindChild(safe, "SafeBody");
            GameObject pivot = FindChild(safe, "SafeDoorPivot");
            GameObject oldDoor = FindChild(safe, "SafeDoorVisual");
            DisableRenderer(body);
            BoxCollider bodyCollider = body != null ? body.GetComponent<BoxCollider>() : null;
            if (bodyCollider != null)
            {
                // Preserve a back-wall collider without leaving a solid box in front
                // of the reward. The moving door keeps its original collider.
                bodyCollider.center = new Vector3(0f, 0f, 0.43f);
                bodyCollider.size = new Vector3(1f, 1f, 0.12f);
            }
            DisableRenderer(oldDoor);
            if (safe == null || pivot == null) return;

            GameObject bodyArt = ResetGroup(safe.transform, "InteractiveArt_SafeBody");
            Part("BackWall", bodyArt.transform, new Vector3(0f, 0.60f, 0.38f), new Vector3(1.20f, 1.20f, 0.08f), dark);
            Part("TopShell", bodyArt.transform, new Vector3(0f, 1.16f, 0f), new Vector3(1.20f, 0.08f, 0.80f), steel);
            Part("BottomShell", bodyArt.transform, new Vector3(0f, 0.04f, 0f), new Vector3(1.20f, 0.08f, 0.80f), steel);
            Part("LeftShell", bodyArt.transform, new Vector3(-0.56f, 0.60f, 0f), new Vector3(0.08f, 1.12f, 0.80f), steel);
            Part("RightShell", bodyArt.transform, new Vector3(0.56f, 0.60f, 0f), new Vector3(0.08f, 1.12f, 0.80f), steel);
            for (int x = -1; x <= 1; x += 2)
                Part($"Foot_{x}", bodyArt.transform, new Vector3(x * 0.43f, -0.09f, 0f), new Vector3(0.22f, 0.18f, 0.52f), dark);

            GameObject doorArt = ResetGroup(pivot.transform, "InteractiveArt_SafeDoor");
            Part("DoorLeaf", doorArt.transform, new Vector3(0.6f, 0f, 0f), new Vector3(1.18f, 1.16f, 0.10f), steel);
            Part("InnerInset", doorArt.transform, new Vector3(0.6f, 0f, -0.065f), new Vector3(0.92f, 0.88f, 0.035f), dark);
            Part("HingeTop", doorArt.transform, new Vector3(0.04f, 0.38f, 0.08f), new Vector3(0.09f, 0.22f, 0.09f), brass, PrimitiveType.Cylinder);
            Part("HingeBottom", doorArt.transform, new Vector3(0.04f, -0.38f, 0.08f), new Vector3(0.09f, 0.22f, 0.09f), brass, PrimitiveType.Cylinder);
            Part("HandleHub", doorArt.transform, new Vector3(0.75f, 0f, -0.105f), new Vector3(0.18f, 0.08f, 0.18f), brass, PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
            Part("HandleBar", doorArt.transform, new Vector3(0.75f, 0f, -0.18f), new Vector3(0.48f, 0.07f, 0.07f), brass);
            Part("KeypadPlate", doorArt.transform, new Vector3(1.0f, 0.28f, -0.11f), new Vector3(0.22f, 0.34f, 0.035f), dark);
            for (int row = 0; row < 3; row++) for (int col = 0; col < 2; col++)
                Part($"Key_{row}_{col}", doorArt.transform, new Vector3(0.95f + col * 0.09f, 0.37f - row * 0.10f, -0.135f), new Vector3(0.055f, 0.055f, 0.018f), brass);
        }

        private static void BuildFlashlights(Scene scene, Material pickupYellow, Material rubber, Material steel, Material uv)
        {
            GameObject pickup = Find(scene, "UVLightPickup");
            if (pickup != null)
            {
                pickup.transform.localPosition = new Vector3(0f, 0.56f, 0.02f);
                pickup.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
                DisableRenderer(pickup);
                GameObject art = ResetGroup(pickup.transform, "InteractiveArt_UVFlashlight");
                FlashlightModel(art.transform, pickupYellow, steel, uv, Vector3.zero, 1.15f);
                SaveGeneratedPrefab(art, "UVFlashlight");
            }

            GameObject effect = Find(scene, "UVLightEffect");
            if (effect != null)
            {
                GameObject equipped = ResetGroup(effect.transform, "EquippedFlashlightVisual");
                equipped.transform.localPosition = new Vector3(0.28f, -0.20f, 0.42f);
                equipped.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
                FlashlightModel(equipped.transform, rubber, steel, uv, Vector3.zero, 0.32f);
                equipped.SetActive(false);

                UVLightController controller = effect.GetComponentInParent<UVLightController>();
                if (controller != null)
                {
                    SerializedObject serializedController = new SerializedObject(controller);
                    serializedController.FindProperty("equippedVisual").objectReferenceValue = equipped;
                    serializedController.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void FlashlightModel(Transform parent, Material rubber, Material steel, Material uv, Vector3 position, float scale)
        {
            Part("Body", parent, position, new Vector3(0.18f, 0.55f, 0.18f) * scale, rubber, PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
            Part("FrontRing", parent, position + new Vector3(0f, 0f, 0.32f * scale), new Vector3(0.25f, 0.12f, 0.25f) * scale, steel, PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
            Part("Lens", parent, position + new Vector3(0f, 0f, 0.405f * scale), new Vector3(0.19f, 0.035f, 0.19f) * scale, uv, PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
            Part("Grip", parent, position + new Vector3(0f, -0.20f * scale, -0.03f * scale), new Vector3(0.12f, 0.28f, 0.10f) * scale, rubber);
            Part("Switch", parent, position + new Vector3(0f, 0.13f * scale, 0.02f), new Vector3(0.08f, 0.04f, 0.12f) * scale, steel);
        }

        private static void BuildKey(Scene scene, Material brass, Material dark)
        {
            GameObject key = Find(scene, "KeyPickup");
            if (key == null) return;
            if (key.transform.parent != null && key.transform.parent.name == "SecretDrawer")
            {
                key.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            }
            DisableRenderer(key);
            GameObject art = ResetGroup(key.transform, "InteractiveArt_AntiqueKey");
            Part("Shaft", art.transform, Vector3.zero, new Vector3(0.95f, 0.22f, 0.22f), brass);
            Part("Bow", art.transform, new Vector3(-0.58f, 0f, 0f), new Vector3(0.42f, 0.62f, 0.22f), brass, PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
            Part("BowInset", art.transform, new Vector3(-0.60f, 0f, -0.04f), new Vector3(0.18f, 0.28f, 0.24f), dark, PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
            Part("ToothA", art.transform, new Vector3(0.48f, -0.24f, 0f), new Vector3(0.22f, 0.38f, 0.22f), brass);
            Part("ToothB", art.transform, new Vector3(0.72f, -0.15f, 0f), new Vector3(0.18f, 0.20f, 0.22f), brass);
            SaveGeneratedPrefab(art, "AntiqueKey");
        }

        private static void BuildCabinet(Scene scene, Material wood, Material trim, Material brass)
        {
            GameObject cabinet = Find(scene, "Cabinet");
            if (cabinet == null) return;
            string[] parts = { "Back", "Top", "Bottom", "Side_Left", "Side_Right" };
            foreach (string name in parts) Apply(FindChild(cabinet, name), wood);
            GameObject door = FindChild(cabinet, "CabinetDoor");
            DisableRenderer(door);
            if (door == null) return;
            GameObject art = ResetGroup(door.transform, "InteractiveArt_CabinetDoor");
            Part("DoorPanel", art.transform, Vector3.zero, new Vector3(1f, 1f, 1f), wood);
            Part("Inset", art.transform, new Vector3(-0.55f, 0f, 0f), new Vector3(0.08f, 0.72f, 0.72f), trim);
            Part("Handle", art.transform, new Vector3(-0.65f, 0f, 0.32f), new Vector3(0.10f, 0.18f, 0.10f), brass, PrimitiveType.Cylinder);
            Part("HingeTop", art.transform, new Vector3(0f, 0.48f, -0.42f), new Vector3(0.13f, 0.18f, 0.13f), brass, PrimitiveType.Cylinder);
            Part("HingeBottom", art.transform, new Vector3(0f, -0.48f, -0.42f), new Vector3(0.13f, 0.18f, 0.13f), brass, PrimitiveType.Cylinder);
        }

        private static void BuildFuse(Scene scene, Material glass, Material copper, Material dark)
        {
            BuildFuseVisual(Find(scene, "FusePickup"), "InteractiveArt_Fuse", glass, copper, dark, true);
            BuildFuseVisual(Find(scene, "InsertedFuseVisual"), "InteractiveArt_InsertedFuse", glass, copper, dark, false);
        }

        private static void BuildFuseVisual(GameObject root, string groupName, Material glass, Material copper, Material dark, bool savePrefab)
        {
            if (root == null) return;
            DisableRenderer(root);
            GameObject art = ResetGroup(root.transform, groupName);
            Part("GlassBody", art.transform, Vector3.zero, new Vector3(0.8f, 0.72f, 0.8f), glass, PrimitiveType.Cylinder);
            Part("ContactTop", art.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.88f, 0.18f, 0.88f), copper, PrimitiveType.Cylinder);
            Part("ContactBottom", art.transform, new Vector3(0f, -0.46f, 0f), new Vector3(0.88f, 0.18f, 0.88f), copper, PrimitiveType.Cylinder);
            Part("Core", art.transform, Vector3.zero, new Vector3(0.10f, 0.8f, 0.10f), dark);
            if (savePrefab) SaveGeneratedPrefab(art, "ElectricalFuse");
        }

        private static void BuildFuseBox(Scene scene, Material steel, Material dark, Material copper, Material red)
        {
            GameObject fuseBox = Find(scene, "FuseBox");
            if (fuseBox == null) return;
            Apply(FindChild(fuseBox, "FuseBoxBody"), steel);
            GameObject art = ResetGroup(fuseBox.transform, "InteractiveArt_FuseBox");
            Part("HousingFrame", art.transform, new Vector3(-0.02f, 0f, 0f), new Vector3(0.14f, 1.62f, 1.42f), dark);
            Part("CircuitDisplay", art.transform, new Vector3(-0.10f, 0.25f, 0f), new Vector3(0.05f, 0.62f, 0.82f), steel);
            Part("FuseSocket", art.transform, new Vector3(-0.16f, -0.46f, 0f), new Vector3(0.07f, 0.28f, 0.34f), dark);
            Part("ContactLeft", art.transform, new Vector3(-0.20f, -0.46f, -0.13f), new Vector3(0.04f, 0.13f, 0.06f), copper);
            Part("ContactRight", art.transform, new Vector3(-0.20f, -0.46f, 0.13f), new Vector3(0.04f, 0.13f, 0.06f), copper);
            Part("WarningLight", art.transform, new Vector3(-0.20f, 0.67f, 0.5f), new Vector3(0.08f, 0.10f, 0.10f), red, PrimitiveType.Sphere);
            for (int i = 0; i < 4; i++) Part($"PanelBolt_{i}", art.transform, new Vector3(-0.21f, i < 2 ? 0.67f : -0.67f, i % 2 == 0 ? -0.57f : 0.57f), new Vector3(0.05f, 0.07f, 0.05f), copper, PrimitiveType.Cylinder, new Vector3(0f, 0f, 90f));
        }

        private static void BuildExitDoor(Scene scene, Material wood, Material trim, Material steel, Material brass, Material red)
        {
            GameObject exit = Find(scene, "ExitDoor");
            GameObject leaf = FindChild(exit, "DoorVisual");
            if (exit == null || leaf == null) return;
            DisableRenderer(leaf);
            GameObject art = ResetGroup(leaf.transform, "InteractiveArt_ExitDoor");
            Part("WoodLeaf", art.transform, Vector3.zero, Vector3.one, wood);
            Part("UpperPanel", art.transform, new Vector3(0f, 0.24f, -0.56f), new Vector3(0.72f, 0.24f, 0.06f), trim);
            Part("LowerPanel", art.transform, new Vector3(0f, -0.24f, -0.56f), new Vector3(0.72f, 0.24f, 0.06f), trim);
            Part("MetalBandTop", art.transform, new Vector3(0f, 0.34f, -0.62f), new Vector3(0.92f, 0.06f, 0.05f), steel);
            Part("MetalBandBottom", art.transform, new Vector3(0f, -0.34f, -0.62f), new Vector3(0.92f, 0.06f, 0.05f), steel);
            Part("Handle", art.transform, new Vector3(0.32f, 0f, -0.66f), new Vector3(0.07f, 0.18f, 0.07f), brass, PrimitiveType.Cylinder);
            Part("LockPlate", art.transform, new Vector3(0.32f, -0.12f, -0.64f), new Vector3(0.14f, 0.11f, 0.04f), steel);
            Part("LockIndicator", art.transform, new Vector3(0.32f, -0.12f, -0.68f), new Vector3(0.045f, 0.045f, 0.025f), red, PrimitiveType.Sphere);
        }

        private static void BuildSecretCompartment(Scene scene, Material wood, Material trim, Material brass)
        {
            GameObject drawer = Find(scene, "SecretDrawer");
            if (drawer == null) return;
            Apply(drawer, wood);
            GameObject art = ResetGroup(drawer.transform, "InteractiveArt_SecretCompartment");
            Part("FrontPanel", art.transform, new Vector3(-0.55f, 0f, 0f), new Vector3(0.10f, 1.08f, 1.08f), trim);
            Part("Handle", art.transform, new Vector3(-0.64f, 0f, 0f), new Vector3(0.07f, 0.22f, 0.10f), brass, PrimitiveType.Cylinder, new Vector3(0f, 0f, 90f));
        }

        private static GameObject ResetGroup(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group;
        }

        private static GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material, PrimitiveType type = PrimitiveType.Cube, Vector3? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero);
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private static void DisableRenderer(GameObject target)
        {
            if (target == null) return;
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
        }

        private static void Apply(GameObject target, Material material)
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

        private static void SaveGeneratedPrefab(GameObject instance, string name)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, path);
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == name) return child.gameObject;
            return null;
        }

        private static GameObject FindChild(GameObject root, string name)
        {
            if (root == null) return null;
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
