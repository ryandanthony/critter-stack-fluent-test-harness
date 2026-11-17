# How to use an ancillary store

Some services keep part of their data in an ancillary Marten store, registered with `AddMartenStore<TStore>()` next to the main `AddMarten()`. The harness can write Givens to such a store, assert on its streams and bring its async projections up to date. You don't need to change your host.

## Name the store for stream steps

Call `InStore<TStore>()` before the stream steps that belong to that store:

<!-- Test: AncillaryStoreHowToSamples.UseAnAncillaryStore_in_store -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .InStore<ILedgerStore>()
    .GivenStream<Ledger>(ledgerId, i => [new LedgerOpened(i, "acme")])
    .When(() => new PostEntry(ledgerId, 10m))
    .ThenAppended<EntryPosted>(ledgerId, e => e.Amount == 10m)
    .WhenQuery<LedgerBalance?>(() => new FindLedger(ledgerId))
    .ThenResult(b => b is { Balance: 10m });
```

`InStore` only moves the stream steps: `GivenStream`, `ThenStream*` and `ThenAppended*`. Commands, queries and HTTP requests go through Wolverine as usual. Your handlers decide which store they write to, whether with `[MartenStore(typeof(ILedgerStore))]` or by injecting the store.

Before the query, the harness brings the ledger store's async projections up to date as well as the main store's, so `LedgerBalance` isn't stale.

## Use both stores in one scenario

The store you name stays in effect across `When`, `WhenQuery` and `WhenHttp` until the next `InStore`. `InStore<IDocumentStore>()` switches back to the main store:

<!-- Test: AncillaryStoreHowToSamples.UseAnAncillaryStore_two_stores -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(widgetId, i => [new WidgetCreated(i, "Sprocket")])
    .InStore<ILedgerStore>()
    .GivenStream<Ledger>(ledgerId, i => [new LedgerOpened(i, "acme")])
    .When(() => new PostEntry(ledgerId, 10m))
    .ThenAppendedCount(ledgerId, 1)
    .InStore<IDocumentStore>()
    .ThenStreamEventCount(widgetId, 1);
```

A stream id names a stream within one store. The same id in two stores is two separate streams, and an appended-event assertion only counts events in its own store.

## Sync a store no step names

If a handler writes to the ancillary store and a query reads from it, but no stream step names it, the harness has no reason to sync it. Add it with `UseStore` instead:

<!-- Test: AncillaryStoreHowToSamples.UseAnAncillaryStore_use_store -->
```csharp
await TestHarness
    .Setup(fixture.Host, o => o.UseStore<ILedgerStore>())
    .When(() => new OpenLedger(ledgerId, "acme"))
    .WhenQuery<LedgerBalance?>(() => new FindLedger(ledgerId))
    .ThenResult(b => b is { Owner: "acme" });
```

Without `InStore` or `UseStore`, the query reads whatever the ledger store's projections had reached on their own.

## Projection sync per store

Every sync mode applies to each store in the scenario: the main store, then every store named by `InStore`, then every store added with `UseStore`.

- `CatchUp` uses the store's own daemon if the host runs one for it (`IntegrateWithWolverine()` or `AddAsyncDaemon(...)` on the store), otherwise a temporary daemon built from that store.
- `WaitForNonStale` needs a running daemon for every store that has events.
- A projection error in an ancillary store fails the next action with `projection sync (<mode>) of ILedgerStore failed with …`.

## When it fails

Step labels outside the main store end in ` in TStore`, and the events in the message come from that store:

```
step 3 ThenAppended<EntryPosted>(3f0c…) in ILedgerStore: no EntryPosted matching the predicate appended by step 2 When(PostEntry)
```

A store the host doesn't register fails at once, at the `InStore` or `Setup` line:

```
IUnregisteredStore is not registered in the host. Register it with AddMartenStore<IUnregisteredStore>() before using it in a scenario.
```

See [stores](../reference/api.md#stores) in the API reference and [`UseStore`](../reference/harness-options.md#usestoretstore) in the options reference.
