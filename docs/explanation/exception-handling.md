# Exception handling

A scenario can fail in two very different ways, and the harness treats them differently.

## Behaviour under test vs setup problems

**Handler exceptions** are behaviour. A command that rejects bad input by throwing is doing its job, and a test should be able to say "this throws `WidgetException`". So when a handler throws, the harness captures the exception and lets `ThenThrows` claim it.

**Infrastructure failures** are setup problems: no handler is registered, tracking timed out, the query returned a different type than asked for, projection sync failed, or a Given hit an existing stream. None of these is something a test means to assert, and letting `ThenThrows<Exception>()` swallow them would hide broken tests. So they fail the scenario at once, and `ThenThrows` never sees them.

## Telling them apart

Type alone isn't enough, because a handler can throw anything, including `TimeoutException`. The harness goes by where the exception came from:

- anything from a Given or from projection sync is infrastructure;
- from a `When` or `WhenQuery`, `IndeterminateRoutesException` means no handler;
- Wolverine reports a tracking timeout as a `TimeoutException` saying "This TrackedSession timed out". It uses the same wrapper for a handler's own `TimeoutException`. The difference is timing: a real tracking timeout only fires after `TrackingTimeout` has passed. So a wrapper that arrives early is unwrapped and treated as the handler's exception;
- a wrong response type throws nothing at all. Wolverine just returns `null`, so the harness asks for the raw response and checks its type itself.

The exact behaviour of each case is in the [compatibility](../reference/compatibility.md) page, and each is covered by a test, so a Critter Stack upgrade that changes one fails the build.

## Why an unclaimed exception fails early

Suppose a command throws and the test checks for an appended event before `ThenThrows`:

```csharp
.When(() => new FailWidget(id))
.ThenAppended<WidgetRenamed>(id)
.ThenThrows<WidgetException>()
```

If `ThenAppended` ran normally, it would fail with "no WidgetRenamed appended", which is true but misleading: the real cause is the exception. So any step other than `ThenThrows` that runs while an exception is unclaimed fails with the exception instead, and attaches it as `InnerException`. The rule for test authors is simple: put `ThenThrows` first.

## Why failures include payloads and inner exceptions

Failure messages show event and response JSON, and keep the original exception as `InnerException` with its message and stack trace. This is a test library: the output only ever goes to a developer or a CI log, and seeing the actual data is the fastest way to understand a failure.
