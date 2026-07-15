# Experiment 004: Checkpoint Repeatability

## Status

Planned

## Question

Does the observed atomic twelve-channel carcass update repeat across ordinary
pit returns, tow/reset workflows, and fresh sessions, and which transitions
must a production detector treat as checkpoints versus resets or duplicates?

## Tested build

To be recorded after the experiment is executed. Use the corrected full-cadence
probe introduced by commits `49c386c` and `3897747`.

## Probe property

```text
FF1600Tires.DiscoverySummary
```

No additional properties and no telemetry files are required. Capture the
single summary before and after every labeled transition.

## Preconditions

- Build and deploy with SimHub stopped.
- Use a live iRacing FF1600 session (`CarId=raygr22`), not a replay.
- Record the SimHub version, iRacing version, track, and session type.
- Keep each screenshot's original bytes outside Git and record its SHA-256.
- Restart SimHub before the matrix so counters begin from a known process run.

## Test matrix

Execute each available case independently. Do not combine two transitions
before capturing the result.

| Case | Starting state | Action | Required captures |
| --- | --- | --- | --- |
| A | Pits, fresh session | Drive and return normally | Before exit, on track, immediately after temperature change |
| B | On track with heat | Tow/reset to pits | Immediately before tow, immediately after loading/reset, after values settle |
| C | Pits after a checkpoint | Remain stationary | Start and at least 30 seconds later |
| D | Any completed case | Exit and join a fresh FF1600 session | Before exit, menu/waiting state, new-session initial state |

If a workflow is unavailable or unsafe during the session, mark it not
executed rather than substituting an unlabeled action.

## Record for every observed temperature change

- Case label and wall-clock order
- `SessionId`, raw tick, and raw session time
- Previous and new twelve-value vectors
- Changed-channel count and `AllChangedSameSample`
- Last pit-entry tick/time and its offset from the temperature change
- `OnPitRoad`, normalized pit/pit-lane state, spectating, and replay context
- Whether the change is judged a normal checkpoint, reset candidate,
  duplicate, or unexplained observation

## Provisional interpretation rules

These labels organize observations; they are not production policy:

- **Checkpoint candidate:** a coherent temperature-vector change after driving
  that preserves the same active FF1600 session context.
- **Reset candidate:** a change coincident with tow/loading, session identity
  replacement, telemetry disappearance/reappearance, or a new initial state.
- **Duplicate candidate:** the exact same twelve-value vector is reported again
  without an intervening distinct vector.
- **Unexplained:** evidence is insufficient or the transition contradicts the
  above labels. Preserve it and do not force a classification.

## Acceptance criteria

The experiment is complete when every available matrix case has its artifacts
and observations recorded, including cases where no temperature change occurs.
The resulting evidence must be sufficient to propose explicit detector rules
without relying on `OnPitRoad` as the trigger.

## Results

Not yet executed.

## Remaining uncertainty

Pending experiment execution.
