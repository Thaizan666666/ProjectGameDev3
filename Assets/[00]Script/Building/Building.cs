// using System;
// using PlayerNormal.Project_wide;
// using UnityEngine;

// public class Building : MonoBehaviour, IInteractable, IUpgradable
// {
//     private PlayerInteract playerInteract;
//     private Player player;

//     public BuildingData data;
//     private GameObject newPrefab;

//     public int requireMoney = 300;
//     public bool canUpgrade;
//     public int currentLevel = 1;
//     int maxLevel = 3;
//     void Awake()
//     {
//         canUpgrade = false;
//         Debug.Log($"canUpgrade is {canUpgrade}");
//     }

//     public void SetData(BuildingData newData)
//     {
//         data = newData;

//         if (data == null)
//         {
//             Debug.LogWarning($"{name}: null data");
//             return;
//         }

//         newPrefab = data.buildingLV_1;
//     }

//     public void OnTriggerEnter(Collider other)
//     {
//         canUpgrade = true;
//         playerInteract = other.GetComponent<PlayerInteract>();

//         Debug.Log($"Area upgrade has triggered and canUpgrade is {canUpgrade}");
//     }

//     void OnTriggerStay(Collider other)
//     {
//         if (playerInteract.isPlayerInteract)
//         {
//             OnActive(other.gameObject);
//             playerInteract.isPlayerInteract = false;
//         }
//     }

//     public void OnActive(GameObject gameObject)
//     {
//         player = gameObject.GetComponent<Player>();

//         if(player.money >= requireMoney && canUpgrade)
//         {
//             player.money -= requireMoney;
//             Upgrade();
//         }
//         else
//         {
//             Debug.LogWarning("Haven't enough resource to upgrade or can't upgrade right now.");
//             canUpgrade = false;
//             playerInteract.isPlayerInteract = false;
//             return;
//         }
//     }

//     public void Upgrade()
//     {
//         if (currentLevel < maxLevel)
//         {
//             currentLevel += 1;
//             DecideModel();
//         }
//         else 
//         {
//             Debug.Log("This building has level-max.");
//             return;
//         }
//     }
//     public void DecideModel()
//     {
//         switch (currentLevel)
//         {
//             case 1:
//                 newPrefab = data.buildingLV_1;
//                 ChangeModel(1);
//                 break;
//             case 2:
//                 newPrefab = data.buildingLV_2;
//                 ChangeModel(2);
//                 Destroy(gameObject);
//                 break;
//             case 3:
//                 newPrefab = data.buildingLV_3;
//                 ChangeModel(3);
//                 Destroy(gameObject);
//                 break;
//             default:
//                 newPrefab = data.buildingLV_1;
//                 ChangeModel(1);
//                 Destroy(gameObject);
//                 break;
//         }
//     }

//     public void ChangeModel(int level)
//     {
//         Debug.Log($"This building is level {level}");
//         // GameObject newObject = Instantiate(newPrefab);
//     }

//     public void OnDisactive()
//     {
//         Debug.Log("This is OnDisactive");
//     }

//     public void OnTriggerExit(Collider other)
//     {
//         canUpgrade = false;
//         Debug.Log($"canUpgrade is {canUpgrade}");
//     }
// }

#region Test
using System.Data.Common;
using PlayerNormal.Project_wide;
using UnityEngine;

public class Building : MonoBehaviour, IInteractable, IUpgradable
{
    private PlayerInteract playerInteract;
    private Player player;

    public BuildingData data;

    public int requireMoney;
    public bool canUpgrade;
    public int currentLevel = 1;

    private void Awake()
    {
        canUpgrade = false;
    }

    public void SetData(BuildingData newData)
    {
        data = newData;

        if (data == null)
            Debug.LogWarning($"{name}: null data");
    }

    private void OnTriggerEnter(Collider other)
    {
        playerInteract = other.GetComponent<PlayerInteract>();
        if (playerInteract == null) return;

        canUpgrade = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (playerInteract == null) return;
        if (!playerInteract.isPlayerInteract) return;

        OnActive(other.gameObject);
        playerInteract.isPlayerInteract = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInteract>() == null) return;

        canUpgrade = false;
        playerInteract = null;
    }

    public void OnActive(GameObject interactor)
    {
        player = interactor.GetComponent<Player>();
        if (player == null) return;

        if (!canUpgrade || player.money < requireMoney)
        {
            Debug.LogWarning("Not enough money or can't upgrade right now.");
            return;
        }

        player.money -= requireMoney;
        Upgrade();
    }

    public void Upgrade()
    {
        if (currentLevel >= 3)
        {
            Debug.Log("This building has level-max.");
            return;
        }

        currentLevel++;
        DecideModel();
    }

    private void DecideModel()
    {
        GameObject prefab = currentLevel switch
        {
            1 => data.buildingLV_1,
            2 => data.buildingLV_2,
            3 => data.buildingLV_3,
            _ => data.buildingLV_1
        };

        ChangeModel(prefab);
    }

    private void ChangeModel(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{name}: missing prefab for level {currentLevel}");
            return;
        }

        // spawn as sibling at same position/parent — NOT a child of this object
        GameObject newObject = Instantiate(prefab, transform.position, transform.rotation, transform.parent);

        Building newBuilding = newObject.GetComponent<Building>();
        if (newBuilding != null)
        {
            newBuilding.SetData(data);
            newBuilding.currentLevel = currentLevel;
            newBuilding.requireMoney = requireMoney;
        }

        Destroy(gameObject);
    }

    public void OnDisactive()
    {
        Debug.Log("This is OnDisactive");
    }
}

// using PlayerNormal.Project_wide;
// using UnityEngine;

// public class Building : MonoBehaviour, IInteractable, IUpgradable
// {
//     private PlayerInteract playerInteract;
//     private Player player;

//     public BuildingData data;

//     public bool canUpgrade;
//     public int currentLevel = 1;

//     private void Awake()
//     {
//         canUpgrade = false;
//     }

//     public void SetData(BuildingData newData)
//     {
//         data = newData;

//         if (data == null)
//             Debug.LogWarning($"{name}: null data");
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         playerInteract = other.GetComponent<PlayerInteract>();
//         if (playerInteract == null) return;

//         canUpgrade = true;
//     }

//     private void OnTriggerStay(Collider other)
//     {
//         if (playerInteract == null) return;
//         if (!playerInteract.isPlayerInteract) return;

//         OnActive(other.gameObject);
//         playerInteract.isPlayerInteract = false;
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (other.GetComponent<PlayerInteract>() == null) return;

//         canUpgrade = false;
//         playerInteract = null;
//     }

//     public void OnActive(GameObject interactor)
//     {
//         player = interactor.GetComponent<Player>();
//         if (player == null || data == null) return;

//         int cost = data.GetUpgradeCost(currentLevel + 1);

//         Debug.Log($"max level is {data.MaxLevel}");

//         if (!canUpgrade || cost < 0 || player.money < cost)
//         {
//             Debug.LogWarning("Not enough money or can't upgrade right now.");
//             return;
//         }

//         player.money -= cost;
//         Upgrade();
//     }

//     public void Upgrade()
//     {
//         if (data == null) return;

//         if (currentLevel >= data.MaxLevel)
//         {
//             Debug.Log("This building has level-max.");
//             return;
//         }

//         currentLevel++;
//         DecideModel();
//     }

//     private void DecideModel() => ChangeModel(data.GetPrefab(currentLevel));

//     private void ChangeModel(GameObject prefab)
//     {
//         if (prefab == null)
//         {
//             Debug.LogWarning($"{name}: missing prefab for level {currentLevel}");
//             return;
//         }

//         GameObject newObject = Instantiate(prefab, transform.position, transform.rotation, transform.parent);

//         Building newBuilding = newObject.GetComponent<Building>();
//         if (newBuilding != null)
//         {
//             newBuilding.SetData(data);
//             newBuilding.currentLevel = currentLevel;
//         }

//         Destroy(gameObject);
//     }

//     public void OnDisactive() => Debug.Log("This is OnDisactive");
// }
#endregion