using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PuzzleRoom.Interaction;
using PuzzleRoom.Player;
using PuzzleRoom.Puzzles;
using PuzzleRoom.Puzzles.Hanoi;
using PuzzleRoom.Puzzles.PowerGrid;
using PuzzleRoom.Puzzles.SymbolUnlock;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleRoom.Core
{
    public static class SaveGameSystem
    {
        public const int CurrentVersion = 1;
        private const string FileName = "puzzle-room-save.json";
        private static bool loadRequested;
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasValidSave
        {
            get
            {
                try
                {
                    if (!File.Exists(SavePath)) return false;
                    SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                    return data != null && data.version == CurrentVersion && data.sceneName == "PuzzleRoom";
                }
                catch { return false; }
            }
        }

        public static bool SaveCurrentGame()
        {
            if (SceneManager.GetActiveScene().name != "PuzzleRoom") return false;
            try
            {
                SaveData data = Capture();
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(temp, SavePath);
                Debug.Log($"Game saved: {SavePath}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Save failed: {exception.Message}");
                return false;
            }
        }

        public static void RequestContinue()
        {
            loadRequested = HasValidSave;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (loadRequested) SceneManager.sceneLoaded += HandleSceneLoaded;
        }
        public static void DeleteSave()
        {
            loadRequested = false;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            loadRequested = false;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!loadRequested || scene.name != "PuzzleRoom") return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            new GameObject("SAVE_LoadCoordinator").AddComponent<SaveLoadCoordinator>();
        }

        private static SaveData Capture()
        {
            PlayerInventory inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            PlayerLook look = UnityEngine.Object.FindFirstObjectByType<PlayerLook>();
            GameTimer timer = UnityEngine.Object.FindFirstObjectByType<GameTimer>();
            HanoiPuzzleManager hanoi = UnityEngine.Object.FindFirstObjectByType<HanoiPuzzleManager>();
            CircuitGridManager grid = UnityEngine.Object.FindFirstObjectByType<CircuitGridManager>();
            FinalPowerPuzzleManager final = UnityEngine.Object.FindFirstObjectByType<FinalPowerPuzzleManager>();
            FuseBox fuseBox = UnityEngine.Object.FindFirstObjectByType<FuseBox>();
            SafePuzzle safe = UnityEngine.Object.FindFirstObjectByType<SafePuzzle>();
            SymbolSequencePuzzle symbols = UnityEngine.Object.FindFirstObjectByType<SymbolSequencePuzzle>();

            SaveData data = new()
            {
                version = CurrentVersion,
                sceneName = "PuzzleRoom",
                savedUtc = DateTime.UtcNow.ToString("O"),
                playerPosition = inventory != null ? inventory.transform.position : Vector3.zero,
                playerRotation = inventory != null ? inventory.transform.rotation : Quaternion.identity,
                cameraPitch = look != null ? look.VerticalLookRotation : 0f,
                inventoryItems = inventory != null ? inventory.OwnedItems.Select(item => (int)item).ToArray() : Array.Empty<int>(),
                selectedSlot = inventory != null ? inventory.SelectedSlotIndex : 0,
                elapsedSeconds = timer != null ? timer.ElapsedSeconds : 0f,
                powerRestored = PowerState.IsRestored,
                safeCompleted = safe != null && safe.IsCompleted,
                symbolsCompleted = symbols != null && symbols.IsCompleted,
                hanoiUnlocked = hanoi != null && hanoi.IsUnlocked,
                hanoiCompleted = hanoi != null && hanoi.IsCompleted,
                hanoiMoves = hanoi != null ? hanoi.MoveCount : 0,
                hanoiPegs = hanoi != null ? hanoi.CapturePegIndices() : Array.Empty<int>(),
                gridEnabled = grid != null && grid.IsEnabled,
                gridSolved = grid != null && grid.IsSolved,
                gridRotations = grid != null ? grid.Rotations : 0,
                tileRotations = grid != null ? grid.CaptureRotations() : Array.Empty<int>(),
                finalFuseInserted = final != null && final.FuseInserted,
                routingSolved = final != null && final.CircuitRoutingSolved,
                activationProgress = final != null ? final.ActivationProgress : 0,
                finalCompleted = final != null && final.IsCompleted,
                fuseBoxInserted = fuseBox != null && fuseBox.FuseInserted,
                pickups = SceneObjects<WorldItemPickup>().Select(item => new ActiveRecord { path = PathOf(item.transform), active = item.gameObject.activeSelf }).ToArray(),
                doors = SceneObjects<Door>().Select(door => new DoorRecord { path = PathOf(door.transform), locked = door.IsLocked, open = door.IsOpen }).ToArray(),
                cabinets = SceneObjects<Cabinet>().Select(cabinet => new CabinetRecord { path = PathOf(cabinet.transform), locked = cabinet.IsLocked, open = cabinet.IsOpen, revealed = cabinet.ContentsRevealed }).ToArray()
            };
            return data;
        }

        internal static bool ApplyRequestedSave()
        {
            if (!loadRequested || !HasValidSave) return false;
            loadRequested = false;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                Apply(data);
                Debug.Log("Saved game restored successfully.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Load failed: {exception.Message}");
                return false;
            }
        }

        private static void Apply(SaveData data)
        {
            PlayerInventory inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            CharacterController controller = inventory != null ? inventory.GetComponent<CharacterController>() : null;
            if (controller != null) controller.enabled = false;
            if (inventory != null) inventory.transform.SetPositionAndRotation(data.playerPosition, data.playerRotation);
            if (controller != null) controller.enabled = true;
            inventory?.RestoreItems(data.inventoryItems.Select(value => (ItemId)value).ToArray(), data.selectedSlot);
            UnityEngine.Object.FindFirstObjectByType<PlayerLook>()?.RestoreView(data.cameraPitch);
            UnityEngine.Object.FindFirstObjectByType<GameTimer>()?.Restore(data.elapsedSeconds);

            foreach (DoorRecord record in data.doors ?? Array.Empty<DoorRecord>()) FindPath(record.path)?.GetComponent<Door>()?.RestoreState(record.locked, record.open);
            foreach (CabinetRecord record in data.cabinets ?? Array.Empty<CabinetRecord>()) FindPath(record.path)?.GetComponent<Cabinet>()?.RestoreState(record.locked, record.open, record.revealed);

            UnityEngine.Object.FindFirstObjectByType<SafePuzzle>()?.RestoreState(data.safeCompleted);
            HanoiPuzzleManager hanoi = UnityEngine.Object.FindFirstObjectByType<HanoiPuzzleManager>();
            hanoi?.RestoreState(data.hanoiUnlocked, data.hanoiCompleted, data.hanoiMoves, data.hanoiPegs);
            UnityEngine.Object.FindFirstObjectByType<SymbolSequencePuzzle>()?.RestoreState(data.symbolsCompleted);
            CircuitGridManager grid = UnityEngine.Object.FindFirstObjectByType<CircuitGridManager>();
            grid?.RestoreState(data.gridEnabled, data.gridSolved, data.gridRotations, data.tileRotations);
            UnityEngine.Object.FindFirstObjectByType<FinalPowerPuzzleManager>()?.RestoreState(data.finalFuseInserted, data.routingSolved, data.activationProgress, data.finalCompleted);
            UnityEngine.Object.FindFirstObjectByType<FuseBox>()?.RestoreState(data.fuseBoxInserted);
            if (data.powerRestored) PowerState.Restore();

            // Restore pickups last because completed puzzles may activate their reward objects.
            foreach (ActiveRecord record in data.pickups ?? Array.Empty<ActiveRecord>())
                if (FindPath(record.path) is Transform target) target.gameObject.SetActive(record.active);
        }

        private static IEnumerable<T> SceneObjects<T>() where T : Component =>
            Resources.FindObjectsOfTypeAll<T>().Where(item => item.gameObject.scene == SceneManager.GetActiveScene());

        private static string PathOf(Transform transform)
        {
            List<string> parts = new();
            while (transform != null) { parts.Add(transform.name); transform = transform.parent; }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static Transform FindPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string[] parts = path.Split('/');
            GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(item => item.name == parts[0]);
            Transform current = root != null ? root.transform : null;
            for (int i = 1; current != null && i < parts.Length; i++) current = current.Find(parts[i]);
            return current;
        }

        [Serializable] private sealed class SaveData
        {
            public int version; public string sceneName; public string savedUtc;
            public Vector3 playerPosition; public Quaternion playerRotation; public float cameraPitch;
            public int[] inventoryItems; public int selectedSlot; public float elapsedSeconds; public bool powerRestored;
            public bool safeCompleted; public bool symbolsCompleted; public bool hanoiUnlocked; public bool hanoiCompleted; public int hanoiMoves; public int[] hanoiPegs;
            public bool gridEnabled; public bool gridSolved; public int gridRotations; public int[] tileRotations;
            public bool finalFuseInserted; public bool routingSolved; public int activationProgress; public bool finalCompleted; public bool fuseBoxInserted;
            public ActiveRecord[] pickups; public DoorRecord[] doors; public CabinetRecord[] cabinets;
        }
        [Serializable] private sealed class ActiveRecord { public string path; public bool active; }
        [Serializable] private sealed class DoorRecord { public string path; public bool locked; public bool open; }
        [Serializable] private sealed class CabinetRecord { public string path; public bool locked; public bool open; public bool revealed; }
    }

    [DefaultExecutionOrder(10000)]
    internal sealed class SaveLoadCoordinator : MonoBehaviour
    {
        private void Start()
        {
            SaveGameSystem.ApplyRequestedSave();
            Destroy(gameObject);
        }
    }
}
