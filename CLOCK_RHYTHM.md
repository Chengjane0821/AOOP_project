# Clockmaker: rhythm repair

Repair three clock mechanisms by striking the lit
bell as its moving hand reaches the gold mark. Three lanes use A / S / D on
desktop and selectable bells in VR. No camera movement is required.

The run has a four-beat count-in and three increasingly varied phrases. Notes
approach visibly before their beat; shape, lane numbers and sound supplement
colour. A successful run needs 70% of notes hit. Misses break the combo, and a
failed run can be retried immediately. Pausing freezes musical time.

Success restores the clocks and unlocks the Clockmaker's short memory. The
existing real-world object verification and fifth clue remain required; the
technical timing report stays evidence, rather than a calculation exercise.
Already-earned fifth clues survive save migration. Unfinished timing work does
not count as a rhythm victory. Replay cannot duplicate rewards.

Play from the village's normal starting scene, finish the Elder introduction,
enter the Clockmaker's house, and speak to him or use the PLAY control. Choose
"Repair the clocks". Each numbered clock has a hand and a gold twelve-o'clock
mark. The ivory face stays readable while the outer ring and countdown lights
indicate approaching notes and the hit window.
Press A/S/D for bells 1/2/3; in VR use the normal controller selection on the
numbered brass bells. The camera stays still and locomotion is reserved during
the challenge. Escape or the left controller menu opens pause, restart, leave
and input-offset calibration. J/I panels also freeze musical time.

At 90 BPM the 32-note run lasts about 44 seconds, plus menu time. Perfect is
within 90 ms; Good within 200 ms. At least 23 hits and at most 8 extra strikes
are required. A missed note or off-beat strike breaks the combo. Lit rings,
numbers, hand positions, text feedback and distinct sounds communicate timing.
Positive input offset permits later strikes (for audio/device latency); this
offset is limited to +/-250 ms. It is an advanced pause-menu option.

The wood-and-brass clock cabinet now includes detailed dial marks, separate
repair/combo/timing cards and a progress strip. Brass bells briefly depress when
struck. Numbered key labels and the cabinet's pause legend replace the long
floating control hint; the scoring and timing rules are unchanged.

After success, choose "Remember together", verify the requested clock with the
existing camera workflow, and collect the fifth clue. No camera or hardware
simulation is represented as physical verification. Previously earned clues
migrate from schema 4; unfinished calculation work requires the new game.

Editor commands under `Forgotten Village`:

- `Install Clockmaker rhythm repair`: rebuild only this room and its prefab.
- `Check Clockmaker rhythm repair`: deterministic scoring and save checks.
- `Test Clockmaker rhythm repair`: complete real-time failure/success playthrough,
  injected keyboard input, pause, retry, memory, simulated camera, disk save and
  additive exit/re-entry checks. Test saves use a separate Verification path.
- `Run clock regression unit tests`: run all existing village, tennis and plane
  Edit Mode tests and write the clock regression XML report.
- `Validate clock house integration`: inspect all six saved houses for missing
  scripts and duplicate active cameras/listeners, plus Clockmaker activity wiring.

Implementation lives in `Scripts/ClockRhythm`. The generated scene contains no
new camera, listener, XR rig, downloaded art or external music dependency.
Human headset comfort, controller reach, audible beat feel and physical camera
acceptance still require a device playtest.
