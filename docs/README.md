# CritterStack.TestHarness docs

A fluent Given/When/Then test harness for Marten and Wolverine services. This folder holds two kinds of documentation.

## For people using the harness

User docs follow [Diátaxis](https://diataxis.fr/) ([PDR-0009](./product-decision-log/PDR-0009-diataxis-docs-with-runnable-samples.md)). Start at [index.md](./index.md). The same pages are published as a website built from this folder ([PDR-0012](./product-decision-log/PDR-0012-docs-published-as-a-website.md)), so keep them plain Markdown that reads well on GitHub too.

| Folder | Holds |
|---|---|
| [tutorials/](./tutorials/first-scenario.md) | Learning by doing: your first scenario |
| [how-to/](./index.md#how-to-guides) | Getting one job done |
| [reference/](./reference/api.md) | Every method, option and failure |
| [explanation/](./explanation/execution-model.md) | Why it behaves the way it does |

## For people changing the harness

Work is organized as decisions first, then design, then a plan to build it ([ADR-0001](./architecture-decision-log/ADR-0001-decision-logs-design-and-plan.md)), the same way as Mise and NACS:

| Folder | What it holds | Answers |
|---|---|---|
| [product-decision-log/](./product-decision-log/README.md) | Product Decision Records (`PDR-NNNN`) | _What_ the harness does for a test author, and what it deliberately does not |
| [architecture-decision-log/](./architecture-decision-log/README.md) | Architecture Decision Records (`ADR-NNNN`) | _How_ it is built, and why that way |
| [design/](./design/README.md) | Cohesive design unifying the PDRs and ADRs | How the parts fit together |
| [implementation-plan/](./implementation-plan/00-overview.md) | What v1 delivered, and the packets for what's next | In what order it gets built |

## Rules of the road

- **Decisions are records, not edits.** A changed decision gets a new record that supersedes the old one. Records are never rewritten to say something different.
- **File naming:** `{prefix}-{sequence}-{slug}.md`, where the prefix is `PDR` or `ADR`. Each log has its own sequence.
- **Statuses:** `proposed` (drafted, awaiting review), `accepted` (agreed; for PDRs this is direction that may not be built yet, for ADRs it is locked in), `superseded` (replaced, with a link to the record that replaced it).
- **Traceability:** design docs cite the PDRs/ADRs they rest on. Packets cite the design docs and records they implement.
- **User docs follow records.** If an `explanation/` page disagrees with a record, the record wins and the page is fixed in the same PR.
- **History:** the original build plan, `initial-design.md`, is now [design/01-harness-design.md](./design/01-harness-design.md). The version that drove v1 is at commit `01a7e35` (`git show 01a7e35:initial-design.md`).
