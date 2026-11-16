# PDR-0009: User docs follow Diátaxis, and every sample runs

- **Status:** accepted (the GitHub-only rendering and deferred site generator are superseded by [PDR-0012](./PDR-0012-docs-published-as-a-website.md))
- **Date:** 2026-10-01
- **Deciders:** Ryan
- **Source:** `initial-design.md` (Documentation)

## Context

People adopting the harness arrive with different needs: learning it, getting one job done, looking up a method, or understanding why it behaves a certain way. Mixing those on one page serves none of them. Docs with code samples also rot silently when the API changes.

## Decision

- User docs live in `docs/` as plain Markdown that renders on GitHub, split by [Diátaxis](https://diataxis.fr/): `tutorials/`, `how-to/`, `reference/`, `explanation/`, with [index.md](../index.md) as the entry.
- Every C# sample in the tutorial and how-to guides is **a passing test**, and the page names it.
- The reference covers **every public type, member and failure** of the harness, and IntelliSense (XML doc comments) carries the same step-ordering rules.
- After v1, any change to the harness updates the affected pages **in the same pull request**.

## Alternatives considered

- **README only.** Rejected: too much behaviour (sync modes, exception rules) to fit.
- **Docs site generator.** Deferred: GitHub-rendered Markdown is enough for now.

## Consequences

- Enforcement is mechanical, not by review ([ADR-0012](../architecture-decision-log/ADR-0012-docs-and-portability-enforced-by-tests.md)).
- Project docs (these logs, design, plan) are separate from user docs; see [docs/README.md](../README.md).

## Non-goals

- Checking that reference wording matches XML comments. Only presence is checked.
