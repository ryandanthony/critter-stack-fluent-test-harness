# Appended vs whole-stream assertions

There are two families of stream assertion, and they answer different questions.

| Family | Question | Sees |
|---|---|---|
| `ThenAppended`, `ThenNotAppended`, `ThenAppendedCount` | What did *this command* do? | Only events appended by the most recent `When` |
| `ThenStreamHasEvent`, `ThenStreamHasNoEvent`, `ThenStreamEventCount` | What's in the stream now? | Every event, including Givens and earlier commands |

## Why both exist

A test of a command usually wants to know what that command did. With whole-stream assertions only, this passes even if the command did nothing:

```csharp
.GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
.When(() => new DoSomething(id))
.ThenStreamHasEvent<WidgetCreated>(id) // true because of the Given
```

`ThenAppended<WidgetCreated>` would fail here, which is the point. The appended family is what most command tests should use.

Whole-stream assertions are still useful: after a query (which appends nothing), to check the final state of a stream after several commands, or to prove a stream doesn't exist (`ThenStreamEventCount(id, 0)`).

## How "appended" is measured

Marten doesn't record which command appended an event. The harness works it out from stream versions:

1. Before running, the runner walks the recorded steps. For each `When`, it collects the stream ids used by the appended-event assertions that follow it.
2. Just before invoking the command, the `When` step reads each of those streams' current version with `FetchStreamStateAsync` (0 if the stream doesn't exist yet).
3. The assertion then reads the stream from the next version on, with `FetchStreamAsync(id, fromVersion: snapshot + 1)`.

Only streams an assertion mentions are snapshotted, so a `When` with no appended-event assertions costs nothing extra.

This relies on the command being the only thing writing to the stream while it runs. That holds in a test with fresh ids, which is why each test should create its own.
