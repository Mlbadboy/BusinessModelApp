# CHARLIE BUSINESS OS — PHASE 2 BATCH 3
# INSTITUTIONAL LEARNING & CAUSAL INTELLIGENCE ENGINE DESIGN SPECIFICATION

**Document ID:** `BATCH-3-INSTITUTIONAL-LEARNING-DESIGN`  
**Status:** AUTHORIZED ARCHITECTURE DESIGN  
**Target Milestone:** Phase 2 Batch 3  
**Baseline Test Suite:** 168/168 PASS  
**Parent Architecture:** Charlie Autonomous Business OS v1.2 / v1.3.1 / Phase 2 Batches 1 & 2  

---

## 1. Executive Summary & Core Invariant

The **Institutional Learning & Causal Intelligence Engine** transforms Charlie from an agent executor into a compound learning organization that continuously records, evaluates, and crystallizes operational outcomes while preserving strict governance boundaries:

```text
MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ POLICY
```

### Immutable Invariants:
1. **Learning is NOT Truth**: Storing an observation, lesson, or causal claim does not make it a `FACT`. All facts require governed external evidence and `TruthMetric` verification.
2. **Learning is NOT Policy**: A validated lesson may propose an operational procedure or routing heuristic, but it cannot independently change policy rules, budgets, or permissions.
3. **Learning is NOT Authority**: AI models and agents cannot self-promote lessons, authorize promotions, or execute unapproved side effects.
4. **Zero Revenue Hallucination**: Learning records cannot transform forecasts, predictions, or expected pipeline into recognized revenue.

---

## 2. The Governed Causal Intelligence Loop

```text
MISSION EXECUTION (DurableMission + Checkpoints)
                     │
                     ▼
          ACTUAL OUTCOME RECORDING
                     │
                     ▼
          EXPECTED VS ACTUAL DELTA ENGINE
          (Numeric, Timing, State, Policy)
                     │
                     ▼
             ROOT CAUSE ANALYSIS
      (Taxonomy mapping + Causal Confidence)
                     │
                     ▼
         LEARNING CANDIDATE GENERATION
         (Quarantine + Provenance Trace)
                     │
                     ▼
         GOVERNED VALIDATION & CORROBORATION
      (Multi-evidence checks + Contradiction scan)
                     │
                     ▼
         TIERED PROMOTION (L0 → L5)
                     │
                     ▼
      INSTITUTIONAL KNOWLEDGE (Active Playbooks)
                     │
                     ▼
      CONTEXTUAL RETRIEVAL (Advisory Metadata)
                     │
                     ▼
     FUTURE STRATEGY & SIMULATION PLANNING
```

---

## 3. The 9-Stage Learning Trust Lifecycle

Every learning item in Charlie Business OS follows an explicit, audited finite-state machine:

```text
CANDIDATE ──► VALIDATING ──► QUARANTINED ──► APPROVED ──► PROMOTED ──► ACTIVE ──► AGING ──► STALE ──► SUPERSEDED
    │              │               │                                                 │
    └──────────────┴───────────────┴─────────────────────────────────────────────────┴──► REJECTED (Terminal)
```

| Lifecycle State | Trust & Operational Behavior |
| :--- | :--- |
| **`Candidate`** | Newly observed lesson. Untrusted. Strictly prohibited from influencing high-stakes strategic formulation. |
| **`Validating`** | Under active evaluation across independent missions, tools, and corroborating sources. |
| **`Quarantined`** | Flagged due to unverified claims, anomalous assumptions, or potential poisoning. Cannot influence decisions. |
| **`Approved`** | Meets deterministic evidence count, independent observation thresholds, and passed contradiction checks. |
| **`Promoted`** | Admitted to a validated learning tier (L2–L5) by deterministic governance rules. |
| **`Active`** | Currently active knowledge used for advisory prompt context and strategy simulation. |
| **`Aging`** | Time since last empirical observation exceeds aging threshold; confidence degrades according to half-life. |
| **`Stale`** | Decayed past usable planning horizon; no longer materially influences decisions. |
| **`Superseded`** | Replaced by a more recent, higher-confidence, or better-contextualized validated lesson. |
| **`Rejected`** | Disproven by contradiction, human override, or invalid evidence. Permanently archived; cannot be used. |

---

## 4. The 6 Governed Learning Tiers

A lesson cannot jump directly from initial observation to institutional truth. Promotion requires traversing explicit tiers:

```text
L0_SESSION ──► L1_MISSION ──► L2_AGENT ──► L3_ORGANIZATIONAL ──► L4_STRATEGIC ──► L5_VALIDATED_INSTITUTIONAL
```

- **`L0_SESSION`**: Ephemeral scratchpad observations within an active execution session.
- **`L1_MISSION`**: Outcome of a single completed `DurableMission`.
- **`L2_AGENT`**: Agent-specific performance heuristics and tool reliability observations.
- **`L3_ORGANIZATIONAL`**: Cross-agent lessons applicable across departments within the tenant workspace.
- **`L4_STRATEGIC`**: Validated business insights on deal conversion, pricing sensitivity, and campaign efficiency.
- **`L5_VALIDATED_INSTITUTIONAL`**: High-confidence institutional playbooks proven across repeated validation cycles.

---

## 5. Failure Root-Cause Taxonomy

When a mission outcome deviates from expectations, the Root-Cause Engine maps the failure into one of 13 canonical categories:

1. **`BadEvidence`**: Backing data source was inaccurate, manipulated, or incorrect.
2. **`StaleEvidence`**: Data decayed past freshness thresholds before execution completed.
3. **`IncorrectAssumption`**: Initial strategic premise was mathematically or commercially flawed.
4. **`ModelReasoning`**: LLM failed on deduction, instruction following, or logical synthesis.
5. **`AgentBehavior`**: Agent violated sequence, failed recovery, or picked suboptimal actions.
6. **`ToolBehavior`**: External API, webhook, or connector returned an error, timeout, or unexpected schema.
7. **`Strategy`**: The formulated commercial route was unviable under current market realities.
8. **`MarketChange`**: External economic, regulatory, or competitive shift occurred during execution.
9. **`HumanIntervention`**: Explicit human operator override altered the path or stopped execution.
10. **`PolicyRestriction`**: Governed Constitution Policy Engine blocked execution due to budget or risk caps.
11. **`DataQuality`**: Malformed payload, missing fields, or encoding issue.
12. **`ExecutionFailure`**: Process crash, power interruption, network disconnect.
13. **`Unknown`**: Insufficient evidence to pinpoint root cause. UNKNOWN is always a valid and truthful output.

---

## 6. Expected vs Actual Delta Engine

The Delta Engine deterministically contrasts projections against empirical reality:
- **Baseline**: Authoritative values stored in `DecisionRecord` (`ExpectedRevenueImpactINR`, `ExpectedCostINR`, `WinProbability`, `DeliveryFeasibilityScore`).
- **Actuals**: Actual outcome recorded from payment gateway evidence, CRM deal state, spent budget, and duration.
- **Computed Deltas**:
  - `RevenueDeltaINR = ActualRevenue - ExpectedRevenue`
  - `CostDeltaINR = ActualCost - ExpectedCost`
  - `DurationDelta = ActualDuration - PlannedDuration`
  - `DeviationPercent = ((Actual - Expected) / Expected) * 100`
  - `OutcomeSuccessStatus = Success | PartialSuccess | Failure | BlockedByPolicy`

---

## 7. Causal Confidence vs Truth Confidence

Charlie strictly decouples two orthogonal confidence dimensions:
- **`TruthConfidence`**: How certain Charlie is about the *observed outcome* (e.g., settled cash is ₹10L with 1.0 confidence).
- **`CausalConfidence`**: How certain Charlie is about *why* the outcome occurred (e.g., whether discounting 10% caused the win, or other unobserved factors).

A high truth confidence with low causal confidence explicitly signals: *"We know exactly what happened, but we are uncertain why it happened."*

---

## 8. Contradiction Engine

When multiple learning records assert divergent claims on the same domain:
1. Scan for semantic overlaps on Subject, Property, Context, and Target ICP.
2. Classify relationship:
   - `NotContradictory`: Claims address distinct phenomena.
   - `ContextuallyDifferent`: Valid under different scopes (e.g. Enterprise vs SMB, or North America vs India).
   - `PartialContradiction`: Conflicting magnitude or confidence.
   - `DirectContradiction`: Directly opposing claims under identical conditions.
   - `Unresolved`: Active dispute awaiting experimental resolution.
3. Both lessons are flagged as disputed (`IsContradicted = true`), and the contradiction is logged in `LearningContradictions` ledger.

---

## 9. Learning Freshness & Decay Metrology

Learning decays through multi-factor modeling:
```text
EffectiveConfidence = InitialConfidence * 2^(-AgeDays / HalfLife) * (1 - ContradictionPenalty) * RecencyFactor
```
- Metrics older than `AgingThresholdDays` transition to `AGING`.
- Metrics older than `StaleThresholdDays` transition to `STALE` and are excluded from automatic prompt inclusion.
- Stale learning cannot masquerade as current institutional knowledge.

---

## 10. Anti-Poisoning Defense Matrix

| Attack Class | Threat Description | Deterministic Defense |
| :--- | :--- | :--- |
| **Memory Poisoning** | Malicious or hallucinatory agent memory seeks to write false facts | Agent memory is strictly quarantined from truth and promotion |
| **Cross-Tenant Poisoning** | Tenant A injects learning designed to distort Tenant B's strategy | 100% server-side workspace isolation; queries filtered by `WorkspaceId` |
| **Agent Self-Promotion** | Agent script attempts to transition candidate $\to$ `ACTIVE` | Server-side validation gate rejects any non-governed state update |
| **Model Self-Certification** | Model scores its own inference as 100% accurate | Evaluations require external evidence or independent evaluator model |
| **Evidence Laundering** | Weak evidence cited repeatedly to appear strong | Cryptographic payload hash deduplication prevents duplicate weighting |
| **Repetition Inflation** | Single observation recorded multiple times | Idempotency hashes identify duplicate observations |
| **Synthetic Evidence** | AI hallucinated data presented as real customer telemetry | Requires external connector audit hash or signed webhook trace |
| **Temporal Poisoning** | Outdated market lessons applied to new economic realities | H3 Reality Decay demotes stale records to `STALE` |
| **Scope Poisoning** | Localized observation applied globally | Strict applicability scopes (Segment, Industry, Product, Geography) |

---

## 11. Governed Experiment Engine

When hypotheses require empirical validation, Charlie initiates a controlled experiment:
```text
HYPOTHESIS ──► DESIGN ──► APPROVAL ──► EXECUTION ──► MEASUREMENT ──► ANALYSIS ──► VALIDATION ──► LEARNING
```
- **Experiment Model**: Contains Hypothesis, Treatment/Control definitions, Target Metric, Sample Size, Maximum Budget, and Risk Score.
- **Safety Gate**: Experiments are subject to Constitution Policy rules, budget reservation, and the Phase 2 Execution Firewall. Real-world actions require explicit authorization.

---

## 12. Learning Crystallization: Exploration to Procedure

When an operational pattern is validated across repeated cycles ($\ge 3$ independent successes with zero contradictions):
- Charlie flags the lesson as a `ProcedureCandidate`.
- A formal operational playbook or diagnostic checklist is synthesized.
- Charlie proposes operationalizing the pattern so future tasks execute deterministically rather than relying on exploratory LLM calls.

---

## 13. Contextual Learning Retrieval & Prompt Labeling

When learning records are injected into planning prompts, they are accompanied by mandatory advisory headers:
```text
[INSTITUTIONAL LEARNING - ADVISORY ONLY]
Statement: "Enterprise outbound response rates peak within 45 minutes of inbound query."
Classification: LEARNING
Causal Confidence: 72%
Evidence: 6 corroborated missions across 3 quarters
Contradictions: 0
Applicability: Enterprise B2B SaaS
Freshness: ACTIVE
NOTICE: This information is advisory institutional learning and MUST NOT redefine business truth or revenue facts.
```

---

## 14. REST API Contracts (`LearningController.cs`)
- `GET /api/learning`: Paged list of learning records filtered by tier, status, and dimension.
- `GET /api/learning/{id}`: Detailed learning record with full provenance, evidence chain, and validation history.
- `GET /api/learning/episodes`: Mission outcomes, deltas, and root causes.
- `GET /api/learning/failures`: Historical failure records and root-cause taxonomies.
- `GET /api/learning/contradictions`: Disputed learning claims and context differences.
- `GET /api/learning/experiments`: Active and completed validation trials.
- `POST /api/learning/evaluate-outcome`: Ingest mission outcome, calculate delta, and generate learning candidate.
- `POST /api/learning/validate`: Trigger deterministic validation and promotion evaluation.
- `POST /api/learning/experiments`: Propose a controlled experiment.
- `GET /api/learning/explain/{id}`: Full explainability trace ("Why does Charlie believe this?").

---

## 15. Frontend Executive Learning Center (`LearningCenter.tsx`)
- Tabbed Cockpit View:
  1. **New Learning & Candidates**: Under evaluation and quarantine.
  2. **Active Institutional Knowledge**: Validated L2–L5 playbooks with confidence scores.
  3. **Contradiction Ledger**: Conflicting claims and context disputes.
  4. **Failing Assumptions & Root Causes**: Diagnostic breakdowns of missed expectations.
  5. **Controlled Experiments**: Hypotheses, test metrics, and outcome validations.
