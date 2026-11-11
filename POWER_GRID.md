# Technician: Power-grid emergency

Restore the workshop's lights, conveyor and archive machines in three short
rounds. Select generator A or B, then connect a numbered machine socket. Cables
appear immediately. Each generator has a visible capacity and current load.
Overloading heats its breaker; sustained overload trips it. Disconnect or move
loads before resetting a breaker. The final round needs the backup generator.

All required machines must remain powered safely for four seconds to finish a
round. Each round has a 75-second emergency timer; pause freezes both heat and
time. Failed rounds can be retried. Completing all three restores the
Technician's memory and unlocks his existing cell-phone evidence quest.

The prior safety-controller diagnostic is retained as automatic evidence in
the recovery sequence, so the final archive still requires the deployed RUN
and corrected STOP records. No physical gate command is sent by this activity.
Already-earned fourth clues survive migration; unfinished diagnostic work does
not count as a grid victory. Other house gameplay remains unchanged.

Desktop: A/S select a generator; 1-5 connect a machine (repeat to disconnect);
Space toggles main power; R resets the selected breaker; B toggles backup.
Mouse clicks and normal VR ray/near selection operate the same physical panel.

Start from `Assets/Scenes/rpgpp_lt_scene_1.0.unity`, finish the Elder introduction,
enter the Technician's house, and speak to him or use PLAY. Choose "Restore
power". A/S selects the cyan A or amber B generator, then 1-5 attaches that
machine. Selecting the same generator/socket again removes its cable. A machine
can have one source at a time. All loads are labelled in kW; generator labels
show load/capacity. Separate LOAD and TEMP gauges distinguish assigned power
from breaker heat, which warns before the three-second breaker trip.

The metal equipment cabinet has a bright outline around the selected source,
numbered sockets, reserved covers for later rounds and distinct machine models.
Moving lights along powered cables show current flow. Controls carry their key
labels, with a compact operation legend along the panel's lower edge. Short
action feedback replaces the header's circuit/time/stability status for two
seconds, then the status returns. These presentation changes retain the rules.

Round 1 restores three machines using 6/6 kW generators. Round 2 adds a fourth
machine and starts with breaker A tripped. Round 3 adds a fifth machine and
requires B's +2 kW backup. Four continuous safe seconds complete each round.
The 75-second timer runs only during active play. Escape/controller menu offers
resume, retry current circuit, restart all, or leave. J/I also pauses time.
The existing simulated/USB button toggles main power during the activity.

After three rounds, choose "Recover diagnostic log", complete the existing
cell-phone camera quest, and collect Clue #4. Recovery calculates and saves the
deployed RUN and reference-fixed STOP using the existing controller model.
No physical latch is commanded until the normal final-house evidence checks.
Schema 6 preserves already-earned valid technician clues from earlier versions.

Editor commands in the Forgotten Village menu:

- Install Technician power-grid emergency: rebuild only this scene/prefab.
- Check power-grid rules: run the focused rule/save unit tests.
- Run power-grid regression unit tests: run all village unit regressions.
- Validate power-grid integration: inspect all six houses and grid references.
- Test Technician power-grid emergency: real scene/input/overload/timeout/reward
  playthrough, with isolated saves in `Verification/power-grid-test-save.json`.

Reports use `Verification/power-grid-*`. Hardware simulation is explicitly marked.
Physical headset comfort, reach, USB wiring and the live Pi camera remain manual
acceptance checks. Sources are in `Scripts/PowerGrid`; no additional camera,
listener, XR rig, third-party art or downloaded audio is needed.

Original implementation verification on 2026-11-11: runtime/Editor compilation passed; 48 unit tests passed;
all six scene checks and the complete Technician Play Mode suite passed. The
runtime suite includes a full 75-second failure, retry, three successful rounds,
keyboard/mouse input, shared XR callbacks, ray obstruction checks, simulated
camera/USB, clue/save/re-entry checks. Final panel and overload PNGs were inspected.
The complete village ending suite was updated but not rerun. Device testing
remains as described above. Work is recorded in 33 nonempty local grid commits.
