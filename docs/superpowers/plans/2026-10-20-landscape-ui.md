# Landscape UI Implementation Plan

> **For agentic workers:** Execute inline with superpowers:executing-plans. User approved the proposed design and asked to include the gameplay HUD.

**Goal:** Cohesive high-resolution cartoon UI with usable landscape menus, two upgrade columns, skin preview/catalog split, compact gameplay HUD and centered skin roulette.

**Architecture:** Preserve existing uGUI components, navigation, DI and purchases. Add aspect-aware layout components and reuse original serialized references. Author consistent UI sprites and sliced surfaces. Edit scene/prefab assets with Unity serialization APIs.

**Tech Stack:** Unity 2022.3.62f2, uGUI, TextMeshPro, C#.

**Spec:** Approved proposal in this chat, including subsequent request for unified gameplay UI.

## Global Constraints
- Keep user changes, progression, monetization and save data intact.
- Keep portrait usable; landscape reference 1920x1080, portrait 1080x1920.
- Quota plates landscape width and height at most half original (250x120 becomes 125x60).
- Main menu mascot left, primary action and navigation right.
- Upgrades left: 4 repeatable skills; right: 3 once-purchased perks.
- Skins preview left; catalog right with named rarity dividers.
- Skin roulette centered without reserving hidden button space; daily controls retained.
- Audio muted in editor/background.

## Review Focus
- Aspect ratio changes while windows are open: layout and animations remain aligned.
- Russian and English descriptions: text stays within cards and clickable controls.
- Menu reopen: no duplicate cards, working purchases and selection.
- Hidden roulette controls and save-error retry: no empty button reserve or obscured controls.
- Preview camera framing and UI raycasts after two-column rearrangement.

## Tasks
- [x] 1. Capture baseline dimensions, preserve working files, create UI assets and import at high resolution.
- [x] 2. Add adaptive screen layout and two upgrade parents; validate both aspects and resizing.
- [x] 3. Redesign shared cards, rarity separators and main menu; validate typography, localization and interaction.
- [x] 4. Apply coherent HUD surfaces/icons, halve landscape quota plates, compact timer/growth/pause.
- [x] 5. Center skin roulette, preserve daily and error states; adapt remaining popups.
- [x] 6. Compile and inspect Play Mode at 16:9, 16:10, 21:9, compact landscape and portrait; save screenshots and validation report.

## Execution ledger
- Working in place because the open editor and existing uncommitted environment work are the integration target; no branch/reset/commit of unrelated work.
- Unity CLI is current but its Pipeline integration requires Unity 6; this 2022.3 project uses a temporary editor verification harness, removed after verification.

- Integrated 26 scene/prefab/config assets plus 45 unified UI PNGs, 34 skin thumbnails, font, and responsive layout scripts.
- Preserved unrelated environment/gameplay changes and the pre-existing Game scene edit; no save files or test-only project/audio settings copied from validation.
- Fresh Play Mode menu validation: zero errors; purchases, 4+3 cards, rarity groups, portrait reparenting, localization, text bounds and resizing checked.
- Gameplay session started and pause opened at landscape/portrait sizes; all four quota plates measured 125×60 (PASS). llvmpipe required lowering 3D quality only in the validation copy.
- Independent review completed; card text bounds fixed and verified. No remaining runtime findings from that review.
- Evidence and asset provenance: output/ui-landscape/README.md and accompanying reports/screenshots. No standalone player build was run.

## Revision requested from screenshots
- Strengthen service icon strokes; symmetric settings cog; plain coin symbol.
- Fit reward, leaderboard and result windows to landscape bounds; use a white tintable reward frame.
- Use PerkSeparator in both upgrade columns; restore text left and icon/step/price right.
- Increase quota plates to 180×86.4 with 12px logical gaps and a tight sprite; enlarge timer.
- Round joystick and reduce landscape visuals/travel by three; use sliced growth fill with separate label.
- Validate all popup bounds, rarity variants, descriptions, purchase state, gameplay spacing and joystick travel in Play Mode.
- Current evidence: output/ui-landscape/revision/.
- Revision complete: 9 scene/prefab assets integrated, 51 UI PNGs total; menu validation errors=0, popup bounds PASS, quota spacing 12px PASS, 180×86.4 plates PASS, joystick diameter/travel 86.67/40 PASS. git diff --check clean.

## Money, thumbnails and settings follow-up
- Restored the original money reward subjects as six high-resolution ImageGen illustrations; five reward variants bound to the existing amount thresholds.
- Restored original skin thumbnail camera using SkinItemFactory at 512px for all 34 skins.
- Unified section headers on PerkSeporator, size 36/Bold/uppercase; restored full rarity colors.
- Enabled the existing UI particle systems in the reward popup; verified emission and UI renderers.
- Made slider child visuals follow its Rect Transform dimensions; documented exact hierarchy and Width/Height controls.
- Fresh Play Mode suite errors=0; separate loading-icon check PASS. Current changed files pass diff whitespace checks. Other existing modified UI files were left untouched.
- Evidence: output/ui-landscape/polish/README.md and SLIDERS.md.

## Price chip shape and growth alignment
- User clarified the issue is the price background, not the coin artwork. Menu/SkinsWindow/BuySkinButton/Price Image sliced Pixels Per Unit Multiplier changed from 1 to 2, matching WindowHeader/Balance. Both sprites share the same corner geometry; multiplier 1 doubled the visible corner radius.
- Game/GrowthBarContent/Background moved from y=0 to y=6, aligning the track, fill and label with the visible Card face, whose lower shadow makes its visual center differ from its transform center.
- Verified in Play Mode: price and balance multiplier=2, shop screenshots portrait/landscape; growth center y=6 PASS, gameplay portrait/landscape/ultrawide/compact. Scoped scene diff whitespace check passed. Evidence: output/ui-landscape/centering/.

## Reset confirmation typography
- Expanded confirmation panel and body with portrait/landscape placements in existing ResponsiveCanvasLayout. Body left-aligned; body and both button labels fixed at size 40, no independent autosizing. Button labels fit one line. Confirm uses ButtonCoral, cancel remains green.
- Play Mode verification passed for RU portrait/landscape, EN landscape, TR portrait: all text within bounds, panel within screen, matching font sizes, cancel closes confirmation. No reset action invoked. Evidence: output/ui-landscape/reset/.

## Mystery skin reward icon
- Replaced roulette secret reward gift reference with a generated high-resolution transparent T-shirt on gold hanger and large question mark, matching the currency illustration family. New asset MysterySkin.png 1254×1254 with standard uncompressed UI sprite import; no runtime code changes.
- Play Mode portrait/landscape PASS: full-size sprite loaded, bound to active wheel, preserveAspect=true; screenshots inspected. Evidence: output/ui-landscape/mystery-skin/.

## Simplified mystery skin icon
- Replaced MysterySkin artwork in place, preserving its sprite GUID and roulette reference. Flat smooth shirt, large question mark, plain black hanger; removed folds, seams, highlights and fabric detail. Transparent 1254×1254 PNG.
- Fresh Play Mode verification passed at 1080×1920 and 1920×1080; simplified sprite loaded full resolution and displayed in the active wheel. Updated screenshots inspected.

## Bolder countdown icon
- Kept accepted simplified MysterySkin icon integrated in roulette.
- Generated Timer.png replacement with heavy rim/hands and four broad ticks; preserved sprite GUID/import settings. Enlarged Game scene icon from 66×66 to 84×84 within existing timer plate.
- Play Mode: timer geometry and full-resolution import PASS; landscape, ultrawide, portrait and compact HUD screenshots captured, gameplay shot inspected. Evidence: output/ui-landscape/timer/. Scoped scene whitespace check passes.

## Pause/results and price overlap follow-up
- Menu price uses relative vertical anchoring below a separate title region; landscape buy button height increased to 180 with retained bottom margin. Price stays inside the visible surface.
- Pause panel 960×960; enlarged language/home/reset/close buttons; language below matching SFX/music sliders, home below language. Both sliders 509.416×100 with equal 100×100 handles; audio references unchanged.
- All six result buttons doubled exactly in both dimensions. One row in landscape, two rows in portrait; separate red fail palette. Rescue text/arrow separated to prevent existing overlap.
- Targeted Play Mode checks passed: shop, pause with/without home, both results, portrait/landscape and compact results, text overflow and viewport/button bounds, English win text, runtime errors=0. Evidence: output/ui-landscape/pause-results/. New changed lines whitespace check passed; pre-existing Unity YAML trailing whitespace retained.

## Shared separator and editable menu layouts
- Kept one UISeparator prefab: renamed active PerkSeporator with GUID preserved, deleted unused Skins/PerkSeparator. Rarity group now nests the same prefab, retaining only content/container overrides. Shared font size 36/Bold and styling verified in upgrades and shop.
- Five menu navigation buttons now 460×210 in both profiles, including blue roulette. Icons doubled exactly and moved to a separate left area; labels have their own right area. Play icon doubled too, with matching button height clearance.
- MainMenuWindow now stores valid portrait geometry for edit mode; reset old offset containers. Added explicit ApplyOrientation API and custom Inspector preview/capture buttons with Undo. Runtime automatic orientation behavior retained.
- Play Mode checks PASS: RU portrait/landscape/ultrawide, EN and TR, button/text bounds and icon separation, reuse and language updates of four rarity headers. Runtime errors=0. Edit Mode custom Inspector preview/capture test PASS. Evidence and designer instructions: output/ui-landscape/main-menu/LAYOUTS.md.

## Menu input failure from inactive popup container
- Reproduced current user failure without hiding daily popup in the test: RuntimeWindows was disabled in the scene. Auto daily popup blocked main screen via MenuPopupStack but remained inactive under its parent.
- UiSpawner.SetParent now activates its runtime container; saved scene RuntimeWindows active. Restored only menu profiles and button/icon geometry that had reverted to older serialized values in the main scene.
- Regression test deliberately disables RuntimeWindows before Play Mode, confirms startup popup visibility, closes it using Input System mouse events, clicks all five menu buttons and each close button in portrait and landscape, confirms stack/input recovery, and clicks Play to load Game. No direct onClick or ExecuteEvents shortcuts. Runtime errors=0.
- Main Menu.unity and UiSpawner.cs byte equality with tested copy verified after run. Evidence: output/ui-landscape/input/.

## Editor preview verification and feedback
- Tested current Menu.unity through actual custom Inspector callbacks outside Play Mode, capture of manual position/size, profile switching, save/reopen, and Play Mode orientation changes. All PASS. Physical Inspector button clicks were not simulated; user-reported no-op is not reproduced.
- Inspector now reports target name/count, Game View size, last selected profile and applied/captured status; warns about empty Elements, inactive hierarchy and mismatched aspect. Forces editor/Game View repaint after operations. Capture explicitly records prefab modifications and scene dirty state. Main scene untouched.
- Evidence and editing steps: output/ui-landscape/editor-preview/.

## Capture-only Inspector (user request)
- Removed both preview buttons, their callback and preview/profile mismatch state. Retained two capture buttons, target count, Game View size and capture confirmation. Runtime orientation behavior unchanged.
- Updated designer instructions to explain manual editing and separate profile recording without edit-mode profile application.
- Fresh Unity batchmode compilation and actual Inspector capture callbacks PASS for Portrait/Landscape, no movement on capture, save/reopen persistence. Test scene changes confined to clone. Evidence: output/ui-landscape/editor-preview/capture-only.txt.

## Menu background empty profile cleanup
- Screenshot showed MenuBackground, which had a redundant scaler-only ResponsiveCanvasLayout with no targets. Removed that component from Menu.unity: its solid Image already stretches to all Canvas edges. No replacement script or manual target setup required.
- Verified all three remaining menu scene profiles have assigned targets; MainMenuWindow has 7. GUI Unity check confirms background covers full Game View at 1080x1920 and 1920x1080 without the component. Scene diff limited to its component reference and serialized component block. Evidence: output/ui-landscape/editor-preview/menu-scene-check.txt.

## Main window owns its background
- Moved existing Background Image to first child of MainMenuWindow; removed separate MenuBackground Canvas/Scaler/container and MenuNavigation background field/validation/toggling. Image remains stretched, decorative raycast disabled. Existing window backgrounds and authored profiles preserved.
- Play Mode regression through Input System mouse events PASS in portrait/landscape: background covers screen, hides with main in upgrades/skins, returns on close; settings/leaderboard/daily popup and Play transition work, runtime errors=0. Source and scene match tested clone. Evidence: output/ui-landscape/background-ownership/.

## Prefab Mode capture and complete prefab audit (2026-10-21)
- Audited all 20 UI prefabs. Populated 10 ResponsiveCanvasLayout components across 9 window/canvas prefabs: 131 targets, plus complete 32-target buttonless roulette variant. Existing 17 authored placements preserved. Layout-driven content and Slider fill/handles excluded.
- Added editor-only PrefabLayoutPreview surface in Scene View with real 1080x1920 / 1920x1080 dimensions, including Prefab Stage environment Canvas. Restores original Canvas/Scaler/root Rect state before saving, reapplies surface after save, restores on stage close/domain reload, supports Undo/Redo. Kept only two capture buttons; orientation and roulette variant use dropdowns.
- Roulette geometry moved from hardcoded runtime values to serialized profiles for both variants; single visible action still centers. Pause initialization no longer overrides a profile-owned language position.
- FillUI progress TMP moved under its own editable ProgressLabel while keeping TMP component reference. Removed missing obsolete component from unused RouletteSectorCard; source had no inbound references.
- Actual Prefab Stage checks PASS for every profile/variant: changed positions and sizes, capture through real custom Inspector handler, File/Save, close/reopen, verify every target. Both surface dimensions, save restoration, and Undo/Redo checked. Physical Inspector clicks were not simulated.
- Runtime regression PASS: Input System mouse navigation in portrait/landscape, results/reward bounds and visible sizes, roulette authored placements, language position not overwritten, Play loads Game, no runtime errors. Separate immediate Game View render captures inspected for results/reward/buttonless roulette. Main assets/code match tested clone byte-for-byte after integration. No player build.
- Evidence and workflow: output/ui-landscape/prefab-profiles/README.md.

## Pause orientation fix
- Default Prefab Mode preview follows Game View, starts on stage open/domain reload, applies orientation on opening/rotation, preserves edits on same-orientation resize. Explicit fixed presets remain optional. CanvasScaler Expand logical sizing retained.
- Pause main Landscape panel 1360x900; Portrait 960x960. Removed delayed surface restoration after save; restores within prefabSaved.
- Targeted Prefab Mode save/reopen and automatic follow with child selected PASS; runtime both PauseMenu contexts/orientations, bounds and Slider state PASS. Fresh nine-prefab capture/reopen matrix and 20-prefab audit PASS before final stage-open activation addition; targeted opening/follow checked afterward. Main files match tested clone. Evidence: output/ui-landscape/pause-auto-follow/.

## Responsive UI system folder
- Moved the Layout runtime folder and Editor/UI folder, with all original metadata, to Assets/MadSlime/ResponsiveUI/Runtime and Editor. Added a README code map. Source contents, names, GUIDs and assembly ownership unchanged; no behavior changes.
- Verified file hashes, original paths removed, asmref scope and existing scene/prefab GUID references. Both runtime and editor assemblies compiled successfully with Unity Roslyn using project response files and relocated source/output paths. Full headless Unity import was blocked by the existing project lock; lock untouched. Evidence: output/responsive-ui-move/.
