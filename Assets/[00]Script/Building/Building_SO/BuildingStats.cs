using UnityEngine;

namespace TableForge.Building
{
    [CreateAssetMenu(fileName = "BuildingStat", menuName = "Building_SO/Building Stats")]
    public class BuildingStats : ScriptableObject
    {
        public BuildingName buildingName;
        public BuildingType buildingType;
        public int costToRepaired;
        public int costToUpgrade_2;
        public int costToUpgrade_3;

        public GameObject buildingLV_1;
        public GameObject buildingLV_2;
        public GameObject buildingLV_3;

    }
    
}
