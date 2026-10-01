# API reference

Every public type and method in `CritterStack.TestHarness`, in the order a scenario uses them. Each method lists the stage it's on, the stage it returns, what it does, and how it fails. The XML doc comments describe the same behaviour, so IntelliSense and this page agree.

## Stages

| From | Method | To |
|---|---|---|
| (none) | `TestHarness.Setup(host, configure?)` | `IScenarioStart` |
| `IScenarioStart` | `ForTenant(tenantId)` | `IGivenStage` |
| `IScenarioStart`, `IGivenStage` | `GivenStream(id, events)`, `GivenStream<TAggregate>(id, events)` | `IGivenStage` |
| `IScenarioStart`, `IGivenStage`, `ICommandStage`, `IQueryStage<T>` | `When(command)` | `ICommandStage` |
| `IScenarioStart`, `IGivenStage`, `ICommandStage`, `IQueryStage<T>` | `WhenQuery<TResponse>(query)` | `IQueryStage<TResponse>` |
| `ICommandStage`, `IQueryStage<T>` | `ThenStreamHasEvent`, `ThenStreamHasNoEvent`, `ThenStreamEventCount`, `ThenThrows` | same stage |
| `ICommandStage` | `ThenAppended`, `ThenNotAppended`, `ThenAppendedCount`, `ThenPublished` | `ICommandStage` |
| `IQueryStage<T>` | `ThenResult` | `IQueryStage<T>` |
| `ICommandStage`, `IQueryStage<T>` | `RunAsync(ct)`, `await` | (runs the scenario) |

Calls that aren't in this table don't compile. For example, a Given after a `When`, `ThenAppended` after a `WhenQuery`, or awaiting a scenario with no `When` or `WhenQuery`.

Nothing runs until `RunAsync` is called or the scenario is awaited.

## `TestHarness`

### `Setup(IHost host, Action<HarnessOptions>? configure = null)`

- **Returns:** `IScenarioStart`.
- **Does:** starts recording a scenario against `host`. The harness never starts, stops or disposes the host. An `IAlbaHost` is an `IHost`.
- **Fails:** `ArgumentNullException` if `host` is null.

## `IScenarioStart`

Everything on `IGivenStage`, plus:

### `ForTenant(string tenantId)`

- **Returns:** `IGivenStage` (so it can only be called once, before any Given).
- **Does:** runs every step as `tenantId`. Givens and Then reads use `LightweightSession(tenantId)`, `When` uses `InvokeForTenantAsync`, `WhenQuery` uses `InvokeForTenantAsync<T>`. Without it, the scenario uses Marten's default tenant.
- **Fails:** `ArgumentException` if `tenantId` is null or white space.

## `IGivenStage`

### `GivenStream(Guid id, Func<Guid, object[]> events)`

### `GivenStream<TAggregate>(Guid id, Func<Guid, object[]> events)`

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

## `IThenStage<TSelf>` (on both `ICommandStage` and `IQueryStage<T>`)

Whole-stream assertions read the full stream with `FetchStreamAsync`, including events from Givens and earlier commands.

### `ThenStreamHasEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null)`

- **Passes when:** the stream has at least one `TEvent` matching `predicate` (any `TEvent` if null).
- **Failure message:** `no TEvent [matching the predicate] in the stream`, followed by every event in the stream.

### `ThenStreamHasNoEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null)`

- **Passes when:** no `TEvent` in the stream matches `predicate`.
- **Failure message:** `found TEvent [matching the predicate] in the stream, expected none`, followed by every event.

### `ThenStreamEventCount(Guid id, int expected)`

- **Passes when:** the stream has exactly `expected` events. A missing stream has 0.

### `ThenThrows<TException>(Func<TException, bool>? predicate = null)`

- **Passes when:** the most recent `When` or `WhenQuery` captured an exception that is a `TException` (or a subclass), or, for a cascaded handler's `AggregateException`, has a `TException` among its inner exceptions, and `predicate` (if any) returns true for that exception.
- **Effect:** claims the exception, so later Thens run normally.
- **Failure messages:** `expected TException, but <action> threw nothing`; `expected TException, but <action> threw <type>`; `<action> threw TException, but it did not match the predicate`.

### `When(...)`, `WhenQuery<T>(...)`

The same as on `IGivenStage`.

## `ICommandStage`

Appended-event assertions only see events the most recent `When` appended: the harness snapshots each stream's version before the `When` and reads from the next version on.

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

## `IScenario`

### `RunAsync(CancellationToken cancellationToken = default)`

- **Does:** runs every recorded step in order.
- **Fails:** `ScenarioAssertionException` for the first failing step. See [failure messages](failure-messages.md).

### `GetAwaiter()`

- **Does:** lets the scenario be awaited directly: `await scenario` is `await scenario.RunAsync()`.

## `HarnessOptions`

See [harness options](harness-options.md).

## `ScenarioAssertionException`

The only exception the harness throws for a failing scenario. `Message` starts with the step (`step 3 ThenAppended<WidgetRenamed>(...)`, or `end of scenario`). `InnerException` is the original exception when one caused the failure.
