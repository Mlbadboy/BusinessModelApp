# AI Provider & Routing Policy Catalog

## 1. Controlled AI Task Taxonomy (`AITaskType`)

Agents request execution by **Task Type**, never by raw provider or model name.

| Task Type | Description | Default Preference | Target Strategy | Temperature | Max Tokens |
|---|---|---|---|---|---|
| `ExecutiveBrief` | Morning synthesized brief over verified financial facts | `HighQuality` | `anthropic/claude-3-5-sonnet` | `0.2` | `1500` |
| `VoiceQualification` | Real-time inbound phone call qualification | `UltraLowLatency` | `google/gemini-2.0-flash-001` | `0.1` | `512` |
| `Classification` | Triage and categorization of inbound inquiries | `CostControlled` | `deepseek/deepseek-chat` | `0.0` | `256` |
| `LeadQualification` | In-depth qualification of deal readiness | `Balanced` | `openai/gpt-4o-mini` | `0.2` | `1024` |
| `OpportunityAnalysis` | Strategic deal risk and blocker analysis | `Balanced` | `openai/gpt-4o-mini` | `0.2` | `1024` |
| `GeneralAssistant` | General operational problem solving & code assistance | `Balanced` | `openai/gpt-4o-mini` | `0.3` | `2048` |

---

## 2. Provider Selection Modes (`ProviderSelectionMode`)

- **`GatewayManaged`**: OmniRoute dynamically manages provider selection, failover pools, and upstream latency optimization.
- **`FixedApprovedModel`**: Specific task types bind to vetted model strategies (e.g. Claude 3.5 Sonnet for Executive Briefs).
- **`FallbackPool`**: Seamless failover to secondary providers in the event of upstream rate limits or outages.
