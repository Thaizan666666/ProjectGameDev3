using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TableForge.Fish;

public class FishDatabase : MonoBehaviour, IFishRepository
{
    [SerializeField] private FishStats[] fishStatsAssets;

    private List<FishData> fishes = new();
    private bool isLoaded = false;
    private int lastAssetsHash = -1;

    private void Awake() => EnsureLoaded();

    public void EnsureLoaded()
    {
        int currentHash = ComputeAssetsHash();

        // ★ ถ้า array เปลี่ยน (เพิ่ม/ลบ/สลับ asset) → บังคับ reload แม้ isLoaded จะเป็น true
        if (isLoaded && currentHash == lastAssetsHash)
            return;

        isLoaded = true;
        lastAssetsHash = currentHash;
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

        // 2. Fallback: โหลดจาก Resources
        if (fishes.Count == 0)
        {
            var loaded = Resources.LoadAll<FishStats>("Fish_SO/Fishes");
            foreach (var stats in loaded)
            {
                fishes.Add(ConvertToFishData(stats));
            }
        }
    }

    /// <summary>คำนวณ hash จาก instanceID ของทุก asset ใน array — เปลี่ยนเมื่อเพิ่ม/ลบ/เปลี่ยน asset</summary>
    private int ComputeAssetsHash()
    {
        if (fishStatsAssets == null || fishStatsAssets.Length == 0)
            return 0;

        unchecked
        {
            int hash = 17;
            foreach (var stats in fishStatsAssets)
                hash = hash * 31 + (stats != null ? stats.GetInstanceID() : 0);
            return hash;
        }
    }

    private static FishData ConvertToFishData(FishStats stats) => new FishData
    {
        fishName    = stats.fishName,
        fishTier    = stats.fishTier,
        minWeight   = stats.minWeight,
        maxWeight   = stats.maxWeight,
        percentRate = stats.percentRate,
        Price       = stats.Price,
        Icon        = stats.Icon,
        Prefab      = stats.Prefab
    };

    public IReadOnlyList<FishData> GetAll()
    {
        EnsureLoaded();
        return fishes;
    }

    public IReadOnlyList<FishData> GetByTier(FishTier tier)
    {
        EnsureLoaded();
        return fishes.Where(f => f.fishTier == tier).ToList();
    }

    public FishData GetByName(FishName fishName)
    {
        EnsureLoaded();
        return fishes.FirstOrDefault(f => f.fishName == fishName);
    }

    public FishData GetRandomFish(FishTier tier)
    {
        EnsureLoaded();
        FishData result = WeightedRandomPicker.Pick(GetByTier(tier), f => f.percentRate);
        if (result == null) Debug.LogWarning($"No fish for tier: {tier}");
        return result;
    }
}