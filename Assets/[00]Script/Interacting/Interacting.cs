using UnityEngine;
using UnityEngine.InputSystem;

public class Interacting : MonoBehaviour
{
    private PlayerInputActions _controls;
    [SerializeField] Collider Collider;
    private void Awake()
    {
        _controls = new PlayerInputActions();
        Collider = GetComponent<Collider>();
    }
    private void OnEnable()
    {
        _controls.Interacting.Enable();
    }
    private void OnDisable()
    {
        _controls.Interacting.Disable();
    }
    private void FixedUpdate()
    {

    }
}
