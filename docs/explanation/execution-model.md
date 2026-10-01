# The execution model

A scenario reads like a sequence of actions:

```csharp
await TestHarness.Setup(host)
    .GivenStream<Widget>(id, ...)
    .When(() => new RenameWidget(id, "Gear"))
    .ThenAppended<WidgetRenamed>(id);
```

None of these calls does anything when it runs. Each one adds a step to a list and returns the next stage. The steps run, in order, when you call `RunAsync` or await the scenario.

## Why record first and run later

The alternative is for each call to do its work straight away. That would make every call `async`, so the chain would need an `await` on every line:

```csharp
var s = await TestHarness.Setup(host).GivenStream<Widget>(id, ...);
s = await s.When(...);
await s.ThenAppended<WidgetRenamed>(id);
```

Recording first keeps the scenario as one readable expression with a single `await`.

It also lets the runner look ahead. Appended-event assertions need to know a stream's version *before* the command runs, and the runner can only know which streams to look at if it has already seen the assertions that follow. See [appended vs whole-stream assertions](appended-vs-whole-stream.md).

## Why stages are interfaces

Each call returns an interface that only offers the calls that make sense next. After `When` you can assert on appended events; after `WhenQuery` you can't, because a query appends nothing. After any `When` or `WhenQuery` you can't add more Givens. A scenario with no action can't be awaited at all.

These rules could be checked when the scenario runs, but then a misordered scenario would only fail when someone ran it. As interfaces, they fail at compile time and IntelliSense only shows valid next steps.

Behind the interfaces there is one builder holding one list of steps. Each `WhenQuery<TResponse>` returns a small typed wrapper over that builder, because a scenario can run several queries with different response types and one object can't be generic in all of them.

## Why the harness doesn't own the host

`Setup` takes a host your fixture already built. The harness never starts, configures or disposes it. Services build their hosts in very different ways (transports, auth, configuration), and a test fixture already knows how to do it. The harness only needs `IDocumentStore` and Wolverine from the host's services.
