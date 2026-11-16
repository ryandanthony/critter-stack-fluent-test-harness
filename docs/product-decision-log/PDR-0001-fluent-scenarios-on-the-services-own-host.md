# PDR-0001: Fluent Given/When/Then scenarios on the service's own test host

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Target API, Design), now [design/01-harness-design.md](../design/01-harness-design.md)

## Context

Services built on Marten and Wolverine test behaviour the same way every time: write some events, send a command or query through Wolverine, then check the events, messages and read models that result. Written by hand, each test repeats session setup, tracking, projection waits and stream reads, and the intent ("given this history, when this command, then this event") gets buried. Every such service already has an Alba test fixture that knows how to build its host.

## Decision

- A test is **one fluent chain**: `TestHarness.Setup(host)`, then Givens, then any mix of `When` (command) and `WhenQuery` (query), each followed by its Thens, awaited once.
- `Setup` takes the **host the service's fixture already built** (`IHost`, so any `IAlbaHost` works). The harness never builds, configures, starts or disposes a host.
- Givens write **event streams**. Commands and queries go **through Wolverine**, so the scenario exercises the real handlers, middleware and Marten integration.
- Thens assert on **appended events, whole streams, published messages, query results and thrown exceptions**.
- **Misordered scenarios don't compile**, and IntelliSense only offers the steps that make sense next.

## Alternatives considered

- **Plain xUnit with helpers per service.** Rejected: the helpers get rewritten in every service and drift apart.
- **A harness that builds its own host.** Rejected: services build hosts very differently (transports, auth, configuration), and their fixtures already do it right.
- **Runtime step-order checks.** Rejected as the main guard: a misordered scenario would only fail when someone ran it.

## Consequences

- Drives the stage interfaces and deferred execution ([ADR-0005](../architecture-decision-log/ADR-0005-deferred-steps-and-staged-interfaces.md)) and the dependency rule ([ADR-0004](../architecture-decision-log/ADR-0004-self-contained-harness-project.md)).
- The harness only needs `IDocumentStore` and Wolverine from the host's services.

## Non-goals

- HTTP endpoint testing; Alba already does that ([PDR-0008](./PDR-0008-v1-non-goals.md)).
- Owning containers, databases or host lifetime.
