# PDR index: CritterStack.TestHarness

Product Decision Records for the harness. The "product" is the experience of a test author on a Marten/Wolverine service: what they can write, what they see when it fails, and what the harness deliberately does not do. Each PDR captures the need, what was decided, what was considered instead, and the non-goals.

Status legend: `proposed` (drafted, awaiting Ryan's review), `accepted` (agreed direction; it may not be built yet, see the [implementation plan](../implementation-plan/00-overview.md)), `superseded`.

`**Open:**` items inside an accepted PDR are explicitly undecided and must be resolved (by amending the PDR before its packet starts, or with a new PDR) before that part is built.

The [design](../design/README.md) unifies these PDRs with the [ADRs](../architecture-decision-log/README.md).

| PDR | Title | Status |
|-----|-------|--------|
| [PDR-0001](./PDR-0001-fluent-scenarios-on-the-services-own-host.md) | Fluent Given/When/Then scenarios on the service's own test host | accepted |
| [PDR-0002](./PDR-0002-appended-vs-whole-stream-assertions.md) | Assert what the command did, separately from what the stream holds | accepted |
| [PDR-0003](./PDR-0003-projections-current-before-every-action.md) | Async projections are current before every command and query | accepted |
| [PDR-0004](./PDR-0004-handler-exceptions-are-assertable.md) | Handler exceptions are assertable, but setup problems fail at once | accepted |
| [PDR-0005](./PDR-0005-failures-show-the-actual-data.md) | Failures show the actual data | accepted |
| [PDR-0006](./PDR-0006-adopt-by-copying-the-project.md) | Services adopt the harness by copying the project | accepted |
| [PDR-0007](./PDR-0007-one-tenant-per-scenario.md) | One tenant per scenario | accepted |
| [PDR-0008](./PDR-0008-v1-non-goals.md) | v1 non-goals | accepted |
| [PDR-0009](./PDR-0009-diataxis-docs-with-runnable-samples.md) | User docs follow Diátaxis, and every sample runs | accepted |

## Template

```markdown
# PDR-NNNN: <title>

- **Status:** proposed | accepted | superseded by [PDR-NNNN](./PDR-NNNN-slug.md)
- **Date:** YYYY-MM-DD
- **Deciders:** Ryan
- **Source:** (optional) where this was mined from

## Context

The test author's need or problem. Who hits it and when.

## Decision

What the harness does. Concrete, observable behaviour.

## Alternatives considered

What else was on the table and why it lost.

## Consequences

What this enables, what it costs, which ADRs or design docs it drives.

## Non-goals

What this decision deliberately does not cover.
```
