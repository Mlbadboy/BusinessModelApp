# Production readiness review

## What the application is intended to do

Business Model App is an operations dashboard for leadership teams. Its intended
workflow is to authenticate a user, show a portfolio-level business overview,
then let the user inspect revenue, expenses, strategy, business models, tasks,
agents, recommendations, and supporting administration data. The repository
uses a Clean Architecture-shaped .NET solution (Core, Infrastructure, API) and
a separate React/Vite client.

The API currently registers mock repositories and services. This is useful for
UI and controller development, but it means information is not durable and
should not be presented as customer financial data. The Infrastructure project
and its EF Core data context exist, but are not wired into the API startup.

## Verified user journey

1. The browser starts the React application, using the Vite proxy in development
   or a same-origin `/api` URL by default.
2. A visitor reaches the login or registration page. Successful authentication
   is expected to store a token and populate the `auth-status` query cache.
3. Protected pages render the application layout and dashboard only after a
   current user is available.
4. The dashboard queries financial performance, business health, revenue,
   expense, and strategy data and displays the resulting metrics.
5. Revenue, expense, and strategy pages offer drill-down views and status
   changes for opportunities, optimizations, and mitigations.

This is the intended workflow, not a production-ready workflow yet: the API has
no auth controller, the client contract does not match the registered API
routes, and the analytics dashboard endpoints are not exposed. The journey
therefore cannot complete against the checked-in backend.

## Changes made in this review

* The active application now uses one theme, localization provider, and React
  Query client (the ones mounted at the entry point), avoiding split query
  caches and duplicated developer tools.
* Environment configuration has safe defaults for the API base path, token
  storage key, app name, and mode. A missing environment variable no longer
  creates a local-storage key named `undefined`.
* Authentication mutations now hydrate or clear the same `auth-status` cache
  consulted by the route guard. They also navigate to the actual dashboard
  route (`/`) rather than the previously undefined `/dashboard` route.
* The obsolete duplicate API, provider, authentication context, and unrouted
  prototype component paths were removed rather than hidden from validation.
  TypeScript and ESLint now assess the complete remaining frontend source tree.
* Public routes no longer render the authenticated navigation shell. Visitors
  now see only the login or registration flow until authentication succeeds.

## Release blockers

### Critical — do not release

| Blocker | Evidence | Required resolution |
| --- | --- | --- |
| Authentication cannot work end-to-end. | The client calls login, register, logout, and current-user endpoints; no API auth controller or authentication handler is registered. | Implement an identity-backed auth controller, password hashing, JWT/cookie issuance, refresh/revocation, and integration tests. Do not use client-selected roles during registration. |
| The client and API use different contracts. | The routed client requests analytics, plural expense routes, and revenue trends/risks/opportunities, while the API exposes `api/business/revenue/*` and `api/business/expense/*`; strategy requires authorization. | Publish OpenAPI, generate a single typed client, and remove the duplicate hook/API layers. Add contract tests to CI. |
| Mock services are the runtime data source. | API startup registers `Mock*` services and repositories, while real persistence wiring is commented out. | Configure a production database, migrations, repositories, backups, tenancy boundaries, and a health/readiness endpoint before deployment. |
| Authorization is incomplete and inconsistent. | Authorization middleware is invoked but no authentication scheme is configured; several controller authorization attributes are commented out, while strategy endpoints are protected. | Define policies centrally, enforce them on every sensitive route, and test unauthenticated, forbidden, and authorized scenarios. |
| Financial data controls are absent. | No validation, audit persistence, concurrency control, reconciliation, or immutable financial record workflow is wired into API execution. | Establish data ownership, approval states, audit events, validation, idempotency keys, and reconciliation rules with finance stakeholders. |

### High priority before a pilot

* Replace direct numeric status mutations with server-side commands that record
  actor, reason, timestamp, version, and resulting audit event.
* Provide clear empty, error, and retry states per data panel; a single failed
  query must not make the whole dashboard unusable.
* Add input validation on both client and server, including currency precision,
  date ranges, identifiers, and password policy.
* Introduce structured logging, correlation IDs, metrics, tracing, exception
  monitoring, rate limits, and health/readiness checks.
* Define a deployment model: HTTPS termination, allowed production origins,
  secrets from a managed secret store, restrictive CSP/security headers, and
  dependency/container scanning.
* Build accessible interaction patterns: semantic tab labels, focus handling,
  keyboard testing, sufficient contrast, mobile layout testing, and useful
  no-data explanations.

## Scale and product strategy

1. **Create a reliable system of record first.** Model each financial event as
   an append-only transaction with organization ID, source, occurred-at time,
   currency, status, and audit metadata. Derive dashboard aggregates in a
   read model rather than letting UI cards become the source of truth.
2. **Define tenant and permission boundaries.** Every query and command should
   be scoped to an organization and permission policy. Executives need
   least-privilege capabilities, not role names accepted from a public form.
3. **Make integrations asynchronous and observable.** Use an outbox and a job
   queue for imports, exports, notifications, and AI analysis; make all jobs
   idempotent and expose progress to the user.
4. **Make recommendations explainable.** AI outputs should identify their data
   window, assumptions, confidence, owner, and human approval state. Never let
   an AI recommendation modify revenue, expense, or strategy records without a
   human-reviewed command.
5. **Make the dashboard decision-oriented.** Lead with a time range, data
   freshness, variance from plan, top drivers, and one next action. Let users
   drill from a KPI to the transactions and assumptions that created it.

## Delivery plan and acceptance criteria

### Milestone 1 — trustworthy vertical slice

Deliver login, organization scoping, one revenue-source CRUD workflow, and a
dashboard total backed by a real database. Acceptance criteria: a user can
create a source, refresh the browser, see the persisted value only in their
organization, and view an audit record; unauthenticated and unauthorized
requests are rejected.

### Milestone 2 — financial operations

Deliver expense entries, budgets, period close, exports, and reconciliation.
Acceptance criteria: monetary values use decimal precision; updates are
versioned; exports are authorization-scoped; and totals reconcile to source
records for a selected period and currency.

### Milestone 3 — strategy and intelligence

Deliver goals, measurable targets, approved actions, and explainable
recommendations. Acceptance criteria: status changes require the correct
permission, are audited, can be traced to inputs, and degraded AI/integration
services do not interrupt ordinary financial workflows.

### Milestone 4 — operational hardening

Add contract, unit, integration, accessibility, load, backup/restore, and
security tests to CI/CD. Define SLOs and run a production readiness review
using a non-mock environment before accepting real customer data.

## Current validation baseline

The complete remaining frontend source tree type-checks and passes lint after
these changes. A full Vite production build was started three times but the
execution environment stopped each run while Vite was transforming
dependencies, before it reported a result. The frontend test command currently
has no test files, so it exits non-zero; automated behavior tests must be added
before release. Backend build and tests could not be run because the .NET SDK
is unavailable.
