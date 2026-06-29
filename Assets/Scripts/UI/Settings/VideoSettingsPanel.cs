using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Video category: resolution dropdown, fullscreen toggle, brightness slider (doc requirement).
    /// Brightness is faked through a global overlay alpha managed by <see cref="SettingsService"/>,
    /// since Unity has no portable hardware-gamma API.
    /// </summary>
    public class VideoSettingsPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown resolutionDropdown;   // TextMeshPro dropdown, to match the rest of the UI
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Slider brightnessSlider;

        private readonly List<Resolution> resolutions = new List<Resolution>();

        private void OnEnable()
        {
            var s = SettingsService.Instance;
            if (s == null) return;

            BuildResolutionOptions(s);

            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(s.GetFullscreen());
            if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(s.GetBrightness());
        }

        private void BuildResolutionOptions(SettingsService s)
        {
            if (resolutionDropdown == null) return;

            resolutions.Clear();
            resolutionDropdown.ClearOptions();

            var options = new List<string>();
            int current = 0;
            var seen = new HashSet<string>();

            foreach (var res in Screen.resolutions)
            {
                string label = $"{res.width} x {res.height}";
                if (!seen.Add(label)) continue; // dedupe refresh-rate variants

                resolutions.Add(res);
                options.Add(label);

                if (res.width == s.GetResolutionWidth() && res.height == s.GetResolutionHeight())
                    current = resolutions.Count - 1;
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(current);
            resolutionDropdown.RefreshShownValue();
        }

        // ---- UI hooks --------------------------------------------------------------------------

        public void OnResolutionChanged(int index)
        {
            if (index < 0 || index >= resolutions.Count) return;
            var res = resolutions[index];
            bool fs = fullscreenToggle == null || fullscreenToggle.isOn;
            SettingsService.Instance?.ApplyVideoMode(res.width, res.height, fs);
        }

        public void OnFullscreenChanged(bool fullscreen)
        {
            var s = SettingsService.Instance;
            if (s == null) return;
            s.ApplyVideoMode(s.GetResolutionWidth(), s.GetResolutionHeight(), fullscreen);
        }

        public void OnBrightnessChanged(float value) => SettingsService.Instance?.SetBrightness(value);
    }
}
