// ─────────────────────────────────────────────────────────────
// ObjectActiveCommand.cs
// Yarn command: activate/deactivate any GameObject by name.
// Static, so no target GameObject argument is needed.
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using Yarn.Unity;
using System.Collections;

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
        Debug.LogWarning("Add item");
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

    [YarnCommand("wait_for_catch")]
    public static IEnumerator WaitForCatch(string catchType)
    {
        bool caught = false;
        void OnCaught(string type)
        {
            if (type == catchType) caught = true;
        }

        //FishingEvents.OnCaught += OnCaught;

        while (!caught)
        {
            yield return null;
        }

        //FishingEvents.OnCaught -= OnCaught;
    }

    public static IEnumerable WaitForSells()
    {
        Debug.Log("------Selling---------");
        yield return null;
    }
}
