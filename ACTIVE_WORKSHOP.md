# Carpenter: Quick Hands Workshop

Quick Hands replaces the Carpenter's former Bridge Delivery activity with a
60-second body-action arcade. Reach for highlighted targets, touch paired targets
with two hands, and react to striped-board warnings by leaning or lowering your
head. A three-second ready period precedes the timed challenge.

## Rules and feedback

- Reach the score goal of **900** before the completed 60-second run is judged.
  Reaching the goal early does not skip the remainder of the challenge.
- Each retry generates a fresh seeded pattern. The three 20-second stages use
  shorter response windows, more varied target positions and paired reaches.
- Single targets accept either hand. A pair needs different hands on different
  targets within 0.4 seconds. A controller must leave and enter a target again;
  holding a hand inside one target cannot repeatedly collect it.
- Left, right and duck warnings appear before an obstacle approaches. Maintain
  the indicated small movement continuously to complete the dodge. These
  obstacles are visual feedback and do not physically push the player.
- Successful actions build a combo and a multiplier up to x4. Misses break the
  combo; a failed dodge also removes a small number of points. The large dark-navy
  HUD shows score, time, combo and the permanent 900-point goal. Each active
  target has its own shrinking life bar so its response window is visible.
- Completed runs save a personal best, including a completed run below the goal.
  Leaving an unfinished run does not save its temporary score.

## Controls and calibration

**VR:** touch highlighted targets with the tracked controller positions. Paired targets
need both controllers. Lean left/right or lower your head when indicated. No jump
is required. The starting pose sets a neutral head position and places the targets
within a nearby two-row reach area. Stand or sit comfortably before starting.
Actual headset tracking controls the view; the game does not force its pitch.

**Desktop:** point at a highlighted target and click the left or right mouse button
for the corresponding hand. For a pair, hold both mouse buttons while clicking
either highlighted target. Hold `A` / `D` to lean left/right; hold `S` to duck. The
view moves with these actions and returns to its resting position on release.

Use `Esc` / the controller menu, or the physical PAUSE control, to resume, restart,
recenter or leave. FIT REACH / Recenter reach during a run measures the neutral
pose again and resumes the activity. Time, target windows and animation freeze
while a menu is open. If the headset or both controllers remain untracked for
more than 0.75 seconds, the game opens its pause menu; restore tracking before
resuming or recentering. Both controllers must be tracked for paired reaches.

## Story and saved progress

Start from `Assets/Scenes/rpgpp_lt_scene_1.0.unity`, complete the Elder's arrival
dialogue and enter the Carpenter's house. Talk to him or select PLAY, then choose
"Let's play!". A passed run unlocks his recovered memory. Choose "Remember
together", complete the existing real-world mouse camera quest and receive
Clue #3, the Analog Front-End Record. Replay cannot duplicate that reward.

Save schema **8** records `carpenterArcadeWon`, `carpenterMemoryRecovered` and
`carpenterBestScore`. Fully earned valid mouse records from older saves migrate;
an unfinished bridge activity does not become a Quick Hands victory. Other house
memories, object-verification requirements and final evidence checks retain
their roles. [BRIDGE_DELIVERY.md](BRIDGE_DELIVERY.md) is the retired design's
historical record.

## Sources and verification

Runtime sources are in `Assets/TheForgottenVillage/Scripts/ActiveWorkshop`.
`Editor/ActiveWorkshopBuilder.cs` saves the Carpenter scene/prefab while preserving
the six copied source-room roots, original arrival/exit and shared player rig.
Presentation follows the existing plane and table-tennis games: an open central
playfield, a large flat navy HUD above it, and an independent instructions panel
on the left. Six large matte targets appear directly in the play space; quiet
inactive markers, clear accent colours and target life bars keep the next action
visible. The field is free of an enclosing cabinet, shiny frames and decorative
gold rings. Meshes/materials are saved using the existing `WorkshopArt` helpers.
It does not install a camera body-tracking package: VR input comes from
the project's headset/controllers, with the desktop fallback described above.

Editor commands under `Forgotten Village`:

- Install Carpenter quick hands workshop.
- Run active workshop regression tests.
- Validate active workshop and six bells.
- Test Carpenter quick hands workshop.

Reports use `Verification/active-workshop-*`; the live screenshot path is
`Verification/active-workshop-play-preview.png`. Test saves are isolated from the
normal player save. Check the current reports and
[partner handoff](PARTNER_HANDOFF.md) for recorded results.

Physical headset comfort, controller reach, hand-contact feel, haptics and live
Pi mouse recognition still need device acceptance. Scripted pose/keyboard/mouse
checks and simulated camera evidence do not establish physical device success.


Verification (2026-11-11): final runtime/Editor compilation and all 104 village
unit regressions passed. Complete real-time failure/success, memory, simulated
object evidence, unique clue, disk-save and additive re-entry checks passed.
The final open-layout presentation fixture checked readable HUDs, selection rays,
pause and control restoration; final PNGs were visually inspected. Reports are
`Verification/active-workshop-*`, `clock-rhythm-playmode.txt` and
`active-games-presentation.txt`. Physical hardware and the full multi-house ending
runtime run retain the limits described above.
