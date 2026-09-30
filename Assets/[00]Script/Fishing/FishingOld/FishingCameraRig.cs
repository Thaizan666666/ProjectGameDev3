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
    [Tooltip("Transform ผู้เล่น — ต้องผูกไว้ให้กล้องทั้ง 3 ตัว Follow ด้วย ไม่งั้นกล้องจะหมุนตาม LookAt ได้อย่างเดียว แต่ตำแหน่งค้างอยู่ที่จุด static ตอน authoring ไม่ขยับตามผู้เล่นเลย (CinemachineFollow ต้องมี TrackingTarget ถึงจะทำงาน)")]
    [SerializeField] private Transform player;

    [Header("Priority")]
    [Tooltip("Priority ของกล้องที่กำลังใช้งานตอน fighting ปลา ต้อง 'สูงกว่า' priority กล้องหลักของเกมจริง ๆ (ไม่ใช่แค่เท่ากัน) ไม่งั้นกล้องตกปลาแพ้กล้องหลักตลอด ไม่มีวันได้ active จริง (ค่า default 20 นี้แพ้กล้องหลักที่ปกติตั้งไว้ 100 — เช็ค mainPlayerCamera.Priority ให้ดี)")]
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
        EnsureActivePriorityAboveMainCamera();
        AssignFollowTarget();
        SetActive(false);
    }

    /// <summary>ตั้ง Follow (Body/CinemachineFollow) ของกล้องทั้ง 3 เป็นผู้เล่น — ถ้าไม่ตั้งตรงนี้กล้องจะไม่ขยับตำแหน่งตามผู้เล่นเลย มีแต่หมุนตาม LookAt (fish) เท่านั้น</summary>
    private void AssignFollowTarget()
    {
        if (player == null)
        {
            Debug.LogWarning("[FishingCameraRig] ไม่ได้ผูก player ไว้ — กล้องจะไม่ Follow ผู้เล่นเลย (หมุนตามปลาได้ แต่ตำแหน่งค้างที่จุด static)");
            return;
        }

        if (leftShoulderCamera != null) leftShoulderCamera.Follow = player;
        if (rightShoulderCamera != null) rightShoulderCamera.Follow = player;
        if (overHeadCamera != null) overHeadCamera.Follow = player;
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

    /// <summary>กันพลาดคู่กับ EnsureIdlePriorityBelowMainCamera: ถ้า activePriority ตั้งไว้ต่ำกว่าหรือเท่ากับกล้องหลัก (เช่น กล้องหลักตั้งไว้ 100 แต่ activePriority default แค่ 20) กล้องตกปลาจะไม่มีวันชนะ priority แล้วไม่เคย active จริงเลย</summary>
    private void EnsureActivePriorityAboveMainCamera()
    {
        if (mainPlayerCamera == null) return;
        if (activePriority <= mainPlayerCamera.Priority)
        {
            int safeActive = mainPlayerCamera.Priority + 1;
            Debug.LogWarning($"[FishingCameraRig] activePriority ({activePriority}) <= กล้องหลัก ({mainPlayerCamera.Priority}) — ปรับขึ้นเป็น {safeActive} อัตโนมัติ ไม่งั้นกล้องตกปลาแพ้กล้องหลักตลอด สลับมาไม่ได้เลย");
            activePriority = safeActive;
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
