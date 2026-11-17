# API reference

Every public type and method in `CritterStack.TestHarness`, in the order a scenario uses them. Each method lists the stage it's on, the stage it returns, what it does, and how it fails. The XML doc comments describe the same behaviour, so IntelliSense and this page agree.

## Stages

| From | Method | To |
|---|---|---|
| (none) | `TestHarness.Setup(host, configure?)` | `IScenarioStart` |
| `IScenarioStart` | `ForTenant(tenantId)` | `IGivenStage` |
| `IScenarioStart`, `IGivenStage` | `GivenStream(id, events)`, `GivenStream<TAggregate>(id, events)` | `IGivenStage` |
| `IScenarioStart`, `IGivenStage`, `ICommandStage`, `IQueryStage<T>`, `IHttpStage` | `When(command)` | `ICommandStage` |
| `IScenarioStart`, `IGivenStage`, `ICommandStage`, `IQueryStage<T>`, `IHttpStage` | `WhenQuery<TResponse>(query)` | `IQueryStage<TResponse>` |
| `IScenarioStart`, `IGivenStage`, `ICommandStage`, `IQueryStage<T>`, `IHttpStage` | `WhenHttp(request)` | `IHttpStage` |
| `IScenarioStart`, `IGivenStage` | `InStore<TStore>()` | `IGivenStage` |
| `ICommandStage`, `IQueryStage<T>`, `IHttpStage` | `InStore<TStore>()` | same stage |
| `ICommandStage`, `IQueryStage<T>`, `IHttpStage` | `ThenStreamHasEvent`, `ThenStreamHasNoEvent`, `ThenStreamEventCount`, `ThenThrows` | same stage |
| `ICommandStage`, `IHttpStage` | `ThenAppended`, `ThenNotAppended`, `ThenAppendedCount`, `ThenPublished` | same stage |
| `IQueryStage<T>` | `ThenResult` | `IQueryStage<T>` |
| `IHttpStage` | `ThenStatusCode`, `ThenResponse<TBody>` | `IHttpStage` |
| `ICommandStage`, `IQueryStage<T>`, `IHttpStage` | `RunAsync(ct)`, `await` | (runs the scenario) |

Calls that aren't in this table don't compile. For example, a Given after a `When`, `ThenAppended` after a `WhenQuery`, or awaiting a scenario with no `When`, `WhenQuery` or `WhenHttp`.

Nothing runs until `RunAsync` is called or the scenario is awaited.

The stage interfaces are what the harness hands back. Call them; don't implement them. New members can be added to them in a minor release.

## Stream ids

Every method that names a stream (`GivenStream`, `ThenStream*`, `ThenAppended*`) comes in three forms, so it fits the store's `Events.StreamIdentity`:

| Id | Form | Store |
|---|---|---|
| `Guid` | `GivenStream(Guid id, Func<Guid, object[]> events)`, `ThenAppended<TEvent>(Guid id, ...)` | `StreamIdentity.AsGuid` (Marten's default) |
| `string` | `GivenStream(string id, Func<string, object[]> events)`, `ThenAppended<TEvent>(string id, ...)` | `StreamIdentity.AsString` |
| strongly typed id `TId` | `GivenStream<TId>(TId id, Func<TId, object[]> events)`, `ThenAppended<TEvent, TId>(TId id, ...)` | whichever kind `TId` wraps |

- **Strongly typed ids** are any type Marten accepts as a value type identifier: a single public, gettable property that wraps a `Guid` or a `string`, such as `record struct OrderId(Guid Value)`, a Vogen or StronglyTypedId type. The harness unwraps the id as Marten does; the events factory gets the typed id back. Where an event type is named, give both type arguments (`ThenAppended<OrderShipped, OrderId>(orderId)`), because C# can't infer just one. `ThenStreamEventCount(orderId, n)` and `ThenAppendedCount(orderId, n)` infer `TId`.
- A typed id and the value it wraps name the same stream, so `GivenStream(orderId, ...)` and `ThenAppendedCount(orderId.Value, 1)` agree.
- **Fails when the scenario is built:** `ArgumentException` for a blank string id, a `TId` that isn't a single-value wrapper, or one that wraps anything but a `Guid` or `string` (or an empty string); `ArgumentNullException` for a null id.
- **Fails before any step runs (not claimable):** an id of the wrong kind for the store, for example a `string` on a `Guid` store. See [failure messages](failure-messages.md#infrastructure-failures).
- Step labels show a `Guid` as is, a `string` in quotes (`"order-17"`) and a typed id with its type (`OrderId(3f0c…)`).

The sections below list the `Guid` form. The `string` and typed forms behave the same.

## Stores

A scenario starts on the host's main `IDocumentStore`. Services that keep data in ancillary stores (`AddMartenStore<TStore>()`) switch with `InStore<TStore>()`:

```csharp
await TestHarness.Setup(host)
    .GivenStream<Widget>(widgetId, ...)         // main store
    .InStore<ILedgerStore>()
    .GivenStream<Ledger>(ledgerId, ...)         // ledger store
    .When(() => new PostEntry(ledgerId, 10m))   // Wolverine's [MartenStore] routing picks the handler's store
    .ThenAppended<EntryPosted>(ledgerId)        // still the ledger store
    .InStore<IDocumentStore>()
    .ThenStreamEventCount(widgetId, 1);         // back to the main store
```

- `InStore` changes the store for the stream steps after it (`GivenStream`, `ThenStream*`, `ThenAppended*`) until the next `InStore`, across `When`, `WhenQuery` and `WhenHttp`. `InStore<IDocumentStore>()` is the main store. Commands, queries and requests are unaffected.
- Projection sync covers the main store, every store named by `InStore`, and every store added with `HarnessOptions.UseStore<TStore>()`, in that order. Each store uses its own daemon if the host runs one, otherwise a temporary daemon from that store.
- The same stream id in two stores is two streams. Sessions use the scenario's tenant in every store. Stream ids are checked against the step's store's `StreamIdentity`, and its events are shown with that store's serializer.
- Labels of steps outside the main store end in ` in TStore`, for example `ThenAppended<EntryPosted>(3f0c…) in ILedgerStore`.
- **Fails:** `InStore` throws `InvalidOperationException` at once if `TStore` isn't registered in the host, naming the type and `AddMartenStore<TStore>()`. A projection error in an ancillary store fails the next action with `projection sync (<mode>) of TStore failed with …`.

## `TestHarness`

### `Setup(IHost host, Action<HarnessOptions>? configure = null)`

- **Returns:** `IScenarioStart`.
- **Does:** starts recording a scenario against `host`. The harness never starts, stops or disposes the host. An `IAlbaHost` is an `IHost`.
- **Fails:** `ArgumentNullException` if `host` is null; `InvalidOperationException` if a store added with `UseStore<TStore>()` isn't registered.

## `IScenarioStart`

Everything on `IGivenStage`, plus:

### `ForTenant(string tenantId)`

- **Returns:** `IGivenStage` (so it can only be called once, before any Given).
- **Does:** runs every step as `tenantId`. Givens and Then reads use `LightweightSession(tenantId)`, `When` uses `InvokeForTenantAsync`, `WhenQuery` uses `InvokeForTenantAsync<T>`, and `WhenHttp` applies `HarnessOptions.HttpTenant` to the request. Without it, the scenario uses Marten's default tenant.
- **Fails:** `ArgumentException` if `tenantId` is null or white space.

## `IGivenStage`

### `GivenStream(Guid id, Func<Guid, object[]> events)`

### `GivenStream<TAggregate>(Guid id, Func<Guid, object[]> events)`

Also `GivenStream(string id, Func<string, object[]> events)`, `GivenStream<TAggregate>(string id, ...)`, `GivenStream<TId>(TId id, Func<TId, object[]> events)` and `GivenStream<TAggregate, TId>(TId id, ...)` (see [stream ids](#stream-ids)).

- **Returns:** `IGivenStage`.
- **Does:** writes the events to stream `id` in one `SaveChangesAsync`. The scenario's first Given for an id starts the stream (`StartStream`, or `StartStream<TAggregate>`); later Givens for the same id append. `TAggregate` is ignored on an append. Marks projections as needing a sync.
- **Fails (at once, not claimable):** the events factory throws; the write fails, for example `ExistingStreamIdCollisionException` if the id already exists in the database.

### `When(Func<object> command)`

- **Returns:** `ICommandStage`.
- **Does:** syncs projections if anything was written since the last sync; snapshots the version of every stream a later `ThenAppended*` on this stage reads; invokes the command under Wolverine tracking (`TrackingTimeout`, plus `IncludeExternalTransports()` if `IncludeExternalMessages`). A handler exception is captured for `ThenThrows`.
- **Fails (at once, not claimable):** the factory throws or returns null; no handler is registered; tracking times out; projection sync fails.

### `WhenQuery<TResponse>(Func<object> query)`

- **Returns:** `IQueryStage<TResponse>`.
- **Does:** syncs projections if needed, invokes the query with `InvokeAsync<object>` (or `InvokeForTenantAsync<object>`), and stores the response. A `null` response is stored as `default(TResponse)`. A handler exception is captured for `ThenThrows`.
- **Fails (at once, not claimable):** the factory throws or returns null; no handler is registered; projection sync fails; the handler returns a non-null value that isn't a `TResponse`.

### `WhenHttp(Action<Alba.Scenario> request)`

- **Returns:** `IHttpStage`.
- **Does:** syncs projections if needed; snapshots the streams a later `ThenAppended*` on this stage reads; sends the request through the host's Alba pipeline (`IAlbaHost.Scenario`) under Wolverine tracking, so cascaded messages are waited for and recorded. Under `ForTenant`, applies `HarnessOptions.HttpTenant` after `request`. Alba's own status check is turned off and the response is kept for `ThenStatusCode` and `ThenResponse`. An exception thrown by the endpoint is captured for `ThenThrows`.
- **Status rule:** without a `ThenStatusCode` before the next action, a status outside 200 to 299 fails at once. With one, any status is accepted here and `ThenStatusCode` decides.
- **Fails (at once, not claimable):** the host isn't an `IAlbaHost`; `ForTenant` is set but `HttpTenant` isn't; `request` or `HttpTenant` throws; tracking times out; projection sync fails; an unexpected non-2xx status.

## `IThenStage<TSelf>` (on `ICommandStage`, `IQueryStage<T>` and `IHttpStage`)

Whole-stream assertions read the full stream with `FetchStreamAsync`, including events from Givens and earlier commands. Each also takes a `string` id or a typed id (`ThenStreamHasEvent<TEvent, TId>`, `ThenStreamHasNoEvent<TEvent, TId>`, `ThenStreamEventCount<TId>`), see [stream ids](#stream-ids).

### `ThenStreamHasEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null)`

- **Passes when:** the stream has at least one `TEvent` matching `predicate` (any `TEvent` if null).
- **Failure message:** `no TEvent [matching the predicate] in the stream`, followed by every event in the stream.

### `ThenStreamHasNoEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null)`

- **Passes when:** no `TEvent` in the stream matches `predicate`.
- **Failure message:** `found TEvent [matching the predicate] in the stream, expected none`, followed by every event.

### `ThenStreamEventCount(Guid id, int expected)`

- **Passes when:** the stream has exactly `expected` events. A missing stream has 0.

### `ThenThrows<TException>(Func<TException, bool>? predicate = null)`

- **Passes when:** the most recent `When`, `WhenQuery` or `WhenHttp` captured an exception that is a `TException` (or a subclass), or, for a cascaded handler's `AggregateException`, has a `TException` among its inner exceptions, and `predicate` (if any) returns true for that exception.
- **Effect:** claims the exception, so later Thens run normally.
- **Failure messages:** `expected TException, but <action> threw nothing`; `expected TException, but <action> threw <type>`; `<action> threw TException, but it did not match the predicate`.

### `When(...)`, `WhenQuery<T>(...)`, `WhenHttp(...)`

The same as on `IGivenStage`.

## `ICommandStage`

Appended-event assertions only see events the most recent `When` appended: the harness snapshots each stream's version before the `When` and reads from the next version on. Each also takes a `string` id or a typed id (`ThenAppended<TEvent, TId>`, `ThenNotAppended<TEvent, TId>`, `ThenAppendedCount<TId>`), see [stream ids](#stream-ids).

### `ThenAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null)`

- **Passes when:** the most recent `When` appended at least one matching `TEvent` to the stream.
- **Failure message:** `no TEvent [matching the predicate] appended by <action>`, followed by the appended events.

### `ThenNotAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null)`

- **Passes when:** the most recent `When` appended no matching `TEvent`.

### `ThenAppendedCount(Guid id, int expected)`

- **Passes when:** the most recent `When` appended exactly `expected` events to the stream.

### `ThenPublished<TMessage>(Func<TMessage, bool>? predicate = null)`

- **Passes when:** the tracked session's `Sent` records hold at least one matching `TMessage`.
- **Failure messages:** `<action> did not send TMessage [matching the predicate]`, followed by every sent message; `<action> threw, so there is no tracked session to check`.

## `IQueryStage<TResponse>`

### `ThenResult(Func<TResponse, bool> predicate)`

- **Passes when:** `predicate` returns true for the most recent `WhenQuery`'s response (which may be `null`).
- **Failure messages:** `the result of <action> did not match the predicate`, followed by the response as JSON; `<action> threw <type>, so there is no result`.

## `IHttpStage`

Everything on `IThenStage<IHttpStage>`, plus `ThenAppended`, `ThenNotAppended`, `ThenAppendedCount` and `ThenPublished`, which behave exactly as on `ICommandStage` but refer to the most recent `WhenHttp`.

### `ThenStatusCode(int expected)`

- **Passes when:** the most recent `WhenHttp`'s response status is `expected`.
- **Effect:** lets that `WhenHttp` accept a non-2xx status.
- **Failure messages:** `expected status <n> from <action>, got <m>`, followed by the response body; `<action> threw <type>, so there is no response`.

### `ThenResponse<TBody>(Func<TBody, bool> predicate)`

- **Passes when:** the body of the most recent `WhenHttp`'s response, read as JSON into `TBody` with the host's HTTP serializer (`IScenarioResult.ReadAsJsonAsync<TBody>`), makes `predicate` return true.
- **Failure messages:** `the body of <action> did not match the predicate`, followed by the body; `the body of <action> could not be read as <TBody>: <type>`; `<action> answered <status> with an empty body, so there is no <TBody> to check`; `<action> threw <type>, so there is no response`.

## `IScenario`

### `RunAsync(CancellationToken cancellationToken = default)`

- **Does:** runs every recorded step in order.
- **Fails:** `HarnessAssertionException` for the first failing step. See [failure messages](failure-messages.md).

### `GetAwaiter()`

- **Does:** lets the scenario be awaited directly: `await scenario` is `await scenario.RunAsync()`.

## `HarnessOptions`

See [harness options](harness-options.md).

## `HarnessAssertionException`

The only exception the harness throws for a failing scenario. `Message` starts with the step (`step 3 ThenAppended<WidgetRenamed>(...)`, or `end of scenario`). `InnerException` is the original exception when one caused the failure.
