# ADR-0001: Decision logs, design docs and implementation plan

- **Status:** accepted
- **Date:** 2026-11-16
- **Deciders:** Ryan
- **Source:** `mise` [ADR-0001](https://github.com/ryandanthony/mise/blob/main/docs/architecture-decision-log/ADR-0001-decision-logs-design-and-plan.md), `nats-auth-callout-service` `docs/`

## Context

The harness was designed and built from one 500-line plan, `initial-design.md`, that mixed decisions, API design, file lists, verification findings and a todo list. Its own Documentation section said the parts worth keeping should move out once v1 shipped. Only a short `explanation/design-decisions.md` did. Mise and NACS use a lighter, shared structure that works well: numbered decision records, a design that cites them, and a plan.

## Decision

- Project docs follow the Mise layout, alongside the Diátaxis user docs ([PDR-0009](../product-decision-log/PDR-0009-diataxis-docs-with-runnable-samples.md)):
  - `product-decision-log/`: `PDR-NNNN-<slug>.md`. _What_ the harness does for a test author, and doesn't.
  - `architecture-decision-log/`: `ADR-NNNN-<slug>.md`. _How_ it is built, and why.
  - `design/`: the cohesive design, citing both logs. `initial-design.md` moves here as `01-harness-design.md`.
  - `implementation-plan/`: `00-overview.md` with what v1 delivered and the packets for what's next.
- PDRs and ADRs each keep their own sequence. Records are immutable once `accepted`; a change is a new record that supersedes the old one.
- Records mined from the original plan cite it as `initial-design.md (<section>)`. The full plan as it stood at v1 is at commit `01a7e35` (`git show 01a7e35:initial-design.md`).
- User docs in `explanation/` keep the *why* for readers; records are the contract. When they disagree, the record wins and the page is fixed.

## Consequences

- One place to answer "why is it like this?", and the same habits in all three repos.
- `just decisions` lists records with statuses, and `just docs-check` (run in CI) catches broken relative links.
- Writing records is manual discipline. AGENTS.md and the PR template point at the logs.
