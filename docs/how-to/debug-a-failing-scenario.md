# How to debug a failing scenario

## Read the message

The first line names the step and what went wrong. The lines after it show what was actually there:

<!-- Test: HowToSamples.DebugAFailingScenario_message -->
```
step 3 ThenAppended<WidgetRenamed>(9d2e…): no WidgetRenamed matching the predicate appended by step 2 When(RenameWidget).
Actual events:
  v2 WidgetRenamed {"Id":"9d2e…","Name":"Cog"}
```

- **`step 3`** counts every recorded call, starting at 1 with the first Given.
- **`appended by step 2`** means only that command's events are listed. Givens aren't included. Use `ThenStreamHasEvent` to look at the whole stream.
- **Event lines** are `v<version> <Type> <json>`, using your store's serializer, so property names match what is stored.

## An exception you didn't expect

If a handler throws and no `ThenThrows` claims it, the next step fails and says so:

<!-- Test: HowToSamples.DebugAFailingScenario_unexpected_exception -->
```
step 2 ThenAppended<WidgetRenamed>(…): step 1 When(FailWidget) threw WidgetException and no ThenThrows claimed it. To expect it, put ThenThrows<...>() straight after that step.
```

The handler's exception is the `InnerException`, so its message and stack trace are in the test output below the harness message.

## Setup problems

If the message is about a missing handler, a tracking timeout, the wrong response type, projection sync, or a stream that already exists, the scenario never reached your assertions. See [failure messages](../reference/failure-messages.md) for each one.

## A stream that already exists

`writing the Given failed with ExistingStreamIdCollisionException` means a Given reused a stream id from an earlier test. Create a new `Guid` in each test.
