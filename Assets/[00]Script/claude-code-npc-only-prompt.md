# Unity NPC Setup — Implementation Prompt

## Context
I'm building a 3D Unity game using:
- **Yarn Spinner** for dialogue
- **NavMeshAgent** for player movement (I already have a `PlayerController` with a method `MoveToAndInteract(Transform target, Action onArrive)` and `UnlockControls()`)
- An existing `IInteractable` interface:

```csharp
public interface IInteractable
{
    void Interact();
    void SetHighlighted(bool isHighlighted);
    Transform GetTransform();
    bool CanInteract();
}
```

I need the **NPC side only** implemented — the player/input system already exists.

---

## `NPCDialogue.cs` Requirements

Implement `IInteractable` on this script:

- Fields:
  - `yarnStartNode` (string) — Yarn node to start
  - `dialogueRunner` (DialogueRunner reference)
  - `talkPoint` (Transform) — child object placed ~1–1.5 units in front of the NPC, rotated to face away from the NPC (so player ends up facing the NPC)
  - `highlightIndicator` (GameObject) — world-space icon/outline, disabled by default
  - `animator` (Animator, optional)
  - `isBusy` (bool)

- `Interact()`:
  - If `isBusy`, return immediately.
  - Find the player's `PlayerController` (via tag "Player") and call `MoveToAndInteract(talkPoint, OnArrived)`.

- `OnArrived()`:
  - Set `isBusy = true`.
  - Begin smoothly rotating to face the player in `Update()` using `Quaternion.Slerp` (NOT instant `LookAt` — should feel gradual, not snap instantly).
  - Flatten the Y component of the direction vector so the NPC doesn't tilt its head up/down while turning.
  - Call `dialogueRunner.StartDialogue(yarnStartNode)`.

- `CanInteract()`:
  - Return `!isBusy` (leave a comment showing where to extend with `isSleeping` / `isDead` flags later).

- `SetHighlighted(bool)`:
  - Toggle `highlightIndicator.SetActive(...)`.

- `GetTransform()`:
  - Return `transform`.

- Subscribe to `dialogueRunner.onDialogueComplete` (in `OnEnable`/`OnDisable` or `Start`) to:
  - Stop the facing rotation.
  - Set `isBusy = false`.
  - Call the player's `PlayerController.UnlockControls()`.

---

## Prefab / Hierarchy Setup

Please also give me exact steps (or a script if it can be done via editor scripting) for a reusable **NPC prefab**:

```
NPC_Root                  <- NPCDialogue.cs here
├── Model                 <- mesh + Animator
├── TalkPoint              <- empty Transform, positioned/rotated per above
├── HighlightIndicator      <- world-space icon, disabled by default
└── InteractTrigger         <- Collider, Is Trigger = true, layer = "Interactable"
```

So that making a new NPC only requires: duplicate the prefab, reposition it, set `yarnStartNode`, and swap the model.

---

## Deliverables
1. Fully implemented, commented `NPCDialogue.cs`.
2. Steps to set up the prefab hierarchy above.
3. A short note on which Inspector fields need to be assigned manually per NPC instance vs. which are already wired in the prefab.

Ask me clarifying questions only if something above is ambiguous — otherwise implement directly.
