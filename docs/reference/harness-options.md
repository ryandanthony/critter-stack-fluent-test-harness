# Harness options

Set through the `configure` callback of `TestHarness.Setup`:

```csharp
TestHarness.Setup(host, o =>
{
    o.ProjectionSync = ProjectionSync.WaitForNonStale;
    o.TrackingTimeout = TimeSpan.FromSeconds(30);
});
```

| Option | Type | Default | Effect |
|---|---|---|---|
| `ProjectionSync` | `ProjectionSync` | `CatchUp` | How async projections are brought up to date before each `When`, `WhenQuery` and `WhenHttp`, when something was written since the last sync. |
| `ProjectionTimeout` | `TimeSpan` | 30 seconds | How long one projection sync may take. Exceeding it fails the scenario at once. |
| `TrackingTimeout` | `TimeSpan` | 10 seconds | How long Wolverine's tracked session waits for a `When` or `WhenHttp` and every message it cascades. Exceeding it fails the scenario at once. |
| `IncludeExternalMessages` | `bool` | `false` | Adds `IncludeExternalTransports()` to Wolverine tracking, so cascaded messages sent to real brokers are waited for. |
| `HttpTenant` | `Action<Alba.Scenario, string>?` | `null` | How a `WhenHttp` request carries the scenario's tenant under `ForTenant`, for example `(x, tenant) => x.WithRequestHeader("tenant", tenant)`. Applied after the test's own request configuration. Required for `WhenHttp` in a `ForTenant` scenario. |

## `UseStore<TStore>()`

- **Returns:** the same `HarnessOptions`, so calls chain: `o => o.UseStore<IOrderStore>().UseStore<IBillingStore>()`.
- **Does:** adds an ancillary Marten store (`AddMartenStore<TStore>()`) to the stores projection sync brings up to date before each action. Use it for a store the scenario never names with `InStore`, such as one that only a `[MartenStore]` handler writes and only a query reads. The main store and every store named by `InStore` are always synced, so `UseStore` is never needed for them. `UseStore<IDocumentStore>()` and repeated calls are harmless.
- **Fails:** `TestHarness.Setup` throws `InvalidOperationException` if `TStore` isn't registered in the host, naming the type and `AddMartenStore<TStore>()`.

## `ProjectionSync` values

| Value | Calls, for each store the scenario syncs | Valid on |
|---|---|---|
| `CatchUp` | With that store's daemon running (`IProjectionCoordinator`, or `IProjectionCoordinator<TStore>` for an ancillary store): `host.ForceAllMartenDaemonActivityToCatchUpAsync` (or its `<TStore>` form). Without one: `store.BuildProjectionDaemonAsync()` then `CatchUpAsync(ProjectionTimeout)`. | Any host. |
| `WaitForNonStale` | `host.WaitForNonStaleProjectionDataAsync(ProjectionTimeout)` (or its `<TStore>` form). | Hosts that run `AddAsyncDaemon` for every synced store. Without a daemon it always times out. |
| `None` | Nothing. | Hosts whose projections are all inline. |
