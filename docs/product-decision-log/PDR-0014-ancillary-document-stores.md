# PDR-0014: Scenarios can use ancillary document stores

- **Status:** accepted
- **Date:** 2026-11-17
- **Deciders:** Ryan
- **Supersedes:** the "ancillary document stores" bullet of [PDR-0008](./PDR-0008-v1-non-goals.md)

## Context

Marten services often split their data across a main store and one or more ancillary stores (`AddMartenStore<TStore>()`), each with its own schema, event store and projections. Wolverine routes a handler to one with `[MartenStore(typeof(TStore))]`. v1 always used the main `IDocumentStore`, so a test of such a service couldn't write Givens into an ancillary store, couldn't assert on its streams, and, worst of all, never brought its async projections up to date: a query against an ancillary read model saw stale data with no error.

A single business flow often touches more than one store, for example a command on the orders store that cascades into the main store. Restricting a scenario to one store would split such a flow across scenarios that can't share state.

## Decision

- A scenario starts on the **main store**. **`InStore<TStore>()`** switches the store that the following stream steps use (`GivenStream`, `ThenStream*`, `ThenAppended*`), until the next `InStore`. `InStore<IDocumentStore>()` switches back to the main store. It can be called straight after `Setup`, between Givens and between Thens, and the choice carries across `When`, `WhenQuery` and `WhenHttp`.
- **Projection sync covers every store the scenario uses**, before each action as today ([PDR-0003](./PDR-0003-projections-current-before-every-action.md)): the main store and every store named by `InStore`.
- **`HarnessOptions.UseStore<TStore>()`** adds a store to the sync set even when no step names it, for a store that only a handler writes and only a query reads.
- **Commands, queries and requests don't pick a store.** Wolverine's own `[MartenStore]` routing decides which store a handler uses, as in production.
- A `TStore` that isn't registered fails with `InvalidOperationException` at the `InStore` or `Setup` call, naming the type and `AddMartenStore<TStore>()`.
- A step label names the store when it isn't the main one: `GivenStream<Ledger>(…) in ILedgerStore`. Failure payloads use that store's serializer, and stream ids are checked against that store's `StreamIdentity` ([PDR-0013](./PDR-0013-string-and-strongly-typed-stream-ids.md)).
- `ForTenant` applies to every store.
- The main-store API and behaviour don't change. This is a minor release.

## Alternatives considered

- **One store per scenario, chosen in options.** Rejected: a flow that writes to two stores couldn't be tested as one scenario.
- **A store argument on every stream method** (`GivenStream<TStore, TAggregate>(...)`). Rejected: it would triple the stream overloads again after PDR-0013, and most scenarios use one store for most steps.
- **Sync only the stores named by `InStore`.** Rejected as the only rule: a store written only by a handler would stay stale. `UseStore` covers it.

## Consequences

- Design in [ADR-0020](../architecture-decision-log/ADR-0020-ancillary-stores-per-step-and-synced-together.md).
- Each action can now trigger one catch-up per store the scenario uses.
- `[MartenStore]` handlers need nothing from the harness.

## Non-goals

- Stores that aren't registered in the host's service provider.
- Different tenants per store in one scenario ([PDR-0007](./PDR-0007-one-tenant-per-scenario.md)).
- Syncing stores the scenario doesn't use or name.
