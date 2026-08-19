using System.Collections.Generic;
using UnityEngine;

namespace TableForge.Building
{
    [CreateAssetMenu(fileName = "BuildingStat", menuName = "Building_SO/Building Stats")]
    public class BuildingStats : ScriptableObject
    {
        public BuildingName buildingName;
        public BuildingType buildingType;

        // Dynamic levels: each entry = one level (prefab + cost to reach this level)
        // Level 1 = repair cost, Level 2 = upgrade to level 2, etc.
        public List<BuildingLevelData> levels = new();
    }
}
