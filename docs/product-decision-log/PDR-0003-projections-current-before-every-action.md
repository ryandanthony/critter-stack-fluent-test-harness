# PDR-0003: Async projections are current before every command and query

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Setup options, Projection sync), [explanation/projection-sync.md](../explanation/projection-sync.md)

## Context

Marten's async projections update read models in the background. In a test that is a race: write events, query a read model, and the projection may or may not have run. Hand-written tests paper over it with delays or service-specific waits. Some command handlers also read a projection before acting, so the race isn't limited to queries. Test hosts differ too: some skip the async daemon to start faster, others run it like production.

## Decision

- Before each `When` and each `WhenQuery`, the harness brings async projections up to date, **if anything was written since the last sync**.
- The default mode, `CatchUp`, works on **any host**, with or without a daemon, with no settings to change.
- `WaitForNonStale` is available for hosts that run the daemon and want it exercised the way production does. `None` is for services whose projections are all inline.
- There is no sync before a Then or after the last step. To check a read model after a command, follow the `When` with a `WhenQuery`.
- A sync that fails or exceeds `ProjectionTimeout` fails the scenario at once ([PDR-0004](./PDR-0004-handler-exceptions-are-assertable.md)).

## Alternatives considered

- **Sync after every step.** Rejected: slow, and stream and message assertions never read projections.
- **Leave sync to the test author.** Rejected: it is the most common source of flaky Critter Stack tests.

## Consequences

- Drives the two-path `CatchUp` and the dirty flag ([ADR-0007](../architecture-decision-log/ADR-0007-projection-sync-before-actions.md)).

## Non-goals

- Projections in ancillary document stores ([PDR-0008](./PDR-0008-v1-non-goals.md)).
