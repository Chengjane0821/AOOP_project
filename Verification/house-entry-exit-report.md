# House entry/exit investigation — 2026-11-17

## Result

The two reported failures have **not been reproduced or identified** in this checkout. No gameplay scripts, scenes, prefabs, build settings, or progression rules were changed. Do not treat the diagnostic passes below as a fix or as Quest 3 verification.

The user subsequently narrowed the affected houses to Alchemist (plane) and Elder (tennis), on Quest 3, but could not confirm which fails on entry versus exit. Additional tests for both directions and activity cancellation still passed. `adb devices -l` found no attached device, so on-device logs could not be inspected.

## House comparison

The enabled village is `Assets/Scenes/rpgpp_lt_scene_1.0.unity`. All six houses use the same active transition chain:

`XRSimpleInteractable.selectEntered` → `VillageInteractable.Activate()` → serialized `VillageAction.Use()` → `VillageGame.Enter(index)` → `HouseTransitions`.

Desktop interaction uses a four-metre physics raycast and E to call the same `Activate()` method. Doors are interactable boxes, not automatic trigger portals. All six entrance and exit boxes have enabled, non-trigger BoxColliders, active GameObjects, enabled interaction components, and XRI interaction layer bit 1. Their persistent events target their own `VillageAction.Use()` methods.

| House / scene | Village GameObject / index | Entry and exit | Distinction |
|---|---|---|---|
| `EldersHouse.unity` | `Elder's House entrance` / 0 | Shared additive transition; `EldersHouse - Investigation/Return to village` exits | Entry opens Elder offer. Exit position `(0.056, 1.2, -3.523)` overlaps the copied rustic door, but tested sightlines still hit the exit first. |
| `AlchemistsHouse.unity` | `Alchemist's House entrance` / 1 | Shared additive transition; `AlchemistsHouse - Investigation/Return to village` exits | Dream activity has its own in-room relocation; village transition remains shared. |
| `CarpentersHouse.unity` | `Carpenter's House entrance` / 2 | Shared additive transition; `CarpentersHouse - Investigation/Return to village` exits | No separate village transition implementation. |
| `TechniciansHouse.unity` | `Technician's House entrance` / 3 | Shared additive transition; `TechniciansHouse - Investigation/Return to village` exits | No separate village transition implementation. |
| `ClockmakersHouse.unity` | `Clockmaker's House entrance` / 4 | Shared additive transition; `ClockmakersHouse - Investigation/Return to village` exits | No separate village transition implementation. |
| `AbandonedHouse.unity` | `Abandoned House entrance` / 5 | Shared additive transition; `AbandonedHouse - Investigation/Return to village` exits | Intentionally requires all five fully earned records; this gate was preserved. |

Every house also requires the initial Elder conversation. House activities were inspected only where they bind to the transition system; they were not modified or exercised.

`HouseTransitions` loads the named house with `LoadSceneAsync(..., Additive)`, finds its root `HouseSceneRoot`, binds the persistent village service, makes the house active, hides the configured outdoor roots, and teleports the player to `arrival`. Exit restores outdoor roots, teleports to `VillageGame.entrances[houseIndex]`, and unloads the house. VR relocation uses `XROrigin.MoveCameraToWorldLocation`; desktop relocation temporarily disables its CharacterController.

All six scene names match enabled Build Settings entries. All six root indices, arrival references, exit action indices, and six village return references resolve. Null serialized `VillageAction.game` and XRI manager references in house scenes are intentional: `HouseSceneRoot.Bind()` supplies these after loading. Empty XRI collider lists allow normal automatic discovery of the attached collider.

The disabled `ForgottenVillage.unity` copy also has entrance actions. The editor builders retain an older single-scene teleport fallback and old `House1`–`House6` prefab content. The enabled village has an assigned `HouseTransitions` and an empty `interiors` array, so its house visits use additive loading. Exported house content prefabs do not replace the scene-local transition objects in the enabled house scenes. Copied source-room legacy scene/teleport behaviours are disabled by the house builder; they are not the configured village entry/exit chain.

## Verification actually performed

Unity 6000.0.80f1 compiled and ran the two added Editor diagnostic scripts.

- Read-only Edit Mode geometry audit: six entrances and six exits; collider sightlines from configured return/arrival positions and eight surrounding approaches. All twelve reference sightlines hit the intended interaction box first. No sampled reference body overlaps were reported. Some side approaches hit building/room geometry, so this does not establish reachability from every walking route.
- Initial headless desktop Play Mode: houses 0–4 entered through their serialized `VillageInteractable` event. House 5 entered through `HouseTransitions.Enter(5)` to isolate loading from its progression gate. All six loaded, bound their exit actions, placed the player at arrival, unloaded through their exit interaction event, and returned to the matching village destination. Only the village and current house remained loaded during each visit. All 36 initial assertions passed.
- Follow-up Play Mode tests used `XRInteractionManager.SelectEnter` with a synthetic XRRayInteractor to dispatch the XRSimpleInteractable selection event, exercising the listener installed by `VillageInteractable.Awake`, rather than directly calling `Activate`. All eleven tested door selection paths (five entrances, six exits) had valid collider registration and allowed selection. This is explicit simulated selection, not tracked Quest input or NearFarInteractor hover/input processing.
- Follow-up XRI physics tests used the actual `CurveInteractionCaster` with the Starter Rig far-caster settings: cone detection, 6-degree angle, 10-metre distance, 0.1-metre sphere radius, matching physics mask, and ignored normal triggers. Stabilization was disabled to make deterministic poses. A 5-by-5 aim grid was sampled on each of twelve doors from its configured return/arrival position at a synthetic hand height of 1.2 metres. All 300 samples returned the intended door collider first. This does not establish reachability from every standing position or with every tracked hand pose.
- Elder follow-up: started and stopped tennis without playing a match; verified movement unlock and successful XR exit selection afterward. Alchemist follow-up: entered the dream relocation and returned through `ReturnFromDream(false)` without running the flight challenge; verified movement unlock and successful XR exit selection afterward. The expanded Play Mode suite passed 61 assertions. No puzzle completion, hand-swing, or scoring logic was modified.
- Test writes used `Temp/house-transition-test-save.json`; the player's save path and progression implementation were not modified.
- Headless execution reported unavailable OpenXR runtime and a render-texture error. No headset/controller or rendering success is claimed. The transition assertions passed despite those environment limitations.

Evidence: `Verification/house-transition-geometry.txt`, `Verification/house-transition-playmode.txt`. Initial results are retained in `Verification/house-transition-playmode-before.txt`; the first cone run is retained in `Verification/house-transition-xr-casts.txt`. Full execution logs are in ignored `Temp/house-transition-*.log`. Earlier existing project verification reports were not treated as results of this run.

## Changes made

Only diagnostic assets and this report were added:

- `Assets/TheForgottenVillage/Scripts/Editor/HouseTransitionGeometryChecks.cs` and its `.meta`: read-only scene/collider inspection.
- `Assets/TheForgottenVillage/Scripts/Editor/HouseTransitionPlayChecks.cs` and its `.meta`: focused entry/exit Play Mode checks with an isolated save.
- `Verification/house-transition-geometry-before.txt`, `Verification/house-transition-geometry.txt`, `Verification/house-transition-playmode-before.txt`, `Verification/house-transition-xr-casts.txt`, `Verification/house-transition-playmode.txt`, and this report.

There is no confirmed root cause or justified gameplay fix yet. Existing unrelated workspace changes were preserved; no commits or pushes were made.

## Still needed

On the affected Quest 3/build, reproduce Alchemist and Elder entrance/exit failures from the normal walking route, before/after the relevant activity, with the existing progression prerequisites satisfied. Connect the device with USB debugging authorized so its Unity log can be read. Confirm interaction hover/select feedback, any console/device exception, return-position ground support, and whether the installed build matches this checkout. Then apply a minimal fix to the reproduced failure. Root cause and gameplay fix remain unresolved; no speculative scene changes were made.
