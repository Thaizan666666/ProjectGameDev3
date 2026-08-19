using System.Collections.Generic;
using TableForge.Building;
using UnityEngine;
using System.Linq;

public class BuildingDatabase : MonoBehaviour
{
    [SerializeField] private BuildingStats[] buildingStatsAssets;

    private List<BuildingData> buildings = new();
    private bool isLoaded = false;
    private int lastAssetsHash = -1;

    public BuildingData GetByName(BuildingName buildingName) => buildings.Find(b => b.buildingName == buildingName);
    void Awake()
    {
        EnsureLoadBuilding();
    }

    public void EnsureLoadBuilding()
    {
        int currentHash = ComputeAssetsHash();

        if (isLoaded && currentHash == lastAssetsHash)
            return;

        isLoaded = true;
        lastAssetsHash = currentHash;

        //1. โหลดจาก Inspector-assigned assets
        if (buildingStatsAssets != null)
        {
            foreach (var stats in buildingStatsAssets)
            {
                if(stats == null) continue;
                buildings.Add(ConvertToBuildingData(stats));
            }
        }

        // 2. Fallback: โหลดจาก Resources
        if(buildings.Count == 0)
        {
            var loaded = Resources.LoadAll<BuildingStats>("Building_SO/Buildings");
            foreach (var stats in loaded)
            {
                buildings.Add(ConvertToBuildingData(stats));
            }
        }
    }

    private int ComputeAssetsHash()
    {
        if (buildingStatsAssets == null || buildingStatsAssets.Length == 0)
            return 0;

        unchecked
        {
            int hash = 17;
            foreach (var stats in buildingStatsAssets)
                hash = hash * 31 + (stats != null ? stats.GetInstanceID() : 0);
            return hash;
        }
    }

    private static BuildingData ConvertToBuildingData(BuildingStats stats) => new BuildingData
    {
        buildingName    = stats.buildingName,
        buildingType    = stats.buildingType,
        buildingLV_1    = stats.buildingLV_1,
        buildingLV_2    = stats.buildingLV_2,
        buildingLV_3    = stats.buildingLV_3
    };
}