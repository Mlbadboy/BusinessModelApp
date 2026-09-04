# CHARLIE BUSINESS OS — PHASE 2 BATCH 3
# INSTITUTIONAL LEARNING & CAUSAL INTELLIGENCE ARCHITECTURE DISCOVERY

**Document ID:** `BATCH-3-ARCHITECTURE-DISCOVERY`  
**Execution Timestamp:** 2026-09-04T16:53:00+05:30  
**Phase Status:** PHASE 2 BATCH 3 FORENSIC DISCOVERY COMPLETE  
**Baseline Test Suite:** 168/168 PASS (Phase 1 P1–P14 + Phase 1.5 H0–H16 + Phase 2 Batch 1 + Phase 2 Batch 2)  
**Parent Commit:** `3279962`  

---

## 1. Executive Forensic Discovery Summary

Before implementing **Phase 2 Batch 3: Institutional Learning & Causal Intelligence Engine**, a full codebase forensic inspection was conducted across domain models, persistence contexts, interceptors, agents, memory layers, decision records, mission runtimes, evidence graphs, and API controllers.

### Core Architectural Principle:
```text
MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ POLICY
```
- **Memory**: Records chronological events, working scratchpads, and raw agent impressions.
- **Learning**: Records what appears to have been learned from outcomes (subject to validation and quarantine).
- **Knowledge**: Validated learning that has passed governed multi-tier promotion barriers.
- **Truth**: Governed business reality supported by external evidence, cryptographic hashes, and the Digital Twin / TruthMetric architecture.
- **Policy**: Deterministic rules defining what Charlie may or may not execute.

**Learning MUST NEVER silently redefine truth, bypass policy, or manufacture recognized revenue.**

---

## 2. Relevant Existing Components & Reuse Points

| Component | File Path | Existing Capabilities | Batch 3 Reuse Plan |
| :--- | :--- | :--- | :--- |
| **`TruthClassification`** | `src/.../Domain/DigitalTwin/DigitalTwinModels.cs` | 6-state classification (`Fact`, `Estimate`, `Hypothesis`, `Observation`, `Learning`, `Unknown`) | **Direct Reuse**: Every causal claim, hypothesis, and lesson uses this enum. Zero competing classification enums. |
| **`DigitalTwinSnapshot`** | `src/.../Domain/DigitalTwin/DigitalTwinModels.cs` | Immutable, workspace-scoped, SHA-256 integrity hashed company state | **Integration Point**: Learning records link to originating and validating `DigitalTwinSnapshotId`. |
| **`DecisionRecord`** | `src/.../Domain/Decisions/DecisionRecord.cs` | Immutable decision log with ExpectedRevenueImpactINR, ExpectedCostINR, WinProbability, AssumptionsJson, Rationale, and SHA-256 hash | **Primary Delta Baseline**: Provides the authoritative `ExpectedOutcome` for delta calculation against actual results. |
| **`DurableMission` & `Checkpoints`** | `src/.../Domain/Missions/DurableMissionModels.cs` | Mission lifecycle (12 planning states + 6 execution states), step tracking, spent budget | **Traceability Root**: Connects learning episodes to missions, agents, steps, and checkpoints. |
| **`EventSourcedMissionStore`** | `src/.../Domain/Missions/EventSourcedMissionModels.cs` | Append-only event streams (`MissionCreated`, `StrategySimulated`, `AgentDispatched`, etc.) | **Attribution Chain**: Replay chain: Mission $\to$ Step $\to$ Agent $\to$ Model $\to$ Tool $\to$ Decision $\to$ Outcome. |
| **`IEvidenceGraph`** | `src/.../Domain/Reality/EvidenceGraphModels.cs` | Source nodes, evidence claims, reliability weighting, corroboration/contradiction links | **Grounding Ledger**: Learning references backing evidence claims and sources; no duplicate graph created. |
| **`IRealityDecayEngine`** | `src/.../Domain/Reality/RealityDecayModels.cs` | Exponential decay with half-life, aging/stale/unknown thresholds | **Decay Integration**: Learning decay models use age, volatility, and contradiction frequency. |
| **`AppendOnlyAuditInterceptor`** | `src/.../Infrastructure/Interceptors/AppendOnlyAuditInterceptor.cs` | Enforces zero mutation/deletion on audit, decision, snapshot, and evidence records | **Security Enforcement**: Protects immutable learning transition logs and validated knowledge records. |
| **`IUserContextService`** | `src/.../Core/Interfaces/IUserContextService.cs` | Server-side workspace isolation and claims authentication | **Zero-Trust Tenant Boundary**: Ensures 100% of learning operations are scoped to caller's workspace. |
| **`AgentMemory`** | `src/.../Core/Agents/AgentMemory.cs` | 5-compartment memory (Working, Episodic, Semantic, Evidence, Hypotheses) | **Clear Boundary**: Memory feeds candidate generation, but memory itself is explicitly quarantined from truth. |

---

## 3. Integration & Extension Points

### 3.1 Expected vs Actual Delta Engine
- **Source of Expected**: `DecisionRecord` (`ExpectedRevenueImpactINR`, `ExpectedCostINR`, `WinProbability`, `DeliveryFeasibilityScore`, `AssumptionsJson`).
- **Source of Actual**: `DurableMission` completion state, actual revenue/cash observed from payment/CRM evidence, and execution telemetry.
- **Delta Calculation**: Deterministic comparison computing numeric delta, percentage delta, completion duration, and state deviations.

### 3.2 Root-Cause Diagnostic Taxonomy
Failure analysis maps observed deviations into one of the 13 canonical categories:
`BadEvidence`, `StaleEvidence`, `IncorrectAssumption`, `ModelReasoning`, `AgentBehavior`, `ToolBehavior`, `Strategy`, `MarketChange`, `HumanIntervention`, `PolicyRestriction`, `DataQuality`, `ExecutionFailure`, `Unknown`.

### 3.3 Learning Trust Lifecycle & Promotion State Machine
- **Lifecycle**: `Candidate` $\to$ `Validating` $\to$ `Quarantined` $\to$ `Approved` $\to$ `Promoted` $\to$ `Active` $\to$ `Aging` $\to$ `Stale` $\to$ `Superseded`, with terminal state `Rejected`.
- **Tiers**: `L0_Session` $\to$ `L1_Mission` $\to$ `L2_Agent` $\to$ `L3_Organizational` $\to$ `L4_Strategic` $\to$ `L5_ValidatedInstitutional`.
- **Governance Barrier**: No AI agent or LLM may directly set a learning record to `Approved`, `Promoted`, or `Active`. Transitions require deterministic validation thresholds (evidence count, corroboration, independent observation count, contradiction check).

### 3.4 Contradiction Engine
Evaluates conflicting learning claims and classifies their relationship:
- `NotContradictory`
- `ContextuallyDifferent` (e.g. valid for enterprise vs SMB, or different time horizons)
- `PartialContradiction`
- `DirectContradiction`
- `Unresolved`

Unresolved contradictions remain permanently visible in the Learning Center.

### 3.5 Contextual Learning Retrieval & Prompt Labeling
When learning is retrieved into planning prompts:
- Learning is accompanied by explicit metadata: `Classification = LEARNING`, `CausalConfidence`, `ValidationCount`, `ContradictionCount`, `ApplicabilityScope`, and `Freshness`.
- Models are explicitly instructed: *"This information is advisory institutional learning and MUST NOT redefine business truth or revenue facts."*

---

## 4. Potential Conflicts & Mitigations

1. **Conflict: Duplicate Learning Models**:
   - *Discovery*: In Phase 1.5, `AutonomyAndLearningModels.cs` contained a basic `LearningCandidate` and `IGovernedLearningLoop`.
   - *Mitigation*: The new `InstitutionalLearning` domain extends and subsumes learning governance into a robust, multi-tier enterprise engine without breaking existing interfaces.
2. **Conflict: Ambiguous EvidenceRecord Namespace**:
   - *Discovery*: Two `EvidenceRecord` classes exist (`Commercial` vs `Reality`).
   - *Mitigation*: Use explicit type aliasing (`using EvidenceRecord = BusinessModelApp.Core.Domain.Reality.EvidenceRecord;`).
3. **Conflict: Memory / AI Self-Promotion**:
   - *Risk*: An agent writes a learning candidate and attempts to mark it as `ACTIVE` or `FACT`.
   - *Mitigation*: Server-side `LearningPromotionGuard` strictly rejects any direct promotion attempt from agent memory or AI prompts.

---

## 5. Proposed Batch 3 Additions

1. **Domain Models (`src/BusinessModelApp.Core/Domain/Learning/LearningModels.cs`)**:
   - `LearningRecord`: Complete metrology, causal confidence, evidence pointers, decay state, and lifecycle tracking.
   - `LearningEpisode`: Record of a specific learning event connecting mission, outcome, delta, and root cause.
   - `FailureRecord`: Formal failure capture with root-cause taxonomy.
   - `CorrectionRecord`: Corrective action taken and subsequent outcome verification.
   - `OutcomeRecord`: Deterministic expected vs actual delta container.
   - `Hypothesis`: Causal speculation undergoing validation.
   - `Experiment`: Controlled validation trial with hypothesis, sample, metric, and governance wall.
   - `ContradictionRecord`: Multi-lesson dispute ledger.
2. **Core Service & Interface (`IInstitutionalLearningService.cs`, `InstitutionalLearningService.cs`)**:
   - Mission outcome recording and delta calculation.
   - Root-cause attribution with independent causal confidence vs truth confidence.
   - Learning candidate generation and quarantine.
   - Governed multi-tier promotion engine.
   - Contradiction detection and resolution tracking.
   - Multi-factor learning decay engine.
   - Anti-poisoning validation (memory, cross-tenant, self-promotion, repetition inflation).
   - Contextual retrieval with advisory metadata.
3. **Database Context (`AppDbContext.cs`)**:
   - Add DbSets for `LearningRecords`, `LearningEpisodes`, `FailureRecords`, `OutcomeRecords`, `LearningExperiments`, `LearningContradictions`.
   - Update `AppendOnlyAuditInterceptor` to protect immutable learning logs.
4. **REST API Controller (`LearningController.cs`)**:
   - Scoped strictly by `IUserContextService` for zero cross-tenant leakage.
5. **Frontend Executive Learning Center (`LearningCenter.tsx`)**:
   - Visual dashboard for New Learning, Validating, Active, Contradictions, Failing Assumptions, and Experiments.
6. **Automated Verification Suite (`Phase2Batch3_LearningTests.cs`)**:
   - 25+ adversarial tests (B3-01 to B3-25).

---

## 6. Explicit Non-Goals
- DO NOT allow learning to modify recognized revenue.
- DO NOT allow learning to bypass policy engine rules or kill switches.
- DO NOT allow learning to execute real-world side effects without the Phase 2 execution wall.
- DO NOT create a secondary evidence graph.
- DO NOT allow AI models to self-certify or authorize promotion.

---

## 7. Forensic Sign-Off
Forensic discovery is complete. The implementation plan and design specification are clear. Proceeding to create the Design Specification artifact.
