# How to choose a projection sync mode

The harness brings async projections up to date before each `When` and `WhenQuery`. Pick the mode that matches how your test host runs projections.

| Your test host | Use |
|---|---|
| Has async projections, no `AddAsyncDaemon` | `CatchUp` (the default) |
| Has async projections and runs `AddAsyncDaemon` | `CatchUp` or `WaitForNonStale` |
| Has only inline projections | `None` |

## CatchUp

Works on any host. You don't need to set anything:

<!-- Test: HowToSamples.ChooseProjectionSync_catch_up -->
```csharp
await TestHarness
    .Setup(fixture.Host) // ProjectionSync.CatchUp is the default
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
    .ThenResult(s => s.Name == "Sprocket");
```

## WaitForNonStale

Waits for the host's own daemon. Only use it on a host that runs `AddAsyncDaemon`; without a daemon it times out.

<!-- Test: DaemonHowToSamples.ChooseProjectionSync_wait_for_non_stale -->
```csharp
await TestHarness
    .Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.WaitForNonStale)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
    .ThenResult(s => s.Name == "Sprocket");
```

## None

Skips sync entirely. Inline projections are already current after each write. Async read models stay stale:

<!-- Test: HowToSamples.ChooseProjectionSync_none -->
```csharp
await TestHarness
    .Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.None)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
    .ThenResult(s => s is null); // the async projection never ran
```

## If sync is slow

Raise `ProjectionTimeout` (default 30 seconds). A sync that runs over it fails the scenario with `projection sync (<mode>) failed`.

Why sync runs where it does: [projection sync](../explanation/projection-sync.md).
