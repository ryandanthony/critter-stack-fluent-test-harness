# ADR-0010: Failure payloads use the store's own serializer

- **Status:** accepted (since [ADR-0020](./ADR-0020-ancillary-stores-per-step-and-synced-together.md), events use the serializer of the store the step reads)
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Failures)

## Context

Failures show actual payloads as JSON ([PDR-0005](../product-decision-log/PDR-0005-failures-show-the-actual-data.md)). Services often configure Marten with custom converters, casing or enum handling, so a generic `System.Text.Json` dump can differ from what's stored and mislead.

## Decision

- `PayloadFormatter` serializes events, query responses and published messages with **`IDocumentStore.Options.Serializer()`**.
- Stream assertions list each relevant event (the whole stream, or only the appended events) with **version, type name and JSON**. `ThenResult` shows the response; `ThenPublished` lists sent messages with type names.
- `Failure` builds every `HarnessAssertionException` message with a **step label** (`step N <Method><T>(id)`, with `When(<MessageType>)` or `WhenQuery<TResponse>` for actions), and attaches the cause as `InnerException`.

## Consequences

- Failure JSON matches the database, including custom converters.
- A payload the serializer can't handle must not hide the real failure; the formatter falls back rather than throwing.
