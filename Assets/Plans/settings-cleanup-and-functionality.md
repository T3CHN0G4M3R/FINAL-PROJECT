# Project Overview
- Game Title: Existing FINAL-PROJECT (display title not established; no branding changes).
- High-Level Concept: Existing first-person 3D exploration and object-interaction game. This targeted maintenance task removes unwanted settings and makes the remaining options affect actual gameplay consistently.
- Players: Existing single-player configuration; multiplayer is outside scope.
- Inspiration / Reference Games: Not supplied; preserve existing game design.
- Tone / Art Direction: Preserve the current SlimUI menu styling and gameplay UI.
- Target Platform: Windows desktop, StandaloneWindows64.
- Screen Orientation / Resolution: Landscape; existing menu canvases use 1920x1080 reference resolution.
- Render Pipeline: URP (PC_RPAsset), Unity 6000.3.8f1.

## Selected Approach and Scope
The user selected **both menus, shared logic**: apply the cleanup to the main menu and gameplay pause settings, using a shared settings model and application events. Compared with independently patching both managers, this requires a small shared layer but prevents persistence and behavior from drifting. Rebuilding the UI is not necessary.

Interpret “Video” as the whole Video tab, “Control Layout” as the dropdown row (not the whole Controls tab), and “Game Difficulty” as the difficulty row and Normal/Hardcore buttons. Keep existing hidden key-binding UI hidden; implementing rebinding is outside scope.

Apply scene changes to MainMenu and the three enabled gameplay scenes: TestScene, FarmScene, and Neighborhood Scene. Do not edit recovery scenes, imported demos, or disabled Scene2 without separately confirming scope. Shared script fixes may naturally benefit other users of those scripts.

# Game Mechanics
## Core Gameplay Loop
Preserve movement, exploration, doors, pickup/drop, and held-item use. Settings should provide reliable feedback and player autonomy without changing objectives or difficulty. Hiding prompts must not disable interactions.

## Controls and Input Methods
The project setting is **Both**, and its menu EventSystem uses InputSystemUIInputModule. Preserve existing PlayerInput/StarterAssetsInputs callbacks; do not introduce legacy input polling or change input backends.

Remaining settings:
- Music volume: 0–1, default 0.75; immediately changes designated music sources only. Menu SFX, footsteps, door sounds, and ambience remain unaffected.
- Show HUD: default on; controls gameplay crosshair and other verified HUD elements, never pause/menu navigation.
- Tooltips: default on; controls the existing InteractionPrompt help text. Effective prompt visibility requires HUD on, Tooltips on, and gameplay not paused.
- Horizontal/vertical sensitivity: existing 0–10 ranges and default 1; each affects only its corresponding look axis. Preserve zero as an intentional disabled-axis setting.
- Mouse smoothing: existing 0–1 range and default 0.5; zero bypasses smoothing, increasing values smooth mouse look. Use a documented bounded response-time mapping (e.g. 0–0.1 seconds) with frame-rate-independent filtering; clear residual motion when raw input stops and on pause/focus loss to avoid unintended drift.
- Invert mouse: default off; invert vertical look only.

No new difficulty, graphics, binding, language, or audio-category settings.

# UI
Preserve existing canvases, fonts, colors, animation, and world-space layout. Remove only requested widgets, their labels/decorations, and references; compact the remaining rows within their existing panel.

```text
SETTINGS                         GAME
[GAME]                           Music volume       [----o----]
[CONTROLS]                       Show HUD           [on/off]
                                 Tooltips           [on/off]
[RETURN]

SETTINGS                         CONTROLS
[GAME]                           Horizontal look    [----o----]
[CONTROLS]                       Vertical look      [----o----]
                                 Mouse smoothing    [----o----]
                                 Invert mouse       [on/off]
[RETURN]
```

Pause settings retain their existing layout and value readouts; provide the same smoothing setting there using the existing slider style if needed for parity. No hidden key-binding buttons are made visible. Ensure removed Video controls cannot be reached through keyboard/controller navigation.

# Key Asset & Context
## Existing assets to modify
- `Assets/Scenes/MainMenu.unity`: `Main_Menu/Canv_Options`; remove `BUTTONS/Btn_Video`, `PANELS/PanelVideo`, `Highlights/LineVideo`; remove difficulty buttons, NORMALINE/HARDCORELINE, and the specifically identified difficulty label under PanelGame; remove PanelControls/Dropdown and its Control Layout label. Labels often share the name `Text`, so identify by text and parent, not name alone.
- `Assets/Scenes/TestScene.unity`, `Assets/Scenes/FarmScene.unity`, `Assets/Scenes/Neighborhood Scene.unity`: scene-owned PauseMenu settings, difficulty/Video widgets, navigation and shared-settings bindings. Gameplay HUD uses `UICanvas/Crosshair` and `UICanvas/InteractionPrompt`.
- `Assets/Imported/SlimUI/Modern Menu 1/Scripts/Managers/UISettingsManager.cs`: currently initializes Video/difficulty references unconditionally, changes rendering quality at startup, and polls slider values in Update. Refactor the retained callbacks into a shared settings view; preserve signatures needed by surviving UnityEvents.
- `Assets/Imported/SlimUI/Modern Menu 1/Scripts/Managers/UIMenuManager.cs`: `DisablePanels()` unconditionally dereferences PanelVideo/lineVideo. Remove or safely retire Video navigation without breaking Game/Controls or existing hidden key-binding navigation.
- `Assets/Scripts/PauseMenuManager.cs`: retained callbacks and startup UI synchronization; remove difficulty/Video application. Currently ResumeGame enables crosshair regardless of ShowHUD; music changes the click/whoosh AudioSource; X sets RotationSpeed for both axes; Y only saves its value.
- `Assets/Scripts/InteractionsSystem.cs` (class `InteractionSystem`): `UpdateUI()` currently always writes help text. Respect HUD, Tooltips, and pause visibility while preserving interaction behavior.
- `Assets/Imported/SlimUI/Modern Menu 1/Scripts/Audio/CheckMusicVolume.cs`: menu Camera music AudioSource has no mixer group and is correctly distinct from SFX sources. Make its application react consistently to shared music changes and initialize saved volume.
- `Assets/Imported/Starter Assets/Runtime/FirstPersonController/Scripts/FirstPersonController.cs`: `CameraRotation()` currently uses RotationSpeed for both axes, checks inversion, but ignores YSensitivity and MouseSmoothing. Add a narrowly scoped public look-settings application method and separate axis/filter state. Preserve movement, animation, head tracking, audio, pitch clamps, and device timing.

## New assets
- `Assets/Scripts/Settings/GameSettings.cs`: shared clamped values, existing PlayerPrefs keys, defaults, setters, and change notification; no new global rendering mutations.
- `Assets/Scripts/Settings/GameplaySettingsApplier.cs`: scene component connecting the shared model to FirstPersonController and explicitly assigned gameplay HUD. Subscribe/unsubscribe safely and apply current values on initialization.
- Tests in a project-compatible test assembly if the installed test framework supports it; otherwise use temporary validation checks and documented manual tests without adding packages solely for this task.

## Architecture and verified context
- Keep keys `MusicVolume`, `ShowHUD`, `ToolTips`, `XSensitivity`, `YSensitivity`, `MouseSmoothing`, `Inverted` compatible with existing saves. Do not clear unrelated PlayerPrefs or delete old difficulty/video keys.
- Starter Assets lives in `Assets/Imported/Starter Assets/Runtime/Unity.StarterAssets.asmdef`; it cannot reference default Assembly-CSharp. Place shared settings and adapter in project scripts; inject primitive look settings into the controller through its public method, avoiding circular assembly references or asmdef changes.
- Imported scripts under Assets are editable project assets, not Package source. Do not modify files under Packages.
- Gameplay preview inspection found no looping gameplay music sources; do not invent music tracks or misclassify ambience/SFX. The gameplay music slider should persist the shared preference for the next menu or explicitly designated music source, and must not affect pause SFX.
- Main menu has one EventSystem with the new UI input module and GraphicRaycasters on the existing canvases.
- Preview inspection produced exceptions from preview scene processing, while the pre-inspection console contained no errors. Recheck actual scenes/runtime during implementation rather than treating preview exceptions as diagnosed gameplay bugs.

# Implementation Steps
1. **Recheck impact and exact targets.** Read this plan, inspect dependencies/dependents for every existing asset before editing, verify scene-owned versus prefab-owned UI, and capture current hierarchy/wiring. Inspect gameplay canvas/input configuration and any additional HUD elements. Dependency: approval of this plan.
2. **Implement shared retained settings.** Create GameSettings with initialization, clamping/invalid-value handling, existing-key persistence, and change events. Flush saves at settings close/scene transition and application shutdown rather than every drag event. Handle no-domain-reload runs without stale subscriptions. Dependency: Step 1.
3. **Implement independent look behavior.** Add `ApplyLookSettings(float horizontal, float vertical, float smoothing, bool invertVertical)` (or equivalent) to FirstPersonController. Remove its competing X-only preference initialization. Preserve mouse delta versus gamepad delta-time handling; apply mouse smoothing only to mouse input. Add GameplaySettingsApplier and wire each enabled gameplay scene. Dependency: Step 2.
4. **Connect UI managers.** Route retained main/pause callbacks through GameSettings; initialize sliders without notification, refresh labels/readouts on opening, and remove stale Update caching. Use onValueChanged for all sliders, including keyboard adjustments; eliminate duplicate Drag callbacks and duplicate runtime/persistent listeners. Retire Video/difficulty initialization/application and clean removed reference fields/events safely. Dependency: Steps 2–3.
5. **Apply HUD and tooltip rules.** Make pause/resume obey ShowHUD. Gate InteractionPrompt by the combined visibility rule while leaving doors, item use, and pickup logic unchanged. Never disable the entire UICanvas if it contains menus. Dependency: Steps 2–4.
6. **Fix music routing.** Connect CheckMusicVolume to current shared volume and changes with lifecycle cleanup. Stop PauseMenuManager from setting its SFX AudioSource.volume from music settings. Bind only actual music sources. Dependency: Steps 2–4.
7. **Remove requested UI incrementally.** First clear references and events, then remove Video tab/panel/highlight, difficulty widgets/labels/decorations, and main-menu Control Layout dropdown/label. Repeat relevant changes in each enabled gameplay scene. Compact rows and repair explicit navigation; add pause smoothing parity using existing styling. Do not rebuild panels or change imported reusable UI prefabs globally. Dependency: Steps 4–6.
8. **Validate and save.** Compile after each script change; inspect remaining UnityEvents and serialized references, then test all scenes and a Windows player. Save only intended scenes/assets. Dependency: Step 7.

# Verification & Testing
- No Video tab/panel, Control Layout row, or difficulty selector remains visible or navigable in either menu. Retained Game/Controls/Return, pause/resume, camera menu animation, and hidden bindings state still work.
- Clean/default preferences: values are 0.75 music, 1 X/Y, 0.5 smoothing, HUD/tooltips on, inversion off. Preserve existing valid preferences; clamp corrupted/out-of-range floats safely. Restore original preferences after tests; do not call DeleteAll.
- Every slider responds via pointer drag, track click, keyboard/controller navigation, and loading saved values without writing stale values or doubling callbacks.
- X-only adjustment changes yaw but not pitch; Y-only adjustment changes pitch but not yaw; inversion changes only pitch direction. Test each zero axis and maximum values.
- Compare smoothing 0 and 1 at 30/60/120 FPS. No residual look on pause/resume, stationary mouse, focus loss, or scene transition. Gamepad timing remains correct.
- HUD/tooltips truth table: HUD off hides crosshair and prompts; HUD on + Tooltips off preserves crosshair but hides prompts; both on restores normal prompt updates. Pause hides both; resume respects preferences. Interactions continue when prompts are hidden.
- Music volume 0 silences menu music but leaves hover/click/whoosh audible; 1 restores music. Gameplay slider must not alter pause clicks, footsteps, doors, or ambience. Returning to MainMenu applies the saved value immediately.
- Values agree between main and pause settings, survive scene changes and process restart, and changing them while paused applies on resume without moving the player.
- Removed Video settings must no longer change QualitySettings/shadow distance/mipmap limit at main-menu startup. Preserve authored graphics defaults; do not reset other user/system settings.
- Repeated opening/closing and scene reloads produce no duplicated event handlers, missing-reference exceptions, or settings-related console errors. Verify all three enabled gameplay scenes and Windows build, not only MainMenu.

# Maintenance Amendment: Visible Scene Fades and Reported Problems

## Project Overview
- Game Title, concept, players, art direction, and platform: preserve the project overview above; this is maintenance, not a redesign.
- Screen Orientation / Resolution: preserve existing landscape UI and support different window sizes.
- Render Pipeline: current URP PC_RPAsset, Unity 6000.3.8f1.
- Current scope: repair the intro transitions and the concrete console problems identified below, preserving previously implemented settings. Do not claim to fix every possible undiscovered project defect or the Editor/network infrastructure.
- Current build list differs from the earlier settings plan: MainMenu, Dog1–Dog8, Neighborhood Scene, and FarmScene are enabled; TestScene is disabled. Preserve TestScene as disabled per the current menu decision.

## Confirmed Context and User Decisions
- New Game currently invokes click audio and `NewGameSceneSequence.BeginNewGame` once; it no longer invokes SlimUI's loader.
- The sequence has a 3-second fully-visible Dog hold and a 1-second combined fade (0.5 seconds per phase), and forces the middle delay to zero.
- The imported Shader Graph is transparent, with alpha clipping disabled. Its presence and shader support do not prove a perceptible rendered fade; the exact runtime visibility failure remains unverified.
- The package manager persists, creates a high-order overlay Canvas, and always creates a material from the effect. Its supplied image is black but has Raycast Target disabled; the current sequence changes this at runtime.
- Continue and Load Game still target disabled `TestScene`. Console confirms failed loads followed by a null dereference of `operation.allowSceneActivation` in UIMenuManager lines 267–268.
- Console also confirms numerous negative-scale BoxCollider warnings in Neighborhood houses. Read-only prefab inspection finds 398 negative-scale boxes across the eight imported house prefabs; this is not an exact count of affected scene instances.
- User selected **Reliable UI fade**: keep Easy Transition's manager and provide a project-owned standard-UI alpha effect, avoiding changes to the imported Shader Graph or package source.
- User selected **Disable for now** for Continue and Load Game. Do not invent saved-game restoration or enable TestScene to conceal the stale targets.

## Game Mechanics
### Core Gameplay Loop
Preserve New Game → Dog1 → Dog2 → Dog3 → Dog4 → Dog5 → Dog6 → Dog7 → Dog8 → Neighborhood Scene. Each switch obscures the outgoing scene, loads under black, and reveals the incoming scene. Each Dog remains fully visible for the Inspector-configurable hold, including Dog8 before gameplay.
### Controls and Input Methods
Preserve the current Both backend and existing InputSystemUIInputModule/event-driven button callbacks. Block duplicate starts and UI interaction during a switch. Keep pause, movement, doors, held items, settings, and cursor behavior intact. No new polling-based controls.

## UI
```text
PLAY
[Continue — disabled]
[New Game — enabled]
[Load Game — disabled]
[Return]

Switch: outgoing image → gradual black → black/loading → gradual incoming image
```
Keep menu styling and click audio. Use readable disabled styling and exclude disabled buttons from navigation. A black, screen-filling Image sits above all Dog and gameplay UI only while transitioning; it disappears and stops blocking after completion or recovery.
Inspector defaults proposed: Dog Hold = 3 seconds; Fade Out = 1 second; Fade In = 1 second; Black Hold = 0.2 seconds. Loading time is additional. Preserve existing authored hold values rather than silently resetting them.

## Key Asset & Context
- Modify `Assets/Scripts/NewGameSceneSequence.cs`: accept a TransitionEffect-compatible project UI fade, expose independent fade phases and black hold, keep ordered scenes, strengthen initialization, cancellation and cleanup, and avoid broadly modifying unrelated child images.
- Create `Assets/Scripts/Transitions/UIAlphaFadeEffect.cs`: derive from `PixeLadder.EasyTransition.TransitionEffect`; animate the transition Image's black color alpha 0→1 and 1→0 with unscaled time, easing, and exact endpoints. Supply a valid material because SceneTransitioner creates a material instance even though this effect does not use `_Cutoff`.
- Create `Assets/Transitions/UIAlphaFade.asset`, `Assets/Transitions/UIFadeMaterial.mat`, and a project-owned transition Image prefab with standard UI rendering, full coverage, black color, and Raycast Target enabled. Assign these on the MainMenu manager instance; do not modify imported reusable assets globally.
- Modify `Assets/Scenes/MainMenu.unity`: wire the project fade, persist timing and valid references, disable Continue/Load Game and remove their stale scene-loading calls while keeping New Game audio and wiring.
- Modify `Assets/Imported/SlimUI/Modern Menu 1/Scripts/Managers/UIMenuManager.cs` narrowly: validate scene availability and UI references, reject overlapping loads, handle a null AsyncOperation, and recover safely without dereferencing it. Preserve public signatures and other menus. This file is an imported Assets script, not package source.
- Modify scene-owned collider overrides in `Assets/Scenes/Neighborhood Scene.unity`; inspect FarmScene for the same defect and fix confirmed affected instances there only. Preserve imported house prefabs and recovery scenes.
- Collider repairs may create small scene-owned positive-scale collider child objects or project-owned baked collision mesh assets if necessary; retain original renderer transforms, tags, layers, physics materials, trigger/enabled state, Rigidbody ownership, and interaction references. Inspect InteractionSystem/InteractiveDoor and other collider dependents before moving/removing any collider.
- Existing Easy Transition core, base effect, material, image prefab, imported house prefabs, build scene list, and surviving menu events are dependencies. Use the dependency tool if available; otherwise inspect AssetDatabase dependencies and serialized references before changes.

## Implementation Steps
1. **Capture and recheck state.** Inventory unique console errors/warnings without clearing evidence, inspect exact scene instances and prefab overrides, confirm manager/image initialization and actual object references (do not interpret serialized instanceID placeholders alone as missing references). Record collider world corners and interaction ownership. Dependency: approval of this revised plan.
2. **Implement the project UI alpha effect.** Create the effect/material/image assets and verify compatibility with Easy Transition's material creation and lifecycle. Use UI-compatible blending and no shadow/post-processing dependencies. Test transparent, partial-alpha, and fully-black states independently before scene switching. Dependency: Step 1.
3. **Repair the sequencer and configuration.** Wire the new effect, expose independent fade-out/in and middle timings, preserve 3-second Dog holds, resolve the persistent singleton safely, validate all destinations before starting, monitor cancellation while waiting, and ensure no stale running flag, material, overlay, or raycast block remains. Keep cleanup safe if the manager is destroyed or the player returns to MainMenu. Dependency: Step 2.
4. **Repair invalid menu loading.** Disable Continue/Load Game, remove their stale TestScene loads, retain their visuals and other menu behavior, and add narrow defensive checks to UIMenuManager. Do not redirect these buttons or enable TestScene. Dependency: Steps 1 and 3.
5. **Repair confirmed mirrored colliders incrementally.** For each affected scene-owned BoxCollider, retain visual mirroring while reconstructing the same collision volume under a positive-scale collider transform. Check transformed basis vectors for shear: use a box only when the same oriented volume is representable; otherwise use a suitable baked collision shape after checking Rigidbody constraints. Never fix by blindly taking absolute renderer scale, changing house geometry, deleting collision, or suppressing warnings. Preserve door/trigger/held-item ownership and tags; verify each representative type before applying equivalent repairs. Dependency: Step 1; scene and interaction impact inspection.
6. **Validate, save, and remove temporary diagnostics.** Save only intended assets/scenes and inspect build inclusion and references. Preserve the existing settings functionality and disabled TestScene. Dependency: Steps 2–5.

## Verification & Testing
- Confirm alpha changes produce visible darkening, a completely black midpoint, then visible brightening in the actual Game View; inspect runtime screenshots/video, not just IsTransitioning or shader support.
- Confirm exactly nine switches, Dog1–Dog8 in order then Neighborhood, eight fully-visible holds, and independent Inspector timing. Verify partial alpha around 0.5 and exact 0/1 endpoints at two window resolutions.
- Double-click New Game, interrupt/return to menu, repeat a new game, and simulate a missing scene/reference. No parallel sequence, stuck black overlay, orphan material, missing-reference exception, or permanently blocked UI.
- Continue and Load Game cannot invoke TestScene via mouse or navigation. An invalid direct call to UIMenuManager fails gracefully and leaves menus usable, without null AsyncOperation errors.
- Compare representative repaired collider world corners and collision behavior before/after. Walk walls/floors/stairs, open doors, test triggers and pickup/drop. Confirm no negative-scale BoxCollider warnings in affected enabled scenes; verify meshes and house placement look unchanged.
- Preserve settings persistence, HUD/tooltips, pause/resume, audio routing and independent look axes from the original plan.
- Previous automated test attempts repeatedly stalled during forced Editor domain reload and dropped the connection. Do not repeat the same failing reload cycle. Use manual Play Mode/Game View verification or a compatible standalone/test workflow; if automation cannot run, report that limitation and request manual evidence rather than claiming verification.
- Compile after script edits and test an available Windows development build without changing Unity-generated scripting symbols, package guards, render pipeline, or installing unrelated packages.

# Save and Autosave Amendment

This amendment adds the requested save system and supersedes the earlier temporary decision to disable Continue/Load Game. Preserve existing settings, intro fades, collision repairs, and unrelated gameplay. Only this new amendment is pending implementation; do not repeat completed maintenance work.

# Project Overview
- Game Title: Existing FINAL-PROJECT; no branding changes.
- High-Level Concept: Existing single-player first-person exploration/object-interaction game. Add durable local checkpoints so players can preserve exploration and return to their chosen progress through the pause and main menus.
- Players: Single player; no accounts, cloud storage, networking, or Unity Services.
- Inspiration / Reference Games: Not supplied; retain the existing game design.
- Tone / Art Direction: Existing SlimUI main menu and current gameplay pause styling, fonts, colors, and audio.
- Target Platform: Windows desktop, StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape UI; preserve reference scaling and test 1920x1080 plus a smaller window.
- Render Pipeline: Verified UniversalRenderPipelineAsset / PC_RPAsset, Unity 6000.3.8f1.

## Selected Approach and User Decisions
- **Three manual slots plus one autosave.** Save Game opens a three-slot chooser with overwrite confirmation. Load Game lists all three manual slots and the autosave. Continue loads the newest valid record across these four saves, with a deterministic tie-breaker.
- **Autosave on gameplay scene entry and Exit only.** No periodic autosave timer, interaction-triggered save, or saving in MainMenu/Dog intro scenes. This means progress since entry is not protected against a crash until a manual save or Exit; do not silently add a timer.
- **Player plus world state.** Restore current gameplay scene, player position/yaw/view pitch, held or dropped pickup items, flashlight on/off, and door states. Retain captured state of previously visited supported gameplay scenes within the same run.
- **New Game keeps manual saves.** Confirm replacement of an existing autosave, clear in-memory run state and autosave only after confirmation and destination validation, then replay Dog1–Dog8 → Neighborhood Scene. Cancel changes nothing. Do not delete settings or manual records.
- Compared approaches: one manual save is simpler but gives less checkpoint choice; three slots add a small UI cost and support player autonomy (SDT). Full-world restoration has more integration work than player-only checkpoints but avoids discontinuities in exploration dynamics (MDA). Use local versioned JSON with safe file replacement rather than storing complex world state in PlayerPrefs or adding a cloud backend.

# Game Mechanics
## Core Gameplay Loop
Explore and interact → pause → optionally save to a manual slot → resume or Exit to MainMenu → Play → Continue, Load Game, or New Game. Continue/Load skip the Dog intro and go directly to the saved supported gameplay scene. New Game preserves the authored intro and starts with fresh scene defaults, not an old in-memory snapshot.

Save boundaries are coherent snapshots, not serialization of arbitrary GameObjects. Supported scene state comprises explicitly registered pickups and doors plus the player and held-item relationship. Static architecture, particles, audio playback, arbitrary Rigidbody props, sky/weather time, animation playback position, random timers, and future unimplemented quest systems are not included. Do not advertise these as saved. If new persistent systems are added later, extend the schema deliberately.

## Controls and Input Methods
- Project Settings inspection verified activeInputHandler = 2 (Both); menus use InputSystemUIInputModule and existing PlayerInput/StarterAssetsInputs.
- New UI uses Button.onClick and existing EventSystem navigation; no new legacy polling, input backend changes, or save hotkeys.
- Saving and slot selection work at Time.timeScale = 0. Loading temporarily blocks gameplay input and duplicate commands. Preserve existing pause/Resume/settings behavior.
- InteractionSystem currently handles input even while paused; add a narrow early gate while paused or while restoring/transitioning so right-click/keys cannot mutate the snapshot behind the save UI. Do not redesign the interaction bindings.

# UI
Preserve existing styling; add only save/load/confirmation/status views. Existing main-menu application Exit still quits the application; gameplay pause Exit returns to MainMenu.

```text
PAUSE                         SAVE GAME
[Resume]                      [Slot 1 — Empty / Scene, date]
[Save Game]                    [Slot 2 — Empty / Scene, date]
[Settings]                    [Slot 3 — Empty / Scene, date]
[Exit]                        [Back]
Status: Game saved / Save failed

MAIN MENU → PLAY              LOAD GAME
[Continue]*                   [Autosave — Scene, date]
[New Game]                    [Slot 1 — Scene, date / Empty]
[Load Game]*                  [Slot 2 — Scene, date / Empty]
[Return]                      [Slot 3 — Scene, date / Empty]
                              [Back]

OVERWRITE SLOT?               START NEW GAME?
[Confirm] [Cancel]            Manual saves will be kept.
                              Existing autosave will be replaced.
                              [Confirm] [Cancel]
```

* Disable Continue and Load Game when there is no valid loadable record; exclude unavailable choices from navigation. Corrupt/incompatible/missing-scene entries display an unavailable reason instead of being loadable. Load Game selects a specific valid entry; no OS file picker. Save succeeds only after the disk commit; errors are visible and never show false success. While saving/loading, show status and disable conflicting actions. Save completion keeps the game paused. An Exit save failure keeps the player paused and offers Retry, Exit without saving, or Cancel; exiting without saving requires explicit selection. Use a distinct inline error view/confirmation, not an unexplained console-only warning.

# Key Asset & Context
## Verified existing context
- MainMenu Play buttons: `Main_Menu/Canv_Main/PLAY/VerticalLayout/Btn_Continue`, `Btn_NewGame`, `Btn_LoadGame`, `Btn_Return`. Continue/Load are non-interactable with click audio only; New Game calls `NewGameSceneSequence.BeginNewGame` once.
- Main menu navigation is `SlimUI.ModernMenu.UIMenuManager.PlayCampaign()` / `ReturnMenu()`. Keep animation and click audio; remove no surviving settings behavior.
- Both gameplay scenes have `UICanvas/PauseMenu/PauseUIContainer/MainPauseView` containing Resume/Settings/Exit, bound to PauseMenuManager. ExitToMainMenu currently saves settings only and loads MainMenu.
- Neighborhood has many InteractiveDoor instances and a scene Flashlight. Farm has a Flashlight; neither inspected scene has an attached RandomSceneChanger/DreamSceneChanger. Repeated house/object hierarchy names exist: runtime names, transform paths, instance IDs, and prefab GUIDs alone are not stable unique save identifiers.
- No game-save implementation was found in Assets/Scripts. GameSettings is settings persistence, not world persistence, and remains separate.
- FirstPersonController owns private `_cinemachineTargetPitch`, motion/filter state and head-follow logic. A transform-only teleport would be overwritten by camera updates. Its assembly is `Unity.StarterAssets`, so inject primitive pose values through a public API without referencing Assembly-CSharp save types from that assembly.
- Flashlight initializes state in Awake and again in Start. Restore only after startup initialization and provide a silent state application path so loading neither resets the light nor plays a toggle sound.
- InteractiveDoor.Awake derives open/closed rotations from authored transform; restore through a dedicated method after initialization, keeping logical open state and visual rotation consistent without toggling.
- InteractionSystem.PickUpObject/DropObject manage parenting, Rigidbody collision/kinematic state, colliders, callbacks, and holding animation. Reuse these semantics carefully; do not restore a held item through parenting alone. Capture original parent/physics/collider defaults before holding so restoration does not persist temporary disabled held physics as its normal world state.
- Enabled build scenes: MainMenu, Dog1–Dog8, Neighborhood Scene, FarmScene. TestScene remains disabled. Never load a record into TestScene or enable it to conceal bad data.
- Read-only AssetDatabase dependency/dependent checks performed for the target scenes and scripts. Shared controller/Flashlight/interaction scripts also affect TestScene or imported/sample prefabs; retain compatible defaults and leave those scene/prefab assets unchanged.

## Assets to modify
- `Assets/Scenes/MainMenu.unity`: bind save-aware Continue/Load/New Game flow, add load browser and New Game confirmation with existing style.
- `Assets/Scenes/Neighborhood Scene.unity`, `Assets/Scenes/FarmScene.unity`: add scene persistence adapter, explicit player/interaction references, scene-instance persistent IDs/registrations, Save Game button/slot chooser/status/failure UI.
- `Assets/Scripts/PauseMenuManager.cs`: Save Game view callbacks, route Exit through successful autosave before leaving, preserve pause/cursor/settings behavior and failure recovery.
- `Assets/Scripts/NewGameSceneSequence.cs`: integrate fresh-run start at the validated sequence boundary, without changing Dog timing/fades; register final gameplay entry only once.
- `Assets/Scripts/InteractionsSystem.cs`: snapshot-safe input gate and narrowly scoped held-item restoration/state capture support, preserving existing pickup/drop behavior.
- `Assets/Scripts/InteractiveDoor.cs`: explicit immediate state restore API.
- `Assets/Scripts/Flashlight.cs`: silent state restore API compatible with ordinary Toggle/SetState.
- `Assets/Imported/Starter Assets/Runtime/FirstPersonController/Scripts/FirstPersonController.cs`: primitive pose capture/apply API that restores yaw/pitch, safely teleports with CharacterController disabled temporarily, resets input/movement/look residuals and recomputes camera target without changing settings or general control architecture.
- Modify `Assets/Imported/SlimUI/Modern Menu 1/Scripts/Managers/UIMenuManager.cs` only if required for play-menu refresh/back navigation; prefer a project-owned menu controller rather than expanding imported loader responsibilities.

## Proposed new files
- `Assets/Scripts/Saving/SaveGameData.cs`: serializable versioned DTOs for metadata, current scene/player pose, per-scene pickup/door records and held-item ID.
- `Assets/Scripts/Saving/SaveFileRepository.cs`: four fixed logical save IDs mapped to local paths, validation, listing, atomic commit/recovery, explicit success/error results.
- `Assets/Scripts/Saving/GameSaveManager.cs`: single lifecycle-managed service, in-memory run snapshots, capture/load coordination, autosave triggers and operation guards; initialize idempotently for direct scene play and menu launch.
- `Assets/Scripts/Saving/GameplaySaveAdapter.cs`: Inspector-wired scene registration, initialization readiness, capture and ordered restoration.
- `Assets/Scripts/Saving/PersistentSaveId.cs`: immutable serialized per-scene-instance identifiers; IDs are assigned and validated in the Editor, never regenerated each run. Include scene identity in lookup keys.
- `Assets/Scripts/Saving/SaveMenuController.cs`: main-menu validity refresh, Continue, load-browser selection, New Game confirmation and load status.
- `Assets/Scripts/Saving/SaveSlotPanel.cs`: reusable scene-bound slot list and confirmation/status behavior for pause/manual saves and main-menu loads.
- `Assets/Editor/Saving/SaveIdValidator.cs`: restricted tooling to populate missing IDs for supported scene registrations, detect duplicate copied IDs, and validate scenes without editing imported prefabs globally. Verify impact before any Editor assignment.
- Optional tests under `Assets/Tests/Saving/` in project-compatible assemblies; do not add packages solely for this feature.
- Runtime files only under `Application.persistentDataPath/Saves/`; never write player records into Assets, plans, or the project repository.

# Implementation Steps
1. **Recheck exact wiring and impact before edits.** Confirm dependencies/dependents, prefab ownership, current script state, player camera/controller/hand references and all saveable tagged pickup roots (including tagged objects without InteractableObject). Check installed test support. Preserve loaded-scene setup. Dependency: approval of this amendment.
2. **Implement schema/repository.** Use versioned local JSON with validated required fields, finite transforms, bounded counts, scene allowlist, unique object keys and valid held references. Metadata includes UTC timestamp, monotonic commit order for tie resolution, slot kind, scene display name, run ID and schema version. Load full records when deciding validity, not only metadata. Unknown schema versions fail safely. Write a temp file in the same directory, flush/close, validate, and atomically replace the target with a backup using supported Windows/.NET APIs. Never delete the good target first; on failure retain existing data and remove/quarantine failed temps. Recovery can promote a validated backup only with clear status. Restrict path mapping to fixed slot IDs; no arbitrary paths from JSON. Dependency: Step 1.
3. **Implement scene registration and narrow state APIs.** Create stable scene-instance IDs for doors/pickups, capture their authored parent/scale and physics/collider defaults, and validate duplicate IDs (especially repeated houses). Add controller pose, door apply, silent flashlight apply, and safe held-state methods. Keep imported prefabs unchanged; scene registrations/ID overrides are local to Neighborhood/Farm. Dependency: Steps 1–2.
4. **Implement capture/restore orchestration.** Capture a coherent immutable DTO on the main thread before disk work; any worker thread may handle plain data/IO only. Block overlapping save/load/exit/new-run operations. For load, validate the whole record and target availability before switching, disable input/suppress autosave, wait until scene Start/registration is ready, restore visited scene state, doors and world items first, player pose second, held relationship third, flashlight state last. Restore unheld parent/physics accurately and zero unwanted motion. Publish ready state only after success, then re-enable gameplay/cursor. Missing or duplicate required IDs fail safely with visible recovery, not partial silent success. Dependency: Steps 2–3.
5. **Implement run and autosave boundaries.** Autosave once after successful fresh gameplay entry or ordinary supported gameplay entry, after restoration/initialization readiness, never from MainMenu/Dog scenes. Do not overwrite the newest save immediately just because Continue/Load reconstructs a scene; restoration suppresses entry autosave. Maintain per-scene visited snapshots on supported scene changes; verify outgoing capture occurs before destruction through a project-owned transition boundary where applicable. No scene-changer script is currently attached, so do not invent new travel mechanics. Autosave on Exit captures while paused and commits before leaving; on failure expose Retry/Exit without saving/Cancel. New Game validates its intro destinations, confirms autosave reset if necessary, keeps manual records, clears old run snapshots and replays the existing sequence. Reset persistent state/subscriptions safely across no-domain-reload runs and repeated menu visits. Dependency: Step 4.
6. **Wire pause UI in both gameplay scenes.** Add Save Game styled from existing controls, three manual slots with labels/overwrite confirmation, unscaled status feedback and error recovery. Save stays paused; Back returns to pause, not gameplay. Route existing Exit once through the coordinator, preserving GameSettings.Save, normal time scale/cursor/audio cleanup and MainMenu destination after commit. Restore the prior paused UI state if transition fails. Dependency: Steps 4–5.
7. **Wire MainMenu Play options.** Refresh slot validity when Play/load browser opens and after filesystem changes known to the service. Continue loads newest valid manual/autosave; Load Game shows all four entries and loads the chosen valid record. Replace direct New Game callback with the project-owned confirmation flow while keeping exactly one call to BeginNewGame and existing click audio. Keep application Quit distinct from pause Exit, preserve Return/navigation and intro fades; prevent duplicate clicks/parallel loaders. Dependency: Steps 4–6.
8. **Validate incrementally and save intended assets only.** Compile scripts after each stage, inspect event targets and stable IDs, run repository/state tests and manual end-to-end tests in both gameplay scenes and a Windows development build. Remove temporary diagnostics; record any verification limitation honestly. Dependency: Steps 2–7.

# Verification & Testing
- No records: Continue/Load disabled; New Game starts the existing intro. No autosave files are created for MainMenu or Dog scenes. One entry autosave is created only when Neighborhood is fully initialized.
- Save each manual slot while paused, overwrite only after confirmation, cancel without changing files, and verify slot/date/scene labels and success feedback after commit. Saving never resumes gameplay.
- Move/rotate/view up/down, open/close representative mirrored doors, pick up/use/drop the flashlight and other registered pickup roots. Exit → MainMenu → Play → Continue reproduces saved position/yaw/pitch, item location/held state, flashlight and door logical/visual state. Repeat after application restart.
- Load each manual slot and autosave separately; Continue picks newest valid save deterministically and skips newer invalid ones. Empty/corrupt/incompatible/missing-scene entries cannot invoke a scene load. Test timestamp ties/clock changes with commit-order tie handling.
- Saving mid-door animation produces a documented consistent state (restore snapped to the saved logical target, not an incorrect interpolated angle). Holding physics/animation and flashlight are correct immediately after restore and after a subsequent drop/pickup; no startup flashlight reset or restore toggle audio.
- New Game cancel preserves everything. Confirm retains all manual files and settings, clears autosave and old in-memory run state, replays all Dog scenes/fades, then starts authored defaults and creates a fresh autosave. Loading an old manual run afterward restores that run, not the new run's snapshots.
- Farm direct play supports manual/entry/exit saves. If ordinary supported scene travel is wired later, test revisiting both scenes without losing previously captured doors/items; transitions must capture outgoing state before unload.
- Validate IDs across duplicate house names and copied scene objects. Reparenting a held item or renaming a registered object does not change its ID. Duplicated identifiers, missing required objects, or invalid held references are detected before partial restoration.
- IO tests: missing directory, read-only path/access denied, interrupted temp write, replacement failure, truncated JSON and validated backup recovery. Good save survives failed commits; no false saved message. Exit failures preserve paused gameplay and permit retry/cancel or explicit exit without saving.
- Double-click Save/Continue/Load/Exit/New Game, repeatedly return to menu, start directly in gameplay, and test no-domain-reload if supported. No duplicate service, listener, entry autosave or parallel scene operation; Time.timeScale, cursor, UI input and pause static state are correct.
- Save UI navigation works by pointer and existing keyboard/controller EventSystem at two resolutions. Right-click/E/F while paused/restoring cannot change held items/doors behind the panels. Preserve settings, audio categories, HUD/tooltips, look axes/smoothing and normal interactions.
- Do not repeat the previously stalled forced-domain-reload automated test loop. Use available compatible tests/manual Play Mode and Windows build; distinguish actual verified results from planned checks. No package-source changes, scripting-symbol workarounds, new services, or build-list changes.

## Recovery and Completion — After Connection Loss

### Confirmed implementation state
- All save-system scripts and scene wiring above now exist. Do not recreate them or repeat completed implementation steps.
- Reinspection confirms MainMenu has one SaveSlotPanel and correctly bound ContinueGame/NewGame/OpenLoadGame events. Neighborhood has one adapter, one panel, and 443 unique persistent IDs. Farm has one adapter, one panel, and one unique persistent ID. Save Game and Exit callbacks are bound in both gameplay scenes.
- Earlier runtime tests passed all three manual commits, entry/Exit autosaves, selected-slot loading, player position and camera pitch, held flashlight/off state, saved-ID door state, Continue choosing the newest autosave, Farm manual saving, JSON validation, atomic overwrite/backup, corrupt-record rejection and backup recovery. The latest test repeated these successes and verified New Game cancellation keeps both manual saves and autosave.
- The latest combined test timed out at stage 11 (75-second total budget). Current disk inspection shows a new-run Neighborhood autosave with run ID `cf3d73efa0ba4e78a2314ac66e9eb187`, while manual slots retain old run ID `0b61c62d040c435caf97a24b2322f9d5`. This proves fresh gameplay entry/autosave occurred, not that the entire final fade/sequence cleanup assertion completed before timeout. Do not label the overall test as passing.
- Current Console contains a connection-reset Curl error, not a diagnosed gameplay/compiler error.

### Remaining UI issue and evidence
- The main-menu load browser's Back caption remained too dark in the final runtime screenshot despite SaveSlotPanel setting its color to white.
- Confirmed cause: the cloned caption has ThemedUIElement, whose ThemedUI base calls OnSkinUI every Update. The subclass rewrites TextMeshPro.color from shared themeController.textColor. A one-time white assignment is not a reliable fix.
- Fix only project scene-owned save-dialog text styling: prevent theme scripts on those newly cloned captions from competing with their explicit readable color (disable the caption's ThemedUIElement/isText behavior locally, or replace that caption with a properly configured UI text child after checking references). Preserve original menu buttons and the shared imported theme scripts/assets. Keep the already-corrected world-space font sizing and pause button layout.

### Remaining implementation steps
1. **Safely recover test state before any script import/edit.** Dependency: approval of this completion amendment. Re-read `SaveSystemVerification.cs`, its SessionState keys, and current files. The state currently remains Done; the temporary script can schedule RestoreFiles on a subsequent domain reload, which deletes the active Saves directory. Prevent this pending cleanup first by clearing only the test-state trigger and preserving a copy of its recorded results/paths. Do not trigger a project-wide refresh while the stale cleanup is armed.
2. **Preserve save files before cleanup.** Dependency: Step 1. Current `Saves.verification-backup` does not exist. Never assume this permits deletion of the active Saves directory. Quarantine/copy current test-run records to a timestamped directory outside Assets, verify copies, and retain any records not conclusively attributable to the recorded test run IDs. Restore an original backup only if one is actually found and validated. Do not invoke the temporary test's destructive RestoreFiles blindly.
3. **Fix dialog caption ownership locally.** Dependency: Step 1 and impact inspection. In MainMenu's new SaveSlotPanel, inspect caption components and UnityEvents, disable only competing scene-owned theme behavior, set readable white text, and verify Back/Confirm/Cancel and slot labels at runtime. Do not rebuild the panel, alter original menu styling, or modify package/shared imported source.
4. **Finish focused verification without repeating the long combined test.** Dependency: Steps 2–3. Use a narrowly scoped dialog rendering check and a separate New Game completion check whose budget accounts for eight authored 3-second holds, nine 2.2-second fades/black holds, and actual scene loading. Log current scene, sequencer IsRunning and transition IsTransitioning when assessing progress. Protect original save files in an isolated test directory. Prefer manual verification if automation risks another disconnect; do not keep extending/repeating the previous full save/load/intro test or request forced domain reloads.
5. **Remove temporary diagnostics and finalize.** Dependency: Steps 1–4. After preserving evidence and safe file recovery, remove `Assets/Editor/SaveSystemVerification.cs`, the five `Assets/SaveVerification_*.png` screenshots and their metas, and only associated test SessionState keys. Keep `Assets/Editor/Saving/SaveIdValidator.cs`. Save only intended project scenes/assets; verify no stale automatic cleanup remains. Check Console/compiler status and report Windows standalone and smaller-resolution testing as unverified unless actually performed.

### Completion verification criteria
- Back, Confirm, Cancel and slot labels are visibly readable; pointer and keyboard navigation cannot reach commands behind the modal.
- New Game cancellation changes no records; confirmation keeps manual slots, reaches fresh Neighborhood gameplay and leaves no sequencer/transition/input overlay running after the final fade.
- Existing save/load tests and ID counts remain intact; no periodic autosave has been added.
- Existing user saves are preserved or restored; test-generated records are isolated rather than indiscriminately deleted.
- No temporary verification script, screenshots, stale Done trigger or destructive deferred cleanup remains. Final report distinguishes successful core tests, the prior timeout, and any outstanding manual/build checks.

# Siren Head Main Enemy Amendment

Only this enemy amendment is pending approval for the current request. Preserve completed settings, fades, save/load, and collider work; do not repeat earlier maintenance.

# Project Overview
- Game Title: Existing FINAL-PROJECT; no branding change.
- High-Level Concept: Single-player first-person neighborhood exploration with a roaming Siren Head threat. Break line of sight to escape, avoid its last-seen search area, and retry from the level-entry checkpoint after capture.
- Players: Single player.
- Inspiration / Reference Games: Siren Head is the requested creature reference; no additional reference game supplied.
- Tone / Art Direction: Preserve existing horror neighborhood and imported creature appearance; use Skin_1 at imported size as the proposed default.
- Target Platform: Windows desktop / StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape configuration; verify 1920x1080 and smaller window.
- Render Pipeline: URP PC_RPAsset; Unity 6000.3.8f1.

## Selected Scope and Approach
User selected **first-person capture**, **scene-entry checkpoint retry**, **random street waypoints**, and **Neighborhood outdoors only**. Add one enemy to Neighborhood, replacing the unused `Enemey` capsule placeholder after verifying references. Do not add enemies to Farm, MainMenu, Dog scenes, imported demo, or recovery scenes. Creature cannot enter houses, open doors, jump, or teleport through obstacles. Outdoor players can be pursued across explicitly included accessible outdoor ground; patrol destinations remain street-based.

Two viable navigation designs were considered: random authored street waypoints on a baked NavMesh (selected) versus an ordered fixed route. Random points produce less predictable horror encounters (MDA tension); fixed routes are easier to learn and test but more predictable. Use a small explicit finite-state machine rather than adding a behavior-tree dependency: this request has few states, and Inspector debugging is clearer. Keep agent-driven movement and compatible supplied Generic animations; do not regenerate or rerig this existing humanoid-shaped biped, or convert its working rig to Humanoid unnecessarily.

# Game Mechanics
## Core Gameplay Loop
Explore → avoid or encounter enemy → flee and break sight → enemy investigates last visible position and searches nearby → evade until search ends → enemy resumes street wandering. Capture locks the encounter, plays the supplied Death clip on Siren Head, then reloads the same gameplay scene and restores its entry snapshot. Fair, bounded detection supports player competence (SDT) without omniscient chasing.

### Explicit States
- **Roam:** Walk toward randomly chosen reachable street waypoints, avoiding immediate repeats when alternatives exist. Detect visible player within range and field of view; proximity alone does not reveal a player through walls.
- **Chase:** Run and periodically repath toward the player only while sight is confirmed. Update last-known position only on positive sight checks. Reacquiring sight from any non-capture state immediately resumes Chase.
- **Investigate:** On sustained sight loss, navigate to the recorded last-seen location at walking speed. Never continue tracking an unseen moving player. Bound travel by an Inspector investigation timeout; if the point is unreachable, approach a safe reachable outdoor point without crossing walls and proceed to search.
- **Search:** Walk between valid local NavMesh points around that fixed last-seen position for `searchDuration` seconds (the requested x). This timer starts when investigation reaches its destination or times out, so local search actually lasts x. End → Roam. Zero search duration skips local search.
- **Capture:** One-shot terminal encounter state, stopping navigation/perception and disabling player movement/look/interactions/pause/save/load commands until restart or controlled failure recovery.

### Perception and Capture
Inspector-assigned player root, player sight targets, eye transform, obstacle mask, and navigation area mask. Use range + horizontal FOV + physics occlusion to player head/torso; triggers and enemy/self/held-item colliders must not spuriously obstruct vision. Walls, fences and closed solid doors occlude; verify masks against actual scene layers. Small adjustable sight-loss grace prevents edge flicker, with no hidden target-position updates during loss.

Capture requires distance to the actual player body/collider, appropriate vertical separation, and unobstructed close-range contact/line of sight. Never kill across a wall or floor, and do not rely on an oversized creature collider alone. Adjustable capture distance must be consistent with the agent stopping distance and physical clearance.

### Inspector Configuration
Expose grouped tooltips and safe value validation:
- Walking speed (proposed 2 units/s), running speed (6 units/s), acceleration (12 units/s²), angular speed (180 degrees/s), arrival tolerance, agent radius/height/base offset and allowed areas.
- Sight range (30 units), FOV (100 degrees), perception interval (0.1 s), chase repath interval (0.2 s), sight-loss grace (0.5 s), sight origin/targets and occlusion mask.
- Search duration x (15 s), search radius (12 units), investigation timeout (15 s), waypoint dwell (0 s), local dwell (1 s), destination sampling distance/attempt budget, stuck timeout (3 s) and retry delay (0.5 s).
- Capture distance (proposed 1.5 units; tune from physical dimensions), vertical tolerance, Death playback multiplier (1), blend time (0.15 s), post-animation restart delay (0.5 s), capture camera framing/offset and camera blend duration.
- Locomotion playback multipliers/reference speeds and debug gizmo toggle. Walk/Run are the two locomotion roles; supplied Idle may be used only for stopped/dwell states, plus the requested one-shot Death capture action.
- Runtime read-only state, last-seen marker, remaining search time and path status for diagnosis.
All proposed values are initial tuning, not guaranteed final balance. Timers are scaled gameplay time and freeze during ordinary pause; capture cannot be paused. Finish detection uses animation completion, not a fixed 3.5-second wait that ignores playback speed.

## Controls and Input Methods
Verified Both backend (activeInputHandler = 2), PlayerInput / StarterAssetsInputs, FirstPersonController, and InputSystemUIInputModule. No enemy input component or new controls. Preserve existing bindings. Capture uses explicit gameplay gates and the existing save adapter input-block semantics; restore input ownership safely on errors, not unconditional enable. Do not disable the entire Player GameObject or Animator.

# UI
No new gameplay HUD or enemy state label. Preserve existing menus and settings. Hide crosshair/prompts during capture with correct ownership; restore according to settings on failure. First-person camera framing keeps the existing player perspective while visibly showing Siren Head's supplied Death action. This is not a paired player-body death animation.

```text
Gameplay: existing view + HUD
Capture: locked first-person encounter view, no prompts/pause/save
Death clip completed → optional Inspector delay → same level / entry checkpoint
Inspector: [Movement] [Sight] [Patrol/Search] [Capture] [Runtime State] [Gizmos]
```

# Key Asset & Context
## Verified State
- AI Navigation 2.0.10 is installed; no NavMeshSurface, Agent, Obstacle, Link or Modifier was detected in the loaded Neighborhood, and NavMesh triangulation has zero triangles. Existing sole agent type is Humanoid, radius 0.5 / height 2: not automatically appropriate for the creature.
- Skin_1/2/3 prefabs have valid Generic Avatars and a usable biped skeleton. Skin_1 renderer bounds are approximately 4.1 units tall; determine upright animated body width, feet, pivot and forward direction before fitting clearance (T-pose arm width is not the navigation radius).
- Walking, Running, idle and Death exact non-preview subclips are named `Take 001`; all 81 bound transform paths match Skin_1. Walk = 1.375 s, Run = 0.792 s, Death = 3.5 s. All currently report non-looping; no animation events. Death animates the creature, not the current Humanoid player.
- Player uses Man_MeshAvatar and StarterAssetsThirdPerson controller; no death state observed. FirstPersonController follows its animated head. Preserve player controller/Animator rather than forcing a mismatched Generic creature clip onto it.
- `Enemey` is a childless untagged mesh/capsule at approximately (17.98, 0.94, 8.96), with no AI script. Verify its serialized references before replacement; use a safely sampled street spawn away from the player start.
- AssetDatabase dependency/dependent inspection confirms original imported Siren assets are shared by all three skins and the demo scene. Create project-owned controller/clip copies and prefab wrapper instead of changing these shared assets globally.
- `GameplaySaveAdapter.Start` publishes readiness after startup; `Capture`, `Restore` and `BlockInput(bool)` exist. `GameSaveManager.OnSceneReady` currently restores `_visited[scene]` on ordinary entry and autosaves. Blind scene reload would restore a recent save/death state, not the requested entry checkpoint.
- Save manager exposes Busy, Save, Load and TravelToGameplay. Current schema has no enemy state; do not register creature as a pickup/door or change existing persistent IDs/counts. Enemy resets to authored spawn/Roam on entry/retry/load; exact AI state persistence is outside scope.
- Temporary `Assets/Editor/SaveSystemVerification.cs` is absent. Current console contains a connection-reset Curl error, not an identified gameplay/compiler failure.

## New Project-Owned Assets
- `Assets/Characters/SirenHead/SirenHeadEnemy.prefab`: neutral-scale navigation root, supplied Skin_1 visual child with matching Generic Avatar, Animator, fitted agent, sight/capture anchors, and AI script. No conflicting CharacterController or dynamic Rigidbody driving navigation.
- `Assets/Characters/SirenHead/SirenHeadEnemy.controller` and `Animations/{Walk,Run,Idle,Death}.anim`: validated copies of exact supplied clips. Loop locomotion/idle only; Death one-shot. Disable root motion so the NavMeshAgent exclusively drives locomotion.
- `Assets/Scripts/Enemies/SirenHeadAI.cs`: bounded finite-state machine, perception, waypoint selection, local search, agent/Animator coupling, and gizmos.
- `Assets/Scripts/Enemies/PlayerCaptureSequence.cs`: one-shot capture ownership, existing camera framing, animation completion observation, retry coordination and failure cleanup.
- `Assets/Navigation/NeighborhoodSirenHeadNavMesh.asset`: editor-baked outdoor navigation; scene-owned surface/modifiers and waypoint group. Dedicated creature agent settings in `ProjectSettings/NavMeshAreas.asset` if required after sizing.
- Optional compatible tests under `Assets/Tests/Enemies/`; no extra package installation.

## Existing Files/Items To Modify
- `Assets/Scenes/Neighborhood Scene.unity`: one enemy, explicit player/save/pause/camera references, street points, outdoor navigation boundaries and fitted safe spawn; disable/remove placeholder only after reference checks.
- `Assets/Scripts/Saving/GameSaveManager.cs`: immutable per-current-scene entry snapshot and explicit retry coordinator. Add narrow encounter/transition operation guards; preserve manual saves and existing load/new-run/Exit behavior.
- `Assets/Scripts/Saving/GameplaySaveAdapter.cs`: expose readiness/block state and entry-retry integration only if necessary; reuse coherent capture/restore semantics without changing ID registration.
- `Assets/Scripts/PauseMenuManager.cs`: narrow capture gate on pause/resume/save/exit commands; scenes without capture behave unchanged.
- `Assets/Scripts/InteractionsSystem.cs` and `Assets/Scripts/Settings/GameplaySettingsApplier.cs` only if needed for distinct capture/input/HUD ownership, after reading current files and impact checks. Prefer encounter-owned adapters over broad controller changes.
- No package source edits, Humanoid conversions, global material changes, build-list changes or new input symbols.

# Implementation Steps
1. **Recheck targets and impact.** Read this amendment, inspect dependencies/dependents of every existing edit target (AssetDatabase fallback when dedicated tool unavailable), exact road/outdoor geometry/collision, placeholder references, current save/pause/HUD/input/camera wiring and scene readiness. Preserve scene setup. Dependency: approval.
2. **Validate visual/animation setup first.** Create project-owned clip/controller/prefab assets from the already-rigged source. Verify each clip actually animates at runtime, Walk/Run/Idle looping, Death completion and no root drift. Fit feet/pivot/forward/body clearance using upright animated bounds. Keep supplied Generic rig. Dependency: Step 1.
3. **Bake outdoor navigation.** Create fitted creature agent type if needed, outdoor-only surface collection/masks/modifiers including roads and accessible outdoor ground while excluding roofs/interiors/terrain shortcuts across fences. Include physical blockers; exclude player/enemy/dynamic held items from bake. Generate no automatic jump links. Validate connected street paths and adequate body/head clearance; do not shrink height just to fit doors. Static street obstacles must affect paths; add local scene-owned navigation obstacles only for confirmed dynamic outdoor blockers, never agent+obstacle on same enemy. Dependency: Steps 1–2.
4. **Implement AI and couple locomotion.** Add explicit transitions, range/FOV/LOS perception, last-seen memory, random reachable street patrols, bounded investigation/local search, path/stuck recovery and parameter validation. Use complete paths for roaming/search, bounded projection for inaccessible player targets, and check enabled/on-NavMesh/pathPending before navigation APIs. Actual velocity selects idle/moving presentation; state selects Walk versus Run. Frame-rate-independent timers and bounded sampling prevent spin loops. Dependency: Steps 2–3.
5. **Implement entry checkpoint retry.** Capture a deep immutable entry snapshot after normal fresh-entry or save restoration has fully completed, before enemy activation; on loading a saved game that restored state becomes this scene session's entry checkpoint. Preserve across death reload, do not replace it with the death snapshot. Explicit same-scene retry path validates checkpoint/destination, holds Busy/encounter ownership, bypasses ordinary `_visited` restoration and suppresses retry-entry autosave. Replace current scene's in-memory visited state with a deep copy of the retry checkpoint; preserve other visited scenes, run ID, manual files, existing autosave and settings. Ordinary entries retain prior autosave policy. Clear/rebase entry checkpoint on intentional load/new run/scene change, not on retry. Missing checkpoint/failed restart must report a clear error and restore controllable state rather than overwrite saves or silently load a different scene. Dependency: Step 1; save API impact review.
6. **Implement first-person capture.** Win a single encounter guard only when gameplay ready/not paused/not restoring/transitioning. Stop agent and enter Capture, clear movement/look inputs, block controller/PlayerInput/interactions/pause/save/load and hide gameplay prompts. Preserve active player Animator and frame the supplied creature action from the player perspective; existing camera must not overwrite capture framing. Disable/freeze only necessary camera-follow ownership temporarily, restoring previous values on abort. Observe confirmed Death state entry and normalized completion including Inspector speed; use bounded diagnostic timeout only as failure recovery, never pretend a missing clip played. After completion/delay call entry retry once. No separate player-body death clip is promised. Dependency: Steps 4–5.
7. **Wire Neighborhood and tune.** Place one canonical saved enemy instance on the connected streets, bind player/body sight targets and capture coordinator, waypoint group, masks, Animator and save readiness. Replace placeholder safely, verify safe initial separation and no body clipping through fences/cars. Freeze AI/perception/timers on pause, save restoration and transitions; activate after checkpoint registration. Save intended assets only. Dependency: Steps 3–6.
8. **Verify incrementally.** Compile after each script stage, run focused AI/state/checkpoint tests plus actual Game View animation/capture checks, then Windows development build when available. Avoid prior stalled forced-domain-reload/long combined verification workflow. Isolate test save directory and never delete user saves. Report actual evidence and limitations. Dependency: Steps 2–7.

# Verification & Testing
- Exactly one visible Siren Head in Neighborhood; Skin_1 material renders in URP, correct feet height/forward axis/scale, no leftover active capsule enemy. Other scenes/imported demos unchanged.
- Surface and agent type match; nonempty baked NavMesh, valid street paths around houses/cars/fences; no roofs/interiors or fence-crossing shortcuts. No agent/Rigidbody/root-motion conflicts or invalid destination API errors.
- Far/out-of-FOV player → walking roam. Visible nearby player → Run/Chase. Player behind a wall even nearby → no detection/capture. Test sight at head/torso and held flashlight, triggers, boundaries and vertical separation.
- Break sight at a corner → last-seen position stays fixed while player moves unseen; bounded Investigate then local Search for x; expiry returns to roaming. Reacquisition from Roam/Investigate/Search immediately chases. Verify x = 0, short and long values; pause freezes elapsed search time.
- Walk/Run visibly loop over multiple cycles; animation transitions match actual movement and chase state. Death plays once and is not substituted for player animation. Test playback multipliers 0.5/1/2: reload occurs after real animation completion plus configured delay.
- Unreachable waypoint, blocked path, off-NavMesh target, empty waypoint array and stuck movement recover safely without teleporting, omniscience, infinite sampling or repeated console spam.
- Capture cannot happen through walls, floor or outside allowed range. Duplicate contact/perception events cause one capture and one reload. Escape/pause/save/load cannot interrupt capture or save the death pose. Failure path recovers input/HUD/camera with settings respected.
- Entry checkpoint test: enter Neighborhood, then move, open doors and pick up/drop/toggle flashlight, manually save elsewhere, get caught. Retry restores entry player pose, view pitch, entry door/item/held/light state, not death location or newest manual-save position. Other visited state and all manual/autosave files remain intact; no retry autosave overwrite.
- Load an existing save → wait for readiness → its restored state becomes entry checkpoint for subsequent death. Repeat deaths, intentional loads, menu/new-run visits and direct Neighborhood play without stale checkpoint, service/listener duplication or input/time-scale/cursor corruption.
- Existing Continue, three manual slots, entry/Exit autosaves, pause, settings, independent look axes, HUD/tooltips, doors and flashlight remain functional; persistent IDs/counts unchanged. Farm works unchanged without enemy.
- Inspect runtime state/gizmos and actual Game View, not merely asset existence or Animator parameter values. Test 30/60/120 FPS and two window sizes when possible; do not claim standalone/manual checks passed without running them.

# Player Death, Retry Fades and Proximity Chase Amendment

This amendment supersedes only the earlier creature-death playback, death camera behavior, unfaded retry, and sight-only chase trigger. The enemy, outdoor NavMesh, street waypoints, save-safe scene-entry retry and prior maintenance already exist; do not recreate them.

# Project Overview
- Game Title: Existing FINAL-PROJECT; no branding changes.
- High-Level Concept: Single-player first-person horror exploration. Siren Head detects visible players or nearby unobstructed players; capture causes the player to collapse, followed by a fade-out, entry-checkpoint reload under black, and fade-in.
- Players: Single player.
- Inspiration / Reference Games: Requested Siren Head enemy; retain existing design.
- Tone / Art Direction: Existing neighborhood horror presentation, Skin_1 URP material and established black UI alpha fade.
- Target Platform: Windows / StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape; verify 1920x1080 and a smaller window when available.
- Render Pipeline: URP PC_RPAsset, Unity 6000.3.8f1.

## Confirmed Decisions and Approach
- User selected **Generate player death**: create a compatible non-looping Humanoid death/collapse clip for the existing player. Do not retarget the creature-specific Generic clip or generate a new player mesh/rig. The current player has a valid Humanoid Avatar.
- User selected **Blocked by walls** proximity awareness: proximity ignores the enemy's viewing direction, but requires a body-level unobstructed physics check. It is not through-wall hearing or automatic capture.
- Retargeting the existing creature Death was considered: it preserves the original motion but requires compatibility/motion validation and may not represent a human collapse. A supplied player clip offers artistic control but requires an additional asset. The selected generated Humanoid collapse directly expresses player death.
- For fades, reuse the existing project-owned `UIAlphaFadeEffect`, `UIFadeImage` and material with a small persistent death-retry overlay helper. Compared with using imported SceneTransitioner.LoadScene directly, a project-owned helper can await checkpoint restoration before revealing the level and works when Neighborhood is started directly without a menu manager. Preserve the imported transition manager and intro flow; never run two overlapping full-screen fades.
- Scope remains Neighborhood outdoors only. Farm player/controller assets and imported reusable controllers stay unchanged.

# Game Mechanics
## Core Gameplay Loop
Roam/Patrol → sight OR close-range awareness → Run/Chase → lost sight and left proximity → existing Investigate/Search → Roam. On valid capture: stop enemy → lock player controls/interactions/save/pause → play **player** Death → optional existing post-animation delay → Fade Out → reload same scene under opaque black → restore entry checkpoint → Fade In → release input, save and AI locks.

### Player Death
- Generate `Player_Death.anim`: a neutral upright humanoid loses balance, collapses to the ground, and stays motionless; one shot, no looping recovery or locomotion. Validate real animation data, positive duration, Humanoid compatibility and believable motion on Man_MeshAvatar before wiring gameplay.
- Create a project-owned copy of the currently assigned StarterAssetsThirdPerson Animator Controller for Neighborhood. Preserve existing locomotion blend tree, jumps, parameters and events; add terminal `PlayerDeath` state with Inspector-controlled playback multiplier and transition timing. No exit back to locomotion while dead.
- Play and observe the player's Animator, not `_enemy.Animator`. Siren Head stops moving and uses its compatible Idle pose during capture; it must not enter the creature Death state.
- Keep player Animator active while controller and PlayerInput are blocked. Disable automatic locomotion writes without disabling the entire Player object. Preserve existing root-motion policy; constrain/validate generated translation so the corpse does not slide through geometry.
- Adjust capture camera ownership to follow the animated player eye/head during collapse, rather than the current fixed eye position aimed at Siren Head. Preserve the first-person perspective, prevent head clipping/penetration into ground/walls, and use existing camera/head offset where practical. Cache original camera/Animator/input state for failure recovery. Do not invent a separate third-person death cinematic.
- Completion uses the correct player's state/layer normalized time and transition status at the configured speed. Diagnostic timeouts abort visibly; they must not silently skip missing/incompatible animation or claim successful playback.

### Proximity Chase
- Add Inspector `Proximity Chase Radius` (proposed initial 8 world units, 0 disables proximity), distinct from sight range (30) and capture distance (1.5). Add clear tooltip and gizmo; clamp invalid/negative values.
- Evaluate range using enemy body/root and actual player body center with 3D distance so players on another floor are not automatically nearby. Check occlusion from an enemy body-level origin to player body center; the very tall sight origin must not see over a short wall to satisfy proximity.
- Detection becomes `visible || nearbyUnobstructed`. A player close behind the enemy therefore triggers Chase immediately at the perception cadence, without meeting FOV. Walls/fences/solid closed doors still block; ignore self, player, held-item colliders and triggers consistently with existing sight checks.
- Update last-known position only while either awareness condition is positively met. Once both are false, existing sight-loss grace and last-seen investigation/search apply. Proximity is not a permanent hidden position tracker.
- Capture retains its current separate close-contact/vertical/occlusion checks; entering the proximity radius only starts Chase, never death through walls.

### Timing and Ownership
Expose/retain Inspector death speed, death blend time, post-animation delay, and independent Fade Out / Fade In durations (proposed 1 second each) plus optional black hold (0.2 seconds). UI fades use unscaled time. Ordinary pause still freezes enemy behavior; pause is rejected during death and retry.

# UI
No new menu or HUD. Use the same screen-filling black Image/material/easing as existing fades. Hide crosshair/prompts for the entire death/retry sequence and restore according to settings only when fade-in finishes.

```text
Player collapse [HUD/input locked]
   → visible scene gradually darkens (Fade Out)
   → fully black [load + restore + optional hold]
   → restored level gradually appears (Fade In)
   → normal gameplay resumes

Inspector: Proximity Chase Radius | Player Death Speed | Death Blend
           Fade Out Seconds | Black Hold Seconds | Fade In Seconds
```

The overlay must be above every gameplay canvas, remain opaque across unload/load, and block pointer events only while active. Reuse the established alpha effect, not a new URP Volume or shader workaround. Preserve New Game/Dog fades and prevent overlap with an active SceneTransitioner.

# Key Asset & Context
## Verified Current Context
- `PlayerCaptureSequence.cs` currently starts and watches `Base Layer.Death` on the creature Animator, freezes camera at capture eye position, and calls RetrySceneEntry after completion. This is the precise behavior to replace.
- Player has valid Humanoid `Man_MeshAvatar`, using `Assets/Imported/Starter Assets/Runtime/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller`. Inspected states are locomotion blend, InAir, JumpLand and JumpStart; no player Death state exists.
- Only discovered Death candidates are `Assets/Imported/Siren Head/Animation/Death.fbx` and its project `.anim` copy. Both are Generic (`humanMotion = false`), unsuitable for direct Humanoid playback on the player.
- `SirenHeadAI.cs` updates memory/chase from CanSeePlayer only. ClearLine already ignores self/player/held-item colliders and triggers. `_captureOrigin` is a body-level anchor suitable for proximity occlusion after verifying placement.
- `GameSaveManager` already owns immutable entry checkpoint JSON, Busy/EncounterInProgress, RetrySceneEntry and RetryEntryRoutine. Its OnSceneReady retry branch currently releases Busy/input immediately after Restore; this must defer until Fade In completes.
- `GameplaySaveAdapter` has Ready, Capture, Restore and BlockInput; its early Start initialization blocks input. Preserve capture/restore semantics, stable IDs and autosave suppression.
- `UIAlphaFadeEffect.cs` supplies AnimateOut(Image), AnimateIn(Image) and Configure(out,in,hold), using black alpha and unscaled time. Assets exist at `Assets/Transitions/UIAlphaFade.asset`, `UIFadeImage.prefab`, `UIFadeMaterial.mat`.
- The imported SceneTransitioner survives menu launch and supports PlayTransition(Action) but does not await an asynchronous midpoint action. No SceneTransitioner was detected in the directly opened Neighborhood scene. Do not assume a menu singleton exists.
- Prior final tests passed walking, sight, search, capture and retry/user-save preservation. Temporary enemy verification script/screenshots were removed. Do not recreate stale cleanup hooks or rerun the prior long test indiscriminately.

## Existing Assets to Modify
- `Assets/Scripts/Enemies/PlayerCaptureSequence.cs`: player Animator playback/completion, corpse-eye camera handling, idle enemy presentation and fade-enabled retry configuration.
- `Assets/Scripts/Enemies/SirenHeadAI.cs`: proximity radius/body-level awareness, combined detection and last-seen handling; capture stops into Idle instead of retaining moving pose.
- `Assets/Scripts/Saving/GameSaveManager.cs`: accept/delegate fade-enabled retry, keep encounter/Busy/input ownership until the reveal finishes, explicit restored-ready signaling and failure cleanup.
- `Assets/Scenes/Neighborhood Scene.unity`: project-owned player controller reference, death clip/state parameters, shared fade asset/Image prefab and timing values, proximity defaults/body origin. Preserve player persistent IDs, enemy spawn, navigation and waypoints.
- `Assets/Characters/SirenHead/SirenHeadEnemy.prefab`: proximity default/body origin changes if required, without changing imported creature rig, clips or materials.
- `Assets/Scripts/Saving/GameplaySaveAdapter.cs`, `Assets/Scripts/Settings/GameplaySettingsApplier.cs` only if required for deferred input/HUD release and first-person camera restore; inspect current implementation/impact first. Prefer reuse of existing public APIs.

## New Assets
- `Assets/Characters/Player/Animations/Player_Death.anim`: generated compatible one-shot Humanoid collapse clip; use actual returned asset path as source of truth.
- `Assets/Characters/Player/NeighborhoodPlayer.controller`: project-owned controller copy, existing functionality plus PlayerDeath state and speed parameter.
- `Assets/Scripts/Transitions/DeathRetryTransition.cs`: transient persistent overlay helper using existing Image prefab, material and cloned UIAlphaFadeEffect. Expose alpha/phase for focused verification if appropriate; no redundant global fade singleton.
- Optional focused tests in compatible existing test infrastructure. Do not install new packages or modify package source for this request.

# Implementation Steps
1. **Recheck exact references and impact.** Read this amendment; inspect dependencies/dependents for existing edit targets and imported controller source, player mesh/bones/eye/camera setup, pause/save input gates and fade assets. Confirm no pending verification script triggers. Preserve current scene setup. Dependency: user approval of this amendment.
2. **Generate and validate player Death first.** Use approved Humanoid animation generation for a clear collapse/still end pose. Verify real clip/humanMotion/length/non-looping/events, play on existing player rig, and assess root displacement/head/ground behavior. Stop and report generation/compatibility failure rather than falling back to the creature clip. No rig or mesh regeneration. Dependency: Step 1.
3. **Create/wire project player controller.** Copy the currently assigned controller, preserve locomotion/jump parameters/state machine, add terminal PlayerDeath state and speed/blend configuration, assign only Neighborhood's scene player. Validate actual pose changes and one-shot completion at 0.5/1/2 speed before changing the capture pipeline. Dependency: Step 2.
4. **Implement proximity awareness.** Add radius and body-level occlusion to SirenHeadAI, integrate with sight OR awareness condition and last-known memory updates, and document zero-disable behavior. Retain safe pathfinding and capture-distance rules. Configure on scene/prefab as appropriate. Dependency: Step 1.
5. **Implement restoration-aware persistent retry fades.** Add helper that creates screen-space overlay from existing prefab, clones/configures shared alpha effect, runs Fade Out and holds exact opaque black while coordinator loads/restores the checkpoint, then runs Fade In. Move retry orchestration to a persistent owner before outgoing scene destruction; never run two scene loads or two fades. Keep `_retryPending`/restored readiness distinct from operation completion so OnSceneReady restores state but does not release input/Busy/Encounter prematurely. Release after reveal and dispose cloned effect/material/overlay. Preserve normal entry/load/Exit autosave behavior and save bytes. Dependency: Step 1; existing entry-checkpoint retry API.
6. **Replace capture playback/camera ownership.** Stop enemy and present Idle, play/watch PlayerDeath on the active player Animator, follow the animated player eye during collapse with validated ground/head clearance, and lock input/pause/interactions/save/HUD throughout. After real clip completion and configured delay, request fade-enabled entry retry once. No creature Death call or watch remains. Cache and restore Animator/camera/input state on pre-reload failure; a failure after reload restores a usable new player and clears opaque overlay/locks with visible status. Dependency: Steps 3–5.
7. **Wire, save and clean up.** Bind Neighborhood death/controller/fade/image references and timing defaults, proximity body origin/radius, and preserve outdoor enemy/navigation/IDs. Direct Neighborhood play and menu-launched play must both work. Save intended scenes/assets only; remove temporary diagnostics safely after results/screenshots are retrieved. Dependency: Steps 4–6.
8. **Focused verification.** Compile after each stage; isolate runtime saves in a separate test directory, preserve original user records and evidence. Use actual Game View screenshots/video to assess collapse and partial/opaque/reveal alpha. Avoid forced domain-reload loops and do not claim a Windows build/other FPS/resolutions passed unless performed. Dependency: Steps 2–7.

# Verification & Testing
- Valid Humanoid player clip is non-looping and visibly collapses/stays still. Player Animator enters PlayerDeath; Siren Head Animator remains Idle and never plays creature Death. Player root stays within physical scene bounds.
- At death playback multipliers 0.5/1/2, Fade Out begins only after true player clip completion plus configured delay. No fixed wait based on creature clip duration. Normal locomotion/jump/held-item animation still works before capture.
- Camera stays first person during player collapse, follows animated eye motion, and never clips through head, ground, nearby walls or held item. Verify actual Game View, not just camera coordinates.
- Fade Out shows visible partial alpha, then exact opaque black. No old/new scene flash, falling corpse frame after reload, or clear frame before restore. Fade In begins only after initialization/checkpoint restoration and runs to exact transparent endpoint; HUD/input/AI/save stay locked until completion.
- Test direct Neighborhood play with no menu transition manager, menu-launched gameplay with an existing manager, repeated deaths, zero/short/long fades and black hold, missing clip/effect/Image reference, missing level, failed restore and disabled helper. No duplicate overlays, stuck black screen, orphan material/effect, permanently disabled input or parallel scene load.
- Proximity radius: inside threshold behind enemy and outside FOV → Chase; outside threshold and outside FOV → no detection; inside threshold behind solid wall/fence/closed door or sufficiently separated vertically → no proximity detection. Zero disables only proximity, not normal sight.
- Both awareness conditions lost → fixed last-seen memory → existing investigate/search timer → Roam. Reacquire by sight or proximity → Chase. Close proximity never bypasses separate wall/contact/vertical capture checks.
- Scene-entry retry restores player pose/pitch/world/held-light state, not death pose or latest manual-save location. Manual/autosave bytes and other visited-scene snapshots remain intact; no retry-entry autosave. Existing 443 Neighborhood persistent IDs unchanged.
- Existing main menu/Dog fades, pause, settings, save/load, ordinary entry/Exit autosaves, navigation and Farm remain unchanged. No imported controller/package edits or new input backend/defines.
- Final report distinguishes verified tests from outstanding manual/standalone checks; temporary test scripts, screenshots and deferred cleanup triggers are absent after safe cleanup.

# Camera-Based Scenery Activation Amendment

Only this amendment is pending approval for the current optimization request. Preserve earlier work. The user selected **Visual objects only (recommended)**: activate/deactivate audited scenery visuals, not gameplay objects or entire houses. This does not authorize true asset streaming or changes to enemy behavior.

# Project Overview
- Game Title: Existing FINAL-PROJECT; no branding changes.
- High-Level Concept: Existing first-person horror neighborhood exploration with Siren Head. Reduce active decorative scene objects outside the gameplay camera's view while retaining physical world continuity and off-screen enemy simulation.
- Players: Single player.
- Inspiration / Reference Games: Existing Siren Head reference; no additional design changes.
- Tone / Art Direction: Preserve existing horror scenery, materials, lighting and shadows.
- Target Platform: Windows / StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape configuration; preserve camera FOV, near/far clipping, and UI scaling.
- Render Pipeline: URP PC_RPAsset, Unity 6000.3.8f1.

## Recovery State and Selected Approach
- Native camera frustum culling already works. No custom visibility controller, registry or camera attachment was found after reconnection. No optimization implementation is complete; previous renderer-bound counts were inspection results, not measured performance gains.
- Current loaded Neighborhood scene is dirty. Do not reload, revert, or automatically save unrelated pending changes. Inspect and preserve those edits; obtain confirmation if an intended scene save would also commit unrelated work.
- `Neighborhood` and `NeighborhoodBoundaryFences` are currently inactive; preserve their authored/current inactive state. `NewNeighborhood` and `Neighborhood Vegetation` are active.
- `NewNeighborhood`: 29,647 renderers; preliminary audit found 21,692 active untagged MeshRenderer/MeshFilter-only leaf candidates. All 21,692 cast shadows. Candidates are not yet proven safe against ancestor logic or serialized references.
- `Neighborhood Vegetation`: 412 similar leaves, all shadowcasting and under LODGroups. Exclude vegetation from initial activation ownership to avoid fighting LOD selection or billboard transitions.
- Enemy and player visual children also superficially qualify as mesh-only leaves. Therefore never use a project-wide component-only filter; use explicit scenery roots and protected ancestor/reference checks.
- Existing QualitySettings shadow distance reads 40, but effective URP shadow settings must be checked independently before relying on that value.
- Alternatives considered: full decorative-chunk deactivation reduces controller overhead but has fewer safe targets; async level streaming can reduce resident memory but requires a larger save/navigation-aware restructuring. User chose visual-only activation. It preserves the horror dynamics and fairness (MDA/SDT): turning away does not remove walls, stop the threat, or erase world state.
- Scope: Neighborhood only, initially audited visual leaves under `NewNeighborhood`. No Farm/menu/Dog edits, packages, importer changes, build settings, input changes, or duplicated shadow-proxy meshes.

# Game Mechanics
## Core Gameplay Loop
Keep existing explore, interact, evade, capture, retry and save/load flow unchanged. The manager tracks camera visibility and activates visual-only scenery before it can enter view; it deactivates distant, definitely irrelevant visuals after a short grace period. Collision and enemy perception continue independently of camera direction.

This is **activation control**, not actual loading/unloading: meshes, textures and scene references remain resident. Native frustum culling already avoids most off-screen color rendering, so extra activation overhead may outweigh benefits. Measure before retaining the feature; never promise FPS or memory improvements without evidence.

## Controls and Input Methods
Preserve Both backend, event-driven PlayerInput/StarterAssetsInputs, CinemachineBrain and the capture camera's temporary ownership. Add no new player controls. Inspect the final actual Camera pose/projection each frame, including death FOV changes, rather than virtual-camera settings. The manager must run after current camera movement and before rendering.

# UI
No visible menus, HUD elements or new toggles. Inspector-only enable switch, camera/root assignments, safety distances, grace time and read-only counts:

```text
Scenery Visibility
  Camera: Main Camera (1)
  Registry: NeighborhoodSceneryVisibility
  Nearby safety radius / bounds padding / off-screen grace
  Enabled | registered | active | deactivated | shadow-protected
```

Existing HUD, pause/save dialogs and retry fade remain untouched.

# Key Asset & Context
## New project-owned assets
- `Assets/Scripts/Optimization/CameraSceneryVisibility.cs`: one scene-owned activation manager, cached registrations and spatial-cell bounds, no per-object Update components.
- `Assets/Scripts/Optimization/SceneryVisibilityRegistry.cs`: serializable GameObject/MeshRenderer entries, immutable initial bounds/cell membership and validation metadata. No dependency from StarterAssets or package assemblies to this code.
- `Assets/Optimization/NeighborhoodSceneryVisibility.asset`: scene-independent metadata only if suitable; scene object references must live in the scene component/scene-owned registry, not a persistent ScriptableObject asset. Prefer a scene-owned serialized registry to avoid invalid scene references.
- `Assets/Editor/Optimization/SceneryVisibilityAudit.cs`: explicitly invoked targeted audit/registration and validation; no auto-run cleanup/import hooks, no blanket scene edits or prefab modification.
- Optional tests under `Assets/Tests/Optimization/` using compatible existing test infrastructure. No new packages solely for testing.

## Existing asset to modify
- `Assets/Scenes/Neighborhood Scene.unity`: add one optimization owner outside controlled visual roots, explicitly assign `Main Camera (1)` and audited visual entries. Do not reparent scenery, alter mesh bounds, transform scales, tags/layers, colliders, static flags, materials or light settings. Preserve currently inactive roots.

## Protected context (read, do not modify by default)
- `Assets/Scripts/Enemies/SirenHeadAI.cs`: range/FOV/proximity and collider-based line-of-sight, NavMeshAgent and Animator must remain active off-screen.
- `Assets/Scripts/Enemies/PlayerCaptureSequence.cs`: moves the actual camera in LateUpdate and changes FOV; preserve player collapse, clearance checks, retry ownership and all references.
- `Assets/Scripts/Saving/GameplaySaveAdapter.cs`: explicitly registered persistent IDs, world capture/restore and player input gates; preserve registrations and world states.
- `Assets/Scripts/Saving/PersistentSaveId.cs`, `GameSaveManager.cs`, `Assets/Scripts/InteractiveDoor.cs`, `Assets/Scripts/InteractionsSystem.cs`, sky/weather and navigation components: inspect references and runtime object discovery before registration.
- `Assets/Settings/PC_RPAsset.asset`, `PC_Renderer.asset`, realtime lights and cameras: inspect effective shadows, reflection/secondary camera use and batching; no global configuration changes.
- Dedicated dependency tool is not available in the current tool set. Use read-only AssetDatabase.GetDependencies and dependent/serialized-reference inspection as fallback before implementation edits. Earlier checks show AI used by the enemy prefab and Neighborhood; capture by Neighborhood; save adapter by Farm and Neighborhood. Do not expand these scripts' edit scope.

# Implementation Steps
1. **Recheck current scene and edit impact.** Dependency: approval of this amended plan. Preserve dirty scene/user changes; snapshot current object activeSelf states and all intended registrations. Inspect pipeline/renderer dependencies, realtime shadow distance/cascades, lights, auxiliary cameras/reflections, static batching, and referenced visual objects. Verify no prior implementation appeared after reconnect. Do not repeat earlier completed maintenance.
2. **Audit eligible scenery explicitly.** Dependency: Step 1. Restrict to currently active `NewNeighborhood` visual leaves with Transform, MeshFilter and MeshRenderer only, no children, untagged, no missing components, no LODGroup/Animator/Rigidbody/particle/terrain ownership, no pickup/door/persistent-ID or other gameplay-sensitive ancestor. Review ancestors' scripts, serialized incoming references to each candidate GameObject/Transform/Renderer, and runtime name/tag/component discovery that may depend on active visuals. Protect referenced or uncertain entries rather than guessing. Exclude all player/enemy/held-item/navigation/sky/UI/light/camera roots and inactive root branches. Produce counts and rejection reasons before assigning entries.
3. **Build a cached spatial registry.** Dependency: Step 2. Serialize scene references on one scene-owned owner; build coarse spatial cells from initial world bounds without reparenting. Use aggregate cell bounds that fully enclose assigned meshes, conservative treatment of large/spanning meshes, and separate always-active exclusions. Cache membership once; no full hierarchy searches, LINQ, bounds rebuilding or allocations per frame. Keep meshes with runtime movement or uncertain static ownership outside this registry.
4. **Implement conservative visibility and shadow safety.** Dependency: Steps 1–3. Calculate the actual camera's planes using reusable arrays after camera/capture updates. Test expanded cell bounds; keep nearby visual geometry active with a safety radius, keep frustum-intersecting cells active immediately, and deactivate only after off-screen grace. All preliminary candidates cast shadows, so off-screen does not imply irrelevant: build a conservative shadow-receiver volume from the camera region up to the effective URP shadow distance. For directional lights extend it toward the light across the finite registered scene bounds; conservative AABB sweeps are acceptable even if they retain extra scenery. For shadowed point/spot lights retain potential casters within their influence when they can affect that receiver region. Keep unsupported/mixed/baked configurations or uncertain casters active; preserve baked lighting/reflection/probe contributions and do not alter shadowCastingMode. If auxiliary/reflection camera visibility cannot be accounted for safely, preserve relevant entries or fall back to original activation rather than remove their contribution.
5. **Implement activation ownership and failure recovery.** Dependency: Step 4. Change activeSelf only on registered visual leaves, never parent houses/colliders/scripts. Record only objects deactivated by this manager; disabling the feature or losing its camera/registry restores those objects, never originally inactive/external-disabled objects. Restore entries that are reparented, moved, gain components or otherwise become unsafe; invalidate/refit conservatively rather than toggle gameplay objects. Manager remains outside its controlled hierarchy. Use immediate activation for cells that enter view, stagger only safe off-screen deactivation with a bounded per-frame budget, and use a grace interval to limit churn. Update before the camera renders, even while paused and during capture; honor changed FOV, camera teleports/save restoration and scene reloads. No writes to game-save active state, persistent IDs, enemy readiness or input locks. Reset cleanly on scene unload and repeated/no-domain-reload play.
6. **Wire and validate locally.** Dependency: Steps 3–5. Attach the manager/registry to Neighborhood only; leave the camera's existing components and lens values unchanged. Verify runtime leaf activation, solid walls/floors/doors staying physically present, off-screen shadows, and transitions. If batching or rapid-turn activation generates hitches, narrow the audited set, coarsen conservative grouping, or disable the system rather than introducing unsafe culling. Save only intentional edits after handling existing dirty-scene state.
7. **Benchmark and finalize.** Dependency: Step 6. Compare feature off/on in equivalent camera paths with matching baseline active states. Measure main-thread controller/activation cost, CPU/GPU frame times, spikes and allocations with available profiling tools; active-object reduction alone is not success. Use a Windows development player if available; Editor results are not a standalone guarantee. Retain enabled behavior only when it provides a measured benefit without gameplay/visual regressions; otherwise keep it disabled and report the outcome. Remove temporary diagnostics safely; leave the audit tool explicit/manual, with no deferred cleanup.

# Verification & Testing
- Registry contains only audited scenery visuals. No player/enemy/door/pickup/collider/LOD/sky/navigation/UI/camera component or sensitive referenced child is controlled. Existing inactive roots remain inactive when toggling or disabling the manager.
- Look away/return, spin camera quickly, move toward large buildings and cross cell boundaries: visible meshes are active before rendering, with no missing wall/floor views or persistent flicker. Test near plane, partial bounds, wide death FOV and camera teleport after save/load/retry.
- Off-screen walls still block player motion, enemy sight/proximity/capture and interaction raycasts. Enemy continues roam/chase/investigate/search while off-screen; no attack through walls and no paused/off-screen AI reset.
- Off-screen shadowcasters that influence the visible street remain active. Compare feature off/on shadows at representative low/high sun angles and local lights; LOD trees, sky, baked lighting and reflection effects remain unchanged. Unsupported cases fail conservatively.
- Pause/resume, capture/player death, full retry fade, scene-entry checkpoint restore, manual saves/Continue/Load, held flashlight and door states remain functional. Persistent IDs/counts and user save bytes are not altered by visibility state.
- Missing/disabled camera, disabled manager, invalid registry entries, destroyed/reparented/moved candidate and repeated scene reload recover without exceptions or reviving originally/external-disabled objects. Disabling restores only manager-owned changes.
- No runtime full-scene scans or steady-state allocations. Activation/deactivation churn and static-batching effects are profiled during rapid turns; record median/high-percentile frames and spikes, not only average FPS.
- Report registered/active/deactivated/shadow-protected counts and rejection reasons alongside actual timings. Explicitly state assets stay resident, native frustum culling was already active, and whether the extra system improves performance. Do not claim build/manual/profile checks passed if not executed.

