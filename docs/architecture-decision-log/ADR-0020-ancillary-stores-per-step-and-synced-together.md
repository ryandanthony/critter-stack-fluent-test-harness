# ADR-0020: Ancillary stores are chosen per step and synced together

- **Status:** accepted
- **Date:** 2026-11-17
- **Deciders:** Ryan
- **Source:** runtime probe on a host with a main store and `AddMartenStore<IOrderStore>()`, Marten 9.44.0 / WolverineFx 6.44.0 and Marten 9.62.0 / WolverineFx 6.64.2, 2026-11-17

## Context

[PDR-0014](../product-decision-log/PDR-0014-ancillary-document-stores.md) lets a scenario use several stores. Verified on the floor and on the latest releases, with identical results:

| Case | What happens |
|---|---|
| `GetRequiredService<IOrderStore>()` | a `DocumentStore`, not the main `IDocumentStore` instance |
| `[MartenStore(typeof(IOrderStore))]` command under `TrackActivity().ExecuteAndWaitAsync`, with and without `InvokeForTenantAsync` | handled against the ancillary store; events land in its schema for the right tenant |
| Main-store sync on a daemon host (`ForceAllMartenDaemonActivityToCatchUpAsync`) | doesn't advance the ancillary store's projections (seen stale) |
| `IProjectionCoordinator<IOrderStore>` | registered when the store is `IntegrateWithWolverine()`d or has `AddAsyncDaemon`, even if the main store has no daemon; missing for a plain `AddMartenStore` |
| `ForceAllMartenDaemonActivityToCatchUpAsync<IOrderStore>` with a coordinator | catches the store up; a failing apply comes back in the returned list |
| the same without a coordinator | throws `InvalidOperationException` (no `IProjectionCoordinator<IOrderStore>`) |
| `ancillaryStore.BuildProjectionDaemonAsync()` + `CatchUpAsync` | catches up and throws the apply error, as for the main store |
| `WaitForNonStaleProjectionDataAsync<IOrderStore>` | waits on the ancillary store |

So every rule [ADR-0007](./ADR-0007-projection-sync-before-actions.md) applies to the main store holds per store, keyed on `IProjectionCoordinator<TStore>` instead of `IProjectionCoordinator`.

## Decision

- **`StoreRef`** names a store: the main store, or an ancillary `TStore`. It is built inside the generic `InStore<TStore>()` / `UseStore<TStore>()` and holds typed delegates: resolve the store, check for `IProjectionCoordinator<TStore>`, `ForceAll…<TStore>`, `WaitForNonStale…<TStore>`. The runner never uses reflection over generic methods. Two refs to the same `TStore` are equal.
- **The builder tracks a current store.** It starts on the main store; `InStore<TStore>()` changes it (`IDocumentStore` means main). Each stream step records the store that was current when it was added. Stores are resolved when `InStore`/`UseStore` is called, so an unregistered `TStore` throws there.
- **Stream state is keyed by `(StoreRef, StreamKey)`:** started streams, snapshots and snapshot planning. The same id in two stores is two streams.
- **Per-store execution:** sessions are opened on the step's store, for the scenario's tenant. The id-kind check ([ADR-0019](./ADR-0019-one-stream-key-for-guid-string-and-typed-ids.md)) uses that store's `StreamIdentity`, and event payloads use that store's serializer ([ADR-0010](./ADR-0010-payloads-use-the-stores-serializer.md)).
- **Sync:** the scenario's sync set is the main store, plus every store named by `InStore`, plus every store added by `UseStore`. One dirty flag covers all of them, because a command may write to any store. On each sync, stores are caught up in that order, each with ADR-0007's rule decided for that store: the typed `ForceAll` when it has a coordinator, otherwise a temporary daemon from that store. `WaitForNonStale` uses the typed form. The first failure fails the action, naming the store.
- **Shard watcher:** one `ShardFailureWatcher` ([ADR-0016](./ADR-0016-shard-watcher-catches-missed-apply-errors.md)) per store in the sync set that has a coordinator.
- **Labels** append ` in TStore` for a step on an ancillary store. Main-store labels are unchanged.
- New interface members are a minor change (as in ADR-0019).

## Consequences

- One action can catch up several stores, which costs one catch-up per store.
- Tests: ancillary `ILedgerStore` stores on the existing hosts, each in its own schema ([ADR-0003](./ADR-0003-real-postgres-test-hosts.md)). A plain store on the no-daemon host covers the temporary-daemon path. A store integrated with Wolverine, with a daemon, on the daemon host covers the typed `ForceAll` path and the watcher. A failing projection in an ancillary store on the poison host covers apply errors.
- The harness now relies on `ForceAllMartenDaemonActivityToCatchUpAsync<T>`, `WaitForNonStaleProjectionDataAsync<T>` and `IProjectionCoordinator<T>`, which are public at the floor ([ADR-0018](./ADR-0018-critter-stack-floor-and-latest.md)).
