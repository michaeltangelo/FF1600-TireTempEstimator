# Telemetry Experiment Records

This directory contains durable summaries of telemetry discovery experiments.
Results document what was actually observed; they do not silently promote a
hypothesis into an architectural requirement.

Use a numbered file for each experiment:

```text
001-normalized-identity.md
002-raw-compatibility.md
003-temperature-update-behavior.md
```

## Required record

```markdown
# Experiment NNN: Title

## Status

Planned | In progress | Complete | Inconclusive

## Question

What uncertainty does this experiment resolve?

## Environment

- Date/time and timezone:
- Git commit:
- SimHub version:
- iRacing version/build if available:
- Plugin build configuration:
- Cars/tracks/session types:

## Procedure

Numbered, repeatable steps.

## Observations

Exact values and transitions. Separate observation from interpretation.

## Artifacts

External durable paths, filenames, checksums, and provenance for captures.
Do not commit generated telemetry without explicit review.

## Conclusion

What the evidence supports.

## Remaining uncertainty

What is still unknown and what experiment should address it.
```
