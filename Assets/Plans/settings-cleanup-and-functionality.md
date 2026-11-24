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

# Canvas Video Intro Amendment

Only this amendment is pending approval for this request. Supersede the former Dog1–Dog8 intro with MainMenu → VideoIntro → Neighborhood Scene. Preserve save confirmation, manual saves, gameplay, enemy, settings and retry transitions.

# Project Overview
- Game Title: Existing FINAL-PROJECT; unchanged.
- High-Level Concept: Existing first-person horror exploration, introduced by the supplied cinematic video.
- Players: Single player.
- Inspiration / Reference Games: Existing project; no new design references.
- Tone / Art Direction: Preserve horror presentation; full-screen video with black letterboxing.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Landscape, existing reference UI scaling; preserve 16:9 video aspect at other window sizes.
- Render Pipeline: Existing URP PC_RPAsset, Unity 6000.3.8f1.

# Game Mechanics
## Core Gameplay Loop
New Game (existing autosave replacement confirmation when applicable) → fade out menu → load VideoIntro under black → fade in cinematic → play to actual completion → fade out final frame → load Neighborhood Scene under black → fade in gameplay. No timed Dog holds and no automatic skip control.
## Controls and Input Methods
Preserve Both backend and InputSystemUIInputModule. Retain SaveMenuController.NewGame and its single BeginNewGame call, click audio and duplicate-operation gates. Continue/Load bypass the cinematic as before. No new input bindings.

# UI
```text
VideoIntro Canvas (screen-space overlay)
  Black Background — full viewport
  VideoDisplay RawImage — centered, aspect-fit 1280 × 720
Persistent fade overlay — above video and gameplay UI
```
Use the existing Canvas and VideoPlayer, not a second redundant player. Enable audio from the clip's one embedded track; do not classify it as menu music or modify global audio settings. No new menus or skip button.

# Key Asset & Context
- Assets/Scenes/VideoIntro.unity is currently dirty: live scene contains Main Camera, Directional Light, EventSystem with InputSystemUIInputModule, and Canvas with CanvasScaler/GraphicRaycaster/VideoPlayer. Preserve these unsaved edits; do not reload/revert them. If saving also commits unrelated user work, obtain confirmation first.
- Current VideoPlayer has the supplied clip, RenderTexture mode, no assigned target texture, non-looping playback, playOnAwake enabled, Direct audio. Canvas uses ScreenSpaceCamera. It lacks a RawImage output.
- Assets/Videos/copy_BEC5D5CE-610A-446F-9006-5634618EE150.mov: 1280×720, approximately 22.592 seconds, one audio track. Preserve source/import settings unless Windows decode testing identifies an incompatibility; report codec issues rather than silently replacing the file.
- Build list already enables MainMenu, VideoIntro, Neighborhood Scene and FarmScene; TestScene disabled. Dog scenes are no longer included. No build-list change required.
- Assets/Scripts/NewGameSceneSequence.cs still validates/loads disabled Dog scenes. Modify it to coordinate the new video flow with existing fresh-run/settings behavior.
- Assets/Scripts/Saving/SaveMenuController.cs already routes NewGame through confirmation to BeginNewGame. Preserve callback/public API; scene changes only if wiring/reference correction is needed.
- Create Assets/Scripts/VideoIntroController.cs for video preparation, texture assignment, first-frame readiness, completion/error events, final-frame retention and cleanup.
- Create Assets/Videos/VideoIntro.renderTexture sized 1280×720 for the VideoPlayer and RawImage.
- Reuse Assets/Transitions/UIAlphaFade.asset, UIFadeImage.prefab and UIFadeMaterial.mat, plus established transition infrastructure. SceneTransitioner.LoadScene does not await asynchronous video preparation or gameplay restoration before revealing; coordinate the midpoint from project-owned code instead of altering imported/package source. Reuse existing restoration-aware overlay patterns where compatible, without modifying death-retry semantics.
- Dependencies inspected via AssetDatabase.GetDependencies because a dedicated dependency tool is unavailable. Before editing, inspect dependents and serialized references for all affected scenes/scripts/fade assets; do not alter shared assets globally.

# Implementation Steps
1. Recheck scene ownership, dirty edits, MainMenu button/sequence references, fade and save readiness APIs, dependencies/dependents and current Console. Preserve loaded scene setup. Dependency: approval.
2. Configure existing VideoIntro Canvas as screen-space overlay, black full-screen background and aspect-fit RawImage. Create/wire RenderTexture and retain supplied VideoPlayer clip. Disable autoplay, looping and automatic audio before readiness; use controlled preparation. Dependency: Step 1.
3. Implement VideoIntroController with Prepare/prepareCompleted, first decoded frame readiness and loopPointReached/errorReceived subscriptions. Prepare/prime first frame while black; rewind/hold before starting visible playback so the opening is not lost during loading. Coordinate video/audio start with reveal; hold final frame through outgoing fade without replaying audio. Include bounded preparation timeout, duplicate-event guards and cleanup. Dependency: Step 2.
4. Replace Dog sequence with validated VideoIntro and Neighborhood Scene destinations. Preserve BeginFreshRun boundary after existing confirmation/destination validation, GameSettings.Save, persistence, running guard and cleanup. Persistent project-owned orchestration fades out, loads under black, waits for video readiness, reveals and starts playback, waits for real completion, then fades out and loads gameplay. Await gameplay adapter initialization/restoration before reveal and keep gameplay commands locked until fade-in completes. Ensure entry autosave occurs once with existing policy. Use existing UI alpha effect and Inspector timings (1 second out/in, 0.2 second black hold), cloned per operation. No imported transition-manager source edits. Dependency: Steps 1–3.
5. Support direct VideoIntro Play Mode with idempotent transition bootstrap, no parallel manager/overlay, and no accidental fresh-run reset when opened directly. Missing clip/decode failure reports a visible error and offers controlled return to MainMenu rather than silently claiming playback or hanging black; destination failure restores usable UI/input. Dependency: Steps 3–4.
6. Save only intended scenes/assets, verify one New Game callback and no Dog references in active sequence. Preserve save/load/manual records, enemy/retry fades, optimization and settings. Remove temporary diagnostics without deferred cleanup hooks. Dependency: Steps 2–5.

# Verification & Testing
- New Game reaches VideoIntro (not Dog scenes), displays video inside Canvas with audible embedded track, plays once, then reaches Assets/Scenes/Neighborhood Scene.unity only after completion.
- Actual Game View shows partial fade alpha, opaque loading midpoint and transparent reveal on both switches; no blank first-frame flash, early gameplay reveal, competing overlays or missing final-frame hold.
- Full approximately 22.592-second clip is not replaced by a fixed duration timer; opening is not consumed during preparation/load. Aspect preserved at 1920×1080 and smaller/different-aspect window with black bars.
- Double-click New Game, cancel/confirm autosave replacement, repeated menu visits and direct VideoIntro play: no duplicate load/listener/manager; manual saves and settings remain intact. Continue/Load still go directly to saved gameplay.
- Missing texture/clip, preparation timeout/decode error, missing destination and interrupted operation recover visibly without stuck black/input locks or false success.
- Neighborhood input/HUD/cursor/enemy readiness correct after reveal; no repeated entry autosave. Existing death-retry fades unaffected.
- Compile and inspect Console; test Windows playback/build if available because Editor .mov import does not prove standalone codec support. Distinguish planned versus performed checks. Avoid prior long forced-domain-reload test workflow and preserve user saves.

# Twisting Road — CarScene Amendment

Only this amendment is proposed for the current request. Do not repeat earlier unrelated work.

# Project Overview
- Game Title: Existing FINAL-PROJECT; unchanged.
- High-Level Concept: Build a long, visually disorienting but continuously traversable start-to-finish road using the supplied Simple Roads modular assets. Difficulty comes from alternating bends, hairpins and an elevated crossing, not broken geometry or ambiguous mandatory forks.
- Players: Scene environment only; no vehicle or player controller currently exists in CarScene. Creating driving gameplay is outside this request.
- Inspiration / Reference Games: No additional reference supplied.
- Tone / Art Direction: Supplied low-poly road kit; preserve existing scene presentation.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Preserve existing landscape project configuration.
- Render Pipeline: Existing URP PC_RPAsset, Unity 6000.3.8f1.

# Game Mechanics
## Core Gameplay Loop
User selected the recommended twisting route rather than a branching maze or closed circuit. Construct approximately 1–1.5 km of connected roadway (target 80–110 modules, adjust to measured centerline length), with an entry straight, progressive S-bends, alternating tight and sweeping turns, two or three switchback sequences, a supported overpass crossing an earlier stretch, a final winding section and a distinct finish. Short straight recovery sections separate difficult combinations. This supports MDA challenge and player competence without frustrating dead ends. Completion timing cannot be established until a playable controller exists.
## Controls and Input Methods
Preserve Both input backend. No controls, vehicle physics, timers, checkpoint system or input bindings are added.

# UI
No new menus or HUD. Use scene-owned Start and Finish marker transforms and simple contrasting world-space marker geometry if needed; avoid introducing a UI dependency.

```text
START → entry straight → S-bends → sweeping loop → hairpins
                                             ↓
FINISH ← final winding section ← overpass ← switchbacks
```
Layout is illustrative; final coordinates follow actual connector geometry and crossing clearance.

# Key Asset & Context
- Modify only `Assets/Scenes/CarScene.unity` for the road environment. Live scene is dirty and contains Main Camera, Directional Light and Floor; user explicitly approved preserving and saving these existing edits together with the road.
- Create scene-owned `TwistedRoad` root with numbered modular instances grouped by section, start/finish anchors and collision additions. Preserve prefab links and imported assets unchanged.
- Prefer supplied `road straight.prefab`, `road curve.prefab`, `road turn large.prefab`, `road trun small.prefab`, `road ramp straight.prefab`, `road ramp curve.prefab`, `road ramp turn.prefab` and elevated straight/turn prefabs under `Assets/Imported/Simple Roads/Prefabs/`. Use actual spellings.
- Inspected straight bounds are 8×2×16 with offset center (2,0,0); large turn bounds are 30×2×30 with center (1,0,-1); ramp straight spans 32 horizontally and 10 vertically with center height 4. Bounds are not road endpoint sockets: inspect mesh vertices and sample scene placement before fitting joins.
- Several prefabs, including both ordinary and elevated straights, have no colliders. Other prefabs have colliders whose road/guard coverage still needs verification. Add missing static surface/guard collision only on generated scene instances; never globally edit the kit.
- Floor currently has position (71.10,0,-155.38) and uniform scale 122.56. Inspect its actual mesh, bounds and collision before choosing track origin/height. Preserve it unless a confirmed support adjustment is necessary; prefer scene-owned kit terrain additions over altering existing user geometry.
- Dependency inventory obtained through read-only AssetDatabase.GetDependencies (dedicated dependency tool unavailable). Materials already depend on URP Lit; do not perform blanket material conversion.
- Use a single reusable construction script during implementation; revise that script for fitting fixes rather than repeatedly regenerating unrelated geometry. No persistent editor generator is required unless needed for reproducibility.

# Implementation Steps
1. **Recheck impact and geometry.** Dependency: approval. Inspect CarScene dependents, incoming serialized references, Floor bounds/collision, road mesh orientations, endpoint widths/heights, ramp elevation delta and guard geometry. Inspect kit sample Race Track placement read-only for connector conventions. Preserve current dirty scene without reload/revert. Confirm no existing TwistedRoad appeared since discovery.
2. **Prototype connections.** Dependency: Step 1. Fit representative straight-to-turn, S-bend and ramp-to-elevated connections using transformed road endpoints, positive scales and compatible widths. Verify road surface versus renderer/support bounds. Use scene-owned static MeshColliders for uncovered road meshes where appropriate; avoid duplicates or coarse boxes blocking turns.
3. **Lay out continuous route.** Dependency: Step 2. Use measured module connectors to author one deterministic route with approximately 80–110 pieces and 1–1.5 km target length. Establish section footprints before placement, allowing ample spacing between neighboring switchbacks. Keep meaningful recovery straights, no mandatory forks, no accidental same-level crossings and no unsupported jumps.
4. **Build elevated crossing and support.** Dependency: Steps 2–3. Use compatible ramp/elevated pieces and supports, verify smooth slope joins and underpass clearance from actual meshes. Avoid negative scale mirroring. If elevation pieces cannot form a safe continuous crossing, resolve fitting before continuing rather than leave a disconnected bridge. Add only necessary scene-owned terrain/support kit instances; maintain existing Floor.
5. **Finish presentation and collision.** Dependency: Steps 3–4. Add start/finish anchors and unobtrusive visual markers, organize numbered sections, verify prefab materials render normally, and fill confirmed collision gaps on road/guards. Preserve camera/light configuration; frame Scene View for review without replacing the existing Main Camera. No vehicle, UI, lighting redesign or build-list changes.
6. **Visually validate and save.** Dependency: Step 5. Capture fresh multi-angle Scene View images of the complete track and representative turns/ramp/crossing. Check scale, overlaps, support, seam continuity and elevation clearance. Reuse and revise construction code, capture fresh evidence after each fix, maximum three capture/fix rounds before requesting guidance. Save only intended CarScene changes and authorized preserved edits.

# Verification & Testing
- One continuous Start-to-Finish route; target centerline length measured and reported, not inferred only from module count.
- Inspect every neighboring endpoint for position, height, tangent and width agreement; no visible gaps, raised lips, coplanar z-fighting or guard rails obstructing a join.
- Verify downward surface raycasts along straights, curves and ramps, plus side/guard collision where applicable. Existing colliders alone do not prove full drivable coverage.
- Overpass has adequate road and guard clearance, attached ramps and visible supports; route crossings occur at different elevations and do not intersect meshes/collision.
- Multi-angle overview and close-ups demonstrate a long winding route, switchbacks, recovery straights and distinct start/finish; no magenta materials or floating/sunken pieces.
- No imported prefab/model/material, unrelated scene, controller, save system, input backend or build-list edits.
- Recheck Console for new errors/warnings. Traversal/driving difficulty and completion duration remain unverified because CarScene contains no playable vehicle/controller; report that limitation rather than claim a driving test passed.

# Tangled Abandoned Walkable Route Amendment
Only this amendment is pending for the current request. Supersede the former regular nine-pass layout and flat-only constraint; preserve unrelated completed work.

# Project Overview
- Game Title: Existing FINAL-PROJECT.
- High-Level Concept: Explore an abandoned, tangled roadway on foot, following one continuous route through irregular curves, diagonals, short straights and supported rises/descents. Distribute the route throughout the Floor rather than along a single diagonal or repeated uniform rows.
- Players: Existing single-player Player character.
- Inspiration / Reference Games: No new reference supplied.
- Tone / Art Direction: Desaturated, weathered low-poly infrastructure with sparse overgrowth, inactive streetlights, leaning signs and roadside rubble; visual decay must not make the required route impassable.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape configuration.
- Render Pipeline: Existing URP PC_RPAsset, Unity 6000.3.8f1.

# Game Mechanics
## Core Gameplay Loop
User selected a continuous route, not a branching maze. Walk from the lower corner toward the opposite upper corner, explore irregular broad loops and tight turns, traverse diagonals and supported over/underpasses, descend and continue to the finish. Preserve whole-Floor spatial coverage, but avoid blindly preserving the earlier roughly 17 km length: walking duration and exact new length must be measured and reported rather than guaranteed. Alternating complex sections with short readable recovery sections supports MDA tension and player competence. No mandatory jumps, disconnected gaps or accidental same-level crossings.
## Controls and Input Methods
Preserve Both backend, PlayerInput/StarterAssetsInputs and existing FirstPersonController controls. Do not add legacy polling, new bindings or AI navigation for a manually controlled Player. Keep controller slopeLimit 50 degrees and stepOffset 0.5 unless actual tests establish a need; design continuous ramp slopes conservatively around 10–20 degrees, below that limit, with smooth entries/exits and capsule clearance.

# UI
No new HUD, menus or gameplay systems. Retain Start/Finish anchors, add readable world-space abandoned signposts and sparse visual breadcrumbs at confusing crossings. Decay should not hide the traversable surface or obscure route continuity.

# Key Asset & Context
- Modify `Assets/Scenes/CarScene.unity`: replace only generated TwistedRoad geometry/dressing, preserve Floor and newly added Player/camera edits. Scene is currently dirty; user expressly authorized preserving/saving those edits, moving Player to the start and repairing scene-local walking wiring.
- Floor bounds center (71.10,0,-155.38), extents approximately (612.80,0,612.80); unchanged scale/position. Plan route using world-space bounds and inset safety margins.
- Current TwistedRoad has 4,672 renderers. Existing layout is regular and flat; do not treat prior raycast tests as proof the revised route is walkable.
- Player at approximately (46.16,0.12,50.49) uses CharacterController height 1.7, radius 0.4, slopeLimit 50, stepOffset 0.5, Animator, PlayerInput, StarterAssetsInputs, FirstPersonController and BasicRigidBodyPush. Keep mesh, animation and controller architecture.
- Both Main Camera and Main Camera (1) are currently enabled, tagged MainCamera and have AudioListeners. Verify actual follow ownership before retaining the player camera and locally disabling redundant original camera/listener. Do not assume Cinemachine exists in this scene.
- Player has GameplaySaveAdapter with `_player` assigned but missing interaction/pause references and many unresolved object entries. CarScene is not automatically a supported save scene. Inspect GameSaveManager/SupportedScene handling before disabling this incomplete scene-local adapter if it obstructs walking; do not modify shared saving scripts, add CarScene saving, alter IDs or write user saves.
- Reuse Simple Roads straight/curve/turn/ramp/elevated assets. Large-turn measured sockets are entry (-8,0,-16), exit (16,0,8), centerline radius 24; straight fitting requires correct two-lane scene overrides. Ramp straight rises 8 over 32 horizontal units; validate actual slope curvature and endpoints.
- For diagonals use properly fitted 45-degree connector sections: verify kit support; if unavailable create project-owned partial curved mesh copies with matching road/sidewalk/guard cross-section rather than rotating pieces into disconnected joins.
- Prefer existing PoplarTree001_pr, FastMesh StreetSign-1, ModularHousePack road streetlight assets for dressing after bounds/material/component inspection. Do not attach their unrelated gameplay logic or add hundreds of realtime lights. Use project-owned material copies and sparse mesh rubble if suitable source assets are absent; AI asset generation is unnecessary.
- New assets only as needed under `Assets/Environment/CarScene/Materials/` and `Assets/Environment/CarScene/Meshes/`; optional explicit construction tooling under `Assets/Editor/Environment/`. Do not edit imported shared assets/packages globally.
- Dependency tool unavailable in current read-only tool set: use AssetDatabase.GetDependencies plus dependent and serialized-reference scans before each existing edit. Live scene references must be inspected because unsaved Player additions are absent from disk dependency lists.

# Implementation Steps
1. **Recheck ownership and wiring.** Dependency: approval. Inspect scene dependencies/dependents and incoming references to TwistedRoad, Player, cameras and adapter; read current FirstPersonController camera/grounding code, GameSaveManager unsupported-scene behavior, input action setup and Player Animator/head target. Preserve dirty scene without reload/revert; record original enabled states. Inventory suitable dressing prefabs and ramp meshes/materials.
2. **Author route skeleton and connectors.** Dependency: Step 1. Define a deterministic continuous centerline distributed through lower/middle/upper Floor regions, irregular loops, short straights, 45-degree diagonals and multiple controlled elevation changes. Reserve footprints and crossing heights before instantiation. Fit native or project-owned 45-degree curves and ramp connectors in a small representative section first. Do not approximate joins by overlapping incompatible road ends.
3. **Construct walkable geometry.** Dependency: Step 2. Replace only generated road hierarchy using one reusable builder; preserve unrelated scene objects. Add correct static collision on road/guards, continuous widths/heights/tangents, supported elevated sections and adequate Player head clearance under crossings. Keep supports off required walking lanes and Floor intact. Add side protection at elevated bends; no invisible barriers replacing visibly missing geometry.
4. **Add abandoned presentation.** Dependency: Step 3. Create scene-specific weathered/desaturated material copies, sparse overgrowth beside paths, inactive or damaged streetlights, leaning signs and small rubble patches. Keep clear walkable corridor at least 2 m wide, full capsule head clearance and readable navigation. Place damage at shoulders, not as mandatory broken gaps. Match physical support/ground contact and avoid expensive broad scatter or global lighting changes.
5. **Wire existing Player locally.** Dependency: Steps 1 and 3. Move Player safely to Start with feet on verified surface and initial facing along route; retain valid head/camera follow. Resolve duplicate camera/listener ownership and only confirmed scene-local startup input blockers. Disable the incomplete scene-local save adapter only if impact inspection shows it is inappropriate for this unsupported scene; preserve shared save behavior elsewhere. No new character, controller rewrite, binding changes, save integration or build-list edits.
6. **Test traversal and visually validate.** Dependency: Steps 3–5. Check surface interiors and seams, slope extrema, capsule sweep clearance, crossing heights and Floor containment. Test actual Player movement/look and representative straight/diagonal/curve/ascent/descent/underpass sections in Play Mode, including camera follow and audio listener uniqueness. Capture fresh multi-angle scene views and first-person Game View evidence; revise same builder for defects, at most three capture/fix rounds before asking for guidance. Do not claim full-route traversal from raycasts alone.
7. **Save and report.** Dependency: Step 6. Save intended CarScene and project-owned environment assets, retaining authorized user edits. Remove temporary builder copies and diagnostic hooks safely; no deferred cleanup that affects saves. Report route length, elevation range, spatial coverage, verified walking sections and outstanding end-to-end/build testing. Keep existing user save files untouched.

# Verification & Testing
- One Start-to-Finish route across the Floor with visibly varied straights, curves, diagonals, loops, climbs and descents; no regular repeated nine-row pattern, unintentional crossings or disconnected joins.
- All critical road seams meet in position/height/tangent and collision; surface-interior checks plus seam checks. Investigate precision misses rather than conceal them with a broad underlying Floor collider.
- Slopes and ramp entry/exit steps are safe for existing CharacterController; capsule sweeps show clear lanes, adequate underpass height and no support/guard collision blocking the route.
- Existing Player moves and looks normally on representative sections; grounding, footsteps and first-person head/camera behavior work. Exactly one intended active gameplay camera/listener; no wrong Camera.main resolution or startup save-adapter input lock.
- Weathered materials render in URP; roadside vegetation/rubble/signs appear grounded and deliberately abandoned, without obstructing the corridor, floating props or excessive realtime lights.
- Multi-angle whole-layout and close-up captures, plus actual first-person walking evidence; measure actual route length/elevation, do not promise a completion duration without traversal testing.
- Other scenes, imported assets, settings/save scripts, persistent IDs, packages, input backend and user saves unchanged. No new console/compiler errors; standalone and full end-to-end walking are reported as unverified unless actually tested.

# CarScene Neighborhood Systems Transfer — Recovery Amendment
Only this amendment is pending approval. Supersede prior exclusions of CarScene UI, saving, enemy/navigation, lighting and boundary additions for this requested transfer. Do not rebuild the existing road or repeat completed maintenance.

# Project Overview
- Game Title: Existing FINAL-PROJECT.
- High-Level Concept: Explore the existing tangled abandoned road at night, surrounded by grass and dense trees, with Neighborhood's pause/settings/save and Siren Head threat. Keep the player confined to the continuous roadway with readable rails and physical boundaries.
- Players: Existing single player on foot.
- Inspiration / Reference Games: Existing Neighborhood Scene and Siren Head assets.
- Tone / Art Direction: Existing abandoned low-poly horror route; match source night atmosphere without sacrificing route readability.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape; verify 1920x1080 and smaller windows.
- Render Pipeline: URP PC_RPAsset, Unity 6000.3.8f1.

## Confirmed Recovery State and Decisions
- Live CarScene is dirty. Preserve its unsaved edits without reload/revert; confirm before saving any unrelated newly discovered edits.
- Current roots: Main Camera, Directional Light, Floor, Main Camera (1), Player, TwistedRoad, CarScene First Person Camera. TwistedRoad currently has 801 renderers and 664 colliders; these counts do not prove seam or boundary correctness.
- No PauseMenuManager, SirenHeadAI, PlayerCaptureSequence or GameplaySaveAdapter was found in the live scene. Transfer work is not complete.
- Source dependency inspection confirms existing SirenHeadEnemy prefab/controller/animations, PauseMenuManager, Neighborhood navigation and Tenkoku DynamicSky. Reuse source configuration, not bare imported demo systems.
- AI Navigation 2.0.10, Cinemachine 3.1.7, Input System 1.19.0 and URP 17.3.0 are installed.
- User selected full system transfer including saving/entry checkpoints, and visible rails plus continuous collision. This preserves familiar horror dynamics (MDA) and fair recoverable failure (SDT), at greater integration cost than pause-only or invisible barriers.
- SaveGameData currently permits only Neighborhood/Farm and at most two scene snapshots. CarScene is absent from enabled build scenes; current list includes MainMenu, VideoIntro, Neighborhood and Garden, with Test disabled. Add CarScene without silently enabling Farm or changing the intro destination.

# Game Mechanics
## Core Gameplay Loop
Walk the existing route, avoid Siren Head, pause/save/settings/resume, and retry the CarScene entry checkpoint with existing player-death and fade behavior on capture. Enemy resets on load/retry as in Neighborhood; do not promise exact AI-state or sky-time persistence.
## Controls and Input Methods
Preserve Both backend and current event-driven PlayerInput/StarterAssetsInputs and InputSystemUIInputModule. Rebind transferred scene references to CarScene's existing Player/camera/hand targets. No new character or input backend. Keep pause, save restoration, capture and fade locks coherent.

# UI
Reuse source UICanvas pause/HUD/save dialogs and existing shared settings styling; do not copy the source Player or duplicate EventSystems.
```text
Gameplay: existing first-person camera + crosshair/prompts
Pause: Resume | Save Game | Settings | Exit
Save: three slots + overwrite confirmation + Back/status
Capture: player collapse → black/loading/checkpoint restore → reveal
```
Keep settings visibility rules and readable modal captions; Exit autosaves before MainMenu with existing error recovery.

# Key Asset & Context
- Modify Assets/Scenes/CarScene.unity: scene-local UI, interaction/settings/save references, enemy/capture, road navigation/waypoints, night sky and vegetation, rails/boundaries. Preserve Floor and existing route layout/player.
- Read Assets/Scenes/Neighborhood Scene.unity as source only. Inspect source hierarchy in a safe read-only preview or serialized inspection, including exact sky/night overrides, grass, tree materials/LOD, pause and capture dependencies. Never transplant invalid source scene references.
- Reuse Assets/Characters/SirenHead/SirenHeadEnemy.prefab, project-owned player death controller/clip, and Assets/Transitions fade assets; preserve shared prefab defaults unless a necessary backwards-compatible fix is approved in this scope.
- Modify Assets/Scripts/Saving/SaveGameData.cs narrowly: include CarScene and update bounded scene-count validation consistently without changing schema fields or invalidating existing records. Read repository/manager/adapter consumers first.
- Modify shared save/enemy/pause scripts only for confirmed hard-coded scene assumptions or backwards-compatible integration gaps after impact analysis; prefer scene wiring.
- EditorBuildSettings: add CarScene for save loads/retries; preserve other scene inclusion and New Game destination.
- New Assets/Navigation/CarSceneSirenHeadNavMesh.asset; use existing fitted enemy agent type, not Neighborhood's baked data.
- Project-owned terrain/material/vegetation assets under Assets/Environment/CarScene/ only if required. Never share writable TerrainData with Neighborhood.
- Inspect Tenkoku installed documentation and URP runtime compatibility before promising dynamic sky. Match working source configuration when compatible; if unsupported, report the limitation and seek approval for compatible night sky fallback, rather than modifying package guards or replacing it silently.
- Dedicated dependency tool unavailable: use AssetDatabase.GetDependencies and serialized/dependent inspection for each existing edit target, including live unsaved scene references.

# Implementation Steps
1. Recheck source/destination ownership and dependencies. Read current save, pause, interaction, enemy, capture, camera and sky code; inventory navigation components/agent settings and exact source object references. Inspect current console and pending diagnostic hooks. Preserve dirty scene state. Dependency: approval.
2. Integrate pause/settings/HUD/interactions using source scene-owned objects with remapped references, one EventSystem, intended gameplay camera/listener and current Player. Copy player death Animator configuration without replacing character. Dependency: Step 1.
3. Extend save support narrowly, add CarScene to build list, create complete scene-local adapter/registrations with new unique IDs for CarScene saveable objects only. Preserve existing source IDs and save files; retain entry/Exit autosaves, manual slots and retry checkpoint semantics. Verify older two-scene records remain valid and three supported snapshots pass validation. Dependency: Steps 1–2.
4. Repair roadway confinement. Audit actual surface/guard collision, including curves, diagonal connectors, ramps, seam gaps and overpasses. Add grounded visible rails with static positive-scale collision along both outer roadway edges, and sufficient aligned boundary collision to prevent jumping/stepping over them without blocking the road. Preserve intended Start/Finish access within bounded route; no escape to Floor or shortcuts between close road stretches. Avoid giant boxes cutting across curved lanes; retain at least 2 m clear corridor. Dependency: Step 1.
5. Transfer grass and dense roadside trees using verified source assets/material overrides. Distribute varied clusters across surrounding Floor with clear road/rail setbacks, no growth on elevated lanes, grounded placement and LOD-aware density. Do not clone source terrain at mismatched coordinates or enable all expensive tree colliders/lights. Keep shared imported assets unchanged. Dependency: Step 4.
6. Transfer supported dynamic night sky and source ambient/night presentation, remapping gameplay camera references and preventing duplicate sun/sky ownership. Validate actual Game View sky/materials, road visibility and runtime compatibility; do not substitute a static sky without approval if the requested system fails. Dependency: Steps 1 and 5.
7. Bake road-only CarScene navigation after boundaries/dressing. Match existing enemy agent clearance, exclude Floor shortcuts, supports, player/UI and off-road decoration as walkable surfaces; incorporate real blockers. No automatic jump/drop links or through-rail shortcuts. Verify ramps/underpass clearance and connected waypoint paths without shrinking creature dimensions to conceal fitting failures. Dependency: Steps 4–6.
8. Place one existing SirenHeadEnemy at a safely separated reachable road spawn, rebind patrol/sight/proximity/capture/player Animator/save/camera/fade references. Preserve roam/chase/investigate/search, pause freezing and player death checkpoint retry. Activate after adapter readiness/checkpoint setup. Dependency: Steps 2–3 and 7.
9. Focused verification and save: compile incrementally, inspect UnityEvents/missing references, actual Game View and road boundary tests. Isolate test saves, never delete user records or create deferred cleanup hooks. Save intended assets and authorized preserved CarScene edits only. Dependency: Steps 2–8.

# Verification & Testing
- Exactly one functional pause/HUD/save interface, enemy, intended camera/listener and EventSystem. Source Neighborhood remains unchanged.
- Pause freezes movement/AI timers; settings/HUD/tooltips work; manual slots, Exit autosave, MainMenu load/Continue and entry checkpoint retry work in CarScene without stale source references or save corruption.
- Existing records still validate. New CarScene records and supported three-scene snapshots validate; no unrequested Farm build inclusion or intro rerouting.
- Walk/run/jump into boundaries at straights, tight bends, diagonal seams, ramp transitions, elevated sides and adjacent close road sections. Player cannot reach Floor, jump rails, clip seams or shortcut to another stretch; corridor and Start/Finish remain usable.
- NavMesh is nonempty and matched to enemy type; representative complete paths span turns/ramps/overpass without off-road or wrong-elevation projection. Source baked data is never reused.
- Siren Head walks/runs, detects by sight/proximity with correct occlusion, searches after awareness loss, and captures once; player collapse and restoration-aware retry fades complete with input/HUD released at the right time.
- Night sky actually renders and behaves as configured in URP; trees/grass render without magenta, floating placements or road obstruction. Compare dense scenery performance and LOD transitions; do not claim FPS improvements without measurement.
- Compile/console checks, direct CarScene play and menu-load checks; report whole-route traversal, Windows build, alternate resolutions and performance measurements as unverified unless performed.

# Siren Head Interference and Radio Reset Amendment

Only this amendment is pending approval for the current request. Preserve earlier work; do not repeat other amendments or transfer this feature to other scenes.

# Project Overview
- Game Title: Existing FINAL-PROJECT; unchanged.
- High-Level Concept: First-person horror exploration in Neighborhood. Time spent near Siren Head accumulates audible interference; maximum interference distorts the view and starts a cancellable blackout. Reach and use the Radio before full black to recover.
- Players: Existing single player.
- Inspiration / Reference Games: Existing Siren Head threat; no additional references supplied.
- Tone / Art Direction: Preserve existing horror environment, supplied interference audio and both supplied Volume profiles.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape UI; verify 1920x1080 and a smaller/different-aspect window.
- Render Pipeline: Verified URP PC_RPAsset, Unity 6000.3.8f1; HDR and renderer PostProcessData already enabled/present.

## Decisions and Selected Approach
- User selected **Keep accumulated level**: outside the enemy radius, accumulated interference holds rather than decays. Only Radio resets it during active play.
- User selected **Scene-entry checkpoint**: full blackout uses the existing save-safe entry retry; never reload through ordinary scene-load/visited-state logic or overwrite saves.
- User approved saving Neighborhood with current unsaved edits preserved. Do not reload/revert the dirty scene; seek confirmation if additional unrelated changes appear later.
- Use a project-owned proximity accumulator plus narrow integration with the existing InteractionSystem and retry coordinator. An independent Radio F listener/prompt was considered but risks duplicate F handling, dropping a held flashlight, and competing prompt writers. The selected integration gives one input owner and one prompt writer while preserving other interactions. This supports horror tension (MDA) and a clear recovery action/player agency (SDT).
- Use a standard UI black alpha overlay, not post-exposure alone: it guarantees fully black output even with bright geometry and HUD. Reuse existing fade assets for the restoration-aware reveal. Do not edit shared Volume profile assets, imported prefabs, package source or pipeline defaults.

# Game Mechanics
## Core Gameplay Loop
Explore with quiet continuous static → enter adjustable Siren Head radius → accumulate volume over time → leave radius to stop accumulation, without lowering it → maximum volume switches to danger Volume and begins gradual darkening → approach Radio and press F to restore quiet audio, normal Volume and clear view → otherwise full black retries the scene-entry checkpoint and resets interference.

### Rules and Tuning
- Dedicated looping 2D AudioSource on a new Player child, using `Assets/Imported/Radio interference Sounds/AMB_Continuous_Static_Loop_02.wav`. Do not reuse Player footstep/landing audio or either enemy sound source. Keep music settings from controlling this source; no global AudioListener volume changes.
- Inspector values: baseline volume (proposed 0.08), maximum volume (0.8, adjustable 0–1 with baseline strictly below maximum), enemy influence radius (20 m), accumulated time from baseline to maximum (30 s), blackout duration (5 s), Radio interaction radius (3 m), retry fade-in (1 s), black hold (0.2 s), obstruction mask and explicit references. These are editable initial tuning, not guaranteed final balance. Validate finite values and enforce meaningful positive durations/radii and ordered volume bounds without divide-by-zero.
- Track normalized exposure independently of audible volume: `exposure = Clamp01(exposure + deltaTime / timeToMaximum)` only inside the radius; `source.volume = Lerp(baseline, maximum, exposure)`. Threshold uses clamped exposure == 1, not fragile audio float equality. Use player/enemy body/root positions and 3D distance. Radius is exposure distance, not the enemy's detection/capture radius. No FOV/line-of-sight requirement for interference; walls do not silently alter this distance-based request.
- Below maximum: original `Global Volume` enabled/weight 1, danger `Global Volume (1)` disabled/weight 0; blackout alpha exactly 0.
- At maximum: original Volume disabled/weight 0, danger Volume enabled/weight 1, and black alpha progresses 0→1 over blackout duration. Once maximum is reached, leaving Siren Head does not stop blackout. Player retains movement/look and Radio access until full black; no premature encounter/input lock.
- Radio use before committed retry immediately clears exposure/blackout progress and alpha, returns source to baseline (not silence), and restores the original Volume. If still near Siren Head, accumulation resumes from baseline on the following gameplay update; no unrequested permanent immunity/cooldown.
- Ordinary pause freezes exposure and blackout with scaled gameplay time; the audio remains looping at its current level. Pause/load/intro/capture operations suppress Radio input and exposure advancement. Resume continues remaining progress. Background focus loss must not advance progress unexpectedly.
- At alpha == 1, claim the existing encounter guard, block player/input/interactions through GameplaySaveAdapter, hide HUD, and request entry retry once. No player/creature death animation is triggered by interference. Keep opaque coverage through unload/restore, then fade in only after restored readiness; release input/HUD/enemy after reveal.
- Before claiming the retry, retry/capture contention must be resolved with the existing Busy/EncounterInProgress guards. If physical enemy capture wins first, suspend and remove only this feature's blackout overlay so capture owns its existing death/fade sequence. Never cancel another system's encounter, clear its locks, or start a second reload/fade.
- Exposure/blackout are transient scene-session state, not a new save schema field. Fresh scene entry, explicit saved-game load and successful retry initialize at baseline/normal view. Pause/manual save alone does not reset exposure. Preserve user save bytes, IDs and existing autosave policy; retry must not create an autosave.

## Controls and Input Methods
- Verified activeInputHandler = 2 (Both). Player uses PlayerInput SendMessages with StarterAssets.inputactions; current maps contain Move, Look, Jump and Sprint, no interaction action. Menus use InputSystemUIInputModule.
- Current InteractionSystem polls F for pickup/drop. Introduce a lifecycle-managed event-driven InputAction bound to `<Keyboard>/f` for this existing F path, without editing shared StarterAssets actions, changing backend, or adding duplicate callbacks. Record a one-shot press for the existing interaction processing phase; do not mutate physics/scene state directly inside input callbacks. Disable/unsubscribe/dispose safely and clear stale presses on pause/block/focus changes.
- Add an optional explicitly assigned RadioInteraction reference to InteractionSystem (null retains other-scene behavior). Process an eligible Radio F action before pickup/drop; consume that press so held items stay held and Radio is never picked up. Keep existing E/right-click semantics outside this narrow change.
- Radio eligibility is proximity to its actual collider, independent of aiming, with a body/view-to-Radio obstruction check so no prompt/use through solid walls or floors. Ignore player/held-item/Radio endpoint colliders and triggers consistently. Use the same eligibility result for prompt and action, recomputed when consuming F. Only the configured Player can use it.

# UI
Reuse `UICanvas/InteractionPrompt`, its font/layout and HUD/tooltips visibility rules. Radio prompt takes precedence when eligible, including while holding a flashlight; normal door/item prompt returns after leaving range. No second EventSystem or prompt Update writer.

```text
Normal view: existing crosshair + existing interaction help
Near Radio:  Press F
Maximum:    danger Volume + progressively darker view
Radio F:    normal Volume + clear view + baseline static
Full black: opaque persistent loading cover → restored level fade-in
```

- Add one scene-owned full-viewport black Image/Canvas dedicated to cancellable interference blackout. Use screen-space overlay, responsive anchors and sorting above ordinary gameplay UI, black alpha 0 initially. Raycast Target is false during reversible blackout so it cannot block Radio or pause UI.
- Persistent committed retry cover uses the established retry overlay at exact alpha 1 before scene unload. Never introduce a transparent handoff frame or a second fade-out from zero.
- Respect ShowHUD/ToolTips and pause/capture visibility. Hidden tooltip does not disable Radio interaction. Failures use existing coordinator status plus visible project-style error feedback if that status is not currently displayed; never leave an unexplained opaque screen or only log a recoverable failure.

# Key Asset & Context
## Confirmed live configuration
- Neighborhood is dirty and loaded. Scene roots `Player`, `SirenHeadEnemy`, `Radio`, `Global Volume`, `Global Volume (1)` are present and active.
- `Main Camera (1)` is the active Base camera; post-processing is enabled and mask includes Default layer. Both Volumes are global on Default, weight 1, priority 0: presently both influence the view.
- Original profile: `Assets/Settings/SampleSceneProfile.asset` (Tonemapping, Bloom, Vignette; MotionBlur inactive). Also referenced by PC/Mobile pipeline assets and other scenes; do not modify it globally.
- Danger profile: `Assets/TestScene/Global Volume (1) Profile.asset` (active Vignette, DepthOfField, ChromaticAberration with overridden parameters). Despite its path, this is the supplied danger profile. Preserve authored values and profile persistence.
- Radio is an untagged prefab instance from `Assets/Imported/AK STUDIO ART/Radio/Prefabs/Radio.prefab`, with enabled non-trigger MeshCollider and no interaction/audio component. Add only scene-instance components; leave imported prefab unchanged and do not register Radio as a pickup/save object.
- InteractionSystem lives on `PlayerFollowCamera`, has assigned Player/hand/Animator/InteractionPrompt, but playerCamera is unassigned and falls back to Camera.main. Bind the actual `Main Camera (1)` locally for deterministic interaction behavior.
- `GameplaySaveAdapter.BlockInput` controls controller, PlayerInput and InteractionSystem.PersistenceInputBlocked. `GameSaveManager.BeginEncounter`, `HasEntryCheckpoint`, `RetrySceneEntry` already provide immutable checkpoint and retry ownership. Its retry coroutine restores under black and releases after FadeIn.
- `DeathRetryTransition.Initialize` currently sets alpha 0 and RetryEntryRoutine always calls FadeOut: a small backwards-compatible already-black entry is necessary to avoid double fade/flash after progressive blackout.
- Current Console has no errors in the retrieved entries, but existing negative-scale house BoxCollider warnings. Record as baseline; fixing unrelated house colliders is outside this feature.
- Dependency/dependent checks were performed via read-only AssetDatabase.GetDependencies and asset scans because no dedicated dependency tool is available. InteractionSystem is shared with CarScene/Garden/Test/recovery scenes. Changes must be optional/backwards-compatible; do not edit those scenes.

## New assets
- `Assets/Scripts/Enemies/PlayerInterferenceController.cs`: dedicated AudioSource ownership, validated exposure/blackout state, explicit Player/enemy/Volume/UI/save references, readiness/pause/capture gates, reset and guarded already-black retry.
- `Assets/Scripts/RadioInteraction.cs`: scene-instance proximity/obstruction eligibility and reset API; no separate input polling or prompt writer. Proposed API `CanInteract(Transform player, Camera view)` and `TryResetInterference(...)`, revalidated at action time.
- Neighborhood scene-owned `Player/InterferenceAudio` AudioSource and `InterferenceBlackout` Canvas/Image; reuse existing fade image/material asset where suitable without changing the asset globally.
- Optional focused tests under `Assets/Tests/Interference/` using installed compatible test support; no additional packages solely for this task.

## Existing assets to modify
- `Assets/Scenes/Neighborhood Scene.unity`: new components/UI/audio, Radio scene override, explicit camera/interaction references, initial mutually exclusive Volume state. Save current authorized unsaved edits intact.
- `Assets/Scripts/InteractionsSystem.cs`: optional Radio reference, unified F event processing/consumption, prompt precedence and shared pause/block gates. Preserve public pickup/drop/held-item APIs.
- `Assets/Scripts/Saving/GameSaveManager.cs`: add explicit already-black retry API or backwards-compatible overload routed through existing checkpoint routine. Validate prior to load, acquire input lock under existing encounter guard, skip only outgoing fade for this entry, retain opaque loading/restore/FadeIn and failure cleanup. Existing physical-capture overloads retain behavior.
- `Assets/Scripts/Transitions/DeathRetryTransition.cs`: optional initial alpha/already-black initialization and correct LoadingBlack phase; old callers still start at 0. Retain material/effect lifecycle and existing reveal/black hold.
- Other assets are read-only by default: PlayerCaptureSequence, SirenHeadAI, GameplaySettingsApplier, pause/settings/save DTOs, original/danger profiles, supplied wav/import settings, imported Radio prefab, shared fade assets and PlayerInput asset. If an additional integration edit proves necessary, inspect impact and revise scope before making it.

# Implementation Steps
1. **Recheck impact and current wiring.** Dependency: approval of this amendment. Reinspect all existing edit targets/dependents and live references before edits, preserve dirty Neighborhood, snapshot Volume enabled/weight states and AudioSource configuration, inspect overlay sorting and runtime readiness. Confirm no previous interference system exists or stale automated cleanup is armed. Verify actual Radio collider occlusion and camera mask/profile override persistence; no global pipeline changes.
2. **Implement accumulator and dedicated audio.** Dependency: Step 1. Create PlayerInterferenceController with normalized exposure, Inspector bounds/tooltips, grouped references/runtime diagnostics, baseline initialization and a separate looped 2D source. Gate scaled timers on readiness/pause/block/capture, handle missing references without exception spam, and implement atomic Radio reset with no stale blackout coroutine.
3. **Implement reversible view state.** Dependency: Steps 1–2. Wire exclusive Volume enabled/weight switching without editing profile assets. Create the full-screen non-intercepting black overlay, ramp alpha only at maximum, exact reset/endpoints, restore only controller-owned state on disable/failure. Process Radio reset before blackout commitment in a deterministic frame phase so one accepted press can cancel a pending full-black transition.
4. **Integrate Radio and F ownership.** Dependency: Steps 1–3. Add RadioInteraction to the existing Radio instance, calculate bounded proximity/occlusion eligibility, wire optional Radio reference and actual camera on InteractionSystem. Replace F polling with a managed event-driven InputAction, consume Radio press before pickup/drop, add exact `Press F` prompt precedence while respecting HUD/tooltips/pause/block. Never reparent/pick up Radio or drop held items on Radio use; no persistent-ID changes.
5. **Implement opaque checkpoint handoff.** Dependency: Steps 1–4. Add backwards-compatible initial-alpha support and an already-black retry entry to the persistent coordinator. At full black validate checkpoint/destination, claim encounter once, block input/HUD, initialize persistent cover at alpha 1 before releasing scene cover, skip redundant FadeOut and use existing restore/hold/FadeIn completion. Keep opacity through outgoing destruction and restoration; suppress retry autosave as before. Recover without clearing another encounter's locks or modifying saves if validation/load/restore/overlay fails.
6. **Wire Neighborhood and preserve other systems.** Dependency: Steps 2–5. Assign the supplied clip, two Volumes, Player/enemy/Radio/collider, interaction/prompt/camera/save adapter and established fade assets. Serialize tuning defaults and normal initial Volume state. Verify physical capture wins contention cleanly, direct scene play and menu/save load bootstrap correctly, and partial blackout still allows Radio and ordinary pause.
7. **Focused tests and save.** Dependency: Steps 2–6. Compile incrementally, run bounded isolated state/input/retry tests and actual Game View audio/Volume/partial-black/full-black checks. Protect user saves with an isolated test directory; never delete them or introduce deferred cleanup hooks. Save intended assets and authorized scene edits, remove temporary diagnostics safely and report evidence versus unperformed checks.

# Verification & Testing
- On fresh entry, static loops continuously at baseline, original Volume exclusively active, danger inactive and alpha 0. Existing footsteps, chase music, enemy effects and pause/menu audio remain unaffected; music slider does not scale static.
- Stay inside radius for half configured time: exposure approximately 0.5 and volume halfway between baseline/max. Leave radius: exposure holds. Reenter: accumulation resumes. Test boundary/vertical separation and 30/60/120 FPS where available; no distance-based instant jump or decay.
- Maximum is Inspector-adjustable. Invalid/NaN/negative settings, baseline >= maximum, zero time/radius and missing references fail safely/clamp with clear diagnostics, no divide-by-zero or repeated logs. Inspector changes do not produce an unintended retry.
- Just below maximum: normal Volume and no darkening. At maximum: danger Volume applied and gradual alpha begins in the same frame. Game View confirms actual effect and alpha near 0.5, then exact 1; bright sky/HUD cannot remain visible at full black. Shared profile assets/default pipeline settings remain unchanged.
- Near Radio with view looking away: prompt appears if within range/unobstructed. Outside range/behind solid wall/floor: no prompt or reset. HUD/tooltips off hides prompt but F still works when otherwise eligible. Pause/load/capture suppress action and clear buffered presses.
- Radio F during accumulation or partial blackout resets baseline, normal Volume and exact transparent alpha immediately. Held flashlight stays held/on/off as before; no pickup, drop, door toggle or second action from the same F. Hold F generates one press, repeated presses and repeated enable/disable do not duplicate listeners.
- Radio use on the final reversible blackout frame wins if accepted before retry commitment. After commitment input is blocked and exactly one retry occurs; physical capture/intro/load never run a second competing blackout or scene load.
- Pause mid-accumulation/mid-blackout freezes timers; audio remains looping. Resume continues, focus loss cannot accumulate time or replay stale F. Disable/destroy/missing enemy or Radio recovers owned visual/audio state without altering external inactive objects or capture ownership.
- Full black restores scene-entry player pose/pitch/doors/items/held-light state, not latest manual-save or death pose; exposure is baseline afterward. Persistent opaque cover prevents flashes; reveal completes before HUD/input/AI unlock. Manual/autosave bytes, persistent IDs/counts and other visited scenes remain unchanged; no retry-entry autosave.
- Repeated interference retries and physical-capture retries, direct Neighborhood play, Continue/selected save load, New Game and menu return show no duplicate AudioSources/overlays/listeners, stuck Busy/Encounter state or changed death-fade semantics. Other scenes have no new Radio UI/feature by default and existing F pickup/drop remains functional.
- Test two window sizes for full coverage/readability, capture actual rendered evidence and check Console against existing collider warnings. Do not claim standalone/audio perceptual quality/all-FPS checks passed unless performed; avoid the previous forced-domain-reload/long-test timeout workflow.

# Neighborhood Gameplay Parity — CarScene and GardenScene Amendment

Only this amendment is proposed for the current request. It supersedes earlier exclusions of interference/Radio from other scenes and extends existing enemy/player functionality to CarScene and GardenScene. Preserve completed work; do not rebuild environments or rerun earlier feature implementations.

# Project Overview
- Game Title: Existing FINAL-PROJECT; no branding change.
- High-Level Concept: Preserve first-person horror exploration in the existing road and garden environments while making their Player, Siren Head and Radio/interference systems function like Neighborhood Scene.
- Players: Single player.
- Inspiration / Reference Games: Assets/Scenes/Neighborhood Scene.unity is the functionality reference; existing Siren Head creature.
- Tone / Art Direction: Preserve destination scenery, lighting and placements except for the two Volume states required by interference. No environment or sky redesign.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape canvases; verify 1920x1080 and smaller window.
- Render Pipeline: URP PC_RPAsset, Unity 6000.3.8f1; installed URP 17.3.0.

## Selected Approach
User selected **Repair existing objects** and **All radios reset**. Reuse destination Player/enemy/UI objects, copy relevant source configuration and missing dependencies, remap every scene reference and remove only confirmed redundant gameplay owners. Replacing all objects with source clones would yield a closer hierarchy match but risk losing local placements, references and customizations. Repairing preserves local design while giving equivalent behavior. Multiple recovery stations support player agency (SDT) but reduce sustained horror pressure (MDA); retain source tuning initially rather than silently compensating with faster interference.

# Game Mechanics
## Core Gameplay Loop
Move/explore/interact → encounter roaming Siren Head → chase by sight or unobstructed proximity → break awareness → last-seen investigation/search → resume roaming. Physical capture plays the player's Humanoid collapse, fades to black, restores the **current destination scene's entry checkpoint**, and reveals with controls restored.

Distance-based interference accumulates near the enemy, holds outside its radius, switches normal/danger Volumes at maximum, and gradually blacks out. An eligible nearby Radio F press resets exposure, static to baseline, both Volume states and transparent view without dropping a held object. Full black requests the existing already-black entry retry, not player death. Physical capture and blackout must not start competing retries. Pause freezes gameplay timers; reset is unavailable during pause, capture, restoration or committed retry.

Copy actual current Neighborhood tuning, not historical proposed defaults. Confirmed source interference values are baseline 0.5, maximum 1, influence radius 100, time to maximum 40 seconds, blackout 10 seconds, reveal 1 second and black hold 0.2 seconds; Radio radius 3. Source capture has death speed 1, blend 0.15, post-animation delay 0.5, fade-out/in 1 second and black hold 0.2. Re-read latest source before applying. Preserve existing destination spawn/Radio placements if grounded, reachable and safe; adjust only invalid spawn/waypoint placement and document it.

Enemy remains outdoor-only: CarScene uses its roadway, not Floor shortcuts; Garden uses accessible outdoor ground, excluding building interiors/roofs and fence crossings. Use existing fitted creature dimensions and behavior, not a new AI/rig. Enemy and exposure state reset on entry/load/retry as in source; do not add AI, weather or interference fields to save data.

## Controls and Input Methods
Project Settings inspection confirmed activeInputHandler = 2 (Both). Preserve event-driven PlayerInput/StarterAssetsInputs and InputSystemUIInputModule. Keep the existing unified event-driven F interaction path; add no second Radio listener. Keep one effective movement/look/interaction owner, one intended gameplay camera/listener and one EventSystem per destination. Copy source player movement, grounding, Animator, head/hand/camera settings where needed for parity while retaining destination pose and model/Avatar compatibility. Do not edit shared StarterAssets input assets or controller code merely to remap references.

# UI
Reuse existing uGUI HUD, pause/settings and save dialogs, repairing local bindings rather than rebuilding them. CarScene has two PauseMenuManager/GameplaySettingsApplier sets and two InteractionSystem instances; retain one correctly connected set and disable/remove redundant copies only after incoming-reference checks. Garden has its own existing pause/settings/interaction/save wiring to preserve.

```text
Gameplay: existing first-person view + crosshair + one InteractionPrompt
Nearby eligible Radio: Press F
Pause: Resume | Save Game | Settings | Exit
Maximum exposure: danger Volume + gradual full-screen black
Capture: player collapse → fade/load/checkpoint restore → reveal
Blackout: opaque handoff → checkpoint restore → reveal
```

Use one reversible InterferenceBlackout Canvas/Image and one dedicated looping 2D InterferenceAudio source per player; one visible error text if required. Preserve full-screen coverage, source sorting, non-blocking reversible overlay, settings/HUD/tooltip rules and readable modal captions. Persistent retry overlay remains owned by existing coordinator. Do not duplicate global fade managers.

# Key Asset & Context
## Verified current state
- Source and destination dependencies/dependents queried with the dependency tool; further existing edit targets require impact checks before implementation.
- CarScene is loaded and clean at inspection; preserve loaded scene setup and recheck dirty state before edits/saves. Neighborhood and Garden were inspected from serialized assets, not opened or modified.
- Both CarScene and GardenScene are already enabled in EditorBuildSettings. Preserve build list and MainMenu → VideoIntro → Neighborhood sequence; no build-list or new travel mechanic required.
- CarScene already has SirenHeadEnemy, PlayerInterferenceController and numerous RadioInteraction components. Radio components have collider references but missing interference/player-body/interaction bindings. Interference lacks enemy/HUD/Volumes/blackout/error references; save adapter has missing pause and a large array of null copied object entries. Capture lacks player/save/camera/HUD/interaction bindings. Player camera target requires live reference verification. Two pause/settings sets and two interaction systems require consolidation.
- Garden has SirenHeadEnemy with unbound capture fields, existing player/interaction/pause/settings/save adapter and one registered persistent object. Dependency inventory shows radio model, but no attached RadioInteraction or PlayerInterferenceController was found in serialized scene. Garden still references the older StarterAssetsThirdPerson Animator Controller; use compatible project-owned NeighborhoodPlayer controller for PlayerDeath.
- CarScene has a NavMeshSurface with physics geometry, agent type -1372625422, voxel size 0.2 and data at Assets/Environment/CarScene/SirenHeadRoadNavMesh.asset, plus modifiers. Source has its own surface, modifiers and interior exclusions. No Garden NavMeshSurface/modifier was found in serialized inspection. Prefab-inherited agents and navigation components must be inspected live before adding anything; no baked coverage/path success has yet been verified.
- Source normal/danger profiles are Assets/Settings/SampleSceneProfile.asset and Assets/TestScene/Global Volume (1) Profile.asset. CarScene already references both; Garden references normal profile only. Use separate destination Volume components with these shared profiles; never modify shared profile assets.
- SaveGameData.SupportedScene currently permits Neighborhood, Farm and CarScene, not Garden. Validate caps snapshots at three. GameplaySaveAdapter.ValidateObjects rejects unsupported scenes, null registrations and missing player/interaction/pause; entry checkpoint registration therefore requires repair rather than bypassing validation.
- InteractionSystem currently serializes one optional RadioInteraction. Supporting all placed radios requires a backwards-compatible explicit Radio collection and common selection/eligibility path, not project-wide per-frame discovery.
- Latest retrieved Console contains wheel mesh self-intersection warnings and a Curl connection-reset error. These are baseline evidence, not diagnosed gameplay failures to fix in this transfer.

## Assets to modify
- Assets/Scenes/CarScene.unity: repair existing player/camera/head/hand/interaction/HUD/pause/save/enemy/capture/interference/Radio bindings, consolidate redundant gameplay owners, complete local object registrations, validate road navigation/waypoints. Retain scenery, rails, Floor and Radio placements.
- Assets/Scenes/GardenScene.unity: repair existing Player/enemy/capture references, assign compatible player death controller, add missing interference/audio/blackout/error/danger Volume and Radio component(s), complete save/checkpoint integration, outdoor navigation and local patrol points. Preserve existing flashlight/registered object identity.
- Assets/Scripts/InteractionsSystem.cs: retain legacy singular radioInteraction field for existing scenes and add explicitly serialized additional radios or equivalent migration-safe collection. One shared resolver chooses the nearest eligible radio (stable authored-order tie break), used by prompt and action with eligibility revalidated at consumption. Consume one F press exactly once before pickup/drop; no duplicate callbacks or scans. Null/empty collection retains existing behavior.
- Assets/Scripts/Saving/SaveGameData.cs: add exact Garden path to SupportedScene and increase maximum bounded snapshot count to four to include all existing supported scenes. Preserve version/schema and existing record validity. Inspect repository/manager consumers for duplicated bounds/path assumptions before changes.
- Shared save/enemy/capture/interference/Radio/settings/pause scripts remain read-only by default; modify only the two identified shared scripts above. Any additional integration change must be narrow, backwards-compatible, impact-reviewed and described before proceeding.

## Reuse / new assets
- Reuse Assets/Characters/SirenHead/SirenHeadEnemy.prefab, its existing Generic Animator/locomotion assets, Assets/Characters/Player/NeighborhoodPlayer.controller and Animations/Player_Death.anim, established static/chase/footstep clips, existing source UIFadeImage/UIAlphaFade assets. No regeneration, re-rigging or shared prefab/controller edits.
- New Garden baked navigation asset: Assets/Navigation/GardenSceneSirenHeadNavMesh.asset; scene-local surface/modifiers/interior exclusions and patrol markers. Use existing fitted creature agent type, no new type unless existing geometry truly requires an approved change.
- Reuse existing CarScene baked data after verification; update it only if coverage/geometry requires rebaking and after checking its dependents. Never copy Neighborhood baked data into another scene.
- Missing UI/audio/Volume dependencies are destination scene-owned objects/components, copied with reference remapping; no new universal gameplay prefab or global services needed.
- Persistent IDs: retain valid existing destination IDs. Discard invalid copied null registration slots, audit actual saveable doors/pickups, assign only missing scene-instance IDs with existing explicit validator, and do not register radios/enemy as pickups. Never copy Neighborhood's world registration array.

# Implementation Steps
1. **Recheck source, destinations and impact.** Read this amendment; inspect dependencies/dependents for every edit target, live prefab overrides, player Avatar/Animator/controller/head/hand/camera, radio colliders/placements, Volume camera masks, pause/save events and navigation agents/surfaces/obstacles/links/modifiers. Inventory canonical versus duplicate owners and all incoming references before removing copies. Snapshot dirty states and protect unrelated user edits. Read current InteractionSystem F/UI methods and repository/manager save validation/entry retry logic. Dependency: user approval.
2. **Repair player and shared local UI wiring.** In CarScene and GardenScene retain one working Player/input/interaction/camera/listener/EventSystem chain; bind source-equivalent locomotion/settings, compatible NeighborhoodPlayer Animator, camera target, hand point and prompt. Repair pause/settings/save panels and UnityEvents; consolidate CarScene duplicates without losing surviving event targets. Preserve destination transforms and existing correct functionality. Dependency: Step 1.
3. **Enable valid destination checkpoints.** Extend SaveGameData for Garden/four snapshots, repair adapter player/interaction/pause and actual world registrations, retain existing IDs and add missing IDs only to local saveables. Confirm normal entry creates immutable checkpoint after initialization and retry bypasses visited-state restoration/autosave as source. No user save deletion or accidental test autosave writes. Dependency: Steps 1–2.
4. **Validate destination navigation.** Inspect existing fitted prefab agents, patrol arrays and protected interiors. Validate CarScene road NavMesh and complete paths around bends/ramps/underpasses at correct elevation; retain/rebake only if necessary. Create Garden outdoor-only surface/data/patrol points with real obstacle collision and building/fence exclusions. Do not add automatic jump/drop links, shrink creature clearance, or use source coordinates/data. Retain enemy pose if safely on connected navigation and separated from player; relocate only if invalid. Dependency: Steps 1–3.
5. **Wire Siren Head and capture parity.** Bind destination player/body/sight targets/interaction/adapter/waypoints/interior protection on existing enemy; retain internal visual/audio/anchor references. Copy current source tuning, player death clip, camera/brain/inputs/Animator/HUD/interaction/fade references on capture. Verify ready/checkpoint and encounter ownership before attacks. Dependency: Steps 2–4.
6. **Support multiple radios without duplicate input.** Add migration-safe explicit Radio collection to InteractionSystem; retain singular field fallback and one F event owner. Use shared eligibility selection for prompt and reset, revalidate before action, consume once, ignore missing/disabled radios safely and avoid per-frame scene searches. Bind every existing CarScene RadioInteraction and all actual Garden Radio instances to the correct player/body/interference/interaction with source collider radius/mask settings. Dependency: Steps 1–3.
7. **Complete interference dependencies.** Repair CarScene controller and create Garden counterpart using current source tuning and dedicated static source. Bind normal/danger Volume components and explicit camera post-processing coverage; create/remap source-equivalent full-screen reversible blackout/error objects where absent. Bind existing retry assets/adapter/enemy/HUD. Initialize normal-only state, alpha zero and baseline static; avoid competing overlay, audio, death or pause ownership. Do not modify global rendering/profile defaults or sky. Dependency: Steps 2–3 and 5–6.
8. **Focused validation and save.** Compile after shared script edits, inspect missing/cross-scene references and object counts, test each scene directly and via saved-game reconstruction. Protect saves using isolated verification storage; no automatic cleanup hooks or forced-domain-reload loops. Capture fresh multi-angle Scene View images for placed enemy/radio/navigation context and Game View evidence for actual prompt, distortion, blackout and collapse/fade; revise same transfer setup for fixes, maximum three capture/fix rounds before asking for guidance. Save only intended scenes/scripts/navigation changes and restore prior editor scene setup. Dependency: Steps 2–7.

# Verification & Testing
- Exactly one effective player/input/interaction/HUD/pause/settings/save chain, one enemy, one gameplay camera/listener and EventSystem per scene. No source scene-object references remain. Source Neighborhood asset and all shared imported/prefab/profile/controller assets unchanged.
- Player walking/look/jump/grounding/camera, held-item pickup/drop/use, independent sensitivity/settings, HUD/tooltips and pause/save/Exit work in both scenes. Preserve Garden flashlight and local state identity.
- All CarScene radios reset one exposure level; moving between radios produces correct Press F eligibility even looking away. No prompt/use through solid walls/floors; no pickup/drop on Radio F, including with held flashlight. Overlapping eligible radios reset once, stable tie break; disabled/missing radios skipped. Neighborhood's singular Radio and scenes with no radio remain functional.
- Match current source interference parameters: accumulate only within 100-unit radius; after 20 seconds exposure approximately half and audio halfway between 0.5/1; leaving holds progress. At maximum normal/danger Volumes switch and 10-second reversible blackout begins. Radio resets audio/view immediately; pause freezes timers; full black starts one already-black entry retry with no death animation or reveal flash.
- NavMesh type matches agent, spawn samples correctly and representative patrol/chase paths are complete. No road Floor shortcuts/wrong-elevation projection, interior/roof access or fence crossings. Garden paths traverse accessible outdoors around buildings. Verify actual Walk/Run/Idle poses and agent motion, not asset presence alone.
- Sight/proximity detection, fixed last-seen investigation/search, pause suspension and unobstructed close capture match source. Capture plays PlayerDeath while enemy idles, restores destination entry state under opaque black and releases camera/input/HUD only after reveal. Physical capture versus interference contention yields one encounter/retry.
- Garden saved data and four-scene snapshots validate; older one/two/three-scene records remain valid. Entry checkpoints, repeated death/blackout retries, Continue/selected slots and Exit autosave use the correct destination. Manual/autosave bytes are not overwritten by retries; no enemy/interference persistence added. Existing destination records whose world IDs no longer match must fail visibly, never be silently rewritten or deleted.
- Repeated direct play, menu returns, scene reloads and missing-reference failure paths produce no duplicated listeners/sources/overlays, stuck Busy/Encounter or input locks. Full-screen blackout/reveal and readable dialogs verified at two window sizes when available.
- Console compared against baseline wheel/Curl issues; report new gameplay/compiler defects separately. Windows standalone, all-FPS and full-road traversal checks remain unverified unless performed. Record actual test evidence rather than claiming copied components prove functionality.

# DoorKey Pickup and Neighborhood Door Lock Amendment

Only this amendment is pending approval for this request. Preserve completed work and do not repeat previous amendments.

# Project Overview
- Game Title: Existing FINAL-PROJECT; unchanged.
- High-Level Concept: Existing first-person horror exploration. Obtain DoorKey in CarScene to gain access through the specifically selected Neighborhood exterior door; restarting CarScene removes the key and makes it available again.
- Players: Existing single player.
- Inspiration / Reference Games: Existing project and its interaction systems; no new references requested.
- Tone / Art Direction: Preserve existing key mesh, horror scenery and HUD styling.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape, responsive gameplay HUD.
- Render Pipeline: URP PC_RPAsset, Unity 6000.3.8f1.

## Confirmed Decisions and Approach
- User selected **Include in saves** and **Restarts only**. Ownership persists across ordinary scene travel, Save/Load, and application restart through a loaded record. Ordinary revisits to CarScene keep the collected key hidden. Actual CarScene restart, including physical-capture and interference-blackout entry retry, removes ownership and restores the key at its authored location, even if the entry checkpoint had ownership.
- Use a dedicated logical key flag in GameSaveManager, persisted alongside the existing run data, plus a scene-owned pickup component. Do not make the key a held object or register it as an ordinary PersistentSaveId pickup. This avoids dropping a flashlight, changing the existing strict world-ID counts, and mixing inventory ownership with scene active-state snapshots.
- Alternatives considered: run-only transient flag is simpler but cannot retain saved progress; ordinary held-object pickup reuses physics but conflicts with single-hand behavior and cross-scene ownership. Selected saved logical ownership supports continuity/player competence (SDT) while preserving the gated exploration loop (MDA).
- No new travel mechanic, inventory UI, key-use hotkey, scene rerouting, build-list change or unrelated lock is included. The key is not consumed when opening this door.

# Game Mechanics
## Core Gameplay Loop
Approach DoorKey in CarScene → eligible nearby prompt displays Press F → press F once → ownership becomes true and DoorKey disappears → travel through existing mechanisms to Neighborhood → use existing E door interaction to open/close the selected door. Without ownership, that door remains physically closed and cannot open through any normal public opening API. Restart CarScene → ownership false, key back at original transform; subsequent Neighborhood visits are locked again until reacquired. New Game starts without ownership; loading a save restores its own flag, never leaks ownership from another run.

## Controls and Input Methods
- Verified PlayerSettings activeInputHandler = 2 (Both). Reuse InteractionSystem's existing lifecycle-managed event-driven InputAction bound to Keyboard F; no additional input listener or legacy polling.
- Key eligibility uses an explicitly assigned Player/body/key collider, configurable initial proximity radius 3 m and solid-obstruction mask. Aiming is not required, consistent with approaching the requested object and existing Radio behavior. Use collider ClosestPoint for range and body-level line-of-sight; ignore player, held-item and key endpoint colliders/triggers. Fail closed if a bounded hit buffer saturates.
- Same eligibility method powers prompt and action; revalidate at consumption. Suppress key interaction during pause, focus loss, save restoration, capture, intro/retry transitions or other blocked gameplay.
- Explicit priority: eligible DoorKey first, then eligible Radio, then normal F pickup/drop. One F press is consumed once; picking up the key never drops/reparents/toggles a held flashlight or activates a Radio simultaneously. Existing E and right-click behavior remains unchanged. Guard generic PickUpObject against treating DoorKey as a hand item.

# UI
Reuse the existing uGUI UICanvas/InteractionPrompt and its current font/layout; InteractionSystem remains the sole prompt writer. No extra Canvas, TMP font assets or EventSystem.

```text
Near eligible DoorKey:     Press F
After key pickup:          normal interaction help (no key prompt)
Selected locked door:     Locked — DoorKey required
Selected unlocked door:   existing [E] Abrir / Cerrar Puerta
```

Key/locked-door prompts respect ShowHUD, ToolTips and pause/capture visibility. Key prompt takes precedence even with an object held; the selected door's locked feedback should be shown when targeted rather than advertising that it can open. Hidden HUD/tooltips never disable otherwise eligible interaction.

# Key Asset & Context
## Verified existing assets and identity
- Assets/Scenes/CarScene.unity contains a root prefab instance named DoorKey, sourced from Assets/Imported/Gabies_Assets/Keys/Prefabs/Simple_02.prefab. Authored position (-239.561, 1.433, 257.892), rotation Euler approximately (-33.015, 105.946, 0), scale (4.0582, 4.0582, 4.0582). Serialized instance has no added components; source prefab has no Collider/Rigidbody/MonoBehaviour. Add a fitted scene-instance interaction collider; do not move or replace the key/imported prefab.
- Exact requested Neighborhood door: NewNeighborhood/House6/Floor_0/Doors/Door_1B/Exterior_Door, supplied instance ID 7584710, PersistentSaveId **04f129cafbe5426cb18a4aedd53b606f**, world position approximately (51.34, 2.14, 49.45). Multiple active objects have identical hierarchy names; bind by this persistent ID plus scene identity and recorded position, not GameObject.Find(path/name), and never persist the transient instance ID as identity. Other matching House6 doors remain unchanged.
- Selected door has InteractiveDoor, BoxCollider, renderer and existing persistent ID. Its authored local Y rotation is 119.039 degrees; current InteractiveDoor.Awake infers this as open despite serialized isOpen false. Ensure opt-in locked-door initialization closes it correctly without globally changing ordinary door initialization/rotations. Verify closed panel and child handle alignment against its frame/collision.
- InteractiveDoor currently exposes ToggleDoor(), Open(), Close(), Use(GameObject), RestoreSavedState(bool); none checks ownership. All opening paths including restoration must honor the optional lock, with default behavior unchanged for other doors.
- InteractionSystem already has unified F buffering, Radio precedence, prompt settings, player/hand/camera/Animator references, and persistence/capture gates. Its held-object help currently takes priority over door help; scope any priority change to key/locked feedback only.
- GameSaveManager persists across scenes, owns Save/Load/BeginFreshRun/TravelToGameplay, visited world states and entry-checkpoint retry. SaveGameData version 1 has root metadata and four bounded scene snapshots. GameplaySaveAdapter validates exact world-ID counts and restores doors after scene startup.
- Neighborhood is currently loaded and clean. CarScene was read from disk; recheck live dirty state and preserve scene setup before edits. Dedicated dependency tool is unavailable; AssetDatabase dependency/dependent inspection confirmed shared interaction/adapter impacts in Neighborhood, CarScene, GardenScene, TestScene and recovery assets. Those other scenes stay unedited. Retrieved Console error entries are empty; do not treat that as runtime verification.

## Existing assets to modify
- Assets/Scenes/CarScene.unity: add DoorKeyPickup and fitted interaction collider to the existing DoorKey instance; wire canonical InteractionSystem, key visual/root and Player body. Keep authored placement and all existing systems/scenery.
- Assets/Scenes/Neighborhood Scene.unity: enable the optional DoorKey requirement on the exact door identified above, retain its persistent ID and other scene configuration.
- Assets/Scripts/InteractionsSystem.cs: optional explicit DoorKeyPickup reference, shared eligibility and F consumption before Radio/hand items, prompt precedence, generic pickup protection. Null default retains every other scene's behavior.
- Assets/Scripts/InteractiveDoor.cs: opt-in requiresDoorKey field/read-only IsLocked, checks on ToggleDoor/Open/Use/restoration, lock-change reconciliation and closed initialization. Safe event subscription cleanup; no per-door scene scans. Close always remains legal. Ownership changes force the gated door closed when required, but acquiring the key does not automatically open it.
- Assets/Scripts/Saving/SaveGameData.cs: additive root bool hasDoorKey with absent-field default false for older version-1 records; retain all existing metadata/world validation and ID counts. No new SavedObjectState kind or key registration.
- Assets/Scripts/Saving/GameSaveManager.cs: canonical ownership getter/change notification/acquisition API; include flag in commits, apply validated load ownership before world restore, clear on accepted new run, capture ownership with entry-checkpoint context, and reset specifically on successful CarScene restart restoration. Preserve existing Busy/Encounter/fade/input/autosave semantics.

## New asset
- Assets/Scripts/DoorKeyPickup.cs: scene-owned eligibility and acquisition component with explicit references, radius/mask/tooltips, original transform capture, and reconciliation of visual/collider state to logical ownership. Keep the component/owner alive while hiding its visual subtree and interaction collider, or use an external active scene owner with a root reference; never rely on a disabled component to receive ownership changes. Restoration resets original transform when making the key available. No separate input/prompt Update writer and no runtime name-based scene searches.
- Optional focused compatible tests under Assets/Tests/Interactions/; no package installation solely for testing.

# Implementation Steps
1. **Recheck exact identity, references and impact.** Dependency: approval. Inspect dependencies/dependents and serialized incoming references before each existing edit, canonical CarScene interaction/player/camera/UI/save adapter and key renderer bounds/collider fit. Resolve the requested door by scene plus ID 04f129cafbe5426cb18a4aedd53b606f; record frame/hinge/closed geometry. Preserve unsaved work and seek confirmation before saving newly discovered unrelated changes. Check current travel/restart calls; do not add new travel mechanics.
2. **Implement saved logical ownership.** Dependency: Step 1. Extend root save DTO additively and GameSaveManager's acquisition/capture/load/new-run paths. Keep version-1 legacy reads (missing flag false), atomic repository commits unchanged, no automatic disk writes on pickup. Set ownership before adapter restores door state; reconcile key visibility before input/AI becomes ready. Ensure intentional loading between different runs replaces, rather than ORs, ownership. Capture an immutable entry ownership value separately from existing world checkpoint JSON for ordinary retries in other scenes.
3. **Implement restart-specific reset.** Dependency: Step 2. On CarScene retry, override entry ownership to false after validated restoration but before release/reveal. Reset the active session/entry key value so repeated retries remain false; keep visited CarScene visibility derived from the canonical flag, not stale active-object records. A failed pre-load restart must retain ownership; failures after reconstruction must reconcile to a usable coherent state. Ordinary travel/revisit never clears it; retries in Neighborhood/Garden use their entry ownership alongside world checkpoint, not the CarScene-specific reset. Existing manual/autosave records remain untouched by retries, so explicitly loading a pre-restart save legitimately restores its saved ownership.
4. **Implement pickup eligibility and presentation.** Dependency: Steps 1–3. Add DoorKeyPickup with safe active ownership, explicit collider/player references, cached original transform, bounded proximity/obstruction checks and acquire-once logic. Add scene-local fitted collider and hide renderer subtree/collider immediately on acquisition without holding physics/animation. Show at authored position on reset; initialize from loaded/current ownership. Subscribe/unsubscribe safely and avoid stale state across repeated/no-domain-reload play.
5. **Integrate F and prompts.** Dependency: Step 4. Add optional key reference to the canonical CarScene InteractionSystem. Resolve/revalidate key first and consume F exactly once before Radio/drop logic; generic pickup must reject key objects. Reuse existing prompt writer and settings/block gates, displaying exact Press F before held-item help when eligible. Restore normal help immediately after acquisition/leaving range; no separate input action or prompt owner.
6. **Guard the selected door.** Dependency: Steps 2–3. Add optional lock to InteractiveDoor with compatible defaults. Close the selected authored-open panel during initialization while retaining correct hinge rotations. Block ToggleDoor/Open/Use and saved-open restoration while ownership false; update lock feedback in InteractionSystem. Reconcile when ownership is lost/loaded so prior visited open state cannot bypass the requirement; key acquisition only enables normal E opening. Wire only the exact scene instance, never all duplicate paths or imported prefabs.
7. **Focused verification and save.** Dependency: Steps 2–6. Compile incrementally, inspect local references/events and unchanged persistent-ID arrays, test pickup/lock/load/travel/retry and actual HUD/door collision. Protect user save files with isolated verification storage, no deferred cleanup or forced-domain-reload loop. Save only intended scripts and two scenes, restore original Editor scene setup, remove temporary diagnostics safely and report actual evidence versus outstanding checks.

# Verification & Testing
- Fresh direct/new-run play: selected door is visually/physically closed and rejects E, ToggleDoor, Open and Use without key. Other identically named doors work unchanged. Legacy save with this door open but no key cannot restore it open; check restored player/collision safety.
- Approach DoorKey: Press F appears within configured collider range without aiming; absent outside range/behind solid wall/floor, during pause/focus loss/capture/load. ShowHUD/ToolTips off hides prompt but not allowed pickup. Collider fit makes this small mesh usable without blocking road walking.
- F once grants key and hides all key visuals/interaction collision immediately. Held flashlight parenting/on-off/animation is unchanged, no Radio reset or drop from the same F, held F does not repeatedly acquire. Re-enable/revisit does not duplicate listeners or respawn an owned key.
- Key obtained in CarScene → ordinary travel to Neighborhood → selected door opens/closes via existing E. Key is not consumed. Normal return to CarScene leaves it hidden. No new travel route is promised; test using an existing transition or isolated coordinator travel invocation if not user-accessible.
- Save before pickup/load yields false + visible key + locked door. Save after pickup in CarScene or Neighborhood/load after application restart yields true + hidden key + normal saved door state. Loading records from different runs never leaks ownership; old version-1 records without hasDoorKey default to false and remain valid. Existing world registrations/counts unchanged.
- Physical death and interference blackout restart in CarScene both remove key and show it at (-239.561, 1.433, 257.892) with original rotation/scale. Repeat with entry checkpoint false and true, after manual save and after ordinary revisits; reset overrides entry true. Retrying other scenes follows their entry ownership/world state, without blanket CarScene reset. Failed restart validation does not silently remove ownership.
- Restart does not overwrite existing manual/autosave bytes; later explicit load of an older key-owning record restores it intentionally. Saving after restart stores false. New Game confirmation cancellation changes nothing; accepted new run starts false while preserving manual saves.
- Pause/save/Exit/Continue, Radio/interference, capture/retry fades, input/HUD/settings and other scenes remain functional. Lock reconciliation cannot reopen the door through visited-state restoration; acquiring key never auto-opens it. Verify frame/handle/collider alignment when closed and opening.
- Actual Game View prompt and disappearance, repeated reloads, console/compiler checks and focused isolated data tests. Report standalone/all-resolutions/end-to-end travel as unverified unless performed; no destructive user-save cleanup, package/shared-imported edits, build-list or input-backend changes.

# Letrero → NeighborHoodToGarden → GardenScene Amendment

Only this amendment is pending approval for the current request. Preserve previous functionality; do not repeat earlier amendments.

# Project Overview
- Game Title: Existing FINAL-PROJECT; unchanged.
- High-Level Concept: Existing first-person horror exploration. Looking at Letrero and pressing F transitions from Neighborhood to the supplied garden cinematic, then to Garden gameplay after real video completion.
- Players: Single player.
- Inspiration / Reference Games: Existing project; no new references.
- Tone / Art Direction: Preserve existing scenery, sign, cinematic, HUD and established black alpha fades.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape UI; full-screen fade and aspect-fit video at different window sizes.
- Render Pipeline: URP PC_RPAsset, Unity 6000.3.8f1.

## Approach and Scope
User selected **Look at sign**, not proximity-only use. Use the canonical InteractionSystem's one event-driven F path and existing actual interaction range (currently 3.5 m). This deliberate interaction fits the existing exploration dynamics (MDA); the prompt communicates the action without adding a new input owner.

Two implementation approaches: parameterize/reuse the existing persistent cinematic sequence, or create an independent garden-only sequencer. Reuse is recommended because it already supports video first-frame readiness, retained final frame, restoration-aware UI fades and operation locking. An independent sequence isolates changes but duplicates lifecycle/transition code and risks competing with VideoIntroController's bootstrap. Use narrow backwards-compatible extensions of the existing sequence/controller and a scene-owned sign component; do not use BeginNewGame for this travel or reset the run.

# Game Mechanics
## Core Gameplay Loop
Explore Neighborhood → aim at unobstructed Letrero within interaction range → Press F → outgoing scene fades to fully black → load Assets/Scenes/NeighborHoodToGarden.unity under black → prepare/hold first decoded frame → fade in → play complete supplied video with embedded audio → actual loopPointReached completion → retain final frame and fade out → load Assets/Scenes/GardenScene.unity under black → finish gameplay initialization and any visited-state restoration → fade in → release controls/HUD/enemy/save locks.

Preserve the current run ID, DoorKey ownership, manual saves and previously visited world states. Capture Neighborhood before unloading through the save coordinator. Garden uses its existing ordinary-entry restoration/checkpoint/one-entry-autosave policy; the cinematic is not a supported save scene and creates no cinematic autosave. Existing Garden authored spawn or valid visited snapshot determines player placement; no new arrival teleport is requested.

## Controls and Input Methods
- Read-only settings inspection confirms activeInputHandler = 2 (Both); canonical PlayerFollowCamera InteractionSystem uses the event-driven Keyboard F action, Main Camera (1), Player and 3.5 m range.
- Add no second input action/listener or legacy polling. Optional sign integration is null by default in other scenes.
- Use a central camera ray/small spherecast consistent with existing targeting, but require unobstructed sign selection; do not inherit the door-priority scan's ability to find candidates past walls. Use identical selection for prompt and action; revalidate on F.
- Eligible targeted sign consumes F before Radio/pickup/drop. Never pick up the sign or drop a held flashlight; reject sign objects from generic PickUpObject. If sign activation succeeds, stop processing remaining E/right-click input that frame.
- Pause, focus loss, restoration, capture, interference retry and active intro/travel suppress sign use. Guard duplicate presses and competing transition owners atomically before capture/fade.

# UI
Reuse UICanvas/InteractionPrompt, its existing styling and ShowHUD/ToolTips/pause visibility rules. No new menu, skip button, EventSystem or post-processing fade.

```text
Looking at Letrero in range: Press F
Neighborhood → gradual black → black/loading → cinematic reveal
Video plays once → final frame fades to black → Garden loads/restores → reveal
```

Use existing UIAlphaFade.asset / UIFadeImage.prefab / UIFadeMaterial.mat with per-operation cloned effect/material and existing DeathRetryTransition overlay. Inspector defaults: Fade Out 1 s, Fade In 1 s, Black Hold 0.2 s, matching established fades; loading/preparation adds to black duration. Overlay stays above all UI and survives unload, with exact opaque/transparent endpoints. Do not edit shared fade assets globally or death-retry behavior. Failures report visible status and recover usable input/cursor/UI without unexplained permanent black; never claim playback completed on decode error.

# Key Asset & Context
## Verified context
- Loaded Neighborhood is clean. Letrero is the root object at approximately (24.56, 1.26, 5.86), untagged on Default, containing Transform/MeshFilter/MeshRenderer only. Renderer extents approximately (0.23, 0.23, 0.58). Add a fitted scene-instance collider; keep imported Assets/Imported/Kiara/Letrero.fbx and appearance unchanged.
- All three requested scenes are enabled in EditorBuildSettings; no build-list edit needed.
- NeighborHoodToGarden already contains Canvas/VideoPlayer/VideoIntroController, VideoDisplay RawImage, aspect fitter and black background. Its controller has assigned display/fade/image references; video is non-looping with autoplay disabled and a target RenderTexture.
- Supplied garden clip is Assets/Videos/copy_CAA80AEF-5BCA-47A2-88C9-6911CCBB1D37.mov; target is existing Assets/Videos/VideoIntro.renderTexture. Inspect clip dimensions/audio during implementation, preserving source/import settings and avoiding a redundant VideoPlayer. Existing preparation/completion/error and audio mute logic can be reused.
- VideoIntroController.Start currently calls BeginDirectIntro with no scene arguments. NewGameSceneSequence defaults to VideoIntro → Neighborhood; uncoordinated direct garden cinematic play therefore targets the wrong route. Add scene-aware optional configuration with legacy defaults preserving VideoIntro/MainMenu behavior.
- NewGameSceneSequence.BeginNewGame calls BeginFreshRun; never call it from Letrero. Existing BeginIntroTransition/EndIntroTransition preserve run state and gate readiness but do not capture outgoing state themselves.
- GameSaveManager.TravelToGameplay captures the outgoing adapter but synchronously loads only supported gameplay destinations without fades. Do not use raw scene loading or this method to load the cinematic without a coordinated capture boundary.
- AssetDatabase dependency/dependent checks performed because the dedicated dependency tool is unavailable. InteractionSystem is shared with Garden/Car/Test/recovery scenes; VideoIntroController is used by both cinematics; NewGameSceneSequence is referenced by MainMenu/recovery assets. Runtime save manager references require code impact review, not just serialized dependencies.
- Current retrieved Console error entries are empty; this is not runtime verification.

## Existing files to modify
- Assets/Scripts/InteractionsSystem.cs: optional sign reference, unobstructed targeting/resolution, F consumption and prompt precedence; protect generic pickup and preserve existing key/Radio/door/held-item semantics elsewhere.
- Assets/Scripts/NewGameSceneSequence.cs: reusable explicit gameplay-cinematic entry (exact cinematic/gameplay paths and fade settings), persistent lifecycle, readiness checks and guarded cleanup. Keep BeginNewGame signature/confirmation/fresh-run semantics and default main-menu route unchanged.
- Assets/Scripts/VideoIntroController.cs: scene-aware direct-play bootstrap configuration with existing default route, use correct configured cinematic identity/destination; preserve controlled first-frame readiness, real completion, final frame/audio and cleanup.
- Assets/Scripts/Saving/GameSaveManager.cs: narrow validated begin/end gameplay-cinematic boundary, capture outgoing world state before destruction and acquire transition lock without BeginFreshRun. Reuse current readiness/autosave semantics without weakening save validation or resetting key ownership. Validate both destinations and fade prerequisites before mutation. Keep death retry, explicit save load and ordinary TravelToGameplay behavior compatible.
- Assets/Scenes/Neighborhood Scene.unity: scene-owned Letrero component/collider and canonical InteractionSystem binding, explicit destination/fade references. No sign save-ID registration or environment changes.
- Assets/Scenes/NeighborHoodToGarden.unity: configure exact current cinematic path and Garden destination/direct-play mode. Repair video wiring only if inspection shows a defect.
- GardenScene is read-only by default: use existing adapter/checkpoint readiness; if missing destination wiring prevents safe reveal, report and revise scope rather than disable validation.

## New asset
- Assets/Scripts/SignSceneInteraction.cs: scene-owned configuration/target eligibility/guarded start API for the explicitly assigned sign. No Update input polling or independent prompt writer. Add optional focused tests only within compatible installed test infrastructure.
- Existing transition/image/material/RenderTexture assets are reused unchanged; no packages, generated defines or imported prefab/source changes.

# Implementation Steps
1. **Recheck exact ownership and impact.** Dependency: approval. Reinspect live dirty state, incoming sign/scene references and every existing script's callers/dependents, coordinator readiness/events, interference/capture/HUD gates and video dimensions/audio. Preserve Editor scene setup and any newly dirty work; do not reload/revert or save unrelated changes without confirmation.
2. **Add save-safe cinematic boundary.** Dependency: Step 1. Validate destination availability, supported Garden path, current gameplay readiness and exclusive operation/fade ownership. Capture immutable outgoing Neighborhood snapshot before acquiring block state/unload; preserve run/key/visited data. Acquire Busy/transition/input/HUD ownership synchronously so another capture/blackout cannot win afterward. Expose a narrowly scoped coordinated entry/cleanup path; do not bypass strict adapter validation or write autosave just for leaving Neighborhood.
3. **Parameterize/reuse sequence and direct bootstrap.** Dependency: Step 2. Add explicit travel entry separate from BeginNewGame; bind exact requested paths, configured timing and one video instance. Keep persistent owner/overlay, check overlay usability and load failures, wait for first frame under black, reveal then begin audible playback, await real completion, fade retained final frame, load Garden, await successful validated initialization/checkpoint/restoration before reveal. Do not interpret Ready alone or a stale checkpoint from a prior scene as successful current restoration. Keep locks until fade-in completes, then restore input/HUD/cursor ownership and dispose cloned resources. Configure direct NeighborHoodToGarden play to Garden without fresh-run reset; preserve original VideoIntro direct-play defaults.
4. **Implement aimed sign/F integration.** Dependency: Steps 1–3. Add SignSceneInteraction/fitted collider to Letrero only, target at canonical camera/range with solid obstruction checks. Wire optional sign reference on existing InteractionSystem. Share eligibility for Press F/action, prioritize targeted sign F, consume once, prevent held-item drop/sign pickup and suppress subsequent input after accepted travel. No new tags/listeners or prompt writer.
5. **Wire intended scenes and recover errors.** Dependency: Steps 3–4. Configure NeighborHoodToGarden's existing controller/output for correct route; retain clip/texture/aspect/audio. On pre-unload validation failure leave Neighborhood usable and report visibly. On post-unload failure keep saves/run data intact, clear only owned overlay/locks and provide existing usable menu/status recovery; no silent cinematic skip. Handle destroyed owner/video, preparation timeout/error, missing fade/load failure and restoration failure without duplicate overlays or stale Busy flags.
6. **Focused verification and save.** Dependency: Steps 2–5. Compile after shared-script changes, inspect serialized references and one F owner, perform actual Game View sign/video/fade tests with isolated save storage. Save only intended two scenes/scripts, restore original Editor scene setup, remove temporary diagnostics without deferred cleanup. Avoid prior long forced-domain-reload workflows and distinguish unperformed standalone checks.

# Verification & Testing
- Looking at unobstructed Letrero within actual 3.5 m range displays Press F; looking away/out of range/behind solid wall does not. Hidden HUD/tooltips hides prompt but allows valid interaction. Pause/focus loss/capture/load block it.
- F with held flashlight starts exactly one transition without dropping/toggling it, Radio reset or generic sign pickup. Held F/rapid repeat/combined keys cannot start multiple operations; other scene F key/Radio/pickups and E doors remain unchanged.
- Exact route Neighborhood → NeighborHoodToGarden → GardenScene, never VideoIntro/MainMenu intro route/Neighborhood replay. Direct garden cinematic Play Mode also reaches Garden without resetting run.
- Actual rendered evidence shows partial darkening, full black across both loads and video preparation/Garden restoration, then partial reveal and exact transparent endpoint. No first-frame flash, hidden opening/audio consumption or missing final-frame hold; clip completes through loopPointReached, not a guessed timer.
- Preserve current run/key/manual-save files; outgoing Neighborhood doors/items/player snapshot is retained for an ordinary later revisit. Garden receives its own entry checkpoint and exactly one ordinary-entry autosave after valid readiness, not a cinematic save or premature retry/load reconstruction save.
- Garden movement/look/HUD/pause/save/enemy/interference remain blocked until reveal completes and work afterward. Existing main-menu New Game confirmation/fresh-run route, Continue/Load and physical/interference retry fades unchanged.
- Missing/invalid clip/texture/fade/destination, decode timeout, interrupted playback and failed adapter restoration recover visibly without stuck black/Busy/input locks, parallel loads or unintended save reset. Guards remain valid when death or blackout competes with sign use.
- Test menu-launched versus directly opened Neighborhood, repeated travel/direct cinematic play and different window sizes. Check Console against baseline. Preserve user save files in isolated testing; Windows .mov decoding/audio/build and alternate-resolution checks remain unverified unless performed.

# Baguette Pickup, Hand Presentation and Car Travel — Recovery Amendment

Only this amendment is pending approval for the current request. Preserve completed features and earlier scene edits; do not repeat unrelated amendments.

# Project Overview
- Game Title: Existing FINAL-PROJECT; unchanged.
- High-Level Concept: First-person horror exploration with a Baguette-gated car interaction. Pick up the Garden Baguette to return to Neighborhood, visibly carry it until using the car, then enter CarScene without the hand visual.
- Players: Existing single player.
- Inspiration / Reference Games: Existing project; no new references.
- Tone / Art Direction: Preserve the existing bread mesh, player and horror environment.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape HUD; verify 1920x1080 and a smaller window when possible.
- Render Pipeline: URP PC_RPAsset, Unity 6000.3.8f1.

## Confirmed Decisions and Recovery Evidence
- User selected **Pick up and return**: F on the Garden Baguette acquires it and starts travel directly to Assets/Scenes/Neighborhood Scene.unity. No intermediate video or new arrival teleport is requested.
- User clarified that the Baguette stays visibly in hand until car interaction; when CarScene loads it is no longer in hand.
- Current scripts compile and retrieved Console error entries are empty. This is not proof of working runtime interaction.
- Garden's Baguette still has Transform/MeshFilter/MeshRenderer/MeshCollider/Rigidbody/SignSceneInteraction; no BaguettePickup is attached. Canonical PlayerFollowCamera InteractionSystem has null baguettePickup and sceneSign references, with valid Player, Main Camera (1), HandPoint and InteractionPrompt bindings.
- BaguettePickup.cs now has an unobstructed camera-ray eligibility check from the interrupted edit. Its tooltip still incorrectly says aiming is unnecessary. The prompt edit and all scene wiring must be rechecked before implementation.
- The Garden bread is greatly enlarged: local scale approximately (16.65,14.54,16.65), visual bounds approximately 4.14 × 0.96 × 9.98 world units. Preserve this world prop, but use separately fitted hand-visual scale rather than copying this scale into the player's hand.
- GameSaveManager already contains saved HasBaguette acquisition/load/new-run/Garden-retry logic. ResetStatic currently clears DoorKeyChanged but not BaguetteChanged; repair this lifecycle omission.
- VehicleInteraction.cs currently runs its fade coroutine on the outgoing car object and waits a fixed 0.5 seconds after synchronous loading. Scene destruction can stop that coroutine before reveal. Replace only this travel orchestration with a persistent, restoration-aware owner.
- Garden, Neighborhood and CarScene are already enabled build scenes. Do not change the build list.

## Implementation Approach
Use logical quest ownership plus a scene-owned, non-droppable hand visual rather than making the Baguette an ordinary InteractionSystem.HeldObject. This preserves existing flashlight physics, callbacks and strict save-object registrations, and avoids an unregistered-held-item save failure. Reusing ordinary single-item holding was considered, but would require extra drop, replacement and save semantics that conflict with carrying the Baguette until the car.

Fit a project-owned render-only Baguette visual to Neighborhood's HandPoint using the supplied mesh/material. It must be visible in first person without obstructing the view or colliding with the player; keep ordinary held-item behavior intact and verify there is no overlap with the flashlight. Do not invent a Baguette drop/use control or new animation asset.

Keep acquisition separate from use at the car: retain HasBaguette as acquired ownership, add an additive saved baguetteUsedOnCar flag to record the end of hand presentation. This prevents ordinary revisits from respawning the acquired Garden bread or re-showing it after car use. Existing version-1 records default the new flag to false. Car eligibility continues to use HasBaguette; no extra one-use car restriction is requested. New Game and successful Garden death/blackout retry reset both Baguette flags. Do not consume ownership or change DoorKey rules.

# Game Mechanics
## Core Gameplay Loop
Look at an unobstructed Garden Baguette within configured pickup range → show Press F to pick it up → F accepted once → logical possession true and world Baguette disappears → fade out → load Neighborhood under black → restore/init gameplay and fit/show hand Baguette → fade in and release controls. In Neighborhood, the acquired and not-yet-used Baguette remains visible in hand. Near SM_asset_04, show locked feedback without acquisition or Press F to break the window with acquisition. Accepted car F → fade/load CarScene → commit used-on-car state and remove hand presentation before reveal → normal CarScene gameplay.

## Controls and Input Methods
- Preserve Both backend and the current event-driven Keyboard F InputAction on InteractionSystem. No second input listener, polling loop or backend change.
- Use the same aimed eligibility check for Garden prompt and F action; ignore player/held colliders, respect solid occlusion, range, pause/focus, save/capture and transition locks.
- Consume pickup/car F exactly once, with no flashlight drop, Radio reset or generic bread pickup; stop other input processing after accepted travel.
- Transition validation must precede state mutation. If travel cannot start, leave the player usable and report an error; do not hide the only bread and silently strand progress. Roll back acquisition/use only where this operation owns the uncommitted change.

# UI
Reuse UICanvas/InteractionPrompt, existing font/layout and HUD/ToolTips visibility settings; no new Canvas, inventory panel or EventSystem.

```text
Garden, aimed Baguette:    Press F to pick it up
Neighborhood, no bread:    Locked until Baguette is obtained
Neighborhood, has bread:   Press F to break the window
Hand in Neighborhood:      Baguette visible until car use
CarScene:                  No Baguette hand visual
Travel:                    fade out → opaque loading/restore → fade in
```

Reuse UIAlphaFade.asset/UIFadeImage.prefab/DeathRetryTransition with operation-local cloned resources, proposed timings 1 second out/in and 0.2 second black hold. Keep input/HUD/AI/save locks through reveal. No shared fade asset or URP post-processing changes.

# Key Asset & Context
- Modify Assets/Scenes/GardenScene.unity: add an active external Baguette Pickup Controller, wire the existing bread/collider/player/body/InteractionSystem and fade route. Disable or remove only the obsolete Baguette SignSceneInteraction after incoming-reference inspection; do not use a video-sign route for this pickup. Keep controller active while hiding bread so reset/load events can restore it.
- Modify Assets/Scenes/Neighborhood Scene.unity: attach/configure render-only hand presentation to the existing HandPoint, bind SM_asset_04/Model's valid collider and existing canonical InteractionSystem. Preserve car proximity behavior and player arrival pose from existing visited-state rules.
- Modify Assets/Scripts/BaguettePickup.cs: exact aimed prompt/eligibility, cached original transform, safe acquisition plus direct fade travel request, error state and reset visibility.
- Modify Assets/Scripts/InteractionsSystem.cs: exact pickup prompt, safe single-F consumption and generic-pickup protection; optional references remain null-compatible elsewhere.
- Modify Assets/Scripts/VehicleInteraction.cs: delegate car travel to persistent coordinator, retain messages/proximity and acquisition gate, validate prerequisites, record successful car-use state.
- Modify Assets/Scripts/Saving/GameSaveManager.cs and SaveGameData.cs: saved used-on-car state and new-run/load/retry lifecycle; persistent direct-gameplay travel guard/capture/restoration readiness as needed. Preserve world-ID validation, manual saves, ordinary entry autosave and retry suppression.
- Create Assets/Scripts/BaguetteHandPresentation.cs and Assets/Items/Baguette/BaguetteHandVisual.prefab using the existing Assets/Imported/Yodah_the_Cat/Stylized bread serving mesh/material references. Visual only; no Rigidbody/Collider/input/save registration.
- Create Assets/Scripts/Transitions/GameplayFadeTravel.cs only if necessary as the persistent direct-gameplay fade owner; reuse existing overlay, not a second global fade singleton or cinematic sequencer.
- GameplaySaveAdapter.cs is read-only by default; narrowly extend only if a verified restoration-success signal cannot be obtained from the manager, with impact analysis first. CarScene is read-only by default; absence of Neighborhood's hand presenter should naturally remove the visual on unload.
- No package/imported source or prefab edits, mesh generation, character rig replacement, persistent-ID changes or destructive user-save cleanup.

# Implementation Steps
1. **Recheck recovery and impact.** Inspect dependencies/dependents and live references for every edit target, dirty scene state, obsolete sign component, HandPoint/Animator/camera, prompt text and repository flag serialization. Preserve unsaved work; obtain confirmation before saving unrelated newly discovered edits. Dependency: approval of this amendment.
2. **Repair inventory lifecycle.** Add backward-compatible used-on-car state and change notifications, clear static events safely, reset both flags on accepted fresh run/successful Garden retry and restore each loaded record before visual reconciliation. No disk save on pickup itself. Dependency: Step 1.
3. **Implement persistent direct fade travel.** Validate destination/fade/gameplay readiness and exclusive transition ownership; capture outgoing world before unload, acquire locks synchronously, fade under a persistent owner, load asynchronously, wait for successful destination restoration/checkpoint initialization, commit operation state before destination checkpoint/autosave capture, reveal, then release/dispose. Add bounded failure recovery, no guessed 0.5-second readiness wait or coroutine hosted on destroyed source. Dependency: Steps 1–2.
4. **Finish Garden pickup wiring.** Place controller outside hidden world item, use aimed range/occlusion and exact prompt, cache authored transform, hide immediately on successful acquisition, route F to Neighborhood, reject generic pickup and consume one action. Ensure retry/load reconciliation can reactivate bread. Dependency: Steps 2–3.
5. **Implement Neighborhood hand presentation.** Build render-only prefab from supplied bread, fit hand-local position/rotation/scale to actual first-person view, show for acquired and not-used ownership, reconcile after save restoration and before reveal. Preserve flashlight state and save registrations; inspect real rendered overlap/camera clipping. Dependency: Steps 1–4.
6. **Repair car transition.** Keep existing SM_asset_04 collider/proximity/prompt binding, delegate accepted F to shared persistent travel, end Baguette presentation on successful CarScene entry before reveal. Failed validation/travel must not mark bread used. Dependency: Steps 2–5.
7. **Focused validation and cleanup.** Compile incrementally, inspect references, verify aim/F/hide/travel/hand/car/retry/load behavior and actual Game View fade/hand rendering. Use manual or compatible bounded tests; avoid previously failing forced-domain-reload loops. Isolate test saves, never delete user data. Save only intended scripts/scenes/assets and remove temporary diagnostics without deferred cleanup. Dependency: Steps 2–6.

# Verification & Testing
- Garden aimed/in-range/unobstructed bread shows exactly Press F to pick it up; looking away, walls, out of range and blocked gameplay suppress prompt/action. HUD hidden does not disable an otherwise allowed interaction.
- F performs one pickup, hides all world bread visuals/collision and loads exact Neighborhood Scene path with full opaque coverage and visible fade-in; no unintended video, duplicate acquisition, Radio action or held-item drop.
- Neighborhood shows a reasonably sized bread in the actual player's hand before reveal and while walking/looking; no world-scale giant mesh, clipping/occlusion or flashlight overlap. Visual is not registered as an ordinary HeldObject.
- No bread → locked car text/no travel; acquired bread → Press F to break the window and one safe CarScene transition. CarScene has no hand bread; normal revisits do not re-show a used bread or respawn the Garden prop.
- Successful Garden physical death and blackout retries reset bread ownership/use and restore original Garden transform/availability; failed pre-load retry does not clear ownership. Other scenes retain existing checkpoint semantics.
- Save/load before acquisition, after Neighborhood acquisition and after car use restores the corresponding flags/visuals; legacy records default missing used flag to false. No held-object registration exception or unchanged-world-ID mismatch introduced.
- New Game resets both flags; cancellation and failed transitions preserve progress. Repeated scene entry/load/no-domain-reload play has no stale event handlers, duplicate visuals, stuck Busy locks or orphan overlays.
- Preserve DoorKey, flashlight use/drop, pause/settings, save/Exit, Siren Head, interference and existing intro/cinematic/retry flows. Report actual tests separately from unperformed standalone/alternate-resolution checks.

# DoorKey Return → Neighborhood Door → Mom Video → House Amendment

Only this amendment is pending approval for the current request. Preserve completed systems; do not repeat earlier work. The final clarified route supersedes both immediate key-to-Mom travel and direct door-to-House travel.

# Project Overview
- Game Title: Existing FINAL-PROJECT; unchanged.
- High-Level Concept: Single-player first-person horror exploration. Collect the key in CarScene, resume Neighborhood progress, open the previously locked door, watch the supplied Mom cinematic, then enter a basic playable HouseScene.
- Players: Single player.
- Inspiration / Reference Games: Existing project; Baguette travel and existing cinematic transitions are the behavior references.
- Tone / Art Direction: Preserve existing horror scenery, key, player and UI. House is a minimal safe placeholder play area, not finished house artwork.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape UI; cinematic aspect-fit 16:9 with black bars where needed.
- Render Pipeline: URP PC_RPAsset, Unity 6000.3.8f1.

## Confirmed User Decisions and Recovery State
- Exact route: **DoorKey pickup → Neighborhood Scene → player opens previously blocked door → MomScene video → HouseScene**. The video is not triggered by pickup alone.
- Last checkpoint means **last captured per-scene state** from departure or the explicitly loaded save, matching Baguette travel. It does not mean the death-entry checkpoint or newest save file across unrelated slots. First visit uses authored defaults.
- User selected **Add basic playable setup** for House: reuse existing first-person player/camera, pause/settings/save UI and simple safe floor/spawn. No finished house art, enemy, Radio, interference or navigation is requested there.
- Optional returns use **F interaction with the corresponding existing object**, not new pause-menu return buttons or new travel markers. Preserve sign/car routes and collected-item visibility; do not respawn acquired quest props just to provide return controls.
- Reconnected state: CarScene loaded and clean; DoorKey Pickup exists but has no return-fade configuration. MomScene does not exist. HouseScene currently contains only Main Camera and Directional Light. No implementation of this new route is confirmed, and the earlier interrupted plan append was not saved.
- Verified clip: `Assets/Videos/Escena_final_mama.mp4`, GUID `77777474b830fbd4791a501198e63a96`, 1920×1080, 81.92 seconds, one audio track. Use real playback completion, not an 81.92-second timer.

# Game Mechanics
## Core Gameplay Loop
1. Existing Neighborhood F sign/cinematic travel → Garden; visited Garden resumes its latest captured player/world state.
2. Garden Baguette F → existing acquisition/hand-presentation behavior → fade/load Neighborhood with its latest captured player/world state.
3. Neighborhood Baguette-gated car F → fade/load CarScene with its latest captured state if visited.
4. CarScene DoorKey F → validate/accept travel, acquire key once and hide it → fade out → Neighborhood loads/restores under black with current key ownership → fade in.
5. Player intentionally opens the exact previously locked Neighborhood door via existing E interaction → start Mom cinematic travel once → fade out → Mom loads/prepares first frame under black → fade in → play complete video with embedded audio → hold final frame and fade out → House loads/initializes/restores under black → fade in → release gameplay locks.

Capture outgoing world/player snapshots before unloading. Preserve run ID, DoorKey and Baguette flags, manual saves and other visited states throughout. Current global ownership governs destination door/key visibility; never replace it with stale destination-entry flags on ordinary travel. Key acquisition unlocks but never automatically opens the door and does not consume the key.

Retain actual death/blackout retry behavior: current scene-entry checkpoint restoration, CarScene key reset, Garden Baguette reset and no retry autosave. Ordinary travel must not invoke those reset paths. Mom is cinematic-only and creates no gameplay autosave. House uses existing supported-gameplay entry/Exit/manual-save policy.

## Controls and Input Methods
- Verified activeInputHandler = 2 (Both). Reuse event-driven F InputAction, PlayerInput/StarterAssetsInputs and InputSystemUIInputModule. No new backend, polling or bindings.
- Preserve DoorKey's proximity/obstruction eligibility; aiming is not newly required. Eligible key F consumes once; accepted travel immediately stops further same-frame E/right-click/Radio/drop processing. Failed eligible attempts consume F and show error without falling through to generic pickup.
- Keep existing E door opening input. The transition is opt-in only on the selected door and triggered by an intentional closed-to-open request, never Awake, lock reconciliation, ownership change, saved-state restoration or Close.
- Pause/focus loss/capture/load/intro/travel block quest actions. Guard duplicate input and overlapping fade/load owners synchronously.

# UI
Use existing uGUI HUD, prompts, pause/settings/save views and project black alpha fades. No inventory screen, skip button, new return menu or duplicate EventSystem.

```text
CarScene near key: Press F
F → fade out → Neighborhood load/restore → fade in
Neighborhood door without key: Locked — DoorKey required
Neighborhood door with key: existing E opening prompt
E → fade out → Mom first-frame preparation → fade in
Mom: full-screen aspect-fit Escena_final_mama + embedded audio
Actual video completion → final-frame fade out
House initialization/restoration → fade in
House pause: Resume | Save Game | Settings | Exit
```

Mom Canvas: black full-viewport background, centered RawImage with aspect-fit 16:9, one VideoPlayer/VideoIntroController, persistent fade overlay above all ordinary UI. Create an isolated 1920×1080 RenderTexture rather than modifying existing intro texture assets globally. Disable autoplay/looping and mute during first-frame preparation; reveal held first frame before audible full playback. Preserve final frame during outgoing fade. No playback skip/fixed duration substitute.

Reuse UIAlphaFade.asset/UIFadeImage.prefab/UIFadeMaterial.mat unchanged with operation-local clones; source-equivalent 1-second out/in and 0.2-second black hold defaults. Loading adds to black time. Hold opacity across loading/restoration and locks through final reveal. Respect HUD/ToolTips settings. Failures must provide visible status and usable controlled recovery, not an unexplained black screen or Console-only message.

# Key Asset & Context
## Verified Existing Architecture
- `DoorKeyPickup.TryPickUp` only acquires ownership; its active external owner correctly hides/reconciles the existing key. Real serialized reference inspection confirms key/collider/player/body/canonical interaction wiring; JSON instanceID placeholders were not missing-reference evidence.
- `GameplayFadeTravel.Begin(destination, bool pickup, ...)` currently specializes true as Garden Baguette pickup and false as car use. Key travel cannot safely use false because it would mutate Baguette use state.
- `GameSaveManager.BeginGameplayCinematic` captures outgoing `_visited` state and acquires transition/input locks. Ordinary OnSceneReady restores `_visited[destination]`, captures an entry checkpoint and autosaves. `IsCinematicGameplayReady(adapter)` signals validated restoration; Ready alone is insufficient.
- NewGameSceneSequence already provides persistent gameplay → cinematic → gameplay orchestration. Its BeginGameplayCinematic entry preserves the run, unlike BeginNewGame/BeginFreshRun. Reuse this route for Mom rather than duplicate a sequencer.
- VideoIntroController already supports first-frame hold, loopPointReached completion, embedded Direct audio muting, final-frame hold, error events and configured direct-play destination. Configure Mom direct play to House without fresh-run reset; preserve VideoIntro and NeighborHoodToGarden defaults.
- Selected door identity: Neighborhood `PersistentSaveId 04f129cafbe5426cb18a4aedd53b606f`, approximately (51.34, 2.14, 49.45), NewNeighborhood/House6/Floor_0/Doors/Door_1B/Exterior_Door. Re-resolve by scene plus ID, not duplicated hierarchy name or transient instance ID. InteractiveDoor has ownership guards but no cinematic opening hook.
- Existing player has valid Humanoid Avatar, skinned renderers and NeighborhoodPlayer controller. Reuse verified mesh/rig/controller/clips and FirstPersonController architecture; no generation, rerigging, new animations or controller rewrite.
- Supported save scenes currently Neighborhood/Farm/Car/Garden, maximum four snapshots. House requires exact allowlist and consistent five-snapshot limit; Mom remains unsupported cinematic-only. Retain version-1 legacy record validity.
- Build list enables MainMenu, VideoIntro, Neighborhood, NeighborHoodToGarden, Garden, Car; Test disabled. Mom/House absent. Enable only Mom and House; do not enable Farm/Test or change New Game destination.
- AssetDatabase dependency/dependent inspections were used because dedicated dependency tool unavailable; shared scripts affect other scenes/recovery assets. Recheck impact before each implementation edit. VideoIntroController dependencies confirmed in both existing cinematic scenes, NewGameSceneSequence in MainMenu.

## Existing Assets to Modify
- `Assets/Scripts/DoorKeyPickup.cs`: return route/fade references/timings/Error, validated acquisition plus persistent direct travel, active-owner visibility/reset reconciliation.
- `Assets/Scripts/Transitions/GameplayFadeTravel.cs`: explicit travel intent or separate generic/key entry alongside backward-compatible Baguette wrapper. Key intent must not alter Baguette flags. Preserve persistent overlay/readiness and owned cleanup; verify overlay completion rather than guessed waits.
- `Assets/Scripts/Saving/GameSaveManager.cs`: narrow operation-owned key travel boundary; validate/capture/block before acquisition, preserve quest flags through destination restoration/checkpoint/autosave, expose coherent success/failure. Keep cinematic/retry/load semantics and strict object validation intact. Ensure failed restoration never signals success or releases gameplay behind a running fade.
- `Assets/Scripts/InteractionsSystem.cs`: eligible key F consumes once and returns on accepted travel; display errors and prevent generic pickup fallthrough/same-frame extra input. Stop processing after accepted door travel while ordinary doors remain compatible.
- `Assets/Scripts/InteractiveDoor.cs`: optional intentional-opening transition hook/reference, null default. Unify ToggleDoor/Open/Use closed-to-open path; validate/start Mom route once, never from RestoreSavedState/Awake/Close/reconciliation. Recover door logical/visual state on failed pre-unload acceptance as appropriate.
- `Assets/Scripts/Saving/SaveGameData.cs`: House exact supported path and five-scene bound; unchanged fields/version/legacy flags. Inspect repository/other consumers for duplicated limits before editing.
- `Assets/Scripts/NewGameSceneSequence.cs`, `Assets/Scripts/VideoIntroController.cs`: read/reuse existing explicit route APIs; edit only for verified compatibility, operation-owned cleanup, scene identity/readiness or visible-error gaps. No hard-coded rerouting of existing cinematics or BeginFreshRun on Mom travel.
- `Assets/Scenes/CarScene.unity`: configure existing key return destination/fades without moving key or rebuilding gameplay.
- `Assets/Scenes/Neighborhood Scene.unity`: configure only the exact keyed door for MomScene → HouseScene, preserving its ID/lock/hinge and other doors. Audit existing sign/car revisit wiring.
- `Assets/Scenes/HouseScene.unity`: minimal safe floor/play area/spawn plus reused verified player/camera/head/hand/input/interaction, HUD/pause/settings/save adapter and real local registrations. Remap every reference and omit source enemy/interference/Radio/quest/optimization owners. One player/camera/listener/EventSystem, preserve original light unless readability requires a minimal local adjustment.
- `ProjectSettings/EditorBuildSettings.asset`: enable Mom and House only, preserve other inclusions and intro route.
- GardenScene, BaguettePickup, VehicleInteraction and GameplaySaveAdapter are read-only by default; shared travel changes should provide revisit consistency. Any necessary additional narrow wiring/API correction requires impact inspection and documented scope, never bypassing save validation.

## New Assets / Reuse
- `Assets/Scenes/MomScene.unity`: cinematic-only scene with existing VideoIntroController pattern, supplied clip, full-screen Canvas/RawImage/background, one audio-capable VideoPlayer and explicit House direct-play destination.
- `Assets/Videos/MomScene.renderTexture`: 1920×1080 dedicated output, suitable video settings.
- Optional `Assets/Scripts/DoorSceneTransition.cs`: scene-owned exact cinematic/destination/fade configuration and guarded intentional-open API, no new input/prompt Update writer.
- House primitive/material assets only if needed under `Assets/Environment/HouseScene/`. Reuse current player model/Avatar/controller/clips and UI styles unchanged; no universal prefab redesign.
- Existing shared fade assets, supplied video/import settings, imported models/controllers and original cinematic scene assets are read-only except narrowly scoped explicit fixes above. No packages, rendering symbol workarounds or global material/lighting edits.

# Implementation Steps
1. **Recheck recovered state and impact.** Dependency: approval. Confirm dirty scenes, actual key/door ID and incoming references, current route APIs, clip/player rig/controller and House emptiness, save bounds/autosave timing and absence of stale cleanup hooks. Inspect dependencies/dependents before each existing edit. Preserve unsaved edits; ask before saving unrelated changes. Do not repeat already-completed Baguette work.
2. **Prepare basic playable House destination.** Dependency: Step 1. Reuse validated existing player and approved animation/input setup, safe floor/spawn, camera and HUD/pause/settings/save wiring. No new character generation or imported controller changes. Remove inapplicable copied dependencies after reference checks, register actual local saveables only, never copy Neighborhood's registration array. Extend supported scenes/bounds consistently and enable House. Verify direct movement/look/pause/save initialization before door route is activated.
3. **Create Mom cinematic.** Dependency: Steps 1–2. Create scene/RenderTexture/Canvas/background/aspect-fit display and single VideoPlayer/controller, assign Escena_final_mama and one embedded Direct audio track, disable autoplay/looping, configure House direct-play destination and established fade refs. Enable Mom in build list. Validate prepare/first frame/audio/completion wiring without changing supplied importer/source video. Do not classify cinematic as saveable gameplay.
4. **Implement guarded key-return travel.** Dependency: Steps 1–3. Generalize/add key intent to persistent direct travel; validate source/destination/fade/exclusive ownership before quest mutation. Capture outgoing Car state and block synchronously, acquire/hide key once, fade/load Neighborhood, wait for successful restored readiness/checkpoint/autosave with key true, reveal and release. Retain Baguette flags. Roll back only operation-owned uncommitted changes on failure; do not blindly revert a state after it was committed to a destination autosave or overwrite newer state.
5. **Integrate single-F key behavior and revisits.** Dependency: Step 4. Wire key route/fades, consume eligible F once and stop subsequent input on success, report errors. Audit Garden Baguette, Neighborhood car and sign/cinematic routes for capture-before-unload and latest `_visited` restoration. Preserve current run flags, first-visit defaults and held-item semantics; no new pause return controls/markers or collected-prop respawn. Actual death/blackout resets stay separate.
6. **Wire intentional door → Mom → House.** Dependency: Steps 2–5. Add opt-in door transition hook and bind exact door ID. Only unlocked intentional closed-to-open interaction starts the existing persistent BeginGameplayCinematic route. Start fade immediately on acceptance, without waiting for full door swing; capture consistent logical door state. Do not travel on key acquisition/load/restoration. Preserve key/run/visited snapshots, prepare Mom first frame under black, reveal/play entire video, hold final frame/fade, load/restore House under black and reveal only after validated readiness. No direct key-to-Mom route remains.
7. **Strengthen scoped failure cleanup.** Dependency: Steps 4–6. Handle invalid refs/destinations, capture failure, async load null/exception, destroyed owner/video, decode/preparation timeout, playback interruption and failed gameplay restoration. No duplicate fades or broad external lock release. Visible status and controlled usable recovery; no silent cinematic skip, lost key, false completion or stuck black. Direct Mom play reaches House without fresh-run reset; existing main/garden cinematics remain unchanged.
8. **Focused verification and save.** Dependency: Steps 2–7. Compile incrementally, inspect local reference/event integrity and preserved existing ID counts. Test key return, captured-state revisits, lock/opening, complete 81.92-second video and House save reconstruction in separate bounded sessions; no short timeout that cannot accommodate the whole clip. Capture actual Game View fade/video evidence. Isolate test saves outside Assets, never delete user records or create deferred cleanup/domain-reload loops. Save intended assets and restore original Editor scene setup; report unperformed Windows/build checks honestly.

# Verification & Testing
- Exact route Car key F → Neighborhood, no video yet → E opening of selected keyed door → MomScene → HouseScene. Other doors and existing sign/car/Baguette routes unchanged. Key remains owned, hidden and unconsumed.
- Neighborhood departure pose/pitch/registered world/held-item state survives Garden Baguette return and Car key return. Visited Garden and Car resume their own latest captured states; first visits use defaults. No newest-slot auto-selection or stale destination-entry flag override.
- Eligible key F grants once without dropping/toggling flashlight, resetting Radio or processing E/right-click the same frame. Pause/focus/restoration/capture block action, obstruction/range and HUD settings respected; failed valid attempts report error without generic pickup fallthrough.
- Selected door locked rejects normal opening and saved-open restoration. Acquired key does not auto-open/travel. Intentional Open/Toggle/Use closed-to-open starts one cinematic; restoring an open saved door never starts another scene load. Pre-load failure leaves coherent usable door/player state.
- Mom renders supplied 1920×1080 clip in Canvas, audible embedded track, opening not lost during preparation, real loopPointReached completion after full playback, final frame retained through fade. No fixed-duration substitute, loop, automatic skip or accidental VideoIntro/Garden route.
- Actual rendered partial fade-out, exact black during all three loads, partial reveal and transparent endpoints. House input/HUD/save remain blocked until final reveal, no flash/duplicate overlay/stuck Busy/cursor. Test directly opened versus menu-launched gameplay/Mom.
- House has one working player/camera/listener/EventSystem and safe floor/spawn, readable pause/settings/save UI, valid local adapter/IDs, no source enemy/quest/Radio/interference references. Movement/look/jump/pause/manual-save/Exit/Continue/selected-slot loading work.
- House records/five supported snapshots validate; older version-1 fewer-scene records remain valid. Mom creates no autosave. House ordinary entry autosaves once after valid restoration with current key/run flags; manual files preserved.
- Actual Car death/blackout still resets key; Garden retry still resets Baguette; ordinary travel does neither. Preserve retry-autosave suppression, hand-visual rules and other visited states.
- Double/held input and capture/blackout contention yield one owned operation. Missing clip/texture/fade/scene, decode errors, failed capture/restore, interrupted overlay or direct bootstrap failures recover visibly without falsely claiming video completion or clearing another owner's locks.
- Windows video decode/audio/build, alternate resolutions and full-road traversal remain unverified unless performed. Preserve user saves and imported/shared assets; no package guards/defines/backend changes or unrelated scene edits.

# Full-Height Scenery Fade and Background Sky — CarScene Amendment

Only this amendment is pending approval for the current request. Preserve all previously completed gameplay and environment work. The user selected **Preserve dynamic sky**, rather than dense full-view overcast fog. This supersedes reliance on the previous lower-horizon colour match as the complete solution for giant scenery.

# Project Overview
- Game Title: Existing FINAL-PROJECT; unchanged.
- High-Level Concept: Existing first-person horror exploration on an abandoned road with deliberately oversized cones and tyres. Make distant scenery appear/disappear smoothly at every height while retaining the animated night sky.
- Players: Existing single player.
- Inspiration / Reference Games: Existing project and Tenkoku atmosphere; no new references.
- Tone / Art Direction: Preserve horror lighting, aurora, clouds, giant prop scale and existing blue-grey road fog.
- Target Platform: Windows StandaloneWindows64.
- Screen Orientation / Resolution: Existing landscape; validate ordinary and upward-looking first-person views at 1920x1080 and a smaller window when available.
- Render Pipeline: Verified URP PC_RPAsset, Unity 6000.3.8f1. Last approved project context records URP 17.3.0; recheck installed version before authoring shaders.

## Selected Approach and Trade-offs
- **Selected:** Correct scene-local sky-background depth handling, then apply targeted whole-prop dither fading where necessary. Keeps dynamic sky detail and avoids transparent-object sorting problems, but requires consistent colour/depth/shadow passes and checking visible dither quality.
- **Alternative considered:** Full-screen distance fog with an all-height matching background. Gives more uniform concealment, but substantially obscures the aurora and sky; the user did not choose this appearance.
- Start with the verified finite-dome depth issue, not a blanket camera far-plane increase or enormous dome scaling. If correcting sky depth fully resolves a particular prop's clipping and appearance, avoid unnecessary material replacement on that prop. Add the targeted fade only to the audited oversized scenery that still needs it.
- Preserve nearby silhouettes/readability and distant uncertainty: this supports the existing horror aesthetics (MDA) without changing collision, progression or enemy fairness. Do not scale down cones/tyres or deactivate their physical objects.

# Game Mechanics
## Core Gameplay Loop
Movement, interaction, Siren Head, interference, Radio, pickups, capture, saves and scene travel remain unchanged. As the player approaches or retreats, affected distant prop visuals gradually reveal/conceal as one coherent object, independent of elevation. Clouds and aurora remain behind world geometry rather than acting like physical surfaces through the map.

## Controls and Input Methods
Preserve Both input backend, existing event-driven PlayerInput/StarterAssetsInputs, Cinemachine and capture camera ownership. No new input bindings or controller changes. Evaluate visibility against the final actual gameplay camera pose before rendering, including look-up, camera blends, save teleports and death framing; never use the shadow camera as the fade-distance origin.

# UI
No new HUD, menus, Volume profiles or screen-space overlays. Existing fade/blackout UI stays above gameplay and is unaffected.

```text
Player view: readable near road/props
             distant whole prop → smooth fade → existing sky/background
             upper sky: existing aurora/cloud animation, no dome intersection
Inspector (only if targeted fading is needed):
    Actual Camera | Explicit Prop Renderers | Fade Start | Fade End | Enabled
```

# Key Asset & Context
## Verified current state
- CarScene is loaded and clean. Recheck dirty state before changes; preserve newly discovered user edits without reload/revert or unapproved unrelated saves.
- Main Camera (1) is active at approximately (277.42, 2.50, 333.14), Skybox clear mode, far clip 500. Earlier inspection found virtual-camera far clips 500 and 1500; recheck effective runtime lens before changing anything.
- RenderSettings fog is ExponentialSquared, density 0.025, colour (0.18, 0.21, 0.24). Existing project-owned sky material is Assets/Environment/CarScene/CarSceneFogSky.mat, using Shaders/FogMatchedSky.shader. Its fragment matches only the lower sky via smoothstep(0.10, 0.45, rayDir.y); this alone cannot conceal tall silhouettes against upper sky.
- Three traf_cone renderers use imported M_traf_cone.mat; approximate heights are 115, 214 and 150 world units. Three wheel_10 renderers use imported WheelMaterial.mat; bounds approximately 42 units wide, up to 42 tall. Their giant dimensions are intentional and must remain unchanged.
- Tenkoku cloud sphere shader uses Queue Background+1605, Cull Front, Offset 1,80000, alpha blending, ordinary mesh clip-space vertex position and no explicit ZWrite Off. Aurora sphere uses Queue Background+1600, the same large Offset, additive blending and ordinary mesh clip-space position. These are actual finite mesh passes, separate from the true skybox; runtime code repeatedly rescales both relative to camera far clip, so one-time Transform enlargement is not a robust fix.
- Cloud/aurora shader observations identify a plausible depth/intersection cause, not a completed runtime diagnosis. Establish it with baseline views/depth evidence before declaring the cut fixed.
- URP pipeline at Assets/Settings/PC_RPAsset.asset uses PC_Renderer.asset. HDR and camera post-processing are enabled; renderer has PostProcessData and SSAO. Neither shared pipeline/renderer nor interference Volume profiles require changes for the selected targeted approach.
- Imported cone material includes albedo, normal, metallic, emission and occlusion textures; preserve them. Imported tyre material references AtlasTexture.png. They are shared with imported prefabs: never edit these materials globally.
- URP Lit forward pass calls MixFog after PBR lighting. Targeted fading must not merely dissolve an already solid fog-coloured upper silhouette; account for this interaction explicitly. Package shaders/includes were inspected read-only and must remain unmodified.
- CameraSceneryVisibility.cs exists, but no attached controller was logged in current CarScene inspection. Recheck registration before attributing cuts to activation; do not modify unrelated optimization logic by default.
- Dedicated dependency tools are not available in the current exposed tool set. Read-only AssetDatabase.GetDependencies and dependent asset scans were used for scene, sky, prop materials and renderer. Repeat impact and live serialized-reference checks before implementation edits; use a dedicated dependency query if available then.

## Existing assets to modify locally
- Assets/Scenes/CarScene.unity: assign project-owned sky material/shader copies to the existing Tenkoku cloud/aurora renderer instances and their actual material ownership references. Add a small explicitly wired fading owner only if residual oversized-prop transitions require it; retain all transforms/colliders/gameplay references.
- Assets/Environment/CarScene/Shaders/FogMatchedSky.shader and CarSceneFogSky.mat: preserve existing horizon match by default; tune only if real visual evidence requires a complementary adjustment.

## Proposed project-owned assets
- Assets/Environment/CarScene/Shaders/BackgroundCloudSphere.shader and BackgroundAuroraSphere.shader: copies of the installed source shaders with unique names, preserved notices and corrected include paths. Explicit no depth writes, appropriate depth test, no large depth offset, and background-depth clip position while retaining projected direction, UVs, animation, blend order and sky appearance.
- Assets/Environment/CarScene/Materials/BackgroundCloudSphere.mat and BackgroundAuroraSphere.mat: copies preserving source textures/properties, assigned only to CarScene instances. Inspect other visible sky meshes (galaxy/sun/moon) and extend this treatment only if they exhibit the same verified defect.
- Conditional: Assets/Scripts/Environment/OversizedSceneryFade.cs, one scene-owned manager with explicit renderer references and safe cached MaterialPropertyBlocks; no per-frame scene scans, GameObject deactivation or global shader visibility changes.
- Conditional: Assets/Environment/CarScene/Shaders/OversizedSceneryLit.shader and associated project-owned HLSL helpers, plus Materials/FadingCone.mat and FadingTyre.mat. Preserve source Lit textures, PBR shading, culling, instancing and applicable pipeline passes. Use installed compatible public shader includes; never change files under Packages.
- Optional targeted tests in existing compatible infrastructure. Do not install packages, add TAA, alter renderer mode or change generated scripting symbols to mask dither quality/compatibility problems.

# Implementation Steps
1. **Recheck context and establish evidence.** Dependency: approval of this amendment. Inspect dependencies/dependents and exact live material ownership for Tenkoku cloud/aurora and all six oversized props, final camera lens/pose, shader render states, depth/shadow settings and any activation controller. Capture actual Game View at road level and looking up while near and far from each representative prop. Distinguish finite sky-dome occlusion from camera far clipping and fog/sky colour contrast; do not assume they are the same defect. Preserve original material assignments and current clean/dirty scene state.
2. **Correct sky layers as backgrounds.** Dependency: Step 1. Create project-owned copies preserving source data/notices. Make cloud/aurora passes write no depth, remove the large depth offset, and project at the platform-correct far/background depth so world geometry remains in front regardless of dome size. Retain screen projection, view rays, transparency/additive blend ordering and animation. Correct include paths and handle reversed-Z through Unity's shader-provided conventions, not project scripting defines. Ensure Tenkoku's repeated material updates/reinitialization target the local copies rather than restoring imported materials. No shared imported source or prefab edits.
3. **Validate the minimal sky-depth fix first.** Dependency: Step 2. Repeat baseline paths and upward views; verify aurora/cloud motion, sky coverage when turning, no background occlusion of cones/tyres, no sky depth interfering with SSAO/transparent world materials. Confirm local assignments survive Play Mode entry, scene reconstruction and current sky/weather initialization. If a prop now has acceptable transitions, do not replace its material unnecessarily. Record remaining problematic prop identities and actual distances.
4. **Implement residual whole-prop fade if needed.** Dependency: Step 3. Use a finite-distance fade computed once per explicit prop from actual camera-to-world-bounds distance, not height or per-vertex screen depth, so tall upper/lower sections share a consistent fade. Begin with an adjustable broad range (proposed 45–100 units from the nearest bounds point); tune from measured views rather than treat it as guaranteed final balance. Guarantee full disappearance before any part of the prop reaches the effective camera far plane, using bounds and camera-space depth to account for giant geometry. Nearby props stay fully visible. Apply an ordered/blue-noise dither coverage clip with exact fully-visible/hidden endpoints and smooth monotonic coverage; evaluate shimmer without assuming temporal AA is enabled. For affected giant materials, use this fade as the primary concealment instead of the existing constant-colour fog at upper silhouettes; preserve normal fog for road and other scenery. Retain PBR features and consistently clip forward/depth/depth-normal/shadow passes to avoid invisible occluders or detached shadows. Shadow visibility must use the same player-camera fade value, never shadow-camera distance. Check active renderer path; implement the required passes without globally changing renderer settings.
5. **Integrate safely and expose tuning.** Dependency: Step 4 if needed. Create scene-specific cone/tyre material copies, assign only audited giant instances, and bind a single explicit manager outside controlled visual roots. Preserve and merge existing per-renderer/per-material property blocks rather than overwriting another feature. Update after final camera movement and before render; disabling/missing camera restores only owned fade values, no new material instances each frame or revived externally inactive objects. Fade does not change physics, navmesh, enemy sight, save object identity or activeSelf. Recheck compatibility with reflection/secondary views; use conservative behavior if their visibility cannot be safely handled.
6. **Focused verification, save and cleanup.** Dependency: Steps 2–5 as applicable. Compile incrementally and compare fresh Game View evidence to baseline while approaching/retreating, looking above cone tops and moving around tilted tyres. At most three visual capture/fix rounds before asking for user feedback. Verify pause, capture camera changes, reconstruction and source-compatible sky behavior without modifying user saves. Prefer manual or compatible bounded Play Mode tests; do not repeat historical stalled forced-domain-reload loops or destructive deferred cleanup. Save only intended CarScene/project-owned rendering assets after rechecking dirty state. Remove temporary diagnostics safely and report whether targeted fading was necessary and what was actually verified.

# Verification & Testing
- All giant cone/tyre scales, positions, colliders, object identities and gameplay behavior remain unchanged. Other scenes and shared imported assets/pipeline/renderer/Volume profiles remain unchanged.
- World silhouettes at every height are in front of cloud/aurora backgrounds; no dome-shaped intersection, planar sky cut or sky occlusion of nearby geometry. Sky effects keep their existing animation and upper-sky appearance.
- Approach/retreat and look up/down/sideways around each giant prop: smooth appearance/disappearance, no sudden far-plane slice, no upper/lower mismatch or solid grey silhouette dissolving against a dark upper sky.
- If fading is used: visibility 1 nearby, smooth intermediate coverage, exact 0 beyond end; repeat at wide capture FOV and camera teleport. Depth/SSAO/shadow outputs match visual coverage; no invisible prop depth or floating shadow remains.
- Preserve normal road fog and nearby lighting/textures/emission/normal detail. Inspect dither grain and shimmer in movement at current anti-aliasing settings; do not silently add TAA or change global rendering to hide a poor fade.
- Material assignments survive runtime Tenkoku updates, Play Mode restart and saved-game/entry-checkpoint reconstruction. No duplicated managers/material leaks/property-block conflicts, repeated exceptions or stale hooks.
- Ordinary pause/HUD/settings, physical capture/death reveal, interference blackout, held flashlight, Radio, key and saves/travel remain unaffected. Test-only travel/restore must not commit or delete user records.
- Compare shader/Console status to baseline. Report actual visual checks and any remaining limitations; Windows standalone, alternative resolutions and full-route traversal remain unverified unless performed.

