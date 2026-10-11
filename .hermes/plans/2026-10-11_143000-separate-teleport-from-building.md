# Separate Teleport from Building.cs — Standalone Teleport GameObject with Collider Trigger

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Decouple teleport logic from `Building.cs` — make Teleport self-contained on a dedicated GameObject with its own `Collider Trigger`, so player interact directly triggers teleport without Building script interference.

**Architecture:** Create a new `TeleportTrigger` component that owns the Collider Trigger and handles E-key interaction → KCC-correct teleport via `Motor.SetPosition()`. `Building.cs` becomes pure building data/upgrade memory with no teleport references. The existing `Teleport.cs` (already KCC-fixed) is kept as-is for reference or reused.

**Tech Stack:** Unity 6000.3.9f1, C#, KinematicCharacterController (KCC), Input System

---

### Task 1: Create `TeleportTrigger` script (new file)

**Objective:** Self-contained teleport trigger that detects player on Collider Enter, waits for E key, then teleports using KCC-correct pattern.

**Files:**
- Create: `Assets\[00]Script\Teleportation\TeleportTrigger.cs`

**Step 1: Write the script**

```csharp
using UnityEngine;
using KinematicCharacterController.Examples;

[RequireComponent(typeof(Collider))]
public class TeleportTrigger : MonoBehaviour
{
    [Header("Destination")]
    public Transform destination;

    [Header("Teleport Settings")]
    public float cooldown = 1f;
    public bool canTeleport = true;

    private Collider _collider;
    private ExamplePlayer _examplePlayer;
    private float _lastTeleportTime;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        
        // Resolve player once at startup (or find each time)
        _examplePlayer = FindObjectOfType<ExamplePlayer>();
        if (_examplePlayer == null)
        {
            Debug.LogError("[TeleportTrigger] No ExamplePlayer found in scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Optional: visual feedback — player entered trigger zone
        // Could enable a UI prompt or highlight the object
    }

    private void Update()
    {
        if (!canTeleport) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Optional: only teleport if player is near enough or in UI state
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        if (!canTeleport) return;
        if (Time.time - _lastTeleportTime < cooldown) return;

        // Find the player — could be tagged or resolved via reference
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Get KCC component
        var kcc = player.GetComponent<KinematicCharacterController.Examples.ExampleCharacterController>();
        if (kcc == null) return;

        // Get motor and teleport
        var motor = kcc.Motor;
        if (motor == null) return;

        // Use the same KCC-correct teleport pattern as Teleport.cs
        var teleport = GetComponent<Teleport>(); // reuse existing Teleport if on same object, or reference separately
        if (teleport != null && teleport.destination != null)
        {
            // Disable driver + motor
            _examplePlayer.enabled = false;
            motor.enabled = false;

            // Set position via Motor (bypasses internal state)
            motor.SetPosition(teleport.destination.position);
            motor.BaseVelocity = Vector3.zero;
            if (motor.CharacterController != null)
            {
                motor.CharacterController.Reset();
            }

            // Re-enable motor (stable now)
            motor.enabled = true;

            // Re-enable driver LAST (after motor stable)
            _examplePlayer.enabled = true;

            _lastTeleportTime = Time.time;
            Debug.Log($"[TeleportTrigger] Player teleported to {teleport.destination.position}");
        }
        else
        {
            Debug.LogWarning("[TeleportTrigger] Destination not assigned on this TeleportTrigger.");
        }
    }
}
```

**Step 2: Run test**
- Place object with `TeleportTrigger` + Collider (IsTrigger) + assigned `Destination` Transform in Scene
- Run Unity Editor, approach object, press **E**
- **Expected:** Player teleports to destination, stays put (no snap-back), Console shows `[TeleportTrigger] Player teleported to ...`
- **Verification:** `Player.transform.position == Destination.transform.position`

**Step 3: Commit**
```bash
git add Assets/[00]Script/Teleportation/TeleportTrigger.cs
git commit -m "feat: add TeleportTrigger — standalone collider-trigger teleport, KCC-correct"
```

---

### Task 2: Add standalone Teleport GameObject to Scene

**Objective:** Create the actual GameObject in `WefLabScene 2.unity` that holds the trigger.

**Step 1: Create Prefab or Hierarchy object**
- In Project window: Right-click `Assets\[00]Script\Teleportation/` → **Create Empty** → name `Teleport_Aquarium`
- Add Components:
  - `Collider` → set **Is Trigger** = True
  - `TeleportTrigger` (script from Task 1)
  - Assign **Destination** field → drag `Destination_A` Transform (or create empty GameObject at (14.91, 2.40, 15.35) and assign)
- Position the object at or near `Aquarium_lv.1`'s original position (-22.30, 6.85, 21.77) so player interacts in same spot

**Step 2: Test in Editor**
- Run Scene, walk player into trigger volume, press **E**
- **Expected:** Same as Task 1 Step 2
- **Verification:** Console log + Player position matches destination

**Step 3: Commit**
```bash
# No git change yet unless prefab; just verify in scene
```

---

### Task 3: Remove teleport from `Building.cs` (decouple)

**Objective:** Strip out `_teleport`, `CanTeleport`, `SetTeleportEnabled`, `TryTeleport` calls so Building is pure data.

**Step 1: Read current Building.cs**
- Path: `Assets\[00]Script\Player\PlayerNormal\Project_wide\Building.cs`
- Understand all fields/methods (known: `buildingName`, `currentLevel`, `canUpgrade`, `data`, `_teleport`, `_teleport.TryTeleport()`)

**Step 2: Remove teleport-related code**
Delete or comment out:
- `public Teleport _teleport;` field
- `public void SetTeleportEnabled(bool enabled)` method
- `public bool CanTeleport { get; private set; }` property
- Any `TryTeleport()` calls in Interaction methods
- `OnInteract()` if it only calls teleport

**Step 3: Keep Building data fields**
- `buildingName`, `currentLevel`, `canUpgrade`, `data`, `levels` — keep these if other systems use them, or move to separate `BuildingData.cs`

**Step 4: Test that Building still compiles**
- Open Scene, verify no Compiler Errors
- Confirm `Aquarium_lv.1` Building component still has its data intact (or decide to clear it since it's now handled elsewhere)

**Step 5: Commit**
```bash
git add Assets/[00]Script/Player/PlayerNormal/Project_wide/Building.cs
git commit -m "feat: remove teleport from Building — teleport now handled by standalone TeleportTrigger"
```

---

### Task 4: Update Scene — Remove old Teleport reference from Aquarium Building

**Objective:** `Aquarium_lv.1`'s Building no longer references `_teleport`.

**Step 1: Open `WefLabScene 2.unity`**
- Select `Aquarium_lv.1` GameObject
- Find `Building` component in Inspector
- Remove/clear the `_teleport` field (or delete the component if teleport is fully removed)

**Step 2: Verify**
- Build/Project compiles
- No missing reference errors

**Step 3: Commit**
```bash
git add Assets/\[04]Scenes/WefLabScene 2.unity
git commit -m "chore: remove _teleport reference from Aquarium Building — now uses standalone TeleportTrigger"
```

---

## 🧪 **Tests / Validation Checklist**

| # | Test | Expected Result |
|---|------|-----------------|
| 1 | Place `TeleportTrigger` object in Scene, assign Destination | Object has Collider (IsTrigger) + script + destination reference |
| 2 | Approach object, press **E** | Player teleports to destination position (14.91, 2.40, 15.35) |
| 3 | After teleport, try moving player | Player can move normally (ExamplePlayer re-enabled last) |
| 4 | Press **E** again during cooldown (`1s`) | No teleport occurs; visual/UX feedback if desired |
| 5 | Console after teleport | No `FORCED correction (KCC)` logs after the initial 2-frame correction |
| 6 | Remove old `_teleport` from Aquarium Building | Building compiles, no missing reference errors |

---

## ⚠️ **Risks, Tradeoffs, and Open Questions**

| Risk | Mitigation |
|------|------------|
| **UI prompt system not connected** — if current prompt shows "E to interact" over Building, it may need to point to new Teleport object | Keep a minimal `OnInteract()` in Building that just enables a UI hint over the new Teleport object, or use a global Input system |
| **Player tag / reference resolution** — `GameObject.FindGameObjectWithTag("Player")` is fragile | Consider exposing a public `GameObject player` field on `TeleportTrigger`, or resolve via `PlayerController` reference |
| **Multiple teleport objects in same scene** — if other buildings also want teleport | `TeleportTrigger` is reusable; just assign different `Destination` and position each object independently |
| **Old `Aquarium_lv.1` Building data** — `levels: []` was placeholder; now it's purely data | Decide whether to keep Building script on Aquarium for future upgrade systems, or replace with `BuildingData.cs` |
| **Cooldown design** — `1f` cooldown may feel too short/long | Expose `cooldown` as Inspector variable; tune after playtesting |

---

## ✅ **Done Criteria**

- [ ] `TeleportTrigger.cs` created and functional in Unity Editor
- [ ] Standalone Teleport GameObject placed in `WefLabScene 2.unity` with Collider Trigger + Destination assigned
- [ ] `Building.cs` on `player(V3)` and `Aquarium_lv.1` stripped of teleport code, compiles cleanly
- [ ] Player E → teleport → stays at destination (no snap-back) verified in 3 consecutive runs
- [ ] Console: No `FORCED correction (KCC)` after first frame
- [ ] Git commits after each task with descriptive messages

---

### 📥 **Execution Handoff**

**Plan complete and saved.** Ready to execute using **subagent-driven-development** — I'll dispatch a fresh subagent per task with full context, spec compliance review after each task, then code quality review. Shall I proceed?