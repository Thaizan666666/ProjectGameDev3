using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BoatControll : MonoBehaviour
{
    [Header("แรงเคลื่อนที่ (AddForce)")]
    [Tooltip("แรงขับเคลื่อนไปข้างหน้า/ถอยหลัง ตาม Move.y (W/S)")]
    public float moveForce = 20f;
    [Tooltip("แรงบิดเลี้ยวซ้าย/ขวา ตาม Move.x (A/D)")]
    public float turnTorque = 10f;

    private PlayerInputActions _actions;
    private Rigidbody _rb;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _actions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _actions.BoatPlayer.Move.performed += OnMove;
        _actions.BoatPlayer.Move.canceled += OnMove;
        // หมายเหตุ: ไม่ Enable() action map ตรงนี้ตั้งแต่แรกแล้ว — ต้องมีคนขึ้นเรือก่อน (ดู BoatBoardZone)
        // ถึงจะเปิดผ่าน SetControlEnabled(true) ไม่งั้น WASD จะไปโดนทั้งเรือและตัวละครพร้อมกันตลอดเวลา
    }

    private void OnDisable()
    {
        _actions.BoatPlayer.Move.performed -= OnMove;
        _actions.BoatPlayer.Move.canceled -= OnMove;
        _actions.BoatPlayer.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();

    /// <summary>
    /// เปิด/ปิดการรับ input ควบคุมเรือ (เรียกจาก BoatBoardZone ตอนขึ้น/ลงเรือ)
    /// </summary>
    public void SetControlEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            _actions.BoatPlayer.Enable();
        }
        else
        {
            _moveInput = Vector2.zero;
            _actions.BoatPlayer.Disable();
        }
    }

    private void FixedUpdate()
    {
        // project หัวเรือลงระนาบราบก่อน กันแรงขับพุ่งเอียงขึ้น/ลงตามมุมปิทช์/โคลงของเรือ (คลื่นโยกเรือ)
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        // y (W/S) = แรงขับไปข้างหน้าตามหัวเรือ (แนวราบเท่านั้น), x (A/D) = แรงบิดเลี้ยวรอบแกน Y
        _rb.AddForce(flatForward * _moveInput.y * moveForce, ForceMode.Force);
        _rb.AddTorque(Vector3.up * _moveInput.x * turnTorque, ForceMode.Force);
    }
}
