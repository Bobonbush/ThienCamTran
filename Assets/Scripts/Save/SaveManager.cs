using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trung tâm save/load, singleton DontDestroyOnLoad, tự sinh khi được gọi.
/// - File: {persistentDataPath}/Saves/Save_{slot}.json (mỗi slot 1 file,
///   dùng chung cho mọi scene). Settings do SettingsService lo riêng.
/// - Main menu: SaveManager.Instance.Load(slot) rồi load scene
///   SaveManager.Instance.SavedScene (null = game mới).
/// - Ghi file CHỈ xảy ra khi checkpoint (SaveZone) gọi CheckpointSave/
///   RestAtSaveZone — đúng thiết kế "save khi pray".
/// - Object trong scene tự hỏi trạng thái qua IsChestOpened / IsObjectBroken /
///   IsPuzzleSolved / IsBossDefeated / IsEnemyDead với ID từ SaveIdUtility.
/// - Quái thường chết là dữ liệu TẠM: giữ qua chuyển scene và cả thoát game,
///   nhưng reset khi nghỉ ở Save Zone (RestAtSaveZone).
/// </summary>
public class SaveManager : MonoBehaviour
{
    private const string SaveFolder = "Saves";

    private static SaveManager instance;

    // For testing save game
    
    private int useSlot = 2;

    private bool currentlyInGame = false;



    public static SaveManager Instance
    {
        get
        {
            EnsureExists();
            return instance;
        }
    }

    public int CurrentSlot { get; private set; } = 1;
    public GameSaveData Data { get; private set; } = new GameSaveData();

    public Action RestAtSaveZoneFunction;

    /// <summary>Scene của lần checkpoint gần nhất; null nếu save trống (game mới).</summary>
    public string SavedScene
    {
        get { return string.IsNullOrEmpty(Data.currentScene) ? null : Data.currentScene; }
    }

    private bool pendingContinueRestore;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    private static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject managerObject = new GameObject("SaveManager");
        instance = managerObject.AddComponent<SaveManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.Log("BUSSSSGGGGGGGGGG");
            Destroy(gameObject);
            return;
        }

        instance = this;
        
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void Start()
    {

        
        CutSceneManager.Instance.ForceHideGamePlay();
        

        Load(useSlot);
        
    }


    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ==================== file / slot ====================

    private static string SlotPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, SaveFolder, "Save_" + slot + ".json");
    }

    public bool HasSave(int slot)
    {
        return File.Exists(SlotPath(slot));
    }

    /// <summary>Đọc slot vào bộ nhớ (file chưa có -> save trống, game mới).</summary>
    /// 
    private void InitializeGame()
    {
        string currentScene = Data.currentScene;
        PlayerController player = CutSceneManager.Instance.playerController;
        if(currentScene == String.Empty || currentScene == "")
        {
            // New Game was created
            StartCoroutine(SceneTransitionManager.Instance.TransitionToScene("Warning", Vector3.zero));
        }else
        {
            StartCoroutine(LoadGame(player));
        }

        
    }


    private IEnumerator LoadGame(PlayerController player)
    {
        yield return SceneTransitionManager.Instance.Fade(1.0f);

        
        Data.temporary.deadEnemies.Clear();
        Data.temporary.deathDrop = new DeathDropData();

        CutSceneManager.Instance.ForceHideGamePlay(true);

        player.LockCutScene();


        yield return SceneTransitionManager.Instance.TransitionToScene(Data.currentScene, Data.checkpointPosition);

        


        player.ResetFromDeath();

        yield return new WaitForSeconds(0.3f);
        RestAtSaveZoneFunction?.Invoke();

        player.ReleaseLockCutScene();
        yield return new WaitForSeconds(0.2f);

        yield return SceneTransitionManager.Instance.Fade(0.0f);
    }


    public void Load(int slot)
    {
        if (currentlyInGame) return;
        CurrentSlot = slot;
        Data = new GameSaveData();

        if (HasSave(slot))
        {
            try
            {
                GameSaveData loaded = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SlotPath(slot)));
                if (loaded != null)
                    Data = loaded;
            }
            catch (System.Exception e)
            {
                Debug.LogError("SaveManager: file save slot " + slot + " hỏng, bắt đầu game mới. " + e.Message);
            }
        }

        currentlyInGame = true;
        // Sau khi Load từ menu, lần vào scene đầu tiên sẽ khôi phục inventory + vị trí
        pendingContinueRestore = true;
        InitializeGame();
    }

    public void DeleteSave(int slot)
    {
        if (HasSave(slot))
            File.Delete(SlotPath(slot));

        if (slot == CurrentSlot)
            Data = new GameSaveData();
    }

    private void WriteToDisk()
    {
        string path = SlotPath(CurrentSlot);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(Data, true));
    }

    // ==================== checkpoint / rest ====================

    /// <summary>
    /// Save đầy đủ tại checkpoint: scene hiện tại, save zone, vị trí, inventory.
    /// SaveZone gọi hàm này trong SaveCheckpoint.
    /// </summary>
    public void CheckpointSave(string saveZoneId, PlayerController player)
    {
        Data.currentScene = SceneManager.GetActiveScene().name;
        Data.lastSaveZoneID = saveZoneId;

        if (player != null)
        {
            StoreCheckpointPosition(player.transform.position);
            CaptureInventory(player.GetComponent<Inventory>());
        }

        WriteToDisk();
    }

    /// <summary>
    /// Nghỉ tại Save Zone: xoá dữ liệu tạm (quái thường sống lại, death drop
    /// biến mất) rồi save vĩnh viễn. SaveZone.RestFromUI gọi hàm này.
    /// </summary>
    public void RestAtSaveZone(string saveZoneId, PlayerController player)
    {
        StartCoroutine(PerformRest(saveZoneId, player));
    }


    

    private IEnumerator PerformRest(string saveZoneId, PlayerController player)
    {
        yield return SceneTransitionManager.Instance.Fade(1.0f);
        Data.temporary.deadEnemies.Clear();
        Data.temporary.deathDrop = new DeathDropData();
        CheckpointSave(saveZoneId, player);

        player.ExitSaving();
        player.AnimationExitSaving();

        player.LockCutScene();


        yield return SceneTransitionManager.Instance.ReloadScene();

        yield return new WaitForSeconds(0.2f);
       
        RestAtSaveZoneFunction?.Invoke();

        
        player.ReleaseLockCutScene();

        yield return new WaitForSeconds(0.2f);

        yield return SceneTransitionManager.Instance.Fade(0.0f);
    }

    public void Die(PlayerController player)
    {
        StartCoroutine(PerformDie(player));

    }


    private IEnumerator PerformDie(PlayerController player)
    {
        yield return SceneTransitionManager.Instance.Fade(1.0f);

        Data.temporary.deadEnemies.Clear();
        Data.temporary.deathDrop = new DeathDropData();

        player.LockCutScene();

        if (Data.world.isSteelMode == false)
        {

            yield return SceneTransitionManager.Instance.TransitionToScene(Data.currentScene, Data.checkpointPosition);


            player.ResetFromDeath();

            yield return new WaitForSeconds(0.3f);
            RestAtSaveZoneFunction?.Invoke();

            player.ReleaseLockCutScene();
            yield return new WaitForSeconds(0.2f);
        }else
        {
            SceneTransitionManager.Instance.TransitionToScene("hard", "", Vector3.zero);
        }





         yield return SceneTransitionManager.Instance.Fade(0.0f);

        
    }


    private void StoreCheckpointPosition(Vector3 position)
    {
        // Vị trí nằm TRONG file save để mỗi slot có checkpoint riêng
        Data.checkpointPosition = position;
        Data.hasCheckpointPosition = true;
        // Giữ tương thích với các key checkpoint cũ của team
        PlayerPrefs.SetString("Checkpoint.Scene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetFloat("Checkpoint.X", position.x);
        PlayerPrefs.SetFloat("Checkpoint.Y", position.y);
        PlayerPrefs.SetFloat("Checkpoint.Z", position.z);
        PlayerPrefs.Save();
    }

    // ==================== trạng thái thế giới (vĩnh viễn) ====================

    public bool IsChestOpened(string id) { return Contains(Data.world.openedChests, id); }
    public void MarkChestOpened(string id) { AddUnique(Data.world.openedChests, id); }

    public bool IsObjectBroken(string id) { return Contains(Data.world.brokenObjects, id); }
    public void MarkObjectBroken(string id) { AddUnique(Data.world.brokenObjects, id); }

    public bool IsItemObtained(string id) { return Contains(Data.world.gotLored, id); }
    public void MarkItemObtained(string id) { AddUnique(Data.world.gotLored, id); }

    public bool IsPuzzleSolved(string id) { return Contains(Data.world.solvedPuzzles, id); }
    public void MarkPuzzleSolved(string id) { AddUnique(Data.world.solvedPuzzles, id); }

    public bool IsBossDefeated(string id) { return Contains(Data.world.defeatedBosses, id); }
    public void MarkBossDefeated(string id) { AddUnique(Data.world.defeatedBosses, id); }

    // ==================== quái thường (tạm, theo scene) ====================

    public bool IsEnemyDead(string scene, string enemyId)
    {
        SceneDeadEnemies entry = FindSceneEntry(scene, false);
        return entry != null && entry.enemyIDs.Contains(enemyId);
    }

    public void MarkEnemyDead(string scene, string enemyId)
    {
        SceneDeadEnemies entry = FindSceneEntry(scene, true);
        if (!entry.enemyIDs.Contains(enemyId))
            entry.enemyIDs.Add(enemyId);
    }

    private SceneDeadEnemies FindSceneEntry(string scene, bool createIfMissing)
    {
        foreach (SceneDeadEnemies entry in Data.temporary.deadEnemies)
        {
            if (entry.scene == scene)
                return entry;
        }

        if (!createIfMissing)
            return null;

        SceneDeadEnemies created = new SceneDeadEnemies { scene = scene };
        Data.temporary.deadEnemies.Add(created);
        return created;
    }

    // ==================== death drop ====================

    public void RecordDeathDrop(Vector3 position, int score)
    {
        Data.temporary.deathDrop = new DeathDropData
        {
            exists = true,
            sceneID = SceneManager.GetActiveScene().name,
            position = position,
            score = score,
        };
    }

    public bool TryGetDeathDrop(string scene, out DeathDropData drop)
    {
        drop = Data.temporary.deathDrop;
        return drop != null && drop.exists && drop.sceneID == scene;
    }

    public int ConsumeDeathDrop()
    {
        int score = Data.temporary.deathDrop.score;
        Data.temporary.deathDrop = new DeathDropData();
        return score;
    }

    // ==================== inventory ====================

    public void CaptureInventory(Inventory inventory)
    {
        if (inventory == null)
            return;

        Data.player.inventory.Clear();
        foreach (Inventory.ItemStack stack in inventory.items)
        {
            if (stack == null || stack.amount <= 0)
                continue;
            Data.player.inventory.Add(new SavedItemStack { itemName = stack.itemName, amount = stack.amount, isEquipped = stack.isEquipped });
        }
    }

    private void ApplyInventory(Inventory inventory)
    {
        if (inventory == null)
            return;

        inventory.items.Clear();
        foreach (SavedItemStack saved in Data.player.inventory)
        {
            inventory.items.Add(new Inventory.ItemStack
            {
                itemPrefab = InventoryDatabase.Instance.GetItemByName(saved.itemName).itemPrefab,
                itemName = saved.itemName,
                amount = saved.amount,
                Data = InventoryDatabase.Instance.GetItemDataByName(saved.itemName),
                isEquipped = saved.isEquipped,
            });
        }

        inventory.RestoreEquippedBuffs();
        
    }


    // ==================== khôi phục khi vào scene ====================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
        AttachEnemyPersistence(scene);
        
        

        if (!pendingContinueRestore)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;   // scene menu/cutscene chưa có player — chờ scene sau

        pendingContinueRestore = false;
        ApplyInventory(playerObject.GetComponent<Inventory>());

        // Continue đúng scene đã save -> đặt player về checkpoint gần nhất
        if (scene.name == Data.currentScene && Data.hasCheckpointPosition)
        {
            PlayerController controller = playerObject.GetComponent<PlayerController>();
            if (controller != null)
                controller.Teleport(Data.checkpointPosition);
        }

        WriteToDisk();
    }

    /// <summary>
    /// Quái thường (mọi object có EnemyMove — cùng tiêu chí với
    /// SaveZone.RespawnRegularEnemies): con nào đã chết trong dữ liệu tạm thì
    /// tắt luôn khi vào lại scene; con sống thì gắn reporter để ghi nhận chết.
    /// Không cần thêm gì vào prefab.
    /// </summary>
    private void AttachEnemyPersistence(Scene scene)
    {
        EnemyMove[] enemies = FindObjectsByType<EnemyMove>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EnemyMove enemy in enemies)
        {
            if (enemy == null || enemy.gameObject.scene != scene)
                continue;
            if (enemy.GetComponent<EnemyDeathReporter>() != null)
                continue;

            string id = SaveIdUtility.For(enemy);
            if (IsEnemyDead(scene.name, id))
            {
                enemy.gameObject.SetActive(false);
                continue;
            }

            EnemyDeathReporter reporter = enemy.gameObject.AddComponent<EnemyDeathReporter>();
            reporter.Init(scene.name, id);
        }
    }

    // ==================== helpers ====================

    private static bool Contains(List<string> list, string id)
    {
        return !string.IsNullOrEmpty(id) && list.Contains(id);
    }

    private static void AddUnique(List<string> list, string id)
    {
        if (!string.IsNullOrEmpty(id) && !list.Contains(id))
            list.Add(id);
    }
}
