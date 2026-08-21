using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using Unity.Cinemachine;

namespace KinematicCharacterController.Examples
{
    public class ExamplePlayer : MonoBehaviour
    {
        public ExampleCharacterController Character;
        private PlayerInputActions _controls;
        private bool _isControlEnabled = true;
        private void Awake()
        {
            _controls = new PlayerInputActions();
        }
        private void OnEnable()
        {
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        /// <summary>
        /// เปิด/ปิดการรับ input ควบคุมตัวละคร (ใช้ตอนสลับไปคุมเรือ)
        /// ต้องเช็ค flag นี้ใน Update() ด้วย ไม่งั้น HandleCharacterInput() จะยังเรียก
        /// Character.SetInputs() ทุกเฟรมอยู่ดี (ด้วยค่า 0) ไปเขียนทับ input ที่สคริปต์อื่น (เช่น BoatBoardZone
        /// ตอน auto-walk) เพิ่งสั่งไว้ในเฟรมเดียวกัน — แค่ Disable() action map ไม่พอ
        /// </summary>
        public void SetControlEnabled(bool isEnabled)
        {
            _isControlEnabled = isEnabled;
            if (isEnabled)
            {
                _controls.Player.Enable();
            }
            else
            {
                _controls.Player.Disable();
            }
        }
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (!_isControlEnabled) return;

            if (_controls.Player.LeftClick.WasPressedThisFrame())
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            HandleCharacterInput();
        }

        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            Vector2 move = _controls.Player.Move.ReadValue<Vector2>();
            characterInputs.MoveAxisForward = move.y;
            characterInputs.MoveAxisRight = move.x;

            // จุดสำคัญ: เดิมใช้ CharacterCamera.Transform.rotation
            // ตอนนี้ให้ใช้ rotation ของกล้องจริงที่ CinemachineBrain ขับอยู่แทน
            characterInputs.CameraRotation = Camera.main.transform.rotation;

            characterInputs.JumpDown = _controls.Player.Jump.WasPressedThisFrame();
            characterInputs.CrouchDown = _controls.Player.Crouch.WasPressedThisFrame();
            characterInputs.CrouchUp = _controls.Player.Crouch.WasReleasedThisFrame();

            Character.SetInputs(ref characterInputs);
        }
    }
}