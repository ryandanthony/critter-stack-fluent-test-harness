# ADR-0014: WhenHttp runs the Alba scenario under Wolverine tracking

- **Status:** accepted
- **Date:** 2026-11-16
- **Deciders:** Ryan
- **Source:** runtime probe on the test host (Alba 8.5.3, WolverineFx and WolverineFx.Http 6.44.0), 2026-11-16

## Context

[PDR-0011](../product-decision-log/PDR-0011-scenarios-can-test-http-endpoints.md) adds HTTP requests to scenarios. Verified on the pinned versions:

| Case | What happens |
|---|---|
| `IAlbaHost.Scenario` inside `TrackActivity().ExecuteAndWaitAsync` | the request runs; a message the endpoint cascades shows up in `ITrackedSession.Sent` |
| Endpoint throws | the exception escapes `Scenario` as itself (not a 500) |
| Unknown route, or a GET endpoint returning null | 404 |
| Tenant header with `TenantId.IsRequestHeaderValue("tenant")` | the endpoint's session runs as that tenant |
| Alba status mismatch | `Alba.ScenarioAssertionException`, thrown from inside `Scenario` |
| Response body after `Scenario` returns | readable, and stays readable (`IScenarioResult.ReadAsTextAsync`, `ReadAsJsonAsync<T>`) |

## Decision

- New stage **`IHttpStage : IThenStage<IHttpStage>`** with `ThenStatusCode(int)`, `ThenResponse<TBody>(predicate)` and the command-stage assertions `ThenAppended`, `ThenNotAppended`, `ThenAppendedCount`, `ThenPublished`. `WhenHttp(Action<Alba.Scenario>)` is on `IGivenStage` and `IThenStage`, so it follows any stage ([ADR-0005](./ADR-0005-deferred-steps-and-staged-interfaces.md)). An internal `HttpStage` forwards to the one `ScenarioBuilder`, like `QueryStage`.
- **`WhenHttpStep`**: syncs projections if dirty ([ADR-0007](./ADR-0007-projection-sync-before-actions.md)), snapshots planned streams ([ADR-0006](./ADR-0006-appended-events-by-version-snapshot.md), now shared by `When` and `WhenHttp` through `ISnapshottingStep`), then runs `host.Scenario(...)` inside `TrackActivity(TrackingTimeout).ExecuteAndWaitAsync` and keeps both the `IScenarioResult` and the `ITrackedSession`.
- **Status is the harness's job.** The step calls `IgnoreStatusCode()` after the test's configuration. The runner's plan marks a `WhenHttp` that has a `ThenStatusCode` before the next action; an unmarked one fails at once on a status outside 200 to 299, with the body.
- **Classification** ([ADR-0008](./ADR-0008-exception-classification-by-origin.md)): an exception escaping the request is the endpoint's, captured for `ThenThrows`, except the tracked-session timeout wrapper after 90% of `TrackingTimeout` (infrastructure). Harness failures raised inside the request (request configuration or `HttpTenant` threw) are found through any wrapping and rethrown as themselves.
- **Tenancy:** `HarnessOptions.HttpTenant` (`Action<Alba.Scenario, string>`) is applied after the test's configuration under `ForTenant`. `ForTenant` without it fails at once.
- **Body reads** use the host's Alba JSON strategy (`ReadAsJsonAsync<T>`), so they match what the endpoint wrote. Failures show the raw body text.
- The step's label becomes `WhenHttp(<METHOD> <path><query>)` once the request is built.
- **Dependencies unchanged:** Alba was already a dependency; the package does not reference Wolverine.Http. Only the test project does, pinned at 6.44.0.

## Consequences

- `WhenHttp` needs an `IAlbaHost`; any other `IHost` fails with a clear message at that step.
- Endpoints that map exceptions to `ProblemDetails` answer with a status, so they're asserted with `ThenStatusCode`, not `ThenThrows`.
- A test that calls Alba's `StatusCodeShouldBe` inside `WhenHttp` has it overridden; the docs say to use `ThenStatusCode`.
