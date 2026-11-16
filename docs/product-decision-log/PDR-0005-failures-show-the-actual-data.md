# PDR-0005: Failures show the actual data

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Failures, Captured exceptions), [explanation/design-decisions.md](../explanation/design-decisions.md) (Payloads in failure messages)

## Context

"No `WidgetRenamed` matching the predicate" is true but not enough: the author needs to see what *was* there. An earlier draft left payloads out in case they held personal data.

## Decision

- Every failure is a `HarnessAssertionException` whose message **leads with the step** that failed (number, method, type arguments, message type).
- It then shows **what was actually there**: stream events with version, type name and full JSON payload; the query response as JSON; published messages with type name and JSON.
- When an exception caused the failure, it is the **`InnerException`**, so its message and stack trace reach the test output.
- Payloads are shown **in full**. The personal-data concern was dropped: the harness only runs in tests, and the data is what makes a failure quick to understand.

## Alternatives considered

- **Type names only.** Rejected: forces a debugger session for every predicate failure.
- **Redact payloads.** Rejected: see above.

## Consequences

- JSON must match what is stored, including custom converters ([ADR-0010](../architecture-decision-log/ADR-0010-payloads-use-the-stores-serializer.md)).

## Non-goals

- Pretty diffs between expected and actual. Predicates are code, not values, so there is nothing to diff against.
