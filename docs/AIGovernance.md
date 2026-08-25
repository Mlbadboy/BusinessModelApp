# AI Governance, Telemetry & Cost Attribution

## 1. Multi-Tenant Governance
1. **Context Enforcement**: All outbound AI inferences are tagged with server-derived headers:
   - `X-Organization-Id`: Enforcing organization tenancy.
   - `X-Workspace-Id`: Enforcing workspace scope.
   - `X-User-Id`: Authenticated user ID.
   - `X-Correlation-Id`: Unique request correlation ID for end-to-end tracing.
   - `X-Task-Type`: Controlled task type.
2. **Prompt Data Minimization**: Raw customer Personally Identifiable Information (PII) or un-redacted credentials are never sent to AI gateways.
3. **No Prompt Logging**: Raw prompts and responses are not stored by default in `AICallRecord` to prevent sensitive data leakage.

---

## 2. Immutable Cost & Usage Telemetry (`AICallRecord`)

Every AI inference creates an immutable record in the database:
- `OrganizationId` & `WorkspaceId`
- `TaskType`
- `Provider` & `Model`
- `PromptTokens` & `CompletionTokens`
- `EstimatedCost` (Nullable: recorded only when authoritative upstream pricing exists)
- `LatencyMs`
- `RequestCorrelationId`

---

## 3. Cost Attribution $\rightarrow$ Business Outcomes

The `AICallRecord` schema establishes the foundational relationships for AI ROI attribution:
$$\text{AI Cost (Tokens, INR)} \longrightarrow \text{Lead Qualification} \longrightarrow \text{Opportunity Value} \longrightarrow \text{Closed Won Revenue}$$
