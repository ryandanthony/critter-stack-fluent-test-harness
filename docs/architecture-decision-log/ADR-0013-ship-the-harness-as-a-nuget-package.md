# ADR-0013: Ship the harness as a NuGet package

- **Status:** accepted (**Open** item resolved 2026-11-16: Ryan chose a public repo and nuget.org. The repo was renamed to `ryandanthony/critter-stack-fluent-test-harness`, and the trusted-publishing policy uses that name. The manual-only `release` bullet is superseded by [ADR-0017](./ADR-0017-release-automatically-on-push-to-main.md): releases now run automatically on push to `main`)
- **Date:** 2026-11-16
- **Deciders:** Ryan
- **Supersedes:** [ADR-0004](./ADR-0004-self-contained-harness-project.md); the portability-check bullet of [ADR-0012](./ADR-0012-docs-and-portability-enforced-by-tests.md); the "no packing, tagging or publishing" bullet of [ADR-0011](./ADR-0011-build-quality-versioning-and-ci.md)

## Context

[PDR-0010](../product-decision-log/PDR-0010-adopt-as-a-nuget-package.md) replaces copying the project with a package. [ADR-0004](./ADR-0004-self-contained-harness-project.md) shaped the project for copying (self-contained csproj, copy check in CI). Most of its rules still matter for a package, for a different reason: a package's dependencies and public names become every consumer's.

## Decision

**Package shape** (`src/CritterStack.TestHarness/CritterStack.TestHarness.csproj`):

- Package id **`CritterStack.TestHarness`**, `net10.0`, MIT, README, XML docs, `snupkg` symbols. Built with this repo's `Directory.Build.props` like any other project; analyzers and GitVersion are `PrivateAssets="all"`, so the nuspec depends on **Alba, Marten and WolverineFx only** (minimum versions from `src/Directory.Packages.props`, [ADR-0002](./ADR-0002-pinned-critter-stack-versions.md)).
- Kept from ADR-0004: the harness knows **nothing about any service**; everything that varies is a `HarnessOptions` field; it uses the host's **main `IDocumentStore`** and **Guid stream ids**; public names **must not collide** with Alba, Marten or Wolverine types consumers import alongside it; public API in `CritterStack.TestHarness`, everything else `internal` in `CritterStack.TestHarness.Internal`.
- Dropped from ADR-0004: the csproj no longer needs to build outside this repo, and consumers no longer supply their own Alba/Marten/WolverineFx versions for it (they may still pin higher ones).
- CI builds set `ContinuousIntegrationBuild`, so the package is deterministic and its source links point at the commit.

**Proof** (replaces the copy check from ADR-0012):

- `just pack` writes the `.nupkg` and `.snupkg` to `artifacts/`.
- `just package-check` (`scripts/check-package.sh`, run in CI) restores a throwaway consumer (`samples/ConsumerService`) that has no repo build props, a nuget.org-only feed plus `artifacts/` mapped for `CritterStack.TestHarness`, and an isolated package cache, then runs its scenarios against the packed package.

**Release** (`.github/workflows/build.yml`):

- Every run packs and uploads the packages as a workflow artifact.
- A **`release` job** publishes to **nuget.org** with trusted publishing (GitHub OIDC through `NuGet/login`, no stored API key), tags `v{MajorMinorPatch}` and creates a GitHub release, the same way awen does. It runs only on a manual `workflow_dispatch` with `publish: true` on `main`, so a merge never publishes by accident and nothing runs before the nuget.org trusted-publishing policy exists.
- nuget.org over GitHub Packages because GitHub Packages needs a token even to read ([ADR-0011](./ADR-0011-build-quality-versioning-and-ci.md)), which every consumer's CI would then need.

**Open:** the GitHub repo is private. A public nuget.org package with `RepositoryUrl` and source link pointing at a private repo gives consumers dead links and unusable symbols. Before the first publish: make the repo public, or publish to the festina-lente-io GitHub Packages feed instead and accept the read token.

## Consequences

- One reviewed artifact per version. Upgrades are a version bump in the consumer.
- A published version can't be changed, only unlisted. Behaviour changes need care with SemVer.
- Doc samples still run against the project, not the package. The package check covers the packaging seam.
