# FF1600 Tire Temperature Estimator

A persistent C# SimHub plugin for estimating live tire temperatures in the
iRacing FF1600.

## Why this exists

iRacing's reported carcass temperatures remain static while the car is on
track and update as a step when the car returns to the pits. This project will
use live telemetry together with those pit measurements to build and validate
a four-tire temperature estimator.

## Current milestone

The repository currently contains a verified, untouched copy of SimHub's
official `User.PluginSdkDemo` template. It targets .NET Framework 4.8 and has
been built, deployed, and manually verified in SimHub.

The next bootstrap step is to rename the plugin and expose two health-check
properties. Telemetry logging, thermal estimation, tire readiness, background
workers, and custom WPF UI are intentionally out of scope for this milestone.

## Repository layout

- `src/FF1600TireEstimator.Plugin/` — SimHub plugin solution and source.
- `docs/architecture.md` — architectural boundaries and planned data flow.
- `docs/telemetry-design.md` — telemetry discovery plan and provisional
  persistence contract.
- `docs/testing-protocol.md` — repeatable build and SimHub health checks.
- `data-samples/` — future curated telemetry samples; raw evidence must never
  be overwritten.

## Build prerequisites

- Windows with SimHub installed.
- Visual Studio/MSBuild with .NET Framework 4.8 tooling.
- `SIMHUB_INSTALL_PATH` configured as expected by the official template.

Build the solution under `src/FF1600TireEstimator.Plugin/`. The template's
post-build command deploys the DLL/PDB to SimHub and its RESX file to SimHub's
`Languages` directory.

See `docs/testing-protocol.md` before changing lifecycle or deployment code.
