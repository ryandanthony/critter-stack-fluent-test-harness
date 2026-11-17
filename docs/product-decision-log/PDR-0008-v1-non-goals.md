# PDR-0008: v1 non-goals

- **Status:** accepted (the NuGet bullet is superseded by [PDR-0010](./PDR-0010-adopt-as-a-nuget-package.md), the HTTP bullet by [PDR-0011](./PDR-0011-scenarios-can-test-http-endpoints.md), the string stream ids bullet by [PDR-0013](./PDR-0013-string-and-strongly-typed-stream-ids.md), the ancillary stores bullet by [PDR-0014](./PDR-0014-ancillary-document-stores.md))
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (v1 scope, No data wipe, CI), [reference/compatibility.md](../reference/compatibility.md) (v1 limits)

## Context

A test harness can grow into a test framework. v1 covers the services it was built for and stops there.

## Decision

The harness v1 does **not**:

- Support **string stream ids** (`StreamIdentity.AsString`). It would double every stream method and need a second test host.
- Support **ancillary document stores**. It always uses the host's main `IDocumentStore`.
- Run **several tenants in one scenario** ([PDR-0007](./PDR-0007-one-tenant-per-scenario.md)).
- Test **HTTP endpoints**. Queries go through Wolverine; the HTTP contract stays in Alba endpoint tests.
- **Wipe or reset data.** It never calls `ResetAllMartenDataAsync` or `CleanAllMartenDataAsync`; truncating tables breaks parallel tests. Fresh ids per test give isolation.
- Wait for messages **cascaded by a query**. Queries are not tracked.
- Ship as a **NuGet package** ([PDR-0006](./PDR-0006-adopt-by-copying-the-project.md)).

Anything on this list needs a new PDR that supersedes this one before work starts.

## Alternatives considered

None. This record exists to give "no" a reference.

## Consequences

- Isolation is the test author's job: a fresh `Guid` per stream per test.
- Ancillary stores would need a `UseStore<TStore>()` option so projection sync can call the `<TStore>` overloads.

## Non-goals

This record is the non-goals list.
