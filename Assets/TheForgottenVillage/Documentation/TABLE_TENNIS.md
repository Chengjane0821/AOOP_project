# Elder's House: table tennis

This is the Elder's only main challenge. There is no voltage-divider side quest. The original resident, rustic room, entrance/exit and clue index 0 are retained. The table/bats/ball use `Assets/LowPolyTableTennis`; existing Course Library furniture is relocated without scaling. The measured room is already 11.1 × 7.5 m.

## Play

Speak to the village Elder to begin the investigation, then enter Elder's House. The entry conversation offers **Let's play!**, **Maybe later**, and an active-hand switch. The NPC can be approached again later. Starting places the player at a marked standing position; movement, snap turning and teleportation are suspended during the activity. Pause offers Resume, Restart match, and Leave table. Leaving restores ordinary RPG movement.

Quest: swing the selected controller. Grip alone does nothing. Hold Grip during a correctly timed contact for a smash. Tracking loss or an open menu pauses the ball. The paddle uses XR input poses in the existing XRI camera-offset space; it never moves the XR rig with the paddle.

Desktop: mouse motion controls paddle X/height, F advances it for a stroke, and Space holds Grip. Release F before the next forward stroke. Escape opens pause/retry controls. These controls are an optional physical-paddle simulation, not a trigger-to-hit substitute for VR.

## Rules and counters

- First to five rally points wins a game, with no deuce. First to two games wins the match.
- The Elder serves to the player's half. Return after one table bounce. A wrong-side bounce, net shot, second bounce, or missed return ends the rally. A shot must land on the receiver's half to score for the hitter. Out-of-bounds and timeout recovery use the same fault resolver.
- `rallyHits` counts accepted paddle contacts from both sides and resets on each serve. `gameHits` counts those same contacts across rallies and resets each game. The random inclusive 20–30 target applies to **gameHits**, so a player can build toward it across short rallies.
- Successful smash count persists across rallies within a game. A valid smash requires Grip, player-side reach, configured ball height and descent window, swing/forward speed and face alignment. Invalid moving Grip contacts produce MISS and an Elder point. Pressing Grip without contact never counts.
- At the hit target or second successful smash, the Elder moves his paddle aside. Victory is resolved only after the decisive shot lands and the Elder misses; a pending condition never awards both sides or bypasses the physical outcome.
- A lost best-of-three clears both game scores, points and counters before retry. Starting a retry generates a fresh target. An earned clue is never removed by replaying.

## Physics and tuning

`Assets/TheForgottenVillage/TableTennisTuning.asset` exposes speed limits, normal aim assistance, contact cooldown, smash conditions, AI movement/reaction and shot error. Paddle components expose active XR hand, grip/rotation offsets and tracking speed. Tune these on the actual controllers before assessment.

The table is 2.74 × 1.525 m with a surface near 0.76 m; net height is 0.1525 m. The ball is 40 mm / 2.7 g. Paddle face collision is 0.17 × 0.19 m. The existing 0.02 s fixed timestep is unchanged. A continuous dynamic ball, kinematic paddles, relative swept contact tests and capped impulses prevent fast swings from relying solely on discrete trigger callbacks. Ownership, cooldown and phase checks suppress duplicate scores. Forehand/backhand use both face normals; angular wrist velocity contributes to contact speed. Normal hits retain configurable assistance; timed smashes receive a directed ballistic attacking impulse. The NPC predicts the table bounce, has bounded movement and a reaction delay, and may miss difficult shots.

## Progression

Match victory persists `elderTennisWon` and `puzzleSolved[0]`. Acknowledging the recovered-memory dialogue persists `elderMemoryRecovered` and enables the original scratched-photo evidence. Inspecting that evidence sets `photograph`, awards clue index 0 and updates inventory/journal/tracker. The other four houses still require their original puzzles and camera objects; all five clues are required for the Abandoned House. Physical chest authorization requires the Elder evidence and four live camera records. Existing earned v2 Elder clues migrate; an unfinished divider does not become a tennis victory.

## Validation and remaining acceptance

Use **Forgotten Village → Test Elder tennis and RPG progression** for real Editor Play Mode scene/physics checks. The test saves to `Verification/elder-tennis-test-save.json`, not the player's normal save. Edit Mode cases cover rules, trajectories, persistence and authorization. The generated XML/text reports document executed results. Contact fixtures position physical paddles/balls and set simulated Grip; they are not evidence of physical Quest input testing.

Still required on Meta Quest: active left/right hand calibration; forehand/backhand contact alignment; swing latency at the headset refresh rate; tracking loss/recovery; Grip thresholds; haptics/audio; seated/standing reach; ray UI operation; comfort and room-scale clearance. Native Android/Quest build performance, actual USB Arduino actuation, and real camera verification were not tested. Desktop human playtesting is still needed to judge difficulty. The game intentionally uses simplified rules and assistance, not an official competitive table-tennis simulation.
