# PDR-0010: Services adopt the harness as a NuGet package

- **Status:** accepted
- **Date:** 2026-11-16
- **Deciders:** Ryan
- **Supersedes:** [PDR-0006](./PDR-0006-adopt-by-copying-the-project.md), and the "Ship as a NuGet package" bullet of [PDR-0008](./PDR-0008-v1-non-goals.md)

## Context

[PDR-0006](./PDR-0006-adopt-by-copying-the-project.md) had services copy `src/CritterStack.TestHarness/` into their own repo, deferring a package until the API settled. In practice that makes every consumer own a fork: fixes and Critter Stack upgrades don't reach them, there's no way to tell which version a service copied, and the consumer's repo carries harness source it shouldn't be editing. The API is now stable through v1 and several rounds of coverage work. The tooling for packages already exists in Ryan's other repos (awen publishes to nuget.org from CI).

## Decision

- A service adopts the harness with **one package reference**: `<PackageReference Include="CritterStack.TestHarness" />` in its test project, with the version in its own `Directory.Packages.props`.
- The package brings **only Alba, Marten and WolverineFx** as dependencies, at the versions the harness was tested against (as minimums). Nothing from this repo's build (analyzers, StyleCop, GitVersion, the private feed) reaches the consumer.
- It ships **XML docs** (IntelliSense carries the step-ordering rules), a **symbols package**, and the README.
- Versions follow **SemVer** from Conventional Commits ([ADR-0011](../architecture-decision-log/ADR-0011-build-quality-versioning-and-ci.md)). A breaking change to the public API is a major bump.
- Everything that varies between services is still a `HarnessOptions` field, never a fork.

## Alternatives considered

- **Keep copying the project** ([PDR-0006](./PDR-0006-adopt-by-copying-the-project.md)). Rejected: forks drift, upgrades don't flow, no version trail.
- **Git submodule.** Rejected (as before): awkward in every consumer's CI and IDE.

## Consequences

- Drives the packaging and release design ([ADR-0013](../architecture-decision-log/ADR-0013-ship-the-harness-as-a-nuget-package.md)).
- Consumers can't patch the harness locally. A gap becomes an issue or PR here, and a release.
- A consumer whose Marten/Wolverine/Alba versions are older than the package's minimums can't upgrade the harness until it upgrades those.
- `docs/how-to/add-to-a-service.md` becomes "add the package".

## Non-goals

- Multi-targeting older .NET or Critter Stack majors.
- A source-only package.
