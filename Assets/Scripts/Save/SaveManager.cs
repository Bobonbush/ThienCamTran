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

    /// <summary>Scene trung gian giữ các manager thường trú (SceneTransitionManager,
    /// CutSceneManager, InputManager...). Main menu luôn đi qua đây trước khi vào game.</summary>
    public const string BootScene = "StartGame";

    /// <summary>Scene main menu — nơi quay về khi kết thúc một lượt chơi.</summary>
    public const string MainMenuScene = "MainMenuNew";

    /// <summary>Số slot main menu hiển thị (Slot_1..Slot_4).</summary>
    public const int SlotCount = 4;

    private static SaveManager instance;

    // For testing save game

    private int useSlot = 2;

    private bool currentlyInGame = false;

    // Ý định chọn từ main menu, tiêu thụ khi BootScene load xong
    private int pendingSlot = -1;
    private bool pendingNewGame;
    private bool pendingSteelMode;
    private int pendingIcon;



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
            Destroy(gameObject);
            return;
        }

        instance = this;
        
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void Start()
    {
#if UNITY_EDITOR
        // Tiện test: bấm Play thẳng từ một scene gameplay thì tự vào useSlot.
        // Menu và BootScene phải bỏ qua — CutSceneManager không tồn tại ở menu,
        // và BootScene chỉ được vào qua StartGameFromMenu/StartNewGameFromMenu.
        string activeScene = SceneManager.GetActiveScene().name;
        if (currentlyInGame == false && activeScene != MainMenuScene && activeScene != BootScene)
        {
            CutSceneManager.Instance.ForceHideGamePlay();
            Load(useSlot);
        }
#endif
    }

    private void Update()
    {
        if (currentlyInGame)
            Data.player.playTime += Time.deltaTime;
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

        player = ReacquirePlayer();
        if (player == null)
        {
            Debug.LogError("SaveManager: không tìm thấy Player sau khi load scene " + Data.currentScene);
            yield return SceneTransitionManager.Instance.Fade(0.0f);
            yield break;
        }

        player.ResetFromDeath();

        yield return new WaitForSeconds(0.3f);
        RestAtSaveZoneFunction?.Invoke();

        player.ReleaseLockCutScene();
        yield return new WaitForSeconds(0.2f);

        yield return SceneTransitionManager.Instance.Fade(0.0f);
    }

    /// <summary>Player cũ bị huỷ cùng scene cũ mỗi lần LoadSceneAsync, nên sau khi đổi
    /// scene phải lấy lại bản mới. SceneTransitionManager đã rebind sẵn ở sceneLoaded.</summary>
    private static PlayerController ReacquirePlayer()
    {
        SceneTransitionManager transitions = SceneTransitionManager.Instance;
        return transitions != null ? transitions.CurrentPlayer : null;
    }


    public void Load(int slot)
    {
        LoadIntoMemory(slot);

        if (currentlyInGame == false)
        {
            InitializeGame();
        }
        currentlyInGame = true;
    }

    /// <summary>Chỉ đọc file vào bộ nhớ, KHÔNG khởi động game. Tách ra để khi tạo
    /// game mới còn kịp ghi mode/icon vào Data trước khi InitializeGame chạy.</summary>
    private void LoadIntoMemory(int slot)
    {
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

        // Sau khi Load từ menu, lần vào scene đầu tiên sẽ khôi phục inventory + vị trí
        pendingContinueRestore = true;
    }

    /// <summary>Đọc thử một slot mà KHÔNG đụng vào CurrentSlot/Data — main menu
    /// dùng để vẽ 4 dòng slot.</summary>
    public bool TryPeekSlot(int slot, out GameSaveData data)
    {
        data = null;
        if (!HasSave(slot))
            return false;

        try
        {
            data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SlotPath(slot)));
        }
        catch (System.Exception e)
        {
            Debug.LogError("SaveManager: file save slot " + slot + " hỏng. " + e.Message);
            data = null;
        }

        return data != null;
    }

    // ==================== vào / ra game từ main menu ====================

    /// <summary>Main menu: chơi tiếp một slot đã có.</summary>
    public void StartGameFromMenu(int slot)
    {
        pendingSlot = slot;
        pendingNewGame = false;
        SceneManager.LoadScene(BootScene);
    }

    /// <summary>Main menu: tạo game mới ở slot này (xoá file cũ nếu có).</summary>
    public void StartNewGameFromMenu(int slot, bool steelMode, int iconIndex)
    {
        pendingSlot = slot;
        pendingNewGame = true;
        pendingSteelMode = steelMode;
        pendingIcon = iconIndex;
        SceneManager.LoadScene(BootScene);
    }

    /// <summary>Kết thúc một lượt chơi và quay về main menu. Phải đi qua đây thay vì
    /// LoadScene thẳng, nếu không currentlyInGame kẹt ở true và lần chọn slot sau sẽ
    /// đọc file nhưng không bao giờ chuyển scene.</summary>
    public void ReturnToMenu()
    {
        currentlyInGame = false;
        pendingSlot = -1;
        pendingContinueRestore = false;
        Data = new GameSaveData();

        // Các manager gameplay là DontDestroyOnLoad và giữ tham chiếu tới Player của
        // lượt chơi vừa rồi; SceneTransitionManager còn treo cả PersistentGameUI (HUD)
        // làm con. Không huỷ thì HUD đè lên menu và lượt chơi sau dùng lại tham chiếu chết.
        // StartGame sẽ dựng lại tất cả khi vào game lần nữa.
        SceneTransitionManager.TeardownForMenu();
        CutSceneManager.TeardownForMenu();
        InputManager.TeardownForMenu();

        SceneManager.LoadScene(MainMenuScene);
    }

    /// <summary>Thực thi lựa chọn từ main menu, chạy khi BootScene đã load xong
    /// (lúc này CutSceneManager / SceneTransitionManager mới tồn tại).</summary>
    private void ConsumePendingMenuChoice()
    {
        int slot = pendingSlot;
        pendingSlot = -1;

        CutSceneManager.Instance.ForceHideGamePlay();

        if (pendingNewGame)
            DeleteSave(slot);

        LoadIntoMemory(slot);

        if (pendingNewGame)
        {
            Data.world.isSteelMode = pendingSteelMode;
            Data.player.iconIndex = pendingIcon;
            Data.player.playTime = 0f;

            // Ghi ngay: bình thường chỉ checkpoint mới ghi file, không ghi ở đây thì
            // slot vừa tạo sẽ đọc lại thành trống/Classic nếu người chơi thoát sớm.
            WriteToDisk();
        }

        InitializeGame();
        currentlyInGame = true;
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

        // ReloadScene đã huỷ Player cũ — dùng lại tham chiếu cũ là MissingReferenceException.
        player = ReacquirePlayer();
        if (player == null)
        {
            Debug.LogError("SaveManager: không tìm thấy Player sau khi reload scene lúc nghỉ.");
            yield return SceneTransitionManager.Instance.Fade(0.0f);
            yield break;
        }

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

            player = ReacquirePlayer();
            if (player == null)
            {
                Debug.LogError("SaveManager: không tìm thấy Player sau khi hồi sinh ở " + Data.currentScene);
                yield return SceneTransitionManager.Instance.Fade(0.0f);
                yield break;
            }

            player.ResetFromDeath();

            yield return new WaitForSeconds(0.3f);
            RestAtSaveZoneFunction?.Invoke();

            player.ReleaseLockCutScene();
            yield return new WaitForSeconds(0.2f);
        }else
        {
            // Steel mode: chỉ có một mạng. Xoá file NGAY tại đây chứ không đợi hết
            // cutscene, để thoát game giữa chừng cũng không cứu được lượt chơi.
            DeleteSave(CurrentSlot);
            yield return SceneTransitionManager.Instance.TransitionToScene("hard", Vector3.zero);
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
        // Bàn giao từ main menu: SaveManager là DontDestroyOnLoad nên Start() chỉ
        // chạy đúng một lần, không dùng làm chỗ móc được.
        if (scene.name == BootScene && pendingSlot >= 0)
        {
            ConsumePendingMenuChoice();
            return;
        }

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
