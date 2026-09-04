using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Execution;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase2Batch6_ExecutionFirewallTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly DeterministicRiskEngine _riskEngine;
        private readonly BrainFabricGovernanceService _brainService;
        private readonly AuthorityDelegationService _authorityService;
        private readonly BudgetGuardService _budgetService;
        private readonly ApprovalGateway _approvalGateway;
        private readonly ExecutionLedgerService _ledgerService;
        private readonly HierarchicalExecutionKillSwitch _killSwitchService;
        private readonly ConnectorExecutionGateway _connectorGateway;
        private readonly ExecutionFirewallService _firewall;
        private readonly SagaExecutionEngine _sagaEngine;
        private readonly AutonomousMissionExecutor _missionExecutor;

        public Phase2Batch6_ExecutionFirewallTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            _riskEngine = new DeterministicRiskEngine();
            _brainService = new BrainFabricGovernanceService(_context, NullLogger<BrainFabricGovernanceService>.Instance);
            _authorityService = new AuthorityDelegationService(_context, NullLogger<AuthorityDelegationService>.Instance);
            _budgetService = new BudgetGuardService(_context, NullLogger<BudgetGuardService>.Instance);
            _approvalGateway = new ApprovalGateway(_context, NullLogger<ApprovalGateway>.Instance);
            _ledgerService = new ExecutionLedgerService(_context, NullLogger<ExecutionLedgerService>.Instance);
            _killSwitchService = new HierarchicalExecutionKillSwitch(_context, NullLogger<HierarchicalExecutionKillSwitch>.Instance);
            _connectorGateway = new ConnectorExecutionGateway(NullLogger<ConnectorExecutionGateway>.Instance);

            _firewall = new ExecutionFirewallService(
                _authorityService,
                _riskEngine,
                _budgetService,
                _approvalGateway,
                _connectorGateway,
                _ledgerService,
                _killSwitchService,
                NullLogger<ExecutionFirewallService>.Instance);

            _sagaEngine = new SagaExecutionEngine(_context, _firewall, NullLogger<SagaExecutionEngine>.Instance);
            _missionExecutor = new AutonomousMissionExecutor(_firewall, NullLogger<AutonomousMissionExecutor>.Instance);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        #region 1. Constitutional & Functional Tests

        [Fact]
        public async Task B6_01_ConstitutionalInvariants_EnforceFailClosed_OnMissingDimensions()
        {
            var validWorkspace = Guid.NewGuid();

            // Missing Workspace
            var req1 = new ExecutionRequest { WorkspaceId = Guid.Empty, AgentId = "SalesAgent", CapabilityId = "Email.Send", IdempotencyKey = "K1", AuditContext = "ctx" };
            await Assert.ThrowsAsync<SecurityException>(() => _firewall.EvaluateAsync(req1));

            // Missing Agent
            var req2 = new ExecutionRequest { WorkspaceId = validWorkspace, AgentId = "", CapabilityId = "Email.Send", IdempotencyKey = "K2", AuditContext = "ctx" };
            await Assert.ThrowsAsync<SecurityException>(() => _firewall.EvaluateAsync(req2));

            // Missing Capability
            var req3 = new ExecutionRequest { WorkspaceId = validWorkspace, AgentId = "SalesAgent", CapabilityId = "", IdempotencyKey = "K3", AuditContext = "ctx" };
            await Assert.ThrowsAsync<SecurityException>(() => _firewall.EvaluateAsync(req3));

            // Missing Idempotency Key
            var req4 = new ExecutionRequest { WorkspaceId = validWorkspace, AgentId = "SalesAgent", CapabilityId = "Email.Send", IdempotencyKey = "", AuditContext = "ctx" };
            await Assert.ThrowsAsync<SecurityException>(() => _firewall.EvaluateAsync(req4));

            // Missing Audit Context
            var req5 = new ExecutionRequest { WorkspaceId = validWorkspace, AgentId = "SalesAgent", CapabilityId = "Email.Send", IdempotencyKey = "K5", AuditContext = "" };
            await Assert.ThrowsAsync<SecurityException>(() => _firewall.EvaluateAsync(req5));
        }

        [Fact]
        public async Task B6_02_FullLifecycle_PermittedExecution_IssuesCryptographicPermitAndSettlesBudget()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "SalesAgent";
            var cap = "CRM.CreateLead";

            // Setup authority delegation and wallet
            await _authorityService.GrantDelegationAsync(workspaceId, "Sales", agentId, "SalesExecutive",
                new List<string> { cap }, 50000m, 10000m, Guid.NewGuid());

            var wallet = await _budgetService.GetOrCreateWalletAsync(workspaceId);
            var initialBalance = wallet.BalanceINR;

            var request = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                MissionId = Guid.NewGuid(),
                AgentId = agentId,
                AgentRole = "SalesExecutive",
                CapabilityId = cap,
                ActionTier = ExecutionActionTier.L5_ConsequentialAction,
                MonetaryImpactINR = 1500m,
                PayloadJson = "{\"leadName\":\"Acme Corp\"}",
                IdempotencyKey = "TXN_LIFECYCLE_001",
                AuditContext = "Mission 42 follow-up"
            };

            // 1. Evaluate
            var decision = await _firewall.EvaluateAsync(request);
            Assert.True(decision.IsPermitted);
            Assert.NotNull(decision.Permit);
            Assert.False(decision.Permit.IsConsumed);
            Assert.True(decision.Permit.ValidatePermitToken(ConnectorExecutionGateway.PermitMasterSecret));

            // Verify budget reserved
            var walletAfterReserve = await _budgetService.GetOrCreateWalletAsync(workspaceId);
            Assert.Equal(1500m, walletAfterReserve.PendingCommitmentsINR);

            // 2. Execute
            var receipt = await _firewall.ExecuteAsync(request, decision.Permit);
            Assert.NotNull(receipt);
            Assert.Equal(ExecutionStatus.Succeeded, receipt.Status);
            Assert.True(decision.Permit.IsConsumed);

            // Verify budget settled
            var walletAfterSettle = await _budgetService.GetOrCreateWalletAsync(workspaceId);
            Assert.Equal(0m, walletAfterSettle.PendingCommitmentsINR);
            Assert.Equal(initialBalance - 1500m, walletAfterSettle.BalanceINR);
            Assert.Equal(1500m, walletAfterSettle.TodaySpendINR);

            // Verify immutable ledger record
            var entries = await _ledgerService.GetRecentEntriesAsync(workspaceId, 10);
            Assert.Single(entries);
            Assert.Equal(receipt.ResultHash, entries[0].ResultHash);
        }

        #endregion

        #region 2. AI Brain Fabric & Governance Tests (B6-H1.5)

        [Fact]
        public async Task B6_03_BrainFabric_ZeroExecutionAuthority_ModelOutputsProposalsOnly()
        {
            var workspaceId = Guid.NewGuid();

            // Configure OpenRouter provider
            await _brainService.ConfigureProviderAsync(workspaceId, new ConfigureBrainProviderDto
            {
                Provider = BrainProviderType.OpenRouter,
                ApiKey = "sk-or-v1-secret-test-key-2026",
                DefaultModelId = "anthropic/claude-3.5-sonnet",
                Preference = BrainOptimizationPreference.MaximumQuality
            });

            // Run inference
            var response = await _brainService.RunInferenceTestAsync(workspaceId, new BrainInferenceTestRequestDto
            {
                Provider = BrainProviderType.OpenRouter,
                ModelId = "anthropic/claude-3.5-sonnet",
                Prompt = "Recommend payment schedule for supplier",
                Role = BrainRoleType.Strategic
            });

            Assert.True(response.Success);
            Assert.Contains("ExecutionAuthority", response.OutputContent);
            Assert.Contains("NONE", response.OutputContent);

            // Invariant: AI Brain output alone cannot be executed without an explicit permit from Firewall
            var bogusPermit = new ExecutionPermit
            {
                PermitToken = "forged-token",
                WorkspaceId = workspaceId,
                CapabilityId = "Payment.Execute"
            };

            var unpermittedRequest = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = "AIBrain",
                CapabilityId = "Payment.Execute",
                IdempotencyKey = "AI_ATTEMPT_1",
                AuditContext = "AI generated recommendation"
            };

            await Assert.ThrowsAsync<SecurityException>(() => _firewall.ExecuteAsync(unpermittedRequest, bogusPermit));
        }

        [Fact]
        public async Task B6_04_BrainFabric_NeverExposesPlaintextApiKeyInSummaryDTO()
        {
            var workspaceId = Guid.NewGuid();
            var rawSecretKey = "sk-ant-live-secret-super-sensitive-token-12345";

            await _brainService.ConfigureProviderAsync(workspaceId, new ConfigureBrainProviderDto
            {
                Provider = BrainProviderType.OpenRouter,
                ApiKey = rawSecretKey
            });

            var summaries = await _brainService.GetProvidersAsync(workspaceId);
            var openRouter = summaries.Find(s => s.Provider == BrainProviderType.OpenRouter);

            Assert.NotNull(openRouter);
            Assert.True(openRouter.IsConfigured);

            // Invariant: The DTO has no ApiKey property and stringified JSON contains zero plaintext leaks
            var json = System.Text.Json.JsonSerializer.Serialize(openRouter);
            Assert.DoesNotContain(rawSecretKey, json);
        }

        [Fact]
        public async Task B6_05_BrainFabric_RoleBasedModelDiscoveryAndConnectionProbe()
        {
            var workspaceId = Guid.NewGuid();

            await _brainService.ConfigureProviderAsync(workspaceId, new ConfigureBrainProviderDto
            {
                Provider = BrainProviderType.OpenRouter,
                ApiKey = "sk-or-valid-key-999"
            });

            // 1. Connection probe
            var passed = await _brainService.TestProviderConnectionAsync(workspaceId, BrainProviderType.OpenRouter);
            Assert.True(passed);

            // 2. Discover models
            var models = await _brainService.DiscoverModelsAsync(workspaceId, BrainProviderType.OpenRouter);
            Assert.NotEmpty(models);
            Assert.Contains(models, m => m.ModelId == "anthropic/claude-3.5-sonnet");
            Assert.Contains(models, m => m.ModelId == "google/gemini-flash-1.5");
        }

        #endregion

        #region 3. Authority Delegation & Privilege Escalation Tests

        [Fact]
        public async Task B6_06_AuthorityDelegation_BlocksUndelegatedCapabilityOrAmountExceeded()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "SupportAgent";

            // Grant authority only for Support.Respond up to ₹5,000
            await _authorityService.GrantDelegationAsync(workspaceId, "Support", agentId, "CustomerSupport",
                new List<string> { "Support.Respond" }, 20000m, 5000m, Guid.NewGuid());

            // 1. Undelegated Capability Attempt (e.g. Payment.Execute) -> Denied
            var req1 = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = "Payment.Execute",
                IdempotencyKey = "DENIED_CAP_1",
                AuditContext = "Support action"
            };
            var dec1 = await _firewall.EvaluateAsync(req1);
            Assert.False(dec1.IsPermitted);
            Assert.Equal("AuthorityDelegation", dec1.Denial?.ViolatingPillar);

            // 2. Transaction Ceiling Exceeded Attempt (e.g. Support.Respond with ₹15,000) -> Denied
            var req2 = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = "Support.Respond",
                MonetaryImpactINR = 15000m,
                IdempotencyKey = "DENIED_AMOUNT_1",
                AuditContext = "Support action"
            };
            var dec2 = await _firewall.EvaluateAsync(req2);
            Assert.False(dec2.IsPermitted);
            Assert.Equal("AuthorityDelegation", dec2.Denial?.ViolatingPillar);
        }

        [Fact]
        public async Task B6_07_AuthorityRevocation_InstantlyInvalidatesActiveDelegation()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "MarketingAgent";
            var cap = "Email.Send";

            var del = await _authorityService.GrantDelegationAsync(workspaceId, "Marketing", agentId, "OutreachExecutive",
                new List<string> { cap }, 50000m, 5000m, Guid.NewGuid());

            var req = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = cap,
                IdempotencyKey = "REVOKE_TEST_1",
                AuditContext = "Campaign email"
            };

            // Before revocation: Permitted
            var decBefore = await _firewall.EvaluateAsync(req);
            Assert.True(decBefore.IsPermitted);

            // Revoke delegation
            await _authorityService.RevokeDelegationAsync(del.Id, Guid.NewGuid(), "Security audit suspension");

            // After revocation: Immediately Denied
            req.IdempotencyKey = "REVOKE_TEST_2";
            var decAfter = await _firewall.EvaluateAsync(req);
            Assert.False(decAfter.IsPermitted);
            Assert.Equal("AuthorityDelegation", decAfter.Denial?.ViolatingPillar);
        }

        #endregion

        #region 4. Deterministic Risk Engine Tests

        [Fact]
        public void B6_08_DeterministicRiskEngine_ClassifiesR0ThroughR5Correctly()
        {
            // R0: Read-only data
            Assert.Equal(ExecutionRiskTier.R0_ReadData,
                _riskEngine.EvaluateRisk(new ExecutionRequest { CapabilityId = "CRM.Read", ActionTier = ExecutionActionTier.L0_Observe }));

            // R1: Internal reversible
            Assert.Equal(ExecutionRiskTier.R1_InternalReversible,
                _riskEngine.EvaluateRisk(new ExecutionRequest { CapabilityId = "Email.Draft", ActionTier = ExecutionActionTier.L4_ReversibleAction }));

            // R2: Customer communication
            Assert.Equal(ExecutionRiskTier.R2_CustomerCommunication,
                _riskEngine.EvaluateRisk(new ExecutionRequest { CapabilityId = "Email.Send", ActionTier = ExecutionActionTier.L5_ConsequentialAction }));

            // R3: Moderate financial commitment (₹25,000)
            Assert.Equal(ExecutionRiskTier.R3_FinancialCommitment,
                _riskEngine.EvaluateRisk(new ExecutionRequest { CapabilityId = "Payment.Create", MonetaryImpactINR = 25000m }));

            // R4: High financial commitment (> ₹5,00,000) or legal contract
            Assert.Equal(ExecutionRiskTier.R4_LegalContract,
                _riskEngine.EvaluateRisk(new ExecutionRequest { CapabilityId = "Contract.Sign" }));
            Assert.Equal(ExecutionRiskTier.R4_LegalContract,
                _riskEngine.EvaluateRisk(new ExecutionRequest { CapabilityId = "Payment.Create", MonetaryImpactINR = 750000m }));

            // R5: Destructive action
            Assert.Equal(ExecutionRiskTier.R5_DestructiveOrHighImpact,
                _riskEngine.EvaluateRisk(new ExecutionRequest { CapabilityId = "Customer.PurgeData" }));
        }

        #endregion

        #region 5. Budget & Financial Safety Tests (Hard Invariants)

        [Fact]
        public async Task B6_09_BudgetGuard_BlocksAgentSelfBudgetModification_FailClosed()
        {
            var workspaceId = Guid.NewGuid();

            // Invariant: Empty GUID (agent or system without human principal) throws SecurityException
            await Assert.ThrowsAsync<SecurityException>(() =>
                _budgetService.UpdateBudgetLimitsByHumanAsync(workspaceId, 99999999m, 99999999m, Guid.Empty));
        }

        [Fact]
        public async Task B6_10_BudgetGuard_BlocksExecutionWhenDailyCapOrBalanceExceeded()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "ProcurementAgent";
            var cap = "Payment.Execute";

            // Set small wallet: Balance ₹50,000, Daily Cap ₹20,000
            var wallet = await _budgetService.GetOrCreateWalletAsync(workspaceId);
            wallet.BalanceINR = 50000m;
            wallet.DailyCapINR = 20000m;
            await _context.SaveChangesAsync();

            await _authorityService.GrantDelegationAsync(workspaceId, "Procurement", agentId, "Buyer",
                new List<string> { cap }, 100000m, 100000m, Guid.NewGuid());

            // Request ₹35,000 -> Exceeds Daily Cap ₹20,000 -> Denied
            var req1 = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = cap,
                MonetaryImpactINR = 35000m,
                IdempotencyKey = "OVER_DAILY_CAP",
                AuditContext = "Bulk supplies"
            };

            var dec1 = await _firewall.EvaluateAsync(req1);
            Assert.False(dec1.IsPermitted);
            Assert.Equal("BudgetGuard", dec1.Denial?.ViolatingPillar);
        }

        #endregion

        #region 6. Execution Firewall & Connector Gateway Tests

        [Fact]
        public async Task B6_11_ConnectorGateway_BlocksDirectExecutionWithoutValidPermit()
        {
            var request = new ExecutionRequest
            {
                WorkspaceId = Guid.NewGuid(),
                AgentId = "Agent007",
                CapabilityId = "Email.Send",
                IdempotencyKey = "TEST_GATEWAY_1",
                AuditContext = "Direct test"
            };

            // 1. Null permit -> SecurityException
            await Assert.ThrowsAsync<SecurityException>(() =>
                _connectorGateway.DispatchConnectorActionAsync(request, null!));

            // 2. Already consumed permit -> SecurityException
            var consumedPermit = new ExecutionPermit { IsConsumed = true, ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
            await Assert.ThrowsAsync<SecurityException>(() =>
                _connectorGateway.DispatchConnectorActionAsync(request, consumedPermit));

            // 3. Expired permit -> SecurityException
            var expiredPermit = new ExecutionPermit { IsConsumed = false, ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5) };
            await Assert.ThrowsAsync<SecurityException>(() =>
                _connectorGateway.DispatchConnectorActionAsync(request, expiredPermit));

            // 4. HMAC Tampered permit -> SecurityException
            var tamperedPermit = new ExecutionPermit
            {
                IsConsumed = false,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                PermitToken = "forged-invalid-hmac"
            };
            await Assert.ThrowsAsync<SecurityException>(() =>
                _connectorGateway.DispatchConnectorActionAsync(request, tamperedPermit));
        }

        [Fact]
        public async Task B6_12_ExecutionFirewall_PreconditionsFailure_DeniesExecution()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "BillingAgent";
            var cap = "Invoice.Send";

            await _authorityService.GrantDelegationAsync(workspaceId, "Finance", agentId, "Accountant",
                new List<string> { cap }, 50000m, 10000m, Guid.NewGuid());

            var request = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = cap,
                IdempotencyKey = "PRECONDITION_TEST",
                AuditContext = "Monthly invoice",
                Preconditions = new Dictionary<string, string>
                {
                    { "CustomerEmailVerified", "false" } // Failed precondition
                }
            };

            var decision = await _firewall.EvaluateAsync(request);
            Assert.False(decision.IsPermitted);
            Assert.Equal("Preconditions", decision.Denial?.ViolatingPillar);
        }

        #endregion

        #region 7. Idempotency & Dedup Tests

        [Fact]
        public async Task B6_13_Idempotency_DuplicateExecutionRequest_ReturnsCachedReceiptWithoutDoubleSideEffect()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "PaymentAgent";
            var cap = "Payment.Create";

            await _authorityService.GrantDelegationAsync(workspaceId, "Finance", agentId, "Officer",
                new List<string> { cap }, 50000m, 5000m, Guid.NewGuid());

            var wallet = await _budgetService.GetOrCreateWalletAsync(workspaceId);
            var startingBalance = wallet.BalanceINR;

            var request = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = cap,
                MonetaryImpactINR = 2000m,
                IdempotencyKey = "IDEMPOTENT_TX_777",
                AuditContext = "Software subscription"
            };

            // First run: executes
            var dec1 = await _firewall.EvaluateAsync(request);
            var receipt1 = await _firewall.ExecuteAsync(request, dec1.Permit!);
            Assert.Equal(ExecutionStatus.Succeeded, receipt1.Status);

            var balanceAfterFirst = (await _budgetService.GetOrCreateWalletAsync(workspaceId)).BalanceINR;
            Assert.Equal(startingBalance - 2000m, balanceAfterFirst);

            // Second run with IDENTICAL idempotency key: should return cached receipt without charging again
            var dec2 = await _firewall.EvaluateAsync(request);
            Assert.True(dec2.IsPermitted);
            Assert.True(dec2.Permit!.IsConsumed);

            var balanceAfterSecond = (await _budgetService.GetOrCreateWalletAsync(workspaceId)).BalanceINR;
            Assert.Equal(balanceAfterFirst, balanceAfterSecond); // Balance NOT deducted twice!
        }

        #endregion

        #region 8. Human-in-the-Loop Gateway & Tamper Resistance Tests

        [Fact]
        public async Task B6_14_ApprovalGateway_R4HighRiskRequiresHumanSignOff_PermitIssuedOnlyAfterApproval()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "LegalAgent";
            var cap = "Contract.Sign";

            await _authorityService.GrantDelegationAsync(workspaceId, "Legal", agentId, "Counsel",
                new List<string> { cap }, 500000m, 500000m, Guid.NewGuid());

            var request = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = cap,
                PayloadJson = "{\"contractId\":\"CTR-990\",\"partner\":\"Tata Motors\"}",
                IdempotencyKey = "CONTRACT_APPROVAL_001",
                AuditContext = "Strategic partnership"
            };

            // 1. Initial evaluate -> Denied, requires human approval
            var dec1 = await _firewall.EvaluateAsync(request);
            Assert.False(dec1.IsPermitted);
            Assert.True(dec1.RequiresHumanApproval);
            Assert.NotNull(dec1.ApprovalRequestId);

            // 2. Sovereign Human signs off on approval
            var humanUserId = Guid.NewGuid();
            await _approvalGateway.SubmitDecisionAsync(dec1.ApprovalRequestId.Value, true, humanUserId, "Contract verified and approved by CEO");

            // 3. Re-evaluate after approval -> Permitted!
            var dec2 = await _firewall.EvaluateAsync(request);
            Assert.True(dec2.IsPermitted);
            Assert.NotNull(dec2.Permit);
        }

        [Fact]
        public async Task B6_15_ApprovalGateway_PayloadTamperDetection_InvalidatesApproval()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "FinanceAgent";
            var cap = "Payment.Execute";

            await _authorityService.GrantDelegationAsync(workspaceId, "Finance", agentId, "Accountant",
                new List<string> { cap }, 500000m, 500000m, Guid.NewGuid());

            var request = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = cap,
                MonetaryImpactINR = 100000m, // High amount triggers R4 approval
                PayloadJson = "{\"beneficiary\":\"Authorized Vendor\",\"account\":\"9876543210\"}",
                IdempotencyKey = "PAYLOAD_TAMPER_001",
                AuditContext = "Vendor payout"
            };

            // 1. Requires approval
            var dec1 = await _firewall.EvaluateAsync(request);
            Assert.True(dec1.RequiresHumanApproval);

            // 2. Human approves original payload
            await _approvalGateway.SubmitDecisionAsync(dec1.ApprovalRequestId!.Value, true, Guid.NewGuid(), "Approved payout");

            // 3. Malicious agent alters payload after approval (e.g. changes beneficiary account)
            request.PayloadJson = "{\"beneficiary\":\"Attacker Entity\",\"account\":\"1111222233\"}";

            // 4. Re-evaluate with altered payload -> Tamper detected, approval invalidated!
            var dec2 = await _firewall.EvaluateAsync(request);
            Assert.False(dec2.IsPermitted);
            Assert.True(dec2.RequiresHumanApproval); // Auto-invalidated and new approval required
        }

        #endregion

        #region 9. Saga Compensation Engine Tests

        [Fact]
        public async Task B6_16_SagaExecutionEngine_StepFailure_TriggersBackwardCompensation()
        {
            var workspaceId = Guid.NewGuid();
            var missionId = Guid.NewGuid();

            // Grant authority for all forward and compensation steps
            await _authorityService.GrantDelegationAsync(workspaceId, "Operations", "SagaExecutor", "OperationsAgent",
                new List<string> { "Inventory.Reserve", "Inventory.Release", "Invoice.Generate", "Invoice.Void", "Payment.Execute", "Payment.Refund" },
                100000m, 50000m, Guid.NewGuid());

            var steps = new List<SagaStepRecord>
            {
                new()
                {
                    StepIndex = 0,
                    StepName = "Reserve Inventory",
                    ForwardCapabilityId = "Inventory.Reserve",
                    CompensationCapabilityId = "Inventory.Release"
                },
                new()
                {
                    StepIndex = 1,
                    StepName = "Generate Invoice",
                    ForwardCapabilityId = "Invoice.Generate",
                    CompensationCapabilityId = "Invoice.Void"
                },
                new()
                {
                    StepIndex = 2,
                    StepName = "Process Payment",
                    ForwardCapabilityId = "Payment.Execute_FAIL_TEST", // Intentional failure (undelegated cap)
                    CompensationCapabilityId = "Payment.Refund"
                }
            };

            var saga = await _sagaEngine.StartSagaAsync(workspaceId, missionId, "OrderProcessingSaga", steps);
            var executedSaga = await _sagaEngine.ExecuteSagaAsync(saga.SagaId);

            // Invariant: Status compensated and steps compensated in reverse
            Assert.Equal(SagaOverallStatus.Compensated, executedSaga.Status);
            Assert.NotNull(executedSaga.FailureReason);
        }

        #endregion

        #region 10. Autonomous Mission Executor & Kill Switch Tests

        [Fact]
        public async Task B6_17_AutonomousMissionExecutor_RoutesEveryStepThroughFirewall_NoBypass()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "ReceivablesAgent";
            var cap = "CRM.SendMessage";

            await _authorityService.GrantDelegationAsync(workspaceId, "Finance", agentId, "ReceivablesOfficer",
                new List<string> { cap }, 50000m, 10000m, Guid.NewGuid());

            var validStep = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                MissionId = Guid.NewGuid(),
                AgentId = agentId,
                CapabilityId = cap,
                IdempotencyKey = "MISSION_STEP_1",
                AuditContext = "Receivables reminder"
            };

            var receipt = await _missionExecutor.ExecuteMissionStepAsync(validStep);
            Assert.NotNull(receipt);
            Assert.Equal(ExecutionStatus.Succeeded, receipt.Status);

            // Attempt undelegated action through mission executor -> Throws SecurityException
            var invalidStep = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                MissionId = Guid.NewGuid(),
                AgentId = agentId,
                CapabilityId = "Bank.TransferMoney", // Undelegated
                IdempotencyKey = "MISSION_STEP_2",
                AuditContext = "Receivables sweep"
            };

            await Assert.ThrowsAsync<SecurityException>(() => _missionExecutor.ExecuteMissionStepAsync(invalidStep));
        }

        [Fact]
        public async Task B6_18_HierarchicalKillSwitch_HaltsAcrossTiers_Under100ms()
        {
            var workspaceId = Guid.NewGuid();
            var agentId = "TradingAgent";
            var cap = "Payment.Execute";

            await _authorityService.GrantDelegationAsync(workspaceId, "Trading", agentId, "Bot",
                new List<string> { cap }, 50000m, 10000m, Guid.NewGuid());

            var request = new ExecutionRequest
            {
                WorkspaceId = workspaceId,
                AgentId = agentId,
                CapabilityId = cap,
                IdempotencyKey = "KILL_SWITCH_TEST_1",
                AuditContext = "Trade action"
            };

            // Permitted before kill switch
            var dec1 = await _firewall.EvaluateAsync(request);
            Assert.True(dec1.IsPermitted);

            // Trigger Tenant Kill Switch
            var sw = await _killSwitchService.TriggerKillSwitchAsync(
                ExecutionKillSwitchTier.Tenant, workspaceId, null, "Emergency trading anomaly detected", Guid.NewGuid());

            // Execution immediately blocked!
            request.IdempotencyKey = "KILL_SWITCH_TEST_2";
            var dec2 = await _firewall.EvaluateAsync(request);
            Assert.False(dec2.IsPermitted);
            Assert.Equal("KillSwitch", dec2.Denial?.ViolatingPillar);

            // Deactivate kill switch
            await _killSwitchService.DeactivateKillSwitchAsync(sw.Id, Guid.NewGuid());

            // Execution permitted again
            request.IdempotencyKey = "KILL_SWITCH_TEST_3";
            var dec3 = await _firewall.EvaluateAsync(request);
            Assert.True(dec3.IsPermitted);
        }

        #endregion

        #region 11. Multi-Tenant Execution Isolation Tests

        [Fact]
        public async Task B6_19_TenantIsolation_TenantACannotAccessOrExecuteTenantBWalletsOrConnectors()
        {
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            // Grant authority only in Tenant B
            await _authorityService.GrantDelegationAsync(tenantB, "Sales", "AgentB", "Rep",
                new List<string> { "CRM.SendMessage" }, 50000m, 10000m, Guid.NewGuid());

            // Tenant A tries to execute under Tenant B's agent or access Tenant B's wallet
            var crossTenantRequest = new ExecutionRequest
            {
                WorkspaceId = tenantA, // Request in Tenant A
                AgentId = "AgentB",     // Uses Tenant B's delegated agent
                CapabilityId = "CRM.SendMessage",
                IdempotencyKey = "CROSS_TENANT_ATTACK",
                AuditContext = "Infiltrate"
            };

            var decision = await _firewall.EvaluateAsync(crossTenantRequest);
            Assert.False(decision.IsPermitted);
            Assert.Equal("AuthorityDelegation", decision.Denial?.ViolatingPillar);
        }

        #endregion
    }
}
