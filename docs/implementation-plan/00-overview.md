# Implementation plan: CritterStack.TestHarness

v1 is built: every todo in the original plan is done and CI is green. This plan records what v1 delivered and lists the candidate packets for what comes next. A packet that needs a decision not on record stops and gets a PDR or ADR written first ([ADR-0001](../architecture-decision-log/ADR-0001-decision-logs-design-and-plan.md)).

## Starting point (2026-11-17)

- Harness library: about 2,700 lines in `src/CritterStack.TestHarness/`, public API as in [design/01-harness-design.md](../design/01-harness-design.md#fluent-stage-interfaces).
- Test suite: 159 tests (about 3,600 lines), real Postgres via Testcontainers, three hosts (no daemon, daemon, poison daemon) ([ADR-0003](../architecture-decision-log/ADR-0003-real-postgres-test-hosts.md)). Doc samples and API coverage enforced ([ADR-0012](../architecture-decision-log/ADR-0012-docs-and-portability-enforced-by-tests.md)).
- CI (`build.yml`) green on GitHub for everything pushed. Packs `CritterStack.TestHarness` on every run and proves it installs into a consumer ([ADR-0013](../architecture-decision-log/ADR-0013-ship-the-harness-as-a-nuget-package.md)).
- Published on nuget.org: **1.3.3** (2026-11-17, first publish, manual) and **1.3.4** (2026-11-17, first automatic release, [ADR-0017](../architecture-decision-log/ADR-0017-release-automatically-on-push-to-main.md)). Each version has a matching `v*` tag and GitHub release.
- Marten, WolverineFx and Alba are a floor (9.44.0, 6.44.0, 8.5.3), the package's open-ended minimum, tested on the floor and on the latest releases (Marten 9.62.0, WolverineFx 6.64.2, Alba 9.0.0 on 2026-11-17) ([ADR-0018](../architecture-decision-log/ADR-0018-critter-stack-floor-and-latest.md), P04).
- No service uses the harness yet (P01).

## Packets

Status: **ready** (fully specified), **outline** (written in full when it is next in line), **needs decision** (a record must be written first). **Shipped in** is the first nuget.org version that contains the packet; a dash means it isn't done or doesn't change the package.

| Packet | Outcome | Implements | Depends on | Status | Shipped in |
|---|---|---|---|---|---|
| P01 First consumer | One real service (Mise, which runs Marten + Wolverine, [mise ADR-0012](https://github.com/ryandanthony/mise/blob/main/docs/architecture-decision-log/ADR-0012-marten-and-wolverine.md)) adds the package and has at least one scenario per handler type green in its CI | PDR-0010 | P03 (done) | outline, not started | - |
| P02 Tutorial walk-through | Someone new follows `tutorials/first-scenario.md` by hand from a clean clone; every gap found is fixed in the page and its `DocSamples/` test | PDR-0009 | none | outline, not started | - |
| P03 First publish | Resolve ADR-0013's **Open** item (repo visibility vs feed), add the nuget.org trusted-publishing policy, run the release workflow; `CritterStack.TestHarness` is installable | ADR-0013 | none | done 2026-11-17 | 1.3.3 |
| P04 Critter Stack upgrade | New Marten, Wolverine and Alba releases need no harness upgrade: the floor stays the package minimum, and every push to `main` plus a weekly run test the floor-built harness on the latest releases, opening a `compatibility` issue when one breaks | ADR-0018 (supersedes ADR-0002's pins), ADR-0007, 0008 | none | done 2026-11-17 | - (CI and test infrastructure only; the package's dependencies are unchanged) |
| P05 NuGet package | Packable project, `just pack`, `just package-check`, CI artifact and manual release job | PDR-0010, ADR-0013 | none | done | 1.3.3 |
| P06 String stream ids | `string` overloads of every stream method, second test host | new PDR superseding PDR-0008 | none | needs decision | - |
| P07 Ancillary stores | `UseStore<TStore>()` option, projection sync through `<TStore>` overloads | new PDR superseding PDR-0008 | none | needs decision | - |
| P08 HTTP endpoints | `WhenHttp`, `IHttpStage`, `HttpTenant`, tests and how-to | PDR-0011, ADR-0014 | none | done | 1.3.3 |
| P09 Docs site | Docusaurus site over `docs/` (`website/`, `just docs-site`), built on every docs change, deployed to Pages | PDR-0012, ADR-0015 | none | done, live at https://ryandanthony.github.io/critter-stack-fluent-test-harness/ | - (not in the package) |
| P10 Projection-error flake | Apply errors the daemon hits outside the forced catch-up fail the next action, so `ProjectionErrorTests` is stable on 1-CPU runners (CI was red on `main`) | ADR-0016 | none | done | 1.3.3 |
| P11 Automatic releases | Every push to `main` with a `feat`/`fix`/`perf`/breaking commit since the last tag publishes to nuget.org, tags and creates a GitHub release. GitVersion no longer burns numbers on docs commits. `main` protected | ADR-0017 | P03 | done | - (CI only; first automatic release was 1.3.4) |

1.3.4 adds one package change over 1.3.3: the nuget.org project URL points at the docs site (`c5808f9`).

### Outlines

- **P01 First consumer.** Follow `how-to/add-to-a-service.md` literally in the consuming repo. Anything the guide misses is a doc fix here. Anything the harness can't express is either a `HarnessOptions` field (ADR-0013) or a new record. Done when the consumer's CI runs the scenarios.
- **P02 Tutorial walk-through.** Today the tutorial is only proven by `FirstScenarioSamples`; nobody has read it as a newcomer.
- **P03 First publish.** Done: repo made public and renamed `critter-stack-fluent-test-harness`, trusted-publishing policy added, 1.3.3 published by a manual run on 2026-11-17.
- **P04 Upgrade.** Done differently from the outline. Bumping the pins would have raised every consumer's minimum to Alba 9 and Marten 9.62. Instead the pins became a floor plus a latest run ([ADR-0018](../architecture-decision-log/ADR-0018-critter-stack-floor-and-latest.md)). On 2026-11-17 all 159 tests and the consumer check passed on both 9.44.0 / 6.44.0 / 8.5.3 and 9.62.0 / 6.64.2 / 9.0.0 with no classifier or sync change. The only code change was one test fixture line, so it builds on Alba 8 and 9. Alba 9's breaking changes that touch the harness: the default status expectation is any 2xx, but `WhenHttp` calls `IgnoreStatusCode()` and checks 2xx itself, so it is unaffected. The exceptions moved to `Alba.Exceptions`, but the harness doesn't reference them.

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
