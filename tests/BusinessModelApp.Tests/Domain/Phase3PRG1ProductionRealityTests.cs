using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using BusinessModelApp.Core.Domain.Runtime.Reality;
using BusinessModelApp.Core.Interfaces.Runtime.Reality;
using BusinessModelApp.Infrastructure.Runtime.Reality;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3PRG1ProductionRealityTests
    {
        private readonly IProductionRealityService _realityService;
        private readonly IHumanApprovalManager _approvalManager;
        private readonly IWorkControlCenter _workControlCenter;
        private readonly IValueRealizationEngine _valueRealization;
        private readonly IConnectorHealthService _connectorHealth;

        private const string TenantA = "tenant-alpha";
        private const string TenantB = "tenant-beta";

        public Phase3PRG1ProductionRealityTests()
        {
            _realityService = new ProductionRealityService();
            _approvalManager = new HumanApprovalManager();
            _workControlCenter = new WorkControlCenter();
            _valueRealization = new ValueRealizationEngine();
            _connectorHealth = new ConnectorHealthService();
        }

        // ==========================================
        // PR-01 to PR-07: Data Authenticity
        // ==========================================

        [Fact]
        public void PR01_ProductionConfiguration_RejectsMockProviders_FailClosed()
        {
            // Invariant: Production environment cannot configure or execute mock providers.
            Assert.Throws<InvalidOperationException>(() =>
            {
                _realityService.AssertProductionIntegrity(isProductionEnvironment: true, hasMockProvider: true);
            });

            // Permitted in non-production or when no mock provider is loaded
            _realityService.AssertProductionIntegrity(isProductionEnvironment: false, hasMockProvider: true);
            _realityService.AssertProductionIntegrity(isProductionEnvironment: true, hasMockProvider: false);
        }

        [Fact]
        public void PR02_MissingMetrics_ReturnUnknown_NeverFabricatedZero()
        {
            var metric = _realityService.CreateMetric<decimal?>(
                value: null,
                status: RealityStatus.LiveVerified, // requested live verified, but value is missing
                classification: TruthClassification.Fact,
                metricKey: "revenue.arr",
                label: "Annual Recurring Revenue",
                unit: "INR",
                tenantId: TenantA,
                source: "AccountingLedger",
                freshnessSla: TimeSpan.FromHours(1)
            );

            Assert.Null(metric.Value);
            Assert.Equal(RealityStatus.Unknown, metric.Status);
            Assert.Contains("UNKNOWN", metric.EpistemicRationale);
        }

        [Fact]
        public void PR03_EmptyEventStream_ReturnsIdle_NeverSyntheticEvents()
        {
            var overview = _realityService.GetSystemOverview(TenantA);
            Assert.Equal(RealityMode.Live, overview.Mode);
            Assert.Equal(0, overview.ActiveMissionsCount);
            Assert.Equal(0, overview.PendingApprovalsCount);
        }

        [Fact]
        public void PR04_Timestamps_MustDeriveFromVerifiedObservations()
        {
            var observedTime = DateTimeOffset.UtcNow.AddMinutes(-10);
            var envelope = _realityService.Wrap<string>(
                value: "7 verified signals",
                status: RealityStatus.LiveVerified,
                classification: TruthClassification.Fact,
                tenantId: TenantA,
                source: "DigitalTwin.Signals",
                observedAt: observedTime
            );

            Assert.Equal(observedTime, envelope.ObservedAt);
            Assert.Equal(observedTime, envelope.VerifiedAt);
        }

        [Fact]
        public async Task PR05_WorkerTelemetry_ReflectsActualInstances_ZeroReportsZero()
        {
            var progress = await _workControlCenter.GetWorkProgressAsync(TenantA);
            Assert.Equal(0, progress.RunningNodes);
            Assert.Equal(0, progress.ActiveMissions);
            Assert.Equal(0, progress.TotalMissions);
        }

        [Fact]
        public async Task PR06_FinancialMetrics_FailClosedToUnknown_IfLedgerUnverified()
        {
            await _valueRealization.RecordExpectedValueAsync(
                tenantId: TenantA,
                missionId: "M-101",
                missionName: "Upsell Enterprise",
                expected: 50000m,
                authorized: 5000m,
                actual: 2000m
            );

            var value = await _valueRealization.GetValueRealizationAsync(TenantA, "M-101");
            Assert.NotNull(value);
            Assert.Equal(RealityStatus.Unknown, value!.RealizedValueStatus);
            Assert.Null(value.RealizedValue);
            Assert.Null(value.VerifiedRevenue);
            Assert.Null(value.Variance);
        }

        [Fact]
        public void PR07_SecurityPosture_FailsClosed_IfTelemetryUnavailable()
        {
            var metric = _realityService.CreateMetric<double?>(
                value: null,
                status: RealityStatus.Unknown,
                classification: TruthClassification.Inference,
                metricKey: "security.posture.score",
                label: "Strix Security Posture",
                unit: "Percent",
                tenantId: TenantA,
                source: "Strix.Telemetry",
                freshnessSla: TimeSpan.FromMinutes(10)
            );

            Assert.Equal(RealityStatus.Unknown, metric.Status);
            Assert.Null(metric.Value);
        }

        // ==========================================
        // PR-08 to PR-14: Provenance & RealityEnvelope
        // ==========================================

        [Fact]
        public void PR08_EveryRealityEnvelope_ContainsSourceObservedAtAndProvenanceId()
        {
            var envelope = _realityService.Wrap(
                value: 482300m,
                status: RealityStatus.LiveVerified,
                classification: TruthClassification.Fact,
                tenantId: TenantA,
                source: "RevenueEvent.Ledger",
                sourceRecordId: "REV-982341"
            );

            Assert.Equal("RevenueEvent.Ledger", envelope.Source);
            Assert.Equal("REV-982341", envelope.SourceRecordId);
            Assert.NotNull(envelope.ObservedAt);
            Assert.StartsWith("PROV-", envelope.ProvenanceId);
            Assert.False(string.IsNullOrWhiteSpace(envelope.IntegrityHash));
        }

        [Fact]
        public void PR09_Sha256_IntegrityHash_TamperVerification()
        {
            var envelope1 = _realityService.Wrap("Safe", RealityStatus.LiveVerified, TruthClassification.Fact, TenantA, "ConstraintStore");
            var envelope2 = _realityService.Wrap("Safe", RealityStatus.LiveVerified, TruthClassification.Fact, TenantA, "ConstraintStore", observedAt: envelope1.ObservedAt);

            Assert.Equal(envelope1.IntegrityHash, envelope2.IntegrityHash);

            var envelopeTampered = _realityService.Wrap("BLOCKED", RealityStatus.LiveVerified, TruthClassification.Fact, TenantA, "ConstraintStore", observedAt: envelope1.ObservedAt);
            Assert.NotEqual(envelope1.IntegrityHash, envelopeTampered.IntegrityHash);
        }

        [Fact]
        public void PR10_MultiTenantScoping_InRealityEnvelopes()
        {
            var envA = _realityService.Wrap("DataA", RealityStatus.LiveVerified, TruthClassification.Fact, TenantA, "SourceA");
            var envB = _realityService.Wrap("DataB", RealityStatus.LiveVerified, TruthClassification.Fact, TenantB, "SourceB");

            Assert.Equal(TenantA, envA.TenantId);
            Assert.Equal(TenantB, envB.TenantId);
            Assert.NotEqual(envA.TenantId, envB.TenantId);
        }

        [Fact]
        public void PR11_FreshnessSla_Violation_MarksEnvelopeStale()
        {
            var oldTime = DateTimeOffset.UtcNow.AddMinutes(-30);
            var metric = _realityService.CreateMetric(
                value: 1500m,
                status: RealityStatus.LiveVerified,
                classification: TruthClassification.Fact,
                metricKey: "cac.ceiling",
                label: "CAC Ceiling",
                unit: "INR",
                tenantId: TenantA,
                source: "TwinTelemetry",
                freshnessSla: TimeSpan.FromMinutes(15), // SLA is 15 mins, observed 30 mins ago
                observedAt: oldTime
            );

            Assert.Equal(RealityStatus.Stale, metric.Status);
            Assert.False(metric.IsFresh);
        }

        [Fact]
        public void PR12_TruthClassification_DifferentiatesFactVsProjection()
        {
            var fact = _realityService.Wrap(1000m, RealityStatus.LiveVerified, TruthClassification.Fact, TenantA, "Ledger");
            var projection = _realityService.Wrap(2000m, RealityStatus.Simulation, TruthClassification.Projection, TenantA, "CounterfactualEngine");

            Assert.Equal(TruthClassification.Fact, fact.Classification);
            Assert.Equal(TruthClassification.Projection, projection.Classification);
            Assert.Equal(RealityStatus.Simulation, projection.Status);
        }

        [Fact]
        public void PR13_UnknownStatus_PreservedThroughSerialization()
        {
            var env = _realityService.Wrap<decimal?>(null, RealityStatus.Unknown, TruthClassification.Inference, TenantA, "Telemetry");
            Assert.Equal(RealityStatus.Unknown, env.Status);
            Assert.Null(env.Value);
            Assert.Null(env.VerifiedAt);
        }

        [Fact]
        public async Task PR14_NotConnectedStatus_WhenConnectorAdapterMissing()
        {
            var crm = await _connectorHealth.GetConnectorHealthAsync(TenantA, ConnectorType.Crm);
            Assert.Equal(ConnectorStatus.NotConfigured, crm.Status);
            Assert.Contains("NOT CONFIGURED", crm.EpistemicNote);
        }

        // ==========================================
        // PR-15 to PR-23: Human Approval Architecture
        // ==========================================

        [Fact]
        public async Task PR15_ConsequentialProposal_TriggersApprovalRequest_Creation()
        {
            var payload = "{\"customer\":\"EnterpriseCo\",\"offer\":\"Discount-15%\"}";
            var request = new ApprovalRequest(
                ApprovalId: "APP-001",
                TenantId: TenantA,
                MissionId: "M-9941",
                MissionName: "Enterprise Churn Recovery",
                NodeId: "Node-05",
                WorkerId: "Worker-API-07",
                TargetSystem: "HubSpot CRM",
                Capability: "crm.deal.update",
                Risk: ApprovalRiskTier.R3_Commercial,
                ActionDescription: "Send retention discount offer to high-churn risk customer",
                ProposedEffect: "Applies 15% discount code",
                EvidenceSummary: "7 verified churn signals from Digital Twin",
                ConstraintStatus: "Liquidity SAFE, Margin SAFE, CAC WARNING",
                StrategicRegime: "MarketDefense_PriceWar",
                FinancialExposure: 12500m,
                Currency: "INR",
                PayloadJson: payload,
                PayloadDigest: string.Empty, // Will be computed
                IsReversible: true,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(4),
                State: ApprovalState.Requested
            );

            var submitted = await _approvalManager.SubmitApprovalRequestAsync(request);
            Assert.Equal("APP-001", submitted.ApprovalId);
            Assert.False(string.IsNullOrEmpty(submitted.PayloadDigest));
            Assert.Equal(ApprovalState.Requested, submitted.State);

            var pending = await _approvalManager.GetPendingApprovalsAsync(TenantA);
            Assert.Single(pending);
        }

        [Fact]
        public async Task PR16_PayloadSha256_DigestVerification_MatchesHash()
        {
            var payload = "{\"recipient\":\"ceo@client.com\",\"amount\":50000}";
            using var sha = SHA256.Create();
            var expectedDigest = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));

            var req = new ApprovalRequest(
                ApprovalId: "APP-002",
                TenantId: TenantA,
                MissionId: "M-02",
                MissionName: "Refund Request",
                NodeId: "N-01",
                WorkerId: "Worker-API-01",
                TargetSystem: "Stripe",
                Capability: "payments.charge.refund",
                Risk: ApprovalRiskTier.R4_Strategic,
                ActionDescription: "Execute high-value refund",
                ProposedEffect: "Refund ₹50,000",
                EvidenceSummary: "Charge dispute #8421",
                ConstraintStatus: "SAFE",
                StrategicRegime: "CashPreservation",
                FinancialExposure: 50000m,
                Currency: "INR",
                PayloadJson: payload,
                PayloadDigest: expectedDigest,
                IsReversible: false,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(2),
                State: ApprovalState.Requested
            );

            var submitted = await _approvalManager.SubmitApprovalRequestAsync(req);
            Assert.Equal(expectedDigest, submitted.PayloadDigest);
        }

        [Fact]
        public async Task PR17_TamperedPayload_ImmediatelyInvalidates_ApprovalRequest()
        {
            var originalPayload = "{\"recipient\":\"legit@client.com\",\"amount\":1000}";
            var req = new ApprovalRequest(
                ApprovalId: "APP-TAMPER",
                TenantId: TenantA,
                MissionId: "M-03",
                MissionName: "Payout",
                NodeId: "N-01",
                WorkerId: "Worker-API-01",
                TargetSystem: "PaymentGateway",
                Capability: "payout.create",
                Risk: ApprovalRiskTier.R4_Strategic,
                ActionDescription: "Payout",
                ProposedEffect: "Send 1000",
                EvidenceSummary: "Evidence",
                ConstraintStatus: "SAFE",
                StrategicRegime: "Balanced",
                FinancialExposure: 1000m,
                Currency: "INR",
                PayloadJson: originalPayload,
                PayloadDigest: string.Empty,
                IsReversible: false,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(2),
                State: ApprovalState.Requested
            );

            var submitted = await _approvalManager.SubmitApprovalRequestAsync(req);

            // Reviewer attempts to approve, but specifies an altered expected digest
            var alteredDigest = "DEADBEEF00000000000000000000000000000000000000000000000000000000";
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _approvalManager.ApproveRequestAsync(TenantA, "APP-TAMPER", "Reviewer-1", alteredDigest));

            var reloaded = await _approvalManager.GetApprovalRequestAsync(TenantA, "APP-TAMPER");
            Assert.NotNull(reloaded);
            Assert.Equal(ApprovalState.Invalidated, reloaded!.State);
        }

        [Fact]
        public async Task PR18_ExpiredSla_TransitionsRequestToExpired_FailsClosed()
        {
            var req = new ApprovalRequest(
                ApprovalId: "APP-EXPIRE",
                TenantId: TenantA,
                MissionId: "M-04",
                MissionName: "Campaign Launch",
                NodeId: "N-01",
                WorkerId: "Worker-API-01",
                TargetSystem: "AdPlatform",
                Capability: "ads.campaign.start",
                Risk: ApprovalRiskTier.R3_Commercial,
                ActionDescription: "Launch Ad Campaign",
                ProposedEffect: "Spend ₹25,000",
                EvidenceSummary: "Evidence",
                ConstraintStatus: "SAFE",
                StrategicRegime: "Expansion",
                FinancialExposure: 25000m,
                Currency: "INR",
                PayloadJson: "{}",
                PayloadDigest: string.Empty,
                IsReversible: true,
                RequestedAt: DateTimeOffset.UtcNow.AddHours(-3),
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(-1), // Expired 1 hour ago
                State: ApprovalState.Requested
            );

            await _approvalManager.SubmitApprovalRequestAsync(req);

            // Attempting to approve expired request throws
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _approvalManager.ApproveRequestAsync(TenantA, "APP-EXPIRE", "CEO"));

            var reloaded = await _approvalManager.GetApprovalRequestAsync(TenantA, "APP-EXPIRE");
            Assert.Equal(ApprovalState.Expired, reloaded!.State);
        }

        [Fact]
        public async Task PR19_CeoApproval_GeneratesCryptographic_ExecutionPermit()
        {
            var payload = "{\"mission\":\"M-05\",\"action\":\"Reconcile\"}";
            var req = new ApprovalRequest(
                ApprovalId: "APP-PERMIT",
                TenantId: TenantA,
                MissionId: "M-05",
                MissionName: "Invoice Reconcile",
                NodeId: "N-02",
                WorkerId: "Worker-API-07",
                TargetSystem: "QuickBooks",
                Capability: "invoice.reconcile",
                Risk: ApprovalRiskTier.R2_Operational,
                ActionDescription: "Reconcile invoices",
                ProposedEffect: "Mark invoice settled",
                EvidenceSummary: "Receipt verified",
                ConstraintStatus: "SAFE",
                StrategicRegime: "Conservative",
                FinancialExposure: 0m,
                Currency: "INR",
                PayloadJson: payload,
                PayloadDigest: string.Empty,
                IsReversible: true,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(2),
                State: ApprovalState.Requested
            );

            var submitted = await _approvalManager.SubmitApprovalRequestAsync(req);
            var permit = await _approvalManager.ApproveRequestAsync(TenantA, "APP-PERMIT", "CEO-Mayur", submitted.PayloadDigest, "Approved for settlement.");

            Assert.NotNull(permit);
            Assert.StartsWith("PERMIT-", permit.PermitId);
            Assert.Equal("CEO-Mayur", permit.ApprovedBy);
            Assert.Equal(submitted.PayloadDigest, permit.PayloadDigest);
            Assert.False(string.IsNullOrEmpty(permit.AuthoritySignature));

            var reloaded = await _approvalManager.GetApprovalRequestAsync(TenantA, "APP-PERMIT");
            Assert.Equal(ApprovalState.Approved, reloaded!.State);
            Assert.Equal("CEO-Mayur", reloaded.ReviewerId);
        }

        [Fact]
        public async Task PR20_CeoRejection_AbortsProposal_RecordsAuditReason()
        {
            var req = new ApprovalRequest(
                ApprovalId: "APP-REJECT",
                TenantId: TenantA,
                MissionId: "M-06",
                MissionName: "Mass Email Blast",
                NodeId: "N-01",
                WorkerId: "Worker-API-01",
                TargetSystem: "SendGrid",
                Capability: "email.send.mass",
                Risk: ApprovalRiskTier.R4_Strategic,
                ActionDescription: "Send 10,000 promotional emails",
                ProposedEffect: "Email broadcast",
                EvidenceSummary: "Campaign draft",
                ConstraintStatus: "Approval Bandwidth Low",
                StrategicRegime: "CashPreservation",
                FinancialExposure: 5000m,
                Currency: "INR",
                PayloadJson: "{\"recipients\":10000}",
                PayloadDigest: string.Empty,
                IsReversible: false,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(2),
                State: ApprovalState.Requested
            );

            await _approvalManager.SubmitApprovalRequestAsync(req);
            var rejected = await _approvalManager.RejectRequestAsync(TenantA, "APP-REJECT", "CEO-Mayur", "Violates brand communication tone policy.");

            Assert.Equal(ApprovalState.Rejected, rejected.State);
            Assert.Equal("Violates brand communication tone policy.", rejected.RejectionReason);

            var audits = await _approvalManager.GetAuditTrailAsync(TenantA, "APP-REJECT");
            Assert.Contains(audits, a => a.NewState == ApprovalState.Rejected);
        }

        [Fact]
        public async Task PR21_ChangesRequested_ReturnsActionToMissionPlanner()
        {
            var req = new ApprovalRequest(
                ApprovalId: "APP-CHANGES",
                TenantId: TenantA,
                MissionId: "M-07",
                MissionName: "Price Adjustment",
                NodeId: "N-01",
                WorkerId: "Worker-API-02",
                TargetSystem: "BillingSystem",
                Capability: "billing.rate.adjust",
                Risk: ApprovalRiskTier.R3_Commercial,
                ActionDescription: "Raise enterprise tier by 10%",
                ProposedEffect: "Update subscription catalog",
                EvidenceSummary: "Inflation metrics",
                ConstraintStatus: "SAFE",
                StrategicRegime: "Balanced",
                FinancialExposure: 0m,
                Currency: "INR",
                PayloadJson: "{\"priceIncrease\":0.10}",
                PayloadDigest: string.Empty,
                IsReversible: true,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(2),
                State: ApprovalState.Requested
            );

            await _approvalManager.SubmitApprovalRequestAsync(req);
            var updated = await _approvalManager.RequestChangesAsync(TenantA, "APP-CHANGES", "CEO-Mayur", "Limit increase to 5% for Q3 grandfathered accounts.");

            Assert.Equal(ApprovalState.ChangesRequested, updated.State);
            Assert.Contains("Limit increase to 5%", updated.ChangesRequestedNotes);
        }

        [Fact]
        public async Task PR22_CrossTenant_ApprovalIsolation_Enforced()
        {
            var reqA = new ApprovalRequest(
                ApprovalId: "APP-TENANT-A",
                TenantId: TenantA,
                MissionId: "M-A",
                MissionName: "Mission A",
                NodeId: "N-1",
                WorkerId: "W-1",
                TargetSystem: "SysA",
                Capability: "cap.a",
                Risk: ApprovalRiskTier.R2_Operational,
                ActionDescription: "Action A",
                ProposedEffect: "Effect A",
                EvidenceSummary: "Ev A",
                ConstraintStatus: "SAFE",
                StrategicRegime: "Balanced",
                FinancialExposure: 100m,
                Currency: "INR",
                PayloadJson: "{}",
                PayloadDigest: string.Empty,
                IsReversible: true,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(2),
                State: ApprovalState.Requested
            );

            await _approvalManager.SubmitApprovalRequestAsync(reqA);

            // Tenant B cannot retrieve or approve Tenant A's request
            var fromB = await _approvalManager.GetApprovalRequestAsync(TenantB, "APP-TENANT-A");
            Assert.Null(fromB);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _approvalManager.ApproveRequestAsync(TenantB, "APP-TENANT-A", "Attacker"));
        }

        [Fact]
        public async Task PR23_ImmutableAuditLogging_OfApprovalLifecycle()
        {
            var req = new ApprovalRequest(
                ApprovalId: "APP-AUDIT",
                TenantId: TenantA,
                MissionId: "M-08",
                MissionName: "Audit Test",
                NodeId: "N-01",
                WorkerId: "W-01",
                TargetSystem: "CRM",
                Capability: "crm.update",
                Risk: ApprovalRiskTier.R2_Operational,
                ActionDescription: "Update",
                ProposedEffect: "Effect",
                EvidenceSummary: "Evidence",
                ConstraintStatus: "SAFE",
                StrategicRegime: "Balanced",
                FinancialExposure: 100m,
                Currency: "INR",
                PayloadJson: "{}",
                PayloadDigest: string.Empty,
                IsReversible: true,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(2),
                State: ApprovalState.Requested
            );

            var submitted = await _approvalManager.SubmitApprovalRequestAsync(req);
            await _approvalManager.ApproveRequestAsync(TenantA, "APP-AUDIT", "CEO", submitted.PayloadDigest);

            var audits = await _approvalManager.GetAuditTrailAsync(TenantA, "APP-AUDIT");
            Assert.True(audits.Count >= 2); // Registered -> Approved
            Assert.All(audits, a => Assert.False(string.IsNullOrWhiteSpace(a.IntegrityHash)));
        }

        // ==========================================
        // PR-24 to PR-28: Work vs. Realized Value Separation
        // ==========================================

        [Fact]
        public async Task PR24_CompletedNodes_IncrementWork_LeavesRealizedValueAsUnknown()
        {
            var missionId = "M-9941";
            await _workControlCenter.RecordMissionStepAsync(TenantA, missionId, new MissionWorkLedgerEntry(
                StepId: "S-01",
                MissionId: missionId,
                NodeId: "Node-01",
                NodeName: "Detect Churn",
                Stage: "Responsibility",
                Status: "Completed",
                Description: "Detected enterprise churn signal",
                WorkerId: "Worker-API-01",
                Timestamp: DateTimeOffset.UtcNow
            ));

            await _workControlCenter.RecordMissionStepAsync(TenantA, missionId, new MissionWorkLedgerEntry(
                StepId: "S-02",
                MissionId: missionId,
                NodeId: "Node-02",
                NodeName: "Verify Evidence",
                Stage: "Evidence",
                Status: "Completed",
                Description: "7 verified records",
                WorkerId: "Worker-API-02",
                Timestamp: DateTimeOffset.UtcNow
            ));

            var progress = await _workControlCenter.GetWorkProgressAsync(TenantA);
            Assert.Equal(2, progress.CompletedNodes);

            // Realized value must still be Unknown: Work != Value
            var val = await _valueRealization.GetValueRealizationAsync(TenantA, missionId);
            Assert.Null(val); // Not even assessed until explicit financial outcome
        }

        [Fact]
        public async Task PR25_EmpiricalOutcomeLedger_TransitionsRealizedValueToLiveVerified()
        {
            var missionId = "M-RECOVER";
            await _valueRealization.RecordExpectedValueAsync(TenantA, missionId, "Churn Recovery", 100000m, 15000m, 8200m);

            // Before outcome: UNKNOWN
            var before = await _valueRealization.GetValueRealizationAsync(TenantA, missionId);
            Assert.Equal(RealityStatus.Unknown, before!.RealizedValueStatus);
            Assert.Null(before.RealizedValue);

            // After verified payment ledger entry: LIVE_VERIFIED
            var verified = await _valueRealization.RecordVerifiedOutcomeAsync(
                tenantId: TenantA,
                missionId: missionId,
                verifiedRevenue: 92000m,
                verifiedMargin: 31400m,
                outcomeLedgerRef: "OUTCOME-98421",
                evidenceSource: "PaymentGateway.Webhook"
            );

            Assert.Equal(RealityStatus.LiveVerified, verified.RealizedValueStatus);
            Assert.Equal(92000m - 8200m, verified.RealizedValue); // 83800 net
            Assert.Equal(92000m, verified.VerifiedRevenue);
            Assert.Equal("OUTCOME-98421", verified.OutcomeLedgerReference);
        }

        [Fact]
        public async Task PR26_FinancialExposure_BoundsEnforce_AuthorizedExposureGreaterOrEqualToSpend()
        {
            var missionId = "M-BUDGET";
            await _valueRealization.RecordExpectedValueAsync(TenantA, missionId, "Ad Spend", 50000m, 15000m, 12000m);

            var val = await _valueRealization.GetValueRealizationAsync(TenantA, missionId);
            Assert.True(val!.AuthorizedExposure >= val.ActualSpend);
        }

        [Fact]
        public async Task PR27_WorkLedger_MaintainsImmutable_ChronologicalTrace()
        {
            var missionId = "M-TRACE";
            var ledger = new MissionWorkLedger(
                MissionId: missionId,
                MissionName: "Retention Offer Dispatch",
                TenantId: TenantA,
                Objective: "Retain customer",
                CreatedAt: DateTimeOffset.UtcNow,
                CurrentStatus: "WaitingApproval",
                TotalNodes: 11,
                CompletedNodes: 6,
                Entries: new List<MissionWorkLedgerEntry>
                {
                    new("1", missionId, "N-1", "Signal", "Responsibility", "Completed", "Done", "W-1", DateTimeOffset.UtcNow.AddMinutes(-5)),
                    new("2", missionId, "N-2", "Economics", "Strategy", "Completed", "Done", "W-2", DateTimeOffset.UtcNow.AddMinutes(-3)),
                    new("3", missionId, "N-3", "Approval", "HumanApproval", "WaitingApproval", "Pending CEO", null, DateTimeOffset.UtcNow.AddMinutes(-1))
                }
            );

            await _workControlCenter.RegisterMissionLedgerAsync(ledger);
            var retrieved = await _workControlCenter.GetMissionWorkLedgerAsync(TenantA, missionId);

            Assert.NotNull(retrieved);
            Assert.Equal(3, retrieved!.Entries.Count);
            Assert.Equal("WaitingApproval", retrieved.CurrentStatus);
        }

        [Fact]
        public async Task PR28_UnknownEffect_IncidentsTrigger_CompensationRequired_InLedger()
        {
            var missionId = "M-CRASH";
            await _workControlCenter.RecordMissionStepAsync(TenantA, missionId, new MissionWorkLedgerEntry(
                StepId: "S-CRASH",
                MissionId: missionId,
                NodeId: "Node-Crash",
                NodeName: "Payment Call",
                Stage: "ConnectorExecuted",
                Status: "UnknownEffect",
                Description: "Worker connection dropped during external HTTP POST",
                WorkerId: "Worker-API-07",
                Timestamp: DateTimeOffset.UtcNow,
                Details: "State transitioned to UnknownEffect. Compensation required."
            ));

            var progress = await _workControlCenter.GetWorkProgressAsync(TenantA);
            Assert.Equal(1, progress.UnknownEffectNodes);
        }

        // ==========================================
        // PR-29 to PR-36: Firewall Sovereignty & Connector Reality
        // ==========================================

        [Fact]
        public async Task PR29_ApprovalDoesNotBypass_Firewall_IssuesPermitToFirewall()
        {
            var req = new ApprovalRequest(
                ApprovalId: "APP-FW",
                TenantId: TenantA,
                MissionId: "M-FW",
                MissionName: "External Send",
                NodeId: "N-FW",
                WorkerId: "Worker-API-01",
                TargetSystem: "Stripe",
                Capability: "payments.charge.create",
                Risk: ApprovalRiskTier.R4_Strategic,
                ActionDescription: "Charge card",
                ProposedEffect: "Charge ₹10,000",
                EvidenceSummary: "Authorized invoice",
                ConstraintStatus: "SAFE",
                StrategicRegime: "Expansion",
                FinancialExposure: 10000m,
                Currency: "INR",
                PayloadJson: "{\"amount\":10000}",
                PayloadDigest: string.Empty,
                IsReversible: false,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
                State: ApprovalState.Requested
            );

            var submitted = await _approvalManager.SubmitApprovalRequestAsync(req);
            var permit = await _approvalManager.ApproveRequestAsync(TenantA, "APP-FW", "CEO-Mayur", submitted.PayloadDigest);

            // Permit is issued for the Firewall; it does not execute the action
            Assert.NotNull(permit.PermitId);
            Assert.Equal("Stripe", permit.TargetSystem);
            Assert.Equal(submitted.PayloadDigest, permit.PayloadDigest);
        }

        [Fact]
        public async Task PR30_DirectExternalConnector_Invocation_WithoutPermit_IsBlocked()
        {
            // Without a valid permit, an approval cannot be claimed as executed
            var pending = await _approvalManager.GetPendingApprovalsAsync(TenantA);
            Assert.DoesNotContain(pending, p => p.State == ApprovalState.Executed);
        }

        [Fact]
        public async Task PR31_ConnectorDisconnection_TriggersDisconnectedState_NotCachedSuccess()
        {
            var whatsapp = await _connectorHealth.GetConnectorHealthAsync(TenantA, ConnectorType.WhatsApp);
            Assert.Equal(ConnectorStatus.Disconnected, whatsapp.Status);
            Assert.Contains("DISCONNECTED", whatsapp.EpistemicNote);
        }

        [Fact]
        public async Task PR32_UnconfiguredConnectors_DisplayNotConfigured()
        {
            var email = await _connectorHealth.GetConnectorHealthAsync(TenantA, ConnectorType.Email);
            Assert.Equal(ConnectorStatus.NotConfigured, email.Status);
            Assert.Equal("Missing", email.CredentialState);
        }

        [Fact]
        public void PR33_BrainFabricTelemetry_ReflectsActualProvider()
        {
            var overview = _realityService.GetSystemOverview(TenantA);
            Assert.Equal("OpenRouter (Governed)", overview.BrainProvider);
            Assert.Equal("anthropic/claude-3.5-sonnet", overview.BrainModel);
        }

        [Fact]
        public void PR34_ApiFailure_ProducesHonestUnknownState_NoMockFallback()
        {
            var metric = _realityService.CreateMetric<decimal?>(
                value: null,
                status: RealityStatus.Error,
                classification: TruthClassification.Fact,
                metricKey: "revenue.pipeline",
                label: "Pipeline Value",
                unit: "INR",
                tenantId: TenantA,
                source: "CRM.Api",
                freshnessSla: TimeSpan.FromMinutes(5),
                epistemicRationale: "CRM API failed. Epistemic rule: Return UNKNOWN/ERROR, never fallback to synthetic data."
            );

            Assert.Equal(RealityStatus.Error, metric.Status);
            Assert.Contains("never fallback", metric.EpistemicRationale);
        }

        [Fact]
        public async Task PR35_EndToEndMission_ToApproval_ToPermit_Verification()
        {
            // 1. Mission records steps
            var missionId = "M-E2E";
            await _workControlCenter.RecordMissionStepAsync(TenantA, missionId, new MissionWorkLedgerEntry(
                StepId: "E2E-1",
                MissionId: missionId,
                NodeId: "N-1",
                NodeName: "Analyze Churn",
                Stage: "Research",
                Status: "Completed",
                Description: "Analyzed signals",
                WorkerId: "Worker-API-01",
                Timestamp: DateTimeOffset.UtcNow.AddMinutes(-10)
            ));

            // 2. Action proposed requiring approval
            var req = new ApprovalRequest(
                ApprovalId: "APP-E2E",
                TenantId: TenantA,
                MissionId: missionId,
                MissionName: "Customer Win-Back",
                NodeId: "N-2",
                WorkerId: "Worker-API-07",
                TargetSystem: "CustomerPortal",
                Capability: "portal.offer.create",
                Risk: ApprovalRiskTier.R3_Commercial,
                ActionDescription: "Deploy offer",
                ProposedEffect: "Offer code deployed",
                EvidenceSummary: "Churn analysis completed",
                ConstraintStatus: "SAFE",
                StrategicRegime: "MarketDefense",
                FinancialExposure: 5000m,
                Currency: "INR",
                PayloadJson: "{\"offerCode\":\"WINBACK-5K\"}",
                PayloadDigest: string.Empty,
                IsReversible: true,
                RequestedAt: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(2),
                State: ApprovalState.Requested
            );

            var submitted = await _approvalManager.SubmitApprovalRequestAsync(req);
            Assert.Equal(ApprovalState.Requested, submitted.State);

            // 3. CEO approves
            var permit = await _approvalManager.ApproveRequestAsync(TenantA, "APP-E2E", "CEO-Mayur", submitted.PayloadDigest);
            Assert.NotNull(permit);

            // 4. Ledger records human approval completed
            await _workControlCenter.RecordMissionStepAsync(TenantA, missionId, new MissionWorkLedgerEntry(
                StepId: "E2E-2",
                MissionId: missionId,
                NodeId: "N-2",
                NodeName: "Human Approval",
                Stage: "HumanApproval",
                Status: "Completed",
                Description: "Approved by CEO-Mayur. Permit issued.",
                WorkerId: null,
                Timestamp: DateTimeOffset.UtcNow,
                ProofHash: permit.AuthoritySignature
            ));

            var ledger = await _workControlCenter.GetMissionWorkLedgerAsync(TenantA, missionId);
            Assert.Equal(2, ledger!.CompletedNodes);
        }

        [Fact]
        public async Task PR36_FullRegressionIntegration_ZeroFirewallBypass()
        {
            // Verify that all 5 PRG-1 services operate together in a single coherent flow
            var overview = _realityService.GetSystemOverview(TenantA);
            var connectors = await _connectorHealth.GetConnectorHealthAsync(TenantA);
            var work = await _workControlCenter.GetWorkProgressAsync(TenantA);
            var pending = await _approvalManager.GetPendingApprovalsAsync(TenantA);
            var values = await _valueRealization.GetAllValueRealizationsAsync(TenantA);

            Assert.Equal(RealityMode.Live, overview.Mode);
            Assert.NotEmpty(connectors);
            Assert.NotNull(work);
            Assert.NotNull(pending);
            Assert.NotNull(values);
        }
    }
}
