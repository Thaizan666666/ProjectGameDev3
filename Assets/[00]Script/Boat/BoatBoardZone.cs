using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

/// <summary>
/// ติดกับ Cube (BoxCollider ตั้ง IsTrigger) ที่วางไว้ข้างเรือ
/// ตรวจ Player เข้าใกล้ + กด E -> เดินอัตโนมัติไปหา seatPoint บนเรือ แล้วสลับไปคุมเรือ
/// กด E (CancelControll) ระหว่างคุมเรือ -> คืนการควบคุมให้ตัวละคร
/// </summary>
[RequireComponent(typeof(Collider))]
public class BoatBoardZone : MonoBehaviour
{
    [Header("อ้างอิง")]
    [Tooltip("ตัวละครที่จะขึ้นเรือ")]
    public ExampleCharacterController player;
    [Tooltip("สคริปต์รับ input เดินของตัวละคร (ExamplePlayer) — จะถูกปิดตอนเดินเข้าไปนั่ง/คุมเรือ")]
    public ExamplePlayer playerInputScript;
    [Tooltip("สคริปต์คุมเรือ (BoatControll)")]
    public BoatControll boat;
    [Tooltip("จุดที่ตัวละครจะเดินไปยืน/ขับ (ลาก child Transform บนเรือมาใส่ ทิศทาง forward ของจุดนี้คือทิศที่ตัวละครจะหันหน้า)")]
    public Transform seatPoint;

    [Header("การเดินเข้าไปนั่ง")]
    [Tooltip("ความเร็วเดินเข้าไปหา seat (หน่วยเดียวกับ MaxStableMoveSpeed ของตัวละคร)")]
    public float walkSpeed = 3f;
    public float arriveDistance = 0.15f;

    private PlayerInputActions _controls;
    private bool _playerInRange;
    private bool _isWalkingToSeat;
    private bool _isControllingBoat;

    private void Awake()
    {
        _controls = new PlayerInputActions();
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnEnable()
    {
        _controls.Player.E.Enable();
        _controls.Player.E.performed += OnEnterPressed;

        _controls.BoatPlayer.CancelControll.Enable();
        _controls.BoatPlayer.CancelControll.performed += OnExitPressed;
    }

    private void OnDisable()
    {
        _controls.Player.E.performed -= OnEnterPressed;
        _controls.Player.E.Disable();

        _controls.BoatPlayer.CancelControll.performed -= OnExitPressed;
        _controls.BoatPlayer.CancelControll.Disable();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other)) _playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other)) _playerInRange = false;
    }

    private bool IsPlayerCollider(Collider other)
    {
        return player != null && other.GetComponentInParent<ExampleCharacterController>() == player;
    }

    private void OnEnterPressed(InputAction.CallbackContext ctx)
    {
        if (!_playerInRange || _isWalkingToSeat || _isControllingBoat) return;
        if (seatPoint == null || player == null || playerInputScript == null || boat == null) return;

        _isWalkingToSeat = true;
        playerInputScript.SetControlEnabled(false);
    }

    private void OnExitPressed(InputAction.CallbackContext ctx)
    {
        if (!_isControllingBoat) return;

        _isControllingBoat = false;
        boat.SetControlEnabled(false);
        playerInputScript.SetControlEnabled(true);
    }

    private void Update()
    {
        if (_isWalkingToSeat)
        {
            WalkTowardSeat();
        }
    }

    private void WalkTowardSeat()
    {
        Vector3 toSeat = seatPoint.position - player.transform.position;
        toSeat.y = 0f;
        float distance = toSeat.magnitude;

        if (distance <= arriveDistance)
        {
            // ใช้ seatPoint แค่แกน X,Z + ทิศทางที่หัน ส่วนแกน Y คงค่าที่ตัวละครยืนอยู่จริง (จาก KCC grounding)
            // กันเรื่องเด้ง/กระโดดตำแหน่งกรณี seatPoint วางค่า Y ไว้ไม่ตรงกับพื้นเรือจริง
            Vector3 snapPosition = new Vector3(seatPoint.position.x, player.transform.position.y, seatPoint.position.z);
            player.Motor.SetPositionAndRotation(snapPosition, seatPoint.rotation);

            AICharacterInputs stopInputs = new AICharacterInputs
            {
                MoveVector = Vector3.zero,
                LookVector = seatPoint.forward
            };
            player.SetInputs(ref stopInputs);

            _isWalkingToSeat = false;
            _isControllingBoat = true;
            boat.SetControlEnabled(true);
            return;
        }

        Vector3 moveDir = toSeat.normalized;
        float speedFraction = Mathf.Clamp01(walkSpeed / Mathf.Max(player.MaxStableMoveSpeed, 0.01f));

        AICharacterInputs inputs = new AICharacterInputs
        {
            MoveVector = moveDir * speedFraction,
            LookVector = moveDir
        };
        player.SetInputs(ref inputs);
    }
}
