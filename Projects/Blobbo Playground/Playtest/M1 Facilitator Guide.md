# Blobbo M1 facilitator guide

This protocol collects the human evidence required by `Projects/Blobbo and Chrono/PLAN.md`. It tests
whether unfamiliar players can discover, repeat, predict, and prefer one of the three M1 control modes.

## Prepare the session

1. Use five or more people who have not played or watched this Blobbo experiment before.
2. Give each person an anonymous ID such as `P01`; do not record names or contact details.
3. Extract the entire win-x64 playtest ZIP, then run `Launch M1 Test.cmd`. The build opens directly in
   the M1 lab and does not require a separately installed .NET runtime.
4. Keep this guide and the result sheets out of the participant's view during discovery.
5. Start every comparison with the simplified Ring body. Use the prefilled control order in
   `M1 Participants.csv` to reduce order and learning bias.

The screen intentionally provides the only initial visual prompt: the selected control is bracketed,
the action label appears above Blobbo, and the trajectory, target, outcome, and bounded-acceleration telemetry
respond to play. Do not explain the mechanic before the discovery observation.

## Run one participant

For each control in the participant's assigned order:

1. Select `Ring`, the assigned control, and `Toy`, then press `Reset`.
2. Say only: **"Try to make Blobbo move."** Allow 20 seconds. Record discovery after the single
   on-screen prompt. If they do not discover it, record `no` before offering help.
3. After the first completed gesture, remain silent for another 20 seconds. Record `yes` only if they
   voluntarily make another completed gesture.
4. Select `Target` and reset. Let them make three practice attempts, recording `hit`, `miss`, or
   `in_flight` from the screen after each attempt.
5. Before attempt four, ask them to predict the broad direction and whether this attempt will be
   weaker, similar, or stronger than attempt three. Record the prediction before release, then record
   the observed categories and target outcome.
6. Ask for clarity, fun, and physical fatigue ratings from 1 (low) to 5 (high). Do not discuss the
   other modes until all three are complete.

After all controls, ask which one they would keep and why. Optionally let them compare `Legacy` and
`Hull` in the Toy room; body preference is exploratory and does not replace the control evidence.

## Score the gate

With five participants, "most" means at least three.

- **Discovery:** at least 3 / 5 discover at least two of the three modes without an explanation.
- **Voluntary repetition:** at least 3 / 5 voluntarily repeat a discovered mode in the Toy room.
- **Prediction:** at least 3 / 5 correctly predict both broad direction and relative strength for at
  least two modes after the three practice attempts.
- **Control advantage:** one mode is ranked first by at least 3 / 5 and is not worse on prediction and
  target success, or it leads the measured success/prediction composite by at least 20 percentage
  points without a recurring fatigue or accessibility concern.

Copy the completed CSV files out of the extracted build, summarize counts and recurring observations in
the M1 evidence log, and keep the milestone pending if any criterion lacks evidence. Do not begin M2 or
promote a control merely because the automated checks pass.