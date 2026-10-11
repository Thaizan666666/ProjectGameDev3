using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Building base class implementing upgrade system and interaction UI.
/// Teleportation is now handled by separate TeleportTrigger component.
/// Registers itself by BuildingName for global access.
/// </summary>
public class Building : MonoBehaviour, IInteractable, IUpgradable
{
    // ─────────────────────────────────────────────────────────────
    // Static Registry
    // ─────────────────────────────────────────────────────────────
    private static readonly Dictionary<BuildingName, Building> _instances = new();

    public static Building GetByName(BuildingName name)
        => _instances.TryGetValue(name, out var b) ? b : null;

    // ─────────────────────────────────────────────────────────────
    // Inspector Configuration
    // ─────────────────────────────────────────────────────────────
    [Header("Building Identity")]
    [SerializeField] private BuildingName buildingName;

    [Header("Runtime State (Read-only)")]
    public int currentLevel = 1;
    public BuildingData data;

    [Header("Interaction UI")]
    // public GameObject UI_Interaction;

    // ─────────────────────────────────────────────────────────────
    // Private State
    // ─────────────────────────────────────────────────────────────
    private bool _isPlayerInRange;

    // ─────────────────────────────────────────────────────────────
    // Public Properties
    // ─────────────────────────────────────────────────────────────
    public BuildingName BuildingName => buildingName;

    // ─────────────────────────────────────────────────────────────
    // IInteractable Implementation
    // ─────────────────────────────────────────────────────────────
    public Transform GetTransform() => transform;

    public bool CanInteract() => _isPlayerInRange;

    public void SetHighlighted(bool isHighlighted)
    {
        // if (UI_Interaction != null)
        //     UI_Interaction.SetActive(isHighlighted);
    }

    /// <summary>
    /// Interaction hook — override in derived classes for building-specific behavior.
    /// Teleport is now handled by TeleportTrigger, not here.
    /// </summary>
    public virtual void Interact()
    {
        Log($"Interact called on {buildingName} — override for custom behavior");
    }

    // ─────────────────────────────────────────────────────────────
    // Trigger-based UI Handling (UI only — no teleport)
    // ─────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        // if (!other.CompareTag("Player")) return;

        // _isPlayerInRange = true;

        // if (UI_Interaction != null)
        //     UI_Interaction.SetActive(true);

        // Log($"Player entered — UI shown");
    }

    private void OnTriggerExit(Collider other)
    {
        // if (other.gameObject != _player) return;

        // _isPlayerInRange = false;

        // if (UI_Interaction != null)
        //     UI_Interaction.SetActive(false);

        // Log($"Player exited — UI hidden");
    }

    // ─────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        // No teleport component lookup needed
    }

    private void OnEnable()
    {
        if (_instances.ContainsKey(buildingName))
            LogWarning($"Duplicate BuildingName '{buildingName}' on {name} — replacing previous");

        _instances[buildingName] = this;
    }

    private void OnDisable()
    {
        if (_instances.TryGetValue(buildingName, out var b) && b == this)
            _instances.Remove(buildingName);
    }

    private void Start()
    {
        LoadBuildingData();
    }

    // ─────────────────────────────────────────────────────────────
    // Data Loading & Setup
    // ─────────────────────────────────────────────────────────────
    private void LoadBuildingData()
    {
        var db = FindFirstObjectByType<BuildingDatabase>();
        if (db == null)
        {
            LogWarning("No BuildingDatabase found in scene");
            return;
        }

        data = db.GetByName(buildingName);
        if (data != null)
            Log($"Loaded: {data.levels.Count} levels, MaxLevel={data.MaxLevel}");
        else
            LogWarning($"No data found for {buildingName}");
    }

    /// <summary>Set data directly (for runtime spawning)</summary>
    public void SetData(BuildingData newData, int level = 1)
    {
        data = newData;
        currentLevel = level;
        if (data == null) LogWarning("Assigned null BuildingData");
    }

    // ─────────────────────────────────────────────────────────────
    // Upgrade System
    // ─────────────────────────────────────────────────────────────
    public void Upgrade()
    {
        if (data == null) return;

        if (currentLevel >= data.MaxLevel)
        {
            Log($"Already at max level ({data.MaxLevel})");
            return;
        }

        currentLevel++;
        ApplyUpgradeVisual();
    }

    private void ApplyUpgradeVisual()
    {
        var prefab = data.GetPrefab(currentLevel);
        if (prefab == null)
        {
            LogWarning($"Missing prefab for level {currentLevel}");
            return;
        }

        // Spawn new building as sibling (preserves position/rotation/parent)
        var newObject = Instantiate(prefab, transform.position, transform.rotation, transform.parent);
        var newBuilding = newObject.GetComponent<Building>();

        if (newBuilding != null)
            newBuilding.SetData(data, currentLevel);

        Log($"Upgraded to Lv.{currentLevel}");
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────
    // IUpgradable
    // ─────────────────────────────────────────────────────────────
    public void OnDisactive() => Log("OnDisactive called");

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────
    private void Log(string msg) => Debug.Log($"[Building:{buildingName}] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[Building:{buildingName}] {msg}");
    private void LogError(string msg) => Debug.LogError($"[Building:{buildingName}] {msg}");
}
