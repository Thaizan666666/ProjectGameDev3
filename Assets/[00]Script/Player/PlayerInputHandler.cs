// ─────────────────────────────────────────────────────────────
// PlayerInputHandler.cs
// อ่าน Input ทั้งหมด → expose เป็น Properties ให้ Script อื่นใช้
// Attach: Player GameObject
// ─────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1)]   // รันก่อน Script อื่นทุกตัว
public class PlayerInputHandler : MonoBehaviour
{
    // ── Public Properties ─────────────────────────────────────
    public Vector2 MoveInput { get; private set; }  // WASD
    public Vector2 LookInput { get; private set; }  // Mouse Delta
    public bool JumpPressed { get; private set; }  // one-shot (true แค่ 1 frame)
    public bool SprintHeld { get; private set; }  // continuous hold

    private PlayerInputActions _actions;

    private void Awake() => _actions = new PlayerInputActions();

    private void OnEnable()
    {
        _actions.Enable();

        // Move (WASD)
        _actions.Player.Move.performed += c => MoveInput = c.ReadValue<Vector2>();
        _actions.Player.Move.canceled += c => MoveInput = Vector2.zero;

        // Look (Mouse Delta)
        _actions.Player.Look.performed += c => LookInput = c.ReadValue<Vector2>();
        _actions.Player.Look.canceled += c => LookInput = Vector2.zero;

        // Jump (Space) — one-shot
        _actions.Player.Jump.performed += c => JumpPressed = true;

        // Sprint (Left Shift) — hold
        _actions.Player.Sprint.performed += c => SprintHeld = true;
        _actions.Player.Sprint.canceled += c => SprintHeld = false;
    }

    private void OnDisable() => _actions.Disable();

    // Reset JumpPressed ทุก frame หลัง Script อื่นอ่านค่าไปแล้ว
    private void LateUpdate() => JumpPressed = false;
}