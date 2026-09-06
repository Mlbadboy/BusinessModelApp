using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public class InvalidRuntimeStateTransitionException : Exception
    {
        public string EntityType { get; }
        public string FromState { get; }
        public string ToState { get; }

        public InvalidRuntimeStateTransitionException(string entityType, string fromState, string toState, string? message = null)
            : base(message ?? $"Illegal state transition for {entityType}: '{fromState}' -> '{toState}' is not permitted by runtime invariants.")
        {
            EntityType = entityType;
            FromState = fromState;
            ToState = toState;
        }
    }

    public interface IRuntimeStateTransitionValidator
    {
        bool CanTransition(ResponsibilityState from, ResponsibilityState to);
        void AssertTransition(ResponsibilityState from, ResponsibilityState to);

        bool CanTransition(MissionGraphState from, MissionGraphState to);
        void AssertTransition(MissionGraphState from, MissionGraphState to);

        bool CanTransition(MissionNodeState from, MissionNodeState to);
        void AssertTransition(MissionNodeState from, MissionNodeState to);

        bool CanTransition(RunState from, RunState to);
        void AssertTransition(RunState from, RunState to);

        bool CanTransition(AttemptState from, AttemptState to);
        void AssertTransition(AttemptState from, AttemptState to);

        bool CanTransition(AgentInstanceState from, AgentInstanceState to);
        void AssertTransition(AgentInstanceState from, AgentInstanceState to);

        bool CanTransition(CapabilityState from, CapabilityState to);
        void AssertTransition(CapabilityState from, CapabilityState to);
    }

    public class RuntimeStateTransitionValidator : IRuntimeStateTransitionValidator
    {
        private static readonly HashSet<(ResponsibilityState From, ResponsibilityState To)> AllowedResponsibilityTransitions = new()
        {
            (ResponsibilityState.Draft, ResponsibilityState.Active),
            (ResponsibilityState.Draft, ResponsibilityState.Retired),
            (ResponsibilityState.Draft, ResponsibilityState.Killed),

            (ResponsibilityState.Active, ResponsibilityState.Evaluating),
            (ResponsibilityState.Active, ResponsibilityState.Paused),
            (ResponsibilityState.Active, ResponsibilityState.Retired),
            (ResponsibilityState.Active, ResponsibilityState.Killed),

            (ResponsibilityState.Evaluating, ResponsibilityState.Active),
            (ResponsibilityState.Evaluating, ResponsibilityState.Triggered),
            (ResponsibilityState.Evaluating, ResponsibilityState.AtRisk),
            (ResponsibilityState.Evaluating, ResponsibilityState.Paused),
            (ResponsibilityState.Evaluating, ResponsibilityState.Killed),

            (ResponsibilityState.Triggered, ResponsibilityState.Active),
            (ResponsibilityState.Triggered, ResponsibilityState.Escalated),
            (ResponsibilityState.Triggered, ResponsibilityState.Paused),
            (ResponsibilityState.Triggered, ResponsibilityState.Killed),

            (ResponsibilityState.AtRisk, ResponsibilityState.Active),
            (ResponsibilityState.AtRisk, ResponsibilityState.Triggered),
            (ResponsibilityState.AtRisk, ResponsibilityState.Escalated),
            (ResponsibilityState.AtRisk, ResponsibilityState.Paused),
            (ResponsibilityState.AtRisk, ResponsibilityState.Killed),

            (ResponsibilityState.Escalated, ResponsibilityState.Active),
            (ResponsibilityState.Escalated, ResponsibilityState.Paused),
            (ResponsibilityState.Escalated, ResponsibilityState.Retired),
            (ResponsibilityState.Escalated, ResponsibilityState.Killed),

            (ResponsibilityState.Paused, ResponsibilityState.Active),
            (ResponsibilityState.Paused, ResponsibilityState.Retired),
            (ResponsibilityState.Paused, ResponsibilityState.Killed),
        };

        private static readonly HashSet<(MissionGraphState From, MissionGraphState To)> AllowedMissionGraphTransitions = new()
        {
            (MissionGraphState.Initialized, MissionGraphState.Planning),
            (MissionGraphState.Initialized, MissionGraphState.Cancelled),
            (MissionGraphState.Initialized, MissionGraphState.Killed),

            (MissionGraphState.Planning, MissionGraphState.Active),
            (MissionGraphState.Planning, MissionGraphState.Failed),
            (MissionGraphState.Planning, MissionGraphState.Cancelled),
            (MissionGraphState.Planning, MissionGraphState.Killed),

            (MissionGraphState.Active, MissionGraphState.WaitingHuman),
            (MissionGraphState.Active, MissionGraphState.Verifying),
            (MissionGraphState.Active, MissionGraphState.Compensating),
            (MissionGraphState.Active, MissionGraphState.Completed),
            (MissionGraphState.Active, MissionGraphState.Failed),
            (MissionGraphState.Active, MissionGraphState.Cancelled),
            (MissionGraphState.Active, MissionGraphState.Killed),

            (MissionGraphState.WaitingHuman, MissionGraphState.Active),
            (MissionGraphState.WaitingHuman, MissionGraphState.Cancelled),
            (MissionGraphState.WaitingHuman, MissionGraphState.Killed),

            (MissionGraphState.Verifying, MissionGraphState.Completed),
            (MissionGraphState.Verifying, MissionGraphState.Compensating),
            (MissionGraphState.Verifying, MissionGraphState.Failed),
            (MissionGraphState.Verifying, MissionGraphState.Active),
            (MissionGraphState.Verifying, MissionGraphState.Killed),

            (MissionGraphState.Compensating, MissionGraphState.Failed),
            (MissionGraphState.Compensating, MissionGraphState.Cancelled),
            (MissionGraphState.Compensating, MissionGraphState.Killed)
        };

        private static readonly HashSet<(MissionNodeState From, MissionNodeState To)> AllowedMissionNodeTransitions = new()
        {
            (MissionNodeState.Pending, MissionNodeState.Ready),
            (MissionNodeState.Pending, MissionNodeState.Skipped),
            (MissionNodeState.Pending, MissionNodeState.Cancelled),
            (MissionNodeState.Pending, MissionNodeState.Killed),

            (MissionNodeState.Ready, MissionNodeState.Running),
            (MissionNodeState.Ready, MissionNodeState.Blocked),
            (MissionNodeState.Ready, MissionNodeState.Cancelled),
            (MissionNodeState.Ready, MissionNodeState.Killed),

            (MissionNodeState.Running, MissionNodeState.Waiting),
            (MissionNodeState.Running, MissionNodeState.AwaitingHuman),
            (MissionNodeState.Running, MissionNodeState.Verifying),
            (MissionNodeState.Running, MissionNodeState.Succeeded),
            (MissionNodeState.Running, MissionNodeState.Failed),
            (MissionNodeState.Running, MissionNodeState.Cancelled),
            (MissionNodeState.Running, MissionNodeState.Killed),

            (MissionNodeState.Waiting, MissionNodeState.Running),
            (MissionNodeState.Waiting, MissionNodeState.Failed),
            (MissionNodeState.Waiting, MissionNodeState.Cancelled),
            (MissionNodeState.Waiting, MissionNodeState.Killed),

            (MissionNodeState.Blocked, MissionNodeState.Ready),
            (MissionNodeState.Blocked, MissionNodeState.Failed),
            (MissionNodeState.Blocked, MissionNodeState.Cancelled),
            (MissionNodeState.Blocked, MissionNodeState.Killed),

            (MissionNodeState.AwaitingHuman, MissionNodeState.Running),
            (MissionNodeState.AwaitingHuman, MissionNodeState.Cancelled),
            (MissionNodeState.AwaitingHuman, MissionNodeState.Killed),

            (MissionNodeState.Verifying, MissionNodeState.Succeeded),
            (MissionNodeState.Verifying, MissionNodeState.Failed),
            (MissionNodeState.Verifying, MissionNodeState.Killed)
        };

        private static readonly HashSet<(RunState From, RunState To)> AllowedRunTransitions = new()
        {
            (RunState.Pending, RunState.Admitted),
            (RunState.Pending, RunState.Cancelled),
            (RunState.Pending, RunState.Killed),

            (RunState.Admitted, RunState.Running),
            (RunState.Admitted, RunState.Cancelled),
            (RunState.Admitted, RunState.Killed),

            (RunState.Running, RunState.Suspended),
            (RunState.Running, RunState.AwaitingHuman),
            (RunState.Running, RunState.Reconciling),
            (RunState.Running, RunState.Completed),
            (RunState.Running, RunState.Failed),
            (RunState.Running, RunState.Cancelled),
            (RunState.Running, RunState.Killed),

            (RunState.Suspended, RunState.Running),
            (RunState.Suspended, RunState.Cancelled),
            (RunState.Suspended, RunState.Killed),

            (RunState.AwaitingHuman, RunState.Running),
            (RunState.AwaitingHuman, RunState.Cancelled),
            (RunState.AwaitingHuman, RunState.Killed),

            (RunState.Reconciling, RunState.Completed),
            (RunState.Reconciling, RunState.Failed),
            (RunState.Reconciling, RunState.Killed)
        };

        private static readonly HashSet<(AttemptState From, AttemptState To)> AllowedAttemptTransitions = new()
        {
            (AttemptState.Claimed, AttemptState.Executing),
            (AttemptState.Claimed, AttemptState.AbortedByFence),
            (AttemptState.Claimed, AttemptState.Failed),

            (AttemptState.Executing, AttemptState.Verifying),
            (AttemptState.Executing, AttemptState.Succeeded),
            (AttemptState.Executing, AttemptState.Failed),
            (AttemptState.Executing, AttemptState.TimedOut),
            (AttemptState.Executing, AttemptState.UnknownEffect),
            (AttemptState.Executing, AttemptState.AbortedByFence),

            (AttemptState.Verifying, AttemptState.Succeeded),
            (AttemptState.Verifying, AttemptState.Failed),
            (AttemptState.Verifying, AttemptState.UnknownEffect),
            (AttemptState.Verifying, AttemptState.AbortedByFence),

            (AttemptState.UnknownEffect, AttemptState.Succeeded),
            (AttemptState.UnknownEffect, AttemptState.Failed)
        };

        private static readonly HashSet<(AgentInstanceState From, AgentInstanceState To)> AllowedAgentInstanceTransitions = new()
        {
            (AgentInstanceState.Created, AgentInstanceState.Ready),
            (AgentInstanceState.Created, AgentInstanceState.Terminated),
            (AgentInstanceState.Created, AgentInstanceState.Failed),

            (AgentInstanceState.Ready, AgentInstanceState.Running),
            (AgentInstanceState.Ready, AgentInstanceState.Suspended),
            (AgentInstanceState.Ready, AgentInstanceState.Terminated),
            (AgentInstanceState.Ready, AgentInstanceState.Retired),

            (AgentInstanceState.Running, AgentInstanceState.Waiting),
            (AgentInstanceState.Running, AgentInstanceState.Blocked),
            (AgentInstanceState.Running, AgentInstanceState.AwaitingHuman),
            (AgentInstanceState.Running, AgentInstanceState.Completed),
            (AgentInstanceState.Running, AgentInstanceState.Suspended),
            (AgentInstanceState.Running, AgentInstanceState.Failed),
            (AgentInstanceState.Running, AgentInstanceState.Terminated),

            (AgentInstanceState.Waiting, AgentInstanceState.Running),
            (AgentInstanceState.Waiting, AgentInstanceState.Failed),
            (AgentInstanceState.Waiting, AgentInstanceState.Terminated),

            (AgentInstanceState.Blocked, AgentInstanceState.Running),
            (AgentInstanceState.Blocked, AgentInstanceState.Failed),
            (AgentInstanceState.Blocked, AgentInstanceState.Terminated),

            (AgentInstanceState.AwaitingHuman, AgentInstanceState.Running),
            (AgentInstanceState.AwaitingHuman, AgentInstanceState.Terminated),

            (AgentInstanceState.Recovering, AgentInstanceState.Ready),
            (AgentInstanceState.Recovering, AgentInstanceState.Failed),
            (AgentInstanceState.Recovering, AgentInstanceState.Terminated),

            (AgentInstanceState.Suspended, AgentInstanceState.Ready),
            (AgentInstanceState.Suspended, AgentInstanceState.Terminated),

            (AgentInstanceState.Completed, AgentInstanceState.Retired),
            (AgentInstanceState.Failed, AgentInstanceState.Recovering),
            (AgentInstanceState.Failed, AgentInstanceState.Terminated)
        };

        private static readonly HashSet<(CapabilityState From, CapabilityState To)> AllowedCapabilityTransitions = new()
        {
            (CapabilityState.Registered, CapabilityState.Sandboxed),
            (CapabilityState.Registered, CapabilityState.Active),
            (CapabilityState.Registered, CapabilityState.Revoked),
            (CapabilityState.Registered, CapabilityState.Killed),

            (CapabilityState.Sandboxed, CapabilityState.Active),
            (CapabilityState.Sandboxed, CapabilityState.Revoked),
            (CapabilityState.Sandboxed, CapabilityState.Killed),

            (CapabilityState.Active, CapabilityState.Deprecated),
            (CapabilityState.Active, CapabilityState.Revoked),
            (CapabilityState.Active, CapabilityState.Killed),

            (CapabilityState.Deprecated, CapabilityState.Revoked),
            (CapabilityState.Deprecated, CapabilityState.Killed)
        };

        public bool CanTransition(ResponsibilityState from, ResponsibilityState to) =>
            from == to || AllowedResponsibilityTransitions.Contains((from, to));

        public void AssertTransition(ResponsibilityState from, ResponsibilityState to)
        {
            if (!CanTransition(from, to))
                throw new InvalidRuntimeStateTransitionException("Responsibility", from.ToString(), to.ToString());
        }

        public bool CanTransition(MissionGraphState from, MissionGraphState to) =>
            from == to || AllowedMissionGraphTransitions.Contains((from, to));

        public void AssertTransition(MissionGraphState from, MissionGraphState to)
        {
            if (!CanTransition(from, to))
                throw new InvalidRuntimeStateTransitionException("MissionGraph", from.ToString(), to.ToString());
        }

        public bool CanTransition(MissionNodeState from, MissionNodeState to) =>
            from == to || AllowedMissionNodeTransitions.Contains((from, to));

        public void AssertTransition(MissionNodeState from, MissionNodeState to)
        {
            if (!CanTransition(from, to))
                throw new InvalidRuntimeStateTransitionException("MissionNode", from.ToString(), to.ToString());
        }

        public bool CanTransition(RunState from, RunState to) =>
            from == to || AllowedRunTransitions.Contains((from, to));

        public void AssertTransition(RunState from, RunState to)
        {
            if (!CanTransition(from, to))
                throw new InvalidRuntimeStateTransitionException("RuntimeRun", from.ToString(), to.ToString());
        }

        public bool CanTransition(AttemptState from, AttemptState to) =>
            from == to || AllowedAttemptTransitions.Contains((from, to));

        public void AssertTransition(AttemptState from, AttemptState to)
        {
            if (!CanTransition(from, to))
                throw new InvalidRuntimeStateTransitionException("RuntimeAttempt", from.ToString(), to.ToString());
        }

        public bool CanTransition(AgentInstanceState from, AgentInstanceState to) =>
            from == to || AllowedAgentInstanceTransitions.Contains((from, to));

        public void AssertTransition(AgentInstanceState from, AgentInstanceState to)
        {
            if (!CanTransition(from, to))
                throw new InvalidRuntimeStateTransitionException("AgentInstance", from.ToString(), to.ToString());
        }

        public bool CanTransition(CapabilityState from, CapabilityState to) =>
            from == to || AllowedCapabilityTransitions.Contains((from, to));

        public void AssertTransition(CapabilityState from, CapabilityState to)
        {
            if (!CanTransition(from, to))
                throw new InvalidRuntimeStateTransitionException("Capability", from.ToString(), to.ToString());
        }
    }
}
