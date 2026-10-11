// ─────────────────────────────────────────────────────────────
// PlayerController.cs
// Bridges "walk to this point then do something" requests (e.g. NPC
// dialogue) with the Kinematic Character Controller example stack
// (ExamplePlayer + ExampleCharacterController).
//
// While an auto-move is active, ExamplePlayer (manual input) is
// disabled and movement is driven instead via
// ExampleCharacterController's AI input path (SetInputs(ref
// AICharacterInputs)). Disabling ExamplePlayer also disables its own
// Input Action map, which is what actually locks player controls.
//
// Attach: same GameObject as ExamplePlayer (the one tagged "Player").
// ─────────────────────────────────────────────────────────────
using System;
using System.Collections;
using UnityEngine;
using KinematicCharacterController.Examples;
using Yarn.Unity;

[RequireComponent(typeof(ExamplePlayer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private ExamplePlayer examplePlayer;
    [Tooltip("Distance (XZ) to the target at which the character is considered arrived.")]
    [SerializeField] private float arriveDistance = 0.3f;
    [Tooltip("Distance (XZ) within which movement input starts scaling down, so the character decelerates into the target instead of overshooting it at full speed.")]
    [SerializeField] private float slowDownRadius = 1.5f;
    [Tooltip("Degrees per second for player_turn_to_point.")]
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float turnStopAngle = 1f;

    private ExampleCharacterController Character => examplePlayer.Character;

    /// <summary>true ตั้งแต่เริ่มเดินไปคุย NPC จนกว่า UnlockControls() — ใช้กันระบบอื่น (เช่น FishingCastController)
    /// เผลอ re-enable examplePlayer คืนก่อน dialogue จบ (เช่น ตกปลาสำเร็จระหว่าง dialogue แล้วเดินได้ก่อนคุยจบ)</summary>
    public bool IsInteracting { get; private set; }

    private bool _isAutoMoving;
    private Transform _moveTarget;
    private Action _onArrive;
    // Paused while player_turn_to_point's coroutine owns rotation, so the
    // "stay put and face the NPC" branch below doesn't fight it every frame.
    private bool _suppressFacing;

    private void Reset()
    {
        examplePlayer = GetComponent<ExamplePlayer>();
    }

    public void MoveToAndInteract(Transform target, Action onArrive)
    {
        _moveTarget = target;
        _onArrive = onArrive;
        _isAutoMoving = true;
        IsInteracting = true;

        examplePlayer.enabled = false;
    }

    [YarnCommand("UnlockControlls")]
    public void UnlockControls()
    {
        _isAutoMoving = false;
        _moveTarget = null;
        _onArrive = null;
        _suppressFacing = false;
        IsInteracting = false;

        examplePlayer.enabled = true;
    }
    [YarnCommand("LockControlls")]
    public void lockControls()
    {
        examplePlayer.enabled = false;
    }

    /// <summary>
    /// Yarn: <<player_turn_to_point "PlayerGameObjectName" "PointObjectName">>
    /// Looks up an empty GameObject by name and rotates the player to face it,
    /// via Character.Motor.SetRotation() — not transform.rotation directly,
    /// since KCC re-syncs transform.rotation FROM Motor.TransientRotation every
    /// physics tick, which would silently overwrite a direct transform edit.
    /// Does not move the player. Dialogue waits until the turn finishes.
    /// </summary>
    [YarnCommand("player_turn_to_point")]
    public IEnumerator TurnToPoint(string pointName)
    {
        GameObject pointObj = GameObject.Find(pointName);
        if (pointObj == null)
        {
            Debug.LogWarning($"{name}: no GameObject named '{pointName}' found.", this);
            yield break;
        }

        Vector3 toPoint = pointObj.transform.position - transform.position;
        toPoint.y = 0f;
        if (toPoint.sqrMagnitude < 0.0001f) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(toPoint.normalized);

        _suppressFacing = true;
        while (Quaternion.Angle(Character.Motor.TransientRotation, targetRotation) > turnStopAngle)
        {
            Quaternion nextRotation = Quaternion.RotateTowards(Character.Motor.TransientRotation, targetRotation, turnSpeed * Time.deltaTime);
            Character.Motor.SetRotation(nextRotation);
            yield return null;
        }
        Character.Motor.SetRotation(targetRotation);
        _suppressFacing = false;
    }

    private void Update()
    {
        if (_moveTarget == null) return;

        Vector3 toTarget = _moveTarget.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (!_isAutoMoving) return; // หันเข้า NPC ไปแล้วรอบเดียวตอนมาถึง ไม่ต้องทำอะไรต่อจนกว่าจะ UnlockControls

        if (distance <= arriveDistance)
        {
            _isAutoMoving = false;

            // ตัด MoveVector ค้างจากเฟรมก่อนออกก่อน ไม่งั้น Character ยังไถลต่อด้วย input เก่า
            // (Motor ใช้ input ล่าสุดค้างไว้จนกว่าจะมีคนสั่ง zero ให้ชัด ๆ — บั๊กเดียวกับตอนกด F ขณะเดินอยู่)
            SetAIInputs(Vector3.zero, Vector3.zero);

            // หันเข้าหา NPC ให้เสร็จก่อน ค่อยเริ่ม dialogue (callback) ทีหลัง — ไม่งั้น dialogue
            // box โผล่พร้อมกับที่ตัวละครกำลังหันอยู่ ดูเหมือนหันไม่เสร็จก็คุยไปแล้ว
            Transform faceTarget = _moveTarget.parent != null ? _moveTarget.parent : _moveTarget;
            Action callback = _onArrive;
            _onArrive = null;
            StartCoroutine(FaceThenInvoke(faceTarget, callback));
        }
        else
        {
            Vector3 direction = toTarget / distance;
            // Scale move strength down inside slowDownRadius so the character decelerates
            // into the target instead of blowing past the (small) arrive threshold at full speed.
            float moveStrength = Mathf.Clamp01(distance / slowDownRadius);
            SetAIInputs(direction * moveStrength, direction);
        }
    }

    /// <summary>
    /// หันเข้าหา NPC ครั้งเดียวตอนมาถึง (เรียกจาก Update ตอน arrive) ใช้ turnSpeed/turnStopAngle
    /// เดียวกับ player_turn_to_point แล้วปล่อย ไม่หันต่อเนื่องซ้ำทุกเฟรมเหมือนเดิมอีก
    /// </summary>
    /// <summary>หันเข้าหา NPC ให้เสร็จก่อน แล้วค่อยเรียก callback (เริ่ม dialogue) ทีหลัง</summary>
    private IEnumerator FaceThenInvoke(Transform faceTarget, Action callback)
    {
        yield return FaceTargetOnce(faceTarget);
        callback?.Invoke();
    }

    private IEnumerator FaceTargetOnce(Transform faceTarget)
    {
        Vector3 toFace = faceTarget.position - transform.position;
        toFace.y = 0f;
        if (toFace.sqrMagnitude < 0.0001f) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(toFace.normalized);

        _suppressFacing = true;
        while (Quaternion.Angle(Character.Motor.TransientRotation, targetRotation) > turnStopAngle)
        {
            Quaternion nextRotation = Quaternion.RotateTowards(Character.Motor.TransientRotation, targetRotation, turnSpeed * Time.deltaTime);
            Character.Motor.SetRotation(nextRotation);
            yield return null;
        }
        Character.Motor.SetRotation(targetRotation);
        _suppressFacing = false;
    }

    private void SetAIInputs(Vector3 moveVector, Vector3 lookVector)
    {
        AICharacterInputs inputs = new AICharacterInputs
        {
            MoveVector = moveVector,
            LookVector = lookVector
        };
        Character.SetInputs(ref inputs);
    }
}
