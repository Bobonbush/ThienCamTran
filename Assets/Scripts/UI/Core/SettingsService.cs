using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Persistent application-wide video and audio settings. The main menu and pause menu both
    /// use this service, and saved values are applied again whenever the game starts.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SettingsService : MonoBehaviour
    {
        public const int DefaultResolutionWidth = 1920;
        public const int DefaultResolutionHeight = 1080;
        public const bool DefaultFullscreen = false;
        public const float DefaultBrightness = 0.5f;
        public const float DefaultVolume = 1f;

        public static SettingsService Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterParam = "MasterVolume";
        [SerializeField] private string musicParam = "MusicVolume";
        [SerializeField] private string sfxParam = "SfxVolume";

        [Header("Video")]
        [SerializeField] private CanvasGroup brightnessOverlay;

        private const string KeyMaster = "set_vol_master";
        private const string KeyMusic = "set_vol_music";
        private const string KeySfx = "set_vol_sfx";
        private const string KeyBrightness = "set_brightness";
        private const string KeyFullscreen = "set_fullscreen";
        private const string KeyResWidth = "set_res_w";
        private const string KeyResHeight = "set_res_h";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SfxLibrary library = Resources.Load<SfxLibrary>("SfxLibrary");
            AudioMixer mixer = library != null && library.output != null
                ? library.output.audioMixer
                : null;
            EnsureExists(mixer);
        }

        public static SettingsService EnsureExists(AudioMixer mixer)
        {
            if (Instance == null)
            {
                GameObject serviceObject = new GameObject(nameof(SettingsService));
                Instance = serviceObject.AddComponent<SettingsService>();
            }

            Instance.ConfigureMixer(mixer);
            Instance.EnsureBrightnessOverlay();
            Instance.ApplyAll();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureBrightnessOverlay();
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyAll();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        public void ConfigureMixer(AudioMixer mixer)
        {
            if (mixer != null) audioMixer = mixer;
        }

        public void ApplyAll()
        {
            ApplyVolume(masterParam, GetMasterVolume());
            ApplyVolume(musicParam, GetMusicVolume());
            ApplyVolume(sfxParam, GetSfxVolume());
            ApplyBrightness(GetBrightness());
            ApplyVideoMode(GetResolutionWidth(), GetResolutionHeight(), GetFullscreen(), false);
        }

        public float GetMasterVolume() => PlayerPrefs.GetFloat(KeyMaster, DefaultVolume);
        public float GetMusicVolume() => PlayerPrefs.GetFloat(KeyMusic, DefaultVolume);
        public float GetSfxVolume() => PlayerPrefs.GetFloat(KeySfx, DefaultVolume);

        public void SetMasterVolume(float value) => StoreVolume(KeyMaster, masterParam, value);
        public void SetMusicVolume(float value) => StoreVolume(KeyMusic, musicParam, value);
        public void SetSfxVolume(float value) => StoreVolume(KeySfx, sfxParam, value);

        public void SetAudio(float master, float music, float sfx)
        {
            SetMasterVolume(master);
            SetMusicVolume(music);
            SetSfxVolume(sfx);
            PlayerPrefs.Save();
        }

        public void PreviewAudio(float master, float music, float sfx)
        {
            ApplyVolume(masterParam, Mathf.Clamp01(master));
            ApplyVolume(musicParam, Mathf.Clamp01(music));
            ApplyVolume(sfxParam, Mathf.Clamp01(sfx));
        }

        public void ResetAudio() => SetAudio(DefaultVolume, DefaultVolume, DefaultVolume);

        private void StoreVolume(string prefKey, string mixerParameter, float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(prefKey, value);
            ApplyVolume(mixerParameter, value);
        }

        private void ApplyVolume(string mixerParameter, float linear)
        {
            if (audioMixer == null || string.IsNullOrEmpty(mixerParameter)) return;
            float decibels = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
            if (!audioMixer.SetFloat(mixerParameter, decibels))
                Debug.LogWarning($"SettingsService: AudioMixer parameter '{mixerParameter}' is not exposed.", this);
        }

        public float GetBrightness() => PlayerPrefs.GetFloat(KeyBrightness, DefaultBrightness);
        public bool GetFullscreen() => PlayerPrefs.GetInt(KeyFullscreen, DefaultFullscreen ? 1 : 0) == 1;
        public int GetResolutionWidth() => PlayerPrefs.GetInt(KeyResWidth, DefaultResolutionWidth);
        public int GetResolutionHeight() => PlayerPrefs.GetInt(KeyResHeight, DefaultResolutionHeight);

        public void SetBrightness(float brightness)
        {
            brightness = Mathf.Clamp01(brightness);
            PlayerPrefs.SetFloat(KeyBrightness, brightness);
            ApplyBrightness(brightness);
        }

        public void PreviewBrightness(float brightness) => ApplyBrightness(brightness);

        public void ApplyVideoMode(int width, int height, bool fullscreen)
        {
            ApplyVideoMode(width, height, fullscreen, true);
        }

        public void SetVideo(int width, int height, bool fullscreen, float brightness)
        {
            SetBrightness(brightness);
            ApplyVideoMode(width, height, fullscreen, true);
            PlayerPrefs.Save();
        }

        public void ResetVideo()
        {
            SetVideo(DefaultResolutionWidth, DefaultResolutionHeight, DefaultFullscreen, DefaultBrightness);
        }

        private void ApplyVideoMode(int width, int height, bool fullscreen, bool persist)
        {
            width = Mathf.Max(640, width);
            height = Mathf.Max(360, height);
            if (persist)
            {
                PlayerPrefs.SetInt(KeyResWidth, width);
                PlayerPrefs.SetInt(KeyResHeight, height);
                PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
            }

            FullScreenMode mode = fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            if (Screen.width != width || Screen.height != height || Screen.fullScreenMode != mode)
                Screen.SetResolution(width, height, mode);
        }

        private void ApplyBrightness(float brightness)
        {
            EnsureBrightnessOverlay();
            if (brightnessOverlay != null)
                brightnessOverlay.alpha = Mathf.Lerp(0.65f, 0f, Mathf.Clamp01(brightness));
        }

        private void EnsureBrightnessOverlay()
        {
            if (brightnessOverlay != null) return;

            GameObject canvasObject = new GameObject(
                "BrightnessOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject shadeObject = new GameObject("Shade", typeof(RectTransform), typeof(Image));
            shadeObject.transform.SetParent(canvasObject.transform, false);
            RectTransform shadeRect = (RectTransform)shadeObject.transform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;
            Image shade = shadeObject.GetComponent<Image>();
            shade.color = Color.black;
            shade.raycastTarget = false;

            brightnessOverlay = canvasObject.GetComponent<CanvasGroup>();
            brightnessOverlay.interactable = false;
            brightnessOverlay.blocksRaycasts = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyVolume(masterParam, GetMasterVolume());
            ApplyVolume(musicParam, GetMusicVolume());
            ApplyVolume(sfxParam, GetSfxVolume());
            ApplyBrightness(GetBrightness());
        }
    }
}
