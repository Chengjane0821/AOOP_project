# Carpenter: Bridge Delivery

> **Retired / historical implementation.** The current Carpenter activity is
> [Quick Hands Workshop](ACTIVE_WORKSHOP.md), a body-action arcade. The bridge
> description and test results below document the previous `ebf8091` delivery;
> they are not acceptance results for the replacement. Current normal village
> rebuilding installs Quick Hands. Preserve these notes when reviewing history.

Build a miniature timber bridge, then send a real physics cart over it. Pick up
pieces, rotate them and release near the bridge guides to fasten them in place.
Try a design immediately; a failed crossing keeps the layout so it can be fixed.

Three deliveries introduce a basic bridge, a wider span with misleading short
timber, and a heavy cart that needs reinforcement under every deck section.
Unfastened or badly oriented pieces cannot form the bridge. Short or weak timber
breaks when loaded: both the timber and cart fall using Rigidbody physics. Success
requires the cart to arrive at the far bank at bridge height. Merely placing the
pieces never awards a win.

Desktop: left mouse grabs/drags/releases timber; Q/E rotates while held; Space
tests the bridge; R recovers the selected piece or clears the build. The physical
TEST, RETRY and RESET controls use the same actions. VR uses the existing XR grab
and ray/near select controls. Only the desktop view receives a downward workshop
view; tracked headset pitch is never forced. Desktop reinforcements can be
dropped onto their visible deck board and fasten underneath, making the hidden
support location easier to reach. Fastening provides sound and controller
feedback. Escape/controller menu pauses the cart and opens resume/retry/leave
controls; desktop panels face the current view so they remain readable.

Finish all three deliveries, hear the Carpenter's short recovered memory, then
complete the existing real-world mouse camera request to receive Clue #3, the
Analog Front-End Record. Save schema 7 retains already-earned valid clues; an
unfinished amplifier calculation does not count as a bridge delivery victory.

Editor menu commands under Forgotten Village:

- Install Carpenter bridge delivery: rebuild only this house scene and prefab.
- Run bridge delivery unit tests: run all village rule/save regressions.
- Validate bridge delivery integration: check saved scene and grab/cart bindings.
- Test Carpenter bridge delivery: real-scene input, failed/successful cart runs,
  reinforcement, pause, reward and save/re-entry checks with isolated test saves.

Sources are under `Scripts/BridgeDelivery`; reports use `Verification/bridge-delivery-*`.
Physical headset reach, grab feel and live Pi object recognition require device
acceptance. Other house gameplay and the final evidence/authorization flow remain.

Verification (2026-11-11): runtime/Editor compile, village unit regressions and the
complete Carpenter playthrough passed. The live suite uses actual mouse dragging,
Q/E rotation, XR grab callbacks, gravity-driven cart failures and three successful
crossings, short/weak timber, visible-board reinforcement placement, pause physics,
clue verification, disk save and additive re-entry. It never alters physics time
or sets success flags. See `bridge-delivery-playmode.txt` and the live preview PNG.
The full multi-house ending runtime suite was updated but not rerun here.
