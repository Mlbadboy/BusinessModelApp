using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Domain.Governance;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;
using BusinessModelApp.Infrastructure.Execution;
using BusinessModelApp.Infrastructure.Runtime;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch30RuntimeKernelTests
    {
        private readonly Guid _tenantA = Guid.NewGuid();
        private readonly Guid _tenantB = Guid.NewGuid();

        // ====================================================================
        // A. IDENTITY CONTRACT TESTS
        // ====================================================================

        [Fact]
        public void Identity_ShouldRejectEmptyGuid()
        {
            Assert.Throws<ArgumentException>(() => new ResponsibilityId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new MissionGraphId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new MissionRunId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new RuntimeRunId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new AgentInstanceId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new ExecutionIntentId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new ExecutionAttemptId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new LeaseId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new CheckpointId(Guid.Empty));
            Assert.Throws<ArgumentException>(() => new RuntimeEventId(Guid.Empty));
        }

        [Fact]
        public void Identity_ShouldRejectInvalidStringIdentifiers()
        {
            Assert.Throws<ArgumentException>(() => new MissionNodeId("   "));
            Assert.Throws<ArgumentException>(() => new AgentDefinitionId("   "));
            Assert.Throws<ArgumentException>(() => new CapabilityId("", "v1"));
            Assert.Throws<ArgumentException>(() => new CapabilityId("crm.update", ""));
            Assert.Throws<ArgumentException>(() => CapabilityId.Parse("invalid-capability-string"));
        }

        [Fact]
        public void Identity_EqualityAndCanonicalFormatting()
        {
            var id1 = ResponsibilityId.New();
            var id2 = ResponsibilityId.From(id1.Value);
            Assert.Equal(id1, id2);

            var cap = CapabilityId.Parse("CRM.Lead-Update:V1");
            Assert.Equal("crm.lead-update", cap.Name);
            Assert.Equal("v1", cap.Version);
            Assert.Equal("crm.lead-update:v1", cap.ToString());
        }

        [Fact]
        public void Identity_FenceTokenMonotonicInvariants()
        {
            var t1 = new FenceToken(10);
            var t2 = t1.Next();

            Assert.Equal(11, t2.Value);
            Assert.True(t2 > t1);
            Assert.True(t1 < t2);
            Assert.Throws<ArgumentOutOfRangeException>(() => new FenceToken(-1));
        }

        // ====================================================================
        // B. STATE MACHINE VALIDATION TESTS
        // ====================================================================

        [Fact]
        public void StateTransitions_LegalTransitionsSucceed()
        {
            var validator = new RuntimeStateTransitionValidator();

            // Responsibility
            Assert.True(validator.CanTransition(ResponsibilityState.Draft, ResponsibilityState.Active));
            Assert.True(validator.CanTransition(ResponsibilityState.Active, ResponsibilityState.Evaluating));
            Assert.True(validator.CanTransition(ResponsibilityState.Evaluating, ResponsibilityState.Triggered));

            // Mission Graph
            Assert.True(validator.CanTransition(MissionGraphState.Initialized, MissionGraphState.Planning));
            Assert.True(validator.CanTransition(MissionGraphState.Planning, MissionGraphState.Active));
            Assert.True(validator.CanTransition(MissionGraphState.Active, MissionGraphState.Completed));

            // Run
            Assert.True(validator.CanTransition(RunState.Pending, RunState.Admitted));
            Assert.True(validator.CanTransition(RunState.Admitted, RunState.Running));
            Assert.True(validator.CanTransition(RunState.Running, RunState.Completed));

            // Attempt
            Assert.True(validator.CanTransition(AttemptState.Claimed, AttemptState.Executing));
            Assert.True(validator.CanTransition(AttemptState.Executing, AttemptState.Succeeded));
            Assert.True(validator.CanTransition(AttemptState.Executing, AttemptState.UnknownEffect));
        }

        [Fact]
        public void StateTransitions_IllegalTransitionsFailClosed()
        {
            var validator = new RuntimeStateTransitionValidator();

            Assert.False(validator.CanTransition(ResponsibilityState.Killed, ResponsibilityState.Active));
            Assert.Throws<InvalidRuntimeStateTransitionException>(() =>
                validator.AssertTransition(ResponsibilityState.Killed, ResponsibilityState.Active));

            Assert.False(validator.CanTransition(RunState.Completed, RunState.Running));
            Assert.Throws<InvalidRuntimeStateTransitionException>(() =>
                validator.AssertTransition(RunState.Completed, RunState.Running));

            Assert.False(validator.CanTransition(AgentInstanceState.Terminated, AgentInstanceState.Running));
            Assert.Throws<InvalidRuntimeStateTransitionException>(() =>
                validator.AssertTransition(AgentInstanceState.Terminated, AgentInstanceState.Running));
        }

        // ====================================================================
        // C. EVENT INTEGRITY & ENVELOPE HASHING TESTS
        // ====================================================================

        [Fact]
        public void EventEnvelope_GeneratesDeterministicSha256Hashes()
        {
            var evt = new RunStartedEvent(RuntimeRunId.New(), "worker-01")
            {
                WorkspaceId = _tenantA,
                CorrelationId = Guid.NewGuid(),
                CausationId = Guid.NewGuid()
            };

            var envelope = RuntimeEventEnvelope.Create(evt, "GENESIS", 1);

            Assert.NotNull(envelope.EventHash);
            Assert.NotNull(envelope.PayloadHash);
            Assert.Equal("GENESIS", envelope.PreviousEventHash);
            Assert.Equal(1, envelope.SequenceNumber);
            Assert.True(envelope.VerifyHashIntegrity("GENESIS"));
        }

        [Fact]
        public void EventEnvelope_TamperedPayloadBreaksHashIntegrity()
        {
            var evt = new RunStartedEvent(RuntimeRunId.New(), "worker-01")
            {
                WorkspaceId = _tenantA
            };

            var envelope = RuntimeEventEnvelope.Create(evt, "GENESIS", 1);

            // Tamper with payload JSON
            var tampered = new RuntimeEventEnvelope
            {
                EventId = envelope.EventId,
                EventType = envelope.EventType,
                OccurredAtUtc = envelope.OccurredAtUtc,
                RecordedAtUtc = envelope.RecordedAtUtc,
                CorrelationId = envelope.CorrelationId,
                CausationId = envelope.CausationId,
                WorkspaceId = envelope.WorkspaceId,
                ActorId = envelope.ActorId,
                SequenceNumber = envelope.SequenceNumber,
                PayloadJson = envelope.PayloadJson + "{\"tampered\": true}",
                PayloadHash = envelope.PayloadHash,
                PreviousEventHash = envelope.PreviousEventHash,
                EventHash = envelope.EventHash
            };

            Assert.False(tampered.VerifyHashIntegrity("GENESIS"));
        }

        [Fact]
        public void EventEnvelope_MismatchedPreviousHashBreaksChain()
        {
            var evt = new RunStartedEvent(RuntimeRunId.New(), "worker-01")
            {
                WorkspaceId = _tenantA
            };

            var envelope = RuntimeEventEnvelope.Create(evt, "PREVIOUS_HASH_001", 2);

            Assert.False(envelope.VerifyHashIntegrity("DIFFERENT_PREVIOUS_HASH"));
            Assert.True(envelope.VerifyHashIntegrity("PREVIOUS_HASH_001"));
        }

        // ====================================================================
        // D. AUTHORITATIVE EVENT STORE TESTS
        // ====================================================================

        [Fact]
        public async Task EventStore_AppendsMonotonicallyAndMaintainsHashChain()
        {
            var store = new InMemoryRuntimeEventStore();
            var runId = RuntimeRunId.New();

            var e1 = new RunCreatedEvent(runId, MissionGraphId.New()) { WorkspaceId = _tenantA };
            var e2 = new RunStartedEvent(runId, "worker-alpha") { WorkspaceId = _tenantA };
            var e3 = new RunCompletedEvent(runId, "Target DSO reached") { WorkspaceId = _tenantA };

            var env1 = await store.AppendEventAsync(e1, runId.Value);
            var env2 = await store.AppendEventAsync(e2, runId.Value);
            var env3 = await store.AppendEventAsync(e3, runId.Value);

            Assert.Equal(1, env1.SequenceNumber);
            Assert.Equal(2, env2.SequenceNumber);
            Assert.Equal(3, env3.SequenceNumber);

            Assert.Equal("GENESIS", env1.PreviousEventHash);
            Assert.Equal(env1.EventHash, env2.PreviousEventHash);
            Assert.Equal(env2.EventHash, env3.PreviousEventHash);

            bool isIntact = await store.VerifyStreamIntegrityAsync(runId.Value);
            Assert.True(isIntact);
        }

        [Fact]
        public async Task EventStore_ReadStreamsByCorrelationAndTenant()
        {
            var store = new InMemoryRuntimeEventStore();
            var runId = RuntimeRunId.New();
            var correlationId = Guid.NewGuid();

            var e1 = new RunCreatedEvent(runId, MissionGraphId.New())
            {
                WorkspaceId = _tenantA,
                CorrelationId = correlationId
            };
            var e2 = new RunCreatedEvent(RuntimeRunId.New(), MissionGraphId.New())
            {
                WorkspaceId = _tenantB,
                CorrelationId = Guid.NewGuid()
            };

            await store.AppendEventAsync(e1, runId.Value);
            await store.AppendEventAsync(e2, Guid.NewGuid());

            var corrStream = await store.ReadCorrelationStreamAsync(correlationId);
            Assert.Single(corrStream);
            Assert.Equal(correlationId, corrStream[0].CorrelationId);

            var tenantAStream = await store.ReadTenantStreamAsync(_tenantA);
            Assert.Single(tenantAStream);

            var tenantBStream = await store.ReadTenantStreamAsync(_tenantB);
            Assert.Single(tenantBStream);
        }

        // ====================================================================
        // E. EVENT BUS & IDEMPOTENT DELIVERY TESTS
        // ====================================================================

        [Fact]
        public async Task EventBus_DeduplicatesDuplicateEventDelivery()
        {
            var bus = new InMemoryRuntimeEventBus();
            int deliveryCount = 0;

            bus.Subscribe<RunStartedEvent>("SubscriberAlpha", (evt, env, ct) =>
            {
                deliveryCount++;
                return Task.CompletedTask;
            });

            var evt = new RunStartedEvent(RuntimeRunId.New(), "worker-01") { WorkspaceId = _tenantA };
            var envelope = RuntimeEventEnvelope.Create(evt, "GENESIS", 1);

            // Publish the same event 5 times
            for (int i = 0; i < 5; i++)
            {
                await bus.PublishAsync(envelope);
            }

            // Must only be delivered ONCE to SubscriberAlpha
            Assert.Equal(1, deliveryCount);
            Assert.Equal(1, bus.GetProcessedEventCount("SubscriberAlpha"));
        }

        [Fact]
        public async Task EventBus_IndependentSubscribersBothReceiveEventOnce()
        {
            var bus = new InMemoryRuntimeEventBus();
            int sub1Count = 0;
            int sub2Count = 0;

            bus.Subscribe<RunStartedEvent>("Sub1", (evt, env, ct) =>
            {
                sub1Count++;
                return Task.CompletedTask;
            });

            bus.Subscribe<RunStartedEvent>("Sub2", (evt, env, ct) =>
            {
                sub2Count++;
                return Task.CompletedTask;
            });

            var evt = new RunStartedEvent(RuntimeRunId.New(), "worker-01") { WorkspaceId = _tenantA };
            var envelope = RuntimeEventEnvelope.Create(evt, "GENESIS", 1);

            await bus.PublishAsync(envelope);
            await bus.PublishAsync(envelope); // duplicate

            Assert.Equal(1, sub1Count);
            Assert.Equal(1, sub2Count);
        }

        // ====================================================================
        // F. LEASE MANAGER & FENCING PROTOCOL TESTS
        // ====================================================================

        [Fact]
        public async Task LeaseManager_AcquiresInitialLeaseWithFenceToken()
        {
            var leaseManager = new InMemoryRuntimeLeaseManager();
            var runId = RuntimeRunId.New();
            var attemptId = ExecutionAttemptId.New();

            var result = await leaseManager.AcquireLeaseAsync(runId, attemptId, "worker-A", _tenantA, TimeSpan.FromMinutes(1));

            Assert.Equal(LeaseStatus.Acquired, result.Status);
            Assert.NotNull(result.Lease);
            Assert.Equal(FenceToken.Initial, result.Token);
            Assert.Equal("worker-A", result.Lease.OwnerId);
        }

        [Fact]
        public async Task LeaseManager_ActiveLeaseBlocksDifferentWorkerWithConflict()
        {
            var leaseManager = new InMemoryRuntimeLeaseManager();
            var runId = RuntimeRunId.New();

            await leaseManager.AcquireLeaseAsync(runId, ExecutionAttemptId.New(), "worker-A", _tenantA, TimeSpan.FromMinutes(5));

            // Worker B tries to acquire active lease
            var conflictResult = await leaseManager.AcquireLeaseAsync(runId, ExecutionAttemptId.New(), "worker-B", _tenantA, TimeSpan.FromMinutes(1));

            Assert.Equal(LeaseStatus.Conflict, conflictResult.Status);
            Assert.Null(conflictResult.Lease);
        }

        [Fact]
        public async Task LeaseManager_ExpiredLeaseIncrementsFenceTokenAndFencesStaleWorker()
        {
            var leaseManager = new InMemoryRuntimeLeaseManager();
            var runId = RuntimeRunId.New();
            var attempt1 = ExecutionAttemptId.New();

            // Worker A gets lease with negative duration (already expired)
            var leaseA = await leaseManager.AcquireLeaseAsync(runId, attempt1, "worker-A", _tenantA, TimeSpan.FromMilliseconds(-100));
            var tokenA = leaseA.Token;

            // Worker B takes over expired run
            var leaseB = await leaseManager.AcquireLeaseAsync(runId, ExecutionAttemptId.New(), "worker-B", _tenantA, TimeSpan.FromMinutes(5));
            var tokenB = leaseB.Token;

            Assert.True(tokenB > tokenA);

            // Worker A wakes up and attempts to renew or validate with stale tokenA
            var renewResult = await leaseManager.RenewLeaseAsync(runId, leaseA.Lease!.LeaseId, tokenA, TimeSpan.FromMinutes(1));
            Assert.Equal(LeaseStatus.Fenced, renewResult.Status);

            bool isValid = await leaseManager.ValidateFenceTokenAsync(runId, tokenA);
            Assert.False(isValid);

            bool isBValid = await leaseManager.ValidateFenceTokenAsync(runId, tokenB);
            Assert.True(isBValid);
        }

        // ====================================================================
        // G. RUNTIME REPLAY ENGINE TESTS
        // ====================================================================

        [Fact]
        public async Task ReplayEngine_DeterministicReplayReconstructsExactRunState()
        {
            var store = new InMemoryRuntimeEventStore();
            var replayEngine = new InMemoryRuntimeReplayEngine(store);
            var runId = RuntimeRunId.New();

            await store.AppendEventAsync(new RunCreatedEvent(runId, MissionGraphId.New()) { WorkspaceId = _tenantA }, runId.Value);
            await store.AppendEventAsync(new RuntimeAdmissionGrantedEvent(runId, _tenantA, 5) { WorkspaceId = _tenantA }, runId.Value);
            await store.AppendEventAsync(new RunStartedEvent(runId, "worker-01") { WorkspaceId = _tenantA }, runId.Value);
            await store.AppendEventAsync(new RunCompletedEvent(runId, "Mission successful") { WorkspaceId = _tenantA }, runId.Value);

            var replay = await replayEngine.ReplayAsync(runId);

            Assert.True(replay.IsSuccessful);
            Assert.Equal(4, replay.EventsReplayedCount);
            Assert.Equal(RunState.Completed, replay.ReconstructedState);
        }

        [Fact]
        public async Task ReplayEngine_ReplayFromSequenceReconstructsIntermediateState()
        {
            var store = new InMemoryRuntimeEventStore();
            var replayEngine = new InMemoryRuntimeReplayEngine(store);
            var runId = RuntimeRunId.New();

            await store.AppendEventAsync(new RunCreatedEvent(runId, MissionGraphId.New()) { WorkspaceId = _tenantA }, runId.Value);
            await store.AppendEventAsync(new RunStartedEvent(runId, "worker-01") { WorkspaceId = _tenantA }, runId.Value);
            await store.AppendEventAsync(new RunPausedEvent(runId, "Awaiting approval") { WorkspaceId = _tenantA }, runId.Value);

            // Replay starting from sequence 2
            var replay = await replayEngine.ReplayFromSequenceAsync(runId, 2);

            Assert.True(replay.IsSuccessful);
            Assert.Equal(2, replay.EventsReplayedCount);
            Assert.Equal(RunState.Suspended, replay.ReconstructedState);
        }

        [Fact]
        public async Task ReplayEngine_SimulationReplayProvesZeroExternalSideEffects()
        {
            var store = new InMemoryRuntimeEventStore();
            var replayEngine = new InMemoryRuntimeReplayEngine(store);
            var runId = RuntimeRunId.New();

            await store.AppendEventAsync(new RunCreatedEvent(runId, MissionGraphId.New()) { WorkspaceId = _tenantA }, runId.Value);
            await store.AppendEventAsync(new RunStartedEvent(runId, "worker-01") { WorkspaceId = _tenantA }, runId.Value);

            var replay = await replayEngine.ReplaySimulationAsync(runId, "alternate-model-gpt4o");

            Assert.True(replay.IsSuccessful);
            Assert.True(replay.IsSimulationOnly);
            Assert.Empty(replay.InvariantViolations);
        }

        // ====================================================================
        // H. UNKNOWN EFFECT HANDLING TESTS
        // ====================================================================

        [Fact]
        public void UnknownEffect_TimeoutMarksOutcomeUnknownAndBlocksBlindRetry()
        {
            var outcome = ExecutionOutcome.TimedOut("Payment gateway socket timeout after 10000ms");

            Assert.Equal(ExecutionOutcomeStatus.TimedOut, outcome.Status);
            Assert.True(outcome.IsUnknownEffect);
            Assert.Contains("UNKNOWN_EFFECT", outcome.VerificationEvidence);

            // Attempt state transition
            var validator = new RuntimeStateTransitionValidator();
            Assert.True(validator.CanTransition(AttemptState.Executing, AttemptState.UnknownEffect));
            Assert.True(validator.CanTransition(AttemptState.UnknownEffect, AttemptState.Succeeded)); // After manual reconciliation
            Assert.True(validator.CanTransition(AttemptState.UnknownEffect, AttemptState.Failed));    // After manual reconciliation
        }

        // ====================================================================
        // I. ENTERPRISE SCHEDULER TESTS
        // ====================================================================

        [Fact]
        public async Task Scheduler_EnforcesPerTenantConcurrencyQuotas()
        {
            var scheduler = new EnterpriseSchedulerEngine();
            var jobs = new List<ScheduledJobId>();

            // Schedule up to MaxConcurrentJobsPerTenant (5)
            for (int i = 0; i < EnterpriseSchedulerEngine.MaxConcurrentJobsPerTenant; i++)
            {
                var id = await scheduler.ScheduleAsync(new ScheduledJobRequest(_tenantA, $"Job-{i}", async ct =>
                {
                    await Task.Delay(500, ct);
                }));
                jobs.Add(id);
            }

            // 6th job for same tenant should be rejected due to concurrency limit
            var excessJob = await scheduler.ScheduleAsync(new ScheduledJobRequest(_tenantA, "Job-Excess", async ct =>
            {
                await Task.Delay(100, ct);
            }));

            // Small delay to let execution attempt run
            await Task.Delay(50);

            var status = await scheduler.GetStatusAsync(excessJob, _tenantA);
            Assert.NotNull(status);
            Assert.Equal(ScheduledJobStatus.Failed, status.Status);
            Assert.Contains("concurrency limit", status.FailureReason);
        }

        [Fact]
        public async Task Scheduler_DeduplicatesJobByIdempotencyKey()
        {
            var scheduler = new EnterpriseSchedulerEngine();
            string idempotencyKey = "DAILY-DSO-SCAN-2026-09-06";

            var req1 = new ScheduledJobRequest(_tenantA, "ScanDSO")
            {
                IdempotencyKey = idempotencyKey
            };
            var req2 = new ScheduledJobRequest(_tenantA, "ScanDSO-Duplicate")
            {
                IdempotencyKey = idempotencyKey
            };

            var id1 = await scheduler.ScheduleAsync(req1);
            var id2 = await scheduler.ScheduleAsync(req2);

            Assert.Equal(id1, id2);
        }

        [Fact]
        public async Task Scheduler_CrossTenantCancellationThrowsException()
        {
            var scheduler = new EnterpriseSchedulerEngine();
            var jobId = await scheduler.ScheduleAsync(new ScheduledJobRequest(_tenantA, "TenantA-Task"));

            // Tenant B attempts to cancel Tenant A's job
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await scheduler.CancelAsync(jobId, _tenantB, "Unauthorized cancellation attempt");
            });
        }

        // ====================================================================
        // J. RUNTIME ADMISSION CONTROLLER TESTS
        // ====================================================================

        [Fact]
        public async Task AdmissionController_AdmitsValidRequestWithinCeiling()
        {
            var controller = new RuntimeAdmissionController();
            var runId = RuntimeRunId.New();
            var graphId = MissionGraphId.New();

            var req = new AdmissionRequest(runId, _tenantA, graphId)
            {
                RequestedBudget = new RunResourceBudget { MaxFinancialExposure = 500.00m }
            };

            var decision = await controller.EvaluateAdmissionAsync(req);

            Assert.True(decision.IsAdmitted);
            Assert.Null(decision.RejectionReason);
        }

        [Fact]
        public async Task AdmissionController_RejectsFinancialExposureAboveCeiling()
        {
            var controller = new RuntimeAdmissionController();
            var req = new AdmissionRequest(RuntimeRunId.New(), _tenantA, MissionGraphId.New())
            {
                RequestedBudget = new RunResourceBudget { MaxFinancialExposure = 50_000.00m } // Above $10,000 ceiling
            };

            var decision = await controller.EvaluateAdmissionAsync(req);

            Assert.False(decision.IsAdmitted);
            Assert.Contains("exceeds tenant ceiling", decision.RejectionReason);
        }

        [Fact]
        public async Task AdmissionController_RejectsEmptyWorkspaceId()
        {
            var controller = new RuntimeAdmissionController();
            var req = new AdmissionRequest(RuntimeRunId.New(), Guid.NewGuid(), MissionGraphId.New())
            {
                WorkspaceId = Guid.Empty
            };

            var decision = await controller.EvaluateAdmissionAsync(req);

            Assert.False(decision.IsAdmitted);
            Assert.Contains("Missing or empty WorkspaceId", decision.RejectionReason);
        }

        // ====================================================================
        // K. CAPABILITY RESOLVER TESTS
        // ====================================================================

        [Fact]
        public async Task CapabilityResolver_ResolvesRegisteredActiveCapability()
        {
            var resolver = new RuntimeCapabilityResolver();
            var req = new CapabilityRequest(new CapabilityId("crm.lead-update", "v1"), _tenantA)
            {
                RequiredTrustTier = 0.8,
                MaxPermittedRiskTier = 2
            };

            var result = await resolver.ResolveCapabilityAsync(req);

            Assert.True(result.IsResolved);
            Assert.Equal(CapabilityState.Active, result.State);
        }

        [Fact]
        public async Task CapabilityResolver_RejectsUnregisteredCapability()
        {
            var resolver = new RuntimeCapabilityResolver();
            var req = new CapabilityRequest(new CapabilityId("nonexistent.tool", "v1"), _tenantA);

            var result = await resolver.ResolveCapabilityAsync(req);

            Assert.False(result.IsResolved);
            Assert.Contains("not registered", result.FailureReason);
        }

        [Fact]
        public async Task CapabilityResolver_RejectsInsufficientTrustTier()
        {
            var resolver = new RuntimeCapabilityResolver();
            var req = new CapabilityRequest(new CapabilityId("crm.lead-update", "v1"), _tenantA)
            {
                RequiredTrustTier = 0.4 // Minimum required is 0.7
            };

            var result = await resolver.ResolveCapabilityAsync(req);

            Assert.False(result.IsResolved);
            Assert.Contains("below required minimum", result.FailureReason);
        }

        [Fact]
        public async Task CapabilityResolver_RejectsRiskTierAbovePermittedCeiling()
        {
            var resolver = new RuntimeCapabilityResolver();
            var req = new CapabilityRequest(new CapabilityId("crm.lead-update", "v1"), _tenantA)
            {
                RequiredTrustTier = 0.9,
                MaxPermittedRiskTier = 1 // Capability requires R2
            };

            var result = await resolver.ResolveCapabilityAsync(req);

            Assert.False(result.IsResolved);
            Assert.Contains("exceeds requester permitted ceiling", result.FailureReason);
        }

        // ====================================================================
        // L. ZERO-TRUST TENANT ISOLATION GUARD TESTS
        // ====================================================================

        [Fact]
        public void TenantIsolation_MatchingTenantPasses()
        {
            var guard = new Phase3TenantIsolationGuard();
            guard.AssertTenantAccess(_tenantA, _tenantA, "RuntimeRun");
            Assert.True(guard.IsAccessible(_tenantA, _tenantA));
        }

        [Fact]
        public void TenantIsolation_CrossTenantAccessThrowsViolationException()
        {
            var guard = new Phase3TenantIsolationGuard();

            Assert.Throws<Phase3TenantIsolationViolationException>(() =>
                guard.AssertTenantAccess(_tenantA, _tenantB, "RuntimeRun-Secret"));

            Assert.False(guard.IsAccessible(_tenantA, _tenantB));
        }

        [Fact]
        public void TenantIsolation_EmptyTenantThrowsArgumentException()
        {
            var guard = new Phase3TenantIsolationGuard();

            Assert.Throws<ArgumentException>(() =>
                guard.AssertTenantAccess(Guid.Empty, _tenantA, "Resource"));

            Assert.Throws<ArgumentException>(() =>
                guard.AssertTenantAccess(_tenantA, Guid.Empty, "Resource"));
        }

        // ====================================================================
        // M. MISSION GRAPH VALIDATION (DAG INVARIANTS)
        // ====================================================================

        [Fact]
        public void MissionGraph_AcyclicGraphPassesValidation()
        {
            var graphId = MissionGraphId.New();
            var graph = new MissionGraph(graphId, _tenantA, "Recover Receivables");

            var n1 = new MissionNode(MissionNodeId.From("N1-Perceive"), graphId, "Perceive Overdue", MissionNodeType.Perception);
            var n2 = new MissionNode(MissionNodeId.From("N2-Analyze"), graphId, "Analyze Invoices", MissionNodeType.Analysis);
            var n3 = new MissionNode(MissionNodeId.From("N3-Verify"), graphId, "Verify Totals", MissionNodeType.Verification);

            graph.AddNode(n1);
            graph.AddNode(n2);
            graph.AddNode(n3);

            graph.AddEdge(n1.NodeId, n2.NodeId);
            graph.AddEdge(n2.NodeId, n3.NodeId);

            MissionGraphValidator.AssertValid(graph);
        }

        [Fact]
        public void MissionGraph_CyclicGraphFailsValidation()
        {
            var graphId = MissionGraphId.New();
            var graph = new MissionGraph(graphId, _tenantA, "Cyclic Loop");

            var n1 = new MissionNode(MissionNodeId.From("A"), graphId, "A", MissionNodeType.Analysis);
            var n2 = new MissionNode(MissionNodeId.From("B"), graphId, "B", MissionNodeType.Analysis);
            var n3 = new MissionNode(MissionNodeId.From("C"), graphId, "C", MissionNodeType.Analysis);

            graph.AddNode(n1);
            graph.AddNode(n2);
            graph.AddNode(n3);

            // Create cycle: A -> B -> C -> A
            graph.AddEdge(n1.NodeId, n2.NodeId);
            graph.AddEdge(n2.NodeId, n3.NodeId);
            graph.AddEdge(n3.NodeId, n1.NodeId);

            var ex = Assert.Throws<InvalidMissionGraphException>(() => MissionGraphValidator.AssertValid(graph));
            Assert.Contains("cycles", ex.Message);
        }

        [Fact]
        public void MissionGraph_ExecutionNodeWithoutCapabilityFailsValidation()
        {
            var graphId = MissionGraphId.New();
            var graph = new MissionGraph(graphId, _tenantA, "Execute Without Tool");

            var n1 = new MissionNode(MissionNodeId.From("Exec1"), graphId, "Send Letter", MissionNodeType.Execution)
            {
                RequiredCapabilityId = null // Invalid!
            };

            graph.AddNode(n1);

            var ex = Assert.Throws<InvalidMissionGraphException>(() => MissionGraphValidator.AssertValid(graph));
            Assert.Contains("must declare a required CapabilityId", ex.Message);
        }

        // ====================================================================
        // N. RUNTIME AUDIT LEDGER TESTS
        // ====================================================================

        [Fact]
        public async Task RuntimeAuditStore_AppendsAndMaintainsImmutableHistory()
        {
            var auditStore = new RuntimeAuditStore();
            var runId = RuntimeRunId.New();

            var record1 = RuntimeAuditRecord.Create(_tenantA, runId, null, "RunCreated", "None", "Pending", "POL-01", Guid.NewGuid(), Guid.NewGuid(), "actor-1");
            var record2 = RuntimeAuditRecord.Create(_tenantA, runId, ExecutionAttemptId.New(), "AttemptClaimed", "Pending", "Running", "POL-01", Guid.NewGuid(), Guid.NewGuid(), "actor-1");

            await auditStore.RecordAuditAsync(record1);
            await auditStore.RecordAuditAsync(record2);

            var history = await auditStore.GetAuditHistoryAsync(runId);
            Assert.Equal(2, history.Count);
            Assert.NotEmpty(history[0].RecordHash);
            Assert.NotEmpty(history[1].RecordHash);
        }

        // ====================================================================
        // O. BUSINESS CONSTITUTION TESTS
        // ====================================================================

        [Fact]
        public void BusinessConstitution_RequiresHumanApprovalForHighSpendOrRisk()
        {
            var constitution = new BusinessConstitution
            {
                WorkspaceId = _tenantA,
                MaxAutonomousSpendPerAction = 50.00m,
                MaxPermittedAutonomousRiskTier = 1,
                DefaultControlMode = BusinessControlMode.EXECUTE_BOUNDED
            };

            // Low spend, low risk in bounded mode: autonomous
            Assert.False(constitution.RequiresHumanApproval(25.00m, 1, BusinessControlMode.EXECUTE_BOUNDED));

            // High spend: requires approval
            Assert.True(constitution.RequiresHumanApproval(100.00m, 1, BusinessControlMode.EXECUTE_BOUNDED));

            // High risk (R3): requires approval
            Assert.True(constitution.RequiresHumanApproval(10.00m, 3, BusinessControlMode.EXECUTE_BOUNDED));

            // EXECUTE_WITH_APPROVAL mode: always requires approval
            Assert.True(constitution.RequiresHumanApproval(5.00m, 0, BusinessControlMode.EXECUTE_WITH_APPROVAL));

            // SIMULATE mode: zero side-effects, does not require human approval
            Assert.False(constitution.RequiresHumanApproval(1000.00m, 5, BusinessControlMode.SIMULATE));
        }

        // ====================================================================
        // P. BATCH 6 REGRESSION WALL VERIFICATION
        // ====================================================================

        [Fact]
        public void Batch6RegressionWall_ExecutionFirewallAndConstitutionRemainOperational()
        {
            // Verify Batch 6 sovereign execution constitution is strictly intact
            Assert.False(string.IsNullOrWhiteSpace(ExecutionConstitution.LAW));
            Assert.Contains("Execution Firewall", ExecutionConstitution.LAW);

            // Invariant: empty workspace fails closed with SecurityException
            Assert.Throws<System.Security.SecurityException>(() =>
                ExecutionConstitution.AssertConstitutionalValidity(
                    Guid.Empty, "agent-1", "email.send", "idem-01", "audit"));

            // Verify Batch 6 deterministic risk engine operates as expected
            var riskEngine = new DeterministicRiskEngine();
            var readReq = new ExecutionRequest
            {
                CapabilityId = "analytics.read",
                ActionTier = ExecutionActionTier.L2_Recommend,
                MonetaryImpactINR = 0m
            };
            var readRisk = riskEngine.EvaluateRisk(readReq);
            Assert.Equal(ExecutionRiskTier.R0_ReadData, readRisk);

            var deleteReq = new ExecutionRequest
            {
                CapabilityId = "database.delete",
                ActionTier = ExecutionActionTier.L5_ConsequentialAction,
                MonetaryImpactINR = 100_000m
            };
            var deleteRisk = riskEngine.EvaluateRisk(deleteReq);
            Assert.Equal(ExecutionRiskTier.R5_DestructiveOrHighImpact, deleteRisk);
        }
    }
}
