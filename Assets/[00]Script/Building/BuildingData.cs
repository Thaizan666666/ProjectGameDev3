using UnityEngine;
using System;

[Serializable]
public class BuildingData
{
    public BuildingName buildingName;
    public BuildingType buildingType;
    public int costToRepaired;
    public int costToUpgrade_2;
    public int costToUpgrade_3;

    public GameObject buildingLV_1;
    public GameObject buildingLV_2;
    public GameObject buildingLV_3;

    public int GetUpgradeCost(int targetLevel) => targetLevel switch
    {
        1 => costToRepaired,
        2 => costToUpgrade_2,
        3 => costToUpgrade_3,
        _ => -1
    };

    public GameObject GetPrefab(int level) => level switch
    {
        1 => buildingLV_1,
        2 => buildingLV_2,
        3 => buildingLV_3,
        _ => null
    };

    // Highest level this building actually has a prefab for
    public int MaxLevel => buildingLV_3 != null ? 3 : buildingLV_2 != null ? 2 : 1;
}
