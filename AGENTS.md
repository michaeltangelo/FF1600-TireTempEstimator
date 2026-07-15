# Repository Guidelines

## Scope

This repository contains a classic C# SimHub plugin for iRacing's FF1600.
Preserve compatibility with the installed official SimHub SDK template.

## Non-negotiable constraints

- Keep the plugin on .NET Framework 4.8 and AnyCPU unless a separately
  approved migration is supported by SimHub.
- Do not modernize the project format, upgrade dependencies, or replace
  SimHub references speculatively.
- Use documented or official-template-demonstrated SimHub APIs.
- Never commit `.vs`, `bin`, `obj`, `*.csproj.user`, secrets, or generated
  telemetry.
- Never overwrite or discard raw experimental evidence. Raw telemetry and
  append-only pit-temperature checkpoints are durable inputs.
- Keep fitted models separate from raw evidence. Models are replaceable;
  observations are not.
- Future file I/O and model fitting must not block `DataUpdate`.
- Expose central plugin properties for dashboards and widgets. Do not copy
  estimation algorithms into dashboard expressions.

## Change protocol

Explain meaningful changes before applying them, keep commits focused, inspect
diffs, and build after meaningful plugin edits. Stop and ask rather than
guessing about SimHub lifecycle, property, or deployment behavior.

Do not add telemetry logging, thermal estimation, readiness logic, background
workers, or custom WPF UI during the repository-bootstrap milestone.
