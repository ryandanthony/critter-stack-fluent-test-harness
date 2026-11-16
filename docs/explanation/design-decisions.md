# Design decisions

The choices behind the harness, and what v1 deliberately leaves out. Each one is on record as a [product decision](../product-decision-log/README.md) or an [architecture decision](../architecture-decision-log/README.md), and the full design is in [`design/`](../design/README.md).

## One package, three dependencies

The harness ships as the `CritterStack.TestHarness` NuGet package, which depends on Alba, Marten and WolverineFx and nothing else. It knows nothing about any service's aggregates, projections, schemas or transports. Everything that varies between services is a `HarnessOptions` field, so a service adds the package and uses it as is. An earlier version had services copy the project into their repo instead; copies drifted and upgrades didn't reach them, so it became a package.

## Queries through Wolverine, not HTTP

`WhenQuery` invokes the query handler through Wolverine. HTTP concerns (routing, status codes, serialization) belong in endpoint tests, which Alba already does well. Keeping them apart means a scenario tests behaviour, not transport.

## One tenant per scenario

`ForTenant` can only be called once, straight after `Setup`. Allowing the tenant to change between steps would make it unclear which tenant each assertion reads from. To compare tenants, write one scenario per tenant.

## No data wipe

The harness never resets or cleans the database. Truncating tables breaks parallel tests and other fixtures sharing the database. Fresh ids per test give the same isolation without the cost.

## Payloads in failure messages

Failures show event, response and message payloads as JSON, and keep the original exception. An earlier draft left payloads out in case they held personal data. That was dropped: the harness only runs in tests, and the data is what makes a failure quick to understand.

## What v1 leaves out

- **String stream ids.** Supporting them doubles every stream method and needs a second test host. Guid ids cover the services this was built for.
- **Ancillary document stores.** Projection sync would need to know the store's type to call the right Marten overloads. A `UseStore<TStore>()` option could add this later.
- **Several tenants in one scenario.** See above.

See [compatibility](../reference/compatibility.md) for the exact limits.
