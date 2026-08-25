# Business Metrics & Calculation Catalog

**Algorithm Version**: `HealthEngine:v1.0`  
**Purpose**: Provide deterministic, mathematically verifiable definitions for all commercial, pipeline, and business health metrics. No metric in this catalog may be estimated or altered by generative AI.

---

## 1. Primary Metrics

| Metric Key | Display Name | Definition | Mathematical Formula | Required Data Inputs | Period | Null / Insufficient Data Behavior | Owner |
|---|---|---|---|---|---|---|---|
| `METRIC_PIPELINE_VALUE` | Total Pipeline Value | Sum of estimated value across all active (non-ClosedLost) opportunities | $\sum \text{EstimatedValue}_i \quad \forall \, \text{Stage}_i \neq \text{ClosedLost}$ | Active Opportunities | Real-time | Returns `₹0`, Confidence: `1.0` | Growth |
| `METRIC_WEIGHTED_FORECAST` | Weighted Forecast | Probability-adjusted total pipeline value | $\sum (\text{EstimatedValue}_i \times \text{Probability}_i) \quad \forall \, \text{Stage}_i \neq \text{ClosedLost}$ | Opportunities with Probability | Real-time | Returns `₹0`, Confidence: `1.0` | Growth |
| `METRIC_CLOSED_WON_REVENUE` | Closed Won Value | Total revenue value from closed-won deals | $\sum \text{EstimatedValue}_i \quad \forall \, \text{Stage}_i = \text{ClosedWon}$ | Closed Opportunities | Real-time / Period | Returns `₹0`, Confidence: `1.0` | Finance |
| `METRIC_PIPELINE_COVERAGE` | Pipeline Coverage Ratio | Weighted forecast divided by workspace revenue target | $\frac{\text{Weighted Forecast}}{\text{Revenue Target}}$ (Default target = ₹50,00,000 / Quarter if unconfigured) | Weighted Forecast, Workspace Target | Current Quarter | If Target = 0, fallback to Default Target; if no opps, ratio = `0.0` | Executive |
| `METRIC_WIN_RATE` | Commercial Win Rate | Ratio of Closed Won deals over total resolved deals | $\frac{N_{\text{ClosedWon}}}{N_{\text{ClosedWon}} + N_{\text{ClosedLost}}}$ | Resolved Opportunities | Last 90 Days | If total closed < 3, returns baseline `0.5`, Confidence: `Low (0.2)` | Growth |
| `METRIC_CONVERSION_VELOCITY` | Conversion Velocity | Average duration in days from Opportunity creation to ClosedWon or current active age | $\frac{1}{N} \sum (\text{ClosedDate}_i - \text{CreatedDate}_i)$ | Opportunities with timestamps | Last 90 Days | If no closed opps, uses active stage age average. | Operations |
| `METRIC_STALLED_RISK_INDEX` | Stalled Deal Risk Index | Percentage of pipeline value in opportunities with no logged activity in > 14 days | $\frac{\sum \text{Value}_{\text{inactive > 14d}}}{\text{Total Pipeline Value}} \times 100$ | Opportunities, Activities | Current | Returns `0%` if no opps exist. | Risk/Strategy |
| `METRIC_LEAD_QUAL_RATE` | Lead Qualification Rate | Percentage of inbound leads successfully qualified to Opportunity | $\frac{N_{\text{Qualified Leads}}}{N_{\text{Total Leads}}} \times 100$ | Inbound Leads | Last 30 Days | If Leads < 3, returns `0%`, Confidence: `Low (0.25)` | Growth |

---

## 2. Business Health Score Formulation (`HealthEngine:v1.0`)

The overall **Business Health Score** ($H$) is a bounded composite index from `0` to `100`:

$$H = w_1 \cdot S_{\text{pipeline}} + w_2 \cdot S_{\text{conversion}} + w_3 \cdot S_{\text{velocity}} + w_4 \cdot S_{\text{risk}}$$

### Component Sub-Scores & Standard Weights:

| Component | Weight ($w_i$) | Component Formula & Scaling |
|---|---|---|
| **Pipeline Coverage Sub-score ($S_{\text{pipeline}}$)** | `40%` ($0.40$) | $\text{Clamp}\left(\frac{\text{Coverage Ratio}}{2.0} \times 100, \, 0, \, 100\right)$ *(2.0x coverage = 100 pts)* |
| **Lead & Deal Conversion Sub-score ($S_{\text{conversion}}$)** | `25%` ($0.25$) | $(\text{Win Rate} \times 50) + (\text{Lead Qual Rate} \times 50)$ |
| **Activity Velocity Sub-score ($S_{\text{velocity}}$)** | `20%` ($0.20$) | $\text{Clamp}\left(100 - (\text{Avg Days to Close} \times 1.5), \, 20, \, 100\right)$ |
| **Risk Mitigation Sub-score ($S_{\text{risk}}$)** | `15%` ($0.15$) | $\text{Clamp}\left(100 - \text{Stalled Risk Index}, \, 0, \, 100\right)$ |

---

## 3. Data Completeness & Confidence Score Formulation

> **Principle**: Insufficient historical data results in **Low Statistical Confidence**, NOT an artificial low health score.

The **Confidence Score** ($C \in [0.0, 1.0]$) is computed based on data sample size thresholds:

$$C = 0.40 \cdot \min\left(1.0, \frac{N_{\text{opps}}}{5}\right) + 0.30 \cdot \min\left(1.0, \frac{N_{\text{leads}}}{10}\right) + 0.30 \cdot \min\left(1.0, \frac{N_{\text{activities}}}{15}\right)$$

- **High Confidence**: $C \ge 0.75$ (Sufficient statistical basis for executive forecasting)
- **Medium Confidence**: $0.40 \le C < 0.75$ (Early trends emerging; review outliers)
- **Low Confidence**: $C < 0.40$ (Insufficient sample size; baseline hypotheses applied)

---

## 4. Explainable Evidence Contract

Every calculated fact produces a structured, citeable `EvidenceRecord`:

```json
{
  "evidenceId": "EVD-2026-08-001",
  "evidenceType": "Pipeline",
  "metricKey": "METRIC_WEIGHTED_FORECAST",
  "metricValue": 1850000.0,
  "formattedValue": "₹18,50,000",
  "calculationVersion": "HealthEngine:v1.0",
  "formula": "Σ(EstimatedValue * Probability)",
  "period": "2026-Q3",
  "sourceEntities": [
    { "entityType": "Opportunity", "id": "6b3f...", "name": "Apex Retail - Enterprise Rollout", "contributionValue": 925000.0 }
  ],
  "confidenceScore": 0.85,
  "impactLevel": "High",
  "generatedAt": "2026-08-25T14:15:00Z"
}
```
