using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerNormal.Project_wide
{
    public class PlayerFishing : MonoBehaviour
    {
        public Animator rodAnim;
        [SerializeField] float maxCooldown = 300.0f;
        public float coolDown = 0;
        public bool isCooldown;
        InputAction swingRodAction;

        void Awake()
        {
            swingRodAction = InputSystem.actions.FindAction("Player/SwingRod");
        }

        void Start()
        {
            rodAnim = GetComponent<Animator>();

            isCooldown = true;
        }

        void OnEnable()
        {
            swingRodAction?.Enable();
        }

        void OnDisable()
        {
            swingRodAction?.Disable();
        }

        void Update()
        {
            if (!isCooldown)
            {
                if(coolDown <= 0)
                {
                    coolDown = 0.0f;
                    isCooldown = true;
                }
                else
                {
                    coolDown -= 1.0f;
                }
            }

            if (swingRodAction.WasPressedThisFrame() && coolDown == 0)
            {
                rodAnim.SetTrigger("Fishing");
                Debug.Log("Player is swinging");
                coolDown = maxCooldown;
                isCooldown = false;
            }
        }
    }
    
}
