# ADR index: CritterStack.TestHarness

Architecture Decision Records for the harness. Each ADR captures one decision, its context, and its consequences.

Status legend: `proposed` (agreed direction, not yet implemented), `accepted` (locked in and reflected in the code), `superseded`.

The [design](../design/README.md) unifies these ADRs with the [PDRs](../product-decision-log/README.md).

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-0001](./ADR-0001-decision-logs-design-and-plan.md) | Decision logs, design docs and implementation plan | accepted |
| [ADR-0002](./ADR-0002-pinned-critter-stack-versions.md) | Pinned stack: .NET 10, Marten 9, Wolverine 6, Alba 8, xunit v3 | accepted (Critter Stack pins superseded by ADR-0018) |
| [ADR-0003](./ADR-0003-real-postgres-test-hosts.md) | Test against real Postgres with Testcontainers and per-host schemas | accepted (extended by ADR-0019, ADR-0020) |
| [ADR-0004](./ADR-0004-self-contained-harness-project.md) | The harness is one self-contained project with three dependencies | superseded by [ADR-0013](./ADR-0013-ship-the-harness-as-a-nuget-package.md) |
| [ADR-0005](./ADR-0005-deferred-steps-and-staged-interfaces.md) | Deferred steps behind staged interfaces | accepted |
| [ADR-0006](./ADR-0006-appended-events-by-version-snapshot.md) | "Appended" is measured by a stream-version snapshot | accepted |
| [ADR-0007](./ADR-0007-projection-sync-before-actions.md) | Projection sync runs before actions, guarded by a dirty flag | accepted (extended by ADR-0016, ADR-0020) |
| [ADR-0008](./ADR-0008-exception-classification-by-origin.md) | Exceptions are classified by origin, not only by type | accepted |
| [ADR-0009](./ADR-0009-commands-tracked-queries-through-the-bus.md) | Commands run under Wolverine tracking, queries go straight through the bus | accepted (extended by ADR-0014) |
| [ADR-0010](./ADR-0010-payloads-use-the-stores-serializer.md) | Failure payloads use the store's own serializer | accepted (per store since ADR-0020) |
| [ADR-0011](./ADR-0011-build-quality-versioning-and-ci.md) | Build quality, versioning and CI | accepted (no-publishing bullet superseded by ADR-0013, mainline mode by ADR-0017) |
| [ADR-0012](./ADR-0012-docs-and-portability-enforced-by-tests.md) | Docs and portability are enforced by tests | accepted (portability bullet superseded by ADR-0013) |
| [ADR-0013](./ADR-0013-ship-the-harness-as-a-nuget-package.md) | Ship the harness as a NuGet package | accepted (manual-release bullet superseded by ADR-0017) |
| [ADR-0014](./ADR-0014-whenhttp-through-alba-under-tracking.md) | WhenHttp runs the Alba scenario under Wolverine tracking | accepted |
| [ADR-0015](./ADR-0015-docusaurus-site-on-github-pages.md) | Docusaurus site over docs/, deployed to GitHub Pages | accepted |
| [ADR-0016](./ADR-0016-shard-watcher-catches-missed-apply-errors.md) | A shard watcher catches apply errors the forced catch-up misses | accepted (per store since ADR-0020) |
| [ADR-0017](./ADR-0017-release-automatically-on-push-to-main.md) | Every push to main that ships a change releases automatically | accepted |
| [ADR-0018](./ADR-0018-critter-stack-floor-and-latest.md) | Critter Stack floor versions, tested on the floor and on the latest releases | accepted |
| [ADR-0019](./ADR-0019-one-stream-key-for-guid-string-and-typed-ids.md) | One stream key for Guid, string and strongly typed ids | accepted (keyed per store since ADR-0020) |
| [ADR-0020](./ADR-0020-ancillary-stores-per-step-and-synced-together.md) | Ancillary stores are chosen per step and synced together | accepted |

## Template

```markdown
# ADR-NNNN: <title>

- **Status:** proposed | accepted | superseded by [ADR-NNNN](./ADR-NNNN-slug.md)
- **Date:** YYYY-MM-DD
- **Deciders:** Ryan
- **Source:** (optional) where this was mined from

## Context

The forces at play: requirements (cite PDRs), constraints, what hurts today.

## Decision

What we will do. Specific enough to check code against.

## Consequences

What becomes easier, what becomes harder, what is now off the table.
```
