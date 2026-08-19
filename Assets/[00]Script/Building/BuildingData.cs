using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingData
{
    public BuildingName buildingName;
    public BuildingType buildingType;
    
    // Dynamic levels from BuildingStats
    public List<BuildingLevelData> levels = new();

    /// <summary>Cost to upgrade TO targetLevel (1 = repair, 2 = upgrade to Lv2, etc.)</summary>
    public int GetUpgradeCost(int targetLevel)
    {
        if (targetLevel <= 0 || targetLevel > levels.Count) return -1;
        return levels[targetLevel - 1].upgradeCost;
    }

    /// <summary>Prefab for the given level (1-indexed)</summary>
    public GameObject GetPrefab(int level)
    {
        if (level <= 0 || level > levels.Count) return null;
        return levels[level - 1].prefab;
    }

    /// <summary>Highest level that has a valid prefab</summary>
    public int MaxLevel
    {
        get
        {
            for (int i = levels.Count - 1; i >= 0; i--)
            {
                if (levels[i].prefab != null) return i + 1;
            }
            return 0;
        }
    }
}
