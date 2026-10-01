# How to assert published messages

## Assert a cascaded message

`ThenPublished<T>` checks the messages the most recent `When` sent, as recorded by Wolverine's tracked session:

<!-- Test: HowToSamples.AssertPublishedMessages_cascaded -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .When(() => new PublishWidget(id))
    .ThenPublished<WidgetPublishedNotice>(n => n.Id == id);
```

`ThenPublished` is only available after `When`. A `WhenQuery` isn't tracked.

## Hosts with real transports

If your test host sends to real brokers (for example Service Bus or Kafka emulators), let tracking wait for them and give it more time:

<!-- Test: HowToSamples.AssertPublishedMessages_real_transports_options -->
```csharp
await TestHarness
    .Setup(fixture.Host, o =>
    {
        o.IncludeExternalMessages = true;
        o.TrackingTimeout = TimeSpan.FromSeconds(30);
    })
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .When(() => new PublishWidget(id))
    .ThenPublished<WidgetPublishedNotice>();
```

If tracking still times out, the scenario fails at the `When` with `Wolverine tracking timed out`.
