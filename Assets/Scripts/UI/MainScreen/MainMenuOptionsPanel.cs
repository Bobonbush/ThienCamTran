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

        private static IReadOnlyList<InputBindingService.BindingDefinition> Rows =>
            InputBindingService.Definitions;

        private IOptionsHost owner;
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
        private readonly List<TMP_Text> keyboardLabels = new List<TMP_Text>();
        private readonly List<TMP_Text> gamepadLabels = new List<TMP_Text>();
        private int listeningIndex = -1;
        private bool controlsDirty;

        /// <summary>Host supplies only assets, so both the main menu and the in-game book overlay
        /// can drive the same authored "Option_Panel " prefab.</summary>
        public void Initialize(IOptionsHost menu)
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
            Bind(UIRuntime.Find(transform, "Save_Button"), SaveCurrent, true);
            Bind(UIRuntime.Find(transform, "Reset_Button"), ResetCurrent, true);

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
            Bind(target, () => SelectCategory(category));
        }

        /// <summary>Button wiring using the host's artwork.</summary>
        private Button Bind(Transform target, UnityEngine.Events.UnityAction action, bool forceSelectedVisual = false)
        {
            return UIRuntime.EnsureButton(target, action, owner.SelectedButtonSprite, forceSelectedVisual);
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

            // Snapshot the authored row BEFORE anything is configured. Row 0 reuses the template in
            // place, so cloning the template afterwards would copy an already-built gamepad column
            // into every subsequent row.
            GameObject prototype = Instantiate(template.gameObject, content);
            prototype.SetActive(false);

            keyboardLabels.Clear();
            gamepadLabels.Clear();
            for (int i = 0; i < Rows.Count; i++)
            {
                InputBindingService.BindingDefinition definition = Rows[i];
                Transform row = i == 0 ? template : Instantiate(prototype, content).transform;
                row.gameObject.SetActive(true);
                row.name = $"Control_Item_{definition.displayName}";

                Transform keyBox = UIRuntime.Find(row, "Button_Box");
                keyboardLabels.Add(UIRuntime.FirstText(keyBox, "Control_Button"));
                int captured = i;
                Bind(keyBox, () => BeginKeyCapture(captured, BindingDevice.Keyboard));

                gamepadLabels.Add(BuildGamepadCell(keyBox, definition, captured));

                // Set the row label last: the two key cells reserve their space via the label's right
                // margin, so it has to be measured after the cells are positioned.
                TMP_Text actionLabel = row.GetComponent<TMP_Text>();
                if (actionLabel != null)
                {
                    actionLabel.text = definition.displayName;
                    // A long name like "Inventory" otherwise wraps onto a second line and collides
                    // with the row below. Keep the authored size as the ceiling and shrink to fit.
                    UIRuntime.ConfigureSingleLineListText(actionLabel, Mathf.Max(actionLabel.fontSize, 12f));
                    actionLabel.alignment = TextAlignmentOptions.MidlineLeft;
                }
            }

            Destroy(prototype);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
        }

        /// <summary>
        /// Clones the authored keyboard cell to make a gamepad column, so the second column matches
        /// the art without the prefab needing a second Button_Box. The pair keeps the authored cell's
        /// right edge and grows leftward into the label's space, so the row width is unchanged.
        /// </summary>
        private TMP_Text BuildGamepadCell(
            Transform keyBox,
            InputBindingService.BindingDefinition definition,
            int index)
        {
            RectTransform keyRect = keyBox as RectTransform;
            if (keyRect == null || keyRect.parent == null) return null;

            RectTransform padRect = Instantiate(keyBox.gameObject, keyBox.parent).transform as RectTransform;
            if (padRect == null) return null;
            padRect.name = "Button_Box_Gamepad";
            padRect.SetSiblingIndex(keyRect.GetSiblingIndex() + 1);
            LayOutKeyCells(keyRect, padRect);

            TMP_Text padLabel = UIRuntime.FirstText(padRect, "Control_Button");
            UIRuntime.ConfigureSingleLineListText(padLabel);
            UIRuntime.ConfigureSingleLineListText(UIRuntime.FirstText(keyRect, "Control_Button"));

            if (!definition.gamepadRebindable)
            {
                // Move/Purify are whole-stick bindings on a gamepad - nothing per-direction to rebind.
                // The clone inherited the keyboard cell's click handler; drop it before the deferred
                // Destroy so the dead cell cannot start a rebind in the meantime.
                Button existing = padRect.GetComponent<Button>();
                if (existing != null)
                {
                    existing.onClick.RemoveAllListeners();
                    existing.interactable = false;
                    Destroy(existing);
                }
                if (padLabel != null)
                {
                    padLabel.text = InputBindingService.StickBindingLabel;
                    padLabel.color = new Color(padLabel.color.r, padLabel.color.g, padLabel.color.b, 0.45f);
                }
                return padLabel;
            }

            Bind(padRect, () => BeginKeyCapture(index, BindingDevice.Gamepad));
            return padLabel;
        }

        /// <summary>
        /// Splits the authored single-cell slot into two side-by-side cells. Both keep the authored
        /// anchoring and height; only x and width change, so the row rect and the vertical rhythm of
        /// the list are untouched.
        /// </summary>
        private void LayOutKeyCells(RectTransform keyCell, RectTransform padCell)
        {
            const float gap = 8f;
            const float widenFactor = 1.62f;

            Vector2 authoredPosition = keyCell.anchoredPosition;
            Vector2 authoredSize = keyCell.sizeDelta;

            float rightEdge = authoredPosition.x + authoredSize.x * 0.5f;
            float pairWidth = authoredSize.x * widenFactor;
            float cellWidth = (pairWidth - gap) * 0.5f;
            float leftEdge = rightEdge - pairWidth;

            keyCell.sizeDelta = new Vector2(cellWidth, authoredSize.y);
            keyCell.anchoredPosition = new Vector2(leftEdge + cellWidth * 0.5f, authoredPosition.y);

            padCell.anchorMin = keyCell.anchorMin;
            padCell.anchorMax = keyCell.anchorMax;
            padCell.pivot = keyCell.pivot;
            padCell.sizeDelta = new Vector2(cellWidth, authoredSize.y);
            padCell.anchoredPosition = new Vector2(rightEdge - cellWidth * 0.5f, authoredPosition.y);

            // Keep the row label clear of both cells.
            TMP_Text rowLabel = keyCell.parent != null ? keyCell.parent.GetComponent<TMP_Text>() : null;
            RectTransform rowRect = keyCell.parent as RectTransform;
            if (rowLabel != null && rowRect != null)
            {
                Vector4 margin = rowLabel.margin;
                rowLabel.margin = new Vector4(
                    margin.x,
                    margin.y,
                    rowRect.rect.width * 0.5f - leftEdge + gap,
                    margin.w);
                rowLabel.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }

        private void BeginKeyCapture(int index, BindingDevice device)
        {
            if (bindings == null || index < 0 || index >= Rows.Count) return;
            listeningIndex = index;

            List<TMP_Text> column = device == BindingDevice.Gamepad ? gamepadLabels : keyboardLabels;
            if (index < column.Count && column[index] != null)
                column[index].text = device == BindingDevice.Gamepad ? "PRESS BUTTON..." : "PRESS KEY...";

            bindings.StartInteractiveRebind(Rows[index].id, device, (success, message) =>
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
            for (int i = 0; i < Rows.Count; i++)
            {
                MenuBindingId id = Rows[i].id;
                if (i < keyboardLabels.Count && keyboardLabels[i] != null)
                    keyboardLabels[i].text = bindings.GetDisplayString(id, BindingDevice.Keyboard);
                if (i < gamepadLabels.Count && gamepadLabels[i] != null)
                    gamepadLabels[i].text = bindings.GetDisplayString(id, BindingDevice.Gamepad);
            }
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
