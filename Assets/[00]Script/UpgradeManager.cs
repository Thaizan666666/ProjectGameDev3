using System;
using System.Collections.Generic;
using PlayerNormal.Project_wide;
using UnityEngine;
using Yarn.Unity;
using TableForge.Equipment.Fishing;

public class UpgradeManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static UpgradeManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerWallet walletComponent;
    [SerializeField] private GameTimeSystem gameTime;
    [Header("Rod Upgrade")]
    [SerializeField] public RodStats rodStats; // Reference to your Rod.asset (RodStats ScriptableObject)

    private IWallet wallet;

    // ── Upgrade Queue ─────────────────────────────────────────

    private struct PendingUpgrade
    {
        public BuildingName buildingName;
        public int targetLevel;
    }

    private readonly List<PendingUpgrade> _queue = new();
    private readonly HashSet<BuildingName> _queuedNames = new();

    // ── Rod Upgrade State ─────────────────────────────────────
    private bool _lastRodUpgradeResult;
    private string _rodUpgradeFailReason = ""; // "no_rod", "max_level", "not_enough_money"

    private void Awake()
    {
        Instance = this;
        wallet = walletComponent;

        if (wallet == null)
            Debug.LogWarning($"{name}: missing PlayerWallet reference");
        
        if (rodStats == null)
            Debug.LogWarning($"{name}: missing RodStats reference for rod upgrades");
    }

    private void OnEnable()
    {
        if (gameTime != null)
            gameTime.OnDayChanged += OnDayChanged;
    }

    private void OnDisable()
    {
        if (gameTime != null)
            gameTime.OnDayChanged -= OnDayChanged;
    }

    // ── Public API ────────────────────────────────────────────

    public bool TryRequestUpgrade(Building building)
    {
        if (building == null || building.data == null)
        {
            Debug.LogWarning("[UpgradeManager] Building or data is null.");
            return false;
        }

        int targetLevel = building.currentLevel + 1;
        int cost = building.data.GetUpgradeCost(targetLevel);

        if (cost < 0)
        {
            Debug.LogWarning($"[UpgradeManager] {building.BuildingName} already at max level.");
            return false;
        }

        if (_queuedNames.Contains(building.BuildingName))
        {
            Debug.LogWarning($"[UpgradeManager] {building.BuildingName} already queued for upgrade.");
            return false;
        }

        if (!wallet.TrySpend(cost))
        {
            Debug.LogWarning($"[UpgradeManager] Not enough money to upgrade {building.BuildingName} (need {cost}).");
            return false;
        }

        _queue.Add(new PendingUpgrade
        {
            buildingName = building.BuildingName,
            targetLevel = targetLevel
        });
        _queuedNames.Add(building.BuildingName);

        Debug.Log($"[UpgradeManager] {building.BuildingName} queued → Lv.{targetLevel} (money deducted: {cost})");
        return true;
    }

    public int PendingCount => _queue.Count;

    // ── Yarn Dialogue Check (static — no GameObject lookup needed) ──

    private bool _lastCheckResult;

    [YarnFunction("canUpgradeSuccess")]
    public static bool GetCanUpgradeSuccess()
    {
        return Instance != null && Instance.LastCheckResult;
    }

    [YarnCommand("canTryUpgrade")]
    public static void CanTryUpgrade(string buildingName)
    {
        var inst = Instance;
        inst._lastCheckResult = false;

        if (string.IsNullOrEmpty(buildingName))
        {
            Debug.LogWarning("[UpgradeManager] canTryUpgrade: empty buildingName.");
            return;
        }

        if (!Enum.TryParse<BuildingName>(buildingName, out var name))
        {
            Debug.LogWarning($"[UpgradeManager] canTryUpgrade: unknown BuildingName '{buildingName}'.");
            return;
        }

        Building b = Building.GetByName(name);
        if (b == null || b.data == null)
        {
            Debug.LogWarning($"[UpgradeManager] canTryUpgrade: Building '{buildingName}' not found.");
            return;
        }

        int targetLevel = b.currentLevel + 1;
        int cost = b.data.GetUpgradeCost(targetLevel);

        if (cost < 0)
        {
            Debug.Log($"[UpgradeManager] canTryUpgrade: {name} at max level.");
            return;
        }

        if (inst._queuedNames.Contains(name))
        {
            Debug.Log($"[UpgradeManager] canTryUpgrade: {name} already queued.");
            return;
        }

        if (!inst.wallet.TrySpend(cost))
        {
            Debug.Log($"[UpgradeManager] canTryUpgrade: not enough money for {name} (need {cost}).");
            return;
        }

        inst._queue.Add(new PendingUpgrade
        {
            buildingName = name,
            targetLevel = targetLevel
        });
        inst._queuedNames.Add(name);

        inst._lastCheckResult = true;
        Debug.Log($"[UpgradeManager] canTryUpgrade: {name} queued → Lv.{targetLevel} (money deducted: {cost})");
    }

    /// <summary>อ่านผลลัพธ์ล่าสุดจาก canTryUpgrade</summary>
    public bool LastCheckResult => _lastCheckResult;

    // ── Rod Upgrade Yarn Integration ──────────────────────────

    [YarnFunction("canUpgradeRodSuccess")]
    public static bool GetCanUpgradeRodSuccess()
    {
        return Instance != null && Instance._lastRodUpgradeResult;
    }

    [YarnFunction("rodUpgradeFailReason")]
    public static string GetRodUpgradeFailReason()
    {
        return Instance != null ? Instance._rodUpgradeFailReason : "no_instance";
    }

    [YarnFunction("getRodCurrentLevel")]
    public static int GetRodCurrentLevel()
    {
        if (Instance == null) return -1;
        var result = Inventory.instance.FindRodInHotbarOrInventory();
        return result.currentLevel;
    }

    [YarnFunction("getRodUpgradeCost")]
    public static int GetRodUpgradeCost()
    {
        if (Instance == null || Instance.rodStats == null) return -1;
        var result = Inventory.instance.FindRodInHotbarOrInventory();
        if (result.currentLevel <= 0) return -1;
        
        int nextLevel = result.currentLevel + 1;
        // FIX: Check bounds before accessing array
        if (nextLevel > Instance.rodStats.levelRod.Count) return -1;
        
        return Instance.rodStats.levelRod[nextLevel - 1].upgradeCostRod;
    }

    [YarnFunction("getRodMaxLevel")]
    public static int GetRodMaxLevel()
    {
        if (Instance == null || Instance.rodStats == null) return 0;
        return Instance.rodStats.levelRod.Count;
    }

    [YarnFunction("getRodNameAtLevel")]
    public static string GetRodNameAtLevel(int level)
    {
        if (Instance == null || Instance.rodStats == null) return "";
        if (level <= 0 || level > Instance.rodStats.levelRod.Count) return "";
        return Instance.rodStats.levelRod[level - 1].rodName.ToString();
    }

    [YarnFunction("getRodTierAtLevel")]
    public static string GetRodTierAtLevel(int level)
    {
        if (Instance == null || Instance.rodStats == null) return "";
        if (level <= 0 || level > Instance.rodStats.levelRod.Count) return "";
        return Instance.rodStats.levelRod[level - 1].rodTier.ToString();
    }

    [YarnCommand("canTryUpgradeRod")]
    public static void CanTryUpgradeRod()
    {
        var inst = Instance;
        if (inst == null)
        {
            Debug.LogWarning("[UpgradeManager] CanTryUpgradeRod: Instance is null");
            return;
        }

        inst._lastRodUpgradeResult = false;
        inst._rodUpgradeFailReason = "";

        if (inst.rodStats == null)
        {
            Debug.LogWarning("[UpgradeManager] CanTryUpgradeRod: RodStats not assigned");
            inst._rodUpgradeFailReason = "no_data";
            return;
        }

        if (Inventory.instance == null)
        {
            Debug.LogWarning("[UpgradeManager] CanTryUpgradeRod: Inventory instance not found");
            inst._rodUpgradeFailReason = "no_inventory";
            return;
        }

        // Find rod in hotbar or inventory
        var (rodItem, currentLevel, slot) = Inventory.instance.FindRodInHotbarOrInventory();

        if (rodItem == null || currentLevel <= 0)
        {
            Debug.Log("[UpgradeManager] CanTryUpgradeRod: No rod found in hotbar or inventory");
            inst._rodUpgradeFailReason = "no_rod";
            return;
        }

        int maxLevel = inst.rodStats.levelRod.Count;
        if (currentLevel >= maxLevel)
        {
            Debug.Log($"[UpgradeManager] CanTryUpgradeRod: Rod already at max level ({maxLevel})");
            inst._rodUpgradeFailReason = "max_level";
            return;
        }

        int nextLevel = currentLevel + 1;
        int cost = inst.rodStats.levelRod[nextLevel - 1].upgradeCostRod;

        if (cost < 0)
        {
            Debug.LogWarning($"[UpgradeManager] CanTryUpgradeRod: Invalid cost for level {nextLevel}");
            inst._rodUpgradeFailReason = "invalid_cost";
            return;
        }

        if (!inst.wallet.TrySpend(cost))
        {
            Debug.Log($"[UpgradeManager] CanTryUpgradeRod: Not enough money (need {cost})");
            inst._rodUpgradeFailReason = "not_enough_money";
            return;
        }

        // Perform the upgrade immediately
        bool success = inst.PerformRodUpgrade(rodItem, slot, nextLevel);
        
        inst._lastRodUpgradeResult = success;
        if (!success)
        {
            inst._rodUpgradeFailReason = "upgrade_failed";
        }
    }

    /// <summary>
    /// Performs the actual rod upgrade - replaces the item's prefab reference
    /// </summary>
    private bool PerformRodUpgrade(ItemSO rodItem, Slot slot, int newLevel)
    {
        if (rodStats == null) return false;

        if (newLevel <= 0 || newLevel > rodStats.levelRod.Count) return false;
        GameObject newPrefab = rodStats.levelRod[newLevel - 1].prefabRod;
        if (newPrefab == null)
        {
            Debug.LogError($"[UpgradeManager] No prefab for rod level {newLevel}");
            return false;
        }

        // Update the item's prefab references
        rodItem.itemPrefab = newPrefab;
        rodItem.handItemPrefab = newPrefab;

        // Update the slot's visual if needed
        Inventory.instance.UpdateSlotVisual(slot);

        // Update carried item visual immediately
        Inventory.instance.UpdateCarriedItem();

        Debug.Log($"[UpgradeManager] Rod upgraded to Level {newLevel} (cost: {rodStats.levelRod[newLevel - 1].upgradeCostRod})");
        return true;
    }

    // ── Day Callback ──────────────────────────────────────────

    private void OnDayChanged(int newDay)
    {
        if (_queue.Count == 0) return;

        Debug.Log($"[UpgradeManager] ▶ Day {newDay} — processing {_queue.Count} pending upgrade(s)...");

        List<PendingUpgrade> batch = new(_queue);
        _queue.Clear();
        _queuedNames.Clear();

        foreach (var pending in batch)
        {
            Building b = Building.GetByName(pending.buildingName);

            if (b == null)
            {
                Debug.LogWarning($"[UpgradeManager] {pending.buildingName} not found in scene — skipping.");
                continue;
            }

            if (b.currentLevel + 1 != pending.targetLevel)
            {
                Debug.LogWarning(
                    $"[UpgradeManager] {pending.buildingName} level mismatch " +
                    $"(expected Lv.{pending.targetLevel}, current Lv.{b.currentLevel}) — skipping.");
                continue;
            }

            b.Upgrade();
        }

        Debug.Log($"[UpgradeManager] ✓ All upgrades processed for Day {newDay}.");
    }
}
