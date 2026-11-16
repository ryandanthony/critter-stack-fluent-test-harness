# ADR-0007: Projection sync runs before actions, guarded by a dirty flag

- **Status:** accepted (daemon path extended by [ADR-0016](./ADR-0016-shard-watcher-catches-missed-apply-errors.md): a shard watcher also catches apply errors the forced catch-up doesn't return)
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Setup: `ProjectionSync`, Projection sync), [explanation/projection-sync.md](../explanation/projection-sync.md)

## Context

[PDR-0003](../product-decision-log/PDR-0003-projections-current-before-every-action.md) needs projections current before each `When` and `WhenQuery` on any host. Marten's two "force catch-up" APIs each only work on one kind of host (verified on 9.44):

- `ForceAllMartenDaemonActivityToCatchUpAsync` needs the daemon's `IProjectionCoordinator` and throws `InvalidOperationException` without it.
- `BuildProjectionDaemonAsync` + `CatchUpAsync` builds a temporary daemon, and on a host that already runs one fails with `ProgressionProgressOutOfOrderException`.

## Decision

- `ProjectionSync.CatchUp` (default) **picks the path at run time**: if the host has an `IProjectionCoordinator`, call `host.ForceAllMartenDaemonActivityToCatchUpAsync(ct)` and treat a non-empty returned exception list as a failure. Otherwise build a daemon, `CatchUpAsync(ProjectionTimeout, ct)`, and dispose it.
- `ProjectionSync.WaitForNonStale` calls `host.WaitForNonStaleProjectionDataAsync(ProjectionTimeout)`. It always times out without a daemon.
- `ProjectionSync.None` does nothing.
- `ScenarioContext` keeps a **dirty flag**, set by every Given and `When`. Sync runs before a `When` or `WhenQuery` only when it is set, then clears it.
- Any sync error or timeout is an **infrastructure failure**.
- An internal hook counts sync calls so tests can prove "`WhenQuery` then `WhenQuery` syncs once".

## Consequences

- No settings change between daemon and no-daemon hosts.
- Both paths, and each mode's failure, are covered by tests on separate hosts ([ADR-0003](./ADR-0003-real-postgres-test-hosts.md)).
