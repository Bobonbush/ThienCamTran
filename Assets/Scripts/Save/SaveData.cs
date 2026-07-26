using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toàn bộ dữ liệu của MỘT slot save (Save_x.json). Ba nhóm tách bạch:
/// - PlayerData / WorldData: vĩnh viễn (equipment, inventory, rương đã mở,
///   tường đã vỡ, boss đã hạ, puzzle đã giải...).
/// - TemporaryWorldData: reset khi nghỉ ở Save Zone (quái thường đã chết
///   theo từng scene, vị trí rơi đồ khi chết) — vẫn ghi vào file để thoát
///   game vào lại không mất.
/// Settings KHÔNG nằm ở đây — SettingsService lo riêng, dùng chung mọi slot.
/// JsonUtility không serialize được HashSet/Dictionary nên dùng List.
/// </summary>
[Serializable]
public class GameSaveData
{
    public PlayerData player = new PlayerData();
    public WorldData world = new WorldData();
    public TemporaryWorldData temporary = new TemporaryWorldData();
    public string currentScene = "";
    public string lastSaveZoneID = "";
    public Vector3 checkpointPosition;
    public bool hasCheckpointPosition;
}

[Serializable]
public class PlayerData
{
    public List<string> equipmentIDs = new List<string>();
    public List<SavedItemStack> inventory = new List<SavedItemStack>();
    public int score;
}

[Serializable]
public class SavedItemStack
{
    public string itemName;
    public int amount;
    public bool isEquipped;
}

[Serializable]
public class WorldData
{
    public List<string> brokenObjects = new List<string>();
    public List<string> defeatedBosses = new List<string>();
    public List<string> openedChests = new List<string>();
    public List<string> solvedPuzzles = new List<string>();
    public List<string> gotLored = new List<string>();
    public bool isSteelMode = false;
}

[Serializable]
public class TemporaryWorldData
{
    public List<SceneDeadEnemies> deadEnemies = new List<SceneDeadEnemies>();
    public DeathDropData deathDrop = new DeathDropData();
}

[Serializable]
public class SceneDeadEnemies
{
    public string scene;
    public List<string> enemyIDs = new List<string>();
}

[Serializable]
public class DeathDropData
{
    public bool exists;
    public string sceneID;
    public Vector3 position;
    public int score;
}
