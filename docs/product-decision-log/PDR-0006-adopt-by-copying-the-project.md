# PDR-0006: Services adopt the harness by copying the project

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Portability rule, Risks: verified portability), [how-to/add-to-a-service.md](../how-to/add-to-a-service.md)

## Context

The harness is for a handful of services owned by one person. Publishing and versioning a package adds a release process, a feed and version skew between services, before anyone knows whether the API is settled. Copying source in is cheap, and each service can patch it if a Critter Stack upgrade bites.

## Decision

- A service adopts the harness by **copying `src/CritterStack.TestHarness/`** (`.cs` and `.csproj` only) into its repo and referencing it from its test project.
- The only thing the service supplies is **package versions** for Alba, Marten and WolverineFx.
- The copied project must build and run **without anything else from this repo** (no build props, analyzers, private feeds or versioning).
- Everything that varies between services is a `HarnessOptions` field, never a code edit.

## Alternatives considered

- **NuGet package now.** Deferred, not rejected: a later PDR can supersede this one once the API is stable. CI already extracts the version a release job would need.
- **Git submodule.** Rejected: awkward in every consumer's CI and IDE.

## Consequences

- Drives the self-contained project rule ([ADR-0004](../architecture-decision-log/ADR-0004-self-contained-harness-project.md)) and the portability check in CI ([ADR-0012](../architecture-decision-log/ADR-0012-docs-and-portability-enforced-by-tests.md)).
- Public type names must not clash with Alba's (hence `HarnessAssertionException`, not `ScenarioAssertionException`).
- Fixes don't flow to copies automatically; consumers re-copy.

## Non-goals

- A published package or release process in v1 ([PDR-0008](./PDR-0008-v1-non-goals.md)).
