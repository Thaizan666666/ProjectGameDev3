using Newtonsoft.Json.Linq;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerNormal.Project_wide
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] InputAction moveAction;
        [SerializeField] InputAction jumpAction;

        void Awake()
        {
            moveAction = InputSystem.actions.FindAction("Player/Move");
            jumpAction = InputSystem.actions.FindAction("Player/Jump");
        }

        void OnEnable()
        {
            // Project-wide maps are already enabled by default;
            // explicit Enable() is optional but documents intent.
            moveAction?.Enable();
            jumpAction?.Enable();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
        }

        void Update()
        {
            Vector2 move = moveAction.ReadValue<Vector2>();

            transform.Translate(new Vector3(move.x, 0.0f, move.y) * Time.deltaTime * 5.0f);
        }
    }
    
}
