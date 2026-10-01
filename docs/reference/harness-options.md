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
| `ProjectionSync` | `ProjectionSync` | `CatchUp` | How async projections are brought up to date before each `When` and `WhenQuery`, when something was written since the last sync. |
| `ProjectionTimeout` | `TimeSpan` | 30 seconds | How long one projection sync may take. Exceeding it fails the scenario at once. |
| `TrackingTimeout` | `TimeSpan` | 10 seconds | How long Wolverine's tracked session waits for a `When` and every message it cascades. Exceeding it fails the scenario at once. |
| `IncludeExternalMessages` | `bool` | `false` | Adds `IncludeExternalTransports()` to Wolverine tracking, so cascaded messages sent to real brokers are waited for. |

## `ProjectionSync` values

| Value | Calls | Valid on |
|---|---|---|
| `CatchUp` | With a daemon (`IProjectionCoordinator` registered): `host.ForceAllMartenDaemonActivityToCatchUpAsync`. Without one: `store.BuildProjectionDaemonAsync()` then `CatchUpAsync(ProjectionTimeout)`. | Any host. |
| `WaitForNonStale` | `host.WaitForNonStaleProjectionDataAsync(ProjectionTimeout)`. | Hosts that run `AddAsyncDaemon`. Without a daemon it always times out. |
| `None` | Nothing. | Hosts whose projections are all inline. |
