using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveZone : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;
    [SerializeField] private CheckpointWorldUI checkpointUI;

    // Kept serialized for compatibility with existing prefab/scene data.
    public int inUsed;

    private int collisionCnt;
    private PlayerController activePlayer;
    private bool openStatusAfterExit;

    public bool CanInteract => collisionCnt > 0 && activePlayer == null;
    public bool IsMenuOpen => checkpointUI != null && checkpointUI.IsOpen;

    private void Awake()
    {
        if (checkpointUI == null)
            checkpointUI = GetComponentInChildren<CheckpointWorldUI>(true);
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract || player == null || left == null || right == null)
            return;

        activePlayer = player;
        openStatusAfterExit = false;

        if (player.EnterSaving(left.position, right.position, this))
            inUsed = 1;
        else
            activePlayer = null;
    }

    public new IInteractable.Type GetType()
    {
        return IInteractable.Type.Save;
    }

    public void ShowMenu(PlayerController player)
    {
        if (player != activePlayer || checkpointUI == null)
            return;
        SaveCheckpoint(player);
        checkpointUI.Open(this, player);
        // TODO(GLOBAL_SAVE): trigger the global-save implementation here.
        checkpointUI.NotifySaveCompleted();
    }

    public void RestFromUI(PlayerController player)
    {
        if (player == null || player != activePlayer)
            return;

        RestorePlayer(player);
        SaveManager.Instance.RestAtSaveZone(SaveIdUtility.For(this), player);

        //CloseMenu(false);
    }



    public void StatusFromUI()
    {
        CloseMenu(true);
    }

    public void CancelFromUI()
    {
        CloseMenu(false);
    }

    public void OnSavingAnimationExited(PlayerController player)
    {
        if (player != activePlayer)
            return;

        checkpointUI?.Hide();
        activePlayer = null;
        inUsed = 0;

        if (!openStatusAfterExit)
            return;

        openStatusAfterExit = false;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.OpenMenu("Status");
        else
            Debug.LogError("SceneTransitionManager is unavailable, so Status cannot open.", this);
    }

    private void CloseMenu(bool openStatus)
    {
        if (activePlayer == null)
        {
            checkpointUI?.Hide();
            inUsed = 0;
            return;
        }

        openStatusAfterExit = openStatus;
        checkpointUI?.Hide();

        if (!activePlayer.ExitSaving())
            Debug.LogWarning("Checkpoint UI requested an exit before the saving animation was ready.", this);
    }

    private void RestorePlayer(PlayerController player)
    {
        Damageable damageable = player.GetComponent<Damageable>();
        if (damageable != null)
        {
            int restored = Mathf.Max(0, damageable.MaxHealth - damageable.Health);
            damageable.Health = damageable.MaxHealth;
            if (restored > 0)
                CharacterEvents.characterHealed?.Invoke(player.gameObject, restored);
        }

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.Mana = stats.MaxMana;
        }

        player.SetSafeGround(player.transform.position);
    }

    private void RespawnRegularEnemies()
    {
        EnemyMove[] enemies = Object.FindObjectsByType<EnemyMove>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (EnemyMove enemy in enemies)
        {
            if (enemy == null || enemy.gameObject.scene != gameObject.scene)
                continue;

            Damageable damageable = enemy.GetComponent<Damageable>();
            if (damageable == null || damageable.IsAlive)
                continue;

            enemy.gameObject.SetActive(true);
            enemy.transform.position = enemy.spawn;
            enemy.DenyDesireMove();

            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            if (body != null)
                body.linearVelocity = Vector2.zero;

            damageable.Health = damageable.MaxHealth;
            damageable.IsAlive = true;

            EnemyDeathReporter reporter = enemy.GetComponent<EnemyDeathReporter>();
            if (reporter != null)
                reporter.ResetReport();
        }
    }

    private void SaveCheckpoint(PlayerController player)
    {
        // SaveManager ghi Save_x.json (scene, save zone, vị trí, inventory)
        // và tự viết lại các key PlayerPrefs Checkpoint.* cũ cho tương thích.
        SaveManager.Instance.CheckpointSave(SaveIdUtility.For(this), player);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            collisionCnt++;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            collisionCnt = Mathf.Max(0, collisionCnt - 1);
    }
}
