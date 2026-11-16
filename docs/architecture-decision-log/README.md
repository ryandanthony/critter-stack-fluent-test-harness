# ADR index: CritterStack.TestHarness

Architecture Decision Records for the harness. Each ADR captures one decision, its context, and its consequences.

Status legend: `proposed` (agreed direction, not yet implemented), `accepted` (locked in and reflected in the code), `superseded`.

The [design](../design/README.md) unifies these ADRs with the [PDRs](../product-decision-log/README.md).

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-0001](./ADR-0001-decision-logs-design-and-plan.md) | Decision logs, design docs and implementation plan | accepted |
| [ADR-0002](./ADR-0002-pinned-critter-stack-versions.md) | Pinned stack: .NET 10, Marten 9, Wolverine 6, Alba 8, xunit v3 | accepted |
| [ADR-0003](./ADR-0003-real-postgres-test-hosts.md) | Test against real Postgres with Testcontainers and per-host schemas | accepted |
| [ADR-0004](./ADR-0004-self-contained-harness-project.md) | The harness is one self-contained project with three dependencies | accepted |
| [ADR-0005](./ADR-0005-deferred-steps-and-staged-interfaces.md) | Deferred steps behind staged interfaces | accepted |
| [ADR-0006](./ADR-0006-appended-events-by-version-snapshot.md) | "Appended" is measured by a stream-version snapshot | accepted |
| [ADR-0007](./ADR-0007-projection-sync-before-actions.md) | Projection sync runs before actions, guarded by a dirty flag | accepted |
| [ADR-0008](./ADR-0008-exception-classification-by-origin.md) | Exceptions are classified by origin, not only by type | accepted |
| [ADR-0009](./ADR-0009-commands-tracked-queries-through-the-bus.md) | Commands run under Wolverine tracking, queries go straight through the bus | accepted |
| [ADR-0010](./ADR-0010-payloads-use-the-stores-serializer.md) | Failure payloads use the store's own serializer | accepted |
| [ADR-0011](./ADR-0011-build-quality-versioning-and-ci.md) | Build quality, versioning and CI | accepted |
| [ADR-0012](./ADR-0012-docs-and-portability-enforced-by-tests.md) | Docs and portability are enforced by tests | accepted |

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
