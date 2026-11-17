# ADR-0017: Every push to main that ships a change releases automatically

- **Status:** accepted
- **Date:** 2026-11-17
- **Deciders:** Ryan
- **Supersedes:** the manual-only `release` bullet of [ADR-0013](./ADR-0013-ship-the-harness-as-a-nuget-package.md); the GitVersion "mainline mode" detail of [ADR-0011](./ADR-0011-build-quality-versioning-and-ci.md)

## Context

[ADR-0013](./ADR-0013-ship-the-harness-as-a-nuget-package.md) released only on a manual `workflow_dispatch` with `publish: true`, because the trusted-publishing policy didn't exist yet and the repo was private. v1.3.3 shipped that way on 2026-11-17. Now that the policy exists and the repo is public, Ryan wants the build and the version to happen on every commit to `main`, with nothing to click.

Two things made "publish every push" a bad fit as configured:

- GitVersion's Mainline strategy with `ContinuousDeployment` on `main` gave every commit its own patch number, including `docs:` and `chore:` commits (tested: `docs, chore, fix, docs` after v1.3.3 gave 1.3.5 to 1.3.8). Publishing each one would fill nuget.org with versions that change nothing.
- A nuget.org version can't be replaced, only unlisted, so a release has to be deliberate about what it contains.

## Decision

- **GitVersion** (`GitVersion.yml`): `main` uses `ContinuousDelivery` and the Mainline strategy is dropped (`TaggedCommit` only). Every commit since the last `v` tag then shares the next version, so `docs:` commits no longer burn numbers. The bump rules don't change: `type!:` is major, `feat:` is minor, and anything else is patch. A `BREAKING CHANGE:` footer without `!` is **not** detected by GitVersion 6.8 (tested), so breaking commits must use `!`.
- **Release on push** (`.github/workflows/build.yml`, `release` job): runs on every push to `main` after the `Build and test` job (build, tests, package check, docs check) passes.
  - `scripts/release-plan.sh` decides. It releases when any commit since the last `v` tag is `feat`, `fix` or `perf`, or has `!`. `docs`, `chore`, `ci`, `test`, `refactor`, `style` and `build` commits alone don't release, and their changes ship with the next release. A commit that is already tagged never releases twice.
  - The version is the pinned `dotnet-gitversion` tool (`dotnet-tools.json`, the same 6.8.2 as `GitVersion.MsBuild`). The job tags the commit locally, rebuilds and packs so the package gets the plain version (no `-N` suffix), checks that the `.nupkg` name matches, pushes to nuget.org with trusted publishing and `--skip-duplicate`, then pushes the tag and creates the GitHub release.
  - Only the newest `main` commit releases: if `main` moved on during the build, the job skips and the newer run releases everything. Releases are serialized (`concurrency: release`, never cancelled).
  - `workflow_dispatch` with `force: true` releases even without a releasable commit, for example to ship a README-only change to the nuget.org page.
- **Required check:** the build job is named `Build and test` (the docs job is `Docs site`), so branch protection can require it unambiguously.

## Consequences

- Merging a `feat:` or `fix:` to `main` puts a new version on nuget.org within minutes, with no manual step. Commit types now decide releases, not only version numbers, so a mislabelled commit either ships when it shouldn't or doesn't ship when it should.
- Untagged builds on `main` are versioned `X.Y.Z-N`, a prerelease of the next release. Their CI artifacts are never published.
- The tag is pushed only after nuget.org accepts the package. If the job fails after the push, re-running it is safe: `--skip-duplicate` skips the existing version and the tag and release are created then.
