using UnityEngine;

[System.Serializable]
public class RodLevelData
{
    public GameObject prefabRod;
    public int upgradeCostRod;
    
    // Per-level identity
    public RodName rodName;
    public RodTier rodTier;
}
