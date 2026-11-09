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
