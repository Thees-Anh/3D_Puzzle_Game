#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuzzleRoom.EditorTools
{
    [InitializeOnLoad]
    public static class AndroidProjectConfigurator
    {
        private const string ConfiguredKey = "PuzzleRoom.AndroidConfig.V1";

        static AndroidProjectConfigurator() => EditorApplication.delayCall += ConfigureOnce;

        private static void ConfigureOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(ConfiguredKey, false)) return;
            ConfigureAndroid();
            SessionState.SetBool(ConfiguredKey, true);
        }

        [MenuItem("Tools/Puzzle Room/Android/Configure Project")]
        public static void ConfigureAndroid()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;

            ConfigureLongAudio("Assets/Audio/Music/Gameplay/mystery_loop.wav");
            ConfigureLongAudio("Assets/Audio/Music/Victory/victory_theme.wav");
            ConfigureLongAudio("Assets/Audio/Ambience/room_dark.wav");
            ConfigureLongAudio("Assets/Audio/Ambience/room_powered.wav");

            AssetDatabase.SaveAssets();
            Debug.Log("Android project configured: landscape, API 23+, IL2CPP, ARM64, OpenGLES3, APK testing, and streamed long audio.");
        }

        [MenuItem("Tools/Puzzle Room/Android/Build Development APK")]
        public static void BuildDevelopmentApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Android Build Support is not installed for this Unity Editor version.");
                return;
            }

            ConfigureAndroid();
            string outputDirectory = Path.GetFullPath("Builds/Android");
            Directory.CreateDirectory(outputDirectory);
            string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/PuzzleRoom.unity" };
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, "PuzzleRoom-Development.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"Development APK built: {options.locationPathName}");
            else
                Debug.LogError($"Android build failed: {report.summary.result}. Check the Build Report and Console.");
        }

        private static void ConfigureLongAudio(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .7f;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
