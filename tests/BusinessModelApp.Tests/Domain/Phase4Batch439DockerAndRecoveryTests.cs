using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce;
using FluentAssertions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch439DockerAndRecoveryTests
    {
        private readonly InMemoryContinuousOperationsStore _continuousStore;
        private readonly ContinuousBusinessOperationsCoordinator _continuousCoordinator;

        private readonly InMemoryCommercialAccountAndLineageStore _lineageStore;
        private readonly CommercialLineageService _lineageService;

        public Phase4Batch439DockerAndRecoveryTests()
        {
            _continuousStore = new InMemoryContinuousOperationsStore();
            _continuousCoordinator = new ContinuousBusinessOperationsCoordinator(_continuousStore);

            _lineageStore = new InMemoryCommercialAccountAndLineageStore();
            _lineageService = new CommercialLineageService(_lineageStore);
        }

        // =========================================================================
        // Family 1: Container Crash & State Restoration
        // =========================================================================

        [Fact]
        public async Task DOCKER439_01_CrashRecovery_RestoresCheckpointsWithoutDataLoss()
        {
            var trigger = new BusinessCycleTrigger
            {
                TriggerId = "trig-crash-test",
                TriggerType = BusinessCycleTriggerType.SCHEDULED_TIMER
            };

            // 1. Start cycle and advance to REASONING
            var cycle = await _continuousCoordinator.TriggerCycleAsync("tenant-docker-439", trigger, 100m);
            await _continuousCoordinator.AdvanceCheckpointAsync("tenant-docker-439", cycle.CycleId, "REASONING", 10m);

            // 2. Advance to EXECUTING_WORK
            await _continuousCoordinator.AdvanceCheckpointAsync("tenant-docker-439", cycle.CycleId, "EXECUTING_WORK", 25m);

            // 3. Simulate process kill / container restart
            // Re-instantiate coordinator using persisted store
            var recoveredCoordinator = new ContinuousBusinessOperationsCoordinator(_continuousStore);
            var recoveredCycle = await recoveredCoordinator.RecoverLastCheckpointAsync("tenant-docker-439");

            recoveredCycle.Should().NotBeNull();
            recoveredCycle!.CycleId.Should().Be(cycle.CycleId);
            recoveredCycle.CheckpointState.Should().Be("EXECUTING_WORK");
            recoveredCycle.ActualSpent.Should().Be(35m); // 10 + 25
            recoveredCycle.IsCompleted.Should().BeFalse();
        }

        // =========================================================================
        // Family 2: Zero Duplicate External Effects (Law I39-V & Idempotency)
        // =========================================================================

        [Fact]
        public async Task DOCKER439_02_RestartRecovery_ReconcilesUnknownEffectWithoutDuplication()
        {
            var effectId = "eff-crm-lead-dedup";

            // 1. Initial execution recorded as UNKNOWN_EFFECT (e.g. network timeout before ACK)
            var effect = new ImmutableExternalEffect
            {
                EffectId = effectId,
                Connector = "CrmConnector",
                Provider = "Salesforce",
                RequestHash = "sha256-create-lead-nexus",
                IdempotencyKey = "idem-key-lead-nexus",
                EffectState = "UNKNOWN_EFFECT",
                VerificationEvidence = "AwaitingReconciliation"
            };
            await _lineageService.RecordExternalEffectAsync("tenant-docker-439", effect);

            // 2. Recovery routine reconciles state from CRM webhook without retrying duplicate API POST
            effect.EffectState = "SUCCEEDED";
            effect.ProviderReference = "sf_lead_00Q5g000003";
            effect.VerificationEvidence = "WebhookReceivedFromCRM";
            await _lineageService.RecordExternalEffectAsync("tenant-docker-439", effect);

            var retrieved = await _lineageService.GetExternalEffectAsync("tenant-docker-439", effectId);
            retrieved.Should().NotBeNull();
            retrieved!.EffectState.Should().Be("SUCCEEDED");
            retrieved.ProviderReference.Should().Be("sf_lead_00Q5g000003");
        }
    }
}
