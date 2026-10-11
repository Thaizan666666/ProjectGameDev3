using UnityEngine;
using System.Collections.Generic;

namespace TableForge.Equipment.Fishing
{
    [CreateAssetMenu(fileName = "RodStat", menuName = "Scriptable Objects/Equipments/Fishing/Rod Stats")]

    public class RodStats : ScriptableObject
    {
        public List<RodLevelData> levelRod = new();
    }
}