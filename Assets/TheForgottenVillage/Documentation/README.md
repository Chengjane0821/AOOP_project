# The Forgotten Village — implementation and launch guide

## Launch
Open `Assets/Scenes/rpgpp_lt_scene_1.0.unity` in Unity 6000.0.80f1 and press Play, or build and launch the first enabled Build Settings scene. The original village is the main outdoor world and is integrated with the game systems. The source `Assets/Scenes/My VR room.unity` remains byte-for-byte unchanged; the pre-integration village copy is `Assets/TheForgottenVillage/Scenes/VillageEnvironmentReference.unity`.

The project targets Windows Standalone 64-bit, URP, XR Interaction Toolkit 3.1.3 and OpenXR. The Oculus Touch interaction profile is enabled. Connect Quest Link and make the installed Meta/OpenXR runtime active before starting Play for headset use. A headset and physical controllers still require an on-device acceptance pass.

Without a headset, the player selects desktop mode automatically. Click the Game view to focus it. WASD moves, mouse movement looks around, E interacts with a targeted object within four metres. Mouse clicks operate open panels. Escape opens/closes the field menu; J opens the journal; I opens the inventory. Movement is disabled while a panel is open. VR uses the existing XRI starter action bindings: controller rays and near interaction, movement and snap turn. The left-controller menu input opens the field menu. Floor teleportation is available in the interiors.

## Gameplay route
Start Game opens the arrival message. Speak to the gold-hatted Village Elder beside the arrival area and finish all four dialogue lines. Six named entrances are placed at existing village building perimeter anchors. The first five houses may be completed in any order. Every interior has a teal return control.

- Elder: inspect the scratched photograph, measure the unloaded and loaded divider, then set RL approximately 20 kilohms and validate. The physical lever switches the load. Desktop players may use E on it or the panel toggle.
- Alchemist: use the potentiometer or panel +/- controls to select approximately 5 kilohms with C=100 nF. Validate both the 100 Hz passband and 5 kHz rejection requirements.
- Carpenter: set RD approximately 6.75 kilohms for gain near -8 with gm=2 mS and RL=10 kilohms. Workshop parts can also be grabbed and placed in the measurement socket; that optional interaction does not replace the gain puzzle.
- Technician: run the deployed pass-by-value diagnostic, apply the const-reference fix, run it again, and validate. The USB button and simulation button invoke the same diagnostic event. Faulty RUN and corrected STOP results are saved.
- Clockmaker: measure the original T=10 ns path, then find the minimum passing clock period, 11.5 ns. Negative slack is described as a potential timing violation, never as a proven incident cause.
- Abandoned House: unlock with five clues, review all four evidence stations, then use the verified safe-state console. The game validates evidence before sending the physical gate command. The reveal and replay/menu options follow.

Inventory and clue journal are functional paged panels. Evidence and ordinary specimens are stored by stable names, avoiding duplicate rewards. New Game resets progress; Continue restores the save and returns to the village.

## Files and architecture
Runtime source is under `Assets/TheForgottenVillage/Scripts`:

- `Core/VillageGame.cs`: main progression, scene references, transitions, rewards, final authorization and save storage.
- `Core/VillageSave.cs`: versioned serializable progress, clue/evidence flags and inventory.
- `Core/VillageAction.cs`, `Core/VillageInteractable.cs`: persistent scene action bindings shared by desktop E and XR selection.
- `Core/HouseLockDisplay.cs`: visible abandoned-door lock state.
- `Core/VillageAudio.cs`: generated confirmation, reward and ambient audio.
- `Player/VillagePlayer.cs`: mutually exclusive XR/desktop control and transitions.
- `Puzzles/Engineering.cs`: pure circuit and timing calculations.
- `Puzzles/EngineeringStation.cs`: five configured station types, measurements, diagnostics, traces and validation.
- `Puzzles/WorkshopSocket.cs`: optional trigger-based component inspection.
- `Hardware/SerialProtocol.cs`, `Hardware/HardwareConsole.cs`: parsing, threaded serial transport, simulation, bounded message queue and acknowledged gate requests.
- `UI/VillageUI.cs`: world-space title/menu/dialogue, puzzle controls, journal, inventory, settings and ending.
- `Editor/VillageBuilder.cs`: repeatable scene generation using Unity Editor APIs and source assets.
- `Editor/VillageAutomation.cs`: repeatable scene construction and original vertical-slice checks.
- `Editor/SeparateHouseBuilder.cs`: duplicates the actual room scene into six independent additive scenes, wires the original village entrances, validates scene roots/references and camera/listener exclusivity.
- `Editor/HouseSceneChecks.cs`: additive-scene integration playthrough and rendered captures.

The game uses the original village scene plus six additive house scenes generated by opening and duplicating `My VR room`. Each copy retains its source room hierarchy; its source XR rig, camera, audio listener and event/interaction managers are disabled. A single persistent village rig moves to the house spawn and returns before the additive scene unloads. Shared quest, inventory, journal, UI and hardware services remain in the village scene. The first five houses have independent engineering stations, narrative displays and records; the final scene has evidence stations and the verified safe-state console. Runtime actions use serialized scene references and UnityEvents rather than `GameObject.Find`.

Generated prefabs: `House1.prefab` through `House6.prefab`, and `VRRoomFoundation.prefab`. The foundation is extracted from the rustic floor in My VR room. Furniture, shelves, storage, lamps, tables, monitor, notebooks, artwork and the lever are reused from `_Course Library`. `Verification/reused-assets.txt` records source asset paths. Retain the existing asset licence files when distributing the project. No third-party art was downloaded or relicensed.

House prefabs contain their local geometry and station components; the complete scene carries the cross-house game references. Rebuilding supplies these references automatically. A raw house prefab dropped into an unrelated scene is not a standalone game.

## Persistence
`forgotten-village-v1.json` is stored under `Application.persistentDataPath`. Writes use a temporary file plus atomic replacement and a `.bak` recovery copy. Only values and IDs are serialized, never scene objects. The test runner redirects its save to `Verification/test-save.json`.

## Regeneration and checks
`Forgotten Village > Build six copied house scenes` rebuilds the additive rooms from the preserved source room and integrates them into the village. `Forgotten Village > Validate six copied scenes` checks each room, puzzle, missing scripts, active cameras/listeners and entrance wiring. `Forgotten Village > Run integrated Play Mode checks` exercises the additive transitions and game callbacks. Let script compilation finish before running checks; automated checks are not a substitute for a human VR playthrough.

The model harness in `Verification/ModelTests.cs` tests the actual calculation, save-model and protocol source. `Verification/Compile.ps1` checks C# against the installed Unity reference assemblies. Read `Verification/house-playtest-results.txt`, `Verification/separate-scene-validation.txt`, `Verification/model-tests.txt` and `Verification/firmware-tests.txt` for actual results.

## Hardware
See `HARDWARE.md`. Firmware is `Arduino/ForgottenVillageController/ForgottenVillageController.ino` at project root. Simulation is on by default and requires no serial device.

## Remaining physical validation
Quest Link tracking, comfort, controller bindings and grabbing must be verified with a connected headset. Uploading and exercising the Uno/SG90 requires the real hardware and Arduino toolchain. Acknowledgment means the firmware accepted a command; there is no servo-position sensor, so it does not prove the gate physically moved. No physical hardware pass is claimed.
