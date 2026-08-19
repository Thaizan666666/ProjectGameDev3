// ─────────────────────────────────────────────────────────────
// NPCDialogue.cs
// NPC-side of the talk-to-NPC flow: player walks to TalkPoint,
// NPC turns to face the player, Yarn dialogue starts, and control
// is handed back to the player when the dialogue ends.
// Attach: NPC_Root (see prefab hierarchy notes)
// Requires the player GameObject (tagged "Player") to have a
// PlayerController component (see Assets/[00]Script/Player/PlayerController.cs).
// ─────────────────────────────────────────────────────────────
using System;
using UnityEngine;
using Yarn.Unity;

public class NPCDialogue : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private string yarnStartNode;
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Talk Setup")]
    [Tooltip("Child transform ~1-1.5 units in front of the NPC, rotated to face away from the NPC so the player ends up facing the NPC on arrival.")]
    [SerializeField] private Transform talkPoint;
    [Tooltip("World-space icon/outline shown when this NPC is the current interact target. Disabled by default.")]
    [SerializeField] private GameObject highlightIndicator;
    [SerializeField] private Animator animator;

    [Header("Facing")]
    [Tooltip("Higher = snappier turn toward the player once they arrive.")]
    [SerializeField] private float faceTurnSpeed = 6f;

    public bool isBusy { get; private set; }

    private Transform _playerTransform;
    private bool _isFacingPlayer;

    private void OnEnable()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(HandleDialogueComplete);
    }

    private void OnDisable()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
    }

    private void Update()
    {
        if (!_isFacingPlayer || _playerTransform == null) return;

        // Flatten Y so the NPC turns in place instead of tilting its head up/down.
        Vector3 toPlayer = _playerTransform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, faceTurnSpeed * Time.deltaTime);
    }

    public void Interact()
    {
        if (isBusy) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning($"{name}: no GameObject tagged 'Player' found.", this);
            return;
        }

        PlayerController playerController = playerObj.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning($"{name}: Player is missing a PlayerController component.", this);
            return;
        }

        _playerTransform = playerObj.transform;
        playerController.MoveToAndInteract(talkPoint, OnArrived);
    }

    private void OnArrived()
    {
        isBusy = true;
        _isFacingPlayer = true;

        _ = dialogueRunner.StartDialogue(yarnStartNode);
    }

    private void HandleDialogueComplete()
    {
        _isFacingPlayer = false;
        isBusy = false;

        if (_playerTransform != null && _playerTransform.TryGetComponent(out PlayerController playerController))
        {
            playerController.UnlockControls();
        }

        _playerTransform = null;
    }

    public bool CanInteract()
    {
        // TODO: extend later, e.g. return !isBusy && !isSleeping && !isDead;
        return !isBusy;
    }

    public void SetHighlighted(bool isHighlighted)
    {
        if (highlightIndicator != null)
            highlightIndicator.SetActive(isHighlighted);
    }

    public Transform GetTransform() => transform;
}
