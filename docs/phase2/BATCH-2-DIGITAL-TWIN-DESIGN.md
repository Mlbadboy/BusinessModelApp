# CHARLIE BUSINESS OS — PHASE 2 BATCH 2
# COMPANY DIGITAL TWIN & STRICT REALITY CLASSIFICATION DESIGN SPECIFICATION

**Document ID:** `BATCH-2-DIGITAL-TWIN-DESIGN`  
**Status:** AUTHORIZED ARCHITECTURE DESIGN  
**Target Milestone:** Phase 2 Batch 2  
**Baseline Test Suite:** 145/145 PASS (Phase 1 P1–P14 + Phase 1.5 H0–H16 + Phase 2 Batch 1)  
**Parent Architecture:** Charlie Autonomous Business OS v1.2 / v1.3.1  

---

## 1. Executive Summary & Architectural Mandate

The **Company Digital Twin** is Charlie's authoritative, structured representation of the company's **CURRENT KNOWN STATE**.

### Critical Architectural Invariant: NOT a Second Source of Truth
The Digital Twin must **NOT** become an independent source of truth, an ungrounded state cache, or an AI hallucination sandbox. It is strictly a **governed projection of reality**.

The canonical pipeline is:
```text
EXTERNAL SOURCE / CONNECTOR / TELEMETRY
                   │
                   ▼
            EVIDENCE RECORD
                   │
                   ▼
     REALITY KERNEL (EvidenceGraph + RealityDecay)
                   │
                   ▼
       DIGITAL TWIN PROJECTION (Strict Classification)
                   │
                   ▼
          STRATEGY SIMULATOR (Feasibility Gates)
                   │
                   ▼
          MISSION / EXECUTION (Phase 2 Wall)
```

The Digital Twin cannot unilaterally promote claims, manufacture facts, or convert estimates into recognized realities.

---

## 2. Strict Reality Classification

Every single field and metric within the Company Digital Twin carries an explicit `TruthClassification`.

```csharp
public enum TruthClassification
{
    Fact = 1,         // Grounded in verified external evidence, cryptographic hashes, or reconciled ledgers
    Estimate = 2,     // Derived from statistical modeling, historical ACV/win-rates, or formulaic approximations
    Hypothesis = 3,   // Proposed future scenario, AI assumption, or ungrounded strategic speculation
    Observation = 4,  // Raw external telemetry or connector reading before corroboration
    Learning = 5,     // Institutional memory, historical post-mortem, or playbook guidance
    Unknown = 6       // Insufficient or absent evidence (First-Class State: NOT zero, NOT synthetic)
}
```

### Promotion Invariant State Machine
The classification boundaries are strictly enforced. The following transitions are **BLOCKED** without the governed promotion process through `IEvidenceGraph`:
- `ESTIMATE` $\to$ `FACT` : **BLOCKED**
- `HYPOTHESIS` $\to$ `FACT` : **BLOCKED**
- `LEARNING` $\to$ `FACT` : **BLOCKED**
- `OBSERVATION` $\to$ `FACT` : **BLOCKED** (Must be corroborated with confidence $\ge 0.70$ and active non-revoked sources)
- `UNKNOWN` $\to$ `FACT` : **BLOCKED** (Requires empirical evidence records)

---

## 3. UNKNOWN is a First-Class State

When Charlie does not possess sufficient, fresh, and corroborated evidence for any business dimension:
- The Digital Twin **MUST** return `TruthClassification.Unknown`.
- It **MUST NEVER** substitute `0m`, `0`, or `null` disguised as certainty.
- It **MUST NEVER** synthesize fake company data, fake deals, fake employee counts, or fake revenue to appease UI components.

`Unknown` signals executive uncertainty and triggers strategic inquiry or discovery missions.

---

## 4. Field-Level Provenance & Metrology

A single global confidence score is prohibited. Every Digital Twin field supports comprehensive metrology:

```csharp
public class DigitalTwinField<T>
{
    public string FieldName { get; set; }
    public T? Value { get; set; }
    public TruthClassification Classification { get; set; }
    public double Confidence { get; set; } // 0.0 to 1.0
    public List<Guid> EvidenceRecordIds { get; set; } = new();
    public List<string> GroundingEvidenceHashes { get; set; } = new();
    public string Source { get; set; }
    public DateTime ObservedAt { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public FreshnessState Freshness { get; set; }
    public TimeSpan EvidenceAge => DateTime.UtcNow - ObservedAt;
    public bool IsDisputed { get; set; }
    public Guid? ActiveConflictId { get; set; }
}
```

Example:
Recognized Revenue = `₹10,00,000` is traced directly to:
- Source: `PaymentGateway (Razorpay)`
- EvidenceRecordId: `3f2504e0-4f89-41d3-9a0c-0305e82c3301`
- Observation: Settled payout payload hash `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`
- Classification: `Fact`
- Freshness: `VERIFIED`
- Confidence: `1.0`

---

## 5. The 22 Business Dimensions

The Digital Twin exposes state across 22 canonical enterprise dimensions, integrating existing models (`FinancialReality`, `CommercialReality`, `DeliveryReality`, `ConnectorReality`, `RevenueBaseline`) while eliminating duplicate competing models:

1. **Financial**: Cash in bank, monthly burn, runway days, settled cash, outstanding receivables.
2. **Revenue**: Recognized revenue, contracted guaranteed revenue, four-state revenue baseline.
3. **Commercial**: Pipeline metrics, ACV, sales cycle, win rate.
4. **Customers**: Active accounts, churn rate, account health scores.
5. **Prospects**: Verified prospects, ICP qualification status, lead velocity.
6. **Opportunities**: Active deal stages, expected close dates, proposal statuses.
7. **Products**: Active product offerings, tier configurations, delivery models.
8. **Services**: Professional service catalog, implementation packages, consulting tiers.
9. **Employees**: Verified headcount, talent capabilities, team utilization.
10. **Departments**: Engineering, Sales, Product, Operations, Executive.
11. **Sales**: Sales velocity, quotas, rep allocation, conversion rates.
12. **Marketing**: Lead generation channels, campaign attribution, CAC.
13. **Operations**: Delivery capacity, project throughput, slot allocations.
14. **Delivery**: Active customer projects, engineering slots, implementation milestones.
15. **Inventory**: (Physical/Digital resource inventories, licenses, cloud allocations).
16. **Suppliers**: Cloud infrastructure vendors, API providers, telephony carriers.
17. **Partners**: Channel partners, strategic alliances, referral networks.
18. **Contracts**: Signed MSAs, active SOWs, payment milestones, SLA commitments.
19. **Connectors**: External integration telemetry (CRM, Bank, Gateway, Web, Voice).
20. **Market Signals**: Competitive intelligence, market pricing shifts, demand indicators.
21. **Risks**: Payment default risk, capacity constraints, connector degradation, compliance risks.
22. **Objectives / Strategic State**: CEO revenue mandate, active strategy routes, mission statuses.

---

## 6. Reality Freshness & Reality Decay (H3 Integration)

Every time-sensitive field is evaluated through `IRealityDecayEngine`:
- **VERIFIED (Fresh)**: Age $\le$ AgingThreshold (e.g. 30 days for general metrics, 15 days for strict financial metrics).
- **AGING**: AgingThreshold $<$ Age $\le$ StaleThreshold (confidence degraded according to half-life).
- **STALE**: StaleThreshold $<$ Age $\le$ UnknownThreshold (strictly unusable for verified planning).
- **UNKNOWN**: Age $>$ UnknownThreshold (decayed into UNKNOWN; stale facts are never presented as current facts).

---

## 7. Conflicting Evidence & Disputed State

When disparate sources report conflicting data (e.g., CRM reports revenue = ₹50L, Payment Gateway reports revenue = ₹46L):
- Charlie does **NOT** arbitrarily pick one or silently average them.
- A deterministic `DigitalTwinConflict` is generated:
  - `SourceA` vs `SourceB`
  - `ValueA` vs `ValueB`
  - `Timestamps`, `ConfidenceScores`, `FieldPath`
  - `ResolutionStatus`: `Unresolved`, `Resolved`, `Superseded`
- The field is flagged as `IsDisputed = true`.
- Strategy simulators are notified that the underlying reality is disputed.

---

## 8. Multi-Tenant Security & Tenant Isolation

1. **Server-Side Enforcement**: All Digital Twin operations are scoped by `WorkspaceId`.
2. **Zero Cross-Tenant Leakage**: Queries, dimensions, snapshots, history, conflicts, and evidence traces require workspace authorization verified via `IUserContextService`.
3. **Quarantine Test**: Tenant A requesting Tenant B's Digital Twin state, snapshots, or conflicts receives an immediate HTTP 403 / unauthorized exception.

---

## 9. Versioning, Immutable Snapshots, and Diff Engine

### DigitalTwinSnapshot
```csharp
public class DigitalTwinSnapshot : Entity
{
    public Guid WorkspaceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string SourceVersion { get; set; } = "1.0";
    public string RealityVersion { get; set; } = "1.0";
    public string EvidenceVersion { get; set; } = "1.0";
    public string TwinVersion { get; set; } = "2.0";
    public string IntegrityHash { get; set; } = string.Empty;
    public string SerializedStateJson { get; set; } = "{}";
}
```

### DigitalTwinDiff
A deterministic comparison between Snapshot A and Snapshot B:
- `Added`: Fields newly discovered or grounded.
- `Removed`: Fields deleted or deprecated.
- `Changed`: Values changed between snapshots.
- `BecameStale`: Fields that transitioned from Fresh $\to$ Stale.
- `BecameUnknown`: Fields that lost evidence or decayed past the threshold.
- `ConfidenceIncreased`: Confidence delta $> 0$.
- `ConfidenceDecreased`: Confidence delta $< 0$.
- `ClassificationChanged`: (e.g. Observation $\to$ Fact).
- `EvidenceChanged`: Evidence record IDs or source additions.

---

## 10. Digital Twin Health Metrology

The Digital Twin computes an objective, unmanufactured health report:
```csharp
public class DigitalTwinHealthReport
{
    public double EvidenceCoveragePercent { get; set; } // % of fields grounded in evidence
    public double FreshnessPercent { get; set; }        // % of fields in Fresh state
    public double UnknownRatioPercent { get; set; }     // % of fields in Unknown state
    public double StaleRatioPercent { get; set; }       // % of fields in Stale state
    public double ConflictRatioPercent { get; set; }    // % of fields currently in disputed/conflict state
    public double AverageConfidence { get; set; }       // Average confidence of known fields
    public int ActiveConnectorCount { get; set; }       // Number of healthy telemetry connectors
    public int TotalTrackedFields { get; set; }
}
```

---

## 11. AI Boundary & Revenue Safety Invariant

1. **AI Safety Law**:
   - AI models (Gemini, Claude, GPT, local LLMs) may summarize, suggest, hypothesize, and extract observations.
   - AI models **CANNOT** create `FACT`.
   - AI models **CANNOT** directly promote a hypothesis or claim into `FACT`.
   - AI models **CANNOT** mutate recognized revenue or overwrite evidence hashes.

2. **Revenue Safety Law**:
   - Pipeline, weighted pipeline, run rate, AI estimate, forecast, simulation, and hypothesis **CANNOT** become recognized revenue.
   - Recognized revenue must originate from reconciled payment or banking evidence records.

---

## 12. Service Contract (`ICompanyDigitalTwinService`)

```csharp
public interface ICompanyDigitalTwinService
{
    Task<DigitalTwinState> GetCurrentStateAsync(Guid workspaceId, CancellationToken ct = default);
    Task<DigitalTwinDimensionState> GetDimensionAsync(Guid workspaceId, DigitalTwinDimension dimension, CancellationToken ct = default);
    Task<DigitalTwinFieldState?> GetFieldAsync(Guid workspaceId, string fieldPath, CancellationToken ct = default);
    Task<IReadOnlyList<DigitalTwinSnapshotSummary>> GetHistoryAsync(Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<DigitalTwinConflictRecord>> GetConflictsAsync(Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<DigitalTwinFieldState>> GetStaleDataAsync(Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<DigitalTwinFieldState>> GetUnknownsAsync(Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<EvidenceRecord>> GetEvidenceForFieldAsync(Guid workspaceId, string fieldPath, CancellationToken ct = default);
    Task<DigitalTwinSnapshot> CreateSnapshotAsync(Guid workspaceId, CancellationToken ct = default);
    Task<DigitalTwinDiff> CompareSnapshotsAsync(Guid workspaceId, Guid snapshotAId, Guid snapshotBId, CancellationToken ct = default);
    Task<DigitalTwinHealthReport> GetHealthReportAsync(Guid workspaceId, CancellationToken ct = default);
}
```

---

## 13. Verification Matrix
The test suite `tests/BusinessModelApp.Tests/Domain/Phase2Batch2DigitalTwinTests.cs` validates:
- Classification non-collapse invariants.
- Promotion gates (unauthorized promotion fails closed).
- Multi-tenant boundary quarantine (cross-tenant queries fail closed).
- Field-level provenance and verifiable hash lineage.
- Deterministic conflict detection and resolution states.
- Snapshot idempotency and diff determinism.
- Revenue safety enforcement (pipeline cannot become revenue).
- Unknown-world verification (missing data yields UNKNOWN, zero fake data).
- Adversarial red-team attacks (AI hallucination injection, cross-tenant snapshot tampering, unauthorized fact promotion).
