# ADR-0004: The harness is one self-contained project with three dependencies

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Portability rule, Files: harness library, Risks: verified portability), commit `1e46caa`

## Context

Services adopt the harness by copying the project ([PDR-0006](../product-decision-log/PDR-0006-adopt-by-copying-the-project.md)). The first real copy failed twice: the csproj got its `TargetFramework` from this repo's `Directory.Build.props`, and the public `ScenarioAssertionException` clashed with `Alba.ScenarioAssertionException` (CS0104) in every file that imported both namespaces.

## Decision

- `CritterStack.TestHarness.csproj` references **Alba, Marten and WolverineFx only**, with no `ProjectReference` and no custom imports.
- It sets **`TargetFramework`, `Nullable` and `ImplicitUsings` itself**. Analyzers, StyleCop and GitVersion come from `src/Directory.Build.props` and stay behind when the project is copied, along with the private feed.
- The harness knows **nothing about any service**: no aggregates, projections, schemas, fixtures or transports. Everything that varies is a `HarnessOptions` field (`ProjectionSync`, `ProjectionTimeout`, `TrackingTimeout`, `IncludeExternalMessages`).
- It always uses the host's **main `IDocumentStore`** and **Guid stream ids** ([PDR-0008](../product-decision-log/PDR-0008-v1-non-goals.md)).
- Public names **must not collide** with Alba, Marten or Wolverine types that consumers import alongside it.
- Public API in `CritterStack.TestHarness` (one public type per file, stages under `Stages/`); everything else is `internal` in `CritterStack.TestHarness.Internal`, visible to the test project only.

## Consequences

- The rule is proven on every push by `just portability` ([ADR-0012](./ADR-0012-docs-and-portability-enforced-by-tests.md)).
- `Directory.Build.props`, `Directory.Build.targets` and `stylecop.json` must stay together in `src/`; the `.targets` keeps only the repo's `stylecop.json` by folder.
