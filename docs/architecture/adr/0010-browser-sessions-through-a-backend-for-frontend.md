# 0010. Browser sessions through a Backend for Frontend

- Status: Accepted
- Date: 2026-10-24

## Context

The web frontend needs to call LearnHub's APIs as the signed-in user. The APIs are bearer-only resource servers ([ADR 0009](0009-delegated-authentication-with-entra-external-id.md)), so someone must hold the user's access and refresh tokens. Where they live decides which browser attacks matter:

- Tokens that JavaScript can read, whether in memory or in `localStorage`, are stolen by any cross-site scripting (XSS) flaw and reused from another machine. A refresh token stolen this way extends access for as long as it lives.
- Tokens in a cookie the browser sends to the APIs on its own are safe from that theft, but make the APIs vulnerable to cross-site request forgery (CSRF), and CORS then has to allow credentials.

[RFC 10017](https://www.rfc-editor.org/rfc/rfc10017) (OAuth 2.0 for Browser-Based Applications, BCP 212) describes three architectures. In decreasing order of security they are a Backend for Frontend, a token-mediating backend, and an OAuth client running in the browser. LearnHub stores personal data and, later, payments.

## Decision

The web frontend talks to LearnHub through a **Backend for Frontend (BFF)**, served from the same origin as the frontend.

- The BFF is a confidential OpenID Connect client. It runs Authorization Code + PKCE, and stores and refreshes the tokens on the server.
- The browser receives only a session cookie: `__Host-` prefix, `HttpOnly`, `Secure`, `SameSite=Strict`. It never sees an OAuth token.
- The BFF protects its state-changing endpoints against CSRF: it requires a custom header or an antiforgery token, and checks `Origin`.
- The BFF forwards calls to the APIs with `Authorization: Bearer <access token>`.
- The APIs stay bearer-only. They never authenticate a cookie, so they need no antiforgery.
- CORS stays closed on the APIs. The browser only calls the BFF on its own origin, and BFF-to-API calls are server to server. Each API keeps an explicit, empty-by-default allowlist (`Cors:AllowedOrigins`) for deliberate exceptions such as a local development frontend, never with credentials.
- The frontend and BFF technology is chosen when the frontend is built. ASP.NET Core with YARP (MIT) and a server-side framework such as Next.js route handlers both fit. Duende BFF is commercial ([ADR 0007](0007-no-commercially-licensed-libraries.md)).

## Alternatives considered

- **Single-page app holding tokens in the browser (public client with PKCE):** simplest, with no backend and no CSRF, but any XSS reads the tokens. RFC 10017 ranks it least secure.
- **Tokens in an `HttpOnly` cookie sent straight to the APIs:** removes the token theft but makes every API cookie-authenticated. Each API would then need CSRF defenses and credentialed CORS.
- **Token-mediating backend:** the backend obtains the tokens but hands the access token to JavaScript, so XSS can still steal it.

## Consequences

- No OAuth token is ever readable by JavaScript. XSS can still use the open session from the page (session riding), so preventing XSS (output encoding, CSP) remains the main defense.
- One more deployable to build and run, with server-side session state to store and protect.
- The APIs stay simple: bearer tokens, no cookies, no antiforgery, CORS closed.
- Revisit if a client cannot use a same-origin backend (for example a third-party integration), or if the cost of running the BFF outweighs the risk it removes.
