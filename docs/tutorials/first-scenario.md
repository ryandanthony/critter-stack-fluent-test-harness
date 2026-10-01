# Tutorial: your first scenario

In this tutorial we'll write a test for a small service that renames widgets. We'll start from an empty test, make it pass, break it on purpose to see the failure message, then extend it to read a projection. It takes about ten minutes.

You need Docker running (for Postgres) and the .NET 10 SDK.

## 1. Look at the service

The service has one aggregate, `Widget`, and these pieces:

- an event `WidgetCreated(Id, Name)`, and an event `WidgetRenamed(Id, Name)`;
- a command `RenameWidget(Id, Name)` whose Wolverine handler appends `WidgetRenamed`;
- an async projection `WidgetSummaryProjection` that builds a `WidgetSummary` read model;
- a query `GetWidgetById(Id)` whose handler returns the `WidgetSummary`.

They live in `src/CritterStack.TestHarness.Tests/TestDomain/`. You don't need to change them.

## 2. Start the service in a fixture

Tests need a running host. Create a fixture that builds one with Alba, against a Postgres container:

<!-- Test: WidgetServiceFixture -->
```csharp
public sealed class WidgetServiceFixture(PostgresContainerFixture postgres) : IAsyncLifetime
{
    public IAlbaHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMarten(options =>
            {
                options.Connection(postgres.ConnectionString);
                options.DatabaseSchemaName = "tutorial";
                options.Projections.Add<WidgetSummaryProjection>(ProjectionLifecycle.Async);
            })
            .IntegrateWithWolverine(integration => integration.MessageStorageSchemaName = "tutorial_wolverine");

        builder.Services.DisableAllExternalWolverineTransports();
        builder.Host.UseWolverine(options =>
        {
            options.Policies.AutoApplyTransactions();
            options.Discovery.IncludeType(typeof(WidgetHandlers));
        });

        Host = await AlbaHost.For(builder, _ => { });
    }

    public async ValueTask DisposeAsync() => await Host.DisposeAsync();
}
```

The Postgres container comes from `PostgresContainerFixture`, which the test project already starts once for every test.

## 3. Write the scenario

Add a test class that uses the fixture, with one test:

<!-- Test: FirstScenarioSamples.Renaming_a_widget_records_the_new_name -->
```csharp
public sealed class FirstScenarioSamples(WidgetServiceFixture fixture) : IClassFixture<WidgetServiceFixture>
{
    [Fact]
    public async Task Renaming_a_widget_records_the_new_name()
    {
        var widgetId = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(widgetId, id => [new WidgetCreated(id, "Sprocket")])
            .When(() => new RenameWidget(widgetId, "Gear"))
            .ThenAppended<WidgetRenamed>(widgetId, e => e.Name == "Gear");
    }
}
```

Read it top to bottom: given a widget called Sprocket, when we rename it to Gear, then a `WidgetRenamed` with the name Gear was appended.

Run it:

```
just test
```

The test passes. The first run takes a few seconds longer while Docker pulls Postgres.

## 4. Break it on purpose

Change the expected name in the last line from `"Gear"` to `"Cog"`:

```csharp
            .ThenAppended<WidgetRenamed>(widgetId, e => e.Name == "Cog");
```

Run the tests again. The test fails with a message like this:

<!-- Test: FirstScenarioSamples.Broken_scenario_shows_the_actual_events -->
```
step 3 ThenAppended<WidgetRenamed>(5b1f…): no WidgetRenamed matching the predicate appended by step 2 When(RenameWidget).
Actual events:
  v2 WidgetRenamed {"Id":"5b1f…","Name":"Gear"}
```

Notice three things:

- it names the step that failed, `step 3`, which is the third call after `Setup`;
- it says which command the events came from, `step 2 When(RenameWidget)`;
- it shows the event that was actually appended, with its data, so you can see the name is `Gear`.

Change `"Cog"` back to `"Gear"`. The test passes again.

## 5. Check the read model

The rename should also update the `WidgetSummary` read model. Add a second test that queries it after the command:

<!-- Test: FirstScenarioSamples.Renaming_a_widget_updates_its_summary -->
```csharp
    [Fact]
    public async Task Renaming_a_widget_updates_its_summary()
    {
        var widgetId = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(widgetId, id => [new WidgetCreated(id, "Sprocket")])
            .When(() => new RenameWidget(widgetId, "Gear"))
            .ThenAppended<WidgetRenamed>(widgetId, e => e.Name == "Gear")
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(widgetId))
            .ThenResult(summary => summary.Name == "Gear");
    }
```

Run the tests. Both pass.

Notice the query sees the new name even though `WidgetSummary` is an async projection: the harness caught it up before the query ([why](../explanation/projection-sync.md)).

## What you've built

You wrote two scenarios against a real Marten and Wolverine host: one that checks the events a command appends, and one that also checks a read model. You've seen what a failure looks like and where to find the actual data in it.

Next:

- add the harness to your own service: [how to add the harness to a service](../how-to/add-to-a-service.md);
- look up every assertion: [API reference](../reference/api.md).
