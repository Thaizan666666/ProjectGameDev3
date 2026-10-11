using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Building base class implementing interaction, teleportation, and upgrade systems.
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
    public GameObject UI_Interaction;

    [Header("Teleport")]
    [SerializeField] private Teleport _teleport;

    // ─────────────────────────────────────────────────────────────
    // Private State
    // ─────────────────────────────────────────────────────────────
    private bool _isPlayerInRange;
    private GameObject _player; // Cached player reference

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
        if (UI_Interaction != null)
            UI_Interaction.SetActive(isHighlighted);
    }

    public void Interact()
    {
        // Resolve player reference (fallback if cached ref was lost)
        ResolvePlayer();

        if (_player == null)
        {
            LogError("Player not found — check 'Player' tag exists in scene");
            return;
        }

        if (_teleport == null)
        {
            LogError("Teleport component missing on building");
            return;
        }

        if (!_teleport.CanTeleport)
        {
            LogWarning($"Teleport disabled for {buildingName} (CanTeleport=false)");
            return;
        }

        if (_teleport.destination == null)
        {
            LogError("Teleport destination not assigned");
            return;
        }

        Log($"Interact triggered — teleporting to {_teleport.destination.name}");
        _teleport.TryTeleport(_player);
    }

    // ─────────────────────────────────────────────────────────────
    // Trigger-based UI Handling
    // ─────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _player = other.gameObject;
        _isPlayerInRange = true;
        _teleport?.SetTeleportEnabled(true);

        if (UI_Interaction != null)
            UI_Interaction.SetActive(true);

        Log($"Player entered — UI shown, teleport enabled");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject != _player) return;

        _isPlayerInRange = false;
        _player = null;
        _teleport?.SetTeleportEnabled(false);

        if (UI_Interaction != null)
            UI_Interaction.SetActive(false);

        Log($"Player exited — UI hidden, teleport disabled");
    }

    // ─────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (_teleport == null)
            _teleport = GetComponent<Teleport>();
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
    private void ResolvePlayer()
    {
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Log(string msg) => Debug.Log($"[Building:{buildingName}] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[Building:{buildingName}] {msg}");
    private void LogError(string msg) => Debug.LogError($"[Building:{buildingName}] {msg}");
}
