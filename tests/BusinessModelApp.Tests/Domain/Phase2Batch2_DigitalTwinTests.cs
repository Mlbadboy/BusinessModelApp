using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Domain.Connectors;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.DigitalTwin;
using BusinessModelApp.Infrastructure.Interceptors;
using BusinessModelApp.Infrastructure.Reality;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using EvidenceRecord = BusinessModelApp.Core.Domain.Reality.EvidenceRecord;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase2Batch2_DigitalTwinTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(new AppendOnlyAuditInterceptor())
                .Options;

            return new AppDbContext(options);
        }

        private CompanyDigitalTwinService CreateService(AppDbContext dbContext)
        {
            var evidenceGraph = new EvidenceGraphService();
            var decayEngine = new RealityDecayEngine();
            var logger = NullLogger<CompanyDigitalTwinService>.Instance;

            return new CompanyDigitalTwinService(dbContext, evidenceGraph, decayEngine, logger);
        }

        // =========================================================================
        // 1. STRICT REALITY CLASSIFICATION TESTS
        // =========================================================================

        [Fact]
        public void TruthClassification_CategoriesAreDistinctAndNonInterchangeable()
        {
            var fact = TruthClassification.Fact;
            var estimate = TruthClassification.Estimate;
            var hypothesis = TruthClassification.Hypothesis;
            var observation = TruthClassification.Observation;
            var learning = TruthClassification.Learning;
            var unknown = TruthClassification.Unknown;

            Assert.NotEqual(fact, estimate);
            Assert.NotEqual(fact, hypothesis);
            Assert.NotEqual(fact, observation);
            Assert.NotEqual(fact, learning);
            Assert.NotEqual(fact, unknown);

            Assert.NotEqual(estimate, hypothesis);
            Assert.NotEqual(observation, learning);
            Assert.NotEqual(unknown, estimate);
        }

        [Fact]
        public void DigitalTwinFieldState_CorrectlyRetainsAssignedClassification()
        {
            var factField = DigitalTwinFieldState.Fact("Financial.CashInBankINR", "Cash in Bank", DigitalTwinDimension.Financial, 1000000m, "₹10,00,000.00", "Bank.Feed", Guid.NewGuid(), "hash_123", 1.0, FreshnessState.VERIFIED, "Verified bank balance.");
            var estimateField = DigitalTwinFieldState.Estimate("Commercial.WinRate", "Win Rate", DigitalTwinDimension.Commercial, 0.25, "25.0%", "Model", 0.70, "Historical approximation.");
            var hypothesisField = DigitalTwinFieldState.Hypothesis("Strategy.Route", "Route Hypothesis", DigitalTwinDimension.StrategicState, "EnterpriseAI", "Enterprise AI Focus", "AI.Planner", "Proposed market expansion.");
            var observationField = DigitalTwinFieldState.Observation("Connectors.Telemetry", "Raw Telemetry", DigitalTwinDimension.Connectors, "CONNECTED", "CONNECTED", "Webhook", 0.80, "Raw webhook feed.");
            var learningField = DigitalTwinFieldState.Learning("Playbook.Guidance", "Guidance", DigitalTwinDimension.MarketSignals, "B2B Focus", "B2B Focus", "Playbook", "Historical learning.");
            var unknownField = DigitalTwinFieldState.Unknown("Inventory.Licenses", "Licenses", DigitalTwinDimension.Inventory, "Untracked asset.");

            Assert.Equal(TruthClassification.Fact, factField.Classification);
            Assert.Equal(TruthClassification.Estimate, estimateField.Classification);
            Assert.Equal(TruthClassification.Hypothesis, hypothesisField.Classification);
            Assert.Equal(TruthClassification.Observation, observationField.Classification);
            Assert.Equal(TruthClassification.Learning, learningField.Classification);
            Assert.Equal(TruthClassification.Unknown, unknownField.Classification);
        }

        // =========================================================================
        // 2. PROMOTION INVARIANTS & BARRIER TESTS
        // =========================================================================

        [Theory]
        [InlineData(TruthClassification.Estimate)]
        [InlineData(TruthClassification.Hypothesis)]
        [InlineData(TruthClassification.Learning)]
        [InlineData(TruthClassification.Unknown)]
        public void PromotionGuard_BlocksDirectPromotionToFact_WithoutGovernedEvidence(TruthClassification fromClassification)
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                TruthClassificationPromotionGuard.ValidatePromotion(
                    from: fromClassification,
                    to: TruthClassification.Fact,
                    confidence: 0.95,
                    hasEvidence: true,
                    isCorroborated: true,
                    isStale: false,
                    isDirectAiPromotion: false);
            });

            Assert.Contains("Invariant Violation", ex.Message);
        }

        [Fact]
        public void PromotionGuard_BlocksPromotionToFact_WhenEvidenceIsMissing()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                TruthClassificationPromotionGuard.ValidatePromotion(
                    from: TruthClassification.Observation,
                    to: TruthClassification.Fact,
                    confidence: 0.90,
                    hasEvidence: false, // NO EVIDENCE
                    isCorroborated: true);
            });

            Assert.Contains("backing EvidenceRecord IDs", ex.Message);
        }

        [Fact]
        public void PromotionGuard_BlocksPromotionToFact_WhenConfidenceBelowThreshold()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                TruthClassificationPromotionGuard.ValidatePromotion(
                    from: TruthClassification.Observation,
                    to: TruthClassification.Fact,
                    confidence: 0.65, // Below 0.70 threshold
                    hasEvidence: true,
                    isCorroborated: true);
            });

            Assert.Contains("minimum confidence score of 0.70", ex.Message);
        }

        [Fact]
        public void PromotionGuard_BlocksPromotionToFact_WhenEvidenceIsStale()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                TruthClassificationPromotionGuard.ValidatePromotion(
                    from: TruthClassification.Observation,
                    to: TruthClassification.Fact,
                    confidence: 0.95,
                    hasEvidence: true,
                    isCorroborated: true,
                    isStale: true); // STALE EVIDENCE
            });

            Assert.Contains("Stale or expired evidence cannot be promoted", ex.Message);
        }

        [Fact]
        public void PromotionGuard_BlocksDirectAiPromotion_ToFact()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                TruthClassificationPromotionGuard.ValidatePromotion(
                    from: TruthClassification.Observation,
                    to: TruthClassification.Fact,
                    confidence: 1.0,
                    hasEvidence: true,
                    isCorroborated: true,
                    isStale: false,
                    isDirectAiPromotion: true); // AI INJECTION
            });

            Assert.Contains("AI cannot directly create or promote any metric to FACT", ex.Message);
        }

        // =========================================================================
        // 3. MULTI-TENANT QUARANTINE & ISOLATION TESTS
        // =========================================================================

        [Fact]
        public async Task MultiTenantQuarantine_TenantACannotAccessTenantBData()
        {
            using var db = CreateInMemoryDbContext();
            var service = CreateService(db);

            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            // Seed Tenant B with confidential financial evidence
            await db.EvidenceRecords.AddAsync(new EvidenceRecord
            {
                WorkspaceId = tenantB,
                SourceType = EvidenceSourceType.PaymentGateway,
                SourceSystem = "Razorpay",
                EvidenceDigest = "Authoritative payment settlement verified for transaction pay_tenantB (₹5,000,000.00 INR).",
                RawPayloadHash = "hash_tenant_b_secret",
                CanonicalPayloadHash = "hash_canonical_tenant_b",
                Status = VerificationStatus.VerifiedFact,
                RetrievedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            // Query Tenant A state
            var stateA = await service.GetCurrentStateAsync(tenantA);
            var stateB = await service.GetCurrentStateAsync(tenantB);

            // Tenant A must see UNKNOWN or 0 for revenue, while Tenant B sees ₹50L
            var revA = stateA.Dimensions[DigitalTwinDimension.Revenue].Fields.First(f => f.FieldPath == "Revenue.RecognizedRevenueINR");
            var revB = stateB.Dimensions[DigitalTwinDimension.Revenue].Fields.First(f => f.FieldPath == "Revenue.RecognizedRevenueINR");

            Assert.Equal(TruthClassification.Unknown, revA.Classification);
            Assert.Equal("UNKNOWN", revA.DisplayValue);

            Assert.Equal(TruthClassification.Fact, revB.Classification);
            Assert.True(revB.DisplayValue.Contains("50,00,000.00") || revB.DisplayValue.Contains("5,000,000.00"));
        }

        [Fact]
        public async Task MultiTenantQuarantine_CrossTenantSnapshotComparison_IsDenied()
        {
            using var db = CreateInMemoryDbContext();
            var service = CreateService(db);

            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            var snapA = await service.CreateSnapshotAsync(tenantA);
            var snapB = await service.CreateSnapshotAsync(tenantB);

            // Tenant A attempts to compare its snapshot with Tenant B's snapshot
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CompareSnapshotsAsync(tenantA, snapA.Id, snapB.Id);
            });
        }

        // =========================================================================
        // 4. FIELD-LEVEL PROVENANCE METROLOGY TESTS
        // =========================================================================

        [Fact]
        public async Task FieldLevelProvenance_EveryFactIsCryptographicallyTraceable()
        {
            using var db = CreateInMemoryDbContext();
            var service = CreateService(db);

            var workspaceId = Guid.NewGuid();
            var evidenceRecordId = Guid.NewGuid();
            string rawHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

            await db.EvidenceRecords.AddAsync(new EvidenceRecord
            {
                Id = evidenceRecordId,
                WorkspaceId = workspaceId,
                SourceType = EvidenceSourceType.PaymentGateway,
                SourceSystem = "Razorpay",
                EvidenceDigest = "Authoritative payment settlement verified for transaction pay_prov_test (₹1,500,000.00 INR).",
                RawPayloadHash = rawHash,
                CanonicalPayloadHash = rawHash,
                Status = VerificationStatus.VerifiedFact,
                RetrievedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var state = await service.GetCurrentStateAsync(workspaceId);
            var revField = state.Dimensions[DigitalTwinDimension.Revenue].Fields.First(f => f.FieldPath == "Revenue.RecognizedRevenueINR");

            Assert.Equal(TruthClassification.Fact, revField.Classification);
            Assert.True(revField.DisplayValue.Contains("15,00,000.00") || revField.DisplayValue.Contains("1,500,000.00"));
            Assert.Contains(evidenceRecordId, revField.EvidenceRecordIds);
            Assert.Contains(rawHash, revField.GroundingEvidenceHashes);
            Assert.Equal(1.0, revField.Confidence);
            Assert.Equal(FreshnessState.VERIFIED, revField.Freshness);
            Assert.True(revField.IsGroundedFact);

            // Verify evidence retrieval via field path
            var fetchedEvidence = await service.GetEvidenceForFieldAsync(workspaceId, "Revenue.RecognizedRevenueINR");
            Assert.Single(fetchedEvidence);
            Assert.Equal(evidenceRecordId, fetchedEvidence[0].Id);
        }

        // =========================================================================
        // 5. CONFLICTING EVIDENCE & DISPUTED STATE TESTS
        // =========================================================================

        [Fact]
        public async Task ConflictingEvidence_DetectsDiscrepancyBetweenCrmAndGateway_CreatesConflictRecord()
        {
            using var db = CreateInMemoryDbContext();
            var service = CreateService(db);
            var workspaceId = Guid.NewGuid();

            // 1. Payment Gateway reports settled revenue = ₹46L
            await db.EvidenceRecords.AddAsync(new EvidenceRecord
            {
                WorkspaceId = workspaceId,
                SourceType = EvidenceSourceType.PaymentGateway,
                SourceSystem = "Razorpay",
                EvidenceDigest = "Authoritative payment settlement verified for transaction pay_settled (₹4,600,000.00 INR).",
                RawPayloadHash = "hash_gateway_46L",
                CanonicalPayloadHash = "hash_gateway_46L",
                Status = VerificationStatus.VerifiedFact,
                RetrievedAt = DateTime.UtcNow
            });

            // 2. CRM reports Closed-Won deal = ₹50L
            await db.Opportunities.AddAsync(new Opportunity
            {
                WorkspaceId = workspaceId,
                Title = "Acme Enterprise Deal",
                EstimatedValue = 5000000m,
                Stage = OpportunityStage.ClosedWon,
                Probability = 100
            });
            await db.SaveChangesAsync();

            var state = await service.GetCurrentStateAsync(workspaceId);
            var conflicts = await service.GetConflictsAsync(workspaceId);

            // A conflict MUST be recorded deterministically
            Assert.Single(conflicts);
            var conflict = conflicts[0];
            Assert.Equal("Financial.TotalRevenueINR", conflict.FieldPath);
            Assert.Equal(ConflictResolutionStatus.Unresolved, conflict.Status);
            Assert.True(conflict.ValueA.Contains("50,00,000.00") || conflict.ValueA.Contains("5,000,000.00"));
            Assert.True(conflict.ValueB.Contains("46,00,000.00") || conflict.ValueB.Contains("4,600,000.00"));

            // The financial revenue field must be marked as disputed
            var finRev = state.Dimensions[DigitalTwinDimension.Financial].Fields.First(f => f.FieldPath == "Financial.TotalRevenueINR");
            Assert.True(finRev.IsDisputed);
            Assert.Equal(conflict.Id, finRev.ActiveConflictId);
        }

        // =========================================================================
        // 6. SNAPSHOT REPRODUCIBILITY & IMMUTABILITY TESTS
        // =========================================================================

        [Fact]
        public async Task DigitalTwinSnapshot_ReproducesDeterministicIntegrityHash()
        {
            var workspaceId = Guid.NewGuid();
            var timestamp = new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc);
            var payload = "{\"test\":\"reproducibility\"}";

            string hash1 = DigitalTwinSnapshot.ComputeIntegrityHash(workspaceId, timestamp, payload);
            string hash2 = DigitalTwinSnapshot.ComputeIntegrityHash(workspaceId, timestamp, payload);

            Assert.Equal(hash1, hash2);
            Assert.Equal(64, hash1.Length);
        }

        [Fact]
        public async Task DigitalTwinSnapshot_IsProtectedByAppendOnlyAuditInterceptor()
        {
            using var db = CreateInMemoryDbContext();
            var workspaceId = Guid.NewGuid();

            var snapshot = new DigitalTwinSnapshot
            {
                WorkspaceId = workspaceId,
                CreatedAt = DateTime.UtcNow,
                IntegrityHash = "test_hash_immutable",
                SerializedStateJson = "{}"
            };

            await db.DigitalTwinSnapshots.AddAsync(snapshot);
            await db.SaveChangesAsync();

            // Attempt to mutate snapshot
            snapshot.IntegrityHash = "tampered_hash";
            db.Entry(snapshot).State = EntityState.Modified;

            var modEx = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await db.SaveChangesAsync();
            });
            Assert.Contains("immutable", modEx.Message);

            // Attempt to delete snapshot
            db.Entry(snapshot).State = EntityState.Deleted;
            var delEx = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await db.SaveChangesAsync();
            });
            Assert.Contains("append-only", delEx.Message);
        }

        // =========================================================================
        // 7. DIFF ENGINE DETERMINISM TESTS
        // =========================================================================

        [Fact]
        public async Task DigitalTwinDiffEngine_CapturesChangesBetweenSnapshots()
        {
            using var db = CreateInMemoryDbContext();
            var service = CreateService(db);
            var workspaceId = Guid.NewGuid();

            // Snapshot A (Initial ungrounded state)
            var snapA = await service.CreateSnapshotAsync(workspaceId);

            // Add new verified revenue evidence
            await db.EvidenceRecords.AddAsync(new EvidenceRecord
            {
                WorkspaceId = workspaceId,
                SourceType = EvidenceSourceType.PaymentGateway,
                SourceSystem = "Stripe",
                EvidenceDigest = "Authoritative payment settlement verified for transaction pay_diff (₹2,000,000.00 INR).",
                RawPayloadHash = "hash_diff_revenue",
                CanonicalPayloadHash = "hash_diff_revenue",
                Status = VerificationStatus.VerifiedFact,
                RetrievedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            // Snapshot B (Post-evidence state)
            var snapB = await service.CreateSnapshotAsync(workspaceId);

            // Compare snapshots
            var diff = await service.CompareSnapshotsAsync(workspaceId, snapA.Id, snapB.Id);

            Assert.NotNull(diff);
            Assert.NotEmpty(diff.Changes);

            // Revenue classification changed from Unknown to Fact
            var revChange = diff.Changes.FirstOrDefault(c => c.FieldPath == "Revenue.RecognizedRevenueINR" && c.DiffType == "ClassificationChanged");
            Assert.NotNull(revChange);
            Assert.Equal(TruthClassification.Unknown, revChange.OldClassification);
            Assert.Equal(TruthClassification.Fact, revChange.NewClassification);
        }

        // =========================================================================
        // 8. REVENUE SAFETY INVARIANT TESTS
        // =========================================================================

        [Fact]
        public async Task RevenueSafety_PipelineAndForecastNeverBecomeRecognizedRevenue()
        {
            using var db = CreateInMemoryDbContext();
            var service = CreateService(db);
            var workspaceId = Guid.NewGuid();

            // Massive unclosed sales pipeline: ₹50,00,000
            await db.Opportunities.AddAsync(new Opportunity
            {
                WorkspaceId = workspaceId,
                Title = "Mega Deal",
                EstimatedValue = 5000000m,
                Stage = OpportunityStage.Proposal,
                Probability = 70
            });
            await db.SaveChangesAsync();

            var state = await service.GetCurrentStateAsync(workspaceId);
            var revField = state.Dimensions[DigitalTwinDimension.Revenue].Fields.First(f => f.FieldPath == "Revenue.RecognizedRevenueINR");
            var pipelineField = state.Dimensions[DigitalTwinDimension.Revenue].Fields.First(f => f.FieldPath == "Revenue.WeightedPipelineINR");

            // Invariant: Pipeline cannot become recognized revenue without reconciled payment evidence!
            Assert.Equal(TruthClassification.Unknown, revField.Classification);
            Assert.Equal("UNKNOWN", revField.DisplayValue);

            // Pipeline is correctly classified as ESTIMATE
            Assert.Equal(TruthClassification.Estimate, pipelineField.Classification);
            Assert.True(pipelineField.DisplayValue.Contains("35,00,000.00") || pipelineField.DisplayValue.Contains("3,500,000.00"));
        }

        // =========================================================================
        // 9. UNKNOWN WORLD & FIRST-CLASS UNKNOWN TESTS
        // =========================================================================

        [Fact]
        public async Task UnknownWorld_MissingEvidenceYieldsUnknown_NeverSyntheticDefaults()
        {
            using var db = CreateInMemoryDbContext();
            var service = CreateService(db);
            var workspaceId = Guid.NewGuid();

            var state = await service.GetCurrentStateAsync(workspaceId);
            var unknowns = await service.GetUnknownsAsync(workspaceId);

            Assert.NotEmpty(unknowns);

            // Inventory fields must be UNKNOWN
            var computeField = state.Dimensions[DigitalTwinDimension.Inventory].Fields.First(f => f.FieldPath == "Inventory.CloudComputeAllocations");
            Assert.Equal(TruthClassification.Unknown, computeField.Classification);
            Assert.Equal("UNKNOWN", computeField.DisplayValue);
            Assert.NotEqual("0", computeField.DisplayValue);

            // Customer NPS must be UNKNOWN
            var npsField = state.Dimensions[DigitalTwinDimension.Customers].Fields.First(f => f.FieldPath == "Customers.NetPromoterScore");
            Assert.Equal(TruthClassification.Unknown, npsField.Classification);
            Assert.Equal("UNKNOWN", npsField.DisplayValue);

            // Market Demand growth must be UNKNOWN
            var demandField = state.Dimensions[DigitalTwinDimension.MarketSignals].Fields.First(f => f.FieldPath == "MarketSignals.MarketDemandGrowthRate");
            Assert.Equal(TruthClassification.Unknown, demandField.Classification);
        }

        // =========================================================================
        // 10. ADVERSARIAL RED-TEAM INJECTION TESTS
        // =========================================================================

        [Fact]
        public void Adversarial_AiAttemptToPromoteEstimateToFact_IsBlocked()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                TruthClassificationPromotionGuard.ValidatePromotion(
                    from: TruthClassification.Estimate,
                    to: TruthClassification.Fact,
                    confidence: 0.99,
                    hasEvidence: true,
                    isCorroborated: true,
                    isStale: false,
                    isDirectAiPromotion: true);
            });

            Assert.Contains("AI cannot directly create or promote", ex.Message);
        }

        [Fact]
        public void Adversarial_AttemptToPromoteLearningToFact_IsBlocked()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                TruthClassificationPromotionGuard.ValidatePromotion(
                    from: TruthClassification.Learning,
                    to: TruthClassification.Fact,
                    confidence: 1.0,
                    hasEvidence: true,
                    isCorroborated: true);
            });

            Assert.Contains("LEARNING heuristic cannot be promoted to FACT", ex.Message);
        }

        [Fact]
        public void Adversarial_AttemptToPromoteUnknownWithoutEvidence_IsBlocked()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                TruthClassificationPromotionGuard.ValidatePromotion(
                    from: TruthClassification.Unknown,
                    to: TruthClassification.Fact,
                    confidence: 0.50,
                    hasEvidence: false,
                    isCorroborated: false);
            });

            Assert.Contains("UNKNOWN cannot be directly promoted to FACT", ex.Message);
        }

        [Fact]
        public async Task DigitalTwinHealthReport_CalculatesObjectiveMetrology()
        {
            using var db = CreateInMemoryDbContext();
            var service = CreateService(db);
            var workspaceId = Guid.NewGuid();

            var health = await service.GetHealthReportAsync(workspaceId);

            Assert.True(health.TotalTrackedFields > 20);
            Assert.True(health.UnknownRatioPercent > 0.0);
            Assert.True(health.EvidenceCoveragePercent >= 0.0);
            Assert.True(health.AverageConfidence >= 0.0);
        }
    }
}
