# ADR-0003: Test against real Postgres with Testcontainers and per-host schemas

- **Status:** accepted (extended by [ADR-0019](./ADR-0019-one-stream-key-for-guid-string-and-typed-ids.md): a fourth host with string stream ids; and by [ADR-0020](./ADR-0020-ancillary-stores-per-step-and-synced-together.md): ancillary stores on existing hosts, each in its own schema)
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Tests: fixtures), commits `b84d1af`, `01a7e35`

## Context

The harness is only as trustworthy as its proof against the real Critter Stack: the daemon, tracking, tenancy and serializer all behave differently against in-memory fakes, or don't exist there. The suite needs hosts with and without a daemon that don't interfere.

## Decision

- **One Postgres 17 Testcontainer** per test run, started by an xunit v3 assembly fixture (`PostgresContainerFixture`).
- An abstract **`HarnessHostFixture`** builds a real Alba host with Marten (conjoined event tenancy) and Wolverine (external transports disabled, handlers registered with `Discovery.IncludeType`, because `IncludeAssembly` didn't find them in the probe).
- Each host gets **its own schema** for documents, events and Wolverine envelopes (`harness_nodaemon`, `harness_daemon`, `harness_poison`), so hosts never see each other's data. Each host has its own xunit collection; collections run in parallel.
- The test host always sets **`ValidateScopes = true`**, so scoped-from-root bugs fail everywhere, not only under an IDE's Development environment (found when `WhenQuery` resolved the scoped `IMessageBus` from the root provider).
- At most **one host** runs the failing projection (`PoisonDaemonHostFixture`). Two hosts catching up a failing projection in one run made Marten report the error on only one, flaking about 1 in 5.
- Every test carries a **`business_value` trait** stating what it protects.

## Consequences

- `just test` needs Docker. CI's `ubuntu-latest` runner has it.
- The suite runs in about 15 seconds locally and stays deterministic (12 consecutive green runs after the poison-host change).
