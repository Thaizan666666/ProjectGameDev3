// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// public class FishDatabase : MonoBehaviour, IFishRepository
// {
//     [SerializeField]
//     private List<FishData> fishes = new()
//     {
//         new FishData { fishName = FishName.Sardine, fishTier = FishTier.Common, minWeight = 5, maxWeight = 20, percentRate = 70 },
//         new FishData { fishName = FishName.Salmon, fishTier = FishTier.Common, minWeight = 15, maxWeight = 35, percentRate = 40 },
//         new FishData { fishName = FishName.WhiteSnapper, fishTier = FishTier.Common, minWeight = 10, maxWeight = 35, percentRate = 60 },
//         new FishData { fishName = FishName.Tuna, fishTier = FishTier.Rare, minWeight = 80, maxWeight = 200, percentRate = 10 },
//         new FishData { fishName = FishName.Swordfish, fishTier = FishTier.Rare, minWeight = 100, maxWeight = 300, percentRate = 5 },
//         new FishData { fishName = FishName.Dunkleosteus, fishTier = FishTier.Boss, minWeight = 500, maxWeight = 1200, percentRate = 1 }
//     };

//     public IReadOnlyList<FishData> GetAll() => fishes;

//     public IReadOnlyList<FishData> GetByTier(FishTier tier) =>
//         fishes.Where(f => f.fishTier == tier).ToList();

//     public FishData GetByName(FishName fishName) =>
//         fishes.FirstOrDefault(f => f.fishName == fishName);

//     // Random pick within a tier, ignores zone
//     public FishData GetRandomFish(FishTier tier)
//     {
//         FishData result = WeightedRandomPicker.Pick(GetByTier(tier), f => f.percentRate);

//         if (result == null)
//             Debug.LogWarning($"No fish for tier: {tier}");

//         return result;
//     }
// }
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TableForge.Fish;

public class FishDatabase : MonoBehaviour, IFishRepository
{
    [SerializeField] private FishStats[] fishStatsAssets; // Drag FishStats assets จาก Fishes folder มาใน Inspector

    private List<FishData> fishes = new();

    private void Awake()
    {
        fishes.Clear();

        // 1. โหลดจาก Inspector-assigned assets
        if (fishStatsAssets != null)
        {
            foreach (var stats in fishStatsAssets)
            {
                if (stats == null) continue;
                fishes.Add(ConvertToFishData(stats));
            }
        }

        // 2. Fallback: โหลดจาก Resources ถ้ายังไม่ได้ assign ใน Inspector
        if (fishes.Count == 0)
        {
            var loaded = Resources.LoadAll<FishStats>("Fish_SO/Fishes");
            foreach (var stats in loaded)
            {
                fishes.Add(ConvertToFishData(stats));
            }
        }
    }

    private FishData ConvertToFishData(FishStats stats) => new FishData
    {
        fishName   = stats.fishName,
        fishTier   = stats.fishTier,
        minWeight  = stats.minWeight,
        maxWeight  = stats.maxWeight,
        percentRate = stats.percentRate,
        Price      = stats.Price,
        Icon       = stats.Icon,
        Prefab     = stats.Prefab
    };

    public IReadOnlyList<FishData> GetAll() => fishes;

    public IReadOnlyList<FishData> GetByTier(FishTier tier) =>
        fishes.Where(f => f.fishTier == tier).ToList();

    public FishData GetByName(FishName fishName) =>
        fishes.FirstOrDefault(f => f.fishName == fishName);

    // Random pick within a tier, ignores zone
    public FishData GetRandomFish(FishTier tier)
    {
        FishData result = WeightedRandomPicker.Pick(GetByTier(tier), f => f.percentRate);
        if (result == null) Debug.LogWarning($"No fish for tier: {tier}");
        return result;
    }
}