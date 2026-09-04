# CHARLIE BUSINESS OS — PHASE 2 BATCH 4 FORENSIC DISCOVERY
## External Reality Fabric, Market Radar & Opportunity Intelligence

---

## 1. Verified Baseline & Repository State

* **Git Commit**: `b5dded68a3f602c001b0679d665f148d888df646`
* **Preceding Regression Suite**: **211 / 211 PASS** (0 failures, 0 skipped)
* **Frontend Production Build**: **PASS** (`tsc && vite build`: 0 errors)
* **Working Tree**: Clean (`nothing to commit, working tree clean`)
* **Certified Baseline**:
  - Phase 1 P1–P14: **CERTIFIED**
  - Phase 1.5 H0–H16: **CERTIFIED**
  - Phase 2 Batch 1 (Security Hardening & Zero-Trust Substrate): **CERTIFIED**
  - Phase 2 Batch 2 (Company Digital Twin & Strict Reality Classification): **CERTIFIED**
  - Phase 2 Batch 3 (Institutional Learning & Causal Intelligence Engine): **CERTIFIED**
  - Phase 2 Batch 3 Hardening Layer (Deep Validation, Concurrency & Benchmark Lab): **CERTIFIED**

---

## 2. Master Charlie Invariants

```text
MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ POLICY
EXTERNAL INTELLIGENCE: SIGNAL ≠ EVIDENCE ≠ TRUTH ≠ HYPOTHESIS ≠ OPPORTUNITY ≠ DECISION
```

And:
```text
No AI model, agent, memory, connector, tool, external source, market signal,
generated content, hypothesis, opportunity, recommendation, simulation, or learning record may independently:
- define business truth
- acquire authority
- modify policy
- modify recognized revenue
- spend money
- authorize itself
- promote itself
- execute consequential real-world side effects
```

Charlie's deterministic governance substrate remains sovereign. Consequential execution remains strictly behind the Execution Wall (reserved for Batch 6). Batch 4 terminates at **governed strategic recommendation**.

---

## 3. Existing Architecture & Major Reuse Points

| Component | Location | Existing Capabilities | Batch 4 Reuse Strategy |
| :--- | :--- | :--- | :--- |
| **EvidenceRecord** | `Core/Domain/Reality/EvidenceRecord.cs` | Canonical external evidence grounding ledger, hashes (`RawPayloadHash`, `CanonicalPayloadHash`), temporal separation (`ObservedAt`, `RetrievedAt`, `FreshUntil`), `VerificationStatus`. | Extend/inherit for `ExternalEvidenceRecord` to capture source reliability, independence, contamination risk, and market scope. |
| **Company Digital Twin** | `Core/Domain/DigitalTwin/DigitalTwinModels.cs` | 22 canonical enterprise dimensions, including Dimension 20 (`MarketSignals`), Dimension 21 (`Risks`), and Dimension 22 (`StrategicState`). | Ingest verified external evidence into the Digital Twin as `Observation` or `Estimate`, never ungrounded `Fact`. |
| **TruthMetric & Reality Decay** | `Core/Domain/Reality/RealityDecayModels.cs` | Half-life decay policies, freshness states (`VERIFIED`, `AGING`, `STALE`, `UNKNOWN`), planning guards. | Establish domain-specific decay policies for external market signals (pricing: 2d, news: 7d, trends: 30d, regulations: 180d). |
| **Counterfactual Engine** | `Core/Interfaces/ICounterfactualEngine.cs` | Interventions on Digital Twin snapshots, prediction bounds, confounders, hypothesis outputs. | Simulate market scenarios (base, upside, downside, competitor reaction, regulatory shock) for opportunities. |
| **Why-NOT Alternative Engine** | `Core/Domain/Learning/LearningModels.cs` | Competing hypotheses ($H_1, H_2, H_3$), prior vs current confidence, elimination rationale. | Force external market signals to evaluate competing hypotheses (e.g. competitor price cut: market share expansion vs inventory clearance vs cost reduction). |
| **Contamination Scoring** | `Core/Domain/Learning/LearningModels.cs` | 8-factor deterministic metrology vector (`ContaminationRisk > 0.30` blocks strategic promotion). | Apply to external sources to detect duplicate press releases, single-source bias, and manipulated signals. |
| **Uncertainty Budget** | `Core/Domain/Learning/LearningModels.cs` | Harmonic business certainty metrology across Revenue, Market, Customer, Competitive, Operational, Strategic. | Dynamically link Market and Competitive uncertainty to active external signals and unresolved market hypotheses. |
| **Decision Engine** | `Core/Decisions/IDecisionEngine.cs` | Explains decision chain: Objective $\rightarrow$ Gap $\rightarrow$ Trade-offs $\rightarrow$ Evidence $\rightarrow$ Constitution. | Link market opportunities and strategic recommendations into the immutable decision influence graph. |
| **Governed Connectors & Vault** | `Core/Domain/Connectors/ConnectorModels.cs` | AES-256-GCM vault, capability tokens, probe health checks, kill switches. | Connect external feeds (public web, RSS, competitor monitors, price feeds, filings) under governed connector tokens. |
| **Append-Only Audit Interceptor** | `Infrastructure/Interceptors/AppendOnlyAuditInterceptor.cs` | EF Core interceptor preventing modifications to immutable audit/evidence entities. | Register external evidence, signal clusters, and recommendations as append-only records. |

---

## 4. New Components Required in Batch 4

1. **External Evidence & Source Registry (`B4-H1`, `B4-H2`)**:
   - `ExternalEvidenceRecord`: Rich provenance, claim hash, content hash, publisher, region, industry, reliability score.
   - `ExternalSourceRegistry` & `IExternalSourceRegistry`: Configurable registry of external sources (web, price feeds, regulatory, job market) with deterministic reliability profiles.
   - `SourceTrustProfile`: Versioned historical reliability, independence factor, and manipulation risk.
   - `SignalCluster` & Deduplication Engine: Prevents 10 copies of the same syndicated PR release from counting as 10 independent corroborations.
2. **Signal Engine & Market Radar (`B4-H3`)**:
   - `ExternalSignal`: 19 signal types (PriceChange, ProductLaunch, CompetitorMove, HiringSurge, RegulatoryChange, etc.).
   - `IMarketRadarService` & `MarketRadarService`: Continuous signal detection, clustering, and deterministic prioritization.
3. **Hypothesis & Why-NOT Reasoning (`B4-H4`)**:
   - Competing hypotheses for market movements; prevents forcing a single narrative.
   - `IEvidenceCollectionPlanner`: Proposes queries to confirm/refute competing explanations.
4. **Opportunity & Threat Intelligence (`B4-H5`, `B4-H6`)**:
   - `MarketOpportunity` & `MarketThreat`: Structured commercial entities with customer problem, target segment, revenue potential, time to value.
   - Deterministic `CommercialOpportunityScore`: 13-dimensional formulaic scoring with confidence intervals.
   - Counterfactual Scenario Matrix: Base, upside, downside, competitor response, macro shock.
5. **Strategic Recommendation & Governance (`B4-H7`)**:
   - `StrategicRecommendation`: Complete evidence chain, why-NOT analysis, commercial score, scenario results, sensitivity analysis.
   - Uncertainty budget impact tracking and learning episode generation upon outcome observation.
6. **Executive UI (`B4-H10`)**:
   - `MarketRadar.tsx`: Live signals, competitor movements, trends, opportunities, threats, Why-NOT drawer, clear epistemic badges (`FACT`, `OBSERVATION`, `ESTIMATE`, `HYPOTHESIS`, `SIMULATION`, `UNKNOWN`).
7. **Permanent Benchmark Laboratory Extension (`B4-H9`)**:
   - Golden scenarios 1 to 12 (False competitor claim, duplicate evidence, conflicting prices, stale pricing, prompt injection, counterfactual collapse, regulatory shock, etc.).

---

## 5. Non-Goals (Explicit Boundaries)

* **NO Autonomous Consequential Execution**: Batch 4 MUST NOT execute real-world side effects (buying ads, changing live customer pricing, sending autonomous outreach, executing contracts).
* **NO Batch 5/6 Bypass**: Security Command Center and Governed Execution Firewall are separate certified batches.
* **NO Epistemic Promotion Without Evidence**: External observation $\ne$ internal fact.

---

## 6. Verification & Implementation Roadmap

```text
B4-H0: Forensic Discovery + Baseline Lock [THIS DOCUMENT]
  ↓
B4-H1: External Evidence & Source Registry Foundation
  ↓
B4-H2: Freshness, Trust & Signal Deduplication
  ↓
B4-H3: Signal Engine & Market Radar Service
  ↓
B4-H4: Hypothesis Engine & Why-NOT Competitor Reasoning
  ↓
B4-H5: Opportunity & Threat Intelligence Engine
  ↓
B4-H6: Commercial Scoring & Counterfactual Scenario Matrix
  ↓
B4-H7: Strategic Recommendation & Uncertainty Integration
  ↓
B4-H8: Poisoning, Prompt Injection, Chaos & Concurrency Hardening
  ↓
B4-H9: Permanent Benchmark Laboratory (12 Golden Scenarios)
  ↓
B4-H10: Executive UI (Market Radar & Opportunity Explorer)
  ↓
B4-H11: Full Regression & Gate Certification (B4-G01 to B4-G34)
```
