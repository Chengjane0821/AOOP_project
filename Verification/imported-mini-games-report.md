# Planetary Defence and Carpenter tools — 2026-11-18

## Delivered behavior

PowerGrid's active gameplay has been replaced inside the existing Technician
house. The room now has a launch terminal. Starting Planetary Defence teleports
the existing player/XR Origin into a large space area 250 metres away in the same
scene, using the existing plane game's isolated-area pattern. The 2.6-metre planet,
0.75-metre enemies and 3-metre approach ring occupy a much larger view than the
initial tabletop version. There is no additional Unity scene or Build Settings
entry. Room geometry is hidden during play; leaving, failure and victory restore
the house, camera/sky settings, tracker and original arrival position.

Twenty ships approach the planet. Normal XR Select locks a ship and launches a
homing interceptor; desktop mouse clicks use the same firing path. Clear all 20
threats while retaining at least one of five integrity points. Five impacts fail
the run. Pause freezes the actors and clock; retry resets the wave. Eight threats,
eight interceptors and six flashes are pooled. Actor colliders/layers are compatible
with the existing Starter Rig; no extra camera, XR rig, input bindings or physics
projectile dependency was introduced.

Carpenter retains its original cue schedule, obstacle path, head/hand collision
rules, 60-second duration, 900-point goal, score, reward and save code. Only the
obstacle's visual children were replaced. Hammer, hand saw and crowbar rotate
through existing dodge cues. Their lengths are 0.42, 0.50 and 0.56 metres. A scale
compensation frame prevents the old warning's flattened scale from deforming tools.
An inner pivot orients the tools; trigger-only bounds on Ignore Raycast follow the
visuals. Imported Rigidbody and solid collider components are omitted from these
scene instances. The instruction card hides and the existing score/direction HUD
moves upward only during tool warnings, then both restore their authored state.

## Imported assets inspected and used

| Pack | Available resources inspected | Used |
|---|---|---|
| `Assets/PlanetaryDefence` | Legacy 2D game scripts and scenes; four enemy prefabs, RocketBullet, three pickups, three particle-effect prefabs; planet/enemy/rocket/shield/bullet sprites and PSD source artwork; audio; font; old image effects. No `.anim` clips found. | `PlanetDetailed.png`, `Enemy1.png`–`Enemy3.png`, `Rocket.png`, `PlanetShield.png`, `SquareBullet.png` (distant stars); `Shoot.wav`, `EnemyDeath.wav`, `EnemyHit.wav`. |
| `Assets/CarpenterToolSet` | Hammer, hand saw, crowbar and nail meshes/prefabs; albedo, normal, AO and metallic/smoothness textures; example scene; Standard materials. | `prefabs/hammer.prefab`, `prefabs/hand_saw.prefab`, `prefabs/Crowbar.prefab`, their meshes and original albedo textures through new URP materials. Small nails were left unused for readability. |

The imported asset files themselves were preserved. The legacy Planetary desktop
singleton game/camera, PlayerPrefs high scores, 2D physics and post-processing are
not loaded into the RPG. Artwork is presented as unlit textured quads in 3D space;
this integration does not claim the pack contains a volumetric planet model.

## Progression and scope

`PowerGridHouse` is retained as the Technician progression adapter. Its new
`defence` reference is assigned; the obsolete `run` reference is null. Success still
sets `technicianGridWon` and `puzzleSolved[3]`. Existing memory recovery, deployed
RUN / repaired STOP evidence, cell-phone verification, unique fourth clue and
save schema 8 remain in place. Already-earned valid progress does not require a
new migration. No Arduino behavior was changed.

The active PowerGrid gameplay root and its controls/cables/machines were removed
from the Technician scene and content prefab. Existing PowerGrid source and pure
rule tests remain as historical/compatibility code; normal rebuild and test
entry points now route to the replacement. No existing source files, scenes or
imported asset packs were deleted. No commits or pushes were made.

`imported-mini-games-scope.txt` confirms the village scene/build list, transition,
save/progression, player/XR, hardware, other mini-game code and Carpenter
rules/input/movement/view source are unchanged. The checked copied-room serialized
objects and every original prefab instance placement/override are unchanged in
both house scenes. Builder snapshots also compare original arrival, exit action,
exit position, house index and copied-root bindings before/after installation.

## Verification

- Unity 6000.0.80f1 successfully compiled the runtime and Editor scripts.
- **108 Edit Mode tests passed, 0 failed, 0 skipped**, including the new countdown,
  spawn cadence, duplicate-resolution, win/failure and clean-retry rules plus the
  existing village/save/game rule regressions.
- The focused imported-games Play Mode suite passed **29 assertions**: real
  additive house loading; space teleport/hide/restore; actual enemy movement;
  pause/resume; five-impact failure; retry/leave; unloading a running arena without
  overwriting the village return; a real injected mouse click and simulated XR
  selection launching moving interceptors; a complete 20-interception victory;
  original Technician recovery/evidence/clue/save flow; all three flying tools;
  unchanged 60-second Carpenter victory and clue path; and house exits. No runtime
  errors were captured in the passing run.
- After the final tool HUD positioning adjustment, the existing complete Carpenter
  Play Mode suite passed **29 assertions**, including real 60-second failure and
  success, retry, keyboard/mouse and tracked-pose paths, pause, unique reward, disk
  save, exit/re-entry, recenter and title cancellation. No runtime errors captured.
- Six-house scene validation passed: expected room roots/arrivals, no missing
  scripts, no enabled copied cameras/listeners; replacement pools/adapter and
  Carpenter references are populated. Generated URP materials and meshes were
  inspected through rendered previews.
- The enlarged space preview is `planetary-defence-play-preview.png`. A short
  tool presentation preview additionally checks that warning UI moves/restores
  and captures `carpenter-flying-tools-preview.png`.

The focused suite uses temporary save files under `Temp/`. The existing Carpenter
suite uses its designated `Verification/active-workshop-test-save.json` fixture.
The player's normal save path was not used. Object recognition was explicitly
simulated. Earlier interrupted attempts and development errors remain in diagnostic
history; only the completed passing reports above are acceptance evidence.

Transition tests check the immediate configured teleport destination before normal
CharacterController ground settling, followed by completed scene unload. Physical
ground clearance on the Quest is still a manual acceptance check.

## Files changed

Modified gameplay assets:

- `Assets/TheForgottenVillage/Scenes/TechniciansHouse.unity`
- `Assets/TheForgottenVillage/Scenes/CarpentersHouse.unity`
- `Assets/TheForgottenVillage/Prefabs/TechniciansHouseContent.prefab`
- `Assets/TheForgottenVillage/Prefabs/CarpentersHouseContent.prefab`

Modified code:

- `Scripts/PowerGrid/PowerGridHouse.cs` — replacement binding and guarded original reward.
- `Scripts/UI/VillageUI.cs` — only the new defence pause route and Technician journal instructions.
- `Scripts/Editor/PowerGridBuilder.cs` — normal builder routes to replacement; historical builder retained.
- `Scripts/Editor/PowerGridAutomation.cs` — replacement test menu name.
- `Scripts/Editor/PowerGridChecks.cs` — accepts the replacement rather than requiring obsolete grid actors.
- `Scripts/Editor/PowerGridPlayChecks.cs` — replacement test/progression helper routing.
- `Scripts/Editor/ActiveWorkshopBuilder.cs` — installs imported obstacle visuals during normal rebuild.
- `Scripts/Editor/ActiveWorkshopChecks.cs` — permits trigger-only visual tool bounds, rejects solid physics.

Paths in this code list are relative to `Assets/TheForgottenVillage/`.

Added code (each with its Unity `.meta`):

- `Scripts/PowerGrid/PlanetaryDefenceSession.cs`
- `Scripts/PowerGrid/PlanetaryDefenceRun.cs`
- `Scripts/PowerGrid/PlanetaryDefenceThreat.cs`
- `Scripts/PowerGrid/PlanetaryDefenceProjectile.cs`
- `Scripts/PowerGrid/PlanetaryDefenceControl.cs`
- `Scripts/ActiveWorkshop/WorkshopToolVisual.cs`
- `Scripts/Editor/PlanetaryDefenceBuilder.cs`
- `Scripts/Editor/CarpenterToolsBuilder.cs`
- `Scripts/Editor/ImportedMiniGameAutomation.cs`
- `Scripts/Editor/ImportedMiniGamePlayChecks.cs`
- `Tests/Editor/PlanetaryDefenceTests.cs`

Added materials under `Assets/TheForgottenVillage/Materials/` (each with `.meta`):

- `Defence PlanetDetailed.mat`, `Defence Enemy1.mat`, `Defence Enemy2.mat`, `Defence Enemy3.mat`
- `Defence Rocket.mat`, `Defence PlanetShield.mat`, `Defence SquareBullet.mat`
- `Defence controls navy.mat`, `Defence platform.mat`
- `Flying tool hammer_material.mat`, `Flying tool handsaw_material.mat`, `Flying tool crowbar.mat`

Documentation: new `PLANETARY_DEFENCE.md` and this report; updated `POWER_GRID.md`
to mark the grid as historical and `ACTIVE_WORKSHOP.md` to identify the new visuals.
Task-specific build/test/scope reports, XML and previews use the
`planetary-defence-*`, `carpenter-tools-*` and `imported-mini-games-*` prefixes in
`Verification/`. Existing scene-check and Carpenter regression output files were
regenerated; the existing Editor diagnostic log retains its history.

Pre-existing unrelated changes (including the Clock arcade material, imported
asset folders, prior house diagnostics and NetworkManager assets) were preserved.

## Inspector setup and manual acceptance

**No manual Inspector wiring is required.** The two saved scenes and content
prefabs contain their references. Start through the village as usual. The launch
terminal / Technician invitation starts Planetary Defence. Editor installation
and focused test commands are available under Forgotten Village.

Still test physically on Quest 3: normal left/right controller Select behavior,
stereo transparency, readable field/HUD extent for seated and standing heights,
comfortable yaw alignment, real dodging/tool recognition, return-floor clearance,
and standalone frame rate/memory. No Quest device playthrough, standalone Android
build or performance measurement is claimed. Live Pi camera/Arduino acceptance
was not performed; those integrations were not changed.
