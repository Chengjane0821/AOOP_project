# Alchemist's endless sky

The official Alchemist challenge replaces the RC filter station. Accept her invitation, survive **60 active seconds**, finish the recovery conversation, then inspect the record to collect existing Clue #2. Declining does not advance progress. The Abandoned House still needs all five clues.

## Controls and comfort

The existing XR player moves to a fixed viewing station. Aircraft movement never drives the camera. Stand or sit in a clear area, then Begin flight calibrates the headset's tracked position. Small lateral movement steers; a controlled upward motion raises the aircraft; crouching lowers it. Large jumps are unnecessary. Right A also raises the aircraft. Seated controls use the left stick for horizontal/vertical movement and A/B for up/down. Use the existing menu to pause, recalibrate, retry or return.

Desktop: A/D steer and hold lateral position; Space rises; Left Ctrl lowers; Esc pauses; R recalibrates (or retries after a crash). Desktop input is excluded while XR is active.

Tracking loss or an XR Origin discontinuity pauses flight. Recalibrate before resuming. Head rotation alone is not steering. Lift requires height, upward velocity and dwell; hysteresis prevents repeated head-bob activation.

## Runtime and tuning

`PlaneDreamTuning.asset` exposes body thresholds, smoothing, aircraft bounds, acceleration and obstacle approach settings. Default lift is 0.11 m, crouch 0.18 m and full lateral input 0.25 m. These require physical headset acceptance testing and individual accessibility tuning.

`PlaneDreamRun` owns countdown, flight, pause, failure and success. Only active fixed-step flight advances the exact 60-second clock. Retry resets calibration, plane, scenery, obstacles and elapsed time. Collision is resolved before time advancement; a crash cannot also win the same step.

PlaneRunner aircraft, rocks and valley models are reused with URP material copies. Scenery scrolls toward the stationary viewing station to represent forward travel. Vendor scene cameras, global managers and keyboard scripts are not instantiated. No second XR Origin is created.

Six randomized barrier patterns each expose an achievable safe target. Minimum approach and spacing account for maximum world speed and aircraft travel/acceleration. Obstacle groups are pooled. Relative swept-box tests supplement trigger colliders, preventing fast obstacles from tunnelling. Colored outlines show the actual collision bounds. Visual aircraft banking does not rotate its collision box.

## Progression and persistence

Dream victory saves the solved challenge but does not grant the clue. Recovery dialogue unlocks the record; inspection awards the original clue identity `Memory Signal Filtering Record` with a new dream/experiment narrative. Save version 4 preserves previously earned clues; unfinished legacy RC progress must complete the new challenge. Three other houses retain their technical-object requirements. Arduino communication remains shared and unchanged.

## Verification

Use Forgotten Village > Test Alchemist plane dream for actual editor Play Mode integration. The successful-run test uses an editor-only route-following input and the full duration; it does not verify physical body input. Edit Mode tests cover body mapping, timing, safe patterns, collision and clue migration. See tracked `Verification/plane-dream-*` results and the development report for executed outcomes.

Physical Meta Quest acceptance still requires checking tracked-space calibration, ray UI, seated/body modes, pause on tracking loss, readable text, rendering performance and comfortable reachable movements. Test in an unobstructed play area with a stationary observer if needed.
