// ─────────────────────────────────────────────────────────────
// ThirdPersonCamera.cs
// หมุน CameraFollowTarget ตาม Mouse Input
// Cinemachine VCam Follow target นี้ → ได้ Orbit effect รอบ Player
// Attach: Player GameObject
// ─────────────────────────────────────────────────────────────
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Empty child ของ Player ที่ Cinemachine VCam จะ Follow")]
    [SerializeField] private Transform cameraFollowTarget;

    //[Header("Camera Pivot Height")]
    //[Tooltip("ความสูงเหนือ Player ที่กล้อง orbit รอบ (ปกติ = 1.0)")]
    //[SerializeField] private float pivotHeight = 1.0f;

    [Header("Mouse Sensitivity")]
    [SerializeField, Range(0.01f, 1f)] private float sensitivityX = 0.22f;
    [SerializeField, Range(0.01f, 1f)] private float sensitivityY = 0.22f;

    [Header("Vertical Clamp (degrees)")]
    [SerializeField] private float minPitch = -25f;   // มองลงได้สูงสุด
    [SerializeField] private float maxPitch = 65f;   // มองขึ้นได้สูงสุด

    [Header("Camera Smoothing")]
    [SerializeField] private bool smooth = true;
    [SerializeField] private float smoothSpeed = 22f;

    // ── State ─────────────────────────────────────────────────
    private PlayerInputHandler _input;
    private float _targetYaw, _targetPitch;
    private float _yaw, _pitch;
    private const float DEAD = 0.01f;

    private void Awake() => _input = GetComponent<PlayerInputHandler>();

    private void Start()
    {
        // Init จาก rotation ปัจจุบัน → ป้องกัน Camera กระโดดตอน Play Mode
        var angles = cameraFollowTarget.eulerAngles;
        _yaw = _targetYaw = angles.y;
        _pitch = _targetPitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // LateUpdate: รันหลัง Movement Script เสมอ
    private void LateUpdate()
    {
        //cameraFollowTarget.position = transform.position + Vector3.up * pivotHeight;

        var look = _input.LookInput;

        if (look.sqrMagnitude > DEAD)
        {
            // Mouse/Delta คืนค่า pixels/frame → ไม่ต้องคูณ Time.deltaTime
            _targetYaw += look.x * sensitivityX;
            _targetPitch -= look.y * sensitivityY;  // Invert Y: เมาส์ขึ้น = มองขึ้น
        }

        // จำกัดมุม Vertical
        _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);

        // Smooth interpolation
        if (smooth)
        {
            _yaw = Mathf.LerpAngle(_yaw, _targetYaw, Time.deltaTime * smoothSpeed);
            _pitch = Mathf.LerpAngle(_pitch, _targetPitch, Time.deltaTime * smoothSpeed);
        }
        else { _yaw = _targetYaw; _pitch = _targetPitch; }

        // ══ KEY CONCEPT ════════════════════════════════════════════
        //  หมุน World Rotation ของ CameraFollowTarget
        //  Cinemachine Transposer (LockToTargetNoRoll) คำนวณ offset
        //  ใน local space ของ target → เมื่อ target หมุน กล้องจะ orbit
        //  แม้ Player จะหมุน (เป็น child) เพราะเราตั้ง World Rotation
        // ═══════════════════════════════════════════════════════════
        cameraFollowTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}