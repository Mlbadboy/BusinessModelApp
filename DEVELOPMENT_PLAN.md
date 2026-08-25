# BusinessModelApp V2 Development Plan

## Product decision

**BusinessModelApp is an AI Business Operating System, not a collection of
dashboards and not a voice product.** Its purpose is to give each organization
one trustworthy view of performance, explain material changes, and turn
evidence-backed recommendations into accountable human actions. Voice is one
interface of the future Growth Agent.

The first objective remains engineering recovery: make the existing system
safe, testable, and usable. The second objective is product viability: a real
business must be able to onboard, understand its health quickly, trust the
numbers, and act on recommendations.

## Current baseline and release rule

The checked-in API registers mock services, does not provide an authentication
controller or authentication scheme, and does not expose the contract consumed
by the current frontend. Do not collect real customer or financial data until
Phases 1–6 acceptance criteria pass in a non-mock environment.

## Non-negotiable architectural rules

1. **Tenant first.** Every persistent business record has an `OrganizationId`;
   every query, command, cache key, export, event, and background job is scoped
   to it. A user belongs to an organization/workspace and has policy-based
   permissions.
2. **Ledger before dashboard.** Revenue, expenses, pipeline movements, and AI
   costs are immutable source events. Aggregates and health scores are derived
   read models, never the source of truth.
3. **Human authority.** AI may analyse, draft, rank, and recommend. It cannot
   change financial records, commitments, or customer communication without an
   explicit human-approved command and an audit event.
4. **One contract.** Publish OpenAPI from the API and generate one typed client.
   Do not maintain separate hand-written endpoint, DTO, or authentication
   implementations.
5. **Evidence is a feature.** Every insight and recommendation must expose its
   metric window, source records/documents, assumptions, confidence, expected
   impact, model/version, and suggested next action.
6. **Async where it matters.** Imports, exports, document indexing, AI work,
   notifications, and voice processing use idempotent queued jobs plus an
   outbox. SignalR is used only for valuable live status and notifications.

## Canonical product domain

```text
Organization → Workspace → Users / Roles / Permissions
      ↓
Business Profile → Customer / Lead → Interaction → Opportunity → Activity
      ↓                              ↓                  ↓
Knowledge Documents              Won / Lost         Proposal / Follow-up
      ↓                              ↓
Revenue Events ←────────────── Revenue Attribution
      ↓
Expenses / Budget / Cash / Forecast / Strategy / Risks
      ↓
Business Health → Insights → Recommendations → Human Decision → Outcome
```

`Lead`, `Interaction`, `Opportunity`, and `Activity` are distinct records:

* A **lead** is a potential customer.
* An **interaction** records any voice, email, web, WhatsApp, meeting, or SMS
  contact.
* An **opportunity** represents a qualified commercial outcome with amount,
  probability, stage, owner, and expected close date.
* An **activity** is an accountable next action linked to a lead or opportunity.

Voice, email, web, WhatsApp, and SMS are acquisition/interaction channels—not
parallel CRM systems.

## Phase 1 — Backend stabilization and security baseline

**Goal:** create a compiling, deterministic, secure API foundation.

* Resolve ID and repository-contract consistency; finish service and cache
  implementations; remove mock services from the production composition root.
* Configure dependency injection, validation, structured errors, health and
  readiness endpoints, migrations, secrets, logging, correlation IDs, and
  metrics.
* Implement authentication, policy-based authorization, organization context,
  audit events, input validation, rate limiting, and secure configuration
  before exposing any business endpoint.

**Exit criteria:** authenticated users cannot access another organization’s
data; an unauthorized request is rejected; every sensitive command has an
audit event; production startup never registers mock data services.

## Phase 2 — Frontend foundation and human workflow

**Goal:** make the browser workflow understandable and resilient.

* Keep one provider tree, typed client, design system, route guard, and error
  model; provide loading, empty, retry, and permission-denied states.
* Implement the onboarding journey: create organization, invite user, add
  business profile, select currency/time zone, import or enter first data.
* Validate accessibility, keyboard flow, mobile layout, data freshness labels,
  and task-oriented navigation.

**Product exit criteria:** a new organization can complete onboarding in under
10 minutes; a first-time user can identify the current health, top risk, top
opportunity, and next recommended action in under 30 seconds.

## Phase 3 — Database and canonical business domain

**Goal:** make business data durable, tenant-scoped, and explainable.

* Create migrations and constraints for organizations, memberships, leads,
  interactions, opportunities, activities, financial events, budgets, risks,
  recommendations, documents, and audit events.
* Use decimal monetary values with explicit currency, occurred-at timestamps,
  source/import identity, versions, soft-delete policy, and idempotency keys.
* Build read models for dashboard totals, trends, forecasts, pipeline, and
  health dimensions from source events.

**Exit criteria:** values survive a refresh and restore; records reconcile to
their source events for a selected period; imports and writes are idempotent.

## Phase 4 — API contract and integration security

**Goal:** expose a stable, safe product contract.

* Version the API; publish OpenAPI; generate the frontend client and contract
  tests; remove endpoint drift.
* Deliver one vertical slice end-to-end: revenue-source/event CRUD, aggregate,
  dashboard card, audit history, and authorization.
* Add pagination, filtering, sorting, optimistic concurrency, export
  authorization, secure upload scanning, and policy tests.

**Exit criteria:** a user can enter revenue, refresh, see the correct derived
total, inspect its evidence, and see no data outside their organization.

## Phase 5 — Frontend integration

**Goal:** connect the proven API contract to an understandable, resilient
browser workflow.

* Generate and use the typed API client; implement loading, empty, retry,
  permission-denied, and offline/degraded states.
* Deliver onboarding, revenue entry, dashboard drill-down, opportunity/action,
  audit-history, and authorized export workflows.
* Validate keyboard navigation, mobile layout, accessibility, data freshness,
  and task-oriented information architecture with representative users.

**Exit criteria:** a user can complete the organization onboarding flow, enter a
revenue event, refresh, understand the resulting dashboard change, inspect its
evidence, and complete the next assigned action without developer assistance.

## Phase 6 — Testing and production hardening

**Goal:** prove the product works, recovers, and protects data.

* Unit-test domain rules and score calculations; integration-test API,
  authorization, database migrations, and outbox; add frontend component and
  end-to-end onboarding-to-dashboard tests.
* Add accessibility, load, backup/restore, vulnerability, dependency, secret,
  and penetration testing; define SLOs, alerts, incident playbooks, and data
  retention/deletion processes.
* Apply security hardening: encryption in transit/at rest, secure sessions,
  CSP/headers, file controls, abuse limits, audit review, and AI data controls.

**Release gate:** all critical workflows, tenant isolation, backup/restore,
and security tests pass in a production-like non-mock environment.

## Phase 7 — Product Foundation and Business Intelligence

**Goal:** establish the signature product experience before agent interfaces.

Create a transparent **Business Health Score** with Revenue, Profitability,
Growth, Cash, Operations, and Risk dimensions. Each score must show formula,
period, target, drill-down evidence, and freshness—not a black-box AI number.

Create a daily **Executive Brief**:

* three improving signals;
* two signals needing attention;
* one opportunity worth investigation; and
* three ranked recommended actions.

**Exit criteria:** a user can ask “Why did revenue change?” and receive a
traceable answer; recommendations contain reason, sources, confidence, impact,
and a human-owned next action.

## Phase 8 — AI Growth Agent and voice interface

**Goal:** make voice a high-value Growth Agent channel, not a silo.

* Add call log, consent, streaming STT/TTS, qualification, human handoff,
  scheduling, lead/interactions memory, follow-up, and opportunity write-back.
* Use organization profile, consented lead history, existing opportunities, and
  approved knowledge as context; summarize every call with confidence and
  proposed next action.
* Attribute pipeline and revenue to interactions. Track calls, minutes, STT,
  TTS, tokens, LLM cost, total cost/call, cost/qualified lead,
  cost/opportunity, and cost/revenue generated.

**Exit criteria:** a qualified interaction reliably creates/updates the correct
lead, interaction, opportunity, activity, attribution, and audit records; a
human can approve handoff and follow-up.

## Phase 9 — AI Business Brain

**Goal:** connect specialized agents through the same governed context and
outcome loop.

* Build business memory from tenant-scoped structured data plus approved,
  indexed Business Knowledge documents (plans, reports, SOPs, pricing, product
  information).
* Implement an AI orchestrator and role-specific CFO, Strategy, Growth,
  Operations, and Executive agents behind common evidence, permission, cost,
  evaluation, and approval contracts.
* Record prompt/version, retrieved context IDs, model, token/cost, confidence,
  recommendation, human decision, and observed outcome for every AI run.

* CFO Agent: variance, cash, profitability, forecast.
* Strategy Agent: goals, risks, scenarios, recommendations.
* Growth Agent: leads, pipeline, conversion, attribution.
* Operations Agent: tasks, capacity, execution bottlenecks.
* Executive Agent: concise cross-domain brief and prioritized decisions.

No agent may bypass policies, evidence requirements, approval, or cost limits.

## Phase 10 — SaaS scale

**Goal:** operate securely and economically across organizations.

* Add workspace billing, usage metering, AI budget controls, observability,
  queues, caching, database scaling, rate limiting, and horizontal scale.
* Define tenant data residency, retention, export/deletion, support access,
  and incident procedures.

## Phase 11 — Omnichannel growth

**Goal:** add channels using the same Lead → Interaction → Opportunity model.

Integrate voice, WhatsApp, email, web, and SMS only after consent, attribution,
handoff, and follow-up behavior are proven in the Growth Agent.

## Delivery cadence

Each phase ends with a demo of the user outcome, automated tests, telemetry,
security review, and a rollback plan. Do not begin a phase solely because the
previous code compiles; begin it only when its exit criteria and data
governance obligations are met.
