# Contributing to LearnHub

LearnHub is built like a team product: small pull requests, automated checks and a protected `main`.

## Prerequisites

- .NET SDK pinned in [`global.json`](global.json) (10.0.4xx)
- Docker, for Aspire resources (PostgreSQL) and the database tests (Testcontainers)

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
dotnet restore LearnHub.slnx --locked-mode
dotnet format LearnHub.slnx --verify-no-changes --no-restore
dotnet build LearnHub.slnx --configuration Release --no-restore
dotnet test --solution LearnHub.slnx --configuration Release --no-build
```

Fix formatting with `dotnet format LearnHub.slnx`.

## Conventions

- **Build:** shared settings in `Directory.Build.props`; warnings are errors; analyzers at `latest-recommended`.
- **Packages:** versions live only in `Directory.Packages.props` (Central Package Management). Never add `Version` to a `PackageReference`. After adding or changing a package, run `dotnet restore` and commit the updated `packages.lock.json` files; CI restores in locked mode and fails on a mismatch. Check a new package's exact name, owner and license (ADR 0007) before adding it.
- **Vulnerabilities:** NuGet Audit checks every package, transitive ones included. A moderate, high or critical advisory fails the build; low ones show up as Dependabot alerts. Fix it by upgrading, replacing or removing the package; never suppress the warning to get a green build. To look at it by hand: `dotnet list LearnHub.slnx package --vulnerable --include-transitive`.
- **GitHub Actions:** pinned by full commit SHA with the version as a comment (`uses: actions/checkout@<sha> # v7`). Dependabot proposes the updates.
- **Style:** `.editorconfig` is enforced at build time. LF line endings, file-scoped namespaces.
- **Tests:** test-first when behavior changes. Test names read as sentences: `Method_scenario_expected_result`.
- **Decisions:** architecture decisions are recorded as ADRs.

## Secrets

A secret never enters the repository, including `appsettings.Development.json`, examples and throwaway branches. If one leaks, **rotate or revoke it first**, then check where it was used, and only then clean the history: deleting the commit does not invalidate a copy someone already took. Push protection and secret scanning are enabled on the repository, but they are a safety net, not the rule.

| Where | Secrets live in | Notes |
|---|---|---|
| Repository | Nothing | Public configuration only: the Entra `Authority`, `ValidAudiences` and the JWKS are not secrets. |
| Local development | User Secrets of the AppHost (`dotnet user-secrets`), Aspire parameters declared with `AddParameter(name, secret: true)` | Stored outside the repository but **not encrypted**. Aspire injects them into the services as environment variables. |
| CI (GitHub Actions) | GitHub secrets, scoped to the job that needs them | Deploy to Azure through OIDC federated credentials, with no long-lived Azure credential stored in GitHub. |
| Azure | Key Vault, read with the service's Managed Identity | Prefer no secret at all: Managed Identity for Azure resources, certificates or federated credentials for Client Credentials. |

