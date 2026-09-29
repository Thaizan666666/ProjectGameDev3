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

        examplePlayer.enabled = false;
    }

    [YarnCommand("UnlockControlls")]
    public void UnlockControls()
    {
        _isAutoMoving = false;
        _moveTarget = null;
        _onArrive = null;
        _suppressFacing = false;

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

        if (_isAutoMoving)
        {
            if (distance <= arriveDistance)
            {
                _isAutoMoving = false;

                Action callback = _onArrive;
                _onArrive = null;
                callback?.Invoke();

                // The callback (e.g. NPCDialogue.OnArrived -> dialogue completes
                // synchronously for a trivial/empty Yarn node) may have already
                // called UnlockControls(), clearing _moveTarget. Bail out if so.
                if (_moveTarget == null) return;
                // Otherwise fall through to face-the-NPC below, so turning
                // starts the same frame we arrive.
            }
            else
            {
                Vector3 direction = toTarget / distance;
                // Scale move strength down inside slowDownRadius so the character decelerates
                // into the target instead of blowing past the (small) arrive threshold at full speed.
                float moveStrength = Mathf.Clamp01(distance / slowDownRadius);
                SetAIInputs(direction * moveStrength, direction);
                return;
            }
        }

        // Paused while player_turn_to_point's coroutine is driving rotation instead.
        if (_suppressFacing) return;

        // Arrived: stay put but keep smoothly turning to face the NPC (the talk
        // point's parent) for as long as _moveTarget is set, i.e. until
        // UnlockControls() is called when the dialogue ends.
        Transform faceTarget = _moveTarget.parent != null ? _moveTarget.parent : _moveTarget;
        Vector3 toFace = faceTarget.position - transform.position;
        toFace.y = 0f;
        SetAIInputs(Vector3.zero, toFace.sqrMagnitude > 0.0001f ? toFace.normalized : Vector3.zero);
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
