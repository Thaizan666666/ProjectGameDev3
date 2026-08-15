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
        _actions.BoatPlayer.Enable();
        _actions.BoatPlayer.Move.performed += OnMove;
        _actions.BoatPlayer.Move.canceled += OnMove;
    }

    private void OnDisable()
    {
        _actions.BoatPlayer.Move.performed -= OnMove;
        _actions.BoatPlayer.Move.canceled -= OnMove;
        _actions.BoatPlayer.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();

    private void FixedUpdate()
    {
        // y (W/S) = แรงขับไปข้างหน้าตามหัวเรือ, x (A/D) = แรงบิดเลี้ยวรอบแกน Y
        _rb.AddForce(transform.forward * _moveInput.y * moveForce, ForceMode.Force);
        _rb.AddTorque(Vector3.up * _moveInput.x * turnTorque, ForceMode.Force);
    }
}
