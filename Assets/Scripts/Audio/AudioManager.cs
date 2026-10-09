using System;
using System.Collections;
using System.Collections.Generic;
using PuzzleRoom.Core;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace PuzzleRoom.Audio
{
    [Serializable]
    public sealed class AudioCueDefinition
    {
        public AudioCue cue;
        public AudioCategory category = AudioCategory.Sfx;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float minPitch = 1f;
        [Range(0.5f, 1.5f)] public float maxPitch = 1f;
        [Range(0f, 1f)] public float spatialBlend;
    }

    /// <summary>Persistent pooled audio service with category volumes and fades.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioCueDefinition[] library;
        [SerializeField, Min(4)] private int oneShotPoolSize = 16;
        [SerializeField] private string gameplaySceneName = "PuzzleRoom";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Optional Audio Mixer routing")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [SerializeField] private AudioMixerGroup uiGroup;

        private readonly Dictionary<AudioCue, AudioCueDefinition> definitions = new();
        private AudioSource[] oneShotPool;
        private int poolIndex;
        private AudioSource musicSource;
        private AudioSource ambienceSource;
        private Coroutine musicFade;
        private Coroutine ambienceFade;

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildLibrary();
            BuildSources();
            LoadSavedVolumes();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            PowerState.PowerRestored += HandlePowerRestored;
        }

        private void Start() => HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            PowerState.PowerRestored -= HandlePowerRestored;
        }

        public void Play(AudioCue cue, Vector3? worldPosition = null)
        {
            if (!definitions.TryGetValue(cue, out AudioCueDefinition definition) ||
                definition.clips == null || definition.clips.Length == 0) return;

            AudioClip clip = definition.clips[UnityEngine.Random.Range(0, definition.clips.Length)];
            if (clip == null) return;

            AudioSource source = NextSource();
            source.transform.position = worldPosition ?? transform.position;
            source.spatialBlend = worldPosition.HasValue ? Mathf.Max(.75f, definition.spatialBlend) : definition.spatialBlend;
            source.volume = definition.volume * GetCategoryLinearVolume(definition.category);
            source.pitch = UnityEngine.Random.Range(definition.minPitch, definition.maxPitch);
            source.outputAudioMixerGroup = GetGroup(definition.category);
            source.PlayOneShot(clip);
        }

        public void PlayGameplayAudio()
        {
            FadeLoop(musicSource, AudioCue.GameplayMusic, .8f, ref musicFade);
            FadeLoop(ambienceSource, PowerState.IsRestored ? AudioCue.PoweredAmbience : AudioCue.RoomAmbience, 1.2f, ref ambienceFade);
        }

        public void PlayVictoryAudio()
        {
            StopLoop(musicSource, .5f, ref musicFade);
            FadeLoop(musicSource, AudioCue.Victory, .25f, ref musicFade, false);
        }

        public void SetMasterVolume(float linear) => SetVolume("MasterVolume", "Audio.Master", linear);
        public void SetMusicVolume(float linear) => SetVolume("MusicVolume", "Audio.Music", linear);
        public void SetSfxVolume(float linear) => SetVolume("SFXVolume", "Audio.SFX", linear);
        public void SetAmbienceVolume(float linear) => SetVolume("AmbienceVolume", "Audio.Ambience", linear);
        public void SetUIVolume(float linear) => SetVolume("UIVolume", "Audio.UI", linear);

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == gameplaySceneName) PlayGameplayAudio();
            else if (scene.name == mainMenuSceneName)
            {
                FadeLoop(musicSource, AudioCue.MenuMusic, .8f, ref musicFade);
                StopLoop(ambienceSource, .4f, ref ambienceFade);
            }
            else
            {
                StopLoop(musicSource, .4f, ref musicFade);
                StopLoop(ambienceSource, .4f, ref ambienceFade);
            }
        }

        private void HandlePowerRestored()
        {
            FadeLoop(ambienceSource, AudioCue.PoweredAmbience, 1.2f, ref ambienceFade);
        }

        private void BuildLibrary()
        {
            definitions.Clear();
            if (library == null) return;
            foreach (AudioCueDefinition item in library)
                if (item != null) definitions[item.cue] = item;
        }

        private void BuildSources()
        {
            oneShotPool = new AudioSource[Mathf.Max(4, oneShotPoolSize)];
            for (int i = 0; i < oneShotPool.Length; i++)
            {
                GameObject child = new GameObject($"OneShot_{i + 1:00}");
                child.transform.SetParent(transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = 12f;
                oneShotPool[i] = source;
            }
            musicSource = CreateLoopSource("MusicLoop", musicGroup);
            ambienceSource = CreateLoopSource("AmbienceLoop", ambienceGroup);
        }

        private AudioSource CreateLoopSource(string name, AudioMixerGroup group)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private AudioSource NextSource()
        {
            AudioSource result = oneShotPool[poolIndex];
            poolIndex = (poolIndex + 1) % oneShotPool.Length;
            return result;
        }

        private void FadeLoop(AudioSource source, AudioCue cue, float duration, ref Coroutine routine, bool loop = true)
        {
            if (!definitions.TryGetValue(cue, out AudioCueDefinition definition) || definition.clips == null || definition.clips.Length == 0) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FadeToClip(source, definition, duration, loop));
        }

        private IEnumerator FadeToClip(AudioSource source, AudioCueDefinition definition, float duration, bool loop)
        {
            float start = source.volume;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            source.clip = definition.clips[0];
            source.loop = loop;
            source.outputAudioMixerGroup = GetGroup(definition.category);
            source.volume = 0f;
            source.Play();
            float target = definition.volume * GetCategoryLinearVolume(definition.category);
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, target, t / duration);
                yield return null;
            }
            source.volume = target;
        }

        private void StopLoop(AudioSource source, float duration, ref Coroutine routine)
        {
            if (source == null || !source.isPlaying) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FadeOut(source, duration));
        }

        private static IEnumerator FadeOut(AudioSource source, float duration)
        {
            float start = source.volume;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            source.Stop();
        }

        private void SetVolume(string parameter, string key, float linear)
        {
            linear = Mathf.Clamp01(linear);
            PlayerPrefs.SetFloat(key, linear);
            if (audioMixer != null) audioMixer.SetFloat(parameter, LinearToDb(linear));
            RefreshLoopVolumes();
        }

        private void LoadSavedVolumes()
        {
            SetMasterVolume(PlayerPrefs.GetFloat("Audio.Master", 1f));
            SetMusicVolume(PlayerPrefs.GetFloat("Audio.Music", .65f));
            SetSfxVolume(PlayerPrefs.GetFloat("Audio.SFX", .85f));
            SetAmbienceVolume(PlayerPrefs.GetFloat("Audio.Ambience", .45f));
            SetUIVolume(PlayerPrefs.GetFloat("Audio.UI", .8f));
        }

        private void RefreshLoopVolumes()
        {
            if (musicSource != null) musicSource.volume = GetCategoryLinearVolume(AudioCategory.Music);
            if (ambienceSource != null) ambienceSource.volume = GetCategoryLinearVolume(AudioCategory.Ambience);
        }

        private static float LinearToDb(float linear) => linear <= .0001f ? -80f : Mathf.Log10(linear) * 20f;
        private static float GetCategoryLinearVolume(AudioCategory category) =>
            PlayerPrefs.GetFloat(category switch
            {
                AudioCategory.Music => "Audio.Music",
                AudioCategory.Ambience => "Audio.Ambience",
                AudioCategory.UI => "Audio.UI",
                _ => "Audio.SFX"
            }, 1f) * PlayerPrefs.GetFloat("Audio.Master", 1f);

        private AudioMixerGroup GetGroup(AudioCategory category) => category switch
        {
            AudioCategory.Music => musicGroup,
            AudioCategory.Ambience => ambienceGroup,
            AudioCategory.UI => uiGroup,
            _ => sfxGroup
        };
    }

    public static class GameAudio
    {
        public static void Play(AudioCue cue) => AudioManager.Instance?.Play(cue);
        public static void PlayAt(AudioCue cue, Vector3 position) => AudioManager.Instance?.Play(cue, position);
    }
}
