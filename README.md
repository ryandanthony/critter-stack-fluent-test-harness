# critter-stack-fluent-test-harness

A fluent Given/When/Then test harness for services built on [Marten](https://martendb.io) and [Wolverine](https://wolverinefx.net). It plugs into your existing [Alba](https://jasperfx.github.io/alba/) test host, writes Givens as event streams, sends commands and queries through Wolverine and HTTP requests through Alba, and asserts on the events, messages, read models and responses that result.

```
dotnet add package CritterStack.TestHarness
```

```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .When(() => new RenameWidget(id, "Gear"))
    .ThenAppended<WidgetRenamed>(id, e => e.Name == "Gear")
    .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
    .ThenResult(s => s.Name == "Gear");
```

- Async projections are brought up to date before each command, query and request, with or without a running daemon.
- `ThenAppended` checks only what the command or request appended, not events from the Givens.
- `WhenHttp` tests endpoints in the same chain: `ThenStatusCode`, `ThenResponse<T>`, `ThenAppended`, `ThenPublished`.
- Handler exceptions can be asserted with `ThenThrows`; setup problems fail at once with a clear message.
- Failures show the actual events, messages and responses as JSON.

## Documentation

Start with the [tutorial](https://github.com/ryandanthony/critter-stack-fluent-test-harness/blob/main/docs/tutorials/first-scenario.md), or see the [documentation index](https://github.com/ryandanthony/critter-stack-fluent-test-harness/blob/main/docs/index.md). (Absolute links, because this README is also the NuGet package readme.)

Working on the harness itself? Decisions are recorded as [PDRs](docs/product-decision-log/README.md) and [ADRs](docs/architecture-decision-log/README.md), with the [design](docs/design/README.md) and [implementation plan](docs/implementation-plan/00-overview.md) alongside. See [AGENTS.md](AGENTS.md).

## Build and test

You need the .NET 10 SDK, [just](https://github.com/casey/just), Docker (the tests start Postgres with Testcontainers), and a GitHub token with `read:packages` stored for the `github` NuGet source (for `FestinaLente.CodeStandards`).

```
just build
just test
just package-check   # pack and install into a sample consumer
just docs-site       # build the docs website (Node 20)
```

## License

[MIT](LICENSE)
