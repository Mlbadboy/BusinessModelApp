# CHARLIE BUSINESS OS — PHASE 2 BATCH 4 ARCHITECTURE
## External Reality Fabric, Market Radar & Opportunity Intelligence

---

## 1. Architectural Scope

Batch 4 provides Charlie with an **External Reality Fabric**, transforming raw external business signals into structured, corroborated commercial opportunities and threats, while strictly preserving the master invariants:

```text
MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ POLICY
EXTERNAL INTELLIGENCE: SIGNAL ≠ EVIDENCE ≠ TRUTH ≠ HYPOTHESIS ≠ OPPORTUNITY ≠ DECISION
```

External information is untrusted input. It cannot modify recognized revenue, override policy, self-promote, or cross the **Phase 2 Execution Wall** into autonomous consequential action.

---

## 2. End-to-End Cognitive Pipeline

```text
EXTERNAL FEED (Web / News / Price Feeds / Regulatory / Competitor Sites)
      ↓
INGESTION & SANITIZATION (Prompt-Injection Scanner & Zero-Trust Neutralization)
      ↓
CRYPTOGRAPHIC PROVENANCE (ContentHash, ClaimHash, Publisher Lineage)
      ↓
REALITY DECAY (Domain-specific half-life: pricing 2d, news 5d, regulatory 180d)
      ↓
SIGNAL DEDUPLICATION (Claim clustering; 10 copies ≠ 10 independent confirmations)
      ↓
MARKET RADAR (19 Signal Types, Deterministic Prioritization)
      ↓
HYPOTHESIS ENGINE (Primary H1 vs Why-NOT Alternatives H2, H3)
      ↓
OPPORTUNITY / THREAT FORMULATION (Customer Problem, Strategic Fit, Revenue Potential)
      ↓
COMMERCIAL SCORING (13-dimension weighted formula, confidence intervals)
      ↓
COUNTERFACTUAL SIMULATION (Base, Upside, Downside, Competitor Response, Macro Shock)
      ↓
STRATEGIC RECOMMENDATION (Advisory only; requires CEO / Governance approval)
      │
      ╳  PHASE 2 EXECUTION WALL (Autonomous Real-World Action Blocked)
      │
[BATCH 6 — GOVERNED EXECUTION FIREWALL]
```

---

## 3. Subsystem Architecture

### A. External Source Registry & Trust Metrology
* Located in `BusinessModelApp.Infrastructure.ExternalReality.ExternalSourceRegistry`.
* Implements `IExternalSourceRegistry`.
* Maintains dynamic `SourceTrustProfile` tracking historical accuracy, freshness, independence, and manipulation risk.
* Quarantines prompt-injection patterns (`ignore.*instructions`, `system: override`, `bypass.*governance`, `grant.*root`) into data-only records with `IsSanitizedDataOnly = true` and `PromptInjectionRiskScore = 0.95`.

### B. Signal Engine & Market Radar
* Located in `BusinessModelApp.Infrastructure.ExternalReality.MarketRadarService`.
* Implements `IMarketRadarService`.
* Prioritizes signals via deterministic multi-factor formula:
  $$\text{PriorityScore} = (\text{Magnitude} \times 0.35) + (\text{Confidence} \times 0.25) + (\text{Freshness} \times 0.20) + (\text{Novelty} \times 0.20)$$
* Maps competitor movements into structured `CompetitorProfile` entities.

### C. Opportunity & Threat Intelligence Engine
* Located in `BusinessModelApp.Infrastructure.ExternalReality.OpportunityIntelligenceService`.
* Implements `IOpportunityIntelligenceService` and `IThreatIntelligenceService`.
* Formulates `MarketOpportunity` and `MarketThreat`.
* Calculates deterministic 13-dimensional commercial opportunity score with confidence intervals and blocking factors (e.g. `ContaminationRisk > 0.30` blocks strategic promotion).
* Evaluates 5 counterfactual scenarios strictly classified as `TruthClassification.Hypothesis` / `IsSimulation = true`.
* Emits `StrategicRecommendation` containing Why-NOT elimination proofs, expected value, and downside risk bounds.

---

## 4. Execution Wall Preservation

Under no circumstances can an external signal or advisory recommendation execute a consequential real-world action. Autonomous actions require Batch 6 execution capabilities governed by:
- Scoped capability tokens
- Agent wallet balances
- Policy Engine permission grants
- CEO / human-in-the-loop approvals
