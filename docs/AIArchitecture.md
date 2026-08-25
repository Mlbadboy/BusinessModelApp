# BusinessModelApp — AI Infrastructure Architecture

## 1. Architectural Principle
> **"BusinessModelApp owns the BUSINESS. OmniRoute owns AI TRAFFIC. Providers own MODEL INFERENCE."**

BusinessModelApp does not directly couple its business agents, controllers, or services to OpenAI, Anthropic, Google, DeepSeek, or any single AI provider. All AI requests flow through the provider-neutral `IAIInferenceGateway` to the **OmniRoute** infrastructure gateway.

---

## 2. Component Boundaries & Responsibilities

```text
┌────────────────────────────────────────────────────────┐
│                   BUSINESS APPLICATION                 │
│  • Lead Qualification        • Opportunity Lifecycle   │
│  • Deterministic Health      • Evidence Engine         │
│  • ExecutiveBriefService     • AutonomousAgent         │
└──────────────────────────┬─────────────────────────────┘
                           │ AIRequest (AITaskType, Verified Facts)
                           ▼
┌────────────────────────────────────────────────────────┐
│                  IAIInferenceGateway                   │
│  • Server-Side Tenant Context Resolution (UserContext) │
│  • Telemetry Logging (AICallRecord: Tokens, Latency)   │
│  • Normalized AIResponse Packaging                     │
└──────────────────────────┬─────────────────────────────┘
                           │ TaskType + Preference
                           ▼
┌────────────────────────────────────────────────────────┐
│                IAIRoutingPolicyService                 │
│  • Policy Selection (Quality, Latency, Cost Tiers)     │
│  • ProviderSelectionMode (GatewayManaged, Fallback)    │
└──────────────────────────┬─────────────────────────────┘
                           │ Standard HTTP (/v1/chat/completions)
                           │ Headers: X-Org-Id, X-Workspace-Id
                           ▼
┌────────────────────────────────────────────────────────┐
│             OMNIROUTE INFRASTRUCTURE GATEWAY           │
│  • Model Routing             • Provider Failover       │
│  • Rate Limiting & Retries   • In-Memory Caching       │
└──────────────────────────┬─────────────────────────────┘
                           │ Upstream Inference
                           ▼
┌────────────────────────────────────────────────────────┐
│   PROVIDERS (Claude 3.5 Sonnet, Gemini Flash, etc.)    │
└────────────────────────────────────────────────────────┘
```

---

## 3. Server-Side Security Authority
1. **No Agent-Supplied Identities**: `AIRequest.OrganizationId`, `AIRequest.WorkspaceId`, and `AIRequest.UserId` are strictly overwritten and enforced by `IUserContextService` from the authenticated principal.
2. **No Arbitrary Destination URLs**: The OmniRoute base URL is sourced solely from trusted server configuration (`appsettings.json` / Environment variables).
3. **Append-Only Telemetry**: Every AI interaction persists an immutable `AICallRecord` tracking token counts, latency, model, and provider. Updates and deletes are rejected at the database level.
