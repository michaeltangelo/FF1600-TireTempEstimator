# Experiment 001: Normalized Identity and Update Cadence

## Status

Complete with follow-up gaps

## Question

Which normalized SimHub values reliably distinguish an active iRacing FF1600
session from another car or an ineligible state, and what `DataUpdate` cadence
does the plugin observe?

## Environment

- Date/time and timezone: July 15, 2026, 01:34-01:37 America/New_York
- Git commit: `8716db5` (`Add consolidated telemetry discovery display`)
- SimHub version: Not captured; executable metadata reported `1.0.0.0`, which
  is not treated as a reliable product version
- iRacing version/build if available: Not captured
- Plugin build configuration: Debug, .NET Framework 4.8, AnyCPU
- Cars: Ray Formula 1600 and Mazda MX-5 Cup
- Track/session type: Not captured

## Probe property

```text
FF1600Tires.DiscoverySummary
```

The single multiline property reports game, car, session, running, paused,
replay, spectating, pit, frame-time, and update-cadence values. Bind one SimHub
text widget to this property and capture the complete display at each state.

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

The screenshots were supplied in timestamp order following the planned state
sequence. The initial no-game state was omitted; the post-session state was
reported by the operator as effectively equivalent.

| Local capture time | Operator state | Car identity | Running | Pit / lane | Hz |
| --- | --- | --- | --- | --- | --- |
| 01:34:53 | FF1600 garage | `raygr22` / `Ray Formula 1600` | True | 1 / 1 | 60.02 |
| 01:35:20 | FF1600 pit lane | `raygr22` / `Ray Formula 1600` | True | 1 / 1 | 59.99 |
| 01:35:33 | FF1600 on track | `raygr22` / `Ray Formula 1600` | True | 0 / 0 | 59.96 |
| 01:35:42 | FF1600 returned to pits | `raygr22` / `Ray Formula 1600` | True | 1 / 1 | 60.00 |
| 01:35:48 | Intended replay/spectating check | `raygr22` / `Ray Formula 1600` | True | 1 / 1 | 60.02 |
| 01:36:02 | FF1600 session exited | unavailable / unavailable | False | unavailable | 59.98 |
| 01:37:19 | Other-car garage | `mx5_mx52016` / `Mazda MX-5 Cup` | True | 1 / 1 | 59.97 |
| 01:42:52 | FF1600 replay | `raygr22` / `Ray Formula 1600` | True | 0 / 0 | 60.02 |

Additional observations:

- `Game` remained `IRacing` after the session exited, while `GameRunning`
  became false and car identity became unavailable.
- `GamePaused` remained false in every capture.
- In the confirmed replay capture, `GameReplay` remained false while
  `Spectating` became true. Normalized replay exclusion cannot rely on
  `GameReplay`; `Spectating` must be checked independently.
- The displayed FF1600 session ID appeared stable across the active-session
  captures and changed after exit, but the widget clipped the long GUID at its
  edges. Exact GUID equality is not claimed from these images.
- Both normalized pit flags were `1` in the garage and pit lane. They
  distinguished those states from on-track driving but did not distinguish
  garage from pit lane in this experiment.
- `FrameTimeUtc` advanced throughout the experiment.
- Observed update cadence stayed tightly grouped around 60 Hz.

## Artifacts

No telemetry files were produced. The external ShareX screenshot directory is
the durable source for these images; the images are not committed to Git.

| Filename | Bytes | SHA-256 |
| --- | ---: | --- |
| `Discord_2026-07-15_01-34-53.png` | 174276 | `C335C652FADF371025C7B94FA65D53CEA28A37C6270DBD4A0C895163F716AB4D` |
| `Discord_2026-07-15_01-35-20.png` | 193067 | `F81D5F4AEB3066A0B91679D4A5E201A828839BF38E5185D473368D1ADBF408AD` |
| `Discord_2026-07-15_01-35-33.png` | 268402 | `6D4D648004941CE63E36F6DF2DFC48EA1AA43F9B50EA68559D77E4BC4B93CC6F` |
| `Discord_2026-07-15_01-35-42.png` | 205936 | `731E6AB6ECF583024E944E7EC8D03B981403543CC26507D14895784EC55E996B` |
| `Discord_2026-07-15_01-35-48.png` | 192263 | `40ACBE075CCEC63D2ACB09D2FD9BF5708DB37608D65C2E643168C8B6A412B718` |
| `SimHubWPF_2026-07-15_01-36-02.png` | 11211 | `36C68B9AF3BE8B9BFFD0DAEA2975FC4ADA3DC592399FA2E3E84E979DAA7FC8B9` |
| `SimHubWPF_2026-07-15_01-37-19.png` | 238645 | `C564E8EDBAD397EA5BDAD2862A8038FB94E558F55837DD29DE0A6DF056A317CB` |
| `SimHubWPF_2026-07-15_01-42-52-replay.png` | 190213 | `5FF270D3E88C83F9DBC8F1B3A7701B39CFE3086B7C1D7E947442E1B7C55F7742` |

## Conclusion

The normalized FF1600 identity observed in this environment is:

```text
GameName: IRacing
CarId: raygr22
CarModel: Ray Formula 1600
```

The Mazda observation demonstrates that normalized car identity can separate
the tested FF1600 from at least one other iRacing car. A future eligibility rule
must require `GameRunning == true` in addition to exact approved car identity;
`GameName == IRacing` or session ID alone is insufficient after session exit.
It must also require `Spectating == false`; the tested replay presented as
spectating while normalized `GameReplay` remained false.

The normalized pit flags are candidates for separating on-track from pit-area
states, not for distinguishing garage from pit lane. Approximately 60 Hz is the
first measured `DataUpdate` cadence and is evidence for later sampling-rate and
buffer calculations, not yet a required rate.

## Remaining uncertainty

- Repeat the non-FF1600 observation on track; only its garage state was
  captured.
- Capture a readable full session GUID or export the summary as text.
- Capture the actual SimHub product version, track, and session type.
- Test at least one additional FF1600 session to see whether `CarId` and
  `CarModel` remain stable across tracks and session types.
