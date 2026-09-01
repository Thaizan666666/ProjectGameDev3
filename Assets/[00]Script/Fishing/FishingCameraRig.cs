// ─────────────────────────────────────────────────────────────
// FishingCameraRig.cs
// สลับ Priority ของ 3 CinemachineCamera (Left/Right/OverHead) ตาม
// FishController.RelativeDir แล้วปล่อยให้ CinemachineBrain blend เอง
// Attach: GameObject ว่าง ๆ ในฉาก (เช่น "FishingCameraRig")
// ต้องลาก 3 CinemachineCamera เข้า field ใน Inspector (ดู IMPLEMENTATION_SPEC.md)
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using Unity.Cinemachine;

public class FishingCameraRig : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera leftShoulderCamera;
    [SerializeField] private CinemachineCamera rightShoulderCamera;
    [SerializeField] private CinemachineCamera overHeadCamera;
    [Tooltip("กล้องหลักของ Player ตอนไม่ได้ตกปลา — ใส่ไว้เพื่อกันพลาด: ตอน Awake จะบังคับ idlePriority ให้ต่ำกว่ากล้องนี้เสมอ ไม่ว่าจะตั้งค่าใน Inspector ผิดมายังไงก็ตาม (ไม่ใส่ก็ได้ ถ้ามั่นใจว่าตั้ง idlePriority ต่ำพอแล้ว)")]
    [SerializeField] private CinemachineCamera mainPlayerCamera;

    [Header("Priority")]
    [SerializeField] private int activePriority = 20;
    [Tooltip("Priority ของทั้ง 3 กล้องตอนไม่ได้ fighting ปลา ต้อง 'ต่ำกว่า' priority กล้องหลักของเกมจริง ๆ (ไม่ใช่แค่เท่ากัน) ไม่งั้น Cinemachine เจอ tie แล้วจะค้างกล้องตกปลาไว้ ไม่สลับกลับกล้องหลัก")]
    [SerializeField] private int idlePriority = -100;
    [SerializeField] private int inactivePriority = 10;

    private FishController _fish;
    private bool _isActive;

    // บังคับกล้องตกปลาทั้ง 3 ตัวลง idle ตั้งแต่เปิดเกม กันไม่ให้ priority ที่ค้างอยู่ใน Inspector (เช่น จากตอนทดสอบ)
    // สูงกว่ากล้องหลักของ player แล้วโดน active ตั้งแต่ต้นเกมทั้งที่ยังไม่ได้เริ่ม encounter
    private void Awake()
    {
        EnsureIdlePriorityBelowMainCamera();
        SetActive(false);
    }

    /// <summary>ป้องกันอีกขั้น: ถ้า idlePriority ตั้งไว้ใน Inspector สูงกว่าหรือเท่ากับกล้องหลักของ player โดยไม่ตั้งใจ จะปรับลงให้ต่ำกว่าเสมออัตโนมัติ</summary>
    private void EnsureIdlePriorityBelowMainCamera()
    {
        if (mainPlayerCamera == null) return;
        if (idlePriority >= mainPlayerCamera.Priority)
        {
            int safeIdle = mainPlayerCamera.Priority - 1;
            Debug.LogWarning($"[FishingCameraRig] idlePriority ({idlePriority}) >= กล้องหลัก ({mainPlayerCamera.Priority}) — ปรับลงเป็น {safeIdle} อัตโนมัติกันกล้องตกปลาแย่ง priority กล้องหลักตอนไม่ได้ตกปลา");
            idlePriority = safeIdle;
        }
    }

    public void SetFish(FishController fish)
    {
        _fish = fish;
        Transform lookAtTarget = fish != null ? fish.transform : null;
        if (leftShoulderCamera != null) leftShoulderCamera.LookAt = lookAtTarget;
        if (rightShoulderCamera != null) rightShoulderCamera.LookAt = lookAtTarget;
        if (overHeadCamera != null) overHeadCamera.LookAt = lookAtTarget;
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        if (!active)
        {
            SetPriorities(idlePriority, idlePriority, idlePriority);
        }
    }

    private void Update()
    {
        if (!_isActive || _fish == null) return;

        // ปลาเหนื่อยแล้ว (Tired) ไม่มีกลไกสวนทางให้เล่นแล้ว มีแต่กดดึงเข้าอย่างเดียว —
        // ไม่ต้องสลับกล้องตาม RelativeDir อีก (ปลายังว่ายวนอยู่ ถ้าสลับตามจะกลายเป็นกล้องส่ายไปมาไม่หยุดตอนกำลังดึง)
        // ค้างกล้องตัวล่าสุดที่ active อยู่ไว้แทน
        if (_fish.IsTired) return;

        switch (_fish.RelativeDir)
        {
            case RelativeDirection.Left:
                SetPriorities(activePriority, inactivePriority, inactivePriority);
                break;
            case RelativeDirection.Right:
                SetPriorities(inactivePriority, activePriority, inactivePriority);
                break;
            case RelativeDirection.Forward:
                SetPriorities(inactivePriority, inactivePriority, activePriority);
                break;
        }
    }

    private void SetPriorities(int left, int right, int overHead)
    {
        if (leftShoulderCamera != null) leftShoulderCamera.Priority = left;
        if (rightShoulderCamera != null) rightShoulderCamera.Priority = right;
        if (overHeadCamera != null) overHeadCamera.Priority = overHead;
    }
}
