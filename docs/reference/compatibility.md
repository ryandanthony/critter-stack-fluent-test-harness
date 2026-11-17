# Compatibility

## Supported versions

The package depends on Alba, Marten and WolverineFx with a minimum version and **no upper bound**. Any newer release, minor or major, installs. Your service's own versions win.

| Dependency | Minimum (floor) | Also tested on |
|---|---|---|
| .NET | `net10.0` | |
| Marten | 9.44.0 | latest stable |
| WolverineFx / WolverineFx.Marten | 6.44.0 | latest stable |
| Alba | 8.5.3 | latest stable (9.x included) |
| PostgreSQL (tests) | 17 | |

Every build runs the full suite and the packaged-consumer check on the minimums. A separate compatibility run repeats both against the newest stable Marten, WolverineFx and Alba on nuget.org, on every push to `main` and every Monday. When a new release breaks the harness, that run opens an issue labelled `compatibility` ([ADR-0018](../architecture-decision-log/ADR-0018-critter-stack-floor-and-latest.md)). Last verified on 2026-11-17 against Marten 9.62.0, WolverineFx 6.64.2 and Alba 9.0.0.

The harness library references Alba, Marten and WolverineFx only. A host running WolverineFx 6.44 or later in the default `TypeLoadMode.Dynamic` also needs `WolverineFx.RuntimeCompilation`. That is the host's dependency, not the harness's.

`WhenHttp` works with any endpoint Alba can call. Its tests run against WolverineFx.Http endpoints at the same versions as WolverineFx. The package does not depend on WolverineFx.Http.

## Limits

| Not supported | What it would need |
|---|---|
| Ancillary document stores (`IDocumentStore` subtypes) | A generic `UseStore<TStore>()` option so projection sync can call the `<TStore>` overloads of the Marten host extensions. |
| More than one tenant per scenario | Run one scenario per tenant. |
| Strongly typed ids that wrap anything but a `Guid` or `string` | Nothing: Marten's stream identity is one or the other. |

The harness always uses the host's main `IDocumentStore`. Stream ids can be a `Guid`, a `string` or a strongly typed id that wraps either, matching the store's `Events.StreamIdentity` ([PDR-0013](../product-decision-log/PDR-0013-string-and-strongly-typed-stream-ids.md)).

## Behaviour that depends on the Critter Stack version

These were verified against the minimum versions and are covered by tests, which run on both the minimums and the latest releases, so a release that changes them fails the build or the compatibility run:

- `ForceAllMartenDaemonActivityToCatchUpAsync` needs a registered `IProjectionCoordinator`.
- `WaitForNonStaleProjectionDataAsync` always times out without a daemon.
- A top-level handler exception is rethrown as-is under tracking. A cascaded one is wrapped in `AggregateException`.
- A tracking timeout and a handler's own `TimeoutException` both arrive wrapped in "This TrackedSession timed out…".
- A missing handler throws `IndeterminateRoutesException`.
- `InvokeAsync<T>` returns `default(T)` instead of throwing when the handler returns a different type.
- `FetchStreamStateAsync` with a `string` id on a `Guid` store returns null instead of throwing, which is why the harness checks the id kind itself.
- Strongly typed ids are unwrapped with JasperFx's `ValueTypeInfo`, the same rules Marten uses for value type identifiers.
