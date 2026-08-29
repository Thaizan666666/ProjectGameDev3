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

    [Header("Priority")]
    [SerializeField] private int activePriority = 20;
    [Tooltip("Priority ของทั้ง 3 กล้องตอนไม่ได้ fighting ปลา ต้อง 'ต่ำกว่า' priority กล้องหลักของเกมจริง ๆ (ไม่ใช่แค่เท่ากัน) ไม่งั้น Cinemachine เจอ tie แล้วจะค้างกล้องตกปลาไว้ ไม่สลับกลับกล้องหลัก")]
    [SerializeField] private int idlePriority = -100;
    [SerializeField] private int inactivePriority = 10;

    private FishController _fish;
    private bool _isActive;

    public void SetFish(FishController fish) => _fish = fish;

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
