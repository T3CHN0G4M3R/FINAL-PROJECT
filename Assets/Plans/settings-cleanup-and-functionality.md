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
