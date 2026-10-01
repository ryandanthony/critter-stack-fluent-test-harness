# Failure messages

Every failure is a `ScenarioAssertionException` whose message starts with the step that failed:

```
step 4 ThenAppended<WidgetRenamed>(3f0c…): no WidgetRenamed matching the predicate appended by step 3 When(RenameWidget).
Actual events:
  v2 WidgetRenamed {"Id":"3f0c…","Name":"Cog"}
```

When an exception caused the failure, it is the `InnerException`. Payloads are serialized with the store's own Marten serializer.

## Infrastructure failures

These are test setup problems. They fail the scenario at the step where they happen, and `ThenThrows` can't claim them.

| When | Message (after the step) | `InnerException` |
|---|---|---|
| No handler is registered | `no Wolverine handler is registered for <Message>` | `IndeterminateRoutesException` |
| Tracking timed out | `Wolverine tracking timed out after <TrackingTimeout> (TrackingTimeout) waiting for <Command> and its cascaded messages` | `TimeoutException` |
| Query returned the wrong type | `expected the handler to return <TResponse>, but it returned <Actual>` | none |
| Projection sync failed or timed out | `projection sync (<mode>) failed with <type> (ProjectionTimeout <timeout>)` | the sync exception |
| A Given's write failed | `writing the Given failed with <type>. If the stream already exists, use a fresh id per test.` | for example `ExistingStreamIdCollisionException` |
| A factory threw | `building the events/command/query threw <type>` | the factory's exception |
| A factory returned null | `the command/query factory returned null` | none |
| A predicate threw | `the predicate threw <type>` | the predicate's exception |

## Handler exceptions

An exception thrown by a handler is captured. It must be claimed by a `ThenThrows` before any other step runs, otherwise the scenario fails at that step:

```
step 2 ThenAppended<WidgetRenamed>(…): step 1 When(FailWidget) threw WidgetException and no ThenThrows claimed it. To expect it, put ThenThrows<...>() straight after that step.
```

If nothing follows the action, the step is `end of scenario`. The `InnerException` is the handler's exception. A cascaded handler's exception comes as an `AggregateException`, shown as `AggregateException [WidgetException]`.

## Assertion failures

| Assertion | Message (after the step) | Followed by |
|---|---|---|
| `ThenStreamHasEvent` | `no <Event> [matching the predicate] in the stream` | every event in the stream |
| `ThenStreamHasNoEvent` | `found <Event> [matching the predicate] in the stream, expected none` | every event in the stream |
| `ThenStreamEventCount` | `expected <n> event(s) in the stream, found <m>` | every event in the stream |
| `ThenAppended` | `no <Event> [matching the predicate] appended by <action>` | the appended events |
| `ThenNotAppended` | `found <Event> [matching the predicate] appended by <action>, expected none` | the appended events |
| `ThenAppendedCount` | `expected <n> event(s) appended by <action>, found <m>` | the appended events |
| `ThenPublished` | `<action> did not send <Message> [matching the predicate]` | every sent message |
| `ThenPublished` after a throw | `<action> threw, so there is no tracked session to check` | none |
| `ThenResult` | `the result of <action> did not match the predicate` | the response as JSON |
| `ThenResult` after a throw | `<action> threw <type>, so there is no result` | none |
| `ThenThrows`, nothing thrown | `expected <T>, but <action> threw nothing` | none |
| `ThenThrows`, wrong type | `expected <T>, but <action> threw <type>` | none |
| `ThenThrows`, predicate false | `<action> threw <T>, but it did not match the predicate` | none |

Events are listed as `v<version> <Type> <json>`; messages as `<Type> <json>`.
