using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BusinessModelApp.Core.Domain.Commercial;
using CoreUser = BusinessModelApp.Core.Domain.Users.User;
using CoreRole = BusinessModelApp.Core.Domain.Users.Role;

namespace BusinessModelApp.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<CoreUser, CoreRole, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Multi-Tenant & Commercial DbSets
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Opportunity> Opportunities { get; set; }
        public DbSet<Interaction> Interactions { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }
        public DbSet<BusinessActivity> BusinessActivities { get; set; }
        public DbSet<BusinessModelApp.Core.AI.AICallRecord> AICallRecords { get; set; }
        public DbSet<BusinessModelApp.Core.AI.Governance.AIUsageDaily> AIUsageDailies { get; set; }
        public DbSet<BusinessModelApp.Core.AI.Governance.AIBudgetPolicy> AIBudgetPolicies { get; set; }
        public DbSet<BusinessModelApp.Core.AI.Governance.ApprovalRequest> ApprovalRequests { get; set; }
        public DbSet<BusinessModelApp.Core.AI.Governance.AITrafficControlPolicy> AITrafficControlPolicies { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Auth.RefreshToken> RefreshTokens { get; set; }
        public DbSet<VoiceCallRecord> VoiceCallRecords { get; set; }
        public DbSet<VoiceWebhookEvent> VoiceWebhookEvents { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Connectors.ConnectorEntity> Connectors { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Reality.EvidenceRecord> EvidenceRecords { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Objectives.BusinessObjective> BusinessObjectives { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Strategy.BusinessStrategy> BusinessStrategies { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.WorldModel.CompanySnapshot> CompanySnapshots { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Decisions.DecisionRecord> DecisionRecords { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Missions.DurableMission> DurableMissions { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Missions.DurableMissionCheckpoint> MissionCheckpoints { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.DigitalTwin.DigitalTwinSnapshot> DigitalTwinSnapshots { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.DigitalTwin.DigitalTwinConflictRecord> DigitalTwinConflicts { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.LearningRecord> LearningRecords { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.OutcomeRecord> OutcomeRecords { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.FailureRecord> FailureRecords { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.CorrectionRecord> CorrectionRecords { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.LearningEpisode> LearningEpisodes { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.LearningContradictionRecord> LearningContradictions { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.LearningExperiment> LearningExperiments { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.AlternativeHypothesis> AlternativeHypotheses { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.CounterfactualSimulation> CounterfactualSimulations { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.LearningInfluenceRecord> LearningInfluenceRecords { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.LearningReversalNotice> LearningReversalNotices { get; set; }
        public DbSet<BusinessModelApp.Core.Domain.Learning.UncertaintyBudget> UncertaintyBudgets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Organization
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slug).IsUnique();
            });

            // Workspace
            modelBuilder.Entity<Workspace>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("INR");
                
                entity.HasOne(e => e.Organization)
                      .WithMany(o => o.Workspaces)
                      .HasForeignKey(e => e.OrganizationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Lead
            modelBuilder.Entity<Lead>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContactName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.CompanyName).HasMaxLength(200);
                if (Database.IsSqlServer())
                {
                    entity.Property(e => e.RowVersion).IsRowVersion();
                }
                else
                {
                    entity.Property(e => e.RowVersion).IsConcurrencyToken();
                }

                entity.OwnsOne(e => e.ObservedState);

                entity.HasOne(e => e.Workspace)
                      .WithMany(w => w.Leads)
                      .HasForeignKey(e => e.WorkspaceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Opportunity
            modelBuilder.Entity<Opportunity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.Property(e => e.EstimatedValue).HasPrecision(18, 2);
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("INR");
                if (Database.IsSqlServer())
                {
                    entity.Property(e => e.RowVersion).IsRowVersion();
                }
                else
                {
                    entity.Property(e => e.RowVersion).IsConcurrencyToken();
                }

                entity.OwnsOne(e => e.ObservedState);

                entity.HasOne(e => e.Workspace)
                      .WithMany(w => w.Opportunities)
                      .HasForeignKey(e => e.WorkspaceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lead)
                      .WithOne(l => l.Opportunity)
                      .HasForeignKey<Opportunity>(e => e.LeadId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // EvidenceRecord (Reality Engine Grounding Ledger)
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Reality.EvidenceRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SourceSystem).HasMaxLength(150);
                entity.Property(e => e.SourceIdentifier).HasMaxLength(200);
                entity.Property(e => e.RawPayloadHash).IsRequired().HasMaxLength(64);
                entity.Property(e => e.CanonicalPayloadHash).IsRequired().HasMaxLength(64);
                entity.HasIndex(e => e.WorkspaceId);
                entity.HasIndex(e => e.RawPayloadHash);
                entity.HasIndex(e => e.CanonicalPayloadHash);
                entity.HasIndex(e => e.ParentEntityId);
            });

            // BusinessObjective (Executive Targets & Grounded Progress)
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Objectives.BusinessObjective>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.OwnsOne(e => e.ObservedProgress);
                entity.HasIndex(e => e.WorkspaceId);
                entity.HasIndex(e => e.Status);
            });

            // BusinessStrategy (Deterministic Funnel & Feasibility)
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Strategy.BusinessStrategy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StrategyName).IsRequired().HasMaxLength(250);
                entity.Property(e => e.StrategyVersion).HasMaxLength(50);
                entity.HasIndex(e => e.ObjectiveId);
                entity.HasIndex(e => e.FeasibilityState);
            });

            // CompanySnapshot (Field-by-Field Truth Snapshot)
            modelBuilder.Entity<BusinessModelApp.Core.Domain.WorldModel.CompanySnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Ignore(e => e.VerifiedRevenueINR);
                entity.Ignore(e => e.VerifiedSettledCashINR);
                entity.Ignore(e => e.ActivePipelineINR);
                entity.Ignore(e => e.QualifiedPipelineINR);
                entity.Ignore(e => e.OpenOpportunitiesCount);
                entity.Ignore(e => e.VerifiedProspectsCount);
                entity.Ignore(e => e.ActiveDeliveryProjectsCount);
                entity.Ignore(e => e.AvailableDeliverySlots);
                entity.Ignore(e => e.ReservedComputeBudgetINR);
                entity.Ignore(e => e.OutstandingInvoicesINR);
                entity.Ignore(e => e.PaymentRiskScore);
                entity.Ignore(e => e.OverallEvidenceCoverageScore);
                entity.HasIndex(e => e.WorkspaceId);
                entity.HasIndex(e => e.CapturedAt);
            });

            // DecisionRecord (Immutable Lineage)
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Decisions.DecisionRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SelectedAlternative).IsRequired().HasMaxLength(250);
                entity.HasIndex(e => e.ObjectiveId);
                entity.HasIndex(e => e.StrategyId);
                entity.HasIndex(e => e.SupersedesDecisionId);
            });

            // DurableMission & DurableMissionCheckpoint
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Missions.DurableMission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.HasIndex(e => e.WorkspaceId);
                entity.HasIndex(e => e.ObjectiveId);
                entity.HasIndex(e => e.State);
                entity.HasMany(e => e.Checkpoints)
                      .WithOne()
                      .HasForeignKey(c => c.MissionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BusinessModelApp.Core.Domain.Missions.DurableMissionCheckpoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StepName).IsRequired().HasMaxLength(150);
                entity.HasIndex(e => e.MissionId);
                entity.HasIndex(e => e.StepIndex);
            });

            // Interaction
            modelBuilder.Entity<Interaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Channel).HasMaxLength(50);

                entity.HasOne(e => e.Lead)
                      .WithMany(l => l.Interactions)
                      .HasForeignKey(e => e.LeadId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Activity (Legacy & General activities)
            modelBuilder.Entity<Activity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PerformedByName).HasMaxLength(100);

                entity.HasOne(e => e.Opportunity)
                      .WithMany(o => o.Activities)
                      .HasForeignKey(e => e.OpportunityId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditEvent (Security & compliance audit log)
            modelBuilder.Entity<AuditEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ActionName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PerformedByName).HasMaxLength(100);
                entity.HasIndex(e => new { e.WorkspaceId, e.Timestamp });
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
            });

            // BusinessActivity (Commercial touchpoints)
            modelBuilder.Entity<BusinessActivity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PerformedByName).HasMaxLength(100);

                entity.HasOne(e => e.Opportunity)
                      .WithMany()
                      .HasForeignKey(e => e.OpportunityId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lead)
                      .WithMany()
                      .HasForeignKey(e => e.LeadId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.WorkspaceId, e.CreatedAt });
            });

            // Core User Organization Scoping
            modelBuilder.Entity<CoreUser>(entity =>
            {
                entity.HasOne<Organization>()
                      .WithMany()
                      .HasForeignKey(u => u.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Workspace>()
                      .WithMany()
                      .HasForeignKey(u => u.DefaultWorkspaceId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // AICallRecord (Append-only AI telemetry)
            modelBuilder.Entity<BusinessModelApp.Core.AI.AICallRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Model).IsRequired().HasMaxLength(150);
                entity.Property(e => e.EstimatedCost).HasPrecision(18, 6);
                entity.Property(e => e.RequestCorrelationId).HasMaxLength(100);
                entity.Property(e => e.OmniRouteRequestId).HasMaxLength(100);

                entity.HasIndex(e => new { e.OrganizationId, e.WorkspaceId, e.CreatedAt });
                entity.HasIndex(e => e.RequestCorrelationId);
            });

            // AIUsageDaily (Fast derived FinOps aggregation)
            modelBuilder.Entity<BusinessModelApp.Core.AI.Governance.AIUsageDaily>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EstimatedCost).HasPrecision(18, 6);
                entity.HasIndex(e => new { e.OrganizationId, e.WorkspaceId, e.Date }).IsUnique();
            });

            // AIBudgetPolicy
            modelBuilder.Entity<BusinessModelApp.Core.AI.Governance.AIBudgetPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MonthlyBudgetCap).HasPrecision(18, 2);
                entity.Property(e => e.DailyBudgetCap).HasPrecision(18, 2);
                entity.Property(e => e.MaxCostPerRequest).HasPrecision(18, 2);
                entity.Property(e => e.WarningThresholdPercent).HasPrecision(5, 2);
                entity.HasIndex(e => new { e.OrganizationId, e.WorkspaceId }).IsUnique();
            });

            // ApprovalRequest
            modelBuilder.Entity<BusinessModelApp.Core.AI.Governance.ApprovalRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ActionType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.Property(e => e.RequesterName).HasMaxLength(150);
                entity.Property(e => e.DecidedByName).HasMaxLength(150);
                entity.HasIndex(e => new { e.OrganizationId, e.WorkspaceId, e.Status });
            });

            // AITrafficControlPolicy (Kill-switch)
            modelBuilder.Entity<BusinessModelApp.Core.AI.Governance.AITrafficControlPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DisabledReason).HasMaxLength(255);
                entity.Property(e => e.UpdatedByName).HasMaxLength(150);
                entity.HasIndex(e => new { e.OrganizationId, e.WorkspaceId }).IsUnique();
            });

            // RefreshToken
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Auth.RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedByIp).HasMaxLength(50);
                entity.Property(e => e.RevokedByIp).HasMaxLength(50);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(255);
                entity.Property(e => e.RevocationReason).HasMaxLength(255);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
            });

            // VoiceCallRecord
            modelBuilder.Entity<VoiceCallRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProviderCallId).IsRequired().HasMaxLength(256);
                entity.Property(e => e.ContactName).HasMaxLength(200);
                entity.Property(e => e.CompanyName).HasMaxLength(200);
                entity.Property(e => e.EstimatedCostINR).HasPrecision(18, 4);
                entity.Property(e => e.ActualCostINR).HasPrecision(18, 4);

                entity.HasIndex(e => e.ProviderCallId).IsUnique();
                entity.HasIndex(e => new { e.WorkspaceId, e.CreatedAt });
                entity.HasIndex(e => e.LeadId);

                entity.HasOne(e => e.Lead)
                      .WithMany()
                      .HasForeignKey(e => e.LeadId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // VoiceWebhookEvent (Idempotency ledger)
            modelBuilder.Entity<VoiceWebhookEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventId).IsRequired().HasMaxLength(256);
                entity.Property(e => e.ProviderCallId).IsRequired().HasMaxLength(256);

                entity.HasIndex(e => new { e.Provider, e.EventId }).IsUnique();
                entity.HasIndex(e => e.ProviderCallId);
            });

            // ConnectorEntity (Charlie Connect integration control plane)
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Connectors.ConnectorEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AccountIdentifier).HasMaxLength(200);
                entity.HasIndex(e => new { e.WorkspaceId, e.Provider }).IsUnique();
                entity.HasIndex(e => new { e.WorkspaceId, e.Status });
            });

            // DigitalTwinSnapshot (Immutable, cryptographic point-in-time state)
            modelBuilder.Entity<BusinessModelApp.Core.Domain.DigitalTwin.DigitalTwinSnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IntegrityHash).IsRequired().HasMaxLength(64);
                entity.Property(e => e.SourceVersion).HasMaxLength(50);
                entity.Property(e => e.RealityVersion).HasMaxLength(50);
                entity.Property(e => e.EvidenceVersion).HasMaxLength(50);
                entity.Property(e => e.TwinVersion).HasMaxLength(50);
                entity.HasIndex(e => new { e.WorkspaceId, e.CreatedAt });
                entity.HasIndex(e => e.IntegrityHash);
            });

            // DigitalTwinConflictRecord (Deterministic multi-source dispute ledger)
            modelBuilder.Entity<BusinessModelApp.Core.Domain.DigitalTwin.DigitalTwinConflictRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FieldPath).IsRequired().HasMaxLength(250);
                entity.Property(e => e.SourceA).HasMaxLength(150);
                entity.Property(e => e.SourceB).HasMaxLength(150);
                entity.Property(e => e.ResolutionNote).HasMaxLength(500);
                entity.HasIndex(e => new { e.WorkspaceId, e.Status });
                entity.HasIndex(e => new { e.WorkspaceId, e.FieldPath });
            });

            // LearningRecord
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.LearningRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Statement).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Domain).HasMaxLength(150);
                entity.Property(e => e.IntegrityHash).HasMaxLength(64);
                entity.Ignore(e => e.EvidenceRecordIds);
                entity.Ignore(e => e.GroundingEvidenceHashes);
                entity.Ignore(e => e.AlternativeHypotheses);
                entity.OwnsOne(e => e.ContaminationScore);
                entity.HasIndex(e => new { e.WorkspaceId, e.State });
                entity.HasIndex(e => new { e.WorkspaceId, e.Tier });
            });

            // OutcomeRecord
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.OutcomeRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExpectedRevenueINR).HasPrecision(18, 2);
                entity.Property(e => e.ActualRevenueINR).HasPrecision(18, 2);
                entity.Property(e => e.ExpectedCostINR).HasPrecision(18, 2);
                entity.Property(e => e.ActualCostINR).HasPrecision(18, 2);
                entity.HasIndex(e => new { e.WorkspaceId, e.MissionId });
            });

            // FailureRecord
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.FailureRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Diagnosis).IsRequired().HasMaxLength(1000);
                entity.HasIndex(e => new { e.WorkspaceId, e.RootCause });
            });

            // CorrectionRecord
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.CorrectionRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ActionTaken).IsRequired().HasMaxLength(1000);
                entity.HasIndex(e => e.FailureRecordId);
            });

            // LearningEpisode
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.LearningEpisode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Narrative).HasMaxLength(1000);
                entity.HasIndex(e => new { e.WorkspaceId, e.MissionId });
            });

            // LearningContradictionRecord
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.LearningContradictionRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Details).HasMaxLength(1000);
                entity.HasIndex(e => new { e.WorkspaceId, e.Status });
            });

            // LearningExperiment
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.LearningExperiment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Hypothesis).IsRequired().HasMaxLength(500);
                entity.Property(e => e.TargetMetric).IsRequired().HasMaxLength(150);
                entity.Property(e => e.BudgetCapINR).HasPrecision(18, 2);
                entity.Property(e => e.SpentINR).HasPrecision(18, 2);
                entity.HasIndex(e => new { e.WorkspaceId, e.Status });
            });

            // AlternativeHypothesis
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.AlternativeHypothesis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.HypothesisCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Statement).IsRequired().HasMaxLength(500);
                entity.Ignore(e => e.SupportingEvidenceIds);
                entity.Ignore(e => e.RefutingEvidenceIds);
                entity.HasIndex(e => e.LearningRecordId);
            });

            // CounterfactualSimulation
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.CounterfactualSimulation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ModelId).HasMaxLength(150);
                entity.Property(e => e.SimulationVersion).HasMaxLength(50);
                entity.HasIndex(e => new { e.WorkspaceId, e.SourceMissionId });
            });

            // LearningInfluenceRecord
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.LearningInfluenceRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SourceType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SourceName).HasMaxLength(200);
                entity.HasIndex(e => new { e.WorkspaceId, e.DecisionId });
            });

            // LearningReversalNotice
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.LearningReversalNotice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
                entity.Property(e => e.EstimatedRevenueDeviationINR).HasPrecision(18, 2);
                entity.HasIndex(e => new { e.WorkspaceId, e.LearningRecordId });
            });

            // UncertaintyBudget
            modelBuilder.Entity<BusinessModelApp.Core.Domain.Learning.UncertaintyBudget>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CalculationVersion).HasMaxLength(50);
                entity.HasIndex(e => new { e.WorkspaceId, e.CalculatedAt });
            });
        }
    }
}
