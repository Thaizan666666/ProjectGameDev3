// ─────────────────────────────────────────────────────────────
// ThirdPersonMovement.cs
// Camera-Relative Movement: W = ทิศกล้อง, A/D = ซ้าย/ขวากล้อง
// ตัวละคร auto-rotate หันหน้าตาม direction ที่เดิน (Elden Ring style)
// Attach: Player GameObject
// ─────────────────────────────────────────────────────────────
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float sprintSpeed = 8.5f;
    [SerializeField] private float accel = 10f;    // ความเร็วในการเร่ง/ชะลอ

    [Header("Character Rotation")]
    [Tooltip("เวลา smooth หมุน (วินาที) — น้อย = หมุนเร็ว")]
    [SerializeField] private float rotSmooth = 0.08f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpForce = 5.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float fallMult = 2f;   // ตอนตก gravity หนักขึ้น (snappy)

    [Header("Ground Check")]
    [SerializeField] private Transform groundPt;      // Empty child ที่เท้า Player
    [SerializeField] private float groundR = 0.25f;
    [SerializeField] private LayerMask groundMask;

    // ── Private ──────────────────────────────────────────────
    private CharacterController _cc;
    private PlayerInputHandler _input;
    private Transform _cam;   // Camera.main.transform

    private float _speed, _rotVel, _yVel, _targetAngle;
    private bool _grounded;
    private const float TERMINAL = -53f, THRESHOLD = 0.01f;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputHandler>();
        _cam = Camera.main.transform;
    }

    private void Update() { GroundCheck(); GravityJump(); Move(); }

    void GroundCheck() => _grounded = Physics.CheckSphere(
        groundPt.position, groundR, groundMask, QueryTriggerInteraction.Ignore);

    void GravityJump()
    {
        if (_grounded)
        {
            if (_yVel < 0) _yVel = -2f;               // pin to ground
            if (_input.JumpPressed) _yVel = jumpForce; // jump!
            return;
        }
        // กลางอากาศ: ตอนตก gravity หนักขึ้น → รู้สึก snappy แบบ Elden Ring
        float m = _yVel < 0 ? fallMult : 1f;
        _yVel += gravity * m * Time.deltaTime;
        _yVel = Mathf.Max(_yVel, TERMINAL);
    }

    void Move()
    {
        var inp = _input.MoveInput;
        bool hasInp = inp.sqrMagnitude > THRESHOLD;

        // Target speed: 0 ถ้าไม่กด, run/sprint ถ้ากด
        float tgtSpd = hasInp ? (_input.SprintHeld ? sprintSpeed : runSpeed) : 0f;
        _speed = Mathf.MoveTowards(_speed, tgtSpd, accel * Time.deltaTime);

        var moveDir = Vector3.zero;
        if (hasInp)
        {
            // ══ Camera-Relative Direction ═══════════════════════════
            //  Project camera axes ลง XZ plane (ตัด Y ทิ้ง)
            var fwd = _cam.forward; fwd.y = 0; fwd.Normalize();
            var rgt = _cam.right; rgt.y = 0; rgt.Normalize();

            //  W (inp.y = +1) → camera forward    S (inp.y = -1) → camera back
            //  D (inp.x = +1) → camera right      A (inp.x = -1) → camera left
            moveDir = (fwd * inp.y + rgt * inp.x).normalized;
            // ═════════════════════════════════════════════════════════

            // หมุนตัวละครให้หันหน้าตาม moveDir (smooth)
            _targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float ang = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, _targetAngle, ref _rotVel, rotSmooth);
            transform.rotation = Quaternion.Euler(0, ang, 0);
        }

        // Apply velocity: horizontal + vertical (gravity/jump)
        _cc.Move((moveDir * _speed + Vector3.up * _yVel) * Time.deltaTime);
    }

    // Debug: แสดง GroundCheck sphere ใน Scene View
    void OnDrawGizmosSelected()
    {
        if (!groundPt) return;
        Gizmos.color = _grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundPt.position, groundR);
    }
}