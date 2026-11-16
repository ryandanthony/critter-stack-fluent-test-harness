# ADR-0005: Deferred steps behind staged interfaces

- **Status:** accepted
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Design: deferred execution, Fluent stage interfaces, Step ordering), [explanation/execution-model.md](../explanation/execution-model.md)

## Context

A scenario should read as one chain with a single `await` ([PDR-0001](../product-decision-log/PDR-0001-fluent-scenarios-on-the-services-own-host.md)), misordered steps should not compile, and appended-event assertions need information from steps that come *after* the `When` ([ADR-0006](./ADR-0006-appended-events-by-version-snapshot.md)).

## Decision

- Every fluent call **records a step** (`IScenarioStep`) and returns the next stage. Nothing runs until `RunAsync(ct)` or `await` (`GetAwaiter`). `ScenarioRunner` runs the steps in order with a shared `ScenarioContext`, honouring the cancellation token.
- Stages are **interfaces** that only offer valid next calls:
  - `Setup` returns `IScenarioStart` (`ForTenant`, once) which is an `IGivenStage` (`GivenStream`, `When`, `WhenQuery`).
  - `When` returns `ICommandStage`: whole-stream Thens, `ThenThrows`, plus `ThenAppended*` and `ThenPublished`.
  - `WhenQuery<TResponse>` returns `IQueryStage<TResponse>`: whole-stream Thens, `ThenThrows`, plus `ThenResult`.
  - Both extend `IThenStage<TSelf>`, which can go to another `When` or `WhenQuery`. No Givens after the first action.
  - Only `ICommandStage` and `IQueryStage` are runnable (`IScenario`), so a scenario with no action doesn't compile.
- One internal, non-generic **`ScenarioBuilder`** holds the step list and implements `IScenarioStart`, `IGivenStage` and `ICommandStage`. Each `WhenQuery<TResponse>` returns a small **`QueryStage<TResponse>`** that forwards to the same builder, because a scenario can run several queries with different response types.

## Consequences

- IntelliSense shows only valid steps; XML doc comments state the stage each method is valid on and returns.
- The runner can pre-scan the step list before anything executes.
- Exceptions surface at `await`, not at the call that recorded the step.
