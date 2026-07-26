using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Frontend-only four-slot selector used by MainMenuNew.</summary>
    public sealed class MainMenuSaveSlotPanel : MonoBehaviour
    {
        [Serializable]
        private sealed class SlotPreview
        {
            public bool filled;
            public int iconIndex;
            public string mode = "Classic";
            public string time = "00:00:00";
            public int points;
        }

        private readonly SlotPreview[] slots =
        {
            new SlotPreview { filled = true, iconIndex = 0, mode = "Classic", time = "01:32:45", points = 1200 },
            new SlotPreview(), new SlotPreview(), new SlotPreview()
        };

        private MainMenuController owner;
        private Sprite defaultIcon;
        private Sprite[] icons;
        private Transform[] slotRows;
        private GameObject filledDetail;
        private GameObject newDetail;
        private int selectedSlot = -1;
        private int selectedIcon;
        private string selectedMode = "Classic";
        private readonly System.Collections.Generic.List<MenuButtonVisual> modeVisuals =
            new System.Collections.Generic.List<MenuButtonVisual>();

        public void Initialize(MainMenuController menu, Sprite[] selectableIcons)
        {
            owner = menu;
            icons = selectableIcons ?? Array.Empty<Sprite>();
            slotRows = new Transform[4];
            for (int i = 0; i < slotRows.Length; i++)
            {
                int captured = i;
                slotRows[i] = UIRuntime.Find(transform, $"Slot_{i + 1}");
                owner.EnsureButton(slotRows[i], () => SelectSlot(captured));
            }

            filledDetail = FindObject("Save_Slot_Detail");
            newDetail = FindObject("Save_Slot_New");
            defaultIcon = FindDisplaySprite(UIRuntime.Find(transform, "Default_Icon"));

            owner.EnsureButton(UIRuntime.Find(filledDetail?.transform, "Clear_Save"), ClearSelectedSlot);
            owner.EnsureButton(UIRuntime.Find(filledDetail?.transform, "Play_Game"), PlayFilledSlot);
            owner.EnsureButton(UIRuntime.Find(newDetail?.transform, "Play_Game"), CreateAndPlaySlot);

            BindIconChoices();
            BindModes();
            RenderRows();
        }

        public void Open()
        {
            selectedSlot = -1;
            ShowDetails(null);
            RenderRows();
        }

        /// <returns>True when Back was consumed by a detail page.</returns>
        public bool Back()
        {
            if (selectedSlot < 0) return false;
            selectedSlot = -1;
            ShowDetails(null);
            return true;
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            selectedSlot = index;
            Sfx.Play(SfxId.UiConfirm);

            if (slots[index].filled)
            {
                PopulateFilledDetail(slots[index]);
                ShowDetails(filledDetail);
            }
            else
            {
                selectedIcon = 0;
                selectedMode = "Classic";
                RefreshIconSelection();
                RefreshModeSelection();
                ShowDetails(newDetail);
            }
        }

        private void PopulateFilledDetail(SlotPreview data)
        {
            SetText(filledDetail, "Game_Mode", $"GAME MODE\n{data.mode.ToUpperInvariant()}");
            SetText(filledDetail, "Game_Time", $"TIME SPENT\n{data.time}");
            SetText(filledDetail, "Game_Point", $"POINT\n{data.points:N0}");
            SetDetailIcon(filledDetail, GetIcon(data.iconIndex));
        }

        private void ClearSelectedSlot()
        {
            if (selectedSlot < 0) return;
            slots[selectedSlot] = new SlotPreview();
            RenderRows();
            selectedSlot = -1;
            ShowDetails(null);
            Sfx.Play(SfxId.UiDecline);
        }

        private void PlayFilledSlot()
        {
            if (selectedSlot < 0 || !slots[selectedSlot].filled) return;
            SlotPreview data = slots[selectedSlot];
            owner.NotifyPlayRequested(selectedSlot, data.mode, data.iconIndex);
            Sfx.Play(SfxId.UiConfirm);
        }

        private void CreateAndPlaySlot()
        {
            if (selectedSlot < 0) return;
            slots[selectedSlot] = new SlotPreview
            {
                filled = true,
                iconIndex = selectedIcon,
                mode = selectedMode,
                time = "00:00:00",
                points = 0
            };
            RenderRows();
            owner.NotifyPlayRequested(selectedSlot, selectedMode, selectedIcon);
            PopulateFilledDetail(slots[selectedSlot]);
            ShowDetails(filledDetail);
            Sfx.Play(SfxId.UiConfirm);
        }

        private void BindIconChoices()
        {
            Transform content = UIRuntime.Find(newDetail?.transform, "Content");
            if (content == null) return;

            Transform defaultChoice = UIRuntime.Find(content, "Default_Icon");
            owner.EnsureButton(defaultChoice, () => SelectIcon(0));
            for (int i = 1; i <= 11; i++)
            {
                int iconIndex = i;
                Transform choice = UIRuntime.Find(content, $"Icon_{i}");
                owner.EnsureButton(choice, () => SelectIcon(iconIndex));
                if (choice != null && i - 1 < icons.Length && icons[i - 1] != null)
                    UIRuntime.SetSlotIcon(choice, icons[i - 1], 0.18f);
            }
        }

        private void BindModes()
        {
            Transform classic = UIRuntime.Find(newDetail?.transform, "Classic");
            Button classicButton = owner.EnsureButton(
                classic,
                () => SelectMode(classic, "Classic"),
                true);
            MenuButtonVisual visual = owner.ConfigureButtonVisual(classicButton, true);
            if (visual != null) modeVisuals.Add(visual);
            SelectMode(classic, "Classic");
        }

        private void SelectIcon(int iconIndex)
        {
            selectedIcon = Mathf.Clamp(iconIndex, 0, 11);
            RefreshIconSelection();
            Sfx.Play(SfxId.UiConfirm);
        }

        private void RefreshIconSelection()
        {
            SetDetailIcon(newDetail, GetIcon(selectedIcon));
            Transform content = UIRuntime.Find(newDetail?.transform, "Content");
            if (content == null) return;

            for (int i = 0; i <= 11; i++)
            {
                string name = i == 0 ? "Default_Icon" : $"Icon_{i}";
                Transform choice = UIRuntime.Find(content, name);
                if (choice == null) continue;
                Outline outline = choice.GetComponent<Outline>();
                if (outline == null) outline = choice.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.83f, 0.52f, 0.16f, 1f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.enabled = i == selectedIcon;
            }
        }

        private void SelectMode(Transform mode, string modeName)
        {
            selectedMode = modeName;
            RectTransform highlighter = UIRuntime.Find<RectTransform>(newDetail?.transform, "Select_Highlighter");
            if (highlighter != null && mode != null)
            {
                highlighter.SetParent(mode, false);
                highlighter.anchorMin = new Vector2(0.5f, 1f);
                highlighter.anchorMax = new Vector2(0.5f, 1f);
                highlighter.pivot = new Vector2(0.5f, 0f);
                highlighter.anchoredPosition = new Vector2(0f, 4f);
                highlighter.SetAsLastSibling();
            }
            RefreshModeSelection();
        }

        private void RefreshModeSelection()
        {
            foreach (MenuButtonVisual visual in modeVisuals)
                if (visual != null) visual.SetLatched(false);

            Transform activeMode = UIRuntime.Find(newDetail?.transform, selectedMode);
            MenuButtonVisual activeVisual = activeMode != null
                ? activeMode.GetComponent<MenuButtonVisual>()
                : null;
            if (activeVisual != null) activeVisual.SetLatched(true);
        }

        private void RenderRows()
        {
            for (int i = 0; i < slotRows.Length; i++)
            {
                Transform row = slotRows[i];
                if (row == null) continue;
                TMP_Text label = UIRuntime.Find<TMP_Text>(row, "Slot_Name");
                if (label != null)
                    label.text = slots[i].filled
                        ? $"SAVE SLOT {i + 1}  •  {slots[i].mode.ToUpperInvariant()}"
                        : $"SAVE SLOT {i + 1}  •  NEW GAME";
                UIRuntime.SetSlotIcon(UIRuntime.Find(row, "Icon_Slot"),
                    slots[i].filled ? GetIcon(slots[i].iconIndex) : null);
            }
        }

        private Sprite GetIcon(int iconIndex)
        {
            if (iconIndex <= 0) return defaultIcon;
            int arrayIndex = iconIndex - 1;
            return arrayIndex < icons.Length && icons[arrayIndex] != null ? icons[arrayIndex] : defaultIcon;
        }

        private void SetDetailIcon(GameObject detail, Sprite sprite)
        {
            UIRuntime.SetSlotIcon(UIRuntime.Find(detail?.transform, "Icon_Slot_Detail"), sprite);
        }

        private static Sprite FindDisplaySprite(Transform root)
        {
            if (root == null) return null;
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = images.Length - 1; i >= 0; i--)
                if (images[i].sprite != null) return images[i].sprite;
            return null;
        }

        private static void SetText(GameObject root, string name, string value)
        {
            TMP_Text text = UIRuntime.Find<TMP_Text>(root?.transform, name);
            if (text != null) text.text = value;
        }

        private void ShowDetails(GameObject active)
        {
            if (filledDetail != null) filledDetail.SetActive(active == filledDetail);
            if (newDetail != null) newDetail.SetActive(active == newDetail);

            // The authored layout places the four slot rows on the left and the chosen slot's
            // detail page on the right. Keep both visible so another slot can be selected directly.
            for (int i = 0; i < slotRows.Length; i++)
                if (slotRows[i] != null) slotRows[i].gameObject.SetActive(true);
        }

        private GameObject FindObject(string name)
        {
            Transform found = UIRuntime.Find(transform, name);
            return found != null ? found.gameObject : null;
        }
    }
}
