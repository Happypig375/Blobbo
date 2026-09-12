# Blobbo interaction playtest

Run `Launch Playground Test.cmd`. This is an experimental feedback harness, not the production game.
Use Menu to switch rooms; you do not need to test everything in one room or follow a fixed order.
The participant window uses a fixed 1280×720 desktop canvas; maximize/fullscreen is not part of this
playtest because resizing can leave stale or duplicated rendering while that path is under review.
On a cold first launch, a black window may briefly appear while local shaders compile; wait for the
playground to finish loading rather than treating that transient frame as a loading-screen guarantee.

## Choose an experiment

- Combined: the water-capable Legacy Blobbo, balloons, heat/cooling, a fan, and one purple rewindable
  prop. Try interactions between them using the same Grab, Pull, and Swipe controls.
- Toy / Target: focused body and control comparisons. Legacy, Ring, and Hull are available here;
  the Combined scenario keeps Legacy because the other bodies do not implement water behavior.
- Menu also offers the original Scenes 01–05 as separate baselines, including math collisions and
  velocity-driven eyes/trails. All rooms are included in this package.
- `Launch M1 Test.cmd` starts the controlled M1 protocol in Toy with Ring. Use its separate facilitator
  guide and CSV; exploratory feedback does not substitute for that protocol.

## Try the gestures

Choose Grab, Pull, or Swipe at the top. Start on Blobbo's visible body, not an empty part of the room.

- Grab: drag Blobbo, then flick and release. Watch the grip/tether and deformation while holding.
- Pull: drag away from the intended destination, then release. The dots preview the launch direction.
- Swipe: make a quick stroke starting on Blobbo and release in the orange arrow's direction.

For each, notice whether the body looks touchable before contact, follows your intent during movement,
and gives a readable result after release and collision. Try near an edge, near another object, and after
absorbing water. Report surprises even if you are unsure whether they are bugs.

`Repeat input` resets the room and repeats your last completed gesture using the currently selected body
and control. It anchors the first contact at the fresh body's center and keeps the stroke's relative
movement and timing. This is input replay for comparison, not a replay of the original trajectory: prior
settling, motion, deformation, absorbed water, and other room state are not restored. If the translated
stroke leaves the reset playfield, the screen reports that the repeat was cancelled. Record a shorter
stroke to try again. Changing rooms clears the stored gesture.

## Combined-room controls

The same actions are available as labelled buttons.

| Key | Action |
| --- | --- |
| W (or Grave) | Pour a bounded water burst above Blobbo. |
| H | Toggle the hot plate. Orange coils mean ready, yellow means contact, and gray means off. Bring water or a filled body above the plate; the blue zone cools smoke. |
| B | Revive the balloons after popping. |
| Down | Start a preview of the purple prop's recent history. |
| Left / Right | Scrub earlier / later while previewing. |
| Enter / Esc | Commit / cancel the prop preview. |
| Space | Pause or resume; an active gesture is cancelled. |
| R | Reset the fixture, including particles and histories. |
| Esc | Return to Menu when not previewing rewind. |

Rewind pauses the room while you inspect the purple prop's history. Committing rewinds only that prop,
not Blobbo, absorbed water, balloons, or the rest of the room. Cancelling leaves the prop's present
state intact. Enter/Esc restore the previous pause/run state. Header actions also cancel a preview:
Resume starts the room, Menu leaves it, and Reset or room/body/control changes rebuild the fixture.
Water/heat/revive actions do not run while paused. Old gestures cannot leak into the next test.

## Original rooms

The original rooms keep their own baseline controls:

| Room | What to try |
| --- | --- |
| 01 — Blobbo / balloons | Drag Blobbo and release; hold Grave for water, Space to pause, Enter to revive Blobbo, R to reset the entire fixture. |
| 02 — Box rewind | Pause/preview, scrub Left/Right, then commit or cancel using the labelled controls. The cyan target is the historical ghost; compare it with the resumed box. |
| 03 — Math | Observation room: the moving wall and balls push expressions together; watch them combine and simplify. |
| 04 — Eyes / trails | Observation room: watch pupils track velocity and trails follow the bouncing squares. No drag controls. |
| 05 — Thermal / fan | Drag Blobbo; H toggles heat, W adds water, Space pauses, Enter revives Blobbo. Watch heating, cooling, and fan motion. |

Use each room's Quit/Menu button to choose another experiment.

## Leave feedback

Use `Combined Feedback.csv`: one row per observation, identifying the room, body/control, action, what
you expected, and what happened. Include which other mechanic helped or interfered. A short recording or
before/during/after screenshots are useful, but optional. Do not include personal information in captures.
Keep exploratory observations separate from M1 preference/target-attempt scores.