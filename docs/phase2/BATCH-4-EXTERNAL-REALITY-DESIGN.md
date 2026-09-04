# CHARLIE BUSINESS OS — BATCH 4 EXTERNAL REALITY DESIGN
## Ingestion, Cryptographic Provenance, Decay & Deduplication

---

## 1. Information Model: External Evidence

Every external observation ingested into Charlie's External Reality Fabric is modeled via `ExternalEvidenceRecord`:
* **Cryptographic Hashes**:
  - `ContentHash`: Verbatim SHA-256 hash of normalized payload.
  - `ClaimHash`: SHA-256 hash of title and canonical summary, enabling syndicated claim deduplication.
* **Temporal Metrology**:
  - `ObservedAt`: When the event occurred in the real world.
  - `RetrievedAt`: When Charlie fetched and verified the content.
  - `FreshUntil`: Calculated from domain-specific decay half-life.
* **Reality Classification**:
  - Strictly classified as `Observation` or `Estimate`.
  - **INVARIANT**: External evidence is NEVER promoted to `Fact` without multi-source authoritative verification.

---

## 2. Domain-Specific Reality Decay Half-Life

| Source Category | Half-Life | Freshness Boundary | Stale Boundary |
| :--- | :---: | :---: | :---: |
| **Price Feeds** | 2.0 days | 4 days | > 5 days |
| **Competitor Product Changes** | 3.0 days | 6 days | > 10 days |
| **Public Web / News** | 5.0 to 7.0 days | 14 days | > 21 days |
| **Social / Sentiment Signals** | 2.0 days | 4 days | > 7 days |
| **Industry Market Trends** | 30.0 days | 60 days | > 90 days |
| **Regulatory Disclosures** | 180.0 days | 365 days | > 540 days |

Formula:
$$\text{FreshnessScore} = \text{clamp}\left(0.5^{\frac{\text{AgeDays}}{\text{HalfLifeDays}}}, 0.0, 1.0\right)$$

---

## 3. Syndicated Claim Deduplication & Cluster Engine

Syndicated press releases distributed across multiple news aggregators are grouped via `SignalCluster`:
* **Syndication Ratio**:
  $$\text{SyndicationRatio} = \frac{\text{TotalCopyCount}}{\max(1, \text{UniqueSourceCount})}$$
* **Independence Decay**:
  $$\text{IndependenceEstimate} = \text{clamp}\left(\frac{1.0}{\max(\text{SyndicationRatio}, \sqrt{\text{TotalCopyCount}})}, 0.20, 1.0\right)$$

**Rule**: 10 identical wire articles do NOT equal 10 independent confirmations. Corroboration increases only when independent, distinct sources confirm the claim.
