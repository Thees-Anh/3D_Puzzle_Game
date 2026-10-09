#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleRoom.Player;
using PuzzleRoom.Interaction;
using PuzzleRoom.UI;
using PuzzleRoom.Puzzles;
using PuzzleRoom.Puzzles.Hanoi;
using PuzzleRoom.Puzzles.SymbolUnlock;
using PuzzleRoom.Puzzles.PowerGrid;
using PuzzleRoom.Core;
using UnityEditor.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    /// <summary>
    /// Editor-only helper that builds the first room blockout from Unity primitives.
    /// It does not add gameplay behaviour and can be run again from the Tools menu.
    /// </summary>
    [InitializeOnLoad]
    public static class PuzzleRoomPrototypeBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string DoorPrefabPath = "Assets/Prefabs/Door.prefab";

        static PuzzleRoomPrototypeBuilder()
        {
            EditorApplication.delayCall += EnsurePrototypeSetup;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += EnsurePrototypeSetup;
            }
        }

        [MenuItem("Tools/Puzzle Room/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Replace PuzzleRoom scene?",
                    "Assets/Scenes/PuzzleRoom.unity already exists. Replace it with a fresh prototype?",
                    "Replace",
                    "Cancel");

                if (!replace)
                {
                    return;
                }
            }

            BuildAndSaveScene();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        [MenuItem("Tools/Puzzle Room/Setup First Person Player")]
        public static void SetupFirstPersonPlayer()
        {
            AddPlayerToPuzzleRoomIfMissing(true);
        }

        private static void EnsurePrototypeSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                BuildAndSaveScene();
            }

            AddPlayerToPuzzleRoomIfMissing(false);
            // Step 12 is intentionally configured early so unrelated legacy setup
            // helpers cannot prevent the final flow from being installed.
            AddVictoryFlowIfMissing(false);
            EnsureMainMenuAndBuildSettings();
            AddInteractionPrototypeIfMissing(false);
            AddDoorPrototypeIfMissing(false);
            AddPuzzleOnePrototypeIfMissing(false);
            AddUVLightPrototypeIfMissing(false);
            AddUVRevealClueIfMissing(false);
            AddHanoiPuzzleIfMissing(false);
            AddSymbolUnlockPuzzleIfMissing(false);
            AddCabinetAndFuseIfMissing(false);
            AddCircuitPuzzleIfMissing(false);
        }

        [MenuItem("Tools/Puzzle Room/Setup Final Power Puzzle")]
        public static void SetupCircuitPuzzle() => AddCircuitPuzzleIfMissing(true);

        private static void AddCircuitPuzzleIfMissing(bool selectFuseBox)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject fuseBoxRoot = FindObjectInScene(scene, "FuseBox");
            if (fuseBoxRoot == null)
            {
                Debug.LogError("FuseBox from Step 11 was not found.");
                return;
            }

            GameObject existingFinalCanvas = FindRootObject(scene, "FinalPowerCanvas");
            bool hasVersion5 = FindObjectInScene(scene, "FinalGridCustomRouteV5") != null;
            if (existingFinalCanvas != null && fuseBoxRoot.GetComponent<FinalPowerPuzzleManager>() != null && hasVersion5)
            {
                UpgradeFinalGridVisuals(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (selectFuseBox)
                {
                    Selection.activeGameObject = fuseBoxRoot;
                    EditorGUIUtility.PingObject(fuseBoxRoot);
                }
                if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            if (existingFinalCanvas != null)
            {
                Object.DestroyImmediate(existingFinalCanvas);
            }

            GameObject oldCanvas = FindRootObject(scene, "CircuitCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(fuseBoxRoot);

            foreach (MonoBehaviour component in fuseBoxRoot.GetComponents<MonoBehaviour>())
            {
                if (component != null && component.GetType().Name == "CircuitPuzzle")
                {
                    Object.DestroyImmediate(component);
                }
            }

            CircuitGridManager grid = fuseBoxRoot.GetComponent<CircuitGridManager>();
            if (grid == null) grid = fuseBoxRoot.AddComponent<CircuitGridManager>();
            FinalPowerPuzzleManager finalPuzzle = fuseBoxRoot.GetComponent<FinalPowerPuzzleManager>();
            if (finalPuzzle == null) finalPuzzle = fuseBoxRoot.AddComponent<FinalPowerPuzzleManager>();
            AudioSource audioSource = fuseBoxRoot.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = fuseBoxRoot.AddComponent<AudioSource>();

            GameObject canvasRoot = new GameObject("FinalPowerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FinalPowerPuzzleUI));
            SceneManager.MoveGameObjectToScene(canvasRoot, scene);
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            FinalPowerPuzzleUI ui = canvasRoot.GetComponent<FinalPowerPuzzleUI>();

            GameObject panel = new GameObject("FinalPowerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvasRoot.transform, false);
            SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(920f, 880f));
            panel.GetComponent<Image>().color = new Color(0.035f, 0.05f, 0.065f, 0.98f);
            CreateText("Title", panel.transform, "FINAL POWER SYSTEM", new Vector2(0f, 395f), new Vector2(850f, 50f), 30, Color.white);
            Text phaseStatus = CreateText("PhaseStatus", panel.transform, "FIND THE LONG ROUTE THROUGH LIGHT, CONTROL, AND EXIT", new Vector2(0f, 350f), new Vector2(850f, 55f), 20, new Color(1f, 0.82f, 0.25f));
            Text lightStatus = CreateText("LightOutput", panel.transform, "LIGHT: OFF", new Vector2(-280f, 305f), new Vector2(240f, 40f), 20, Color.red);
            Text controlStatus = CreateText("ControlOutput", panel.transform, "CONTROL: OFF", new Vector2(0f, 305f), new Vector2(240f, 40f), 20, Color.red);
            Text exitStatus = CreateText("ExitOutput", panel.transform, "EXIT: OFF", new Vector2(280f, 305f), new Vector2(240f, 40f), 20, Color.red);

            GameObject gridPanel = new GameObject("GridPanel", typeof(RectTransform));
            gridPanel.transform.SetParent(panel.transform, false);
            SetRect(gridPanel.GetComponent<RectTransform>(), new Vector2(0f, 5f), new Vector2(650f, 610f));
            CreateText("PowerSource", gridPanel.transform, "POWER SOURCE", new Vector2(-205f, 280f), new Vector2(230f, 35f), 17, new Color(1f, 0.8f, 0.2f));

            ConnectionDirection straight = ConnectionDirection.Left | ConnectionDirection.Right;
            CircuitTile[] tiles = new CircuitTile[36];
            bool[] requiredRoute = new bool[36];
            for (int row = 0; row < 6; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    int index = row * 6 + column;
                    GetLongRouteTileConfiguration(row, column, out ConnectionDirection connections, out int solvedRotation);
                    requiredRoute[index] = IsRequiredRouteTile(row, column);
                    int initialRotation;
                    if (connections == straight)
                    {
                        initialRotation = index % 2 == 0 ? 1 : 3;
                    }
                    else
                    {
                        initialRotation = (solvedRotation + (index % 2 == 0 ? 1 : 2)) % 4;
                    }
                    tiles[index] = CreateCircuitTile(gridPanel.transform, row, column, connections, initialRotation, solvedRotation);
                }
            }

            Text rotations = CreateText("RotationCounter", gridPanel.transform, "Rotations: 0", new Vector2(-170f, -285f), new Vector2(240f, 40f), 19, Color.white);
            Text poweredCounter = CreateText("PoweredCounter", gridPanel.transform, "Route: 0/20", new Vector2(205f, -285f), new Vector2(190f, 40f), 18, Color.white);
            Button reset = CreateButton("ResetCircuit", gridPanel.transform, "RESET CIRCUIT", new Vector2(30f, -285f), new Vector2(190f, 52f));
            UnityEventTools.AddPersistentListener(reset.onClick, ui.ResetCircuit);
            Button close = CreateButton("Close", panel.transform, "CLOSE", new Vector2(375f, -400f), new Vector2(120f, 48f));
            UnityEventTools.AddPersistentListener(close.onClick, ui.Close);

            GameObject activationPanel = new GameObject("ActivationPanel", typeof(RectTransform));
            activationPanel.transform.SetParent(panel.transform, false);
            SetRect(activationPanel.GetComponent<RectTransform>(), new Vector2(0f, -15f), new Vector2(760f, 430f));
            CreateText("ActivationTitle", activationPanel.transform, "ACTIVATION SEQUENCE REQUIRED", new Vector2(0f, 145f), new Vector2(700f, 50f), 25, new Color(1f, 0.82f, 0.25f));
            Text activationProgress = CreateText("ActivationProgress", activationPanel.transform, "Activation: 0/3", new Vector2(0f, 90f), new Vector2(400f, 40f), 20, Color.white);
            Button control = CreateButton("ActivateControl", activationPanel.transform, "CONTROL", new Vector2(-230f, 0f), new Vector2(190f, 75f));
            Button light = CreateButton("ActivateLight", activationPanel.transform, "LIGHT", Vector2.zero, new Vector2(190f, 75f));
            Button exit = CreateButton("ActivateExit", activationPanel.transform, "EXIT", new Vector2(230f, 0f), new Vector2(190f, 75f));
            UnityEventTools.AddPersistentListener(control.onClick, ui.ActivateControl);
            UnityEventTools.AddPersistentListener(light.onClick, ui.ActivateLight);
            UnityEventTools.AddPersistentListener(exit.onClick, ui.ActivateExit);
            activationPanel.SetActive(false);

            SerializedObject serializedGrid = new SerializedObject(grid);
            serializedGrid.FindProperty("rows").intValue = 6;
            serializedGrid.FindProperty("columns").intValue = 6;
            SerializedProperty tileRefs = serializedGrid.FindProperty("tiles");
            tileRefs.arraySize = 36;
            for (int index = 0; index < 36; index++) tileRefs.GetArrayElementAtIndex(index).objectReferenceValue = tiles[index];
            serializedGrid.FindProperty("sourceRow").intValue = 0;
            serializedGrid.FindProperty("sourceColumn").intValue = 1;
            serializedGrid.FindProperty("sourceEdge").intValue = (int)ConnectionDirection.Up;
            SerializedProperty outputs = serializedGrid.FindProperty("outputs");
            outputs.arraySize = 3;
            ConfigureCircuitOutput(outputs.GetArrayElementAtIndex(0), PowerChannel.Light, 5, 1, ConnectionDirection.None, lightStatus);
            ConfigureCircuitOutput(outputs.GetArrayElementAtIndex(1), PowerChannel.Control, 1, 5, ConnectionDirection.None, controlStatus);
            ConfigureCircuitOutput(outputs.GetArrayElementAtIndex(2), PowerChannel.Exit, 5, 4, ConnectionDirection.None, exitStatus);
            serializedGrid.FindProperty("rotationCounterText").objectReferenceValue = rotations;
            serializedGrid.FindProperty("poweredCounterText").objectReferenceValue = poweredCounter;
            SerializedProperty requiredRouteRefs = serializedGrid.FindProperty("requiredRouteTiles");
            requiredRouteRefs.arraySize = requiredRoute.Length;
            for (int index = 0; index < requiredRoute.Length; index++)
            {
                requiredRouteRefs.GetArrayElementAtIndex(index).boolValue = requiredRoute[index];
            }
            serializedGrid.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedFinal = new SerializedObject(finalPuzzle);
            serializedFinal.FindProperty("puzzleId").stringValue = "final-power-grid";
            serializedFinal.FindProperty("circuitGrid").objectReferenceValue = grid;
            SerializedProperty activation = serializedFinal.FindProperty("activationSequence");
            activation.arraySize = 3;
            activation.GetArrayElementAtIndex(0).enumValueIndex = (int)PowerChannel.Control;
            activation.GetArrayElementAtIndex(1).enumValueIndex = (int)PowerChannel.Light;
            activation.GetArrayElementAtIndex(2).enumValueIndex = (int)PowerChannel.Exit;
            serializedFinal.FindProperty("audioSource").objectReferenceValue = audioSource;
            serializedFinal.ApplyModifiedPropertiesWithoutUndo();

            GameObject player = FindRootObject(scene, "Player");
            SerializedObject serializedUI = new SerializedObject(ui);
            serializedUI.FindProperty("panel").objectReferenceValue = panel;
            serializedUI.FindProperty("gridPanel").objectReferenceValue = gridPanel;
            serializedUI.FindProperty("activationPanel").objectReferenceValue = activationPanel;
            serializedUI.FindProperty("phaseStatusText").objectReferenceValue = phaseStatus;
            serializedUI.FindProperty("activationProgressText").objectReferenceValue = activationProgress;
            serializedUI.FindProperty("circuitGrid").objectReferenceValue = grid;
            serializedUI.FindProperty("playerControlLock").objectReferenceValue = player != null ? player.GetComponent<PlayerControlLock>() : null;
            serializedUI.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);

            FuseBox fuseBox = fuseBoxRoot.GetComponent<FuseBox>();
            if (fuseBox == null) fuseBox = fuseBoxRoot.AddComponent<FuseBox>();
            GameObject insertedFuse = FindObjectInScene(scene, "InsertedFuseVisual");
            SerializedObject serializedFuseBox = new SerializedObject(fuseBox);
            serializedFuseBox.FindProperty("finalPuzzle").objectReferenceValue = finalPuzzle;
            serializedFuseBox.FindProperty("circuitUI").objectReferenceValue = ui;
            serializedFuseBox.FindProperty("insertedFuseVisual").objectReferenceValue = insertedFuse;
            serializedFuseBox.ApplyModifiedPropertiesWithoutUndo();

            UpgradeFinalGridVisuals(scene);
            GameObject version5 = new GameObject("FinalGridCustomRouteV5");
            version5.transform.SetParent(fuseBoxRoot.transform, false);
            CreateActivationClueIfMissing(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (selectFuseBox) Selection.activeGameObject = fuseBoxRoot;
            if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Migrated Step 11 to the fixed 4x4 Final Power Puzzle.");
        }

        private static CircuitTile CreateCircuitTile(Transform parent, int row, int column, ConnectionDirection connections, int initial, int solved)
        {
            GameObject tileObject = new GameObject($"Tile_R{row}C{column}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CircuitTile));
            tileObject.transform.SetParent(parent, false);
            SetRect(tileObject.GetComponent<RectTransform>(), new Vector2(-205f + column * 82f, 205f - row * 82f), new Vector2(72f, 72f));
            Image image = tileObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.24f);
            Button button = tileObject.GetComponent<Button>();
            button.targetGraphic = image;
            RectTransform wireShape = CreateCircuitWireShape(tileObject.transform, connections, out Graphic[] wireGraphics);

            CircuitTile tile = tileObject.GetComponent<CircuitTile>();
            SerializedObject serializedTile = new SerializedObject(tile);
            serializedTile.FindProperty("baseConnections").intValue = (int)connections;
            serializedTile.FindProperty("initialRotation").intValue = initial;
            serializedTile.FindProperty("solvedRotation").intValue = solved;
            serializedTile.FindProperty("button").objectReferenceValue = button;
            serializedTile.FindProperty("background").objectReferenceValue = image;
            serializedTile.FindProperty("visual").objectReferenceValue = wireShape;
            SerializedProperty graphics = serializedTile.FindProperty("wireGraphics");
            graphics.arraySize = wireGraphics.Length;
            for (int index = 0; index < wireGraphics.Length; index++)
            {
                graphics.GetArrayElementAtIndex(index).objectReferenceValue = wireGraphics[index];
            }
            serializedTile.ApplyModifiedPropertiesWithoutUndo();
            return tile;
        }

        private static void GetLongRouteTileConfiguration(
            int row,
            int column,
            out ConnectionDirection connections,
            out int solvedRotation)
        {
            int tileNumber = row * 6 + column + 1;
            solvedRotation = 0;
            switch (tileNumber)
            {
                case 2:  connections = ConnectionDirection.Up | ConnectionDirection.Down; break;
                case 8:  connections = ConnectionDirection.Up | ConnectionDirection.Down; break;
                case 14: connections = ConnectionDirection.Up | ConnectionDirection.Left; break;
                case 13: connections = ConnectionDirection.Right | ConnectionDirection.Down; break;
                case 19: connections = ConnectionDirection.Up | ConnectionDirection.Down; break;
                case 25: connections = ConnectionDirection.Up | ConnectionDirection.Right; break;
                case 26: connections = ConnectionDirection.Left | ConnectionDirection.Right | ConnectionDirection.Down; break;
                case 32: connections = ConnectionDirection.Up; break;
                case 27: connections = ConnectionDirection.Left | ConnectionDirection.Right; break;
                case 28: connections = ConnectionDirection.Left | ConnectionDirection.Up; break;
                case 22: connections = ConnectionDirection.Up | ConnectionDirection.Down; break;
                case 16: connections = ConnectionDirection.Down | ConnectionDirection.Right; break;
                case 17: connections = ConnectionDirection.Left | ConnectionDirection.Up | ConnectionDirection.Down; break;
                case 11: connections = ConnectionDirection.Down | ConnectionDirection.Right; break;
                case 12: connections = ConnectionDirection.Left; break;
                case 23: connections = ConnectionDirection.Up | ConnectionDirection.Right; break;
                case 24: connections = ConnectionDirection.Left | ConnectionDirection.Down; break;
                case 30: connections = ConnectionDirection.Up | ConnectionDirection.Left; break;
                case 29: connections = ConnectionDirection.Right | ConnectionDirection.Down; break;
                case 35: connections = ConnectionDirection.Up; break;
                default:
                    int pattern = tileNumber % 4;
                    connections = pattern == 0
                        ? ConnectionDirection.Up | ConnectionDirection.Right | ConnectionDirection.Down | ConnectionDirection.Left
                        : pattern == 1
                            ? ConnectionDirection.Up | ConnectionDirection.Right | ConnectionDirection.Left
                            : pattern == 2
                                ? ConnectionDirection.Up | ConnectionDirection.Right
                                : ConnectionDirection.Left | ConnectionDirection.Right;
                    break;
            }
        }

        private static bool IsRequiredRouteTile(int row, int column)
        {
            int tileNumber = row * 6 + column + 1;
            switch (tileNumber)
            {
                case 2: case 8: case 14: case 13: case 19: case 25: case 26: case 32:
                case 27: case 28: case 22: case 16: case 17: case 11: case 12:
                case 23: case 24: case 30: case 29: case 35:
                    return true;
                default:
                    return false;
            }
        }

        private static void UpgradeFinalGridVisuals(Scene scene)
        {
            GameObject gridPanel = FindObjectInScene(scene, "GridPanel");
            if (gridPanel == null) return;

            for (int row = 0; row < 6; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    GameObject tileObject = FindObjectInScene(scene, $"Tile_R{row}C{column}");
                    CircuitTile tile = tileObject != null ? tileObject.GetComponent<CircuitTile>() : null;
                    if (tile == null || tileObject.transform.Find("WireShape") != null) continue;

                    Transform oldVisual = tileObject.transform.Find("WireVisual");
                    if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);
                    RectTransform shape = CreateCircuitWireShape(tileObject.transform, tile.BaseConnections, out Graphic[] graphics);
                    SerializedObject serializedTile = new SerializedObject(tile);
                    serializedTile.FindProperty("visual").objectReferenceValue = shape;
                    SerializedProperty graphicRefs = serializedTile.FindProperty("wireGraphics");
                    graphicRefs.arraySize = graphics.Length;
                    for (int index = 0; index < graphics.Length; index++)
                    {
                        graphicRefs.GetArrayElementAtIndex(index).objectReferenceValue = graphics[index];
                    }
                    serializedTile.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Transform oldSource = gridPanel.transform.Find("PowerSource");
            if (oldSource != null) Object.DestroyImmediate(oldSource.gameObject);
            if (gridPanel.transform.Find("SourcePort") == null)
            {
                CreateText("SourcePort", gridPanel.transform, "POWER\nv", new Vector2(-123f, 272f), new Vector2(180f, 55f), 18, new Color(1f, 0.82f, 0.2f));
                CreateText("LightPort", gridPanel.transform, "[ LIGHT ]", new Vector2(-123f, -270f), new Vector2(150f, 40f), 17, new Color(0.4f, 0.9f, 1f));
                CreateText("ControlPort", gridPanel.transform, "[ CONTROL ]", new Vector2(300f, 123f), new Vector2(170f, 40f), 17, new Color(0.4f, 0.9f, 1f));
                CreateText("ExitPort", gridPanel.transform, "[ EXIT ]", new Vector2(123f, -270f), new Vector2(140f, 40f), 17, new Color(0.4f, 0.9f, 1f));
                CreateText(
                    "GridRule", gridPanel.transform,
                    "Find one continuous route. Some tiles are decoys.",
                    new Vector2(0f, -345f), new Vector2(650f, 38f), 16, new Color(0.82f, 0.86f, 0.92f));
            }
        }

        private static void UpgradeFinalGridDifficulty(Scene scene, GameObject fuseBoxRoot)
        {
            if (FindObjectInScene(scene, "FinalGridDifficultyV2") != null)
            {
                return;
            }

            ConnectionDirection straight = ConnectionDirection.Left | ConnectionDirection.Right;
            ConnectionDirection corner = ConnectionDirection.Up | ConnectionDirection.Right;
            ConnectionDirection tee = ConnectionDirection.Up | ConnectionDirection.Right | ConnectionDirection.Left;
            ConnectionDirection[] bases =
            {
                corner, straight, straight, corner,
                tee, straight, straight, corner,
                corner, straight, straight, tee,
                ConnectionDirection.Right, straight, tee, corner
            };
            int[] solved = { 0, 0, 0, 2, 2, 0, 0, 3, 0, 0, 0, 2, 0, 0, 2, 3 };
            int[] initial = { 2, 1, 3, 1, 0, 1, 3, 1, 3, 1, 3, 0, 2, 1, 1, 0 };

            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    int index = row * 4 + column;
                    GameObject tileObject = FindObjectInScene(scene, $"Tile_R{row}C{column}");
                    CircuitTile tile = tileObject != null ? tileObject.GetComponent<CircuitTile>() : null;
                    if (tile == null) continue;

                    Transform oldShape = tileObject.transform.Find("WireShape");
                    if (oldShape != null) Object.DestroyImmediate(oldShape.gameObject);
                    RectTransform shape = CreateCircuitWireShape(tileObject.transform, bases[index], out Graphic[] graphics);

                    SerializedObject serializedTile = new SerializedObject(tile);
                    serializedTile.FindProperty("baseConnections").intValue = (int)bases[index];
                    serializedTile.FindProperty("initialRotation").intValue = initial[index];
                    serializedTile.FindProperty("solvedRotation").intValue = solved[index];
                    serializedTile.FindProperty("visual").objectReferenceValue = shape;
                    SerializedProperty graphicRefs = serializedTile.FindProperty("wireGraphics");
                    graphicRefs.arraySize = graphics.Length;
                    for (int graphicIndex = 0; graphicIndex < graphics.Length; graphicIndex++)
                    {
                        graphicRefs.GetArrayElementAtIndex(graphicIndex).objectReferenceValue = graphics[graphicIndex];
                    }
                    serializedTile.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GameObject gridPanel = FindObjectInScene(scene, "GridPanel");
            Text poweredCounter = null;
            if (gridPanel != null)
            {
                Transform source = gridPanel.transform.Find("SourcePort");
                if (source != null) source.GetComponent<RectTransform>().anchoredPosition = new Vector2(-180f, 276f);
                Transform lightPort = gridPanel.transform.Find("LightPort");
                if (lightPort != null) lightPort.GetComponent<RectTransform>().anchoredPosition = new Vector2(-300f, 80f);
                Transform controlPort = gridPanel.transform.Find("ControlPort");
                if (controlPort != null) controlPort.GetComponent<RectTransform>().anchoredPosition = new Vector2(300f, -40f);
                Transform exitPort = gridPanel.transform.Find("ExitPort");
                if (exitPort != null) exitPort.GetComponent<RectTransform>().anchoredPosition = new Vector2(60f, -230f);

                Transform existingCounter = gridPanel.transform.Find("PoweredCounter");
                poweredCounter = existingCounter != null
                    ? existingCounter.GetComponent<Text>()
                    : CreateText("PoweredCounter", gridPanel.transform, "Powered: 0/16", new Vector2(205f, -285f), new Vector2(190f, 40f), 18, Color.white);
            }

            CircuitGridManager grid = fuseBoxRoot.GetComponent<CircuitGridManager>();
            if (grid != null)
            {
                SerializedObject serializedGrid = new SerializedObject(grid);
                serializedGrid.FindProperty("sourceRow").intValue = 0;
                serializedGrid.FindProperty("sourceColumn").intValue = 0;
                serializedGrid.FindProperty("sourceEdge").intValue = (int)ConnectionDirection.Up;
                SerializedProperty outputs = serializedGrid.FindProperty("outputs");
                if (outputs.arraySize == 3)
                {
                    outputs.GetArrayElementAtIndex(0).FindPropertyRelative("row").intValue = 1;
                    outputs.GetArrayElementAtIndex(0).FindPropertyRelative("column").intValue = 0;
                    outputs.GetArrayElementAtIndex(0).FindPropertyRelative("edgeDirection").intValue = (int)ConnectionDirection.Left;
                    outputs.GetArrayElementAtIndex(1).FindPropertyRelative("row").intValue = 2;
                    outputs.GetArrayElementAtIndex(1).FindPropertyRelative("column").intValue = 3;
                    outputs.GetArrayElementAtIndex(1).FindPropertyRelative("edgeDirection").intValue = (int)ConnectionDirection.Right;
                    outputs.GetArrayElementAtIndex(2).FindPropertyRelative("row").intValue = 3;
                    outputs.GetArrayElementAtIndex(2).FindPropertyRelative("column").intValue = 2;
                    outputs.GetArrayElementAtIndex(2).FindPropertyRelative("edgeDirection").intValue = (int)ConnectionDirection.Down;
                }
                serializedGrid.FindProperty("poweredCounterText").objectReferenceValue = poweredCounter;
                serializedGrid.FindProperty("requireAllTilesPowered").boolValue = true;
                serializedGrid.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject phaseStatusObject = FindObjectInScene(scene, "PhaseStatus");
            if (phaseStatusObject != null && phaseStatusObject.GetComponent<Text>() != null)
            {
                phaseStatusObject.GetComponent<Text>().text = "POWER ALL 16 TILES AND 3 OUTPUTS";
            }

            GameObject versionMarker = new GameObject("FinalGridDifficultyV2");
            versionMarker.transform.SetParent(fuseBoxRoot.transform, false);
        }

        private static RectTransform CreateCircuitWireShape(Transform parent, ConnectionDirection connections, out Graphic[] graphics)
        {
            GameObject root = new GameObject("WireShape", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), Vector2.zero, new Vector2(72f, 72f));
            System.Collections.Generic.List<Graphic> wireParts = new System.Collections.Generic.List<Graphic>();
            wireParts.Add(CreateWirePart("Center", root.transform, Vector2.zero, new Vector2(14f, 14f)));
            if ((connections & ConnectionDirection.Up) != 0) wireParts.Add(CreateWirePart("Up", root.transform, new Vector2(0f, 22f), new Vector2(9f, 44f)));
            if ((connections & ConnectionDirection.Right) != 0) wireParts.Add(CreateWirePart("Right", root.transform, new Vector2(22f, 0f), new Vector2(44f, 9f)));
            if ((connections & ConnectionDirection.Down) != 0) wireParts.Add(CreateWirePart("Down", root.transform, new Vector2(0f, -22f), new Vector2(9f, 44f)));
            if ((connections & ConnectionDirection.Left) != 0) wireParts.Add(CreateWirePart("Left", root.transform, new Vector2(-22f, 0f), new Vector2(44f, 9f)));
            graphics = wireParts.ToArray();
            return root.GetComponent<RectTransform>();
        }

        private static Image CreateWirePart(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject part = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            part.transform.SetParent(parent, false);
            SetRect(part.GetComponent<RectTransform>(), position, size);
            Image image = part.GetComponent<Image>();
            image.color = new Color(0.78f, 0.82f, 0.88f);
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureCircuitOutput(SerializedProperty output, PowerChannel channel, int row, int column, ConnectionDirection edge, Text status)
        {
            output.FindPropertyRelative("channel").enumValueIndex = (int)channel;
            output.FindPropertyRelative("row").intValue = row;
            output.FindPropertyRelative("column").intValue = column;
            output.FindPropertyRelative("edgeDirection").intValue = (int)edge;
            output.FindPropertyRelative("statusText").objectReferenceValue = status;
        }

        private static void CreateActivationClueIfMissing(Scene scene)
        {
            if (FindObjectInScene(scene, "EmergencyStartupProcedure") != null) return;
            GameObject clue = new GameObject("EmergencyStartupProcedure");
            SceneManager.MoveGameObjectToScene(clue, scene);
            clue.transform.position = new Vector3(-5.86f, 1.75f, 0f);
            clue.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            TextMesh text = clue.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "EMERGENCY STARTUP PROCEDURE\nI. CONTROL SYSTEM\nII. LIGHTING SYSTEM\nIII. EXIT SYSTEM";
            text.fontSize = 48;
            text.characterSize = 0.045f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(0.9f, 0.85f, 0.65f);
            clue.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        [MenuItem("Tools/Puzzle Room/Setup Cabinet and Fuse")]
        public static void SetupCabinetAndFuse()
        {
            AddCabinetAndFuseIfMissing(true);
        }

        private static void AddCabinetAndFuseIfMissing(bool selectCabinet)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject cabinetRoot = FindObjectInScene(scene, "Cabinet");
            if (cabinetRoot == null)
            {
                GameObject placeholder = FindObjectInScene(scene, "Cabinet_Placeholder");
                Transform puzzleObjects = placeholder != null ? placeholder.transform.parent : null;
                Vector3 cabinetPosition = placeholder != null
                    ? placeholder.transform.position
                    : new Vector3(-5.68f, 0.9f, -2.5f);

                if (placeholder != null)
                {
                    Object.DestroyImmediate(placeholder);
                }

                cabinetRoot = new GameObject("Cabinet");
                SceneManager.MoveGameObjectToScene(cabinetRoot, scene);
                cabinetRoot.transform.SetParent(puzzleObjects, true);
                cabinetRoot.transform.position = cabinetPosition;

                CreateCabinetPart("Back", cabinetRoot.transform, new Vector3(-0.25f, 0f, 0f), new Vector3(0.12f, 1.8f, 1.8f));
                CreateCabinetPart("Top", cabinetRoot.transform, new Vector3(0f, 0.84f, 0f), new Vector3(0.6f, 0.12f, 1.8f));
                CreateCabinetPart("Bottom", cabinetRoot.transform, new Vector3(0f, -0.84f, 0f), new Vector3(0.6f, 0.12f, 1.8f));
                CreateCabinetPart("Side_Left", cabinetRoot.transform, new Vector3(0f, 0f, -0.84f), new Vector3(0.6f, 1.56f, 0.12f));
                CreateCabinetPart("Side_Right", cabinetRoot.transform, new Vector3(0f, 0f, 0.84f), new Vector3(0.6f, 1.56f, 0.12f));

                GameObject pivotObject = new GameObject("CabinetDoorPivot");
                pivotObject.transform.SetParent(cabinetRoot.transform, false);
                pivotObject.transform.localPosition = new Vector3(0.36f, 0f, -0.78f);

                GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = "CabinetDoor";
                door.transform.SetParent(pivotObject.transform, false);
                door.transform.localPosition = new Vector3(0f, 0f, 0.78f);
                door.transform.localScale = new Vector3(0.12f, 1.55f, 1.56f);

                GameObject fuse = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fuse.name = "FusePickup";
                fuse.transform.SetParent(cabinetRoot.transform, false);
                fuse.transform.localPosition = new Vector3(0.18f, 0f, 0f);
                fuse.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                fuse.transform.localScale = new Vector3(0.12f, 0.42f, 0.12f);

                WorldItemPickup fusePickup = fuse.AddComponent<WorldItemPickup>();
                SerializedObject serializedFuse = new SerializedObject(fusePickup);
                serializedFuse.FindProperty("itemId").enumValueIndex = (int)ItemId.Fuse;
                serializedFuse.FindProperty("displayName").stringValue = "Fuse";
                serializedFuse.ApplyModifiedPropertiesWithoutUndo();
                fuse.SetActive(false);

                Cabinet cabinet = cabinetRoot.AddComponent<Cabinet>();
                SerializedObject serializedCabinet = new SerializedObject(cabinet);
                serializedCabinet.FindProperty("isLocked").boolValue = true;
                serializedCabinet.FindProperty("requiredItem").enumValueIndex = (int)ItemId.Key;
                serializedCabinet.FindProperty("lockedMessage").stringValue = "Requires Key";
                serializedCabinet.FindProperty("doorPivot").objectReferenceValue = pivotObject.transform;
                serializedCabinet.FindProperty("openAngle").floatValue = 100f;
                serializedCabinet.FindProperty("openSpeed").floatValue = 120f;
                serializedCabinet.FindProperty("containedItem").objectReferenceValue = fuse;
                serializedCabinet.ApplyModifiedPropertiesWithoutUndo();
            }

            Cabinet existingCabinet = cabinetRoot.GetComponent<Cabinet>();
            if (existingCabinet != null)
            {
                SerializedObject serializedCabinet = new SerializedObject(existingCabinet);
                serializedCabinet.FindProperty("openAngle").floatValue = 100f;
                serializedCabinet.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (selectCabinet)
            {
                Selection.activeGameObject = cabinetRoot;
                EditorGUIUtility.PingObject(cabinetRoot);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("Configured the Key-locked Cabinet and Fuse reward.");
        }

        private static GameObject CreateCabinetPart(string name, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            return part;
        }

        [MenuItem("Tools/Puzzle Room/Setup Hanoi Bookshelf Puzzle")]
        public static void SetupHanoiPuzzle()
        {
            AddHanoiPuzzleIfMissing(true);
        }

        private static void AddHanoiPuzzleIfMissing(bool selectPuzzle)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject hanoiRoot = FindObjectInScene(scene, "HanoiPuzzle");
            if (hanoiRoot == null)
            {
                GameObject oldBookshelf = FindObjectInScene(scene, "Bookshelf");
                GameObject body = FindObjectInScene(scene, "BookshelfBody") ?? FindObjectInScene(scene, "Bookshelf_Placeholder");
                GameObject drawer = FindObjectInScene(scene, "SecretDrawer");
                GameObject key = FindObjectInScene(scene, "KeyPickup");
                Transform puzzleObjects = oldBookshelf != null ? oldBookshelf.transform.parent : body != null ? body.transform.parent : null;

                if (body != null)
                {
                    body.transform.SetParent(null, true);
                }

                if (drawer != null)
                {
                    drawer.transform.SetParent(null, true);
                }

                if (oldBookshelf != null)
                {
                    Object.DestroyImmediate(oldBookshelf);
                }

                hanoiRoot = new GameObject("HanoiPuzzle");
                SceneManager.MoveGameObjectToScene(hanoiRoot, scene);
                hanoiRoot.transform.SetParent(puzzleObjects, false);
                hanoiRoot.transform.localPosition = new Vector3(5.68f, 0.35f, 1.5f);

                if (body != null)
                {
                    body.name = "BookshelfBody";
                    body.transform.SetParent(hanoiRoot.transform, false);
                    body.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    body.transform.localRotation = Quaternion.identity;
                    body.transform.localScale = new Vector3(0.6f, 2.4f, 3f);
                }

                if (drawer == null)
                {
                    drawer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    drawer.name = "SecretDrawer";
                }

                drawer.transform.SetParent(hanoiRoot.transform, false);
                drawer.transform.localPosition = new Vector3(-0.38f, 0.3f, 0f);
                drawer.transform.localRotation = Quaternion.identity;
                drawer.transform.localScale = new Vector3(0.5f, 0.3f, 1.8f);

                if (key == null)
                {
                    key = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    key.name = "KeyPickup";
                    key.transform.SetParent(drawer.transform, false);
                    key.transform.localPosition = new Vector3(-0.65f, 0.25f, 0f);
                    key.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    key.transform.localScale = new Vector3(0.35f, 0.08f, 0.12f);

                    WorldItemPickup keyPickup = key.AddComponent<WorldItemPickup>();
                    SerializedObject serializedKey = new SerializedObject(keyPickup);
                    serializedKey.FindProperty("itemId").enumValueIndex = (int)ItemId.Key;
                    serializedKey.FindProperty("displayName").stringValue = "Key";
                    serializedKey.ApplyModifiedPropertiesWithoutUndo();
                }
                else if (key.transform.parent != drawer.transform)
                {
                    key.transform.SetParent(drawer.transform, true);
                }

                key.SetActive(false);

                HanoiPuzzleManager manager = hanoiRoot.AddComponent<HanoiPuzzleManager>();
                Transform pegsGroup = new GameObject("Pegs").transform;
                pegsGroup.SetParent(hanoiRoot.transform, false);
                Transform booksGroup = new GameObject("Books").transform;
                booksGroup.SetParent(hanoiRoot.transform, false);

                HanoiPeg leftPeg = CreateHanoiPeg(pegsGroup, manager, "LeftPeg", "Left Peg", -0.88f);
                HanoiPeg middlePeg = CreateHanoiPeg(pegsGroup, manager, "MiddlePeg", "Middle Peg", 0f);
                HanoiPeg rightPeg = CreateHanoiPeg(pegsGroup, manager, "RightPeg", "Right Peg", 0.88f);

                Material bookMaterial = GetOrCreateHanoiBookMaterial();
                HanoiBook[] books =
                {
                    CreateHanoiBook(booksGroup, manager, BookSize.Small, 0.4f, bookMaterial),
                    CreateHanoiBook(booksGroup, manager, BookSize.Medium, 0.56f, bookMaterial),
                    CreateHanoiBook(booksGroup, manager, BookSize.Large, 0.72f, bookMaterial),
                    CreateHanoiBook(booksGroup, manager, BookSize.ExtraLarge, 0.88f, bookMaterial)
                };

                GameObject resetButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                resetButton.name = "ResetButton";
                resetButton.transform.SetParent(hanoiRoot.transform, false);
                resetButton.transform.localPosition = new Vector3(-0.45f, 1.8f, 1.2f);
                resetButton.transform.localScale = new Vector3(0.18f, 0.22f, 0.32f);
                HanoiResetInteractable reset = resetButton.AddComponent<HanoiResetInteractable>();
                SerializedObject serializedReset = new SerializedObject(reset);
                serializedReset.FindProperty("puzzleManager").objectReferenceValue = manager;
                serializedReset.ApplyModifiedPropertiesWithoutUndo();

                Text counter = CreateHanoiMoveCounter(scene);
                SerializedObject serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("puzzleId").stringValue = "bookshelf-sequence";
                serializedManager.FindProperty("leftPeg").objectReferenceValue = leftPeg;
                serializedManager.FindProperty("middlePeg").objectReferenceValue = middlePeg;
                serializedManager.FindProperty("rightPeg").objectReferenceValue = rightPeg;
                SerializedProperty bookReferences = serializedManager.FindProperty("books");
                bookReferences.arraySize = books.Length;
                for (int index = 0; index < books.Length; index++)
                {
                    bookReferences.GetArrayElementAtIndex(index).objectReferenceValue = books[index];
                }
                serializedManager.FindProperty("moveSpeed").floatValue = 1.8f;
                serializedManager.FindProperty("travelHeight").floatValue = 0.75f;
                serializedManager.FindProperty("moveCounterText").objectReferenceValue = counter;
                serializedManager.FindProperty("secretDrawer").objectReferenceValue = drawer.transform;
                serializedManager.FindProperty("drawerOpenOffset").vector3Value = new Vector3(-0.65f, 0f, 0f);
                serializedManager.FindProperty("drawerSpeed").floatValue = 0.7f;
                serializedManager.FindProperty("rewardObject").objectReferenceValue = key;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
            }

            if (hanoiRoot != null)
            {
                Vector3 position = hanoiRoot.transform.localPosition;
                hanoiRoot.transform.localPosition = new Vector3(position.x, 0.35f, position.z);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (selectPuzzle)
            {
                Selection.activeGameObject = hanoiRoot;
                EditorGUIUtility.PingObject(hanoiRoot);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("Configured the 4-book Tower of Hanoi puzzle while preserving the existing Key reward.");
        }

        private static HanoiPeg CreateHanoiPeg(Transform parent, HanoiPuzzleManager manager, string objectName, string label, float localZ)
        {
            GameObject pegObject = new GameObject(objectName);
            pegObject.transform.SetParent(parent, false);
            pegObject.transform.localPosition = new Vector3(-0.48f, 0f, localZ);
            HanoiPeg peg = pegObject.AddComponent<HanoiPeg>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "PegVisual";
            visual.transform.SetParent(pegObject.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            visual.transform.localScale = new Vector3(0.12f, 1.3f, 0.12f);

            Transform[] slots = new Transform[4];
            for (int index = 0; index < slots.Length; index++)
            {
                GameObject slot = new GameObject($"Slot_{index}");
                slot.transform.SetParent(pegObject.transform, false);
                slot.transform.localPosition = new Vector3(-0.08f, 0.56f + index * 0.16f, 0f);
                slots[index] = slot.transform;
            }

            SerializedObject serializedPeg = new SerializedObject(peg);
            serializedPeg.FindProperty("pegName").stringValue = label;
            serializedPeg.FindProperty("puzzleManager").objectReferenceValue = manager;
            SerializedProperty serializedSlots = serializedPeg.FindProperty("slots");
            serializedSlots.arraySize = slots.Length;
            for (int index = 0; index < slots.Length; index++)
            {
                serializedSlots.GetArrayElementAtIndex(index).objectReferenceValue = slots[index];
            }
            serializedPeg.ApplyModifiedPropertiesWithoutUndo();
            return peg;
        }

        private static HanoiBook CreateHanoiBook(Transform parent, HanoiPuzzleManager manager, BookSize size, float width, Material material)
        {
            GameObject bookObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bookObject.name = $"Book_{size}";
            bookObject.transform.SetParent(parent, false);
            bookObject.transform.localScale = new Vector3(0.24f, 0.13f, width);
            bookObject.GetComponent<MeshRenderer>().sharedMaterial = material;

            HanoiBook book = bookObject.AddComponent<HanoiBook>();
            SerializedObject serializedBook = new SerializedObject(book);
            serializedBook.FindProperty("size").enumValueIndex = (int)size - 1;
            serializedBook.FindProperty("puzzleManager").objectReferenceValue = manager;
            serializedBook.FindProperty("selectedLift").floatValue = 0.12f;
            serializedBook.ApplyModifiedPropertiesWithoutUndo();
            return book;
        }

        private static Material GetOrCreateHanoiBookMaterial()
        {
            const string path = "Assets/Materials/HanoiBook.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Standard"))
            {
                name = "HanoiBook",
                color = new Color(0.42f, 0.19f, 0.08f)
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Text CreateHanoiMoveCounter(Scene scene)
        {
            GameObject canvasObject = new GameObject("HanoiHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            GameObject textObject = new GameObject("MoveCounter", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(240f, 70f);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.text = "Moves: 0\nMinimum: 15";
            return text;
        }

        [MenuItem("Tools/Puzzle Room/Setup Symbol Unlock Puzzle")]
        public static void SetupSymbolUnlockPuzzle()
        {
            AddSymbolUnlockPuzzleIfMissing(true);
        }

        private static void AddSymbolUnlockPuzzleIfMissing(bool selectPuzzle)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject hanoiRoot = FindObjectInScene(scene, "HanoiPuzzle");
            HanoiPuzzleManager hanoi = hanoiRoot != null ? hanoiRoot.GetComponent<HanoiPuzzleManager>() : null;
            GameObject symbolRoot = FindObjectInScene(scene, "SymbolUnlock");

            if (hanoiRoot != null && hanoi != null && symbolRoot == null)
            {
                symbolRoot = new GameObject("SymbolUnlock");
                symbolRoot.transform.SetParent(hanoiRoot.transform, false);
                SymbolSequencePuzzle sequence = symbolRoot.AddComponent<SymbolSequencePuzzle>();

                GameObject buttons = new GameObject("Buttons");
                buttons.transform.SetParent(symbolRoot.transform, false);

                PuzzleSymbol[] layout =
                {
                    PuzzleSymbol.Diamond,
                    PuzzleSymbol.Sun,
                    PuzzleSymbol.Triangle,
                    PuzzleSymbol.Moon
                };
                float[] zPositions = { -1.15f, -0.38f, 0.38f, 1.15f };
                for (int index = 0; index < layout.Length; index++)
                {
                    CreateSymbolButton(buttons.transform, sequence, layout[index], zPositions[index]);
                }

                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                indicator.name = "BookMechanismUnlockedIndicator";
                indicator.transform.SetParent(symbolRoot.transform, false);
                indicator.transform.localPosition = new Vector3(-0.68f, 1.9f, -1.25f);
                indicator.transform.localScale = new Vector3(0.12f, 0.16f, 0.16f);
                Object.DestroyImmediate(indicator.GetComponent<Collider>());

                Material indicatorMaterial = GetOrCreateSimpleMaterial(
                    "Assets/Materials/HanoiUnlockedIndicator.mat",
                    "HanoiUnlockedIndicator",
                    new Color(0.15f, 1f, 0.25f));
                indicator.GetComponent<MeshRenderer>().sharedMaterial = indicatorMaterial;
                indicator.SetActive(false);

                SerializedObject serializedSequence = new SerializedObject(sequence);
                serializedSequence.FindProperty("puzzleId").stringValue = "hanoi-symbol-unlock";
                SerializedProperty correctSequence = serializedSequence.FindProperty("correctSequence");
                correctSequence.arraySize = 4;
                correctSequence.GetArrayElementAtIndex(0).enumValueIndex = (int)PuzzleSymbol.Moon;
                correctSequence.GetArrayElementAtIndex(1).enumValueIndex = (int)PuzzleSymbol.Triangle;
                correctSequence.GetArrayElementAtIndex(2).enumValueIndex = (int)PuzzleSymbol.Diamond;
                correctSequence.GetArrayElementAtIndex(3).enumValueIndex = (int)PuzzleSymbol.Sun;
                serializedSequence.FindProperty("hanoiPuzzle").objectReferenceValue = hanoi;
                serializedSequence.FindProperty("unlockedIndicator").objectReferenceValue = indicator;
                serializedSequence.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedHanoi = new SerializedObject(hanoi);
                serializedHanoi.FindProperty("startUnlocked").boolValue = false;
                serializedHanoi.FindProperty("unlockedIndicator").objectReferenceValue = indicator;
                serializedHanoi.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (selectPuzzle && symbolRoot != null)
            {
                Selection.activeGameObject = symbolRoot;
                EditorGUIUtility.PingObject(symbolRoot);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("Configured the UV symbol sequence gate for the existing Hanoi puzzle.");
        }

        private static void CreateSymbolButton(
            Transform parent,
            SymbolSequencePuzzle sequence,
            PuzzleSymbol symbol,
            float localZ)
        {
            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonObject.name = $"{symbol}Button";
            buttonObject.transform.SetParent(parent, false);
            buttonObject.transform.localPosition = new Vector3(-0.68f, 1.55f, localZ);
            buttonObject.transform.localScale = new Vector3(0.16f, 0.2f, 0.48f);

            SymbolButton button = buttonObject.AddComponent<SymbolButton>();
            SerializedObject serializedButton = new SerializedObject(button);
            serializedButton.FindProperty("symbol").enumValueIndex = (int)symbol;
            serializedButton.FindProperty("sequencePuzzle").objectReferenceValue = sequence;
            serializedButton.FindProperty("pressOffset").vector3Value = new Vector3(0.06f, 0f, 0f);
            serializedButton.FindProperty("pressSpeed").floatValue = 0.5f;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();

            GameObject label = new GameObject("Label");
            label.transform.SetParent(buttonObject.transform, false);
            label.transform.localPosition = new Vector3(-0.56f, 0f, 0f);
            label.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            label.transform.localScale = new Vector3(4f, 4f, 4f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = symbol.ToString().ToUpperInvariant();
            text.fontSize = 32;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        private static Material GetOrCreateSimpleMaterial(string path, string materialName, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Standard"))
            {
                name = materialName,
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        [MenuItem("Tools/Puzzle Room/Setup UV Reveal Clue")]
        public static void SetupUVRevealClue()
        {
            AddUVRevealClueIfMissing(true);
        }

        private static void AddUVRevealClueIfMissing(bool selectClue)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject clueGroup = FindObjectInScene(scene, "UVRevealObjects");

            if (clueGroup == null)
            {
                clueGroup = new GameObject("UVRevealObjects");
                SceneManager.MoveGameObjectToScene(clueGroup, scene);

                GameObject environment = FindRootObject(scene, "Environment");
                if (environment != null)
                {
                    clueGroup.transform.SetParent(environment.transform, false);
                }
            }

            GameObject oldClue = FindObjectInScene(scene, "UVClue_CheckTheBooks");
            if (oldClue != null)
            {
                Object.DestroyImmediate(oldClue);
            }

            GameObject clue = FindObjectInScene(scene, "UVClue_HiddenSymbols");
            if (clue == null)
            {
                clue = new GameObject("UVClue_HiddenSymbols");
                clue.transform.SetParent(clueGroup.transform, false);
                clue.transform.position = new Vector3(0f, 1.75f, 4.88f);
                clue.transform.rotation = Quaternion.identity;

                string[] labels = { "1  MOON", "2  TRIANGLE", "3  DIAMOND", "4  SUN" };
                string[] names = { "Moon", "Triangle", "Diamond", "Sun" };
                float[] xPositions = { -2.4f, -0.85f, 0.85f, 2.35f };
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                for (int index = 0; index < labels.Length; index++)
                {
                    GameObject symbol = new GameObject(names[index]);
                    symbol.transform.SetParent(clue.transform, false);
                    symbol.transform.localPosition = new Vector3(xPositions[index], 0f, 0f);

                    TextMesh textMesh = symbol.AddComponent<TextMesh>();
                    textMesh.font = font;
                    textMesh.text = labels[index];
                    textMesh.fontSize = 64;
                    textMesh.characterSize = 0.055f;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.color = new Color(0.72f, 0.25f, 1f, 1f);

                    MeshRenderer symbolRenderer = symbol.GetComponent<MeshRenderer>();
                    symbolRenderer.sharedMaterial = font.material;
                    symbolRenderer.enabled = false;

                    UVRevealObject reveal = symbol.AddComponent<UVRevealObject>();
                    SerializedObject serializedReveal = new SerializedObject(reveal);
                    SerializedProperty renderers = serializedReveal.FindProperty("revealRenderers");
                    renderers.arraySize = 1;
                    renderers.GetArrayElementAtIndex(0).objectReferenceValue = symbolRenderer;
                    serializedReveal.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (selectClue)
            {
                Selection.activeGameObject = clue;
                EditorGUIUtility.PingObject(clue);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("Configured the UV-only Moon-Triangle-Diamond-Sun clue on the north wall.");
        }

        [MenuItem("Tools/Puzzle Room/Setup UV Light Reward")]
        public static void SetupUVLightReward()
        {
            AddUVLightPrototypeIfMissing(true);
        }

        private static void AddUVLightPrototypeIfMissing(bool selectPickup)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject player = FindRootObject(scene, "Player");
            PlayerInventory inventory = player != null ? player.GetComponent<PlayerInventory>() : null;

            if (player != null && inventory == null)
            {
                inventory = player.AddComponent<PlayerInventory>();
            }

            Camera playerCamera = player != null ? player.GetComponentInChildren<Camera>(true) : null;
            GameObject uvEffectObject = FindObjectInScene(scene, "UVLightEffect");

            if (playerCamera != null && uvEffectObject == null)
            {
                uvEffectObject = new GameObject("UVLightEffect");
                uvEffectObject.transform.SetParent(playerCamera.transform, false);
                uvEffectObject.transform.localPosition = new Vector3(0f, -0.08f, 0.1f);

                Light light = uvEffectObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = new Color(0.50f, 0.12f, 1f, 1f);
                light.intensity = 8f;
                light.range = 9f;
                light.spotAngle = 64f;
                light.innerSpotAngle = 40f;
                light.shadows = LightShadows.Soft;
                light.enabled = false;
            }

            if (uvEffectObject != null && uvEffectObject.GetComponent<Light>() != null)
            {
                Light configuredUVLight = uvEffectObject.GetComponent<Light>();
                configuredUVLight.color = new Color(0.50f, 0.12f, 1f, 1f);
                configuredUVLight.intensity = 8f;
                configuredUVLight.range = 3.5f;
                configuredUVLight.spotAngle = 64f;
                configuredUVLight.innerSpotAngle = 40f;
            }

            if (playerCamera != null)
            {
                UVLightController controller = playerCamera.GetComponent<UVLightController>();

                if (controller == null)
                {
                    controller = playerCamera.gameObject.AddComponent<UVLightController>();
                }

                SerializedObject serializedController = new SerializedObject(controller);
                serializedController.FindProperty("inventory").objectReferenceValue = inventory;
                serializedController.FindProperty("uvLight").objectReferenceValue = uvEffectObject != null
                    ? uvEffectObject.GetComponent<Light>()
                    : null;
                SerializedProperty equippedVisualProperty = serializedController.FindProperty("equippedVisual");
                if (equippedVisualProperty != null)
                {
                    equippedVisualProperty.objectReferenceValue = uvEffectObject != null
                        ? uvEffectObject.transform.Find("EquippedFlashlightVisual")?.gameObject
                        : null;
                }
                serializedController.FindProperty("toggleKey").intValue = (int)KeyCode.F;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject pickup = FindObjectInScene(scene, "UVLightPickup");

            if (pickup == null)
            {
                pickup = FindObjectInScene(scene, "RewardPlaceholder");
            }

            if (pickup != null)
            {
                pickup.name = "UVLightPickup";
                pickup.transform.localPosition = new Vector3(0f, 0.45f, -0.52f);
                pickup.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
                pickup.transform.localScale = new Vector3(0.35f, 0.18f, 0.18f);

                WorldItemPickup worldPickup = pickup.GetComponent<WorldItemPickup>();

                if (worldPickup == null)
                {
                    worldPickup = pickup.AddComponent<WorldItemPickup>();
                }

                SerializedObject serializedPickup = new SerializedObject(worldPickup);
                serializedPickup.FindProperty("itemId").enumValueIndex = (int)ItemId.UVLight;
                serializedPickup.FindProperty("displayName").stringValue = "UV Light";
                serializedPickup.ApplyModifiedPropertiesWithoutUndo();

                SafePuzzle safePuzzle = pickup.GetComponentInParent<SafePuzzle>(true);
                if (safePuzzle != null)
                {
                    SerializedObject serializedSafe = new SerializedObject(safePuzzle);
                    serializedSafe.FindProperty("rewardObject").objectReferenceValue = pickup;
                    serializedSafe.ApplyModifiedPropertiesWithoutUndo();
                }

                pickup.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (selectPickup && pickup != null)
            {
                Selection.activeGameObject = pickup;
                EditorGUIUtility.PingObject(pickup);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("Configured the UV Light pickup, player item state, and toggleable UV effect.");
        }

        [MenuItem("Tools/Puzzle Room/Setup Puzzle 1 - Safe")]
        public static void SetupPuzzleOne()
        {
            AddPuzzleOnePrototypeIfMissing(true);
        }

        private static void AddPuzzleOnePrototypeIfMissing(bool selectSafe)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject player = FindRootObject(scene, "Player");
            PlayerControlLock controlLock = player != null ? player.GetComponent<PlayerControlLock>() : null;

            if (player != null && controlLock == null)
            {
                controlLock = player.AddComponent<PlayerControlLock>();
                SerializedObject serializedLock = new SerializedObject(controlLock);
                serializedLock.FindProperty("playerMovement").objectReferenceValue = player.GetComponent<PlayerMovement>();
                serializedLock.FindProperty("playerLook").objectReferenceValue = player.GetComponentInChildren<PlayerLook>(true);
                serializedLock.FindProperty("playerInteraction").objectReferenceValue = player.GetComponentInChildren<PlayerInteraction>(true);
                serializedLock.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject keypadRoot = FindRootObject(scene, "KeypadCanvas");
            KeypadUI keypadUI;

            if (keypadRoot == null)
            {
                keypadRoot = new GameObject(
                    "KeypadCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(KeypadUI));
                SceneManager.MoveGameObjectToScene(keypadRoot, scene);

                Canvas canvas = keypadRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = keypadRoot.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                keypadUI = keypadRoot.GetComponent<KeypadUI>();

                GameObject panel = new GameObject("KeypadPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(keypadRoot.transform, false);
                SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(420f, 600f));
                panel.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 0.96f);

                CreateText("Title", panel.transform, "SAFE KEYPAD", new Vector2(0f, 250f), new Vector2(360f, 50f), 30, Color.white);
                Text codeDisplay = CreateText("CodeDisplay", panel.transform, "___", new Vector2(0f, 175f), new Vector2(340f, 70f), 42, new Color(0.55f, 1f, 0.65f));
                Text statusText = CreateText("StatusText", panel.transform, string.Empty, new Vector2(0f, 120f), new Vector2(340f, 40f), 22, new Color(1f, 0.35f, 0.35f));

                string[,] digits =
                {
                    { "1", "2", "3" },
                    { "4", "5", "6" },
                    { "7", "8", "9" }
                };

                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        string digit = digits[row, column];
                        Button button = CreateButton(
                            $"Button_{digit}",
                            panel.transform,
                            digit,
                            new Vector2(-110f + column * 110f, 45f - row * 85f),
                            new Vector2(90f, 65f));
                        UnityEventTools.AddStringPersistentListener(button.onClick, keypadUI.PressDigit, digit);
                    }
                }

                Button clearButton = CreateButton("Button_Clear", panel.transform, "CLEAR", new Vector2(-110f, -210f), new Vector2(90f, 65f));
                UnityEventTools.AddPersistentListener(clearButton.onClick, keypadUI.Clear);

                Button zeroButton = CreateButton("Button_0", panel.transform, "0", new Vector2(0f, -210f), new Vector2(90f, 65f));
                UnityEventTools.AddStringPersistentListener(zeroButton.onClick, keypadUI.PressDigit, "0");

                Button enterButton = CreateButton("Button_Enter", panel.transform, "ENTER", new Vector2(110f, -210f), new Vector2(90f, 65f));
                UnityEventTools.AddPersistentListener(enterButton.onClick, keypadUI.Submit);

                Button closeButton = CreateButton("Button_Close", panel.transform, "X", new Vector2(175f, 260f), new Vector2(45f, 45f));
                UnityEventTools.AddPersistentListener(closeButton.onClick, keypadUI.Close);

                SerializedObject serializedKeypad = new SerializedObject(keypadUI);
                serializedKeypad.FindProperty("panel").objectReferenceValue = panel;
                serializedKeypad.FindProperty("codeDisplay").objectReferenceValue = codeDisplay;
                serializedKeypad.FindProperty("statusText").objectReferenceValue = statusText;
                serializedKeypad.FindProperty("playerControlLock").objectReferenceValue = controlLock;
                serializedKeypad.ApplyModifiedPropertiesWithoutUndo();

                panel.SetActive(false);
            }
            else
            {
                keypadUI = keypadRoot.GetComponent<KeypadUI>();
            }

            if (FindRootObject(scene, "EventSystem") == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
            }

            GameObject safeRoot = FindObjectInScene(scene, "Safe");

            if (safeRoot == null)
            {
                GameObject safeBody = FindObjectInScene(scene, "Safe_Placeholder");
                Transform puzzleObjects = safeBody != null ? safeBody.transform.parent : null;

                safeRoot = new GameObject("Safe");
                SceneManager.MoveGameObjectToScene(safeRoot, scene);
                safeRoot.transform.SetParent(puzzleObjects, false);
                safeRoot.transform.localPosition = new Vector3(-4.6f, 0f, 3.9f);

                if (safeBody != null)
                {
                    safeBody.name = "SafeBody";
                    safeBody.transform.SetParent(safeRoot.transform, false);
                    safeBody.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                    safeBody.transform.localRotation = Quaternion.identity;
                    safeBody.transform.localScale = new Vector3(1.2f, 1.2f, 0.8f);
                }

                GameObject safeDoorPivot = new GameObject("SafeDoorPivot");
                safeDoorPivot.transform.SetParent(safeRoot.transform, false);
                safeDoorPivot.transform.localPosition = new Vector3(-0.61f, 0.6f, -0.46f);

                GameObject safeDoorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                safeDoorVisual.name = "SafeDoorVisual";
                safeDoorVisual.transform.SetParent(safeDoorPivot.transform, false);
                safeDoorVisual.transform.localPosition = new Vector3(0.6f, 0f, 0f);
                safeDoorVisual.transform.localScale = new Vector3(1.2f, 1.2f, 0.1f);

                GameObject reward = GameObject.CreatePrimitive(PrimitiveType.Cube);
                reward.name = "RewardPlaceholder";
                reward.transform.SetParent(safeRoot.transform, false);
                reward.transform.localPosition = new Vector3(0f, 0.45f, -0.52f);
                reward.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

                SafePuzzle safePuzzle = safeRoot.AddComponent<SafePuzzle>();
                SerializedObject serializedSafe = new SerializedObject(safePuzzle);
                serializedSafe.FindProperty("puzzleId").stringValue = "painting-safe";
                serializedSafe.FindProperty("correctCode").stringValue = "527";
                serializedSafe.FindProperty("keypadUI").objectReferenceValue = keypadUI;
                serializedSafe.FindProperty("doorPivot").objectReferenceValue = safeDoorPivot.transform;
                serializedSafe.FindProperty("openAngle").floatValue = 105f;
                serializedSafe.FindProperty("openSpeed").floatValue = 120f;
                serializedSafe.ApplyModifiedPropertiesWithoutUndo();
            }

            SafePuzzle configuredSafe = safeRoot.GetComponent<SafePuzzle>();
            if (configuredSafe != null)
            {
                SerializedObject serializedSafe = new SerializedObject(configuredSafe);
                serializedSafe.FindProperty("openAngle").floatValue = 105f;
                serializedSafe.ApplyModifiedPropertiesWithoutUndo();
            }

            RemovePaintingInteraction(scene, "Painting_Left");
            RemovePaintingInteraction(scene, "Painting_Center");
            RemovePaintingInteraction(scene, "Painting_Right");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (selectSafe)
            {
                Selection.activeGameObject = safeRoot;
                EditorGUIUtility.PingObject(safeRoot);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("Configured Puzzle 1: painting clues 5-2-7, keypad UI, and Safe.");
        }

        private static void RemovePaintingInteraction(Scene scene, string paintingName)
        {
            GameObject painting = FindObjectInScene(scene, paintingName);

            if (painting == null)
            {
                return;
            }

            PaintingClue paintingClue = painting.GetComponent<PaintingClue>();
            if (paintingClue != null)
            {
                Object.DestroyImmediate(paintingClue);
            }
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string content,
            Vector2 position,
            Vector2 size,
            int fontSize,
            Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            SetRect(textObject.GetComponent<RectTransform>(), position, size);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            SetRect(buttonObject.GetComponent<RectTransform>(), position, size);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.24f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            CreateText("Label", buttonObject.transform, label, Vector2.zero, size, 20, Color.white);
            return button;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        [MenuItem("Tools/Puzzle Room/Setup Step 12 Victory Flow")]
        public static void SetupVictoryFlow()
        {
            AddVictoryFlowIfMissing(true);
            EnsureMainMenuAndBuildSettings();
        }

        private static void AddVictoryFlowIfMissing(bool selectTrigger)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject player = FindRootObject(scene, "Player");
            GameObject doorRoot = FindObjectInScene(scene, "ExitDoor");
            GameObject powerRoot = FindRootObject(scene, "PowerSystem");

            if (player == null || doorRoot == null || powerRoot == null)
            {
                Debug.LogError("Step 12 setup requires Player, ExitDoor, and PowerSystem in PuzzleRoom.");
                if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            Door exitDoor = doorRoot.GetComponent<Door>();
            SerializedObject serializedDoor = new SerializedObject(exitDoor);
            serializedDoor.FindProperty("isLocked").boolValue = true;
            serializedDoor.FindProperty("lockedMessage").stringValue = "Locked. No power.";
            serializedDoor.ApplyModifiedPropertiesWithoutUndo();

            GameObject gameFlowRoot = FindRootObject(scene, "GameFlow");
            if (gameFlowRoot == null)
            {
                gameFlowRoot = new GameObject("GameFlow");
                SceneManager.MoveGameObjectToScene(gameFlowRoot, scene);
            }

            GameTimer timer = gameFlowRoot.GetComponent<GameTimer>();
            if (timer == null) timer = gameFlowRoot.AddComponent<GameTimer>();
            GameFlowManager flow = gameFlowRoot.GetComponent<GameFlowManager>();
            if (flow == null) flow = gameFlowRoot.AddComponent<GameFlowManager>();
            AudioSource flowAudio = gameFlowRoot.GetComponent<AudioSource>();
            if (flowAudio == null) flowAudio = gameFlowRoot.AddComponent<AudioSource>();
            flowAudio.playOnAwake = false;

            GameObject canvasRoot = FindRootObject(scene, "VictoryCanvas");
            VictoryUI victoryUI;
            Text completionTime;
            if (canvasRoot == null)
            {
                canvasRoot = new GameObject("VictoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(VictoryUI));
                SceneManager.MoveGameObjectToScene(canvasRoot, scene);
                Canvas canvas = canvasRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                GameObject panel = new GameObject("VictoryPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(canvasRoot.transform, false);
                SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(720f, 470f));
                panel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.06f, 0.97f);
                CreateText("VictoryTitle", panel.transform, "YOU ESCAPED!", new Vector2(0f, 145f), new Vector2(650f, 75f), 46, new Color(0.35f, 1f, 0.65f));
                CreateText("TimeLabel", panel.transform, "COMPLETION TIME", new Vector2(0f, 55f), new Vector2(500f, 45f), 24, Color.white);
                completionTime = CreateText("CompletionTime", panel.transform, "00:00", new Vector2(0f, 0f), new Vector2(500f, 70f), 42, Color.white);
                Button replay = CreateButton("PlayAgain", panel.transform, "PLAY AGAIN", new Vector2(-150f, -130f), new Vector2(240f, 65f));
                Button menu = CreateButton("MainMenu", panel.transform, "MAIN MENU", new Vector2(150f, -130f), new Vector2(240f, 65f));
                UnityEventTools.AddPersistentListener(replay.onClick, flow.PlayAgain);
                UnityEventTools.AddPersistentListener(menu.onClick, flow.ReturnToMainMenu);
                victoryUI = canvasRoot.GetComponent<VictoryUI>();
                SerializedObject serializedUI = new SerializedObject(victoryUI);
                serializedUI.FindProperty("panel").objectReferenceValue = panel;
                serializedUI.FindProperty("completionTimeText").objectReferenceValue = completionTime;
                serializedUI.ApplyModifiedPropertiesWithoutUndo();
                panel.SetActive(false);
            }
            else
            {
                victoryUI = canvasRoot.GetComponent<VictoryUI>();
            }

            SerializedObject serializedFlow = new SerializedObject(flow);
            serializedFlow.FindProperty("gameTimer").objectReferenceValue = timer;
            serializedFlow.FindProperty("playerControlLock").objectReferenceValue = player.GetComponent<PlayerControlLock>();
            serializedFlow.FindProperty("victoryUI").objectReferenceValue = victoryUI;
            serializedFlow.FindProperty("gameplaySceneName").stringValue = "PuzzleRoom";
            serializedFlow.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            serializedFlow.FindProperty("audioSource").objectReferenceValue = flowAudio;
            serializedFlow.ApplyModifiedPropertiesWithoutUndo();

            GameObject indicator = FindObjectInScene(scene, "ExitPowerIndicator");
            if (indicator == null)
            {
                indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                indicator.name = "ExitPowerIndicator";
                SceneManager.MoveGameObjectToScene(indicator, scene);
                indicator.transform.position = new Vector3(0f, 2.65f, -4.72f);
                indicator.transform.localScale = new Vector3(0.55f, 0.15f, 0.08f);
                indicator.GetComponent<Renderer>().sharedMaterial = GetOrCreateSimpleMaterial("Assets/Materials/ExitIndicatorOn.mat", "Exit Indicator On", new Color(0.15f, 1f, 0.35f));
                Object.DestroyImmediate(indicator.GetComponent<Collider>());
            }
            indicator.SetActive(false);

            RoomPowerController roomPower = powerRoot.GetComponent<RoomPowerController>();
            AudioSource powerAudio = powerRoot.GetComponent<AudioSource>();
            if (powerAudio == null) powerAudio = powerRoot.AddComponent<AudioSource>();
            powerAudio.playOnAwake = false;
            SerializedObject serializedPower = new SerializedObject(roomPower);
            serializedPower.FindProperty("exitDoor").objectReferenceValue = exitDoor;
            SerializedProperty poweredObjects = serializedPower.FindProperty("poweredObjects");
            poweredObjects.arraySize = 1;
            poweredObjects.GetArrayElementAtIndex(0).objectReferenceValue = indicator;
            serializedPower.FindProperty("audioSource").objectReferenceValue = powerAudio;
            serializedPower.ApplyModifiedPropertiesWithoutUndo();

            GameObject triggerObject = FindObjectInScene(scene, "ExitTrigger");
            if (triggerObject == null)
            {
                triggerObject = new GameObject("ExitTrigger", typeof(BoxCollider), typeof(ExitTrigger));
                SceneManager.MoveGameObjectToScene(triggerObject, scene);
                triggerObject.transform.position = new Vector3(0f, 1.1f, -5.35f);
                BoxCollider triggerCollider = triggerObject.GetComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.size = new Vector3(1.65f, 2.2f, 0.45f);
            }
            SerializedObject serializedTrigger = new SerializedObject(triggerObject.GetComponent<ExitTrigger>());
            serializedTrigger.FindProperty("gameFlowManager").objectReferenceValue = flow;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();

            CreateExitOpeningIfNeeded(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (selectTrigger)
            {
                Selection.activeGameObject = triggerObject;
                EditorGUIUtility.PingObject(triggerObject);
            }
            if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Step 12 configured: power response, Exit Door, Exit Trigger, timer, and Victory UI.");
        }

        private static void CreateExitOpeningIfNeeded(Scene scene)
        {
            GameObject oldWall = FindObjectInScene(scene, "Wall_South");
            if (oldWall == null || FindObjectInScene(scene, "Wall_South_Left") != null) return;

            Material material = oldWall.GetComponent<Renderer>() != null ? oldWall.GetComponent<Renderer>().sharedMaterial : null;
            oldWall.SetActive(false);
            Transform parent = oldWall.transform.parent;
            GameObject left = CreateCube("Wall_South_Left", parent, new Vector3(-3.525f, 1.6f, -5.1f), new Vector3(5.35f, 3.2f, 0.2f));
            GameObject right = CreateCube("Wall_South_Right", parent, new Vector3(3.525f, 1.6f, -5.1f), new Vector3(5.35f, 3.2f, 0.2f));
            GameObject top = CreateCube("Wall_South_AboveExit", parent, new Vector3(0f, 2.8f, -5.1f), new Vector3(1.7f, 0.8f, 0.2f));
            if (material != null)
            {
                left.GetComponent<Renderer>().sharedMaterial = material;
                right.GetComponent<Renderer>().sharedMaterial = material;
                top.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static void EnsureMainMenuAndBuildSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) == null)
            {
                Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                GameObject canvasRoot = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuController));
                SceneManager.MoveGameObjectToScene(canvasRoot, menuScene);
                Canvas canvas = canvasRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                background.transform.SetParent(canvasRoot.transform, false);
                RectTransform rect = background.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                background.GetComponent<Image>().color = new Color(0.02f, 0.035f, 0.05f, 1f);
                CreateText("Title", background.transform, "3D PUZZLE ROOM", new Vector2(0f, 110f), new Vector2(900f, 100f), 52, Color.white);
                CreateText("Subtitle", background.transform, "ESCAPE ROOM", new Vector2(0f, 40f), new Vector2(600f, 50f), 25, new Color(0.35f, 1f, 0.65f));
                Button play = CreateButton("Play", background.transform, "PLAY", new Vector2(0f, -80f), new Vector2(280f, 75f));
                UnityEventTools.AddPersistentListener(play.onClick, canvasRoot.GetComponent<MainMenuController>().PlayGame);
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, menuScene);
                EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);
                EditorSceneManager.CloseScene(menuScene, true);
            }

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == MainMenuScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(MainMenuScenePath, true));
            if (!scenes.Exists(item => item.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Tools/Puzzle Room/Setup Exit Door")]
        public static void SetupExitDoor()
        {
            AddDoorPrototypeIfMissing(true);
        }

        private static void AddDoorPrototypeIfMissing(bool selectDoor)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject doorRoot = FindObjectInScene(scene, "ExitDoor");
            GameObject doorVisual = FindObjectInScene(scene, "DoorVisual");

            if (doorRoot == null)
            {
                doorVisual = FindObjectInScene(scene, "ExitDoor_Placeholder");

                if (doorVisual == null)
                {
                    Debug.LogError("ExitDoor_Placeholder was not found in PuzzleRoom.");

                    if (openedForSetup)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }

                    return;
                }

                Transform puzzleObjects = doorVisual.transform.parent;
                doorRoot = new GameObject("ExitDoor");
                SceneManager.MoveGameObjectToScene(doorRoot, scene);
                doorRoot.transform.SetParent(puzzleObjects, false);
                doorRoot.transform.localPosition = new Vector3(-0.7f, 1.2f, -4.88f);

                doorVisual.name = "DoorVisual";
                doorVisual.transform.SetParent(doorRoot.transform, false);
                doorVisual.transform.localPosition = new Vector3(0.7f, 0f, 0f);
                doorVisual.transform.localRotation = Quaternion.identity;
                doorVisual.transform.localScale = new Vector3(1.4f, 2.4f, 0.15f);
            }

            Door door = doorRoot.GetComponent<Door>();

            if (door == null)
            {
                door = doorRoot.AddComponent<Door>();
            }

            SerializedObject serializedDoor = new SerializedObject(door);
            serializedDoor.FindProperty("isLocked").boolValue = true;
            serializedDoor.FindProperty("isOpen").boolValue = false;
            serializedDoor.FindProperty("doorPivot").objectReferenceValue = doorRoot.transform;
            serializedDoor.FindProperty("openAngle").floatValue = -90f;
            serializedDoor.FindProperty("openSpeed").floatValue = 120f;
            serializedDoor.FindProperty("lockedMessage").stringValue = "Locked. No power.";
            serializedDoor.FindProperty("messageDuration").floatValue = 1.2f;
            serializedDoor.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath) == null)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(
                    doorRoot,
                    DoorPrefabPath,
                    InteractionMode.AutomatedAction);
                EditorSceneManager.SaveScene(scene);
            }

            if (selectDoor)
            {
                Selection.activeGameObject = doorRoot;
                EditorGUIUtility.PingObject(doorRoot);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("Configured the locked Exit Door and created Assets/Prefabs/Door.prefab.");
        }

        [MenuItem("Tools/Puzzle Room/Setup Interaction Prototype")]
        public static void SetupInteractionPrototype()
        {
            AddInteractionPrototypeIfMissing(true);
        }

        private static void AddInteractionPrototypeIfMissing(bool selectTestObject)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject player = FindRootObject(scene, "Player");
            PlayerInteraction playerInteraction = player != null
                ? player.GetComponentInChildren<PlayerInteraction>(true)
                : null;

            GameObject uiRoot = FindRootObject(scene, "InteractionUI");
            InteractionPromptUI promptUI;

            if (uiRoot == null)
            {
                uiRoot = new GameObject("InteractionUI", typeof(InteractionPromptUI));
                SceneManager.MoveGameObjectToScene(uiRoot, scene);
                promptUI = uiRoot.GetComponent<InteractionPromptUI>();
            }
            else
            {
                promptUI = uiRoot.GetComponent<InteractionPromptUI>();
            }

            if (player != null)
            {
                Camera playerCamera = player.GetComponentInChildren<Camera>(true);

                if (playerInteraction == null && playerCamera != null)
                {
                    playerInteraction = playerCamera.gameObject.AddComponent<PlayerInteraction>();
                }

                if (playerInteraction != null)
                {
                    SerializedObject serializedInteraction = new SerializedObject(playerInteraction);
                    serializedInteraction.FindProperty("playerCamera").objectReferenceValue = playerCamera;
                    serializedInteraction.FindProperty("interactionDistance").floatValue = 2.2f;
                    serializedInteraction.FindProperty("interactionLayers").intValue = -1;
                    serializedInteraction.FindProperty("promptUI").objectReferenceValue = promptUI;
                    serializedInteraction.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GameObject testObject = FindObjectInScene(scene, "InteractionTestObject");

            // The temporary interaction cube was useful during the first prototype,
            // but the completed room now has real interactable objects. Remove it and
            // never recreate it during future automatic setup passes.
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (selectTestObject)
            {
                Selection.activeGameObject = player;
                EditorGUIUtility.PingObject(player);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("Configured the reusable interaction system and removed its obsolete test cube.");
        }

        private static GameObject FindObjectInScene(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);

                foreach (Transform candidate in transforms)
                {
                    if (candidate.name == objectName)
                    {
                        return candidate.gameObject;
                    }
                }
            }

            return null;
        }

        private static void AddPlayerToPuzzleRoomIfMissing(bool selectPlayer)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject existingPlayer = FindRootObject(scene, "Player");
            if (existingPlayer == null)
            {
                GameObject spawn = FindRootObject(scene, "PlayerSpawn");

                existingPlayer = new GameObject("Player");
                SceneManager.MoveGameObjectToScene(existingPlayer, scene);
                existingPlayer.transform.position = spawn != null
                    ? spawn.transform.position
                    : new Vector3(0f, 0f, -2.5f);

                CharacterController controller = existingPlayer.AddComponent<CharacterController>();
                controller.center = new Vector3(0f, 0.9f, 0f);
                controller.radius = 0.35f;
                controller.height = 1.8f;
                controller.slopeLimit = 45f;
                controller.stepOffset = 0.3f;
                controller.skinWidth = 0.08f;
                controller.minMoveDistance = 0f;
                existingPlayer.AddComponent<PlayerMovement>();

                GameObject cameraObject = new GameObject("PlayerCamera");
                cameraObject.transform.SetParent(existingPlayer.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                PlayerLook look = cameraObject.AddComponent<PlayerLook>();

                SerializedObject serializedLook = new SerializedObject(look);
                serializedLook.FindProperty("playerBody").objectReferenceValue = existingPlayer.transform;
                serializedLook.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Added the first-person Player and PlayerCamera to PuzzleRoom.");
            }

            if (selectPlayer)
            {
                Selection.activeGameObject = existingPlayer;
                EditorGUIUtility.PingObject(existingPlayer);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject FindRootObject(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == objectName)
                {
                    return rootObject;
                }
            }

            return null;
        }

        private static void BuildAndSaveScene()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject environment = new GameObject("Environment");
            CreateCube("Floor", environment.transform, new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 10f));
            CreateCube("Ceiling", environment.transform, new Vector3(0f, 3.3f, 0f), new Vector3(12f, 0.2f, 10f));

            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(environment.transform);
            CreateCube("Wall_North", walls.transform, new Vector3(0f, 1.6f, 5.1f), new Vector3(12.4f, 3.2f, 0.2f));
            CreateCube("Wall_South", walls.transform, new Vector3(0f, 1.6f, -5.1f), new Vector3(12.4f, 3.2f, 0.2f));
            CreateCube("Wall_East", walls.transform, new Vector3(6.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 10f));
            CreateCube("Wall_West", walls.transform, new Vector3(-6.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 10f));

            GameObject puzzleObjects = new GameObject("PuzzleObjects");
            CreateCube("ExitDoor_Placeholder", puzzleObjects.transform, new Vector3(0f, 1.2f, -4.88f), new Vector3(1.4f, 2.4f, 0.15f));
            CreateCube("Safe_Placeholder", puzzleObjects.transform, new Vector3(-4.6f, 0.6f, 3.9f), new Vector3(1.2f, 1.2f, 0.8f));

            GameObject paintings = new GameObject("Paintings_Placeholders");
            paintings.transform.SetParent(puzzleObjects.transform);
            CreateCube("Painting_Left", paintings.transform, new Vector3(-1.8f, 2f, 4.88f), new Vector3(1.2f, 1.2f, 0.08f));
            CreateCube("Painting_Center", paintings.transform, new Vector3(0f, 2f, 4.88f), new Vector3(1.2f, 1.2f, 0.08f));
            CreateCube("Painting_Right", paintings.transform, new Vector3(1.8f, 2f, 4.88f), new Vector3(1.2f, 1.2f, 0.08f));

            CreateCube("Bookshelf_Placeholder", puzzleObjects.transform, new Vector3(5.68f, 1.2f, 1.5f), new Vector3(0.6f, 2.4f, 3f));
            CreateCube("Cabinet_Placeholder", puzzleObjects.transform, new Vector3(-5.68f, 0.9f, -2.5f), new Vector3(0.6f, 1.8f, 1.8f));
            CreateCube("FuseBox_Placeholder", puzzleObjects.transform, new Vector3(-5.88f, 1.6f, 2.5f), new Vector3(0.2f, 0.8f, 0.7f));

            GameObject playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.position = new Vector3(0f, 0f, -2.5f);
            playerSpawn.transform.rotation = Quaternion.identity;

            GameObject lighting = new GameObject("Lighting");
            GameObject roomLight = new GameObject("Room Light");
            roomLight.transform.SetParent(lighting.transform);
            roomLight.transform.position = new Vector3(0f, 2.8f, 0f);
            Light lightComponent = roomLight.AddComponent<Light>();
            lightComponent.type = LightType.Point;
            lightComponent.range = 14f;
            lightComponent.intensity = 2f;
            lightComponent.shadows = LightShadows.Soft;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            Debug.Log($"Created Puzzle Room prototype scene at {ScenePath}.");
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            return cube;
        }
    }
}
#endif
