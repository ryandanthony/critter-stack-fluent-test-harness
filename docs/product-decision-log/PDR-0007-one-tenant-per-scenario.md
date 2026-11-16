# PDR-0007: One tenant per scenario

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Multi-tenancy), [explanation/design-decisions.md](../explanation/design-decisions.md) (One tenant per scenario)

## Context

Services with conjoined Marten tenancy need tests that run as a tenant and prove tenants can't see each other's data. Letting the tenant change between steps would make it unclear which tenant each assertion reads from.

## Decision

- `ForTenant(tenantId)` is available **only straight after `Setup`**, once, before any Given.
- When set, **every step uses it**: Givens and Then reads open tenant sessions, `When` and `WhenQuery` invoke for that tenant, and the handler's session sees it.
- Without `ForTenant`, the scenario uses Marten's default tenant.
- To compare tenants, write **one scenario per tenant** (for example, Given under `a`, then a separate scenario under `b` asserting `ThenStreamEventCount(id, 0)`).

## Alternatives considered

- **Per-step tenant.** Rejected: ambiguous assertions.

## Consequences

- One code path for tenant and default invokes, differing only in the call ([ADR-0009](../architecture-decision-log/ADR-0009-commands-tracked-queries-through-the-bus.md)).
- The rule is enforced by the stage interfaces ([ADR-0005](../architecture-decision-log/ADR-0005-deferred-steps-and-staged-interfaces.md)).

## Non-goals

- Several tenants in one scenario ([PDR-0008](./PDR-0008-v1-non-goals.md)).
