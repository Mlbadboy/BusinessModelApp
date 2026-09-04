# CHARLIE BUSINESS OS — PHASE 2 BATCH 3 HARDENING DESIGN SPECIFICATION

## CAUSAL INTELLIGENCE, COUNTERFACTUALS, CONTAMINATION METROLOGY & PERMANENT BENCHMARK LAB

### Core Invariant
```text
MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ POLICY
```

---

## 1. Mathematical & Deterministic Definition of Contamination Metrology

The **Contamination Score Vector** replaces opaque, ungrounded confidence scores with an 8-factor deterministic metrology vector:

$$\mathbf{C} = \langle S_{\text{evid}}, I_{\text{indep}}, C_{\text{causal}}, F_{\text{fresh}}, R_{\text{contra}}, C_{\text{scope}}, R_{\text{src}}, \mathbf{Risk}_{\text{contam}} \rangle$$

### Factor Semantics
1. **Evidence Strength ($S_{\text{evid}} \in [0.0, 1.0]$)**:
   Calculated from grounding evidence quantity and verification status.
   $$S_{\text{evid}} = \min\left(1.0, \frac{\text{VerifiedEvidenceCount} + 0.5 \times \text{ObservedEvidenceCount}}{3}\right)$$
2. **Independence Factor ($I_{\text{indep}} \in [0.0, 1.0]$)**:
   Measures source diversity and penalizes duplicate / single-source observations.
   $$I_{\text{indep}} = \frac{\text{DistinctSourceMissions}}{\text{TotalObservationCount}}$$
3. **Causal Confidence ($C_{\text{causal}} \in [0.0, 1.0]$)**:
   Decoupled belief that the identified intervention or mechanism was the actual causal driver of the observed delta.
4. **Freshness ($F_{\text{fresh}} \in [0.0, 1.0]$)**:
   Integrates with `RealityDecayPolicy`. Degrades exponentially based on half-life:
   $$F_{\text{fresh}} = \exp\left(-\frac{\ln(2) \cdot \text{AgeDays}}{\text{HalfLifeDays}}\right)$$
5. **Contradiction Risk ($R_{\text{contra}} \in [0.0, 1.0]$)**:
   Proportion of unresolved contradictions and competing refutations:
   $$R_{\text{contra}} = \min\left(1.0, \text{ContradictionCount} \times 0.35\right)$$
6. **Scope Confidence ($C_{\text{scope}} \in [0.0, 1.0]$)**:
   Degree of empirical boundary clarity (e.g. valid only for Mid-Market B2B, not enterprise or consumer).
7. **Source Reliability ($R_{\text{src}} \in [0.0, 1.0]$)**:
   Trust score of originating agents and connectors based on historical accuracy.
8. **Overall Contamination Risk ($\mathbf{Risk}_{\text{contam}} \in [0.0, 1.0]$)**:
   Composite indicator that a learning record is polluted, ungrounded, or adversarially influenced:
   $$\mathbf{Risk}_{\text{contam}} = \text{clamp}\left(1.0 - \left(S_{\text{evid}} \cdot I_{\text{indep}} \cdot C_{\text{causal}} \cdot F_{\text{fresh}} \cdot R_{\text{src}}\right) + R_{\text{contra}} \cdot 0.5, 0.0, 1.0\right)$$

### Hard Governance Policy Gate
$$\text{If } \mathbf{Risk}_{\text{contam}} > 0.30 \implies \text{Quarantined / Excluded from Strategic & Institutional Tiers}$$

---

## 2. Counterfactual Reasoning & Simulation Architecture

The **Counterfactual Engine** evaluates hypothetical interventions against observed historical reality without corrupting ground truth:

```text
Historical Reality (Ground Truth)
┌──────────────────────────────────────────────────────────┐
│ Mission M1: Response Time = 8h, Conversion = 12%         │
└────────────────────────────┬─────────────────────────────┘
                             │
                             ▼
                    [Intervention Delta]
              (What if Response Time = 2h?)
                             │
                             ▼
              [Counterfactual Simulation Engine]
    (Deterministic model + Digital Twin context + bounds)
                             │
                             ▼
               [CounterfactualSimulation Entity]
┌──────────────────────────────────────────────────────────┐
│ Simulated Conversion: 16% [Bounds: 14% - 19%]            │
│ Classification: HYPOTHESIS / SIMULATION (NEVER FACT)     │
│ Prediction Confidence: 0.72 | Causal Confidence: 0.48    │
└────────────────────────────┬─────────────────────────────┘
                             │
                             ▼
             [Candidate Governed Experiment]
        (Controlled trial to empirically validate)
```

### Safety Invariants:
1. Counterfactual results MUST have `TruthClassification.Hypothesis` and state explicitly flagged as `SIMULATION`.
2. Under no circumstance may simulated conversion or revenue update recognized revenue in the Digital Twin.

---

## 3. "Why NOT?" Alternative Hypothesis Competition

Rather than prematurely accepting the first correlation, the engine forces competitive hypothesis evaluation:

| Hypothesis Slot | Description | Evaluation Rule |
| :--- | :--- | :--- |
| **$H_1$ (Primary)** | Leading causal explanation for the observed outcome. | Must cite corroborating evidence and address known confounders. |
| **$H_2$ (Alternative A)** | Plausible alternative (e.g. seasonal demand surge rather than pricing change). | System checks if evidence exists to refute or differentiate. |
| **$H_3$ (Alternative B)** | Competitive or macro shift explanation. | Tested against market reality indicators in Digital Twin. |

If alternatives cannot be deterministically ruled out:
$$\text{Remaining Epistemic Uncertainty} = \text{HIGH} \implies \text{Status} = \text{UNRESOLVED}$$

---

## 4. Learning Influence Graph Architecture

Constructs an auditable DAG for every autonomous decision in `DecisionRecord`:

```text
                          [ Decision #D101 ]
                                  │
         ┌────────────────────────┼────────────────────────┐
         ▼                        ▼                        ▼
  [ Truth & Facts ]        [ Advisory Learning ]    [ Governed Policy ]
  - Fact #F12 (Revenue)    - Learning #L42 (Email)   - Policy #P03 (Budget)
  - Fact #F19 (Cash)       - Learning #L55 (Pricing) - Policy #P06 (Margin)
  - Twin Snapshot #DT9                               - Policy #P11 (Risk)
```

Each influence edge records:
- `SourceType`: Truth, Evidence, Learning, Hypothesis, Simulation, Policy, DigitalTwin.
- `ContributionWeight`: Relative significance in the decision formulation.
- `Confidence`: Source confidence metric.
- `IsAdvisory`: Strictly true for all Learning, Hypothesis, and Simulation nodes.

---

## 5. Learning Reversal & Error Propagation Analysis

When new empirical evidence falsifies a previously accepted learning record:

```text
[ Disconfirming Reality Evidence ]
                 │
                 ▼
     [ Initiate Learning Reversal ]
                 │
                 ├─────────────────────────────────────────────────┐
                 ▼                                                 ▼
   [ Demote/Supersede Record ]                      [ Downstream Traversal ]
   (Candidate -> Superseded)                         (Find all linked decisions)
                 │                                                 │
                 ▼                                                 ▼
   [ Create LearningReversalNotice ]                 [ Calculate Impact Deviations ]
   (Immutable audit ledger entry)                     - Revenue Deviation
                                                      - Margin Deviation
                                                      - Mission Retries
```

---

## 6. Uncertainty Budget & Learning Debt Engine

Calculates domain-specific business certainty:
- **Revenue Certainty**: Backed by verified signed contracts and recognized cash.
- **Market Certainty**: Backed by grounded competitive signals and lead freshness.
- **Customer Behavior Certainty**: Backed by verified transaction telemetry.
- **Competitive Certainty**: Backed by external audited benchmarks.
- **Overall Business Certainty**: Harmonic mean weighted by domain risk.

**Learning Debt Scorecard**:
- Count of open, unvalidated hypotheses.
- Count of active, unresolved contradictions.
- Count of decayed, stale lessons still referenced in historical strategies.
- Count of high-impact unknown variables.

---

## 7. Permanent Benchmark Laboratory (5 Dimensions)

| Dimension | Target | Verification Method |
| :--- | :---: | :--- |
| **Functional** | **100%** | Full lifecycle validation: Candidate → Promotion → Decay → Reversal. |
| **Security** | **100%** | Anti-poisoning, cross-tenant barrier, BOLA/IDOR immunity, revenue firewall. |
| **Reliability** | **≥99%** | Concurrency stress (10+ parallel agents), chaos restart recovery, idempotent replay. |
| **Intelligence** | **≥90%** | Causal attribution accuracy, alternative hypothesis recall, counterfactual consistency. |
| **Governance** | **100%** | Policy engine primacy, append-only immutability, sovereign kill switch override. |
