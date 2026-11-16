# ADR-0012: Docs and portability are enforced by tests

- **Status:** accepted (the portability-check bullet is superseded by [ADR-0013](./ADR-0013-ship-the-harness-as-a-nuget-package.md))
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Documentation: keeping the docs accurate; Risks: verified portability, verified API reference completeness), commits `b5a30ca`, `1e46caa`

## Context

[PDR-0009](../product-decision-log/PDR-0009-diataxis-docs-with-runnable-samples.md) promises runnable samples and a complete reference, and [PDR-0006](../product-decision-log/PDR-0006-adopt-by-copying-the-project.md) promises a project that works when copied. Both rot silently unless something fails when they drift.

## Decision

- **`DocSamples/`** in the test project holds a test for every C# block in `docs/tutorials/` and `docs/how-to/`. Each block carries a `<!-- Test: ... -->` marker naming its test.
- **`DocSampleSyncTests`** fails if a line of a C# sample block is missing from the `DocSamples/` sources, or a marker names a test that doesn't exist. Plain-text blocks (example failure messages) aren't line-checked; their tests assert the message shape.
- **`ApiReferenceCoverageTests`** reflects over the harness and fails if any public type, member or enum value is missing from `docs/reference/api.md` or `harness-options.md`. It checks presence, not wording.
- **`scripts/check-portability.sh`** (`just portability`, in CI) copies the harness sources into a throwaway directory with its own `Directory.Packages.props`, `nuget.config` (nuget.org only) and no build props, then runs a separate sample service (`samples/ConsumerService`, an Order domain) against it.

Each check was proven by breaking it on purpose and watching it fail.

## Consequences

- Changing a sample means changing its test, and vice versa.
- Reference wording and XML comments can still disagree; update both together.
