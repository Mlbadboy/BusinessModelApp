# Charlie Business OS — Technical Debt & Vulnerability Register

**Status:** Active Baseline  
**Audited System:** Charlie Business OS (.NET 8 / React 18 / EF Core / SQLite)  
**Last Updated:** September 2026

---

## 1. Technical Debt Ranking Matrix

| ID | Title | Layer | Rank | Impact | Risk | Effort | Recommended Solution |
| :--- | :--- | :--- | :---: | :--- | :--- | :---: | :--- |
| **TD-01** | **BOLA/IDOR in REST Controllers** | API / Auth | **CRITICAL** | Cross-tenant access to objectives, world model, decisions, and missions. | High data leakage risk; unauthorized mission execution. | Medium | Enforce `IUserContextService.GetAuthorizedWorkspaceIdAsync()` and `ITenantIsolationGuard` on all endpoints. |
| **TD-02** | **Hardcoded Fallback JWT Secret Key** | Auth / Crypto | **HIGH** | Static default key `"SecureSecretKeyForBusinessModelAppAuthentication2026"` used if config missing. | Token forgery if deployed without custom environment secret. | Low | Enforce strict environment variable validation; fail application startup in Production if unset. |
| **TD-03** | **Missing Global Query Filters in EF Core** | Persistence | **HIGH** | Queries omitting `.Where(w => w.WorkspaceId == ...)` can read multi-tenant data. | Accidental cross-tenant entity leakage. | Medium | Add EF Core model `HasQueryFilter` for all workspace-scoped entities via `ITenantContextAccessor`. |
| **TD-04** | **Incomplete Append-Only Audit Interception** | Data Integrity | **HIGH** | `DecisionRecord` and `EvidenceRecord` not protected against modification/deletion in interceptor. | Tampering with cryptographic decision hashes or evidence logs. | Low | Expand `AppendOnlyAuditInterceptor` to intercept `DecisionRecord`, `EvidenceRecord`, and `MissionCheckpoint`. |
| **TD-05** | **Synthetic Reality Strings in GovernedToolRegistry** | Agent Runtime | **HIGH** | Mock fallback creates synthetic evidence strings `$"EVD-SEARCH-{Guid}"`. | Violates foundational invariant: *Agent Memory is NOT Truth; Never Create Synthetic Reality*. | Medium | Eliminate synthetic string fallbacks; integrate directly with `CapabilityRegistryService` and return genuine evidence or explicit `UNKNOWN`. |
| **TD-06** | **Missing Multi-Tier Learning Bank Infrastructure** | Intelligence | **MEDIUM** | `GovernedLearningLoop` only handles prompt candidates; lacks full failure/correction/lesson lifecycle. | System cannot systematically learn from mission deltas or forecast mistakes. | High | Implement comprehensive `LearningBank`, `OutcomeDeltaEngine`, L0–L5 learning hierarchy, and decay state machine. |
| **TD-07** | **Peripheral Mocks in Production Service Registry** | Architecture | **MEDIUM** | Peripheral modules (`MockProductService`, `MockRevenueService`, etc.) in `MockServices.cs` (651 lines). | Divergence between core executive runtime and peripheral business entities. | Medium | Replace mock services with domain entity repositories connected to `AppDbContext` and `DigitalTwin`. |
| **TD-08** | **Missing OWASP Security Headers & Global Exception Masking** | API / Web | **MEDIUM** | No CSP, no HSTS, and unhandled exceptions can expose internal framework stack traces. | Clickjacking, XSS expansion, internal server reconnaissance. | Low | Add ASP.NET Core security headers middleware and centralized problem-details exception filter. |
| **TD-09** | **Unrestricted Password Complexity Policy** | Identity | **LOW** | Identity configuration disables digit, casing, and symbol checks with minimum length of 6. | Weak user credentials susceptible to dictionary attacks. | Low | Configure standard enterprise password rules (min length 10, digit, uppercase, symbol required). |
| **TD-10** | **LLamaSharp In-Memory Model Gaps** | AI / Local | **LOW** | Dummy GGUF files in root (`dummy_model.gguf`) are non-functional placeholders. | Offline inference fallback fails if local GGUF execution is triggered. | Medium | Standardize local inference on provider-neutral `IInferenceGateway` and deterministic simulation engine. |

---

## 2. Debt Remediation Schedule

- **Milestone 1 (Immediate - Batch 1):** Remediate TD-01, TD-02, TD-03, TD-04, and TD-05.
- **Milestone 2 (Batch 2 & 3):** Remediate TD-06 and TD-07 during Digital Twin and Learning Bank builds.
- **Milestone 3 (Batch 5 & 6):** Remediate TD-08, TD-09, and TD-10 during Security Command Center and final certification.
