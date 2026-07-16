# Recording Schema v1

## Purpose

This schema is the minimum durable evidence set for FF1600 tire-temperature
model exploration. It is intentionally append-only, iRacing/FF1600 gated, and
small enough to begin controlled data collection quickly.

## Capture behavior

- Continuous telemetry is sampled at 10 Hz.
- Raw carcass temperatures are checked at full `DataUpdate` cadence.
- A change in any raw carcass channel creates a checkpoint record containing
  both the previous and new twelve-value vectors.
- Start, stop, and toggle are SimHub actions.
- Starting while ineligible arms capture. Recording begins only in a live,
  non-spectated, non-replay iRacing FF1600 session with `CarId=raygr22`.
- A session or eligibility transition closes the current run. A later eligible
  transition creates a new run ID.
- `DataUpdate` only creates immutable records and uses a non-blocking bounded
  queue. Serialization and file operations run on the writer thread.

## Storage

Evidence is created with create-new semantics under:

```text
%LOCALAPPDATA%/FF1600TireEstimator/evidence/
  sessions/<simhub-session-guid>/runs/<capture-run-guid>/
    telemetry.jsonl
    checkpoints.jsonl
```

Existing files are never truncated or reopened for append by a later run. The
writer auto-flushes completed lines; a crash may still leave an incomplete
final line, which readers must preserve and ignore rather than rewrite.

## Telemetry record

Each line includes schema version, record type, session/run identity, sequence,
and UTC capture time, followed by:

- car, track, and session context
- lap and track position
- speed, throttle, brake, clutch, gear, and RPM
- surge, sway, and heave acceleration
- yaw, pitch, and roll rate
- air and road temperature
- SimHub's normalized temperature and tire-pressure unit labels
- four brake temperatures
- twelve normalized tire temperatures
- four pressures and four wear values
- normalized pit and pit-lane state
- raw iRacing tick, session time, pit-road/garage/replay state
- twelve raw carcass temperatures

Raw iRacing carcass values are stored as Celsius in
`raw_carcass_temperatures_c`. Normalized values retain SimHub's unit labels in
each telemetry record.

Array order is `FL, FR, RL, RR`; three-zone tire arrays use
`FL inner/middle/outer, FR inner/middle/outer, RL inner/middle/outer,
RR inner/middle/outer`.

## Checkpoint record

Each checkpoint contains the common envelope, normalized pit context, raw
iRacing context, the previous twelve-value vector, and the new vector. Tow,
normal pit return, and other transition classifications remain analysis-time
labels until more evidence supports a durable automatic rule.

Checkpoint records also include:

- `detection_reason`: `temperature_change` for an in-run update or
  `telemetry_resume` when changed values first appear after a temporary
  same-session interruption such as tow/loading.
- `previous_capture_run_id`: the preceding run ID for a
  `telemetry_resume` checkpoint; otherwise `null`.

The recorder retains only the final twelve-value vector and run ID in memory
across a temporary eligibility loss. It never reopens, combines, or modifies
the preceding run's evidence files.

## Failure behavior

The bounded queue holds 4,096 messages. If it fills, the recorder stops capture,
counts the rejected record, and reports a fault in
`FF1600Tires.DiscoverySummary`; it never blocks `DataUpdate` or evicts accepted
evidence.
