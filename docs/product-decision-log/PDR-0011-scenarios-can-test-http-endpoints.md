# PDR-0011: Scenarios can test HTTP endpoints

- **Status:** accepted
- **Date:** 2026-11-16
- **Deciders:** Ryan
- **Supersedes:** the "Test HTTP endpoints" bullet of [PDR-0008](./PDR-0008-v1-non-goals.md), and the HTTP non-goal of [PDR-0001](./PDR-0001-fluent-scenarios-on-the-services-own-host.md)

## Context

v1 left HTTP to plain Alba tests: "the HTTP contract stays in Alba endpoint tests". In practice the services this harness is for expose most of their behaviour through Wolverine.Http endpoints, and their endpoint tests need everything the harness already does: Givens as event streams, async projections caught up first, assertions on appended events and cascaded messages, readable failures. Hand-written Alba tests repeat all of that, and a test that wants to POST and then check the stream can't use the harness at all.

## Decision

- A scenario can send an HTTP request with **`WhenHttp(x => ...)`**, where `x` is Alba's own request builder, so anything Alba can call (Wolverine.Http, minimal APIs, MVC) works.
- After it, the test can assert the **status** (`ThenStatusCode`), the **JSON body** (`ThenResponse<T>`), the **events the request appended**, the **messages it published**, the whole stream, and **an exception the endpoint threw**, all with the same names and failure style as after a `When`.
- **A non-2xx status fails at once** unless the test expects a status with `ThenStatusCode`. Error responses are tested on purpose, not by accident.
- Projections are caught up before the request, like before a `When`.
- `WhenHttp` mixes freely with `When` and `WhenQuery` in one scenario.
- Under `ForTenant`, the test says how the request carries the tenant (`HttpTenant`, for example a header). Without it, the scenario fails rather than send the request as the wrong tenant.

## Alternatives considered

- **Keep HTTP out** ([PDR-0008](./PDR-0008-v1-non-goals.md)). Rejected: endpoint tests are where the duplication is.
- **A harness-specific request DSL** (`WhenPost(url, body)`). Rejected: Alba's builder already covers headers, claims, forms and bodies, and readers know it.
- **Pass Alba's status assertions through.** Rejected: Alba throws its own assertion exception mid-request, which can't name the step or tell a failed status from an endpoint exception.

## Consequences

- Design in [ADR-0014](../architecture-decision-log/ADR-0014-whenhttp-through-alba-under-tracking.md).
- `Setup` needs an `IAlbaHost` for `WhenHttp`. Plain `IHost` scenarios keep working without it.
- The test host gains Wolverine.Http endpoints; the package does not depend on Wolverine.Http.

## Non-goals

- Asserting response headers or non-JSON bodies through the harness. Read them with Alba directly.
- Sending requests outside the in-process Alba host.
