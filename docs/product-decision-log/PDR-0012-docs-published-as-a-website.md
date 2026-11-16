# PDR-0012: User docs are published as a website

- **Status:** accepted
- **Date:** 2026-11-16
- **Deciders:** Ryan
- **Supersedes:** the "Docs site generator. Deferred" alternative of [PDR-0009](./PDR-0009-diataxis-docs-with-runnable-samples.md), and its "renders on GitHub (no site generator in v1)" wording

## Context

[PDR-0009](./PDR-0009-diataxis-docs-with-runnable-samples.md) kept the docs as GitHub-rendered Markdown and deferred a site. With the harness becoming a NuGet package ([PDR-0010](./PDR-0010-adopt-as-a-nuget-package.md)), people who install it need somewhere to read the docs that isn't the source repo: with search, a sidebar that shows the Diátaxis split, and a stable URL to put on the package page. NACS and awen already publish Docusaurus sites the same way.

## Decision

- The docs in `docs/` are published as a **website**, with the Diátaxis sections as the sidebar under "Using the harness" and the decision logs, design and plan under "Project".
- **One source.** The site reads `docs/` as it is. Pages are still plain Markdown that also reads well on GitHub. Nothing is copied or generated into a second docs tree.
- A **broken internal link or anchor fails the build**, so the site can't ship with dead links.
- The site is rebuilt on every change to `docs/` and published from `main`.
- The package's project URL points at the site once it's live.

## Alternatives considered

- **GitHub-rendered Markdown only** ([PDR-0009](./PDR-0009-diataxis-docs-with-runnable-samples.md)). Rejected now: no search or navigation, and not a good landing page for package users.
- **A separate `docs-site/docs` tree** (as NACS has). Rejected: two copies drift. NACS needs a separate tree because its user docs differ from its design docs; here they already live together.

## Consequences

- Build and hosting in [ADR-0015](../architecture-decision-log/ADR-0015-docusaurus-site-on-github-pages.md).
- Markdown in `docs/` must stay valid CommonMark for both GitHub and the site.

## Non-goals

- Versioned docs per package release. One current version until there's a need.
- A blog or changelog site. Release notes stay on GitHub releases.
