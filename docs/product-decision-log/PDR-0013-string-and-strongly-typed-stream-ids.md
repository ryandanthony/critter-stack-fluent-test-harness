# PDR-0013: Scenarios support string and strongly typed stream ids

- **Status:** accepted
- **Date:** 2026-11-17
- **Deciders:** Ryan
- **Supersedes:** the "string stream ids" bullet of [PDR-0008](./PDR-0008-v1-non-goals.md)

## Context

v1 only took `Guid` stream ids. Two kinds of service can't use it as is:

- Services whose store keys streams by string (`Events.StreamIdentity = StreamIdentity.AsString`), for example an order number or an external key. Every Given and stream assertion needs a `string` id, and the harness had no way to pass one.
- Services that use strongly typed ids (`readonly record struct OrderId(Guid Value)`, Vogen, StronglyTypedId) on their aggregates, commands, events and read models. Marten and Wolverine support these for aggregates, projected documents and the aggregate handler workflow. Marten's raw stream API (`StartStream`, `Append`, `FetchStreamAsync`) still takes only the wrapped `Guid` or `string`. Tests would have to write `orderId.Value` everywhere and build the events from a primitive.

## Decision

- Every stream method takes a `Guid`, a `string`, or a strongly typed id:
  - `GivenStream` and `GivenStream<TAggregate>` have `Guid`, `string` and typed (`GivenStream<TId>`, `GivenStream<TAggregate, TId>`) forms. Each events factory receives the id in the same form it was given (`Func<Guid, object[]>`, `Func<string, object[]>`, `Func<TId, object[]>`).
  - `ThenStreamHasEvent`, `ThenStreamHasNoEvent`, `ThenStreamEventCount`, `ThenAppended`, `ThenNotAppended` and `ThenAppendedCount` have `Guid`, `string` and typed forms. A typed form names the id type after the event type (`ThenAppended<OrderShipped, OrderId>(orderId)`); the count forms infer it.
- A **strongly typed id** is any type Marten accepts as a value type identifier: a single public gettable property wrapping a `Guid` or a `string`. The harness unwraps it the same way Marten does. A `TId` that doesn't qualify fails when the scenario is built, with `ArgumentException`.
- A stream id of the **wrong kind for the store** (a `string` on a `Guid` store, or the reverse, including a typed id that wraps the wrong kind) **fails at once**, before any step runs. The message names the step and says which kind the store uses. It is a setup problem, so `ThenThrows` can't claim it.
- Failure messages show a string id in quotes (`"order-17"`) and a typed id with its type (`OrderId(3f0c…)`). Guid ids look as before.
- The Guid API and its behaviour don't change. Adding these overloads is a minor release.

## Alternatives considered

- **Keep Guid only** ([PDR-0008](./PDR-0008-v1-non-goals.md)). Rejected: string-keyed and typed-id services are common in the Critter Stack, and the cost turned out to be one internal id type, not a second copy of every step.
- **String overloads only; typed ids pass `.Value`.** Rejected: typed-id services would have to unwrap ids in every Given and Then, and their events factories would get a primitive instead of the id their events are built from.
- **Let Marten report a wrong-kind id.** Rejected: Marten's error depends on the call. `FetchStreamStateAsync` with a string id on a Guid store returns null without complaint, so an appended assertion could read the wrong events.

## Consequences

- Design in [ADR-0019](../architecture-decision-log/ADR-0019-one-stream-key-for-guid-string-and-typed-ids.md).
- The public surface grows by about thirty overloads across the stage interfaces. Each one is in `reference/api.md`.
- The test suite gains a host configured for string ids ([ADR-0003](../architecture-decision-log/ADR-0003-real-postgres-test-hosts.md)).
- Typed-id call sites need both type arguments where an event type is named, because C# can't infer just one of them.

## Non-goals

- Mixing id kinds in one store. A store has one `StreamIdentity`, and so does a scenario.
- Typed ids that wrap anything other than a `Guid` or `string` (Marten's stream identity is one or the other).
- Binding typed ids from routes in `WhenHttp`. The request is the test's own Alba call, so routing is the endpoint's concern.
