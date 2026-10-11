using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

/// <summary>
/// ติดกับ Cube (BoxCollider ตั้ง IsTrigger) ที่วางไว้ข้างเรือ
/// ตรวจ Player เข้าใกล้ + กด E -> เดินอัตโนมัติไปหา seatPoint บนเรือ แล้วสลับไปคุมเรือ
/// กด E อีกครั้งระหว่างคุมเรือ -> คืนการควบคุมให้ตัวละคร
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
    [Tooltip("จุดที่ตัวละครจะเดินไปยืน/ขับ (ลาก child Transform บนเรือมาใส่) ใช้แค่ตำแหน่ง — ตอนขับตัวละครจะหันตามทิศ forward ของเรือ")]
    public Transform seatPoint;

    [Header("การเดินเข้าไปนั่ง")]
    [Tooltip("ความเร็วเดินเข้าไปหา seat (หน่วยเดียวกับ MaxStableMoveSpeed ของตัวละคร)")]
    public float walkSpeed = 3f;
    public float arriveDistance = 0.15f;
    [Tooltip("เดินนานเกินนี้ (วินาที) แล้วยังไม่ถึง seat (เช่น ทางโดนบล็อก) -> snap เข้าที่นั่งเลย กันตัวละครเดินค้างโดยที่ input ถูกปิด")]
    public float walkTimeout = 3f;

    private PlayerInputActions _controls;
    private bool _playerInRange;
    private bool _isWalkingToSeat;
    private bool _isControllingBoat;
    private float _walkTimer;

    private void Awake()
    {
        _controls = new PlayerInputActions();
        GetComponent<Collider>().isTrigger = true;
    }

    // ใช้ action เดียว (E) ทั้งขึ้นและลง — เดิมแยก Player.E กับ BoatPlayer.CancelControll ที่ผูกปุ่ม E เหมือนกัน
    // ทั้งคู่ยิงในเฟรมเดียวกันโดยไม่การันตีลำดับ ทำให้กดลงเรือแล้วอาจขึ้นกลับทันที
    private void OnEnable()
    {
        _controls.Player.E.Enable();
        _controls.Player.E.performed += OnInteractPressed;
    }

    private void OnDisable()
    {
        _controls.Player.E.performed -= OnInteractPressed;
        _controls.Player.E.Disable();

        // ถูกปิดกลางทาง -> คืนสภาพตัวละคร ไม่งั้น Motor ปิดค้างและตัวละครติดเป็นลูกของเรือ
        // (ข้ามตอน unload scene/ปิดเกม — object กำลังถูกทำลาย SetParent จะ error)
        if (!gameObject.scene.isLoaded) return;
        if (_isControllingBoat) ExitBoat();
        else if (_isWalkingToSeat)
        {
            _isWalkingToSeat = false;
            playerInputScript.SetControlEnabled(true);
        }
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

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        if (_isControllingBoat)
        {
            ExitBoat();
            return;
        }

        if (!_playerInRange || _isWalkingToSeat) return;
        if (seatPoint == null || player == null || playerInputScript == null || boat == null) return;
        // กันกด E กลางอากาศ (ระหว่างกระโดด/ตก) แล้วเริ่มเดินไปที่นั่งทั้งที่ยังไม่แตะพื้น
        if (!player.Motor.GroundingStatus.IsStableOnGround) return;

        _isWalkingToSeat = true;
        _walkTimer = 0f;
        playerInputScript.SetControlEnabled(false);
    }

    private void ExitBoat()
    {
        _isControllingBoat = false;
        boat.SetControlEnabled(false);
        DetachFromBoat();
        playerInputScript.SetControlEnabled(true);
    }

    private void Update()
    {
        if (_isWalkingToSeat)
        {
            WalkTowardSeat();
        }
    }

    /// <summary>
    /// ล็อกตัวละครติดเรือระหว่างขับ: ปิด KCC Motor แล้ว parent เข้ากับเรือ
    /// KCC ตามพื้น dynamic Rigidbody ด้วยการเดาจาก velocity ทีละ physics step ทำให้กระตุกและหันตามเรือไม่ทันตอนเลี้ยวแรง
    /// ปิด Capsule ด้วย ไม่งั้น collider ตัวละครจะกลายเป็นส่วนหนึ่งของ compound collider ของเรือ
    /// </summary>
    private void AttachToBoat()
    {
        player.Motor.enabled = false;
        player.Motor.Capsule.enabled = false;
        player.transform.rotation = Quaternion.LookRotation(boat.transform.forward, boat.transform.up);
        player.transform.SetParent(boat.transform, true);
    }

    private void DetachFromBoat()
    {
        player.transform.SetParent(null, true);
        // SetParent(worldPositionStays) ตอนเรือเอียงตามคลื่นทำให้ scale เพี้ยนทศนิยมเล็กน้อย (เช่น 0.9999998)
        // KCC เช็ค lossyScale แบบเป๊ะแล้ว LogError ทุก physics step -> รีเซ็ตกลับเป็น 1
        player.transform.localScale = Vector3.one;
        Vector3 flatForward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;

        player.Motor.SetPositionAndRotation(player.transform.position, Quaternion.LookRotation(flatForward));
        player.Motor.BaseVelocity = Vector3.zero;
        player.Motor.Capsule.enabled = true;
        player.Motor.enabled = true;
    }

    private void WalkTowardSeat()
    {
        Vector3 toSeat = seatPoint.position - player.transform.position;
        toSeat.y = 0f;
        float distance = toSeat.magnitude;

        _walkTimer += Time.deltaTime;
        if (distance <= arriveDistance || _walkTimer >= walkTimeout)
        {
            SitDown();
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

    private void SitDown()
    {
        // ใช้ seatPoint แค่แกน X,Z ส่วนแกน Y คงค่าที่ตัวละครยืนอยู่จริง (จาก KCC grounding)
        // กันเรื่องเด้ง/กระโดดตำแหน่งกรณี seatPoint วางค่า Y ไว้ไม่ตรงกับพื้นเรือจริง
        Vector3 snapPosition = new Vector3(seatPoint.position.x, player.transform.position.y, seatPoint.position.z);
        player.Motor.SetPositionAndRotation(snapPosition, player.transform.rotation);

        AICharacterInputs stopInputs = new AICharacterInputs();
        player.SetInputs(ref stopInputs);
        AttachToBoat();

        _isWalkingToSeat = false;
        _isControllingBoat = true;
        boat.SetControlEnabled(true);
    }
}
