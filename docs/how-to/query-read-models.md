# How to query read models

`WhenQuery<TResponse>` invokes a query through Wolverine and `ThenResult` asserts on the typed response. Projections are synced first.

## Query after a command

<!-- Test: HowToSamples.QueryReadModels_after_a_command -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .When(() => new PublishWidget(id))
    .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
    .ThenResult(s => s.Published);
```

## Query without a command

<!-- Test: HowToSamples.QueryReadModels_without_a_command -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket"), new WidgetRenamed(i, "Gear")])
    .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
    .ThenResult(s => s.Name == "Gear");
```

## Queries that may return null

Use a nullable `TResponse`. A `null` response is passed to the predicate:

<!-- Test: HowToSamples.QueryReadModels_nullable -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .WhenQuery<WidgetSummary?>(() => new FindWidget(Guid.NewGuid()))
    .ThenResult(s => s is null);
```

## Get the response type right

`TResponse` must be the type the handler returns. If it returns something else, the scenario fails at the `WhenQuery` with `expected the handler to return <TResponse>, but it returned <Actual>`.

Queries go through Wolverine. To test the HTTP endpoint that serves a read model, use `WhenHttp` (see [test HTTP endpoints](test-http-endpoints.md)).

<!-- ci-check: verify docs.yml runs on pull requests. Not for merge. -->
