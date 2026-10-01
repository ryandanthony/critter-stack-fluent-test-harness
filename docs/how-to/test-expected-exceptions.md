# How to test expected exceptions

## Claim the exception first

Put `ThenThrows<T>` straight after the `When` or `WhenQuery` that throws. Any other assertion that runs first fails with the exception instead.

<!-- Test: HowToSamples.TestExpectedExceptions_claim_first -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .When(() => new FailWidget(id))
    .ThenThrows<WidgetException>(e => e.Message.Contains("failed", StringComparison.Ordinal))
    .ThenAppendedCount(id, 0);
```

The predicate is optional. After `ThenThrows`, later assertions run normally, so you can check that nothing was written.

## Exceptions from queries

<!-- Test: HowToSamples.TestExpectedExceptions_query -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .WhenQuery<WidgetSummary>(() => new GetWidgetById(Guid.NewGuid()))
    .ThenThrows<WidgetNotFoundException>();
```

## Exceptions from cascaded messages

If the exception comes from a handler of a message the command cascaded, Wolverine wraps it in an `AggregateException`. `ThenThrows<T>` still matches by the inner type:

<!-- Test: HowToSamples.TestExpectedExceptions_cascaded -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .When(() => new FailWidgetLater(Guid.NewGuid()))
    .ThenThrows<WidgetException>(); // matched inside Wolverine's AggregateException
```

## What can't be claimed

Setup problems fail the scenario at once, even with a `ThenThrows` after them: no handler registered, tracking timeout, wrong query response type, projection sync failure, and a Given that collides with an existing stream. See [failure messages](../reference/failure-messages.md).

Why: [exception handling](../explanation/exception-handling.md).
