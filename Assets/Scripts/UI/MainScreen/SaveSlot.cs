using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A single row in <see cref="SaveSlotSelect"/>. Placeholder for now: shows either "New Game"
    /// (empty) or stubbed stats (location / completion / playtime), plus a Clear Save button.
    /// </summary>
    public class SaveSlot : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Button clearButton;

        [Header("Display")]
        [SerializeField] private TMP_Text indexLabel;     // "1.", "2.", ...
        [SerializeField] private TMP_Text summaryLabel;   // "New Game" or stubbed stats
        [SerializeField] private GameObject statsGroup;   // shown only when the slot has data

        [Tooltip("Placeholder: treat this slot as having data. Real save detection comes later.")]
        [SerializeField] private bool hasData = false;

        public void Bind(int index, Action onSelect, Action onClear)
        {
            if (indexLabel != null) indexLabel.text = $"{index + 1}.";

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelect?.Invoke());
            }
            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(() => onClear?.Invoke());
                clearButton.gameObject.SetActive(hasData);
            }

            Render();
        }

        public void SetEmpty()
        {
            hasData = false;
            if (clearButton != null) clearButton.gameObject.SetActive(false);
            Render();
        }

        private void Render()
        {
            if (summaryLabel != null) summaryLabel.text = hasData ? "Vệ An" : "New Game";
            if (statsGroup != null) statsGroup.SetActive(hasData);
        }
    }
}
