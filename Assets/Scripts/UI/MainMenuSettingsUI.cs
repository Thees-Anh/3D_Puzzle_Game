using PuzzleRoom.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.UI
{
    public sealed class MainMenuSettingsUI : MonoBehaviour
    {
        public const string TouchSensitivityKey = "Controls.TouchLookSensitivity";
        public const string QualityKey = "Graphics.QualityLevel";

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Text sensitivityValue;
        [SerializeField] private Slider qualitySlider;
        [SerializeField] private Text qualityValue;

        private static readonly int[] QualityLevels = { 1, 2, 3 };

        private void Awake()
        {
            ApplySavedSettings();
        }

        public void ApplySavedSettings()
        {
            LoadValues();
            Apply(false);
        }

        public void Open()
        {
            LoadValues();
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Close() => CloseImmediate();

        public void CloseImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        public void ApplyFromUI() => Apply(true);

        public void UpdateSensitivityLabel()
        {
            if (sensitivityValue != null && sensitivitySlider != null)
                sensitivityValue.text = sensitivitySlider.value.ToString("0.00");
        }

        public void UpdateSensitivityLabel(float _) => UpdateSensitivityLabel();

        private void LoadValues()
        {
            masterSlider.value = PlayerPrefs.GetFloat("Audio.Master", 1f);
            musicSlider.value = PlayerPrefs.GetFloat("Audio.Music", .65f);
            sfxSlider.value = PlayerPrefs.GetFloat("Audio.SFX", .85f);
            sensitivitySlider.value = PlayerPrefs.GetFloat(TouchSensitivityKey, .12f);
            int savedQuality = PlayerPrefs.GetInt(QualityKey, 2);
            qualitySlider.value = savedQuality <= 1 ? 0 : savedQuality >= 3 ? 2 : 1;
            UpdateSensitivityLabel();
            UpdateQualityLabel();
        }

        private void Apply(bool save)
        {
            PlayerPrefs.SetFloat("Audio.Master", masterSlider.value);
            PlayerPrefs.SetFloat("Audio.Music", musicSlider.value);
            PlayerPrefs.SetFloat("Audio.SFX", sfxSlider.value);
            PlayerPrefs.SetFloat(TouchSensitivityKey, sensitivitySlider.value);
            int quality = QualityLevels[Mathf.Clamp(Mathf.RoundToInt(qualitySlider.value), 0, QualityLevels.Length - 1)];
            PlayerPrefs.SetInt(QualityKey, quality);
            QualitySettings.SetQualityLevel(quality, true);
            AudioManager.Instance?.SetMasterVolume(masterSlider.value);
            AudioManager.Instance?.SetMusicVolume(musicSlider.value);
            AudioManager.Instance?.SetSfxVolume(sfxSlider.value);
            UpdateSensitivityLabel();
            UpdateQualityLabel();
            if (save) PlayerPrefs.Save();
        }

        public void UpdateQualityLabel()
        {
            if (qualityValue == null || qualitySlider == null) return;
            qualityValue.text = Mathf.RoundToInt(qualitySlider.value) switch
            {
                0 => "LOW",
                2 => "HIGH",
                _ => "MEDIUM"
            };
        }

        public void UpdateQualityLabel(float _) => UpdateQualityLabel();
    }
}
