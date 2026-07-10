using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A clickable label in the overlay tab bar (Status / Map / Inventory / Database).
    /// Purely cosmetic + click forwarding; the actual switching lives in <see cref="MenuTabController"/>.
    /// </summary>
    public class TabButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject selectedIndicator;

        public void Bind(Action onClick)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke());
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedIndicator != null) selectedIndicator.SetActive(selected);
            if (label != null) label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
    }
}
