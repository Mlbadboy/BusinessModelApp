# CHARLIE BUSINESS OS — BATCH 4 MARKET RADAR DESIGN
## Signal Engine, Prioritization & Competitor Tracking

---

## 1. 19 Canonical External Signal Types

1. `PriceChange`
2. `ProductLaunch`
3. `ProductDiscontinuation`
4. `CompetitorMove`
5. `MarketGrowth`
6. `MarketDecline`
7. `CustomerComplaint`
8. `CustomerPreference`
9. `RegulatoryChange`
10. `HiringSurge`
11. `HiringDecline`
12. `FundingEvent`
13. `Partnership`
14. `DistributionChange`
15. `TechnologyShift`
16. `DemandSpike`
17. `DemandDrop`
18. `SupplyDisruption`
19. `SentimentShift`

---

## 2. Deterministic Prioritization Scoring

Signals are prioritized algorithmically without relying on opaque LLM scores:

$$\text{PriorityScore} = (\text{Magnitude} \times 0.35) + (\text{Confidence} \times 0.25) + (\text{Freshness} \times 0.20) + (\text{Novelty} \times 0.20)$$

| Priority Tier | Score Threshold | SLA / Executive Surfacing |
| :--- | :---: | :--- |
| **CRITICAL** | $\ge 0.75$ | Immediate executive alert, auto-generates opportunity or threat evaluation |
| **HIGH** | $\ge 0.55$ | Priority review in executive radar briefing |
| **MEDIUM** | $\ge 0.35$ | Active tracking in market radar queue |
| **LOW** | $< 0.35$ | Background telemetry, archived upon expiration |

---

## 3. Competitor Intelligence Model

Competitor profiles (`CompetitorProfile`) maintain structured dimensions:
* **Pricing Sheet**: Active pricing tiers and promotional discounts.
* **Observed Positioning**: Target enterprise segment and value proposition.
* **Observed Strategy**: Growth vector (expansion, defense, enterprise, self-serve).
* **Hiring Signal**: Surge/Decline in engineering, sales, or executive headcount.
* **Uncertainty Score**: Explicitly tracks what Charlie *does not know* about the competitor.
