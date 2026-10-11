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
    /// <summary>
    /// Yarn: <<turn_object_to_face "ObjectName" "TargetName">>
    /// หมุน ObjectName (แนวนอนเท่านั้น) ให้หันหน้าไปทาง TargetName ทันที (ไม่ smooth — ใช้ตั้งทิศก่อน cutscene/dialogue)
    /// </summary>
    [YarnCommand("turn_object_to_face")]
    public static IEnumerator TurnObjectToFace(string objectName, string targetName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
        {
            Debug.LogWarning($"turn_object_to_face: no GameObject named '{objectName}' found.");
            yield break;
        }

        // Player มี PlayerController ที่คอยหันหน้าเข้า NPC ซ้ำทุกเฟรมอยู่แล้วตอน dialogue (ดู Update's "arrived" branch)
        // ถ้าสั่งหมุนตรงนี้เองจะโดน PlayerController เขียนทับคืนทันทีเฟรมถัดไป ต้องผ่าน TurnToPoint ที่มี _suppressFacing กันไว้แล้วแทน
        PlayerController playerController = obj.GetComponent<PlayerController>();
        if (playerController != null)
        {
            yield return playerController.StartCoroutine(playerController.TurnToPoint(targetName));
            yield break;
        }

        GameObject target = GameObject.Find(targetName);
        if (target == null)
        {
            Debug.LogWarning($"turn_object_to_face: no GameObject named '{targetName}' found.");
            yield break;
        }

        Vector3 toTarget = target.transform.position - obj.transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) yield break;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget);

        // KinematicCharacterMotor sync transform.rotation ทับจาก TransientRotation ทุก physics tick
        // ตั้ง transform.rotation ตรง ๆ จะโดนเขียนทับคืนทันที ต้องสั่งผ่าน Motor.SetRotation() แทน
        var motor = obj.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
        if (motor != null)
            motor.SetRotation(targetRotation);
        else
            obj.transform.rotation = targetRotation;
    }

    /// <summary>
    /// Yarn: <<force_next_catch "CommonFish" 1>>
    /// บังคับให้การตกปลา N รอบถัดไปได้ชนิดที่กำหนดเสมอ (ไม่สุ่ม) — ใช้สำหรับจุด story ที่ต้องได้ปลาตัวที่กำหนดแน่ ๆ
    /// </summary>
    [YarnCommand("force_next_catch")]
    public static void ForceNextCatch(string fishName, int count = 1)
    {
        var spawnController = FishingNewSystem.FishSpawnController.instance;
        if (spawnController == null)
        {
            Debug.LogWarning("force_next_catch: no FishSpawnController in scene.");
            return;
        }

        if (!System.Enum.TryParse(fishName, true, out FishName targetFish))
        {
            Debug.LogWarning($"force_next_catch: '{fishName}' is not a valid FishName.");
            return;
        }

        FishDatabase db = Object.FindFirstObjectByType<FishDatabase>();
        FishData data = db != null ? db.GetByName(targetFish) : null;
        if (data == null)
        {
            Debug.LogWarning($"force_next_catch: '{targetFish}' not found in FishDatabase.");
            return;
        }

        spawnController.ForceNextCatches(data, count);
    }

    /// <summary>
    /// Yarn: <<allow_fishing>> / <<block_fishing>>
    /// ปกติตกปลาไม่ได้ตอน dialogue เปิดอยู่ — ใช้คู่นี้ครอบช่วงที่ต้องการให้ตกปลาได้ระหว่างคุย (เช่น ฉากสอนตกปลา)
    /// </summary>
    [YarnCommand("allow_fishing")]
    public static void AllowFishing()
    {
        var fcc = Object.FindFirstObjectByType<FishingNewSystem.FishingCastController>();
        if (fcc == null) { Debug.LogWarning("allow_fishing: no FishingCastController found."); return; }
        fcc.SetFishingAllowedDuringDialogue(true);
    }

    [YarnCommand("block_fishing")]
    public static void BlockFishing()
    {
        var fcc = Object.FindFirstObjectByType<FishingNewSystem.FishingCastController>();
        if (fcc == null) { Debug.LogWarning("block_fishing: no FishingCastController found."); return; }
        fcc.SetFishingAllowedDuringDialogue(false);
    }

    /// <summary>
    /// Yarn: <<wait_for_catch_count 5>>
    /// รอให้ผู้เล่นจับปลาได้ครบ N ครั้ง (ชนิดไหนก็ได้) ถึงไป dialogue บรรทัดต่อไป
    /// </summary>
    [YarnCommand("wait_for_catch_count")]
    public static IEnumerator WaitForCatchCount(int count)
    {
        var castController = Object.FindFirstObjectByType<FishingNewSystem.FishingCastController>();
        if (castController == null)
        {
            Debug.LogWarning("wait_for_catch_count: no FishingCastController found in scene.");
            yield break;
        }

        int caughtCount = 0;
        void OnCaught(FishData data) => caughtCount++;

        castController.OnFishObtained += OnCaught;
        while (caughtCount < count) yield return null;
        castController.OnFishObtained -= OnCaught;
    }

    /// <summary>
    /// Yarn: <<set_boss_only true>> / <<set_boss_only false>>
    /// เปิด/ปิด Boss-only mode ของ FishSpawnController — ตกปลาได้เฉพาะ Boss tier เท่านั้นตอนเปิด
    /// </summary>
    [YarnCommand("set_boss_only")]
    public static void SetBossOnly(bool enabled)
    {
        var spawnController = FishingNewSystem.FishSpawnController.instance;
        if (spawnController == null)
        {
            Debug.LogWarning("set_boss_only: no FishSpawnController in scene.");
            return;
        }

        spawnController.SetBossOnlyMode(enabled);
    }

    /// <summary>
    /// Yarn: <<start_boss_catch_watch "Dunkleosteus">>
    /// ไม่ block dialogue — แค่เริ่มดักฟังเบื้องหลัง พอจับปลาตัวที่กำหนดได้เมื่อไหร่ (จะอยู่นอก dialogue ก็ได้ เช่น
    /// ขับเรือไปตกปลาเอง) จะ set ตัวแปร $CaughtBossFish = true และปิด Boss-only mode ให้อัตโนมัติ
    /// </summary>
    [YarnCommand("start_boss_catch_watch")]
    public static void StartBossCatchWatch(string fishName)
    {
        var castController = Object.FindFirstObjectByType<FishingNewSystem.FishingCastController>();
        var dialogueRunner = Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>();
        if (castController == null || dialogueRunner == null)
        {
            Debug.LogWarning("start_boss_catch_watch: missing FishingCastController or DialogueRunner.");
            return;
        }

        if (!System.Enum.TryParse(fishName, true, out FishName targetFish))
        {
            Debug.LogWarning($"start_boss_catch_watch: '{fishName}' is not a valid FishName.");
            return;
        }

        void OnCaught(FishData data)
        {
            if (data == null || data.fishName != targetFish) return;

            dialogueRunner.VariableStorage.SetValue("$CaughtBossFish", true);

            var spawnController = FishingNewSystem.FishSpawnController.instance;
            if (spawnController != null) spawnController.SetBossOnlyMode(false);

            castController.OnFishObtained -= OnCaught;
        }

        castController.OnFishObtained += OnCaught;
    }

    [YarnCommand("AddItem")]
    public static void AddItem()
    {
        // Debug.LogWarning("Add item");
        if(Inventory.instance != null)
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
        if (!System.Enum.TryParse(catchType, true, out FishName targetFish))
        {
            Debug.LogWarning($"wait_for_catch: '{catchType}' is not a valid FishName.");
            yield break;
        }

        bool caught = false;
        void OnCaught(FishData data)
        {
            if (data != null && data.fishName == targetFish) caught = true;
        }

        // ระบบใหม่ (FishingNewSystem) ก่อน — ถ้าไม่มีค่อย fallback ไประบบเก่า (FishingGameManager)
        var castController = Object.FindFirstObjectByType<FishingNewSystem.FishingCastController>();
        if (castController != null)
        {
            castController.OnFishObtained += OnCaught;
            while (!caught) yield return null;
            castController.OnFishObtained -= OnCaught;
            yield break;
        }

        FishingGameManager manager = Object.FindFirstObjectByType<FishingGameManager>();
        if (manager == null)
        {
            Debug.LogWarning("wait_for_catch: no FishingCastController or FishingGameManager found in scene.");
            yield break;
        }

        manager.OnFishCaught += OnCaught;
        while (!caught) yield return null;
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
        FadeBlackScreen.Instance.FadeInThenLoadScene(SceneName);
        yield return null;
    }
}
