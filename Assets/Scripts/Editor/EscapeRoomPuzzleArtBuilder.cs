#if UNITY_EDITOR
using System.Collections.Generic;
using PuzzleRoom.Interaction;
using PuzzleRoom.Puzzles.Hanoi;
using PuzzleRoom.Puzzles.PowerGrid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class EscapeRoomPuzzleArtBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MaterialFolder = "Assets/Art/Materials";
        private const string PrefabFolder = "Assets/Art/Prefabs";

        static EscapeRoomPuzzleArtBuilder() => EditorApplication.delayCall += AutoBuildOnce;

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool built = Find(scene, "ART_PuzzleVisuals_v9") != null;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (!built) BuildPuzzleVisuals(false);
        }

        [MenuItem("Tools/Puzzle Room/Art/Build Puzzle Visuals")]
        public static void BuildFromMenu() => BuildPuzzleVisuals(true);

        public static void BuildPuzzleVisuals(bool selectMarker)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before building puzzle visuals.");
                return;
            }

            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            Material frame = MaterialAsset("MAT_PaintingFrame", new Color(0.12f, 0.052f, 0.022f), 0.02f, 0.22f);
            Material canvasA = MaterialAsset("MAT_Painting_Ochre", new Color(0.38f, 0.22f, 0.10f), 0f, 0.18f);
            Material canvasB = MaterialAsset("MAT_Painting_Forest", new Color(0.10f, 0.25f, 0.18f), 0f, 0.18f);
            Material canvasC = MaterialAsset("MAT_Painting_Night", new Color(0.10f, 0.13f, 0.28f), 0f, 0.18f);
            Material gold = MaterialAsset("MAT_PuzzleGold", new Color(0.62f, 0.38f, 0.09f), 0.58f, 0.32f);
            Material uv = MaterialAsset("MAT_UVSymbol", new Color(0.58f, 0.10f, 1f), 0.05f, 0.5f);
            ConfigureEmission(uv, new Color(0.85f, 0.12f, 2.2f));
            Material button = MaterialAsset("MAT_SymbolButtonBody", new Color(0.12f, 0.14f, 0.15f), 0.7f, 0.28f);
            Material pages = MaterialAsset("MAT_OldBookPages", new Color(0.67f, 0.57f, 0.39f), 0f, 0.12f);
            Material redBook = MaterialAsset("MAT_BookLeather_Red", new Color(0.36f, 0.07f, 0.055f), 0f, 0.24f);
            Material blueBook = MaterialAsset("MAT_BookLeather_Blue", new Color(0.055f, 0.13f, 0.30f), 0f, 0.24f);
            Material greenBook = MaterialAsset("MAT_BookLeather_Green", new Color(0.06f, 0.24f, 0.13f), 0f, 0.24f);
            Material brownBook = MaterialAsset("MAT_BookLeather_Brown", new Color(0.25f, 0.11f, 0.045f), 0f, 0.24f);
            Material board = MaterialAsset("MAT_ProcedureBoard", new Color(0.08f, 0.09f, 0.085f), 0.45f, 0.2f);
            Material treeTrunk = MaterialAsset("MAT_PaintingTreeTrunk", new Color(0.22f, 0.09f, 0.025f), 0f, 0.15f);
            Material foliage = MaterialAsset("MAT_PaintingFoliage", new Color(0.08f, 0.30f, 0.12f), 0f, 0.18f);
            Material water = MaterialAsset("MAT_PaintingWater", new Color(0.06f, 0.28f, 0.44f), 0f, 0.30f);
            Material mountain = MaterialAsset("MAT_PaintingMountain", new Color(0.28f, 0.27f, 0.24f), 0f, 0.12f);

            BuildPaintings(scene, frame, canvasA, canvasB, canvasC, gold, treeTrunk, foliage, water, mountain);
            BuildSymbolButtons(scene, button, gold);
            BuildUVSymbols(scene, uv);
            BuildHanoiBooks(scene, pages, redBook, blueBook, greenBook, brownBook, gold);
            PolishFinalPowerUI(scene);
            BuildActivationClue(scene, board, gold);

            GameObject marker = Find(scene, "ART_PuzzleVisuals_v9");
            if (marker == null)
            {
                marker = new GameObject("ART_PuzzleVisuals_v9");
                SceneManager.MoveGameObjectToScene(marker, scene);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (selectMarker) Selection.activeGameObject = marker;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Puzzle visual art pass built without changing puzzle mechanics or solutions.");
        }

        private static void BuildPaintings(
            Scene scene,
            Material frame,
            Material a,
            Material b,
            Material c,
            Material gold,
            Material treeTrunk,
            Material foliage,
            Material water,
            Material mountain)
        {
            string[] names = { "Painting_Left", "Painting_Center", "Painting_Right" };
            int[] treeCounts = { 5, 2, 7 };
            Material[] surfaces = { a, b, c };
            for (int index = 0; index < names.Length; index++)
            {
                GameObject painting = Find(scene, names[index]);
                if (painting == null) continue;
                // The number of trees is now a purely visual clue. Do not show an
                // interaction prompt or explanatory message when aiming at it.
                PaintingClue oldClue = painting.GetComponent<PaintingClue>();
                if (oldClue != null) Object.DestroyImmediate(oldClue);
                Renderer baseRenderer = painting.GetComponent<Renderer>();
                if (baseRenderer != null) baseRenderer.sharedMaterial = surfaces[index];
                GameObject art = ResetGroup(painting.transform, "PuzzleArt_Frame");
                // Painting placeholders are scaled cubes. Their visible face is at
                // local Z = -0.5 (toward the room), so place all decorative geometry
                // beyond that face instead of leaving it embedded inside the cube.
                art.transform.localPosition = new Vector3(0f, 0f, -0.58f);
                Part("Top", art.transform, new Vector3(0f, 0.56f, -0.04f), new Vector3(1.18f, 0.10f, 0.10f), frame);
                Part("Bottom", art.transform, new Vector3(0f, -0.56f, -0.04f), new Vector3(1.18f, 0.10f, 0.10f), frame);
                Part("Left", art.transform, new Vector3(-0.55f, 0f, -0.04f), new Vector3(0.10f, 1.04f, 0.10f), frame);
                Part("Right", art.transform, new Vector3(0.55f, 0f, -0.04f), new Vector3(0.10f, 1.04f, 0.10f), frame);
                BuildLandscape(
                    art.transform,
                    treeCounts[index],
                    index,
                    gold,
                    treeTrunk,
                    foliage,
                    water,
                    mountain);
                if (index == 0) SaveGeneratedPrefab(art, "FramedPainting");
            }
        }

        private static void BuildLandscape(
            Transform parent,
            int treeCount,
            int variation,
            Material sun,
            Material trunk,
            Material leaves,
            Material water,
            Material mountain)
        {
            // Background silhouettes add scenery without introducing extra tree-like
            // objects that could make the counting clue ambiguous.
            GameObject leftMountain = Part("Mountain_Left", parent, new Vector3(-0.22f, 0.13f, -0.095f), new Vector3(0.34f, 0.34f, 0.035f), mountain);
            leftMountain.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject rightMountain = Part("Mountain_Right", parent, new Vector3(0.14f, 0.09f, -0.094f), new Vector3(0.28f, 0.28f, 0.035f), mountain);
            rightMountain.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Part("Sun", parent, new Vector3(0.34f - variation * 0.05f, 0.36f, -0.115f), Vector3.one * 0.105f, sun, PrimitiveType.Sphere);
            Part("River", parent, new Vector3(0f, -0.39f, -0.11f), new Vector3(0.86f, 0.10f, 0.035f), water);

            float spacing = treeCount > 1 ? 0.78f / (treeCount - 1) : 0f;
            for (int index = 0; index < treeCount; index++)
            {
                float x = treeCount > 1 ? -0.39f + index * spacing : 0f;
                float heightVariation = 0.02f * ((index + variation) % 3);
                GameObject tree = new GameObject($"Tree_{index + 1:00}");
                tree.transform.SetParent(parent, false);
                tree.transform.localPosition = new Vector3(x, -0.14f, 0f);
                Part("Trunk", tree.transform, new Vector3(0f, -0.08f, -0.125f), new Vector3(0.035f, 0.23f + heightVariation, 0.028f), trunk);
                Part("Canopy", tree.transform, new Vector3(0f, 0.09f + heightVariation, -0.13f), new Vector3(0.13f, 0.17f, 0.045f), leaves, PrimitiveType.Sphere);
            }
        }

        private static void BuildUVSymbols(Scene scene, Material material)
        {
            GameObject clueRoot = Find(scene, "UVClue_HiddenSymbols");
            if (clueRoot != null)
            {
                // Empty strip of east wall to the viewer's left of the bookshelf.
                // The Hanoi bookshelf spans roughly Z 0..3; this uses Z 3.75..4.9.
                // East wall inner face is around X 6.0. X 5.72 leaves enough
                // clearance for the small meshes so they cannot clip into the wall.
                clueRoot.transform.position = new Vector3(5.72f, 1.35f, 3.85f);
                clueRoot.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            string[] names = { "Moon", "Triangle", "Diamond", "Sun" };
            string[] buttonNames = { "MoonButton", "TriangleButton", "DiamondButton", "SunButton" };
            float[] compactX = { -0.48f, -0.16f, 0.16f, 0.48f };
            for (int symbolIndex = 0; symbolIndex < names.Length; symbolIndex++)
            {
                string name = names[symbolIndex];
                GameObject symbol = Find(scene, name);
                if (symbol == null || symbol.GetComponent<UVRevealObject>() == null) continue;
                symbol.transform.localPosition = new Vector3(compactX[symbolIndex], 0f, 0f);
                TextMesh oldLabel = symbol.GetComponent<TextMesh>();
                if (oldLabel != null) oldLabel.text = string.Empty;
                Renderer oldLabelRenderer = symbol.GetComponent<Renderer>();
                if (oldLabelRenderer != null) oldLabelRenderer.enabled = false;

                Transform oldArt = symbol.transform.Find("PuzzleArt_Symbol");
                if (oldArt != null) Object.DestroyImmediate(oldArt.gameObject);

                // Clone the actual physical button glyph instead of rebuilding a
                // second approximation. Rotating YZ to XY preserves every edge,
                // ray, proportion, and material exactly.
                GameObject sourceButton = Find(scene, buttonNames[symbolIndex]);
                Transform sourceArt = sourceButton != null
                    ? sourceButton.transform.Find("PuzzleArt_ButtonSymbol")
                    : null;
                GameObject art;
                if (sourceArt != null)
                {
                    art = Object.Instantiate(sourceArt.gameObject, symbol.transform);
                    art.name = "PuzzleArt_Symbol";
                    // Button glyphs are centered at local X -0.56. After a 0.42
                    // group scale and a 90-degree turn, that becomes a +0.235 Z
                    // offset. Compensate by -0.30, leaving the mesh about 0.065 m
                    // toward the room instead of buried behind the east wall.
                    art.transform.localPosition = new Vector3(0f, 0f, -0.30f);
                    art.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    art.transform.localScale = Vector3.one * 0.42f;
                    foreach (Renderer renderer in art.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterial = material;
                }
                else
                {
                    art = ResetGroup(symbol.transform, "PuzzleArt_Symbol");
                }

                var renderers = new List<Renderer>();
                renderers.AddRange(art.GetComponentsInChildren<Renderer>(true));
                SerializedObject reveal = new SerializedObject(symbol.GetComponent<UVRevealObject>());
                SerializedProperty refs = reveal.FindProperty("revealRenderers");
                refs.arraySize = renderers.Count;
                for (int i = 0; i < renderers.Count; i++) refs.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                reveal.ApplyModifiedPropertiesWithoutUndo();
                foreach (Renderer renderer in renderers) renderer.enabled = false;
            }
        }

        private static void BuildSymbolButtons(Scene scene, Material body, Material glyph)
        {
            string[] names = { "DiamondButton", "SunButton", "TriangleButton", "MoonButton" };
            foreach (string name in names)
            {
                GameObject button = Find(scene, name);
                if (button == null) continue;
                Apply(button, body);
                Transform oldLabel = button.transform.Find("Label");
                if (oldLabel != null) oldLabel.gameObject.SetActive(false);
                GameObject art = ResetGroup(button.transform, "PuzzleArt_ButtonSymbol");
                string symbolName = name.Replace("Button", string.Empty);
                if (symbolName == "Moon") BuildMoon(art.transform, glyph, new Vector3(-0.56f, 0f, 0f), 0.22f, true);
                else if (symbolName == "Triangle") BuildLoopOnYZ(art.transform, glyph, 3, new Vector3(-0.56f, 0f, 0f), 0.22f, 0f);
                else if (symbolName == "Diamond") BuildLoopOnYZ(art.transform, glyph, 4, new Vector3(-0.56f, 0f, 0f), 0.20f, 45f);
                else BuildSunOnYZ(art.transform, glyph, new Vector3(-0.56f, 0f, 0f), 0.18f);
                if (name == "DiamondButton") SaveGeneratedPrefab(art, "SymbolButtonVisual");
            }
        }

        private static void BuildHanoiBooks(Scene scene, Material pages, Material small, Material medium, Material large, Material extraLarge, Material gold)
        {
            string[] names = { "Book_Small", "Book_Medium", "Book_Large", "Book_ExtraLarge" };
            Material[] covers = { small, medium, large, extraLarge };
            for (int index = 0; index < names.Length; index++)
            {
                GameObject book = Find(scene, names[index]);
                if (book == null || book.GetComponent<HanoiBook>() == null) continue;
                Renderer oldRenderer = book.GetComponent<Renderer>();
                if (oldRenderer != null) oldRenderer.enabled = false;
                GameObject art = ResetGroup(book.transform, "PuzzleArt_Hardcover");
                Part("Pages", art.transform, Vector3.zero, new Vector3(0.92f, 0.68f, 0.94f), pages);
                Part("TopCover", art.transform, new Vector3(0f, 0.40f, 0f), new Vector3(1.04f, 0.12f, 1.04f), covers[index]);
                Part("BottomCover", art.transform, new Vector3(0f, -0.40f, 0f), new Vector3(1.04f, 0.12f, 1.04f), covers[index]);
                Part("Spine", art.transform, new Vector3(-0.50f, 0f, 0f), new Vector3(0.10f, 0.86f, 1.04f), covers[index]);
                Part("SizeBand", art.transform, new Vector3(-0.56f, 0f, 0f), new Vector3(0.025f, 0.18f + index * 0.08f, 0.82f), gold);
                if (index == 0) SaveGeneratedPrefab(art, "HanoiBookVisual");
            }

            string[] pegs = { "LeftPeg", "MiddlePeg", "RightPeg" };
            foreach (string pegName in pegs)
            {
                GameObject peg = Find(scene, pegName);
                GameObject visual = FindChild(peg, "PegVisual");
                if (visual != null) Apply(visual, gold);
            }
        }

        private static void PolishFinalPowerUI(Scene scene)
        {
            Color tileColor = new Color(0.075f, 0.095f, 0.11f, 1f);
            Color wireOff = new Color(0.35f, 0.48f, 0.52f, 1f);
            Color wireOn = new Color(1f, 0.66f, 0.12f, 1f);
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    GameObject tileObject = Find(scene, $"Tile_R{row}C{col}");
                    CircuitTile tile = tileObject != null ? tileObject.GetComponent<CircuitTile>() : null;
                    if (tile == null) continue;
                    Image image = tileObject.GetComponent<Image>();
                    if (image != null) image.color = tileColor;
                    Outline outline = tileObject.GetComponent<Outline>();
                    if (outline == null) outline = tileObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.26f, 0.32f, 0.34f, 1f);
                    outline.effectDistance = new Vector2(2f, -2f);
                    SerializedObject serialized = new SerializedObject(tile);
                    serialized.FindProperty("unpoweredColor").colorValue = tileColor;
                    serialized.FindProperty("poweredColor").colorValue = new Color(0.9f, 0.46f, 0.06f, 1f);
                    serialized.FindProperty("unpoweredWireColor").colorValue = wireOff;
                    serialized.FindProperty("poweredWireColor").colorValue = wireOn;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            StyleStatus(Find(scene, "LightOutput"));
            StyleStatus(Find(scene, "ControlOutput"));
            StyleStatus(Find(scene, "ExitOutput"));
            StyleButton(Find(scene, "ActivateControl"));
            StyleButton(Find(scene, "ActivateLight"));
            StyleButton(Find(scene, "ActivateExit"));
            GameObject panel = Find(scene, "FinalPowerPanel");
            if (panel != null && panel.GetComponent<Image>() != null) panel.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.04f, 0.985f);
        }

        private static void StyleStatus(GameObject target)
        {
            if (target == null) return;
            Text text = target.GetComponent<Text>();
            if (text != null) text.fontSize = 21;
            Outline outline = target.GetComponent<Outline>();
            if (outline == null) outline = target.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void StyleButton(GameObject target)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>();
            if (image != null) image.color = new Color(0.16f, 0.18f, 0.18f, 1f);
            Outline outline = target.GetComponent<Outline>();
            if (outline == null) outline = target.AddComponent<Outline>();
            outline.effectColor = new Color(0.65f, 0.40f, 0.10f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void BuildActivationClue(Scene scene, Material board, Material trim)
        {
            GameObject clue = Find(scene, "EmergencyStartupProcedure");
            if (clue == null) return;
            TextMesh text = clue.GetComponent<TextMesh>();
            if (text != null)
            {
                text.text = "EMERGENCY STARTUP PROCEDURE\nI. CONTROL SYSTEM\nII. LIGHTING SYSTEM\nIII. EXIT SYSTEM";
                text.color = new Color(0.82f, 0.78f, 0.62f);
                text.fontSize = 44;
            }
            GameObject art = ResetGroup(clue.transform, "PuzzleArt_ProcedureBoard");
            Part("Board", art.transform, new Vector3(0f, 0f, 0.035f), new Vector3(3.6f, 1.75f, 0.08f), board);
            Part("TopTrim", art.transform, new Vector3(0f, 0.88f, 0f), new Vector3(3.75f, 0.08f, 0.10f), trim);
            Part("BottomTrim", art.transform, new Vector3(0f, -0.88f, 0f), new Vector3(3.75f, 0.08f, 0.10f), trim);
        }

        private static void BuildMoon(Transform parent, Material material, Vector3 center, float radius, bool yz = false)
        {
            for (int i = 0; i < 7; i++)
            {
                float angle = Mathf.Lerp(-120f, 120f, i / 6f) * Mathf.Deg2Rad;
                Vector3 position = yz
                    ? center + new Vector3(0f, Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius)
                    : center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.025f);
                Part($"Arc_{i}", parent, position, Vector3.one * radius * 0.30f, material, PrimitiveType.Sphere);
            }
        }

        private static void BuildLoop(Transform parent, Material material, int sides, float radius, float offsetDegrees)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = (offsetDegrees + 360f * i / sides) * Mathf.Deg2Rad;
                float b = (offsetDegrees + 360f * (i + 1) / sides) * Mathf.Deg2Rad;
                Vector3 p1 = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
                Vector3 p2 = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0f) * radius;
                BarBetween($"Edge_{i}", parent, p1, p2, material, false);
            }
        }

        private static void BuildLoopOnYZ(Transform parent, Material material, int sides, Vector3 center, float radius, float offsetDegrees)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = (offsetDegrees + 360f * i / sides) * Mathf.Deg2Rad;
                float b = (offsetDegrees + 360f * (i + 1) / sides) * Mathf.Deg2Rad;
                Vector3 p1 = center + new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a)) * radius;
                Vector3 p2 = center + new Vector3(0f, Mathf.Sin(b), Mathf.Cos(b)) * radius;
                BarBetween($"Edge_{i}", parent, p1, p2, material, true);
            }
        }

        private static void BuildSun(Transform parent, Material material, float radius = 0.30f)
        {
            Part("Core", parent, new Vector3(0f, 0f, -0.025f), Vector3.one * radius, material, PrimitiveType.Sphere);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 center = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius * 1.65f;
                GameObject ray = Part($"Ray_{i}", parent, center, new Vector3(radius * 0.24f, radius * 0.75f, 0.035f), material);
                ray.transform.localRotation = Quaternion.Euler(0f, 0f, -i * 45f);
            }
        }

        private static void BuildSunOnYZ(Transform parent, Material material, Vector3 center, float radius)
        {
            Part("Core", parent, center, Vector3.one * radius, material, PrimitiveType.Sphere);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 p = center + new Vector3(0f, Mathf.Sin(angle), Mathf.Cos(angle)) * radius * 1.8f;
                GameObject ray = Part($"Ray_{i}", parent, p, new Vector3(0.04f, 0.06f, 0.16f), material);
                ray.transform.localRotation = Quaternion.Euler(i * 45f, 0f, 0f);
            }
        }

        private static void BarBetween(string name, Transform parent, Vector3 a, Vector3 b, Material material, bool yz)
        {
            Vector3 delta = b - a;
            GameObject bar = Part(name, parent, (a + b) * 0.5f, yz ? new Vector3(0.045f, 0.055f, delta.magnitude) : new Vector3(0.055f, delta.magnitude, 0.045f), material);
            if (yz) bar.transform.localRotation = Quaternion.Euler(Vector3.SignedAngle(Vector3.forward, delta, Vector3.right), 0f, 0f);
            else bar.transform.localRotation = Quaternion.Euler(0f, 0f, -Vector3.SignedAngle(Vector3.up, delta, Vector3.forward));
        }

        private static TextMesh Text(string name, Transform parent, string value, Vector3 position, int size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            TextMesh text = go.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return text;
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

        private static void ConfigureEmission(Material material, Color emissionColor)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
            EditorUtility.SetDirty(material);
        }

        private static void SaveGeneratedPrefab(GameObject instance, string name)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            // These prefabs are owned by this generator, so refreshing them is safe
            // and keeps the asset consistent with the scene visual.
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
