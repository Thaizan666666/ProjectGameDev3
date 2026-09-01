using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Fishing zone in scene, holds its own fish entries
public class FishZone : MonoBehaviour, IRandomFishProvider
{
    [SerializeField] private FishDatabase fishDatabase;
    [SerializeField] private List<FishZoneEntry> entries = new();

    private IFishRepository Repository => fishDatabase;

    // Zone นี้เป็นแค่ trigger volume สำหรับตรวจจับว่าผู้เล่นยืนอยู่ในจุดตกปลาไหม
    // ถ้ามี Renderer ติดมาด้วย (เช่น sphere ที่ใส่ไว้ดูตำแหน่งตอนวางฉาก) ให้ซ่อนตอน Play
    // ไม่งั้นจะโผล่มาเป็นก้อนกลม ๆ ลอยนิ่ง ๆ ปนกับปลาจริงที่กำลังว่ายอยู่
    private void Awake()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
    }

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

    /// <summary>ปลาล่าสุดที่ถูกสุ่ม "โชว์" ไว้ (เช่นจากตู้ FishSlot) — ยังไม่ถูกใช้ตกจริง</summary>
    public FishData PendingFish { get; private set; }

    /// <summary>เรียกตอนสุ่มโชว์ปลา (เช่น FishSlot.TryRandomize) เพื่อจองไว้ว่าปลาตัวนี้คือตัวที่จะตกได้จริงตอนไปตกในโซนนี้</summary>
    public void SetPendingFish(FishData data) => PendingFish = data;

    /// <summary>ดึงปลาที่จองไว้มาใช้ (เรียกตอนเริ่ม encounter ตกจริง) แล้วเคลียร์ทิ้ง — คืน null ถ้าไม่มีปลาจองไว้ (ไม่ได้ไปโชว์ในตู้ก่อน)</summary>
    public FishData ConsumePendingFish()
    {
        FishData result = PendingFish;
        PendingFish = null;
        return result;
    }

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
