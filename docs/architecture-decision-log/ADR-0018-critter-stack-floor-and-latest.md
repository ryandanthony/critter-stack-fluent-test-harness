# ADR-0018: Critter Stack floor versions, tested on the floor and on the latest releases

- **Status:** accepted
- **Date:** 2026-11-17
- **Deciders:** Ryan
- **Supersedes:** the exact Marten, WolverineFx and Alba pins in [ADR-0002](./ADR-0002-pinned-critter-stack-versions.md)
- **Source:** P04 (Critter Stack upgrade), 2026-11-17

## Context

[ADR-0002](./ADR-0002-pinned-critter-stack-versions.md) pins Marten, WolverineFx and Alba to exact versions, and the library is built against those pins. NuGet turns a library's resolved versions into its package's minimum dependency versions. So every bump of the test pins raised the minimum for every consumer, and keeping the harness "current" meant a harness release each time upstream shipped. Marten, Wolverine and Alba release often (between 2026-10-01 and 2026-11-17: Marten 9.44 to 9.62, WolverineFx 6.44 to 6.64.2, Alba 8.5.3 to 9.0.0).

The dependency itself was never the problem. The published 1.3.4 declares `Alba >= 8.5.3`, `Marten >= 9.44.0`, `WolverineFx >= 6.44.0` with no upper bound, and a consumer on Marten 9.62, WolverineFx 6.64.2 and Alba 9.0.0 ran its scenarios green against it unchanged (verified 2026-11-17). The gap was that nothing proved newer releases still work, so the only safe move looked like bumping the pins.

## Decision

- **Floor.** `src/Directory.Packages.props` holds `MartenFloor`, `WolverineFloor` and `AlbaFloor`, the oldest versions the harness supports (9.44.0, 6.44.0, 8.5.3 today). The library always compiles against the floor (`VersionOverride` in its csproj), so the floor is the package's minimum, open ended. Raise a floor only when the harness needs an API or fix it doesn't have, or when the floor stops working with the latest releases. Raising one is a breaking change for consumers below it (`feat!:`).
- **Tested versions.** `MartenVersion`, `WolverineVersion` and `AlbaVersion` default to the floor and can be set from the environment or `-p:`. The test project references Alba, Marten and WolverineFx directly at those versions, so it runs the floor-built harness against them, as a consumer on newer versions would. Every `WolverineFx.*` package uses `WolverineVersion`.
- **Two runs.**
  - `build.yml` (every push and PR, gates releases) runs `just test` and `just package-check` on the floor.
  - `compatibility.yml` (every push to `main`, weekly on Mondays, and on demand) resolves the newest stable releases on nuget.org (`scripts/latest-versions.sh`) and runs the same two recipes against them. A failure opens or updates one `compatibility` issue. The next green run closes it. It doesn't block builds or releases.
  - Locally: `just test-latest` and `just package-check-latest`.
- **Consumer sample.** `samples/ConsumerService` references Alba, Marten and WolverineFx itself, as a real service does, at the floor by default. The latest run moves it through the same three properties.
- **Source compatible across majors.** Test and sample code must build on both the floor and the latest versions (for example no `ConfigureAwait` on `AlbaHost.For`, which returns a `Task` in Alba 8 and an awaitable `AlbaHostBuilder` in Alba 9).
- Everything else in ADR-0002 stands: the SDK pin, exact pins for xunit, Testcontainers, GitVersion and FestinaLente.CodeStandards, the RuntimeCompilation note, and turning version-dependent behaviour into tests.

## Consequences

- A new Marten, Wolverine or Alba release needs no harness change and no harness release. Consumers upgrade on their own schedule. The compatibility run says, within a week, whether that release works.
- When the compatibility run fails, the harness gets a fix that keeps the floor building (a `fix:` release) or the floor is raised (`feat!:`). Either way it is a deliberate change with an issue behind it.
- The behaviour listed in [compatibility.md](../reference/compatibility.md) is now asserted on two version sets. A behaviour that differs between them has to be handled in the classifier or sync code, not just re-pinned.
- The library can't use an API newer than the floor without raising the floor.
- Supersedes the exact Critter Stack pins in ADR-0002's Decision. ADR-0002 otherwise stays in force.
