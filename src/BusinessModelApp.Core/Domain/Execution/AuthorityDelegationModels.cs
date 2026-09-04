using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BusinessModelApp.Core.Domain.Execution
{
    [Table("AuthorityDelegations")]
    public class AuthorityDelegationEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid? OrganizationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string AgentId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string AgentRole { get; set; } = string.Empty;

        [Required]
        public Guid GrantedByUserId { get; set; }

        public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
        public DateTime ValidUntilUtc { get; set; } = DateTime.UtcNow.AddMonths(1);

        public decimal DailySpendLimitINR { get; set; } = 50000m;
        public decimal PerTransactionLimitINR { get; set; } = 10000m;

        /// <summary>
        /// JSON array of allowed capability IDs (e.g. ["CRM.Read", "CRM.CreateLead", "Email.Send"])
        /// </summary>
        public string AllowedCapabilitiesJson { get; set; } = "[]";

        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAtUtc { get; set; }
        public string? RevocationReason { get; set; }
        public Guid? RevokedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsActiveNow()
        {
            var now = DateTime.UtcNow;
            return !IsRevoked && now >= ValidFromUtc && now <= ValidUntilUtc;
        }

        public bool AllowsCapability(string capabilityId)
        {
            if (string.IsNullOrWhiteSpace(AllowedCapabilitiesJson) || !IsActiveNow())
                return false;

            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(AllowedCapabilitiesJson);
                return list != null && list.Contains(capabilityId, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
