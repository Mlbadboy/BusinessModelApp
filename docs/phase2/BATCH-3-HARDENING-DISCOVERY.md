# CHARLIE BUSINESS OS — PHASE 2 BATCH 3 HARDENING DISCOVERY REPORT

## FORENSIC DISCOVERY, ARCHITECTURAL REUSE & HARDENING BLUEPRINT

### Baseline Snapshot
* **Current Status:** Certified Baseline (Phase 1 P1–P14, Phase 1.5 H0–H16, Phase 2 Batches 1, 2, 3)
* **Regression Test Count:** 193 / 193 PASS
* **Frontend Build Status:** PASS (0 TypeScript errors)
* **Preceding Commit:** `d92a5c300ad7a7e6364de92314aff0392a987c5c`
* **Master Invariant:** `MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ POLICY`

---

## 1. Existing Architecture & Components Examined

1. **Learning Domain Substrate (`src/BusinessModelApp.Core/Domain/Learning/LearningModels.cs`)**:
   - `LearningRecord`, `OutcomeRecord`, `FailureRecord`, `CorrectionRecord`, `LearningEpisode`, `LearningContradictionRecord`, `LearningExperiment`.
   - `LearningPromotionGuard`: Enforces validation barriers and blocks direct AI self-promotion.
   - Decoupled `Confidence` vs. `CausalConfidence`.
2. **Learning Service Substrate (`src/BusinessModelApp.Infrastructure/Learning/InstitutionalLearningService.cs`)**:
   - Core orchestrator for delta calculation, root cause categorization (13-cause taxonomy), candidate generation, contradiction scanning, and contextual advisory retrieval.
3. **Decision & Governance Substrate (`BusinessModelApp.Core.Domain.Decisions`)**:
   - `DecisionRecord`: Immutable record with cryptographic hash, grounding evidence references, constitution rules evaluated, and alternative rankings.
4. **Digital Twin Substrate (`BusinessModelApp.Infrastructure.DigitalTwin.CompanyDigitalTwinService.cs`)**:
   - Multi-dimensional company state projection with field-level metrology, snapshotting (`CreateSnapshotAsync`), and cryptographic integrity hashing.
5. **Mission Forking & Counterfactual Foundation (`BusinessModelApp.Core.Domain.Missions/MissionForkModels.cs`)**:
   - `CounterfactualScenario`, `MissionForkResult`, `IMissionForkEngine`.
6. **Append-Only Interception (`BusinessModelApp.Infrastructure.Interceptors/AppendOnlyAuditInterceptor.cs`)**:
   - Enforces immutability on `OutcomeRecord`, `FailureRecord`, `CorrectionRecord`, `LearningEpisode`, `DigitalTwinSnapshot`, and `DecisionRecord`.
7. **Multi-Tenant Security (`BusinessModelApp.Core.Interfaces/IUserContextService.cs`)**:
   - Strict workspace authorization on all controller actions.

---

## 2. Architectural Reuse Points

* **Counterfactual Reasoning**: Reuses `CounterfactualScenario` from `MissionForkModels.cs` and extends it into a dedicated `ICounterfactualEngine` that projects hypothetical interventions into `CounterfactualSimulation` entities tagged explicitly as `TruthClassification.Hypothesis` / `SIMULATION`.
* **Decision Influence Tracing**: Intersects `DecisionRecord`'s `GroundingEvidenceIdsJson`, `AssumptionsJson`, and `ConstitutionRulesEvaluatedJson` with `LearningRecord` references to construct an auditable, directed acyclic `LearningInfluenceGraph`.
* **Dispute & Contradiction Ledgers**: Extends existing `LearningContradictionRecord` multi-source dispute detection to supply "Why NOT?" alternative hypothesis refutations.
* **Audit & Immutability**: Uses `AppendOnlyAuditInterceptor` to secure new historical reversal and influence entities (`LearningReversalNotice`, `LearningInfluenceRecord`).

---

## 3. New Components Required

1. **`AlternativeHypothesis` & Why-NOT Reasoning**:
   - Structures competing hypotheses ($H_1$ primary, $H_2, H_3$ alternatives) with explicit supporting evidence, refuting evidence, and remaining epistemic uncertainty.
2. **`ContaminationScoreVector`**:
   - 8-dimensional deterministic metrology vector: Evidence Strength, Independence Factor, Causal Confidence, Freshness, Contradiction Risk, Scope Confidence, Source Reliability, and Overall Contamination Risk ($0.0 - 1.0$).
3. **`CounterfactualSimulation` & `ICounterfactualEngine`**:
   - Deterministic engine for what-if intervention projections that strictly prevents simulation outputs from entering verified reality.
4. **`LearningInfluenceRecord` & `ILearningInfluenceGraph`**:
   - Reconstructs exact dependencies for any decision: Truth, Evidence, Learning, Hypothesis, Simulation, Policy, and Digital Twin Snapshot.
5. **`LearningReversalNotice` & `LearningReversalEngine`**:
   - Reverses disproven learning, identifies downstream affected decisions, missions, and financial forecasts, and computes error propagation without mutating historical audit records.
6. **`UncertaintyBudget` & `ILearningDebtEngine`**:
   - Quantifies open institutional debt: unresolved hypotheses, contradictions, stale lessons, and multi-domain business certainty scores.
7. **`ILearningBenchmarkLab` & `LearningBenchmarkLabService`**:
   - Permanent 5-dimensional evaluation laboratory: Functional (100%), Security (100%), Reliability (≥99%), Intelligence (≥90%), and Governance (100%).

---

## 4. Dependency Graph

```text
[Mission Outcomes & Telemetry]
              │
              ▼
   [OutcomeRecord / FailureRecord]
              │
              ├──────────────────────────────────────────┐
              ▼                                          ▼
   [ICounterfactualEngine]                     [Why-NOT Hypothesis Engine]
   (SIMULATION / HYPOTHESIS)                   (H1 Primary vs H2, H3 Alternatives)
              │                                          │
              └────────────────────┬─────────────────────┘
                                   ▼
                        [LearningRecord]
                                   │
              ┌────────────────────┴─────────────────────┐
              ▼                                          ▼
   [ContaminationScoreVector]                  [LearningDebtEngine]
   (8-factor metrology vector)                 (UncertaintyBudget & Debt Matrix)
              │                                          │
              └────────────────────┬─────────────────────┘
                                   ▼
                        [LearningPromotionGuard]
                                   │
                        (Deterministic Barrier)
                                   │
                                   ▼
                        [Active / Promoted Learning]
                                   │
              ┌────────────────────┴─────────────────────┐
              ▼                                          ▼
   [ILearningInfluenceGraph]                   [LearningReversalEngine]
   (Audits Decision Dependencies)              (Traces Downstream Error Propagation)
```

---

## 5. Persistence Impact (`AppDbContext`)

New Entity DbSets:
- `AlternativeHypotheses`: DbSet for competing explanatory hypotheses.
- `CounterfactualSimulations`: DbSet for what-if simulation records.
- `LearningInfluenceRecords`: DbSet for decision-to-learning dependency graph nodes.
- `LearningReversalNotices`: DbSet for immutable reversal logs.
- `LearningDebtSnapshots`: DbSet for historical uncertainty budget tracking.

`AppendOnlyAuditInterceptor` updates:
- Add `LearningReversalNotice` and `LearningInfluenceRecord` to the immutable list.

---

## 6. API Surface Impact (`LearningController`)

Extended endpoints (all server-side scoped via `IUserContextService`):
- `GET /api/learning/{id}/why-not`: Retrieves alternative hypothesis analysis for a learning record.
- `GET /api/learning/{id}/contamination`: Returns the 8-factor contamination risk breakdown.
- `GET /api/learning/{id}/influence`: Retrieves the decision influence graph for a decision or learning record.
- `GET /api/learning/{id}/reversal`: Retrieves reversal history and downstream error propagation.
- `GET /api/learning/debt`: Retrieves current Learning Debt scorecard and Uncertainty Budget.
- `POST /api/learning/{id}/counterfactual`: Generates and records a counterfactual simulation.
- `GET /api/learning/benchmarks`: Retrieves historical permanent benchmark runs.
- `POST /api/learning/benchmarks/run`: Executes the 5-dimensional benchmark laboratory test suite.

---

## 7. Frontend Impact (`new-frontend/src/components/Learning/LearningCenter.tsx`)

Additions to Executive Learning Center:
1. **Why NOT? Panel**: Visual display of primary vs. alternative hypotheses with supporting/refuting evidence tags.
2. **Contamination Metrology Meter**: 8-factor breakdown replacing opaque single numbers.
3. **Learning Debt & Uncertainty Budget Gauge**: Visualizing revenue certainty, market certainty, and open debt counts.
4. **Influence Graph Viewer**: Tracing what facts, learnings, and policies influenced decisions.
5. **Reversal Audit Ledger**: Displaying reversed assumptions and their downstream impact.

---

## 8. Security & Anti-Poisoning Impact

- **Hard Contamination Barrier**: `ContaminationRisk > 0.30` deterministically blocks a learning record from active advisory retrieval and promotion to Strategic/Institutional tiers.
- **End-to-End Decision Shield**: Tests verify that even if a poisoned learning enters an agent's prompt context, the deterministic Constitution Policy Engine and Execution Firewall block unauthorized actions or budget expenditures.
- **Zero Cross-Tenant Leakage**: Benchmarks explicitly assert isolation across concurrent tenant workloads.

---

## 9. Performance & Concurrency Risks

- **Risk**: Heavy contradiction and alternative hypothesis searches could degrade retrieval latency.
  - *Mitigation*: Workspace-scoped composite indexing on `WorkspaceId`, `State`, `Tier`, and `StatementHash`.
- **Risk**: Concurrent promotions from multiple agents causing race conditions or lost updates.
  - *Mitigation*: Optimistic concurrency checks and deterministic promotion guards.
- **Risk**: Scale degradation when learning records reach 10K–100K.
  - *Mitigation*: Targeted benchmarking with P50/P95/P99 latency measurements.

---

## 10. Non-Goals

- **No autonomous real-world side effects**: Counterfactual simulations and learning debt recommendations remain purely advisory.
- **No dynamic policy rewriting**: Learning never modifies Constitution rules or budget limits.
- **No recognized revenue modification**: Predictions and simulated outcomes never alter contracted or collected financial truth.
- **No Batch 4 features**: Market Radar, external scrapers, and external connectors are out of scope for this hardening layer.
