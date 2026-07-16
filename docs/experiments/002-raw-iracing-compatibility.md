# Experiment 002: Raw iRacing Compatibility

## Status

Complete with follow-up gaps

## Question

What concrete runtime object does SimHub return from
`StatusDataBase.GetRawDataObject()` for iRacing, and can selected session,
replay, pit, and four-corner carcass members be read safely across lifecycle
states?

## Environment

- Date/time and timezone: July 15, 2026, 02:01-02:08 America/New_York
- Git commits: `295058a` (initial probe) and `0efca63` (corrected wrapper path)
- SimHub version: To be recorded from the UI if available
- iRacing version/build if available: To be recorded
- Plugin build configuration: Debug, .NET Framework 4.8, AnyCPU
- Car: Ray Formula 1600 (`raygr22`)
- Track/session type: Not captured

## Probe property

```text
FF1600Tires.DiscoverySummary
```

The existing single multiline property appends a raw compatibility section
sampled at 1 Hz. No additional SimHub properties are registered.

## Candidate raw members

- Runtime assembly-qualified type
- Nested `Telemetry`, `SessionData`, and `AllSessionData` runtime types
- `Telemetry.SessionUniqueID`, `SessionTime`, and `TickCount`
- `Telemetry.IsInGarage`, `OnPitRoad`, and `IsReplayPlaying`
- Wrapper and telemetry-nested `SessionData.WeekendInfo.SessionID` and
  `SubSessionID`
- `Telemetry.LF/RF/LR/RR tempCL`, `tempCM`, and `tempCR`

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

### Iteration 1: direct-member hypothesis rejected

The first probe assumed `GetRawDataObject()` returned `iRacingSDK.Telemetry`.
Six screenshots established instead that the runtime object was
`IRacingReader.DataSampleEx` from `ICarsReader`. Direct telemetry members were
reported as `<missing>`, while wrapper `SessionData` IDs returned zero.

Reflection of the installed `ICarsReader.dll` then confirmed this public shape:

```text
IRacingReader.DataSampleEx
├── Telemetry: iRacingSDK.Telemetry
├── SessionData: iRacingSDK.SessionData
└── AllSessionData: System.Object
```

The probe was corrected to follow `Telemetry.*` paths and rebuilt/deployed. The
built and deployed DLL SHA-256 hashes matched:

```text
6A7E2A74E500F8277F28711CD744284F6BFD9AD727410DC69C29F66E7659AA04
```

### Iteration 2: corrected paths

The operator supplied these exact state labels:

| Local capture | State | Normalized pit/lane | Raw garage | Raw pit road | Raw replay | Session time | Tick | Front CL/CM/CR | Rear CL/CM/CR |
| --- | --- | --- | --- | --- | --- | ---: | ---: | --- | --- |
| 02:08:12 | Garage | 0 / 0 | False | False | True | 79.850000127153 | 3887 | 52.3187 / 52.3187 / 52.3187 | 52.33371 / 52.33371 / 52.33371 |
| 02:08:16 | In pits | 1 / 1 | False | True | False | 84.8833334604861 | 4189 | 52.3187 / 52.3187 / 52.3187 | 52.33371 / 52.33371 / 52.33371 |
| 02:08:25 | On track | 0 / 0 | False | False | False | 92.9500001271523 | 4672 | 52.3187 / 52.3187 / 52.3187 | 52.33371 / 52.33371 / 52.33371 |
| 02:08:30 | Back in pits | 1 / 1 | False | True | True | 97.9666667938187 | 4973 | varied; see below | varied; see below |

After returning to the pits, the twelve raw channels changed to:

| Corner | CL | CM | CR |
| --- | ---: | ---: | ---: |
| LF | 52.4592 | 52.46695 | 52.4718 |
| RF | 52.4722 | 52.46701 | 52.45935 |
| LR | 52.47876 | 52.48209 | 52.48511 |
| RR | 52.48492 | 52.48206 | 52.47839 |

Additional observations:

- The wrapper and nested runtime types matched the reflected installed types.
- `SessionTime` and `TickCount` advanced across captures.
- All twelve carcass channels were static from the garage through the on-track
  capture, then all had new values in the next captured pit state.
- The 1 Hz screenshots establish a step between samples. They do not establish
  whether all channels update atomically within one telemetry tick.
- `Telemetry.IsInGarage` remained false in the operator-labeled garage state.
- `Telemetry.IsReplayPlaying` was true in the garage and after returning to the
  pits, but false in the intermediate pit and on-track states. It is not a
  reliable literal replay indicator for the planned eligibility gate.
- `Telemetry.OnPitRoad` matched the operator-labeled pit/on-track distinction
  in the in-pits, on-track, and returned-to-pits captures. The garage capture
  was false.
- `SessionUniqueID` remained `1`. Wrapper and telemetry-nested `SessionID` and
  `SubSessionID` remained `0`; none are accepted as durable session identity
  based on this experiment.
- After session exit, the raw wrapper and every probed value became unavailable
  without a reflection exception or plugin failure.

## Artifacts

No telemetry files were produced. The external ShareX screenshot directory is
the durable source; images are not committed to Git.

Initial-path screenshots:

| Filename | Bytes | SHA-256 |
| --- | ---: | --- |
| `Discord_2026-07-15_02-01-07.png` | 140034 | `14A0225B5CBC085CE0609C21D5A9E3829AB398881FA9B2BC7120149EA87E160D` |
| `Discord_2026-07-15_02-01-13.png` | 92960 | `D28848B2C56C223D28487F5F43346C068D738CB5E38622FFA125BB10F55A318C` |
| `Discord_2026-07-15_02-01-25.png` | 97301 | `DA4C3379ACF26757C64D2171F9FAE9E0D17F3ADB0EB5CC5776B83F90A7C9C296` |
| `Discord_2026-07-15_02-01-49.png` | 111803 | `4190E942A03CA89B34EF55FF9E9DA72BBCAEF99FD2922759E0A50D315DEA3578` |
| `Discord_2026-07-15_02-02-10.png` | 126849 | `E3631F630121337BB7029AA8998DEC4F9E2854A6F7AB3919B0C643170930D1D0` |
| `SimHubWPF_2026-07-15_02-02-29.png` | 98848 | `EF9935340E2877F31D9ED3FB1129B9BDE56258C79BC548E6CE152D0DB867E5E1` |

Corrected-path screenshots:

| Filename | Bytes | SHA-256 |
| --- | ---: | --- |
| `Discord_2026-07-15_02-08-12.png` | 93300 | `DF82617EF212B7F1430475A1B4C3F4FD2F886B7EFB735116BD333602AEA02725` |
| `Discord_2026-07-15_02-08-16.png` | 76450 | `D10661C7A8F65167049D44AAD364635D18E06021B1E60B59FFC08DB47C77B11F` |
| `Discord_2026-07-15_02-08-25.png` | 84393 | `C0E76EBF295BFF9FE491D41EFFEF15C0DD27939604E3F511BD92E1A23885DABB` |
| `Discord_2026-07-15_02-08-30.png` | 107492 | `E0835ED138835544865FFC2BC156B36779FE73993D52DA435792D965EC284432` |
| `SimHubWPF_2026-07-15_02-08-41.png` | 65755 | `D29E2BA39C98D025A1731DCD9FF89B1433D9F44613577067843E50642EA90B29` |

## Conclusion

Raw iRacing compatibility is viable in the installed environment through the
public `IRacingReader.DataSampleEx.Telemetry` wrapper path. This is a
version-sensitive dependency and must be guarded by runtime type/member checks
if adopted.

The experiment provides direct evidence for the project premise: the FF1600
carcass channels exposed by iRacing remained static while driving and changed
as a step on return to the pits. The raw carcass channels are therefore useful
as pit checkpoints, not live temperature measurements.

Normalized `GameRunning`, `CarId`, `CarModel`, and `Spectating` remain better
eligibility inputs than the tested raw replay/garage/session-ID fields. Raw
`OnPitRoad` is a useful candidate transition signal but requires more sessions
before approval.

## Remaining uncertainty

- Determine a durable iRacing session/subsession identifier, potentially from
  the wrapper dictionary, without assuming the zero-valued typed properties.
- Confirm raw wrapper/type/member stability across SimHub updates and another
  iRacing session.
- Verify units and map CL/CM/CR to physical inner/middle/outer orientation for
  left- and right-side tires.
- Capture higher-frequency transition evidence to determine update ordering and
  whether all twelve channels change atomically.
- Explain `IsReplayPlaying` and `IsInGarage` semantics before using either.
- Record actual SimHub/iRacing versions, track, and session type.
