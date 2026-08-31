using System;
using PlayerNormal.Project_wide;
using Unity.VisualScripting;
using UnityEngine;

namespace PlayerNormal.Project_wide
{
    public class Building : MonoBehaviour, IInteractable, IUpgradable
{
    [Header("Select building in Inspector (data loads at runtime)")]
    [SerializeField] private BuildingName buildingName;

    [Header("Runtime state (read-only in Inspector)")]
    public int currentLevel = 1;
    public bool canUpgrade;

    public BuildingData data;
    private PlayerInteract playerInteract;
    private Player player;

    private void Awake()
    {
        canUpgrade = false;
    }

    private void Start()
    {
        // Load data from BuildingDatabase by buildingName (like Fish pattern)
        BuildingDatabase db = FindFirstObjectByType<BuildingDatabase>();
        if (db != null)
        {
            data = db.GetByName(buildingName);
            if (data != null)
            {
                Debug.Log($"[Building] {buildingName} loaded: {data.levels.Count} levels, MaxLevel={data.MaxLevel}");
            }
            else
            {
                Debug.LogWarning($"[Building] No data found for {buildingName}");
            }
        }
        else
        {
            Debug.LogWarning("[Building] No BuildingDatabase found in scene");
        }
    }

    /// <summary>Alternative: set data directly (for spawning new building at runtime)</summary>
    public void SetData(BuildingData newData, int level = 1)
    {
        data = newData;
        currentLevel = level;
        if (data == null)
            Debug.LogWarning($"{name}: null data");
    }

    private void OnTriggerEnter(Collider other)
    {
        playerInteract = other.GetComponent<PlayerInteract>();
        if (playerInteract == null) return;
        canUpgrade = true;

        Debug.Log($"canUpgrade is {canUpgrade}");
    }

    private void OnTriggerStay(Collider other)
    {
        if (playerInteract == null) return;
        if (!playerInteract.isPlayerInteract) return;

        Debug.Log($"canUpgrade is {canUpgrade}");
        OnActive(other.gameObject);
        playerInteract.isPlayerInteract = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInteract>() == null) return;
        canUpgrade = false;
        playerInteract = null;

        Debug.Log($"canUpgrade is {canUpgrade}");
    }

    public void OnActive(GameObject interactor)
    {
        player = interactor.GetComponent<Player>();
        if (player == null || data == null) return;

        // Dynamic cost: next level's upgrade cost
        // int nextLevel = currentLevel + 1;
        // int cost = data.GetUpgradeCost(nextLevel);

        // Debug.Log($"[Building] {buildingName} Lv.{currentLevel} → next cost: {cost}, player money: {player.money}");

        // if (!canUpgrade || cost < 0 || player.money < cost)
        // {
        //     Debug.LogWarning("[Building] Not enough money or can't upgrade right now.");
        //     return;
        // }

        // player.money -= cost;
        // Upgrade();
        Debug.Log("OnAcive");
        UpgradeManager.Instance.TryRequestUpgrade(this);
    }

    public void Upgrade()
    {
        if (data == null) return;

        if (currentLevel >= data.MaxLevel)
        {
            Debug.Log($"[Building] {buildingName} is already at max level ({data.MaxLevel}).");
            return;
        }

        currentLevel++;
        DecideModel();
    }

    private void DecideModel() => ChangeModel(data.GetPrefab(currentLevel));

    private void ChangeModel(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[Building] {name}: missing prefab for level {currentLevel}");
            return;
        }

        // Spawn as sibling at same position/parent — NOT a child of this object
        GameObject newObject = Instantiate(prefab, transform.position, transform.rotation, transform.parent);

        Building newBuilding = newObject.GetComponent<Building>();
        if (newBuilding != null)
        {
            newBuilding.SetData(data, currentLevel);
        }

        Debug.Log($"[Building] {buildingName} upgraded to Lv.{currentLevel}");
        Destroy(gameObject);
    }

    public void OnDisactive()
    {
        Debug.Log($"[Building] {buildingName} OnDisactive");
    }
}
}

