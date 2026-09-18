using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Multimodal
{
    public class ComputerSessionManager : IComputerSessionManager
    {
        private readonly ConcurrentDictionary<string, ComputerSession> _sessions = new();

        public Task<ComputerSession> CreateSessionAsync(string tenantId, string applicationContext, string targetGoal)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            var session = new ComputerSession
            {
                TenantId = tenantId,
                ApplicationContext = applicationContext ?? "DesktopEnvironment",
                TargetGoal = targetGoal ?? "Automate governed task",
                CurrentState = ComputerSessionState.Created
            };

            session.TransitionTo(ComputerSessionState.Initializing, "Session initialized", "System");
            _sessions[session.SessionId] = session;
            return Task.FromResult(session);
        }

        public Task<ComputerSession?> GetSessionAsync(string tenantId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                if (session.TenantId != tenantId)
                {
                    throw new UnauthorizedAccessException($"Tenant penetration defense: session {sessionId} belongs to a different tenant");
                }
                return Task.FromResult<ComputerSession?>(session);
            }

            return Task.FromResult<ComputerSession?>(null);
        }

        public Task<IReadOnlyList<ComputerSession>> ListSessionsAsync(string tenantId, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            var list = _sessions.Values
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.CreatedAtUtc)
                .Take(limit)
                .ToList();

            return Task.FromResult<IReadOnlyList<ComputerSession>>(list);
        }

        public Task<ComputerSession> TransitionSessionAsync(string tenantId, string sessionId, ComputerSessionState newState, string reason)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                throw new KeyNotFoundException($"Session {sessionId} not found");
            }

            if (session.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException($"Tenant penetration defense: session {sessionId} does not belong to {tenantId}");
            }

            if (session.IsEmergencyKilled && newState != ComputerSessionState.Cancelled)
            {
                throw new InvalidOperationException($"Cannot transition session {sessionId}: session was emergency killed");
            }

            session.TransitionTo(newState, reason);
            return Task.FromResult(session);
        }

        public Task<ComputerSession> PauseSessionAsync(string tenantId, string sessionId, string humanReason)
        {
            return TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.Paused, $"Paused by human: {humanReason}");
        }

        public Task<ComputerSession> ResumeSessionAsync(string tenantId, string sessionId, string supervisorId)
        {
            return TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.Observing, $"Resumed by supervisor: {supervisorId}");
        }

        public Task<ComputerSession> EmergencyKillSessionAsync(string tenantId, string sessionId, string reason)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                throw new KeyNotFoundException($"Session {sessionId} not found");
            }

            if (session.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException($"Tenant penetration defense: session {sessionId} does not belong to {tenantId}");
            }

            session.IsEmergencyKilled = true;
            session.TransitionTo(ComputerSessionState.Cancelled, $"Emergency kill switch activated: {reason}", "KillSwitch");
            return Task.FromResult(session);
        }
    }
}
