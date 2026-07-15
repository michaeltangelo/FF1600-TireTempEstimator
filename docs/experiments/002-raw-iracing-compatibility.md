# Experiment 002: Raw iRacing Compatibility

## Status

Planned

## Question

What concrete runtime object does SimHub return from
`StatusDataBase.GetRawDataObject()` for iRacing, and can selected session,
replay, pit, and four-corner carcass members be read safely across lifecycle
states?

## Environment

- Date/time and timezone: To be recorded
- Git commit: To be recorded after the probe commit
- SimHub version: To be recorded from the UI if available
- iRacing version/build if available: To be recorded
- Plugin build configuration: Debug, .NET Framework 4.8, AnyCPU
- Cars/tracks/session types: FF1600; exact environment to be recorded

## Probe property

```text
FF1600Tires.DiscoverySummary
```

The existing single multiline property appends a raw compatibility section
sampled at 1 Hz. No additional SimHub properties are registered.

## Candidate raw members

- Runtime assembly-qualified type
- `SessionUniqueID`, `SessionTime`, and `TickCount`
- `IsInGarage`, `OnPitRoad`, and `IsReplayPlaying`
- `SessionData.WeekendInfo.SessionID` and `SubSessionID`
- `LF/RF/LR/RR tempCL`, `tempCM`, and `tempCR`

Missing members, null intermediate objects, and reflection exceptions are
rendered explicitly in the summary.

## Procedure

1. Build and deploy with SimHub stopped.
2. Start SimHub without an active session and capture the raw section.
3. Enter an FF1600 session and capture garage, pit-lane, and on-track states.
4. Observe whether tick/session time advance and whether the runtime type stays
   constant.
5. Capture raw carcass values before driving, while on track, and after
   returning to the pits.
6. Load a replay and capture raw replay, spectating, session, and carcass state.
7. Exit the session and confirm null/error behavior does not affect plugin
   health.

## Observations

Not yet executed.

## Artifacts

No telemetry files are produced. Preserve screenshots externally and record
filenames, sizes, hashes, and provenance here.

## Conclusion

Pending.

## Remaining uncertainty

Pending experiment execution.
