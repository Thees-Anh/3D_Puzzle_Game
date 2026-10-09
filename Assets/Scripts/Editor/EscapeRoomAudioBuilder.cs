#if UNITY_EDITOR
using System.Collections.Generic;
using PuzzleRoom.Audio;
using PuzzleRoom.Player;
using PuzzleRoom.Puzzles.PowerGrid;
using PuzzleRoom.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class EscapeRoomAudioBuilder
    {
        private const string ScenePath = "Assets/Scenes/PuzzleRoom.unity";
        private const string AudioRootName = "AUDIO_System_v1";
        private const string ConfigMarker = "AUDIO_Config_v3";

        private sealed class CueSetup
        {
            public AudioCue cue; public AudioCategory category; public string[] paths;
            public float volume, minPitch, maxPitch, spatial;
            public CueSetup(AudioCue cue, AudioCategory category, float volume, float minPitch, float maxPitch, float spatial, params string[] paths)
            { this.cue=cue; this.category=category; this.volume=volume; this.minPitch=minPitch; this.maxPitch=maxPitch; this.spatial=spatial; this.paths=paths; }
        }

        static EscapeRoomAudioBuilder() => EditorApplication.delayCall += AutoBuild;

        private static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool ready = Find(scene, ConfigMarker) != null;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (!ready) Build(false);
        }

        [MenuItem("Tools/Puzzle Room/Audio/Build Complete Audio System")]
        public static void BuildMenu() => Build(true);

        private static void Build(bool select)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            AssetDatabase.Refresh();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject root = Find(scene, AudioRootName);
            if (root == null)
            {
                root = new GameObject(AudioRootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            AudioManager manager = root.GetComponent<AudioManager>();
            if (manager == null) manager = root.AddComponent<AudioManager>();
            ConfigureLibrary(manager);

            GameObject player = Find(scene, "Player");
            if (player != null && player.GetComponent<PlayerFootsteps>() == null)
                player.AddComponent<PlayerFootsteps>();

            AddButtonAudio(scene);

            if (Find(scene, ConfigMarker) == null)
            {
                GameObject marker = new GameObject(ConfigMarker);
                marker.transform.SetParent(root.transform, false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (select) Selection.activeGameObject = root;
            if (opened) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Complete audio library, pooled AudioManager, footsteps, ambience, music, and UI audio configured.");
        }

        private static void ConfigureLibrary(AudioManager manager)
        {
            List<CueSetup> cues = BuildCues();
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty library = so.FindProperty("library");
            library.arraySize = cues.Count;
            for (int i = 0; i < cues.Count; i++)
            {
                CueSetup cue = cues[i];
                SerializedProperty item = library.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("cue").enumValueIndex = (int)cue.cue;
                item.FindPropertyRelative("category").enumValueIndex = (int)cue.category;
                item.FindPropertyRelative("volume").floatValue = cue.volume;
                item.FindPropertyRelative("minPitch").floatValue = cue.minPitch;
                item.FindPropertyRelative("maxPitch").floatValue = cue.maxPitch;
                item.FindPropertyRelative("spatialBlend").floatValue = cue.spatial;
                SerializedProperty clips = item.FindPropertyRelative("clips");
                clips.arraySize = cue.paths.Length;
                for (int c = 0; c < cue.paths.Length; c++)
                    clips.GetArrayElementAtIndex(c).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(cue.paths[c]);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<CueSetup> BuildCues()
        {
            const string A="Assets/Audio/";
            CueSetup C(AudioCue c, AudioCategory g, float v, string p, float a=.96f, float b=1.04f, float s=0f) => new(c,g,v,a,b,s,A+p);
            var x = new List<CueSetup>
            {
                C(AudioCue.UIHover,AudioCategory.UI,.32f,"SFX/UI/ui_hover.wav"), C(AudioCue.UIClick,AudioCategory.UI,.55f,"SFX/UI/ui_click.wav"),
                C(AudioCue.UIOpen,AudioCategory.UI,.5f,"SFX/UI/ui_open.wav"), C(AudioCue.UIClose,AudioCategory.UI,.45f,"SFX/UI/ui_close.wav"),
                C(AudioCue.Pause,AudioCategory.UI,.55f,"SFX/UI/ui_pause.wav"), C(AudioCue.Resume,AudioCategory.UI,.55f,"SFX/UI/ui_resume.wav"),
                C(AudioCue.Confirm,AudioCategory.UI,.65f,"SFX/UI/confirm.wav"), C(AudioCue.Error,AudioCategory.UI,.65f,"SFX/UI/error.wav"),
                new CueSetup(AudioCue.FootstepWood,AudioCategory.Sfx,.28f,.94f,1.05f,0f,A+"SFX/Player/footstep_wood_1.wav",A+"SFX/Player/footstep_wood_2.wav",A+"SFX/Player/footstep_wood_3.wav"),
                C(AudioCue.ChairSit,AudioCategory.Sfx,.48f,"SFX/Hanoi/book_place.wav",.88f,.94f,1f), C(AudioCue.ChairStand,AudioCategory.Sfx,.42f,"SFX/Hanoi/book_select.wav",.9f,.98f,1f),
                C(AudioCue.PickupUV,AudioCategory.Sfx,.65f,"SFX/Interaction/pickup_uv.wav",.96f,1.04f,1f), C(AudioCue.PickupKey,AudioCategory.Sfx,.68f,"SFX/Interaction/pickup_key.wav",.96f,1.04f,1f), C(AudioCue.PickupFuse,AudioCategory.Sfx,.65f,"SFX/Interaction/pickup_fuse.wav",.96f,1.04f,1f),
                C(AudioCue.UVOn,AudioCategory.Sfx,.5f,"SFX/Interaction/switch_on.wav"), C(AudioCue.UVOff,AudioCategory.Sfx,.45f,"SFX/Interaction/switch_off.wav"),
                C(AudioCue.SafeKey,AudioCategory.UI,.46f,"SFX/Safe/keypad.wav",.96f,1.04f), C(AudioCue.SafeClear,AudioCategory.UI,.45f,"SFX/UI/ui_close.wav"), C(AudioCue.SafeSubmit,AudioCategory.UI,.48f,"SFX/UI/ui_click.wav"), C(AudioCue.SafeWrong,AudioCategory.UI,.7f,"SFX/UI/error.wav"), C(AudioCue.SafeCorrect,AudioCategory.UI,.7f,"SFX/UI/confirm.wav"),
                C(AudioCue.SafeUnlock,AudioCategory.Sfx,.78f,"SFX/Safe/safe_unlock.wav",.98f,1.02f,1f), C(AudioCue.SafeDoor,AudioCategory.Sfx,.72f,"SFX/Safe/safe_door.wav",.98f,1.02f,1f),
                C(AudioCue.SymbolPress,AudioCategory.Sfx,.55f,"SFX/Symbols/symbol_press.wav",.94f,1.06f,1f), C(AudioCue.SymbolWrong,AudioCategory.UI,.62f,"SFX/UI/error.wav"), C(AudioCue.SymbolComplete,AudioCategory.Sfx,.78f,"SFX/Symbols/symbol_complete.wav"), C(AudioCue.MechanismUnlock,AudioCategory.Sfx,.78f,"SFX/Hanoi/mechanism_unlock.wav",.98f,1.02f,1f),
                C(AudioCue.BookSelect,AudioCategory.Sfx,.46f,"SFX/Hanoi/book_select.wav",.92f,1.08f,1f), C(AudioCue.BookPlace,AudioCategory.Sfx,.55f,"SFX/Hanoi/book_place.wav",.92f,1.08f,1f), C(AudioCue.InvalidMove,AudioCategory.UI,.52f,"SFX/UI/error.wav"), C(AudioCue.PuzzleReset,AudioCategory.UI,.52f,"SFX/UI/ui_close.wav"), C(AudioCue.HanoiComplete,AudioCategory.Sfx,.8f,"SFX/Symbols/symbol_complete.wav"), C(AudioCue.DrawerOpen,AudioCategory.Sfx,.67f,"SFX/Hanoi/drawer_open.wav",.98f,1.02f,1f),
                C(AudioCue.CabinetLocked,AudioCategory.Sfx,.58f,"SFX/Door/door_locked.wav",.96f,1.04f,1f), C(AudioCue.CabinetUnlock,AudioCategory.Sfx,.7f,"SFX/Cabinet/key_unlock.wav",.98f,1.02f,1f), C(AudioCue.CabinetOpen,AudioCategory.Sfx,.6f,"SFX/Cabinet/cabinet_open.wav",.98f,1.02f,1f), C(AudioCue.CabinetClose,AudioCategory.Sfx,.62f,"SFX/Door/door_close.wav",.98f,1.02f,1f),
                C(AudioCue.FuseInsert,AudioCategory.Sfx,.75f,"SFX/Electrical/fuse_insert.wav",.98f,1.02f,1f),
                new CueSetup(AudioCue.CircuitRotate,AudioCategory.UI,.38f,.94f,1.06f,0f,A+"SFX/Electrical/circuit_click_1.wav",A+"SFX/Electrical/circuit_click_2.wav",A+"SFX/Electrical/circuit_click_3.wav"),
                C(AudioCue.GridComplete,AudioCategory.Sfx,.78f,"SFX/Electrical/grid_complete.wav"), C(AudioCue.OutputActivate,AudioCategory.UI,.58f,"SFX/Electrical/output_activate.wav",.92f,1.08f), C(AudioCue.WrongActivation,AudioCategory.UI,.68f,"SFX/UI/error.wav"), C(AudioCue.PowerRestored,AudioCategory.Sfx,.85f,"SFX/Electrical/power_restored.wav"),
                C(AudioCue.DoorLocked,AudioCategory.Sfx,.6f,"SFX/Door/door_locked.wav",.96f,1.04f,1f), C(AudioCue.DoorOpen,AudioCategory.Sfx,.68f,"SFX/Door/door_open.wav",.98f,1.02f,1f), C(AudioCue.DoorClose,AudioCategory.Sfx,.7f,"SFX/Door/door_close.wav",.98f,1.02f,1f), C(AudioCue.ExitUnlock,AudioCategory.Sfx,.72f,"SFX/Door/lock_release.wav",.98f,1.02f,1f),
                C(AudioCue.RoomAmbience,AudioCategory.Ambience,.34f,"Ambience/room_dark.wav",1,1), C(AudioCue.PoweredAmbience,AudioCategory.Ambience,.30f,"Ambience/room_powered.wav",1,1), C(AudioCue.GameplayMusic,AudioCategory.Music,.28f,"Music/Gameplay/mystery_loop.wav",1,1), C(AudioCue.Victory,AudioCategory.Music,.72f,"Music/Victory/victory_theme.wav",1,1)
            };
            return x;
        }

        private static void AddButtonAudio(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                // Circuit tiles already have a dedicated rotation cue; hover audio
                // across 36 tiles would be noisy, so leave those untouched.
                if (button.GetComponent<CircuitTile>() != null) continue;
                UIButtonAudio audio = button.GetComponent<UIButtonAudio>();
                if (audio == null) audio = button.gameObject.AddComponent<UIButtonAudio>();
                bool hasDedicatedClick = button.GetComponentInParent<KeypadUI>(true) != null ||
                                         button.GetComponentInParent<FinalPowerPuzzleUI>(true) != null;
                SerializedObject serialized = new SerializedObject(audio);
                // Keypad and Final Power buttons must be silent on hover. Their
                // specific action methods play the appropriate cue on actual click.
                serialized.FindProperty("playHover").boolValue = !hasDedicatedClick;
                serialized.FindProperty("playClick").boolValue = !hasDedicatedClick;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
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
