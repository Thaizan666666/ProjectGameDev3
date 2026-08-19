using UnityEngine;

/// <summary>One level entry: prefab + cost to reach this level. Index+1 = level number.</summary>
[System.Serializable]
public class BuildingLevelData
{
    public GameObject prefab;
    public int upgradeCost; // Cost to upgrade TO this level (level 1 = repair cost)
}
