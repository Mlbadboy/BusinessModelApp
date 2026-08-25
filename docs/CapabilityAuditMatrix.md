# Capability Regression & Deletion Audit Matrix

This document tracks all frontend and backend capabilities, components, and API surfaces across the lifecycle of **BusinessModelApp (AI Business Operating System)**.

Its purpose is to ensure that no functionality is lost during modernization and that every modified or removed file has an explicit classification, replacement target, and status.

---

## Classification Taxonomy

- **`[REPLACED]`**: Re-implemented with typed contracts, modern UI, and non-mock backend services.
- **`[DEFERRED]`**: Explicitly scheduled for a defined future milestone with an architectural reason.
- **`[OBSOLETE]`**: Retired because the underlying concept was superseded or duplicated.

---

## 1. Frontend Component Audit Matrix

| Original Component / File | Historical Capability | Classification | Target Phase / Replacement Module | Status |
|---|---|---|---|---|
| `AnalyticsDashboard.tsx` | Portfolio-level business KPIs and health charts | `[REPLACED]` | **Phase 7 (Business Intelligence)**: Evidence-grounded Health Score & Executive Brief | In Progress |
| `BusinessHealthOverview.tsx` | Synthetic health status card | `[REPLACED]` | **Phase 7 (Business Intelligence)**: Six-dimensional calculated scorecard with drilldowns | Planned |
| `FinancialMetricsCard.tsx` | Top-level revenue and profit summaries | `[REPLACED]` | **Phase 7 (Business Intelligence)**: Unified KPI widget backed by real database metrics | Planned |
| `FinancialPerformance.tsx` | Historical revenue vs expense chart | `[REPLACED]` | **Phase 7 (Business Intelligence)**: Recharts-based financial trends with confidence intervals | Planned |
| `RevenueDashboard.tsx` | Revenue sources & opportunity cards | `[DEFERRED]` | **Commercial Lifecycle Phase**: Opportunity $\rightarrow$ Revenue Stream conversion | Scheduled |
| `RevenueSourceList.tsx` | Table of revenue streams | `[DEFERRED]` | **Commercial Lifecycle Phase**: Unified Revenue Stream management | Scheduled |
| `RevenueTrends.tsx` | Revenue forecasting & growth trajectory | `[DEFERRED]` | **Phase 7 / Growth Agent**: Predictive revenue attribution | Scheduled |
| `ExpenseDashboard.tsx` | Expense breakdowns & budget limits | `[DEFERRED]` | **Financial Controls Phase**: Budgeting, approvals, and reconciliation | Scheduled |
| `ExpenseCategoryList.tsx` | Category-level expense manager | `[DEFERRED]` | **Financial Controls Phase**: Normalized cost center categorization | Scheduled |
| `ExpenseTrends.tsx` | Cost inflation & run-rate trends | `[DEFERRED]` | **Financial Controls Phase**: Anomaly detection & margin compression alerts | Scheduled |
| `StrategyDashboard.tsx` | Strategic initiatives & goals | `[DEFERRED]` | **Phase 9 (Strategy Agent)**: Multi-agent strategic planning with approval gates | Scheduled |
| `RiskManagement.tsx` | Risk matrix & mitigation tracking | `[DEFERRED]` | **Phase 9 (Strategy Agent)**: Autonomous risk monitoring with impact calculation | Scheduled |
| `PerformanceTracking.tsx` | Goal & KPI milestone tracker | `[DEFERRED]` | **Phase 9 (Strategy Agent)**: Milestone delivery & velocity tracking | Scheduled |
| `LoginForm.tsx` | Login input modal | `[REPLACED]` | **Phase 1-2 (Identity Slice)**: Routed Auth page with JWT & tenant session storage | Completed |
| `UnauthorizedPage.tsx` | 403 Forbidden screen | `[REPLACED]` | **Phase 1-2 (Identity Slice)**: Unified route guard with role & workspace permissions | Completed |
| `AuthGuard.tsx` / `AuthContext.tsx` | Ad-hoc React context authentication | `[REPLACED]` | **Phase 1-2 (Identity Slice)**: Single source-of-truth `useAuth` hook + React Query cache | Completed |
| `Sidebar.tsx` | Navigation sidebar | `[REPLACED]` | **Phase 1-2 (Layout)**: Clean responsive App layout with tenant/workspace switcher | Completed |
| `Settings/index.tsx` | Placeholder settings screen | `[DEFERRED]` | **Workspace Governance Phase**: Organization, Workspace & API key settings | Scheduled |
| `BusinessModels/index.tsx` | Static canvas placeholder | `[DEFERRED]` | **Business Canvas Phase**: Interactive 9-block Osterwalder canvas editor | Scheduled |

---

## 2. API & Data Layer Audit Matrix

| Original API / Hook | Historical Capability | Classification | Replacement / Architecture | Status |
|---|---|---|---|---|
| `src/api/QueryProvider.tsx` | Duplicate React Query provider | `[OBSOLETE]` | Single `QueryClientProvider` mounted at application root (`main.tsx`) | Completed |
| `src/api/client.ts` | Axios instance with unverified base URLs | `[REPLACED]` | Typed OpenAPI client with environment configuration (`src/config/env.config.ts`) | Completed |
| `src/api/hooks/useAnalytics.ts` | Untyped analytics hooks | `[REPLACED]` | Generated client query hooks for `/api/analytics` | In Progress |
| `src/api/hooks/useAuth.ts` | Mock token generation hook | `[REPLACED]` | Real ASP.NET Core Identity `/api/auth` endpoints + JWT issuance | In Progress |
| `src/api/hooks/useRevenue.ts` | Mock revenue queries | `[DEFERRED]` | Generated client hooks for `/api/commercial/revenue` | Scheduled |
| `src/api/hooks/useExpense.ts` | Mock expense queries | `[DEFERRED]` | Generated client hooks for `/api/financial/expenses` | Scheduled |
| `src/api/hooks/useStrategy.ts` | Mock strategy/risk hooks | `[DEFERRED]` | Generated client hooks for `/api/strategy` | Scheduled |

---

## 3. Product Safety & Governance Rules

1. **Non-Mock Proof Gate**: No deleted feature will be considered resolved until its replacement is backed by a verified database migration, controller endpoint, and typed frontend contract.
2. **Explainability Mandate**: Every future analytics metric or AI recommendation must reference concrete evidence and database records.
3. **Change Control**: Any future file deletions must be registered in this matrix before merging.
