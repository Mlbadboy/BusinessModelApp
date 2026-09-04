# CHARLIE BUSINESS OS — BATCH 4 OPPORTUNITY & THREAT DESIGN
## 13-Dimension Commercial Scoring, Counterfactuals & Why-NOT Reasoning

---

## 1. 13-Dimension Commercial Opportunity Scoring

Commercial attractiveness is computed deterministically across 13 distinct factors:

1. **Market Attractiveness** (Weight: 0.10)
2. **Customer Pain Severity** (Weight: 0.10)
3. **Revenue Potential INR** (Weight: 0.15)
4. **Margin Potential %** (Weight: 0.10)
5. **Strategic Fit Score** (Weight: 0.15)
6. **Competitive Advantage** (Weight: 0.10)
7. **Execution Ease** (Weight: 0.05)
8. **Time to Value** (Weight: 0.05)
9. **Risk Safety** (Weight: 0.05)
10. **Evidence Confidence** (Weight: 0.05)
11. **Causal Confidence** (Weight: 0.05)
12. **Freshness Metrology** (Weight: 0.05)
13. **Purity from Contamination** (Weight: 0.05)

### Epistemic Dampening
Opportunities with weak evidence or high contamination cannot achieve high commercial ratings:
$$\text{ConfidenceDampener} = 0.60 + (0.40 \times \text{Confidence})$$
$$\text{ContaminationDampener} = 1.0 - (\text{ContaminationRisk} \times 0.40)$$
$$\text{FinalScore} = \text{clamp}(\text{BaseWeightedScore} \times \text{ConfidenceDampener} \times \text{ContaminationDampener}, 0.0, 1.0)$$

### Blocking Factors
- $\text{ContaminationRisk} > 0.30 \implies$ Strategic promotion blocked.
- $\text{Confidence} < 0.40 \implies$ Evidence collection required before simulation.
- $\text{RiskScore} > 0.70 \implies$ Governance risk committee approval required.

---

## 2. Counterfactual Scenario Matrix

Each high-value opportunity generates 5 simulated futures:

1. **Base Scenario**: Standard market conditions and historical conversion.
2. **Upside Scenario**: Competitor does not react; adoption accelerates +35%.
3. **Downside Scenario**: Demand softens 30%; customer CAC rises 20%.
4. **Competitor Response Scenario**: Competitor matching discounts within 30 days.
5. **Macro Shock Scenario**: Regulatory or economic interest shock.

**Invariant**: All scenario results are strictly marked `TruthClassification.Hypothesis` / `IsSimulation = true`. They NEVER modify recognized revenue or financial ledgers.

---

## 3. Why-NOT Alternative Hypothesis Reasoning

Every strategic recommendation documents:
- **Primary Hypothesis ($H_1$)**: Why Charlie recommends this pursuit.
- **Alternative Hypothesis ($H_2$)**: Partnership or joint venture instead of build.
- **Alternative Hypothesis ($H_3$)**: Defensive waiting or maintaining baseline.
- **Elimination Proof**: Concrete economic rationale for rejecting $H_2$ and $H_3$.
