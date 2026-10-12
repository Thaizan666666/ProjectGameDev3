using System.Collections;
using UnityEngine;

/// <summary>
/// Standalone teleport trigger implementing IInteractable (global namespace).
/// Integrates with existing PlayerInteract system — player enters trigger, presses E, teleports.
/// Uses Teleport.cs for KCC-correct position transfer.
/// </summary>
public class TeleportTrigger : MonoBehaviour, IInteractable
{
    [Header("References")]
    [Tooltip("Teleport system that handles the actual position change")]
    public Teleport teleporter;
    public GameObject UI_Interact;

    [Header("Settings")]
    [Tooltip("Cooldown between teleports (seconds)")]
    public float teleportCooldown = 1.0f;

    // Internal state
    private bool canTeleport = true;
    private GameObject player;
    private const string PLAYER_TAG = "Player";

    // Cached components
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
    }

    private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                player = other.gameObject;
                UI_Interact.SetActive(true);
                // Enable teleport when player enters
                if (teleporter != null)
                    teleporter.SetTeleportEnabled(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                player = null;
                UI_Interact.SetActive(false);
                // Disable teleport when player exits
                if (teleporter != null)
                    teleporter.SetTeleportEnabled(false);
            }
        }

    // ─────────────────────────────────────────────────────────────
    // IInteractable Implementation
    // ─────────────────────────────────────────────────────────────

    /// <summary>Called by PlayerInteract when player presses E while this is the current target</summary>
    public void Interact()
    {
        if (!canTeleport) return;
        if (player == null || teleporter == null)
        {
            Debug.LogWarning("[TeleportTrigger] Missing player or teleport reference");
            return;
        }

        FadeBlackScreen.Instance.FadeIn(() =>
        {
            teleporter.TryTeleport(player);
            // FadeBlackScreen.SnapCinemachineCameras();
            FadeBlackScreen.Instance.FadeOut();
        });


        // Execute teleport using the fixed KCC pattern
        // waitFadeInEnd(1.0f);

        // Set cooldown guard
        canTeleport = false;
        StartCoroutine(TeleportCooldown());
    }

    // private IEnumerator waitFadeInEnd(float seconds)
    // {
    //     yield return new WaitForSeconds(seconds);
    //     teleporter.TryTeleport(player);
    // }

    /// <summary>Called by PlayerInteract to show/hide highlight UI</summary>
    public void SetHighlighted(bool isHighlighted)
    {
        // Optional: implement visual feedback (glow, outline, prompt)
        // Could enable a child UI object here
    }

    /// <summary>Transform for distance checks in PlayerInteract</summary>
    public Transform GetTransform() => transform;

    /// <summary>Player must be in trigger AND cooldown ready</summary>
    public bool CanInteract() => canTeleport && player != null && teleporter != null;

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator TeleportCooldown()
    {
        yield return new WaitForSeconds(teleportCooldown);
        canTeleport = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1.0f, 0.3f);
        if (triggerCollider != null)
        {
            Gizmos.DrawWireCube(transform.position, triggerCollider.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(2, 2, 2));
        }
    }
}