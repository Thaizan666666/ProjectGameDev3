# FadeBlackScreen Setup

Script: `Assets/[00]Script/SceneScripts/FadeBlackScreen.cs`
Type: persistent singleton, access anywhere via `FadeBlackScreen.Instance`.

## 1. Create the Canvas prefab (do this once)

1. In the **first scene that loads when the game starts** (e.g. `MainMenu`), right-click Hierarchy → `UI > Canvas`. Rename it `FadeCanvas`.
2. On the `FadeCanvas` GameObject, set **Canvas**:
   - `Render Mode` = `Screen Space - Overlay`
   - `Sort Order` = a high number (e.g. `999`) so it renders on top of every other UI Canvas in every scene.
3. Add component **Canvas Group** to `FadeCanvas` (same GameObject as the Canvas).
4. Right-click `FadeCanvas` → `UI > Image`. Name it `BlackImage`.
   - Stretch the `RectTransform` to fill the whole screen (anchor preset: stretch/stretch, all offsets = 0).
   - Set **Image → Color**: RGB = `0, 0, 0`, **Alpha = 255** (fully opaque black).
     - ⚠️ This alpha must stay at 255. Do **not** lower it — the script fades visibility by animating `CanvasGroup.alpha` (0–1), not the Image's own color alpha. If the Image alpha is less than 255, the screen can never go fully black.
5. Add component **Fade Black Screen** (`FadeBlackScreen.cs`) to the `FadeCanvas` GameObject (same object as the `Canvas Group`, required by `[RequireComponent(typeof(CanvasGroup))]`).
   - Inspector fields:
     - `Fade In Duration` — seconds to go from clear → full black (default `0.5`)
     - `Fade Out Duration` — seconds to go from full black → clear (default `0.5`)

You now have one `FadeCanvas` GameObject sitting in your first scene. Do **not** place another copy of it in any other scene — `Awake()` handles cross-scene persistence itself (see below).

## 2. How it persists across scenes

`FadeBlackScreen.Awake()`:
- On first load: sets itself as `FadeBlackScreen.Instance` and calls `DontDestroyOnLoad(gameObject)` — it survives every subsequent `SceneManager.LoadScene`.
- If a second `FadeBlackScreen` instance is ever created (e.g. accidentally placed in another scene): it detects `Instance` is already set and destroys itself, so you never get duplicates.

Because of this, **the very first scene the game boots into must contain the `FadeCanvas` GameObject**, or `FadeBlackScreen.Instance` will be `null` until some scene that has it loads.

## 3. Calling it from any script

No reference/prefab field needed anywhere else in the project — just call the static instance:

```csharp
// Fade to black, run when fully black
FadeBlackScreen.Instance.FadeIn(() =>
{
    // e.g. move player, close a menu, etc.
});

// Fade from black back to clear
FadeBlackScreen.Instance.FadeOut();

// Fade to black -> load a scene (async) -> fade back to clear automatically
FadeBlackScreen.Instance.FadeInThenLoadScene("StartScene");
```

Always null-check before calling if the script might run before any scene with `FadeCanvas` has loaded:

```csharp
if (FadeBlackScreen.Instance != null)
    FadeBlackScreen.Instance.FadeInThenLoadScene("StartScene");
else
    Debug.LogWarning("FadeBlackScreen.Instance not found — did the boot scene include FadeCanvas?");
```

`MainMenuManager.StartGame()` already uses this pattern as a reference example.

## 4. Quick checklist if fade doesn't appear

- [ ] `FadeCanvas` exists in the first scene that loads, with `Canvas`, `Canvas Group`, `Image`, and `FadeBlackScreen` all on the same GameObject.
- [ ] `Image` color alpha = 255 (opaque). Only `Canvas Group.alpha` should ever read as 0 in the Inspector while editing (that's expected — `Awake()` sets it to 0 at runtime too).
- [ ] Canvas `Sort Order` is higher than every other Canvas in the project.
- [ ] `FadeBlackScreen.Instance` isn't `null` when you call it (check Console for the `Debug.LogWarning` in `MainMenuManager`, or add your own).
- [ ] No error in Console about coroutines/inactive GameObject — means the `FadeCanvas` GameObject itself was inactive when instantiated/loaded.
