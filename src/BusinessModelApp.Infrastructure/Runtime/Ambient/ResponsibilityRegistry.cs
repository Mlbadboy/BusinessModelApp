using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilityRegistry : IResponsibilityRegistry
    {
        private readonly ConcurrentDictionary<string, (ResponsibilityRecord Template, AmbientTriggerCondition Trigger)> _definitions = new();
        private readonly ConcurrentDictionary<string, ResponsibilityRecord> _activeRecords = new();

        public ResponsibilityId ComputeDeterministicId(Guid workspaceId, string definitionId, string entityScope, string triggerId)
        {
            var raw = $"{workspaceId:N}:{definitionId}:{entityScope}:{triggerId}".ToLowerInvariant();
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            
            // Format first 16 bytes as a deterministic Guid
            var guidBytes = new byte[16];
            Array.Copy(hashBytes, guidBytes, 16);
            return ResponsibilityId.From(new Guid(guidBytes));
        }

        public Task RegisterDefinitionAsync(ResponsibilityRecord template, AmbientTriggerCondition trigger, CancellationToken cancellationToken = default)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));

            var key = $"{template.WorkspaceId}:{template.DefinitionId}:{trigger.TriggerId}";
            _definitions[key] = (template, trigger);
            return Task.CompletedTask;
        }

        public Task<ResponsibilityRecord?> GetActiveResponsibilityAsync(ResponsibilityId id, CancellationToken cancellationToken = default)
        {
            _activeRecords.TryGetValue(id.Value.ToString(), out var record);
            return Task.FromResult(record);
        }

        public Task<IReadOnlyList<ResponsibilityRecord>> GetActiveResponsibilitiesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var records = _activeRecords.Values
                .Where(r => r.WorkspaceId == workspaceId && r.State != ResponsibilityLifecycleState.Resolved && r.State != ResponsibilityLifecycleState.Rejected)
                .ToList();
            return Task.FromResult<IReadOnlyList<ResponsibilityRecord>>(records);
        }

        public Task<IReadOnlyList<(ResponsibilityRecord Template, AmbientTriggerCondition Trigger)>> GetRegisteredDefinitionsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var defs = _definitions.Values
                .Where(d => d.Template.WorkspaceId == workspaceId)
                .ToList();
            return Task.FromResult<IReadOnlyList<(ResponsibilityRecord Template, AmbientTriggerCondition Trigger)>>(defs);
        }

        public Task SaveActiveResponsibilityAsync(ResponsibilityRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            _activeRecords[record.Id.Value.ToString()] = record;
            return Task.CompletedTask;
        }
    }
}
