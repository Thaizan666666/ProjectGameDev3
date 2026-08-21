using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Fishing zone in scene, holds its own fish entries
public class FishZone : MonoBehaviour, IRandomFishProvider
{
    [SerializeField] private FishDatabase fishDatabase;
    [SerializeField] private List<FishZoneEntry> entries = new();

    private IFishRepository Repository => fishDatabase;

    public string ZoneName => name;
    public FishDatabase Database => fishDatabase;
    public IReadOnlyList<FishZoneEntry> Entries => entries;

    public void AddEntry(FishTier tier, FishName fishName) =>
        entries.Add(new FishZoneEntry { tier = tier, fishName = fishName });

    public void RemoveEntry(int index)
    {
        if (IsValidIndex(index)) entries.RemoveAt(index);
    }

    public void SetEntryTier(int index, FishTier tier)
    {
        if (IsValidIndex(index)) entries[index].tier = tier;
    }

    public void SetEntryFish(int index, FishName fishName)
    {
        if (IsValidIndex(index)) entries[index].fishName = fishName;
    }

    public FishData GetRandomFish(FishTier tier) => PickFrom(GetCandidates(tier));

    public FishData GetRandomFish() => PickFrom(GetCandidates(null));

    private bool IsValidIndex(int index) => index >= 0 && index < entries.Count;

    private FishData PickFrom(List<FishData> candidates)
    {
        FishData result = WeightedRandomPicker.Pick(candidates, f => f.percentRate);

        if (result == null)
            Debug.LogWarning($"{name}: no fish available");

        return result;
    }

    private List<FishData> GetCandidates(FishTier? tier)
    {
        if (Repository == null)
        {
            Debug.LogWarning($"{name}: missing FishDatabase");
            return new List<FishData>();
        }

        HashSet<FishName> names = new HashSet<FishName>(
            entries.Where(e => tier == null || e.tier == tier.Value).Select(e => e.fishName));

        return Repository.GetAll().Where(f => names.Contains(f.fishName)).ToList();
    }
}
