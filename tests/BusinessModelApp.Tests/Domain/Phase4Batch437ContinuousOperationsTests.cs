using System;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch437ContinuousOperationsTests
    {
        private readonly InMemoryContinuousOperationsStore _store;
        private readonly ContinuousBusinessOperationsCoordinator _coordinator;

        public Phase4Batch437ContinuousOperationsTests()
        {
            _store = new InMemoryContinuousOperationsStore();
            _coordinator = new ContinuousBusinessOperationsCoordinator(_store);
        }

        // =========================================================================
        // Family 1: Continuous Bounded Cycles & Idempotency (Law I39-W)
        // =========================================================================

        [Fact]
        public async Task CONT437_01_TriggerCycle_CreatesBoundedFiniteCycle()
        {
            var trigger = new BusinessCycleTrigger
            {
                TriggerId = "trig-timer-01",
                TriggerType = BusinessCycleTriggerType.SCHEDULED_TIMER,
                SourcePayloadJson = "{\"intervalMinutes\":15}"
            };

            var cycle = await _coordinator.TriggerCycleAsync("tenant-cont-437", trigger, 150m);

            cycle.SequenceNumber.Should().Be(1);
            cycle.AllocatedBudget.Should().Be(150m);
            cycle.CheckpointState.Should().Be("INITIALIZED");
            cycle.IsCompleted.Should().BeFalse();
            cycle.IsTimedOut.Should().BeFalse();
            cycle.BrainSnapshotHash.Should().NotBeNullOrWhiteSpace();
            cycle.PolicySnapshotHash.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task CONT437_02_DuplicateTrigger_IsDeduplicatedIdempotently()
        {
            var trigger = new BusinessCycleTrigger
            {
                TriggerId = "trig-crm-lead-999",
                TriggerType = BusinessCycleTriggerType.INBOUND_LEAD,
                SourcePayloadJson = "{\"leadId\":\"lead-nexus-01\"}"
            };

            var cycle1 = await _coordinator.TriggerCycleAsync("tenant-cont-437", trigger, 100m);
            var cycle2 = await _coordinator.TriggerCycleAsync("tenant-cont-437", trigger, 100m);

            cycle1.CycleId.Should().Be(cycle2.CycleId); // Exactly same cycle returned
            cycle2.SequenceNumber.Should().Be(1);
        }

        // =========================================================================
        // Family 2: Checkpoint State Advancement & Budget Guard
        // =========================================================================

        [Fact]
        public async Task CONT437_03_AdvanceCheckpoint_EnforcesBudgetCap()
        {
            var trigger = new BusinessCycleTrigger
            {
                TriggerId = "trig-timer-02",
                TriggerType = BusinessCycleTriggerType.SCHEDULED_TIMER
            };
            var cycle = await _coordinator.TriggerCycleAsync("tenant-cont-437", trigger, 50m);

            // Step 1: Advance to REASONING (cost 20 <= 50) -> OK
            var ok1 = await _coordinator.AdvanceCheckpointAsync("tenant-cont-437", cycle.CycleId, "REASONING", 20m);
            ok1.Should().BeTrue();

            // Step 2: Advance to PROPOSING (cost 20 + 40 = 60 > 50 cap) -> Fails & marks cycle FAILED
            var ok2 = await _coordinator.AdvanceCheckpointAsync("tenant-cont-437", cycle.CycleId, "PROPOSING", 40m);
            ok2.Should().BeFalse();

            var updated = await _coordinator.GetCycleAsync("tenant-cont-437", cycle.CycleId);
            updated!.CheckpointState.Should().Be("FAILED");
            updated.OutcomeSummary.Should().Contain("budget exceeded");
        }

        // =========================================================================
        // Family 3: Crash Recovery & Checkpoint Resumption
        // =========================================================================

        [Fact]
        public async Task CONT437_04_RecoverLastCheckpoint_ReconstructsActiveState()
        {
            var trigger = new BusinessCycleTrigger
            {
                TriggerId = "trig-timer-03",
                TriggerType = BusinessCycleTriggerType.SCHEDULED_TIMER
            };
            var cycle = await _coordinator.TriggerCycleAsync("tenant-cont-437", trigger, 100m);
            await _coordinator.AdvanceCheckpointAsync("tenant-cont-437", cycle.CycleId, "EXECUTING_WORK", 15m);

            // Simulate process restart: Reconstruct from store
            var recovered = await _coordinator.RecoverLastCheckpointAsync("tenant-cont-437");

            recovered.Should().NotBeNull();
            recovered!.CycleId.Should().Be(cycle.CycleId);
            recovered.CheckpointState.Should().Be("EXECUTING_WORK");
            recovered.ActualSpent.Should().Be(15m);
        }
    }
}
