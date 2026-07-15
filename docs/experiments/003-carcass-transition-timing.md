# Experiment 003: Carcass Transition Timing

## Status

Completed

## Question

At full `DataUpdate` cadence, when do the twelve raw FF1600 carcass channels
change relative to pit entry, do they change in one sample, and what runtime
overhead does the cached-reflection probe add?

## Environment

- Date/time and timezone: 2026-07-15 02:22-02:29 America/New_York
- Probe commits: `49c386c` (initial probe), `3897747` (event retention and
  timing warm-up correction)
- SimHub/iRacing versions: Not recorded
- Plugin build configuration: Debug, .NET Framework 4.8, AnyCPU
- Car: Ray Formula 1600 (`raygr22`)
- Track/session type: Not recorded

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

The first run revealed that a later pit-entry event overwrote the retained
temperature-change detail. The follow-up probe retains `LastPitEntry` and
`LastTemperatureChange` independently and excludes the first 120 calls from
average/maximum timing to reduce JIT/accessor-initialization distortion.

### Initial run

The initial pit capture reported 3,840 samples, one pit entry, no temperature
change, an average probe time of 10.22 microseconds, and a maximum of 9,597.10
microseconds. After driving and returning to the pits, it reported 10,393
samples, two pit entries, and one temperature-change event. Average probe time
was 13.55 microseconds and the maximum was 12,516.00 microseconds. The retained
event contained only the later pit entry, exposing the event-retention defect.

### Corrected run

Before driving, the corrected probe reported 2,186 samples, three pit entries,
no temperature-change events, and 2,065 timing samples after warm-up. Average
probe time was 16.84 microseconds and the maximum was 226.60 microseconds.

After driving and returning to the pits, it reported:

- 7,539 samples and 7,418 timing samples after warm-up
- four pit entries and one temperature-change event
- average probe time 17.40 microseconds; maximum 11,493.10 microseconds
- temperature change at tick 30,426, session time 522.33333
- pit entry at tick 30,427, session time 522.35
- all 12 channels changed in the same `DataUpdate` sample

The twelve before/after values were:

| Channel | Before | After |
| --- | ---: | ---: |
| LF CL | 55.13269 | 54.65268 |
| LF CM | 59.58139 | 57.75974 |
| LF CR | 59.92386 | 58.10016 |
| RF CL | 61.98669 | 59.53775 |
| RF CM | 61.69684 | 59.29474 |
| RF CR | 59.60919 | 57.53510 |
| LR CL | 54.74716 | 54.43710 |
| LR CM | 57.82605 | 57.05206 |
| LR CR | 58.01865 | 57.24112 |
| RR CL | 59.53101 | 58.58432 |
| RR CM | 59.42996 | 58.49966 |
| RR CR | 57.89227 | 57.21637 |

## Artifacts

No telemetry files were produced. The user supplied these ShareX screenshots;
they remain external to the repository:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `SimHubWPF_2026-07-15_02-22-01.png` | 96,198 | `046B7C83CEB2DE34B299366D94F4D439B8F6D58A75174CBA463C993932A63171` |
| `Discord_2026-07-15_02-23-52.png` | 84,535 | `DF47238444337F36D7D37E32D7D7C47863018DD2E1486593378048DA5C8F6593` |
| `Discord_2026-07-15_02-28-00.png` | 68,980 | `8AD3FDB99096A826711C4973AFD691FFCDDC7F2A5EAAE2A477B0AE0C4B1D238D` |
| `Discord_2026-07-15_02-29-30.png` | 101,537 | `82DE43B34050980741DD5D25EF3ECFAF124C5F0DDCEF50DA9EF8EA8D765A283B` |

## Conclusion

The FF1600 carcass-temperature checkpoint is an atomic update across all 12
channels in the observed return-to-pits transition. It occurred one telemetry
tick before the observed `OnPitRoad` rising edge. A checkpoint detector must
therefore trigger on the temperature values themselves; `OnPitRoad` is useful
as validating context but is not a reliable trigger for capturing the change.

## Remaining uncertainty

- This was one corrected transition in one session; repeatability across fresh
  sessions, tracks, and pit workflows remains untested.
- SimHub and iRacing version numbers and the track/session type were not
  recorded.
- The approximately 17-microsecond average is small relative to a 60 Hz update
  interval, but the 11.49-millisecond maximum outlier is not acceptable as a
  production target. The prototype still performs per-sample reflection,
  allocations, and formatting; production code requires a separate optimized
  implementation and measurement.
