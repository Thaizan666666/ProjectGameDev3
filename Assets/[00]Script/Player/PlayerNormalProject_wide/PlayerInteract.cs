using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerNormal.Project_wide
{
    public class PlayerInteract : MonoBehaviour
    {
        [SerializeField] InputAction interactAction;
        public bool isPlayerInteract;

        void Awake()
        {
            interactAction = InputSystem.actions.FindAction("Player/Interact");
        }

        void Start()
        {
            isPlayerInteract = false;
        }

        void OnEnable()
        {
            interactAction?.Enable();
        }

        void OnDisable()
        {
            interactAction?.Disable();
        }

        void Update()
        {
            if (interactAction.WasPressedThisFrame())   //start press button or still
            {
                isPlayerInteract = !isPlayerInteract;
                Debug.Log("Player is interact");
            }
            else if (interactAction.WasReleasedThisFrame()) //release button
            {
                isPlayerInteract = false;
                Debug.Log("Player release interact's button");
            }

        }
    }
}
