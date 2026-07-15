# Experiment 001: Normalized Identity and Update Cadence

## Status

Planned

## Question

Which normalized SimHub values reliably distinguish an active iRacing FF1600
session from another car or an ineligible state, and what `DataUpdate` cadence
does the plugin observe?

## Environment

- Date/time and timezone: To be recorded
- Git commit: To be recorded after the probe commit
- SimHub version: To be recorded
- iRacing version/build if available: To be recorded
- Plugin build configuration: Debug, .NET Framework 4.8, AnyCPU
- Cars/tracks/session types: FF1600 and at least one non-FF1600 car; track and
  session types to be recorded

## Probe properties

```text
FF1600Tires.DiscoveryGameName
FF1600Tires.DiscoveryCarId
FF1600Tires.DiscoveryCarModel
FF1600Tires.DiscoverySessionId
FF1600Tires.DiscoveryGameRunning
FF1600Tires.DiscoveryGamePaused
FF1600Tires.DiscoveryGameReplay
FF1600Tires.DiscoverySpectating
FF1600Tires.DiscoveryIsInPit
FF1600Tires.DiscoveryIsInPitLane
FF1600Tires.DiscoveryFrameTimeUtc
FF1600Tires.DiscoveryDataUpdateHz
```

## Procedure

1. Build and deploy the probe with SimHub stopped.
2. Start SimHub without an active game and record all probe values.
3. Join an iRacing session in the FF1600 and record values in the garage, pit
   lane, and on track.
4. Pause where supported and record the observed state and cadence.
5. Enter replay or spectating mode and record the observed state.
6. Leave the session and confirm identity/state transitions.
7. Join an iRacing session with a non-FF1600 car and repeat garage, pit-lane,
   and on-track observations.
8. Restart SimHub and confirm the probe recovers without retained identity from
   the previous process.

## Observations

Not yet executed. Record exact values and transitions here before drawing an
eligibility conclusion.

## Artifacts

No telemetry files are produced by this probe. Screenshots or manually exported
property tables should be stored durably and listed here with paths and
checksums if used as evidence.

## Conclusion

Pending.

## Remaining uncertainty

Pending experiment execution.
