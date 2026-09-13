using UnityEngine;
using Unity.Cinemachine;
using Yarn.Unity;
using System.Collections;

/// <summary>
/// เชื่อม Yarn Spinner command เข้ากับ Cinemachine Virtual Camera (3D)
/// เพื่อดึงกล้องเข้าใกล้ "ไหล่ขวา" ของผู้เล่นทุกครั้งที่เริ่มบทสนทนา
/// แล้วคืนกล้องกลับสภาพเดิมเมื่อบทสนทนาจบ/พักช่วง
///
/// วิธีตั้งค่าใน Scene:
/// 1. สร้าง CinemachineVirtualCamera ชื่อ "VCam_Dialogue"
///    - Body: Framing Transposer หรือ 3rd Person Follow
///    - Follow: จะถูก set runtime ให้เป็น shoulderAnchor (ดูด้านล่าง)
///    - ปรับ Camera Distance ให้ใกล้ + Screen X ให้เยื้องขวานิดหน่อย
///      (เพื่อให้ได้ฟีล over-the-shoulder ทางขวา)
/// 2. สร้าง empty GameObject ชื่อ "ShoulderAnchor_R" เป็นลูกของโมเดลผู้เล่น
///    วางตำแหน่งไว้ที่หัวไหล่ขวา (Local Position ประมาณ x:+0.3, y:1.5, z:0)
/// 3. ตัวกล้องหลัก (default gameplay cam) ให้ Priority ต่ำกว่า VCam_Dialogue เสมอ
///    แล้วปล่อยให้ CinemachineBrain เป็นคน blend ให้อัตโนมัติ
/// 4. ลาก VCam_Dialogue, ShoulderAnchor_R มาใส่ใน Inspector ของสคริปต์นี้
/// </summary>
public class DialogueCameraController : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera dialogueVCam;
    [SerializeField] private int dialoguePriority = 20;
    [SerializeField] private int idlePriority = 0;

    [Header("Anchors")]
    [SerializeField] private Transform playerShoulderRight; // จุดไหล่ขวาของผู้เล่น
    [SerializeField] private float blendTime = 0.6f;         // ระยะเวลา blend เข้า/ออก

    [Header("NPC Lookup")]
    [Tooltip("ตั้งชื่อ GameObject ของ NPC ให้ตรงกับ id ที่ใช้เรียกใน Yarn เช่น \"Pu\", \"FishSeller\"")]
    [SerializeField] private bool findNpcByName = true;

    private CinemachineBrain brain;

    private void Awake()
    {
        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (dialogueVCam != null)
        {
            dialogueVCam.Priority = idlePriority;
        }
    }

    /// <summary>
    /// เรียกจาก Yarn: <<camera_shoulder_in "NpcId">>
    /// ดึงกล้องเข้าไหล่ขวาของผู้เล่น โดย Look At ไปทาง NPC ที่กำลังคุยด้วย
    /// </summary>
    [YarnCommand("camera_shoulder_in")]
    public void CameraShoulderIn(string npcId = "")
    {
        if (dialogueVCam == null || playerShoulderRight == null) return;

        Transform lookTarget = playerShoulderRight;
        if (findNpcByName && !string.IsNullOrEmpty(npcId))
        {
            GameObject npcObj = GameObject.Find(npcId);
            if (npcObj != null)
            {
                lookTarget = npcObj.transform;
            }
        }

        // Follow เกาะกับไหล่ขวาผู้เล่น (สร้างมุม over-the-shoulder)
        dialogueVCam.Follow = playerShoulderRight;
        // LookAt ไปทาง NPC คู่สนทนา ถ้าหาไม่เจอก็ยังมองไปทางไหล่ผู้เล่น
        dialogueVCam.LookAt = lookTarget;

        if (brain != null)
        {
            brain.DefaultBlend.Time = blendTime;
        }

        dialogueVCam.Priority = dialoguePriority;
    }

    /// <summary>
    /// เรียกจาก Yarn: <<camera_reset>>
    /// คืนกล้องกลับไปใช้กล้องหลักของเกมเพลย์ (blend ออกอัตโนมัติ)
    /// </summary>
    [YarnCommand("camera_reset")]
    public void CameraReset()
    {
        if (dialogueVCam == null) return;
        dialogueVCam.Priority = idlePriority;
    }

    /// <summary>
    /// ตัวอย่าง command เสริมสำหรับรอ action ของระบบเกม เช่น รอผู้เล่นหยิบไอเทม/ตกปลาได้
    /// ต้องมีระบบ event ฝั่งเกมเรียก TriggerItemReceived / TriggerCatch ให้ IEnumerator นี้ทำงานต่อ
    /// (ตัวอย่างโครงไว้ให้ปรับใช้ตามระบบเควสจริงของโปรเจกต์)
    /// </summary>
    [YarnCommand("wait_for_item")]
    public IEnumerator WaitForItem(string itemId)
    {
        bool received = false;
        void OnReceived(string id)
        {
            if (id == itemId) received = true;
        }

        // ตัวอย่าง: สมัคร event จากระบบ Inventory ของคุณเอง
        //InventoryEvents.OnItemReceived += OnReceived;

        while (!received)
        {
            yield return null;
        }

        //InventoryEvents.OnItemReceived -= OnReceived;
    }

    [YarnCommand("wait_for_catch")]
    public IEnumerator WaitForCatch(string catchType)
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
}
