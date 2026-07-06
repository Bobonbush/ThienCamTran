using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// "Select Profile" screen (doc image rId40). The doc only needs PLACEHOLDER save slots, so the
    /// stats are stubbed and selecting any slot just starts a new game. Wired to grow later into
    /// real save data without changing the UI.
    /// </summary>
    public class SaveSlotSelect : UIScreen
    {
        [SerializeField] private SaveSlot[] slots;
        [Tooltip("Scene to load when a slot is chosen / New Game is pressed.")]
        [SerializeField] private string firstGameplayScene = "Opening";

        private System.Action onBack;

        public void Open(System.Action onBack)
        {
            this.onBack = onBack;
            base.Open();
        }

        protected override void OnOpened()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                int index = i;
                slots[i].Bind(index, () => SelectSlot(index), () => ClearSlot(index));
            }
        }

        private void SelectSlot(int index)
        {
            // Placeholder: every slot starts the first gameplay scene. Replace with load-or-new later.
            Debug.Log($"[SaveSlotSelect] Slot {index} selected -> loading '{firstGameplayScene}'.");
            SceneManager.LoadScene(firstGameplayScene);
        }

        private void ClearSlot(int index)
        {
            Debug.Log($"[SaveSlotSelect] Clear save for slot {index} (placeholder).");
            if (index >= 0 && index < slots.Length && slots[index] != null)
                slots[index].SetEmpty();
        }

        public void OnBack()
        {
            Close();
            onBack?.Invoke();
        }
    }
}
