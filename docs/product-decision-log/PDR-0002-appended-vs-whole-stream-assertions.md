# PDR-0002: Assert what the command did, separately from what the stream holds

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Thens, How "appended" is measured), [explanation/appended-vs-whole-stream.md](../explanation/appended-vs-whole-stream.md)

## Context

A command test usually wants to know what *that command* did. If the only stream assertion reads the whole stream, a test like "given `WidgetCreated`, when `DoSomething`, then the stream has `WidgetCreated`" passes even when the command did nothing, because the Given wrote the event.

## Decision

Two families of stream assertion with different names:

- **Appended** (`ThenAppended`, `ThenNotAppended`, `ThenAppendedCount`): only the events the most recent `When` appended. Valid only after a `When`, because a query appends nothing.
- **Whole stream** (`ThenStreamHasEvent`, `ThenStreamHasNoEvent`, `ThenStreamEventCount`): every event in the stream as it is when the assertion runs, including Givens and earlier commands. Valid after any `When` or `WhenQuery`.

Both take an optional predicate, and the "no event" forms only fail on an event that matches it.

## Alternatives considered

- **Whole-stream only.** Rejected: the false pass above.
- **Appended only.** Rejected: checking final state after several commands, or that a stream doesn't exist (`ThenStreamEventCount(id, 0)`), needs the whole stream.

## Consequences

- The harness has to know each stream's version before the command runs ([ADR-0006](../architecture-decision-log/ADR-0006-appended-events-by-version-snapshot.md)).
- "Appended" is only exact when nothing else writes to the stream while the command runs, so tests use fresh ids.

## Non-goals

- Attributing events to a command in production code. This is a test-time measure only.
