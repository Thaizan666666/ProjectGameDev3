using UnityEngine;
using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// InMemoryVariableStorage ไม่ persist ข้าม scene — ถูก Destroy ไปกับ "Dialogue System" ทุกครั้งที่
/// Yarn สั่ง <<LoadScene>> ทำให้ flag เควสทั้งหมด (เช่น $PuSceneMeting, $CaughtBossFish) reset กลับ default
/// ตัวนี้ cache ค่าตัวแปรที่ track ไว้แบบ static (รอดข้าม scene reload ในรอบ Play เดียวกัน) แล้ว restore คืน
/// ให้ InMemoryVariableStorage ตัวใหม่ทุกครั้งที่ scene โหลดเสร็จ
/// Attach: GameObject เดียวกับ DialogueRunner/InMemoryVariableStorage ("Dialogue System")
/// </summary>
[RequireComponent(typeof(InMemoryVariableStorage))]
public class YarnVariablePersistence : MonoBehaviour
{
    // รายชื่อตัวแปรเควสที่ต้องรอดข้าม scene — เพิ่มชื่อใหม่ตรงนี้ทุกครั้งที่ declare ตัวแปรเควสเพิ่ม
    [SerializeField]
    private string[] trackedVariables =
    {
        "$PuSceneMeting", "$PuSceneFishing", "$met_grandfather", "$ShopkeeperMet",
        "$CaughtFiveFish", "$FishmongerMet", "$MetCaptainReady", "$CaughtBossFish", "$Scene3Done"
    };

    private static readonly Dictionary<string, bool> cache = new Dictionary<string, bool>();

    private InMemoryVariableStorage storage;

    private void Awake()
    {
        storage = GetComponent<InMemoryVariableStorage>();

        foreach (var name in trackedVariables)
        {
            if (cache.TryGetValue(name, out bool value))
            {
                storage.SetValue(name, value);
            }
        }
    }

    private void OnDisable()
    {
        // เก็บค่าปัจจุบันไว้ก่อนโดน Destroy ตอน scene เปลี่ยน (LoadScene unload ตัวเดิมเสมอ)
        foreach (var name in trackedVariables)
        {
            if (storage.TryGetValue(name, out bool value))
            {
                cache[name] = value;
            }
        }
    }
}
