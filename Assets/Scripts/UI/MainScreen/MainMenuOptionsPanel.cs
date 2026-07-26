using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Backend-connected options interaction for the authored Option_Panel prefab.</summary>
    public sealed class MainMenuOptionsPanel : MonoBehaviour
    {
        private enum Category { Video, Audio, Control }

        private readonly struct ResolutionChoice
        {
            public readonly int width;
            public readonly int height;
            public ResolutionChoice(int width, int height)
            {
                this.width = width;
                this.height = height;
            }
            public override string ToString() => $"{width} x {height}";
        }

        private static readonly string[] ActionNames =
            { "Up", "Down", "Left", "Right", "Jump", "Dash", "Attack", "Cast", "Heal", "Inventory" };

        private MainMenuController owner;
        private SettingsService settings;
        private InputBindingService bindings;
        private GameObject videoDetail;
        private GameObject audioDetail;
        private GameObject controlDetail;
        private RectTransform categoryHighlighter;
        private Image categoryLabel;
        private GameObject saveDialog;
        private Coroutine dialogRoutine;
        private Category activeCategory;

        private TMP_Dropdown resolution;
        private Toggle fullscreen;
        private Slider brightness;
        private Slider master;
        private Slider music;
        private Slider sound;
        private readonly List<ResolutionChoice> resolutions = new List<ResolutionChoice>();
        private readonly List<TMP_Text> keyLabels = new List<TMP_Text>();
        private int listeningIndex = -1;
        private bool controlsDirty;

        public void Initialize(MainMenuController menu)
        {
            owner = menu;
            settings = SettingsService.EnsureExists(owner.AudioMixer);
            bindings = InputBindingService.EnsureExists(owner.InputActions);

            videoDetail = FindObject("Video_Detail");
            audioDetail = FindObject("Audio_Detail");
            controlDetail = FindObject("Control_Detail");
            categoryHighlighter = UIRuntime.Find<RectTransform>(transform, "Selected_Highlighter");
            categoryLabel = UIRuntime.Find<Image>(transform, "Control_Label");
            saveDialog = FindObject("Save_Dialog");

            BindCategory("Video", Category.Video);
            BindCategory("Audio", Category.Audio);
            BindCategory("Control", Category.Control);
            owner.EnsureButton(UIRuntime.Find(transform, "Save_Button"), SaveCurrent, true);
            owner.EnsureButton(UIRuntime.Find(transform, "Reset_Button"), ResetCurrent, true);

            ConfigureVideo();
            ConfigureAudio();
            ConfigureControls();
            HideSaveDialog();
        }

        public void Open()
        {
            listeningIndex = -1;
            controlsDirty = false;
            bindings?.RevertUnsaved();
            LoadSettingsIntoControls();
            RefreshAllKeyLabels();
            SelectCategory(Category.Video);
        }

        public void CancelUncommitted()
        {
            listeningIndex = -1;
            if (controlsDirty) bindings?.RevertUnsaved();
            settings?.PreviewBrightness(settings.GetBrightness());
            settings?.PreviewAudio(
                settings.GetMasterVolume(),
                settings.GetMusicVolume(),
                settings.GetSfxVolume());
            controlsDirty = false;
        }

        private void OnDestroy()
        {
            bindings?.CancelRebind();
        }

        private void BindCategory(string name, Category category)
        {
            Transform target = UIRuntime.Find(transform, name);
            owner.EnsureButton(target, () => SelectCategory(category));
        }

        private void SelectCategory(Category category)
        {
            activeCategory = category;
            if (videoDetail != null) videoDetail.SetActive(category == Category.Video);
            if (audioDetail != null) audioDetail.SetActive(category == Category.Audio);
            if (controlDetail != null) controlDetail.SetActive(category == Category.Control);

            PositionCategoryHighlighter(UIRuntime.Find(transform, category.ToString()));
            UpdateCategoryLabel(category);
            Sfx.Play(SfxId.UiConfirm);
        }

        private void ConfigureVideo()
        {
            resolution = UIRuntime.Find<TMP_Dropdown>(transform, "Reosolution_Dropdown");
            fullscreen = UIRuntime.Find<Toggle>(transform, "Full_Screen_Toggle");
            brightness = UIRuntime.Find<Slider>(transform, "Brightness_Slider");
            if (brightness != null)
            {
                brightness.minValue = 0f;
                brightness.maxValue = 1f;
                brightness.onValueChanged.AddListener(value => settings?.PreviewBrightness(value));
            }
            BuildResolutionOptions();
        }

        private void BuildResolutionOptions()
        {
            if (resolution == null) return;
            resolutions.Clear();
            HashSet<string> seen = new HashSet<string>();

            foreach (Resolution available in Screen.resolutions)
            {
                string key = $"{available.width}x{available.height}";
                if (seen.Add(key))
                    resolutions.Add(new ResolutionChoice(available.width, available.height));
            }

            AddResolutionIfMissing(SettingsService.DefaultResolutionWidth, SettingsService.DefaultResolutionHeight, seen);
            AddResolutionIfMissing(settings.GetResolutionWidth(), settings.GetResolutionHeight(), seen);
            resolutions.Sort((a, b) =>
            {
                long pixelsA = (long)a.width * a.height;
                long pixelsB = (long)b.width * b.height;
                return pixelsA != pixelsB ? pixelsA.CompareTo(pixelsB) : a.width.CompareTo(b.width);
            });

            List<string> labels = new List<string>();
            foreach (ResolutionChoice choice in resolutions) labels.Add(choice.ToString());
            resolution.ClearOptions();
            resolution.AddOptions(labels);
        }

        private void AddResolutionIfMissing(int width, int height, HashSet<string> seen)
        {
            if (seen.Add($"{width}x{height}"))
                resolutions.Add(new ResolutionChoice(width, height));
        }

        private void ConfigureAudio()
        {
            master = UIRuntime.Find<Slider>(transform, "Master_Slider");
            music = UIRuntime.Find<Slider>(transform, "Music_Slider");
            sound = UIRuntime.Find<Slider>(transform, "Sound_Slider");
            ConfigureVolumeSlider(master);
            ConfigureVolumeSlider(music);
            ConfigureVolumeSlider(sound);
            if (master != null) master.onValueChanged.AddListener(_ => PreviewAudio());
            if (music != null) music.onValueChanged.AddListener(_ => PreviewAudio());
            if (sound != null) sound.onValueChanged.AddListener(_ => PreviewAudio());
        }

        private static void ConfigureVolumeSlider(Slider slider)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }

        private void PreviewAudio()
        {
            settings?.PreviewAudio(
                master != null ? master.value : SettingsService.DefaultVolume,
                music != null ? music.value : SettingsService.DefaultVolume,
                sound != null ? sound.value : SettingsService.DefaultVolume);
        }

        private void ConfigureControls()
        {
            Transform content = UIRuntime.Find(controlDetail?.transform, "Content");
            Transform template = UIRuntime.Find(content, "Control_Item");
            if (content == null || template == null) return;

            keyLabels.Clear();
            for (int i = 0; i < ActionNames.Length; i++)
            {
                Transform row = i == 0 ? template : Instantiate(template.gameObject, content).transform;
                row.name = $"Control_Item_{ActionNames[i]}";
                TMP_Text actionLabel = row.GetComponent<TMP_Text>();
                if (actionLabel != null) actionLabel.text = ActionNames[i];

                Transform keyBox = UIRuntime.Find(row, "Button_Box");
                TMP_Text keyLabel = UIRuntime.FirstText(keyBox, "Control_Button");
                keyLabels.Add(keyLabel);
                int captured = i;
                owner.EnsureButton(keyBox, () => BeginKeyCapture(captured));
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
        }

        private void BeginKeyCapture(int index)
        {
            if (bindings == null || index < 0 || index >= ActionNames.Length) return;
            listeningIndex = index;
            if (index < keyLabels.Count && keyLabels[index] != null)
                keyLabels[index].text = "PRESS KEY...";

            MenuBindingId bindingId = (MenuBindingId)index;
            bindings.StartInteractiveRebind(bindingId, (success, message) =>
            {
                listeningIndex = -1;
                RefreshAllKeyLabels();
                if (success)
                {
                    controlsDirty = true;
                    ShowSaveDialog("PRESS SAVE TO CONFIRM");
                    Sfx.Play(SfxId.UiConfirm);
                }
                else
                {
                    ShowSaveDialog(message);
                    Sfx.Play(SfxId.UiDecline);
                }
            });
        }

        private void SaveCurrent()
        {
            switch (activeCategory)
            {
                case Category.Video:
                    ResolutionChoice choice = GetSelectedResolution();
                    settings.SetVideo(
                        choice.width,
                        choice.height,
                        fullscreen != null && fullscreen.isOn,
                        brightness != null ? brightness.value : SettingsService.DefaultBrightness);
                    break;

                case Category.Audio:
                    settings.SetAudio(
                        master != null ? master.value : SettingsService.DefaultVolume,
                        music != null ? music.value : SettingsService.DefaultVolume,
                        sound != null ? sound.value : SettingsService.DefaultVolume);
                    break;

                case Category.Control:
                    bindings?.SaveOverrides();
                    controlsDirty = false;
                    break;
            }

            ShowSaveDialog("SETTINGS SAVED");
            Sfx.Play(SfxId.UiConfirm);
        }

        private void ResetCurrent()
        {
            switch (activeCategory)
            {
                case Category.Video:
                    settings.ResetVideo();
                    LoadVideoIntoControls();
                    break;

                case Category.Audio:
                    settings.ResetAudio();
                    LoadAudioIntoControls();
                    break;

                case Category.Control:
                    bindings?.ResetToDefaults();
                    controlsDirty = false;
                    RefreshAllKeyLabels();
                    break;
            }

            ShowSaveDialog("DEFAULTS RESTORED");
            Sfx.Play(SfxId.UiDecline);
        }

        private void LoadSettingsIntoControls()
        {
            LoadVideoIntoControls();
            LoadAudioIntoControls();
        }

        private void LoadVideoIntoControls()
        {
            if (resolution != null)
            {
                int index = FindResolution(settings.GetResolutionWidth(), settings.GetResolutionHeight());
                resolution.SetValueWithoutNotify(Mathf.Max(0, index));
                resolution.RefreshShownValue();
            }
            if (fullscreen != null) fullscreen.SetIsOnWithoutNotify(settings.GetFullscreen());
            if (brightness != null) brightness.SetValueWithoutNotify(settings.GetBrightness());
        }

        private void LoadAudioIntoControls()
        {
            if (master != null) master.SetValueWithoutNotify(settings.GetMasterVolume());
            if (music != null) music.SetValueWithoutNotify(settings.GetMusicVolume());
            if (sound != null) sound.SetValueWithoutNotify(settings.GetSfxVolume());
        }

        private ResolutionChoice GetSelectedResolution()
        {
            if (resolutions.Count == 0)
                return new ResolutionChoice(
                    SettingsService.DefaultResolutionWidth,
                    SettingsService.DefaultResolutionHeight);
            int index = resolution != null ? Mathf.Clamp(resolution.value, 0, resolutions.Count - 1) : 0;
            return resolutions[index];
        }

        private int FindResolution(int width, int height)
        {
            for (int i = 0; i < resolutions.Count; i++)
                if (resolutions[i].width == width && resolutions[i].height == height)
                    return i;
            return 0;
        }

        private void RefreshAllKeyLabels()
        {
            if (bindings == null) return;
            for (int i = 0; i < keyLabels.Count; i++)
                if (keyLabels[i] != null)
                    keyLabels[i].text = bindings.GetDisplayString((MenuBindingId)i);
        }

        private void PositionCategoryHighlighter(Transform category)
        {
            if (categoryHighlighter == null || category == null) return;
            Vector2 size = categoryHighlighter.sizeDelta;
            categoryHighlighter.SetParent(category, false);
            categoryHighlighter.anchorMin = new Vector2(0f, 0.5f);
            categoryHighlighter.anchorMax = new Vector2(0f, 0.5f);
            categoryHighlighter.pivot = new Vector2(1f, 0.5f);
            categoryHighlighter.sizeDelta = size;
            categoryHighlighter.anchoredPosition = new Vector2(-8f, 0f);
            categoryHighlighter.SetAsLastSibling();
        }

        private void UpdateCategoryLabel(Category category)
        {
            if (categoryLabel == null) return;
            Sprite sprite = category switch
            {
                Category.Video => owner.VideoLabelSprite,
                Category.Audio => owner.AudioLabelSprite,
                _ => owner.ControlLabelSprite
            };
            if (sprite != null)
            {
                categoryLabel.sprite = sprite;
                categoryLabel.enabled = true;
                categoryLabel.preserveAspect = true;
            }
        }

        private void ShowSaveDialog(string message)
        {
            if (saveDialog == null) return;
            TMP_Text text = saveDialog.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = message;
            saveDialog.SetActive(true);
            if (dialogRoutine != null) StopCoroutine(dialogRoutine);
            dialogRoutine = StartCoroutine(HideDialogAfterDelay());
        }

        private IEnumerator HideDialogAfterDelay()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            HideSaveDialog();
            dialogRoutine = null;
        }

        private void HideSaveDialog()
        {
            if (saveDialog != null) saveDialog.SetActive(false);
        }

        private GameObject FindObject(string name)
        {
            Transform found = UIRuntime.Find(transform, name);
            return found != null ? found.gameObject : null;
        }
    }
}
