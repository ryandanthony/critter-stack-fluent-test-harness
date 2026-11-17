# Design decisions

The choices behind the harness, and what v1 deliberately leaves out. Each one is on record as a [product decision](../product-decision-log/README.md) or an [architecture decision](../architecture-decision-log/README.md), and the full design is in [`design/`](../design/README.md).

## One package, three dependencies

The harness ships as the `CritterStack.TestHarness` NuGet package, which depends on Alba, Marten and WolverineFx and nothing else. It knows nothing about any service's aggregates, projections, schemas or transports. Everything that varies between services is a `HarnessOptions` field, so a service adds the package and uses it as is. An earlier version had services copy the project into their repo instead; copies drifted and upgrades didn't reach them, so it became a package.

## Queries through Wolverine, requests through Alba

`WhenQuery` invokes the query handler through Wolverine, so a scenario can test a read model without caring how it is served. `WhenHttp` covers the other half: the endpoint, its routing, status codes and JSON, through the same Alba pipeline the service already tests with. Both live in the harness so one scenario can mix them. A request runs under Wolverine tracking like a command, so its appended events and cascaded messages can be asserted the same way.

The harness turns Alba's status check off and applies its own rule: a non-2xx fails at once unless a `ThenStatusCode` follows. That keeps the happy path short and makes error responses something a test states on purpose. The tenant isn't guessed: services read it from a header, a claim or the route, so a `ForTenant` scenario has to say how with `HttpTenant`, or fail.

## One tenant per scenario

`ForTenant` can only be called once, straight after `Setup`. Allowing the tenant to change between steps would make it unclear which tenant each assertion reads from. To compare tenants, write one scenario per tenant.

## No data wipe

The harness never resets or cleans the database. Truncating tables breaks parallel tests and other fixtures sharing the database. Fresh ids per test give the same isolation without the cost.

## Payloads in failure messages

Failures show event, response and message payloads as JSON, and keep the original exception. An earlier draft left payloads out in case they held personal data. That was dropped: the harness only runs in tests, and the data is what makes a failure quick to understand.

## Guid, string and typed stream ids

Stream methods take whatever id the service uses: a `Guid`, a `string` for a store with `StreamIdentity.AsString`, or a strongly typed id such as `OrderId` that wraps either. v1 took only `Guid`, since that covered the first services, and adding the others looked like it would double every step. It didn't: inside the harness every id becomes one internal stream key, so the steps are shared and only the public overloads multiply. A typed id is unwrapped with Marten's own value type rules, so whatever Marten accepts as an identifier works here too.

An id of the wrong kind fails before the scenario runs. Marten catches most of these itself, but not all: reading a stream's version with a string on a Guid store just returns nothing, so an appended-event assertion could quietly read the wrong events.

## Ancillary stores

v1 used only the main store. Services that split their data across `AddMartenStore<TStore>()` stores couldn't test those streams, and their async read models were never synced, because Marten's sync helpers for the main store don't touch ancillary stores. Now `InStore<TStore>()` picks the store for the stream steps that follow, and projection sync runs once per store in the scenario.

`InStore` is a step in the chain rather than an argument on every stream method. A flow that crosses stores reads in order, top to bottom, and the stream methods keep the signatures they had. The store stays chosen across `When` because the Thens after a command usually check the store the Givens wrote to.

Only the stores a scenario uses are synced. Syncing every registered store would make each action slower on hosts with several stores, and would let an unrelated store's projection error fail the scenario. `UseStore<TStore>()` covers the store a scenario touches only through handlers and queries.

## What v1 leaves out

- **Several tenants in one scenario.** See above.

See [compatibility](../reference/compatibility.md) for the exact limits.
