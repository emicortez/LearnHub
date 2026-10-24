# 0009. Delegated authentication with Microsoft Entra External ID

- Status: Accepted
- Date: 2026-10-24

## Context

LearnHub needs sign-up and sign-in for students, instructors, and admins. The product requires both Google sign-in and local accounts with an email and password the user chooses ([vision](../../product/vision.md)). Storing passwords safely means slow salted hashing, lockout, email verification, password reset, breached-password checks, and MFA. That is a lot of security-critical work, and none of it is what LearnHub is about.

Several services (Catalog, Enrollment, and the AI Tutor) must know who the caller is without sharing a session store. The constraints:

- no commercially licensed libraries ([ADR 0007](0007-no-commercially-licensed-libraries.md));
- at most USD 20 per month on Azure, free tier first ([quality attributes](../quality-attributes.md));
- one person operating the system.

## Decision

Authentication is delegated to an OpenID Connect identity provider: **Microsoft Entra External ID** in every real environment.

- The provider owns every credential: local accounts with email and password, and Google through federation. LearnHub never sees a password.
- Clients sign in with Authorization Code + PKCE. Services authenticate to each other with Client Credentials.
- Each service is a resource server. It validates RS256 access tokens on its own through OIDC discovery and the provider's JWKS. The issuer, audience, algorithm, and lifetime are checked with a 30-second clock skew.
- Services depend only on the standards (OIDC, JWT), never on a provider SDK, so changing the provider only changes configuration.
- The user id is the `oid` claim. In Entra the `sub` claim is pairwise: the same user gets a different `sub` in each application, so Catalog and Enrollment would not agree on it. The claim name lives in one place per service.
- Tests never call the provider. They issue their own tokens, signed with a test key.

## Alternatives considered

- **Keycloak (self-hosted, open source):** it has every feature we need, and Aspire can run it locally. It is not chosen because we would have to operate it (security patches, backups, a PostgreSQL database). It also has to stay running, since scaling a JVM identity server to zero makes sign-in slow, and an always-on container plus a database would take most of the budget. This is the documented fallback: switching means a new `Authority`, and `sub` as the user id claim.
- **Auth0, Okta, Clerk:** free tiers are limited and the price grows fast with users.
- **Duende IdentityServer:** commercial license (ADR 0007).
- **ASP.NET Core Identity in a service of our own:** a library, not an identity provider. We would store passwords and still have to build the OIDC server.

## Consequences

- No password storage, and no identity server to patch or back up. The core of Entra External ID is free up to 50,000 monthly active users.
- Sign-in depends on Microsoft's availability, and the tenant is configured in a portal rather than in code.
- Moderate lock-in, limited by depending only on standards. The `oid` user id is specific to Entra.
- Local development needs a tenant before authenticated endpoints can be called; tests do not.
- Revisit if the user count approaches the free tier limit, if a required feature such as custom claims is unavailable, or if the budget allows running Keycloak.
