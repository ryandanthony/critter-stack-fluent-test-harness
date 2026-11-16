# PDR-0004: Handler exceptions are assertable, but setup problems fail at once

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Captured exceptions), [explanation/exception-handling.md](../explanation/exception-handling.md)

## Context

A command that rejects bad input by throwing is doing its job, and a test should be able to say so. But a scenario can also throw because the test is broken: no handler registered, tracking timed out, wrong response type, projection sync failed, a Given hit an existing stream. If `ThenThrows<Exception>()` could swallow those, broken tests would pass.

## Decision

- An exception thrown **by a handler** during `When` or `WhenQuery` is captured, and `ThenThrows<T>(predicate?)` can claim it. A cascaded handler's exception (wrapped by Wolverine in `AggregateException`) is claimable by its inner type.
- **Infrastructure failures** fail the scenario at the step where they happen, as a `HarnessAssertionException` naming the step. `ThenThrows` never sees them.
- An **unclaimed** handler exception fails the scenario at the first point where it could hide the real cause: the first Then that isn't `ThenThrows`, the next `When`/`WhenQuery`, or the end. That step reports "`<step>` threw `<type>`" instead of its own, misleading, check. The rule for authors is: put `ThenThrows` first.
- `ThenThrows` when nothing was thrown fails, and `ThenResult` after a claimed query exception fails with "query threw `<type>`, no result" rather than a null reference.

## Alternatives considered

- **Every exception claimable.** Rejected: hides broken tests.
- **Every exception fails at once.** Rejected: rejection paths couldn't be tested.

## Consequences

- Telling the two kinds apart is not possible by type alone ([ADR-0008](../architecture-decision-log/ADR-0008-exception-classification-by-origin.md)).
- The full catalogue is [reference/failure-messages.md](../reference/failure-messages.md).

## Non-goals

- Asserting on infrastructure failures. They are test bugs, not behaviour.
