using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RodData
{
    public RodName rodName;
    public RodTier rodTier;

    public List<RodLevelData> levelRod = new();

    public int GetUpgradeCost(int targetLevel)
    {
        if(targetLevel <= 0 || targetLevel > levelRod.Count) return -1;
        return levelRod[targetLevel - 1].upgradeCostRod;
    }

    public GameObject GetPrefab(int level)
    {
        if (level <= 0 || level > levelRod.Count) return null;
        return levelRod[level - 1].prefabRod;
    }

    public int MaxLevel
    {
        get
        {
            for (int i = levelRod.Count - 1; i >= 0; i--)
            {
                if (levelRod[i].prefabRod != null) return i + 1;
            }
            return 0;
        }
    }
}
