// ─────────────────────────────────────────────────────────────
// NPCWalkTo.cs
// Yarn command for scripted NPC movement: looks up an empty
// GameObject by name and walks this NPC toward it in a straight
// line, pausing the dialogue until it arrives.
// Moves via Rigidbody.linearVelocity (X/Z only) so gravity keeps
// the NPC on the ground through normal physics collision instead
// of a manual raycast.
// Attach: the NPC ROOT (same object as the Rigidbody + Collider).
// Requires: Rigidbody (Use Gravity on, Freeze Rotation X/Y/Z on —
// this script drives rotation manually).
// ─────────────────────────────────────────────────────────────
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class NPCWalkTo : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 4f;
    [SerializeField] private float arriveDistance = 0.1f;
    [SerializeField] private float turnStopAngle = 1f;

    [SerializeField] private Animator animatorNPC;

    /// <summary>
    /// Yarn: <<npc_walk_to_point "NpcGameObjectName" "WalkPointObjectName">>
    /// Looks up an empty GameObject by name and walks this NPC toward its
    /// Transform in a straight line (no pathfinding, no obstacle avoidance).
    /// Dialogue waits until the NPC arrives.
    /// </summary>
    [YarnCommand("npc_walk_to_point")]
    public IEnumerator WalkToPoint(string pointName)
    {
        GameObject pointObj = GameObject.Find(pointName);
        if (pointObj == null)
        {
            Debug.LogWarning($"{name}: no GameObject named '{pointName}' found.", this);
            yield break;
        }
        Transform walkPoint = pointObj.transform;

        Rigidbody rb = transform.root.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"{name}: root '{transform.root.name}' has no Rigidbody.", this);
            yield break;
        }

        while (true)
        {
            Vector3 toPoint = walkPoint.position - transform.root.position;
            toPoint.y = 0f;

            if (toPoint.sqrMagnitude <= arriveDistance * arriveDistance)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                yield break;
            }

            Vector3 direction = toPoint.normalized;
            animatorNPC.SetBool("isGWalk", true);
            // Only drive X/Z; leave Y velocity alone so gravity keeps handling the fall/ground contact.
            rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);

            // Turn to face the walk direction as it moves (flattened, matches NPCDialogue's facing style).
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.root.rotation = Quaternion.Slerp(transform.root.rotation, targetRotation, turnSpeed * Time.deltaTime);

            yield return null;

            animatorNPC.SetBool("isGWalk", false);
        }
    }

    /// <summary>
    /// Yarn: <<npc_turn_to_point "NpcGameObjectName" "PointObjectName">>
    /// Looks up an empty GameObject by name and smoothly rotates this NPC
    /// (via Quaternion.Slerp) to face it, without moving. Dialogue waits
    /// until the turn finishes.
    /// </summary>
    [YarnCommand("npc_turn_to_point")]
    public IEnumerator TurnToPoint(string pointName)
    {
        GameObject pointObj = GameObject.Find(pointName);
        if (pointObj == null)
        {
            Debug.LogWarning($"{name}: no GameObject named '{pointName}' found.", this);
            yield break;
        }
        Transform point = pointObj.transform;

        while (true)
        {
            Vector3 toPoint = point.position - transform.root.position;
            toPoint.y = 0f;

            if (toPoint.sqrMagnitude < 0.0001f) yield break;

            Quaternion targetRotation = Quaternion.LookRotation(toPoint.normalized);
            transform.root.rotation = Quaternion.Slerp(transform.root.rotation, targetRotation, turnSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.root.rotation, targetRotation) <= turnStopAngle) yield break;

            yield return null;
        }
    }
}
