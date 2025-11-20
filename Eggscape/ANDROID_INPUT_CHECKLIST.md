# Android input review and adaptation guide

This project currently uses the legacy `UnityEngine.Input` API with keyboard/mouse bindings. The scripts below need mobile-friendly input (touch buttons or the new Input System) before an Android build will feel playable.

## Scripts that read desktop-only input

| Script | Current input usage | What to adapt for Android |
| --- | --- | --- |
| `Assets/Scripts/Player/Player.cs` | Jump via `Input.GetButton* ("Jump")`; horizontal via `Input.GetAxisRaw("Horizontal")`; attack via `Input.GetMouseButtonDown(0)`; fast-fall via `KeyCode.S`; cancels attack with `KeyCode.S`; victory prompt expects `Space`. | Add on-screen UI (left/right, jump, attack, fast-fall/down). Wire UI events to a mobile input bridge that sets the same actions (move, jump start/hold/release, attack, down). Optionally swap to the Input System and map both keyboard and touch controls. Provide a mobile-friendly continue button for victory instead of `Space`. |
| `Assets/Scripts/Managers/PauseMenu.cs` | Toggles pause on `Escape`. | Add a pause button in the HUD or remap to Android back button via `Input.GetKeyDown(KeyCode.Escape)` (works on Android) plus an on-screen control. |
| `Assets/Scripts/DialogueSystem.cs` | Advance text with `advanceKey` (Space by default) and skip with `skipKey` (Return). | Add UI buttons (e.g., "Próximo" and "Pular"). Hook their `onClick` to public methods that call the same logic as the keyboard path. |
| `Assets/Scripts/Managers/VNController.cs` | Advances VN dialog with mouse click or `Space`/`Return`. | Reuse the same UI buttons as `DialogueSystem` or add a tap-to-continue area that calls the advance method. |
| `Assets/Scripts/Managers/TutorialManager.cs` | Detects left-click to dismiss tutorial steps. | Add a touch/click overlay button to dismiss and call the same handler. |
| `Assets/Scripts/BossCutsceneManager.cs` | Starts/advances cutscene on left-click. | Reuse the dialog advance button/tap area. |
| `Assets/Scripts/Managers/GameManager.cs` | Cheats and debug toggles on `J`, `N`, `F3`, `F4`, `H`, restart on `R`, and victory continue on `Space`. | Keep cheats as editor-only or wrap in `#if UNITY_EDITOR`. Provide a mobile continue button for victory scenes. |
| `Assets/Scripts/Managers/TelegraphedStrikeSpawner.cs` | Spawns strike with `KeyCode.L` (debug). | Wrap in `#if UNITY_EDITOR` or expose a debug UI only in development builds. |
| `Assets/Scripts/Managers/ScytheSpawner.cs` | Spawns scythes with `KeyCode.K` (debug). | Same as above—editor-only or dev UI. |

## Minimal steps to get Android-ready controls
1. **Pick an input approach**
   - *Quickest*: Keep the legacy Input Manager for desktop and add a lightweight "MobileInputBridge" that exposes methods like `OnMove(float x)`, `OnJumpDown/Up`, `OnAttack()` called from UI buttons/joysticks. In scripts, read the bridge state when `Application.isMobilePlatform` is true, otherwise fall back to `Input`.
   - *Recommended*: Migrate to the Unity Input System asset already in the repo (`InputSystem_Actions.inputactions`). Add actions for Move (Vector2/float), Jump, Attack, Pause, DialogueAdvance. Use `OnScreenButton`/`OnScreenStick` to feed them on Android while keeping keyboard bindings for desktop.

2. **Add mobile UI prefabs**
   - Create a Canvas set to "Screen Space – Overlay". Add:
     - Left/right buttons or a virtual joystick bound to **Move** (x-axis).
     - Jump button bound to **Jump** (press/hold/release).
     - Attack button bound to **Attack**.
     - Down/Fast-fall button (maps to the S key behaviour) or treat as a vertical axis on the joystick.
     - Pause button (calls pause toggle) and an optional "Continue" button for victory prompts.
     - Dialogue/Tutorial/Visual-novel advance button that calls the same methods as mouse/keyboard.

3. **Expose public handlers**
   - In `Player`, add public methods invoked by UI: `SetMove(float value)`, `JumpPressed/JumpReleased`, `AttackPressed`, `FastFallPressed` to feed the internal state instead of raw `Input`. Gate existing `Input` reads behind `!Application.isMobilePlatform` or `if (!useMobileInput)`.
   - In dialog/cutscene scripts, add `Advance()`/`Skip()` public methods and call them from UI buttons.
   - For pause and victory continue, add `PauseToggleFromUI()` and `ContinueVictoryFromUI()` methods.

4. **Clean up debug keys for release**
   - Wrap cheat/debug shortcuts in `#if UNITY_EDITOR` or behind a development flag so they do not block mobile flow.

5. **Test on device or emulator**
   - In Build Settings, target Android and use **Build and Run** to deploy to a device/emulator. Verify each touch control triggers the expected action and that no keyboard prompts remain.

## Optional quality improvements
- Show mobile-only tutorials/tooltips for the touch layout.
- Replace on-screen text instructions that mention keyboard keys ("A/D", "Espaço") with touch equivalents when `Application.isMobilePlatform` is true.
- Use larger hit areas and 16:9-safe anchoring so buttons are reachable on phones.
- Consider vibration feedback on key actions using `Handheld.Vibrate()`.
