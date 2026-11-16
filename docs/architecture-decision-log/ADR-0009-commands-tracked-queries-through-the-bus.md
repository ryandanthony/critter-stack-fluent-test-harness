# ADR-0009: Commands run under Wolverine tracking, queries go straight through the bus

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Multi-tenancy, When, Queries, `ThenPublished`), commit `b84d1af`

## Context

Commands can cascade messages that tests assert on, and the harness must wait for them. Queries return a value and should be fast. Both must run as the scenario's tenant ([PDR-0007](../product-decision-log/PDR-0007-one-tenant-per-scenario.md)) and through Wolverine, not HTTP ([PDR-0008](../product-decision-log/PDR-0008-v1-non-goals.md)).

## Decision

- **`When`** runs `host.TrackActivity(TrackingTimeout)[.IncludeExternalTransports()].ExecuteAndWaitAsync(...)`, invoking with `c.InvokeAsync(command)` or `c.InvokeForTenantAsync(tenantId, command)`. The lambda is cast to `Func<IMessageContext, Task>`, because the `Task`/`ValueTask` overloads make an `async` lambda ambiguous (CS0121). The `ITrackedSession` is kept.
- **`ThenPublished<T>`** matches `tracked.Sent.MessagesOf<T>()` and lists `tracked.Sent.AllMessages()` on failure. `Sent` holds only cascaded messages; `Executed` also holds the command.
- **`WhenQuery<TResponse>`** resolves `IMessageBus` **from a new async scope** (it is scoped in Wolverine) and calls `InvokeAsync<object>` or `InvokeForTenantAsync<object>`, then type-checks the result ([ADR-0008](./ADR-0008-exception-classification-by-origin.md)). Queries are not tracked.
- Givens and Then reads open `store.LightweightSession(tenantId)` when a tenant is set. Tenant and default paths share one code path that only differs in the call.

## Consequences

- Hosts with real transports may need `IncludeExternalMessages` or a longer `TrackingTimeout` for cascades ([how-to/assert-published-messages.md](../how-to/assert-published-messages.md)).
- Anything a query handler cascades isn't waited for, and `ThenPublished` isn't offered after a query.
