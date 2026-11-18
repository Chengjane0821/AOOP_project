# Technician: Planetary Defence

The Technician's existing room now contains a Planetary Defence launch terminal
instead of the PowerGrid panel. Starting the game teleports the shared player into
a large isolated space arena 250 metres away in the same house scene, following the
existing plane game's remote-area approach. Leaving, failing or winning restores
the room and returns to its original arrival point. No extra scene load or house
transition changes are required.

The planet is 2.6 metres across, enemies are 0.75 metres tall and the approach ring
has a 3-metre radius, viewed from 5.5 metres away. The room is hidden during play;
a dark space background and distant stars replace the tabletop framing. VR entry
aligns the existing XR Origin toward the action without changing tracked head pitch.

Start from the village, complete the Elder introduction, enter Technician's house
and speak to the Technician or select PLAY DEFENCE. Choose **Defend planet**.
After a three-second countdown, 20 enemy ships approach the central planet.
Aim at an enemy and use the existing XR **Select** control to launch a homing
interceptor. Desktop uses a mouse click. A ship can have one interceptor assigned;
shots have a 0.22-second cooldown. All threats must be resolved with at least one
of the planet's five integrity points remaining to win. Five impacts fail the run.
Menu/Escape pauses; Resume, Retry defence and Leave defence use the shared menu.

## Assets and implementation

The imported Planetary Defence package is a legacy 2D desktop game. This integration
uses its `PlanetDetailed`, `Enemy1`–`Enemy3`, `Rocket`, and `PlanetShield` artwork
as transparent URP unlit quads in the isolated arena, plus `Shoot`, `EnemyDeath`
and `EnemyHit` audio. Original desktop singletons, cameras, PlayerPrefs high scores,
2D physics, pickups and post-processing are not instantiated. The imported
`SquareBullet` texture also provides small distant stars. The package's enemy,
projectile, pickup and particle prefabs and other sprite/PSD resources remain intact.

Eight pooled threats, eight pooled interceptors and six pooled shield flashes bound
runtime object counts. Threats use an enabled SphereCollider, kinematic Rigidbody
without gravity and XRSimpleInteractable on the normal interaction layer. Their
colliders are non-trigger so the existing Starter Rig's ignored-trigger ray/near
casts can select them. Interceptors use continuous target-distance checks rather
than unstable fast Rigidbody contacts. All movement and effects freeze when paused.
No additional camera, XR Origin, input actions or external dependency is installed.
Leaving restores the previous sky/fog, camera clear settings, quest tracker and
player controls. Unloading the house during play respects the village return
destination rather than teleporting back into the disappearing room.

`PlanetaryDefenceSession` owns spawn cadence, five-hit failure, duplicate-resolution
protection and the requirement to resolve all 20 threats. `PlanetaryDefenceRun`
owns the stationary view, pooled actors, selection callbacks, pause/retry and outcome.

## Progression compatibility

`PowerGridHouse` remains the Technician progression adapter and resident binding.
The active scene points to `defence`; its old `run` reference is cleared and the
old gameplay root/components are removed. Existing PowerGrid source, rule tests
and historical builder remain available; normal village rebuilding installs the
replacement. No save schema migration is needed: success still earns
`technicianGridWon` and `puzzleSolved[3]`. Existing recovery, RUN/STOP evidence,
cell-phone quest and fourth clue remain necessary. Already-earned valid saves
continue to work. Arduino handling and house transitions are unchanged.

## Carpenter tools

`CarpenterToolsBuilder` replaces only the children of the original `Body dodge
warning` transform. Hammer, hand saw and crowbar come from the imported
`Assets/CarpenterToolSet/prefabs` folder; small nails were left unused so obstacles
remain recognizable. Generated URP materials reuse the imported albedo textures.
Original materials/prefabs are untouched.

The original 60-second cue scheduler, movement, head-displacement collision rules,
target contacts, score, goal, victory/failure, retry and save behavior are unchanged.
`WorkshopToolVisual` cycles tools by cue index. A separate compensation frame keeps
their proportions under the warning's existing nonuniform scale; an inner pivot
orients a tool horizontally for duck cues. Models are centered and normalized to
0.42 metres (hammer), 0.50 metres (saw) and 0.56 metres (crowbar). Trigger-only
bounding colliders follow those visuals, on Ignore Raycast;
imported solid colliders and Rigidbody components are omitted from these scene
instances, so tools cannot push the player or intercept target-selection rays.
The side instruction card temporarily hides during a dodge warning so it cannot
cover the incoming tool, then returns afterward. The existing main direction/score
HUD moves slightly upward for the same warning, leaving the tool visible, and
returns to its authored position after the warning.

## Editor setup and checks

The saved Technician/Carpenter scenes and content prefabs contain the references;
no manual Inspector wiring is expected. Editor menus under Forgotten Village:

- Install both imported mini-game replacements.
- Install Technician Planetary Defence.
- Replace Carpenter flying obstacle visuals with tools.
- Test imported mini-games and progression.

`Verification/imported-mini-games-*.txt` and the corresponding XML contain results
from this task. Physical Quest controller reach/selection, stereo appearance and
standalone performance still require acceptance testing on the device.
