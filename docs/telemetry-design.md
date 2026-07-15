# Telemetry Discovery Plan and Provisional Persistence Design

## Status and scope

This is a discovery document, not an approved implementation specification.
It records confirmed SDK facts, provisional designs, and decisions that remain
blocked on live experiments. It does not authorize or implement file I/O,
queues, background workers, pit detection, or estimation code.

Interpret statements using these labels:

- **Confirmed:** observed from the installed official template or reflected
  public assemblies.
- **Provisional:** a current design preference that may change after testing.
- **Blocked:** no implementation decision until the named experiment produces
  evidence.

The first implementation must preserve accepted experimental evidence, keep
all disk work off `DataUpdate`, and make missing data explicit rather than
silently presenting an incomplete capture as complete.

## Discovery sequence

Experiments are intentionally small and independently reviewable:

1. **Normalized identity probe:** observe game, car, session, replay,
   spectating, pit state, and `DataUpdate` cadence in the FF1600 and another
   car.
2. **Raw compatibility probe:** establish the raw object's concrete runtime
   type and safely observe selected iRacing session/tire members across menu,
   track, pit, replay, and shutdown transitions.
3. **Temperature update experiment (completed once):** raw carcass values
   changed atomically across all twelve channels one tick before the observed
   `OnPitRoad` rising edge during a return to the pits.
4. **Checkpoint repeatability experiment:** repeat normal pit return, tow/reset,
   and fresh-session transitions to distinguish durable checkpoint behavior
   from resets and other discontinuities.
5. **Performance experiment:** measure update cadence, candidate snapshot size,
   serialization cost, and expected data rate without introducing production
   persistence.
6. **Persistence spike:** only after the earlier results, test a limited JSONL
   writer for shutdown, crash-tail, restart, and disk-error behavior.

Each experiment gets a record under `docs/experiments/` containing the tested
commit, environment, procedure, exact observations, conclusion, and remaining
uncertainty. Generated captures stay outside Git unless a separate review
approves a small curated sample with provenance.

## Confirmed SDK surface

Inspection of the installed SimHub assemblies confirms these public inputs:

- `GameData`: UTC/frame timestamps, game state, SimHub session GUID, lap GUID,
  session start, replay/spectating state, and old/new normalized frames.
- `StatusDataBase`: car/track identity, lap and sector state, pit state,
  speed, throttle, brake, clutch, steering/motion-related values, ambient and
  road temperature, track position, and normalized four-corner tire
  temperatures/pressures/wear.
- `StatusDataBase.GetRawDataObject()`: a public raw-data escape hatch.
- Installed `iRacingSDK`: iRacing `SessionID`, `SubSessionID`,
  `SessionUniqueID`, driver/car IDs, session time/tick count, pit/garage state,
  and left/middle/right carcass values for all four tires.

The raw compatibility probe confirmed that iRacing's payload is exposed as an
`iRacingSDK.Telemetry` object through the raw wrapper's `Telemetry` property.
The twelve `LFtemp*`, `RFtemp*`, `LRtemp*`, and `RRtemp*` members were present
and updated in a live FF1600 session. The payload becomes unavailable outside
an active session. Raw access remains a version-sensitive dependency: a
production adapter must validate the runtime types/members, fail closed when
they are unavailable, and avoid per-frame reflection and allocation.

## Provisional recording eligibility

The plugin may remain loaded for every game and car, but the proposed recorder is
fail-closed. It may enter `Recording` only when all of these are true:

1. SimHub reports an active iRacing session.
2. The player is not in replay or spectating mode.
3. The normalized car ID equals the observed FF1600 ID `raygr22`. The observed
   display model is `Ray Formula 1600`; `CarId` is the eligibility key and the
   model remains recorded context.
4. The user has requested capture.

Unknown, empty, or unexpected car identity must be ineligible. It must never be
treated as FF1600 by default. The comparison observation `mx5_mx52016` (Mazda
MX-5 Cup) confirmed that the normalized car ID changes with the selected car.

If game, session, or car identity changes during recording, the recorder stops
accepting snapshots, finishes the current run through the bounded shutdown
path, and records the reason. It starts a new run only after eligibility is
re-established. Telemetry from two cars or sessions must never share a capture
run or telemetry segment.

## Provisional recorder controls and state

The official template demonstrates SimHub actions and input mappings. The
logging implementation should expose:

```text
FF1600Tires.StartTelemetryCapture
FF1600Tires.StopTelemetryCapture
FF1600Tires.ToggleTelemetryCapture
```

These actions can be mapped through SimHub to a physical control or compatible
dashboard control. A later minimal settings-page button may invoke the same
state transitions; it must not contain separate recording logic.

Expose central status properties for operator feedback:

```text
FF1600Tires.TelemetryEligible
FF1600Tires.TelemetryRecording
FF1600Tires.TelemetryStatus
FF1600Tires.TelemetryFaulted
```

The proposed recorder state machine is:

```text
Stopped -> Armed -> Recording -> Flushing -> Stopped/Armed
                        |
                        +-------> Faulted
```

- `Stopped`: capture is disabled by the user.
- `Armed`: capture was requested but eligibility is not currently satisfied.
- `Recording`: eligible snapshots are being accepted for one capture run.
- `Flushing`: no new snapshots are accepted while queued evidence is written.
- `Faulted`: correctness cannot be guaranteed; explicit operator action is
  required after the cause is resolved.

Start while ineligible enters `Armed`, not `Recording`. Stop is idempotent and
uses the bounded flush contract. Each transition into `Recording` creates a new
`capture_run_id`; an old run is never reopened or appended by a later start.

## Provisional storage roots and separation

The current preference is a configurable storage root, with a future default expressed through an
environment-relative location such as:

```text
%LOCALAPPDATA%/FF1600TireEstimator/
├── evidence/
│   └── sessions/<session-key>/
│       ├── manifests/
│       ├── telemetry/
│       ├── checkpoints/
│       └── diagnostics/
└── models/
```

`evidence/` and `models/` are separate trust domains:

- Evidence is append-only and must not be edited, replaced, compacted in
  place, or deleted by the plugin.
- Models are derived artifacts. They may be versioned, superseded, or removed
  without modifying their source evidence.
- Generated evidence and models remain outside the repository by default.

No database is planned for capture or authoritative storage. JSON Lines
segments are the source of truth. If a query index is useful later, it must be
a disposable derived artifact that can be rebuilt entirely from evidence and
must not be required to interpret or preserve a capture.

## Provisional identity

Every record carries both a logical session key and a capture-run ID.

- `session_key`: initially based on SimHub's `GameData.SessionId` plus the game
  name. After the raw-access spike, iRacing `SubSessionID` and `SessionID` may
  be added as stronger source identifiers; they must not replace earlier keys.
- `capture_run_id`: a new GUID for each recorder start/restart within a
  session. It prevents accidental continuation after a process crash.
- `sequence`: monotonically increasing within one capture run.
- `lap_id`: SimHub's lap GUID when available.

Identity fields are stored in records, not inferred solely from directory
names.

## Provisional record envelope

The current proposal uses UTF-8 JSON Lines for append-only streams. Each line is one independently
parseable record with this envelope:

```json
{
  "schema_version": 1,
  "record_type": "telemetry",
  "session_key": "iracing:<simhub-session-guid>",
  "capture_run_id": "<guid>",
  "sequence": 1,
  "captured_at_utc": "2026-07-15T04:00:00.0000000Z",
  "frame_time_utc": "2026-07-15T04:00:00.0000000Z",
  "sim_time_seconds": null,
  "payload": {}
}
```

Rules:

- `schema_version` is mandatory and only increments with a documented schema
  change.
- UTC timestamps use round-trip ISO 8601 format.
- Numeric units are encoded in field names or fixed by the schema; they never
  depend on SimHub display preferences.
- Unavailable values are `null`, not zero or an invented sentinel.
- New optional fields may be added within a schema version. Renaming, changing
  units, or changing meaning requires a new version.

## Candidate telemetry payload: initial normalized set

The first implementation should take immutable snapshots from confirmed
normalized members only:

- Context: game name, car ID/model, track ID/configuration, session type.
- Progress: current/completed lap, lap GUID, track-position percent/meters,
  current lap time, and session time remaining.
- State: game running/paused/replay, in pit, in pit lane, lap valid.
- Driver inputs: throttle, brake, clutch, gear, steering/orientation values
  selected during implementation review.
- Motion: speed, longitudinal/lateral/heave acceleration where available, and
  yaw/roll/pitch rates where available.
- Environment: air temperature, road temperature, and track length.
- Tires: normalized temperature, inner/middle/outer temperature, pressure,
  wear, and dirt for FL/FR/RL/RR, with explicit units.
- Useful heat inputs: four brake temperatures where available.

Before implementation, each selected member needs a field table recording its
exact SDK property, JSON name, type, unit, nullability, and observed iRacing
behavior. Values that SimHub exposes but iRacing does not update continuously
must still be logged truthfully; the persistence layer must not reinterpret
them as live measurements.

## Provisional pit-temperature checkpoints

Pit checkpoints are a separate append-only stream. A checkpoint records an
observed transition in reported carcass values; it is not merely a copy of the
latest telemetry frame.

Required fields:

- Standard record envelope and linkage to the triggering telemetry sequence.
- Pit-entry/pit-lane/garage state at observation time.
- Previous and new FL/FR/RL/RR inner/middle/outer values.
- Per-corner changed flags and value deltas.
- Last on-track sequence, lap, and timestamp preceding the checkpoint.
- Detection reason and detector version.

The observed transition changed all twelve channels atomically one telemetry
tick before the observed `OnPitRoad` rising edge. The detector must therefore
trigger from a change in the temperature vector itself. Pit, pit-lane, garage,
and session state are recorded as context and validation signals, not assumed
to be an earlier trigger. Exact acceptance, reset, duplicate, and debounce
rules remain blocked on the checkpoint repeatability experiment.

## Provisional append-only files

Use numbered immutable segments rather than one indefinitely growing file:

```text
telemetry/telemetry-000001.jsonl
checkpoints/pit-temperature-000001.jsonl
diagnostics/recorder-000001.jsonl
manifests/manifest-<capture-run-id>.json
```

- Create new segments with create-new semantics; never truncate an existing
  path.
- A writer appends only to the segment it created for its capture run.
- Rotation creates the next segment; it never rewrites the previous one.
- A manifest is write-once for a capture run. Later facts use a new manifest
  or diagnostic record rather than editing it.
- A partial final line after a crash remains untouched. Readers may ignore the
  incomplete tail and record recovery in a new diagnostic segment.

## Confirmed `DataUpdate` boundary and provisional handoff

`DataUpdate` may only validate state, construct a bounded immutable snapshot,
and attempt a non-blocking handoff. It must not open files, serialize JSON,
flush, rotate, wait on locks, or fit models.

A future writer owns serialization and disk access. Its queue is bounded so a
slow or failed disk cannot consume memory without limit.

Queue saturation creates an unavoidable choice between blocking and missing
frames. The approved policy is fail-explicit:

1. Never block `DataUpdate`.
2. Never overwrite or evict a snapshot already accepted by the queue.
3. If a snapshot cannot be accepted, atomically count the rejected frame and
   transition the recorder to a degraded/faulted state.
4. Persist a gap diagnostic with first/last affected sequence or timestamp and
   rejection count as soon as the writer can do so.
5. Expose recorder health through plugin properties.

This does not claim the missing frame was preserved. It preserves the honesty
of the evidence set and prevents silent loss.

## Provisional flush and shutdown

- Periodic flush cadence and segment size are configuration with recorded
  values in the run manifest.
- `End` requests writer completion and waits only for a reviewed, bounded
  timeout outside `DataUpdate`.
- On timeout, record the condition when possible and abandon no in-memory data
  silently. The implementation must expose an unclean-shutdown diagnostic on
  the next run.
- Process crashes and power loss can still truncate the active final record;
  the recovery rule above preserves the original bytes.

## Provisional failure policy

Disk-full, access-denied, path, serialization, and writer exceptions must:

- Be caught on the writer side.
- Stop accepting new evidence when correctness cannot be guaranteed.
- Leave existing evidence untouched.
- Emit SimHub logs and central recorder-health properties.
- Avoid automatic deletion, compaction, or fallback to an unrecorded location.

## Decisions required before implementation

1. Repeat the carcass transition experiment across normal pit return,
   tow/reset, and fresh-session workflows; define checkpoint acceptance,
   duplicate, and reset rules from those observations.
2. Define the version-checked raw iRacing adapter contract and its fail-closed
   behavior before using raw fields in production.
3. Produce the exact field/unit/nullability table from observed FF1600 data.
4. Select sampling cadence from the observed approximately 60 Hz `DataUpdate`
   rate and desired model
   bandwidth.
5. Size the bounded queue and segment rotation from measured record size and
   worst-case disk stalls.
6. Confirm action/state/property behavior in SimHub, including start while
   ineligible, car changes, repeated stop, and fault recovery.
7. Optimize and remeasure the full-cadence temperature detector without the
   prototype's per-frame reflection, allocations, or summary formatting.
