using System;
using System.Collections.Generic;
using PlayerNormal.Project_wide;
using UnityEngine;
using Yarn.Unity;

public class UpgradeManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static UpgradeManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerWallet walletComponent;
    [SerializeField] private GameTimeSystem gameTime;

    private IWallet wallet;

    // ── Upgrade Queue ─────────────────────────────────────────

    private struct PendingUpgrade
    {
        public BuildingName buildingName;
        public int targetLevel;
    }

    private readonly List<PendingUpgrade> _queue = new();
    private readonly HashSet<BuildingName> _queuedNames = new();

    private void Awake()
    {
        Instance = this;
        wallet = walletComponent;

        if (wallet == null)
            Debug.LogWarning($"{name}: missing PlayerWallet reference");
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

    /// <summary>
    /// เรียกจาก Yarn: <<canTryUpgrade "TheSeagullInn">>
    /// static method → ไม่ต้องใส่ชื่อ GameObject
    /// </summary>
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
