# ADR-0016: A shard watcher catches apply errors the forced catch-up misses

- **Status:** accepted (since [ADR-0020](./ADR-0020-ancillary-stores-per-step-and-synced-together.md), one watcher per store the scenario syncs that has a coordinator)
- **Date:** 2026-11-16
- **Deciders:** Ryan
- **Source:** flaky `ProjectionErrorTests.CatchUp_projection_error_fails_the_next_action_with_the_apply_error` (failed CI on `main` at f15852c and 5bc5eb7, and 4 of 8 runs on 1 CPU as far back as 01a7e35)

## Context

[ADR-0007](./ADR-0007-projection-sync-before-actions.md) treats a non-empty list from `ForceAllMartenDaemonActivityToCatchUpAsync` as a projection failure on daemon hosts. That list only holds errors raised *during* the forced catch-up. Marten 9.44 runs it as "stop every agent, catch up, restart the agents", and the daemon also runs on its own between scenarios. When the daemon's background run reaches a bad event first (before the catch-up, or straight after the restart), the shard pauses with the error outside the catch-up. The catch-up then returns an empty list and the scenario carries on as if the projection were current. This was timing dependent and showed up mostly on slow or single-core runners such as CI.

Reading the coordinator's agents after the catch-up doesn't help. The restart is asynchronous, so straight after the call there are often no agents yet, and the paused agent shows up some time later.

## Decision

- On a daemon host with `ProjectionSync.CatchUp`, each scenario subscribes a `ShardFailureWatcher` to the `ShardStateTracker` of every database in the main store. It does this before the first step and disposes it when the scenario ends.
- The watcher records each shard state published with `Paused` or `Stopped` and an exception. Only states published while the scenario runs count, so a failure left over from an earlier scenario on the same host isn't blamed on this one.
- If the forced catch-up returns no errors, the harness asks the watcher. While no failure has been seen and some shard is still behind the tracker's high water mark, it waits up to 500 ms for a shard that is about to pause to do so. Any failures recorded are then raised in the same `AggregateException` as catch-up errors, so they stay infrastructure failures as before.
- The no-daemon path is unchanged. Its temporary daemon reports every apply error from `CatchUpAsync`.

## Consequences

- An apply error fails the next action however the daemon's timing falls. The flaky test passed 10 full-suite runs and 20 isolated runs on 1 CPU.
- A healthy scenario on a daemon host waits only while a shard is still behind the high water mark, and never longer than 500 ms. Once shards are caught up there is no wait at all.
- The harness now relies on `IMartenDatabase.Tracker` and the `ShardState.Action` and `Exception` members, which are public in Marten 9.44 and JasperFx.Events 2.96. They join the pinned surface in [ADR-0002](./ADR-0002-pinned-critter-stack-versions.md).
