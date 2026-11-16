# ADR-0008: Exceptions are classified by origin, not only by type

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Captured exceptions: how each case surfaces, classification rule), commits `fbca57b`, `99ee5e0`

## Context

[PDR-0004](../product-decision-log/PDR-0004-handler-exceptions-are-assertable.md) separates claimable handler exceptions from infrastructure failures. A handler can throw any type, including `TimeoutException`, and Wolverine 6.44 wraps a handler's own `TimeoutException` in **the same** "This TrackedSession timed out…" `TimeoutException` it uses for a real tracking timeout. Verified surface of each case:

| Case | What Wolverine/Marten does |
|---|---|
| Handler throws (`When` or `WhenQuery`) | its own exception, as-is |
| Cascaded handler throws | `AggregateException` with the handler exceptions |
| Tracking timeout | `TimeoutException` "This TrackedSession timed out…", inner `TimeoutException` |
| Handler throws its own `TimeoutException` | the same wrapper, immediately, with the handler's exception inside |
| No handler | `IndeterminateRoutesException` |
| Wrong `TResponse` | nothing: `default(TResponse)` is returned |
| Given stream collision | `ExistingStreamIdCollisionException` |

## Decision

One internal `ExceptionClassifier`:

- Anything from a **Given or projection sync** is infrastructure.
- From a `When` or `WhenQuery`, **`IndeterminateRoutesException`** is infrastructure.
- The tracked-session timeout wrapper is infrastructure **only if the `When` ran for at least 90% of `TrackingTimeout`**. An earlier wrapper is unwrapped and captured as the handler's exception. Elapsed time is the only reliable signal.
- Everything else from a handler is captured. `ThenThrows<T>` matches the exception or, for an `AggregateException`, one of its inner exceptions.
- **Wrong `TResponse` is checked by the harness**: `WhenQuery` invokes as `object` and fails at once on a non-null result that isn't a `TResponse`. `null` passes through, because a handler may legitimately return it.
- Cancellation of the scenario's token propagates as cancellation, never as a captured exception.

## Consequences

- The classifier is the part most likely to break on a Critter Stack upgrade. Each row above is a test.
- A handler that genuinely runs for over 90% of `TrackingTimeout` and then throws its own `TimeoutException` is reported as a tracking timeout. That edge is accepted.
