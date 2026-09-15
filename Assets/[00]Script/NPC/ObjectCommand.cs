// ─────────────────────────────────────────────────────────────
// ObjectActiveCommand.cs
// Yarn command: activate/deactivate any GameObject by name.
// Static, so no target GameObject argument is needed.
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using Yarn.Unity;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;

public static class ObjectCommand
{
    /// <summary>
    /// Yarn: <<set_object_active "ObjectName" true>> / <<set_object_active "ObjectName" false>>
    /// </summary>
    [YarnCommand("set_object_active")]
    public static void SetObjectActive(string objectName, bool active)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
        {
            Debug.LogWarning($"set_object_active: no GameObject named '{objectName}' found.");
            return;
        }
        obj.SetActive(active);
    }
    [YarnCommand("AddItem")]
    public static void AddItem()
    {
        // Debug.LogWarning("Add item");
        Inventory.instance.AddItemToObjectCommand();
    }
    /// <summary>
    /// ตัวอย่าง command เสริมสำหรับรอ action ของระบบเกม เช่น รอผู้เล่นหยิบไอเทม/ตกปลาได้
    /// ต้องมีระบบ event ฝั่งเกมเรียก TriggerItemReceived / TriggerCatch ให้ IEnumerator นี้ทำงานต่อ
    /// (ตัวอย่างโครงไว้ให้ปรับใช้ตามระบบเควสจริงของโปรเจกต์)
    /// </summary>
    [YarnCommand("wait_for_item")]
    public static IEnumerator WaitForItem()
    {
        yield return null;
    }

    /// <summary>
    /// Yarn: <<wait_for_catch "Sardine">>
    /// รอให้ผู้เล่นกด F ตกปลาจริง ๆ จนจับได้ตัวที่ชื่อ catchType เป๊ะ ๆ ถึงไป dialogue บรรทัดต่อไป
    /// catchType ต้องตรงกับ FishName enum และต้องเป็นปลาที่ตั้งค่าไว้ใน FishZone ของฉากจริง
    /// (หา FishZone ในซีนอัตโนมัติ — ตอนนี้มีแค่ตัวเดียว ถ้ามีหลายโซนในอนาคตต้องเปลี่ยนไปหาโซนที่ผู้เล่นยืนอยู่แทน)
    /// </summary>
    [YarnCommand("wait_for_catch")]
    public static IEnumerator WaitForCatch(string catchType)
    {
        FishingGameManager manager = Object.FindFirstObjectByType<FishingGameManager>();
        if (manager == null)
        {
            Debug.LogWarning("wait_for_catch: no FishingGameManager found in scene.");
            yield break;
        }

        FishZone zone = Object.FindFirstObjectByType<FishZone>();
        if (zone == null)
        {
            Debug.LogWarning("wait_for_catch: no FishZone found in scene.");
            yield break;
        }

        if (!System.Enum.TryParse(catchType, true, out FishName targetFish))
        {
            Debug.LogWarning($"wait_for_catch: '{catchType}' is not a valid FishName.");
            yield break;
        }

        if (!zone.Entries.Any(e => e.fishName == targetFish))
        {
            Debug.LogWarning($"wait_for_catch: '{targetFish}' is not configured in FishZone '{zone.ZoneName}' — check its Entries list.");
            yield break;
        }

        bool caught = false;
        void OnCaught(FishData data)
        {
            if (data != null && data.fishName == targetFish) caught = true;
        }

        manager.OnFishCaught += OnCaught;
        while (!caught)
        {
            yield return null;
        }
        manager.OnFishCaught -= OnCaught;
    }

    public static IEnumerable WaitForSells()
    {
        Debug.Log("------Selling---------");
        yield return null;
    }

    [YarnCommand("LoadScene")]
    public static IEnumerator RoadScenewithname(string SceneName)
    {

        yield return null;
    }
}
