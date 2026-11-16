# Compatibility

## Supported versions

Built and tested against:

| Dependency | Version |
|---|---|
| .NET | `net10.0` |
| Marten | 9.44.0 |
| WolverineFx / WolverineFx.Marten | 6.44.0 |
| Alba | 8.5.3 |
| PostgreSQL (tests) | 17 |

The harness library references Alba, Marten and WolverineFx only. A host running WolverineFx 6.44 in the default `TypeLoadMode.Dynamic` also needs `WolverineFx.RuntimeCompilation`; that is the host's dependency, not the harness's.

`WhenHttp` works with any endpoint Alba can call. Its tests run against WolverineFx.Http 6.44.0 endpoints; the package does not depend on WolverineFx.Http.

## v1 limits

| Not supported | What it would need |
|---|---|
| String stream ids (`StreamIdentity.AsString`) | `string` overloads of every stream method, and a test host configured for string ids. |
| Ancillary document stores (`IDocumentStore` subtypes) | A generic `UseStore<TStore>()` option so projection sync can call the `<TStore>` overloads of the Marten host extensions. |
| More than one tenant per scenario | Run one scenario per tenant. |

The harness always uses the host's main `IDocumentStore` and Guid stream ids.

## Behaviour that depends on the Critter Stack version

These were verified against the versions above and are covered by tests, so an upgrade that changes them fails the build:

- `ForceAllMartenDaemonActivityToCatchUpAsync` needs a registered `IProjectionCoordinator`.
- `WaitForNonStaleProjectionDataAsync` always times out without a daemon.
- A top-level handler exception is rethrown as-is under tracking; a cascaded one is wrapped in `AggregateException`.
- A tracking timeout and a handler's own `TimeoutException` both arrive wrapped in "This TrackedSession timed out…".
- A missing handler throws `IndeterminateRoutesException`.
- `InvokeAsync<T>` returns `default(T)` instead of throwing when the handler returns a different type.
