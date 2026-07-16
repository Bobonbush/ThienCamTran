using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Audio category: Master / Music / Sound volume sliders (doc requirement).
    /// Reads current values from <see cref="SettingsService"/> on enable and writes back live.
    /// </summary>
    public class AudioSettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private void OnEnable()
        {
            var s = SettingsService.Instance;
            if (s == null) return;

            // Initialise without firing the change callbacks.
            if (masterSlider != null) masterSlider.SetValueWithoutNotify(s.GetMasterVolume());
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(s.GetMusicVolume());
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(s.GetSfxVolume());
        }

        // Wire each slider's OnValueChanged to the matching method below.
        // The tick doubles as a live preview of the volume being set; the cue's
        // cooldown keeps a drag from machine-gunning.
        public void OnMasterChanged(float v)
        {
            SettingsService.Instance?.SetMasterVolume(v);
            Sfx.Play(SfxId.UiSlider);
        }

        public void OnMusicChanged(float v)
        {
            SettingsService.Instance?.SetMusicVolume(v);
            Sfx.Play(SfxId.UiSlider);
        }

        public void OnSfxChanged(float v)
        {
            SettingsService.Instance?.SetSfxVolume(v);
            Sfx.Play(SfxId.UiSlider);
        }
    }
}
