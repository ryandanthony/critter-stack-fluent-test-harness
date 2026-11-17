# ADR-0019: One stream key for Guid, string and strongly typed ids

- **Status:** accepted (since [ADR-0020](./ADR-0020-ancillary-stores-per-step-and-synced-together.md), stream state is keyed by store and `StreamKey`, and the id-kind check uses the step's store)
- **Date:** 2026-11-17
- **Deciders:** Ryan
- **Source:** runtime probe against Marten 9.44.0 / WolverineFx 6.44.0 (JasperFx 2.79.1) and Marten 9.62.0 / WolverineFx 6.64.2, 2026-11-17

## Context

[PDR-0013](../product-decision-log/PDR-0013-string-and-strongly-typed-stream-ids.md) adds `string` and strongly typed stream ids. Verified on the floor and on the latest releases, with identical results:

| Case | What happens |
|---|---|
| `StartStream(string)` or `FetchStreamAsync(string)` on a `Guid` store | `InvalidOperationException` ("configured for Guid stream identity…") |
| `FetchStreamStateAsync(string)` on a `Guid` store | **returns null**, no error |
| `StartStream(Guid)` or `FetchStreamAsync(Guid)` on a `string` store | `InvalidOperationException` ("configured for string stream identity…") |
| `FetchStreamStateAsync(Guid)` on a `string` store | `InvalidOperationException` ("Parameter '' cannot be null…"), a Npgsql message that doesn't name the cause |
| `StartStream(Type, string, object[])`, `StartStream(Type, Guid, object[])` | exist on `IEventOperations` at the floor; the same as `StartStream<T>` |
| `ValueTypeInfo.ForType(typeof(OrderId))` for `record struct OrderId(Guid Value)`, a `string` wrapper, a class wrapper, a Vogen-style private constructor plus static `From` | succeeds; `SimpleType` is the wrapped type, `ValueProperty` reads it |
| `ValueTypeInfo.ForType` on `Guid`, `string`, `int`, or a struct with two properties | throws `InvalidValueTypeException` |
| A `Guid`-wrapping typed id on aggregates, an `[AggregateHandler]`, an inline snapshot, an async `SingleStreamProjection<TDoc, OrderId>`, `LoadAsync<TDoc>(OrderId)` and a Wolverine query | all work; the stream itself is keyed by the wrapped `Guid` |

So a wrong-kind id is caught by Marten in most calls but not all, and with a message that varies. The typed-id rules the harness needs are already in JasperFx (`JasperFx.Core.Reflection.ValueTypeInfo`), a public type in an assembly Marten brings in.

## Decision

- **One internal `StreamKey`** carries a stream id: a `Guid` or a `string`, plus a display label for step descriptions. Equality uses only the `Guid` or `string`, so a typed id and its wrapped value name the same stream. `ScenarioContext.StartedStreams`, `Snapshots`, `ISnapshottingStep.SnapshotIds`, `GivenStreamStep`, `StreamAssertionStep` and `StreamSnapshots` all use it. There is one code path per step, not a copy for each id kind.
- **`StreamKey` calls Marten's matching overload** (`StartStream`, `StartStream(Type, …)`, `Append`, `FetchStreamAsync`, `FetchStreamStateAsync`) for the kind it holds. `GivenStream<TAggregate>` uses `StartStream(typeof(TAggregate), …)` instead of a captured generic lambda.
- **Typed ids** are unwrapped once, when the step is recorded, with `ValueTypeInfo.ForType(typeof(TId))` and its `ValueProperty`. `Guid` and `string` given as `TId` are used as they are. A `TId` that `ValueTypeInfo` rejects, or whose `SimpleType` isn't `Guid` or `string`, throws `ArgumentException` from the builder call. A null id or a null wrapped string throws `ArgumentNullException`. The events factory still receives the original `TId`.
- **The id-kind check runs before the first step.** `ScenarioRunner` compares each stream step's key with `Store.Options.Events.StreamIdentity` and fails with a `HarnessAssertionException` labelled with that step if they disagree. Nothing has been written at that point, and the null-returning `FetchStreamStateAsync` can't be reached with the wrong kind.
- **Labels:** a `Guid` shows as before, a `string` in quotes (`"order-17"`), and a typed id as `TypeName(value)`.
- **SemVer:** new members on the stage interfaces are a minor change. The interfaces are what the harness returns; consumers call them and never implement them, which `reference/api.md` states.
- **Tests:** a fourth host, `StringIdHostFixture` (schema `harness_stringids`, no daemon), runs string-keyed scenarios. Typed ids that wrap a `Guid` run on the existing no-daemon host, and typed ids that wrap a `string` on the string host. Projection sync doesn't depend on the id kind, so the daemon paths aren't repeated. The typed ids are hand-rolled; Vogen and StronglyTypedId aren't added as test dependencies.

## Consequences

- Adding another id form later (for example a natural key) means one more `StreamKey` factory and the overloads, not new steps.
- The harness now relies on `ValueTypeInfo.ForType`, `SimpleType` and `ValueProperty` from JasperFx, and on `IReadOnlyEventStoreOptions.StreamIdentity`. They join the floor surface in [ADR-0018](./ADR-0018-critter-stack-floor-and-latest.md), and the compatibility run covers them.
- The suite has four hosts. [ADR-0003](./ADR-0003-real-postgres-test-hosts.md) is extended by this record.
