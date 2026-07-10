using UnityEngine;
using UnityEngine.Audio;

namespace Game.UI
{
    /// <summary>
    /// Central persistence + application point for game settings, shared by the pause-menu Options
    /// and the main-menu Options (they are the same screen, just on different backgrounds).
    ///
    /// Stores everything in PlayerPrefs and applies it on startup so settings survive scene loads
    /// and restarts. Audio routes through an <see cref="AudioMixer"/> via exposed parameters
    /// ("MasterVolume", "MusicVolume", "SfxVolume"). Keyboard rebinds are handled separately by
    /// <see cref="KeyboardRebindManager"/> (saved as Input System binding-override JSON).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SettingsService : MonoBehaviour
    {
        public static SettingsService Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterParam = "MasterVolume";
        [SerializeField] private string musicParam = "MusicVolume";
        [SerializeField] private string sfxParam = "SfxVolume";

        [Header("Video")]
        [Tooltip("Full-screen black overlay used to fake brightness (alpha = 1 - brightness). " +
                 "Lives on a DontDestroyOnLoad canvas so it persists across scenes.")]
        [SerializeField] private CanvasGroup brightnessOverlay;

        // PlayerPrefs keys.
        private const string KeyMaster = "set_vol_master";
        private const string KeyMusic = "set_vol_music";
        private const string KeySfx = "set_vol_sfx";
        private const string KeyBrightness = "set_brightness";
        private const string KeyFullscreen = "set_fullscreen";
        private const string KeyResWidth = "set_res_w";
        private const string KeyResHeight = "set_res_h";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ApplyAll();
        }

        public void ApplyAll()
        {
            SetMasterVolume(GetMasterVolume());
            SetMusicVolume(GetMusicVolume());
            SetSfxVolume(GetSfxVolume());
            SetBrightness(GetBrightness());
            ApplyVideoMode(GetResolutionWidth(), GetResolutionHeight(), GetFullscreen());
        }

        // ---- Audio (slider value is linear 0..1; mixer wants dB) -------------------------------

        public float GetMasterVolume() => PlayerPrefs.GetFloat(KeyMaster, 1f);
        public float GetMusicVolume() => PlayerPrefs.GetFloat(KeyMusic, 1f);
        public float GetSfxVolume() => PlayerPrefs.GetFloat(KeySfx, 1f);

        public void SetMasterVolume(float v) => ApplyVolume(KeyMaster, masterParam, v);
        public void SetMusicVolume(float v) => ApplyVolume(KeyMusic, musicParam, v);
        public void SetSfxVolume(float v) => ApplyVolume(KeySfx, sfxParam, v);

        private void ApplyVolume(string prefKey, string mixerParam, float linear)
        {
            linear = Mathf.Clamp01(linear);
            PlayerPrefs.SetFloat(prefKey, linear);
            if (audioMixer != null && !string.IsNullOrEmpty(mixerParam))
            {
                // -80 dB is effectively muted; log curve gives natural-feeling volume.
                float dB = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
                audioMixer.SetFloat(mixerParam, dB);
            }
        }

        // ---- Video -----------------------------------------------------------------------------

        public float GetBrightness() => PlayerPrefs.GetFloat(KeyBrightness, 1f);

        public void SetBrightness(float brightness)
        {
            brightness = Mathf.Clamp(brightness, 0.2f, 1f);
            PlayerPrefs.SetFloat(KeyBrightness, brightness);
            if (brightnessOverlay != null)
                brightnessOverlay.alpha = 1f - brightness; // darker overlay = lower brightness
        }

        public bool GetFullscreen() => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        public int GetResolutionWidth() => PlayerPrefs.GetInt(KeyResWidth, Screen.currentResolution.width);
        public int GetResolutionHeight() => PlayerPrefs.GetInt(KeyResHeight, Screen.currentResolution.height);

        public void ApplyVideoMode(int width, int height, bool fullscreen)
        {
            PlayerPrefs.SetInt(KeyResWidth, width);
            PlayerPrefs.SetInt(KeyResHeight, height);
            PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
            Screen.SetResolution(width, height, fullscreen);
        }
    }
}
