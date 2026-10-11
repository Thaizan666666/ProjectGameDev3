using UnityEngine;
using System.Collections;

/// <summary>
/// Handles instant teleportation of a target GameObject to a destination Transform.
/// Manages CharacterController, Rigidbody, NavMeshAgent, and KinematicCharacterController (KCC) to prevent position conflicts.
/// </summary>
public class Teleport : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Target Transform to teleport to")]
    public Transform destination;

    public bool CanTeleport { get; private set; }

    /// <summary>Enable or disable teleportation for this trigger</summary>
    public void SetTeleportEnabled(bool enabled) => CanTeleport = enabled;

    /// <summary>Attempt to teleport the target to the destination</summary>
    public void TryTeleport(GameObject target)
    {
        if (!ValidateTeleport(target)) return;

        var cc = target.GetComponent<CharacterController>();
        var kcc = target.GetComponent<KinematicCharacterController.Examples.ExampleCharacterController>();
        var rb = target.GetComponent<Rigidbody>();
        var agent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var examplePlayer = target.GetComponent<KinematicCharacterController.Examples.ExamplePlayer>();

        var destPos = destination.position;
        var beforePos = target.transform.position;

        Log($"BEFORE: player={beforePos}, dest={destPos}, delta={Vector3.Distance(beforePos, destPos):F3}");

        // Disable movement components that fight position changes
        DisableMovementComponents(cc, kcc, examplePlayer, rb, agent);

        // Apply teleport — KCC requires Motor.SetPosition(), others use transform.position
        if (kcc != null && kcc.Motor != null)
        {
            kcc.Motor.SetPosition(destPos);
            // Clear internal velocity/state
            kcc.Motor.BaseVelocity = Vector3.zero;
            if (kcc.Motor.CharacterController != null)
            {
                // Reset any grounding/state
                // kcc.Motor.CharacterController.Reset();
            }
        }
        else
        {
            target.transform.position = destPos;
        }

        var afterPos = target.transform.position;
        Log($"AFTER SET: player={afterPos}, match={(Vector3.Distance(afterPos, destPos) < 0.01f)}");

        // Re-enable with correction for CharacterController / KCC
        if (cc != null)
            StartCoroutine(ReenableWithCorrection(cc, target, destPos));
        else if (kcc != null)
            StartCoroutine(ReenableWithCorrectionKCC(kcc, examplePlayer, target, destPos));
        else
            ReenableInstant(rb, agent, target, destPos);
    }

    // ─────────────────────────────────────────────────────────────
    // Private Implementation
    // ─────────────────────────────────────────────────────────────

    private bool ValidateTeleport(GameObject target)
    {
        if (!CanTeleport)
        {
            LogWarning($"Blocked: teleport disabled (CanTeleport=false)");
            return false;
        }
        if (destination == null)
        {
            LogError("Blocked: destination Transform not assigned");
            return false;
        }
        if (target == null)
        {
            LogError("Blocked: target GameObject is null");
            return false;
        }
        return true;
    }

    private void DisableMovementComponents(CharacterController cc, KinematicCharacterController.Examples.ExampleCharacterController kcc, KinematicCharacterController.Examples.ExamplePlayer examplePlayer, Rigidbody rb, UnityEngine.AI.NavMeshAgent agent)
    {
        if (cc != null) cc.enabled = false;
        if (kcc != null) kcc.Motor.enabled = false;  // Disable KCC motor
        if (examplePlayer != null) examplePlayer.enabled = false;  // Disable driver
        if (rb != null) rb.isKinematic = true;
        if (agent != null) agent.enabled = false;
    }

    private void ReenableInstant(Rigidbody rb, UnityEngine.AI.NavMeshAgent agent, GameObject target, Vector3 destPos)
    {
        if (rb != null) rb.isKinematic = false;
        if (agent != null) agent.enabled = true;
        Log($"{target.name} → {destPos} (instant)");
    }

    private IEnumerator ReenableWithCorrection(CharacterController cc, GameObject target, Vector3 destPos)
    {
        yield return new WaitForFixedUpdate();
        cc.enabled = true;

        // Extra frame wait + force correction if CC pushed position back
        yield return null;

        if (Vector3.Distance(target.transform.position, destPos) > 0.1f)
        {
            cc.enabled = false;
            target.transform.position = destPos;
            cc.enabled = true;
            LogWarning($"FORCED correction: player drifted, forced to {target.transform.position}");
        }

        Log($"{target.name} → {destPos} (CharacterController ready)");
    }

    private IEnumerator ReenableWithCorrectionKCC(KinematicCharacterController.Examples.ExampleCharacterController kcc, KinematicCharacterController.Examples.ExamplePlayer examplePlayer, GameObject target, Vector3 destPos)
    {
        // KCC: wait a frame, then re-enable motor
        yield return null;
        kcc.Motor.enabled = true;

        // Extra frame wait + force correction if KCC pushed position back
        yield return null;

        if (Vector3.Distance(target.transform.position, destPos) > 0.1f)
        {
            kcc.Motor.enabled = false;
            kcc.Motor.SetPosition(destPos);
            kcc.Motor.BaseVelocity = Vector3.zero;
            kcc.Motor.enabled = true;
            LogWarning($"FORCED correction (KCC): player drifted, forced to {target.transform.position}");
        }

        // Send zero inputs to clear any residual movement intent
        var zeroInputs = new KinematicCharacterController.Examples.AICharacterInputs
        {
            MoveVector = Vector3.zero,
            LookVector = Vector3.zero
        };
        kcc.SetInputs(ref zeroInputs);

        // Re-enable ExamplePlayer LAST (after motor is stable)
        if (examplePlayer != null)
        {
            examplePlayer.enabled = true;
        }

        Log($"{target.name} → {destPos} (KinematicCharacterController ready)");
    }

    // ─────────────────────────────────────────────────────────────
    // Logging Helpers
    // ─────────────────────────────────────────────────────────────

    private void Log(string msg) => Debug.Log($"[Teleport] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[Teleport] {msg}");
    private void LogError(string msg) => Debug.LogError($"[Teleport] {msg}");
}
