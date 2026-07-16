# Experiment 004: Checkpoint Repeatability

## Status

In progress

## Question

Does the observed atomic twelve-channel carcass update repeat across ordinary
pit returns, tow/reset workflows, and fresh sessions, and which transitions
must a production detector treat as checkpoints versus resets or duplicates?

## Tested build

- Tow/reset observation: corrected full-cadence probe from commits `49c386c`
  and `3897747`
- Session-boundary verification: commit `159bc2b`
- Configuration: Debug, .NET Framework 4.8, AnyCPU

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

### Case coverage

- Case A was not repeated. Experiment 003 already recorded a normal driven pit
  return with an atomic twelve-channel update one tick before `OnPitRoad`.
- Case B was executed at Ledenon in an iRacing Test Drive. The driver started
  on cold tires, drove briefly, spun, and used teleport/tow to return to the
  pits.
- Case C was not executed as a separate 30-second stationary capture.
- Case D verified experiment-state reset across two distinct SimHub session
  GUIDs after the probe defect described below was corrected.

### Case B: tow/reset

The three captures, in timestamp order, show the car initially in the pits, on
track with the initial carcass values still reported, and back in the pits
after teleport/tow. This is a tow/reset observation, not a normal pit entry.

The temperature vector changed at raw tick 7,845 and session time 154.2. The
raw `OnPitRoad` rising edge followed at tick 7,846 and session time 154.21667.
All twelve channels changed in the same sample:

| Channel | Before | After |
| --- | ---: | ---: |
| LF CL | 52.32056 | 51.84534 |
| LF CM | 52.32056 | 52.83130 |
| LF CR | 52.32056 | 53.01926 |
| RF CL | 52.32056 | 53.50635 |
| RF CM | 52.32056 | 53.39621 |
| RF CR | 52.32056 | 53.25848 |
| LR CL | 52.33554 | 58.39117 |
| LR CM | 52.33554 | 60.00424 |
| LR CR | 52.33554 | 60.39072 |
| RR CL | 52.33554 | 55.65167 |
| RR CM | 52.33554 | 55.62424 |
| RR CR | 52.33554 | 55.08347 |

The asymmetric post-stint values were not a return to the initial cold vector.
This supports preserving a tow/reset update as evidence, while labeling its
transition context so later analysis can distinguish it from a driven pit
return.

### Session-boundary defect and Case D verification

The Case B captures revealed that the discovery probe retained counters and
the last event when the SimHub session GUID changed. Commit `159bc2b` resets
transition state and timing statistics when a new active FF1600 `SessionId` is
observed.

Two separate sessions then reported:

| Session GUID prefix | Samples | Pit entries | Changes | Retained temperature event |
| --- | ---: | ---: | ---: | --- |
| `2dd8cbfc` | 558 | 0 | 0 | None |
| `b2367ba7` | 443 | 1 | 0 | None |

The second session's pit entry occurred at tick 381 during its own loading
sequence. Neither session inherited counters or a retained temperature event
from the earlier session.

## Artifacts

The user supplied these screenshots. They remain external to the repository:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `SimHubWPF_2026-07-15_23-44-57.png` | 112,441 | `4BA68D9F6EA9FD7E37AC6B694BC84E6EA0F32A0BDD677E595A13D807417FEEC0` |
| `SimHubWPF_2026-07-15_23-46-08.png` | 108,478 | `79CC271362C56FD7D4374EFDFD26CF63BC6A4EA7D4A18F16F4B11DA9CBCC5CB6` |
| `SimHubWPF_2026-07-15_23-46-20.png` | 112,505 | `9A63B08D9EEBDDCB145B65321F54CB77D312F0314B79F377F717491B742EB408` |
| `codex-clipboard-e8b898f7-7ff1-40a3-a6e2-43c2998eb4fb.png` | 133,802 | `AC262B9D07D24634FE6315BBEF546DE971E9755855652365A5064E880BB0DAF2` |
| `codex-clipboard-438e7bd8-7adc-4545-a690-90c5cf00f897.png` | 161,869 | `77EB0D0E6814DADACB5516D6DD6F8738177AAF499BAF8D9E0018674B7894953F` |

## Remaining uncertainty

- A separate stationary capture has not yet tested the proposed duplicate
  classification over the planned 30-second interval.
- Tow/reset behavior has been observed once. More captures are needed before
  assuming it always preserves a meaningful end-of-stint checkpoint.
- SimHub and iRacing version numbers were not recorded.
