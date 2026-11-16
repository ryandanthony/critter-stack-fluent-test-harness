# ADR-0006: "Appended" is measured by a stream-version snapshot

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (How "appended" is measured, Givens), commit `d246f00`

## Context

Appended-event assertions must see only the events the most recent `When` appended ([PDR-0002](../product-decision-log/PDR-0002-appended-vs-whole-stream-assertions.md)). Marten doesn't record which command appended an event.

## Decision

- Before running, the runner **walks the steps** and, for each `When`, collects the stream ids used by the appended-event assertions that follow it (up to the next action).
- Right before invoking the command, the `When` step reads **`FetchStreamStateAsync`** for each of those ids and stores the version (0 when it returns `null`, which it does for a stream that doesn't exist yet).
- Appended assertions read **`FetchStreamAsync(id, fromVersion: snapshot + 1)`**.
- Streams no appended assertion mentions are **never snapshotted**, so a `When` without them costs nothing extra.
- Givens: the first `GivenStream` for an id calls `StartStream` (`StartStream<TAggregate>` for the typed form); later ones `Append`, so history can be built over several Givens. Each Given is its own `SaveChangesAsync`. A `<TAggregate>` on an append is ignored.

## Consequences

- Correct only while nothing else writes to the stream during the command, which fresh ids per test guarantee.
- A reused id on the first Given hits Marten's `ExistingStreamIdCollisionException`, which is an infrastructure failure ([ADR-0008](./ADR-0008-exception-classification-by-origin.md)).
