# AGENTS.md

## Overview

CritterStack.TestHarness is a fluent Given/When/Then test harness for Marten and Wolverine services. It plugs into a service's existing Alba test host. C# / .NET 10, Marten 9.44, WolverineFx 6.44, Alba 8.5.3, xunit v3, Postgres via Testcontainers.

v1 is built and green in CI. Services adopt it as the `CritterStack.TestHarness` NuGet package ([PDR-0010](docs/product-decision-log/PDR-0010-adopt-as-a-nuget-package.md), [ADR-0013](docs/architecture-decision-log/ADR-0013-ship-the-harness-as-a-nuget-package.md)).

## Read first

| Need | Read |
|---|---|
| What the harness does, and doesn't | [docs/product-decision-log/](docs/product-decision-log/README.md) (`PDR-NNNN`) |
| How it is built, and why | [docs/architecture-decision-log/](docs/architecture-decision-log/README.md) (`ADR-NNNN`) |
| How the parts fit together | [docs/design/](docs/design/README.md) |
| What to build next | [docs/implementation-plan/00-overview.md](docs/implementation-plan/00-overview.md) |
| How users see it (tutorial, how-to, reference, explanation) | [docs/index.md](docs/index.md) |

Records are the contract. If your change needs a decision that isn't on record, or contradicts one, stop and write (or ask for) a new PDR/ADR first. Do not edit an `accepted` record; supersede it.

## Quick build & test

```bash
just build          # Release build, warnings as errors
just test           # Full suite (needs Docker)
just pack           # CritterStack.TestHarness .nupkg + .snupkg into artifacts/
just package-check  # Pack, install into a throwaway consumer service, run its scenarios (needs Docker)
just decisions      # PDR/ADR list with statuses
just docs-check     # Verify relative links in docs/
just docs-site      # Build the Docusaurus site from docs/ (needs Node 20)
```

Restore needs a GitHub token with `read:packages` stored for the `github` NuGet source ([ADR-0011](docs/architecture-decision-log/ADR-0011-build-quality-versioning-and-ci.md)).

## Layout

```
docs/                                 user docs (Diátaxis) + decision logs, design, implementation plan
website/                              Docusaurus site that renders docs/ (no Markdown lives here)
src/
  CritterStack.TestHarness/           the harness: Alba, Marten, WolverineFx only; self-contained csproj
    Stages/                           public stage interfaces
    Internal/                         builder, runner, context, sync, classifier, formatter, Steps/
  CritterStack.TestHarness.Tests/     tests, TestDomain/ (Widget), Infrastructure/ (hosts), DocSamples/
samples/ConsumerService/              sample consumer used by scripts/check-package.sh
```

## Conventions

- **Package:** the harness depends on Alba, Marten and WolverineFx only; build tooling stays `PrivateAssets="all"`. No service-specific knowledge; anything that varies is a `HarnessOptions` field. Public API changes follow SemVer ([ADR-0013](docs/architecture-decision-log/ADR-0013-ship-the-harness-as-a-nuget-package.md)). Publishing is a manual `workflow_dispatch` with `publish: true`.
- **Packages:** central package management, exact pins in `src/Directory.Packages.props` ([ADR-0002](docs/architecture-decision-log/ADR-0002-pinned-critter-stack-versions.md)). Bump `samples/ConsumerService/Directory.Packages.props.template` with it.
- **Quality:** warnings as errors, `FestinaLente.CodeStandards` + StyleCop, file headers on every `.cs`, XML docs on every public member ([ADR-0011](docs/architecture-decision-log/ADR-0011-build-quality-versioning-and-ci.md)).
- **Tests:** real Postgres, one schema per host, every test has a `business_value` trait ([ADR-0003](docs/architecture-decision-log/ADR-0003-real-postgres-test-hosts.md)).
- **Docs:** a public API change updates `docs/reference/` and any affected tutorial/how-to sample and its `DocSamples/` test in the same PR ([ADR-0012](docs/architecture-decision-log/ADR-0012-docs-and-portability-enforced-by-tests.md)).
- **Commits:** Conventional Commits; GitVersion derives the version from them.
