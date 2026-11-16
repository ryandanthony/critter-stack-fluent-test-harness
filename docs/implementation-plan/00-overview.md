# Implementation plan: CritterStack.TestHarness

v1 is built: every todo in the original plan is done and CI is green. This plan records what v1 delivered and lists the candidate packets for what comes next. A packet that needs a decision not on record stops and gets a PDR or ADR written first ([ADR-0001](../architecture-decision-log/ADR-0001-decision-logs-design-and-plan.md)).

## Starting point (2026-11-16)

- Harness library: about 2,100 lines in `src/CritterStack.TestHarness/`, public API as in [design/01-harness-design.md](../design/01-harness-design.md#fluent-stage-interfaces).
- Test suite: 135 tests (about 3,300 lines, 12 s locally), real Postgres via Testcontainers, three hosts (no daemon, daemon, poison daemon) ([ADR-0003](../architecture-decision-log/ADR-0003-real-postgres-test-hosts.md)). Doc samples and API coverage enforced ([ADR-0012](../architecture-decision-log/ADR-0012-docs-and-portability-enforced-by-tests.md)).
- CI (`build.yml`) green on GitHub for everything pushed. No tags, no releases, no package ([PDR-0006](../product-decision-log/PDR-0006-adopt-by-copying-the-project.md)).
- No service has copied the harness in yet.

## Packets

Status: **ready** (fully specified), **outline** (written in full when it is next in line), **needs decision** (a record must be written first).

| Packet | Outcome | Implements | Depends on | Status |
|---|---|---|---|---|
| P01 First consumer | One real service (Mise, which runs Marten + Wolverine, [mise ADR-0012](https://github.com/ryandanthony/mise/blob/main/docs/architecture-decision-log/ADR-0012-marten-and-wolverine.md)) copies the harness in and has at least one scenario per handler type green in its CI | PDR-0006 | none | outline |
| P02 Tutorial walk-through | Someone new follows `tutorials/first-scenario.md` by hand from a clean clone; every gap found is fixed in the page and its `DocSamples/` test | PDR-0009 | none | outline |
| P03 Tag v0.1.0 | First `v` tag so GitVersion and the README can name a version consumers copied | ADR-0011 | P01 | outline |
| P04 Critter Stack upgrade | Bump Marten, Wolverine and Alba to current; any classifier or sync behaviour change caught by tests is handled and recorded (new ADR superseding ADR-0002 if pins change materially) | ADR-0002, 0007, 0008 | none | outline |
| P05 NuGet package | Pack and publish the harness, superseding PDR-0006 | new PDR | P01 | needs decision |
| P06 String stream ids | `string` overloads of every stream method, second test host | new PDR superseding PDR-0008 | none | needs decision |
| P07 Ancillary stores | `UseStore<TStore>()` option, projection sync through `<TStore>` overloads | new PDR superseding PDR-0008 | none | needs decision |

### Outlines

- **P01 First consumer.** Follow `how-to/add-to-a-service.md` literally in the consuming repo. Anything the guide misses is a doc fix here. Anything the harness can't express is either a `HarnessOptions` field (ADR-0004) or a new record. Done when the consumer's CI runs the scenarios.
- **P02 Tutorial walk-through.** Today the tutorial is only proven by `FirstScenarioSamples`; nobody has read it as a newcomer.
- **P03 Tag.** `git tag v0.1.0` on `main` after P01, so the copied source can be traced to a version. No release job yet.
- **P04 Upgrade.** Run `dotnet list package --outdated`, bump `Directory.Packages.props` and `samples/ConsumerService/Directory.Packages.props.template` together, run `just test` and `just portability`, update [reference/compatibility.md](../reference/compatibility.md).

## v1 build log

The todos that drove v1, from the original plan (`git show 01a7e35:initial-design.md`, section Todos). All done.

| Todo | Done when | Result |
|---|---|---|
| verify-apis | Every Marten/Wolverine/Alba API in the design confirmed for the pinned versions | Done 2026-10-01 via Context7, reflection and a runtime probe. Changed the design in seven places: two-path `CatchUp`, the classifier table, `WolverineFx.RuntimeCompilation`, the `ExecuteAndWaitAsync` lambda cast, `IncludeType` discovery, `ThenPublished` reading `Sent`, `WhenQuery` type-checking the response |
| scaffold | `global.json`, `nuget.config`, central packages, build props/targets, `stylecop.json`, slnx, `justfile`; `just build` green on an empty solution | Done |
| project | Harness (Alba, Marten, WolverineFx only) and test project in the slnx | Done |
| stages | Public API with full XML docs; Target API compiles, misordered chain doesn't, no CS1591 | Done |
| core | Builder, runner, context, tenant helpers, projection sync, classifier, failure formatter | Done |
| steps | Given, When, WhenQuery, stream/appended, throws, published, result steps | Done (appended lives in `StreamAssertionStep` with `StreamScope.Appended`) |
| test-domain | `Widget` aggregate, events, commands, queries, inline and async projections | Done, plus `FailWidgetLater`, `TimeOutWidget`, `RecordTenant`, `UnhandledCommand`, `FindWidget` |
| fixtures | Postgres assembly fixture, per-host schemas, daemon and no-daemon hosts and collections | Done |
| tests | Every case in the design's Tests list, each with `business_value` | Done: 75 tests at v1, green three runs in a row in about 15 s |
| versioning | `GitVersion.yml` and `GitVersion.MsBuild`; informational version `{SemVer}+{ShortSha}` | Done |
| ci | `build.yml` builds, prints SemVer, runs tests, uploads trx | Done |
| docs-reference | `reference/` covers every public member and failure | Done, enforced by `ApiReferenceCoverageTests` |
| docs-how-to | Seven how-to guides, every block backed by a test | Done |
| docs-tutorial | Tutorial backed by tests, walked from a clean clone | Done by tests and CI; not yet by a human reader (P02) |
| docs-explanation | Five explanation pages | Done |
| docs-entry | `docs/index.md`, README, compass check | Done |
| verify | `just build` and `just test` green, CI green | Done (version 0.1.6 at the time) |

After v1 (2026-10-01 to 2026-10-02): portability fix and `HarnessAssertionException` rename (`1e46caa`), scoped `IMessageBus` fix (`b84d1af`), and coverage work driven by dotCover and fault injection (failure, cancellation, guard, daemon-error and snapshot-planning paths).

2026-11-16: project docs moved to PDR/ADR logs, design and this plan ([ADR-0001](../architecture-decision-log/ADR-0001-decision-logs-design-and-plan.md)).
