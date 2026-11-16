# ADR-0011: Build quality, versioning and CI

- **Status:** accepted (the no-publishing bullet is superseded by [ADR-0013](./ADR-0013-ship-the-harness-as-a-nuget-package.md))
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Files: `nuget.config`, `Directory.Build.props`, `GitVersion.yml`, `justfile`, `build.yml`), conventions shared with `awen` and `mise`

## Context

The harness should hold the same quality bar as Ryan's other .NET repos, and a reader should be able to trust that a green build means the documented behaviour still holds.

## Decision

- **Warnings as errors**, `FestinaLente.CodeStandards` (StyleCop and others) and `GenerateDocumentationFile` for non-test projects, so a missing XML doc comment fails the build. `src/stylecop.json` and `src/Directory.Build.targets` are copied from awen. Rules are fixed in code; a suppression needs a one-line reason in `.editorconfig`.
- **`nuget.config`** clears default sources, adds nuget.org and the festina-lente-io GitHub feed, and maps `FestinaLente.*` to the feed. Restore needs a token with `read:packages`; CI uses `GITHUB_TOKEN` with `packages: read`.
- **GitVersion** (`GitVersion.yml` from awen, `GitVersion.MsBuild`): mainline mode, `v` tag prefix, Conventional Commit bumps (`type!:` major, `feat:` minor, `fix:` patch). The informational version is `{SemVer}+{ShortSha}`; no version is hand-maintained.
- **`justfile`** is the entry point: `build`, `test`, `portability`, plus `decisions` and `docs-check` ([ADR-0001](./ADR-0001-decision-logs-design-and-plan.md)).
- **`.github/workflows/build.yml`** on pull requests and pushes to `main`: checkout with full history, .NET from `global.json`, NuGet cache, feed credentials, `just build`, version to the job summary, `just test`, `just portability`, `just docs-check`, and upload `*.trx` always.
- No packing, tagging or publishing in v1 ([PDR-0006](../product-decision-log/PDR-0006-adopt-by-copying-the-project.md)).

## Consequences

- Commits must follow Conventional Commits for versions to be right.
- A fork without access to the festina-lente-io feed can't restore.
