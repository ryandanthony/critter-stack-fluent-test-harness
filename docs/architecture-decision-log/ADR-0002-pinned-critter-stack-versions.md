# ADR-0002: Pinned stack: .NET 10, Marten 9, Wolverine 6, Alba 8, xunit v3

- **Status:** accepted (exact Marten, WolverineFx and Alba pins superseded by [ADR-0018](./ADR-0018-critter-stack-floor-and-latest.md): those are now a floor, tested on the floor and on the latest releases)
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Files: `global.json`, `src/Directory.Packages.props`; Risks: verified 2026-10-01)

## Context

The harness depends on behaviour of specific Critter Stack versions (exception shapes, daemon requirements, return values on type mismatch) that isn't documented as contract. Those behaviours have to be pinned and checked, not assumed.

## Decision

- **.NET 10 SDK** pinned in `global.json` (`10.0.302`, `rollForward: latestFeature`), with `"test": { "runner": "Microsoft.Testing.Platform" }`, because the .NET 10 SDK won't run xunit v3 through the old VSTest path without it.
- **Exact versions** in `src/Directory.Packages.props` (central package management): Marten 9.44.0, WolverineFx and WolverineFx.Marten 6.44.0, Alba 8.5.3, xunit.v3 4.0.1 (for assembly fixtures), Testcontainers.PostgreSql 4.15.0, GitVersion.MsBuild 6.8.2, FestinaLente.CodeStandards 1.1.10.
- The test host also needs **WolverineFx.RuntimeCompilation**: Wolverine 6.44 no longer ships the Roslyn compiler, and a host in the default `TypeLoadMode.Dynamic` fails to start without it. That is the host's dependency, not the harness library's.
- Every Critter Stack API the design names was verified three ways (Context7 docs, reflection over the pinned assemblies, a runtime probe against Postgres 17 with and without a daemon). Each finding is a test, so an upgrade that changes behaviour fails CI.

## Consequences

- Upgrades are deliberate: bump the pins, run the suite, update [reference/compatibility.md](../reference/compatibility.md).
- `FestinaLente.CodeStandards` comes from the festina-lente-io GitHub Packages feed, which needs a token even to read, locally and in CI ([ADR-0011](./ADR-0011-build-quality-versioning-and-ci.md)).
