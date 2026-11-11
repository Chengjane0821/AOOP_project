# Clockmaker: rhythm repair

Repair six clock mechanisms by striking the lit bell as its moving hand reaches
the gold mark. Six lanes form two rows: the top row uses A / S / D and the lower
row uses J / K / L on desktop. VR supports physical controller contact as well as
selectable bells. No camera movement is required.

The run has a four-beat count-in and three increasingly varied 16-note phrases. Notes
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
mark. The plain cream face and dark hand stay readable while the approach dots
indicate approaching notes and the hit window.
Press A/S/D for top-row bells 1/2/3 and J/K/L for lower-row bells 4/5/6. In VR,
reach a tracked controller into a numbered matte-bronze bell or use normal controller
selection. Physical strikes use contact-entry edges, so keeping a controller in
a bell does not repeat strikes. The camera stays still and locomotion is reserved during
the challenge. Escape or the left controller menu opens pause, restart, leave
and input-offset calibration. I opens inventory and also freezes musical time.
During the challenge J is reserved for bell 4; it opens the journal normally
again after leaving the activity.

At 90 BPM the 48-note run lasts about 42 seconds, plus menu time. Its three
phrases explore both rows, alternate between them, and build to a faster finale.
All notes remain single strikes with at least one beat between them, retaining
one-hand play. Perfect is within 90 ms; Good within 200 ms. At least 34 hits and
at most 12 extra strikes
are required. A missed note or off-beat strike breaks the combo. Lit rings,
numbers, hand positions, text feedback and distinct sounds communicate timing.
Positive input offset permits later strikes (for audio/device latency); this
offset is limited to +/-250 ms. It is an advanced pause-menu option.

The six clocks now stand directly in an open playfield on slim independent
supports. Plain cream faces, dark hands, restrained tick marks and one gold timing
mark make the moving hand easy to read. Compact matte-bronze bells briefly depress
when struck, and each bell has its own tone. There is no enclosing cabinet or
continuous shelf across the clocks. A large flat navy HUD above the field and an
independent left-side controls panel follow the existing plane/table-tennis
presentation. Numbered key labels, approach dots and a lower goal strip support
the six lanes. Required hits and the stray limit are derived from the 48-note
chart; judgement windows and input-offset behaviour retain their roles.

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
Human headset comfort, reach to both bell rows, contact feel, audible beat timing
and physical camera acceptance still require a device playtest. Earlier three-bell
reports are historical evidence; consult the latest report before claiming the
expanded six-bell version has passed its current checks.

Verification (2026-11-11): final runtime/Editor compilation and all 104 village
unit regressions passed. Complete real-time failure/success, memory, simulated
object evidence, unique clue, disk-save and additive re-entry checks passed.
The final open-layout presentation fixture checked readable HUDs, selection rays,
pause and control restoration; final PNGs were visually inspected. Reports are
`Verification/active-workshop-*`, `clock-rhythm-playmode.txt` and
`active-games-presentation.txt`. Physical hardware and the full multi-house ending
runtime run retain the limits described above.
