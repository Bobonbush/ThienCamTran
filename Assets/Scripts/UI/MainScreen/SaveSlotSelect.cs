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
                slots[i].SetHasData(SaveManager.Instance.HasSave(index));
                slots[i].Bind(index, () => SelectSlot(index), () => ClearSlot(index));
            }
        }

        private void SelectSlot(int index)
        {
            // Load slot vào bộ nhớ; có save thì continue đúng scene đã lưu,
            // slot trống thì bắt đầu game mới.
            SaveManager.Instance.Load(index);
            string scene = SaveManager.Instance.SavedScene ?? firstGameplayScene;
            Debug.Log($"[SaveSlotSelect] Slot {index} selected -> loading '{scene}'.");
            SceneManager.LoadScene(scene);
        }

        private void ClearSlot(int index)
        {
            SaveManager.Instance.DeleteSave(index);
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
