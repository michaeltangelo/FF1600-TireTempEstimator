# Architecture

## Objective

Provide a persistent SimHub plugin that estimates live temperatures for all
four tires of iRacing's FF1600 and later exposes tire-readiness properties to
SimHub dashboards.

## Observed telemetry behavior

iRacing carcass-temperature telemetry is not a continuous on-track signal. It
remains static while driving and changes as a step after returning to the pits.
Those pit values are therefore validation/calibration checkpoints, not a live
temperature feed.

## Planned data flow

1. Capture raw telemetry durably without modifying earlier observations.
2. Append pit-temperature checkpoints as distinct experimental evidence.
3. Fit replaceable models from preserved raw observations.
4. Run a lightweight four-tire estimator from live telemetry.
5. Publish estimator and later readiness values as central SimHub properties.

## Data boundaries

- **Raw telemetry:** durable, append-only experimental evidence.
- **Pit checkpoints:** append-only measured outcomes linked to a run/session.
- **Fitted models:** derived, versioned, and replaceable artifacts stored
  separately from evidence.
- **Live state:** transient estimator state owned by the plugin.
- **Presentation:** dashboards consume plugin properties and contain no copy of
  the estimation algorithm.

Generated captures are ignored by default. Curated samples may be added only
through an explicit review that confirms provenance and removes sensitive or
machine-specific data.

## Runtime constraints

`DataUpdate` is latency-sensitive. Future persistence, checkpoint processing,
and model work must be moved off its synchronous path with bounded buffering
and explicit shutdown/flush behavior. No such worker is part of the current
bootstrap milestone.

## Current milestone boundary

The bootstrap milestone proves that the official template builds, deploys, and
loads, then introduces only the renamed plugin and two health properties. It
does not implement the planned data pipeline or estimator.
