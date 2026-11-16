# CritterStack.TestHarness documentation

The docs are split by what you need right now.

| You want to... | Read |
|---|---|
| Learn the harness by writing a test with it | [Tutorial](#tutorial) |
| Get a specific job done | [How-to guides](#how-to-guides) |
| Look up exactly what a method or option does | [Reference](#reference) |
| Understand why it works the way it does | [Explanation](#explanation) |

## Tutorial

- [Your first scenario](tutorials/first-scenario.md): write, break and extend a scenario against a small service.

## How-to guides

- [Add the harness to a service](how-to/add-to-a-service.md)
- [Choose a projection sync mode](how-to/choose-projection-sync.md)
- [Test expected exceptions](how-to/test-expected-exceptions.md)
- [Assert published messages](how-to/assert-published-messages.md)
- [Test multi-tenant handlers](how-to/test-multi-tenant-handlers.md)
- [Query read models](how-to/query-read-models.md)
- [Test HTTP endpoints](how-to/test-http-endpoints.md)
- [Debug a failing scenario](how-to/debug-a-failing-scenario.md)

## Reference

- [API](reference/api.md): every stage and method.
- [Harness options](reference/harness-options.md)
- [Failure messages](reference/failure-messages.md)
- [Compatibility](reference/compatibility.md): supported versions and v1 limits.

## Explanation

- [The execution model](explanation/execution-model.md)
- [Appended vs whole-stream assertions](explanation/appended-vs-whole-stream.md)
- [Projection sync](explanation/projection-sync.md)
- [Exception handling](explanation/exception-handling.md)
- [Design decisions](explanation/design-decisions.md)

Every code sample in the tutorial and how-to guides is a test in `src/CritterStack.TestHarness.Tests/DocSamples/`. The comment above each sample names its test.

## Changing the harness

Decisions, design and plan live alongside these pages: [product decisions](product-decision-log/README.md) (PDRs), [architecture decisions](architecture-decision-log/README.md) (ADRs), the [design](design/README.md) and the [implementation plan](implementation-plan/00-overview.md). See [docs/README.md](README.md) for how they fit together.
