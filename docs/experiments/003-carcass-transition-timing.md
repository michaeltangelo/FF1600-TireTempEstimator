# Experiment 003: Carcass Transition Timing

## Status

Planned

## Question

At full `DataUpdate` cadence, when do the twelve raw FF1600 carcass channels
change relative to pit entry, do they change in one sample, and what runtime
overhead does the cached-reflection probe add?

## Environment

- Date/time and timezone: To be recorded
- Git commit: To be recorded after the probe commit
- SimHub/iRacing versions: To be recorded if available
- Plugin build configuration: Debug, .NET Framework 4.8, AnyCPU
- Car: Ray Formula 1600 (`raygr22`)
- Track/session type: To be recorded

## Probe property

```text
FF1600Tires.DiscoverySummary
```

The existing single property appends a carcass-transition section. No telemetry
is written to disk.

## Procedure

1. Build and deploy with SimHub stopped.
2. Start an FF1600 session and capture the initial transition section.
3. Leave the pits and drive long enough to generate tire heat.
4. Return to the pits and wait for the reported carcass values to update.
5. Capture the transition section showing pit-entry/change counters, tick,
   session time, changed-channel mask, before/after values, and probe timing.
6. Exit the session and confirm the probe returns to waiting state without
   affecting plugin health.

## Observations

Not yet executed.

## Artifacts

No telemetry files are produced. Preserve screenshots externally and record
filenames, sizes, hashes, and provenance here.

## Conclusion

Pending.

## Remaining uncertainty

Pending experiment execution.
