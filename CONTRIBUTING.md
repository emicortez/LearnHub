# Contributing to LearnHub

LearnHub is built like a team product: small pull requests, automated checks and a protected `main`.

## Prerequisites

- .NET SDK pinned in [`global.json`](global.json) (10.0.4xx)
- Docker, for Aspire resources and integration tests (from upcoming modules)

## Workflow (trunk-based)

1. Sync `main`: `git switch main && git pull`
2. Create a short-lived branch: `git switch -c feat/catalog-create-course`
   - Prefixes: `feat/`, `fix/`, `docs/`, `ci/`, `build/`, `chore/`, `refactor/`, `test/`
3. Commit small units with [Conventional Commits](https://www.conventionalcommits.org/): `feat(catalog): add course aggregate`
4. Run the same checks as CI before pushing (see below)
5. Push and open a pull request using the template
6. Merge with **squash and merge** once CI is green; the branch is deleted automatically

`main` must always be releasable. Branches live hours or a few days, not weeks.

## Local checks (same as CI)

```bash
dotnet restore LearnHub.slnx
dotnet format LearnHub.slnx --verify-no-changes --no-restore
dotnet build LearnHub.slnx --configuration Release --no-restore
dotnet test --solution LearnHub.slnx --configuration Release --no-build
```

Fix formatting with `dotnet format LearnHub.slnx`.

## Conventions

- **Build:** shared settings in `Directory.Build.props`; warnings are errors; analyzers at `latest-recommended`.
- **Packages:** versions live only in `Directory.Packages.props` (Central Package Management). Never add `Version` to a `PackageReference`.
- **Style:** `.editorconfig` is enforced at build time. LF line endings, file-scoped namespaces.
- **Tests:** test-first when behavior changes. Test names read as sentences: `Method_scenario_expected_result`.
- **Decisions:** architecture decisions are recorded as ADRs.
