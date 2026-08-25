using System;
using System.Collections.Generic;
using BusinessModelApp.Core.DTOs.Audit;
using BusinessModelApp.Core.DTOs.Security;

namespace BusinessModelApp.Core.DTOs
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public List<PermissionDto> Permissions { get; set; }
        public List<AccessLevelDto> AccessLevels { get; set; }
        public List<ModuleDto> AllowedModules { get; set; }
        public List<PrivilegeDto> SpecialPrivileges { get; set; }
        public List<ModulePermissionDto> ModulePermissions { get; set; }
        public List<AuditLogDto> AuditLogs { get; set; }
        public List<RateLimitDto> RateLimits { get; set; }
        public List<SessionPolicyDto> SessionPolicies { get; set; }
        public List<SecurityAlertDto> SecurityAlerts { get; set; }
        public List<SecurityPolicyDto> SecurityPolicies { get; set; }
        public List<TwoFactorAuthPolicyDto> TwoFactorAuthPolicies { get; set; }
        public List<BusinessOperationPermissionDto> BusinessOperationPermissions { get; set; }
        public List<EncryptionPolicyDto> EncryptionPolicies { get; set; }
        public List<DataMaskingPolicyDto> MaskingPolicies { get; set; }
        public List<DLPRuleDto> DLPRules { get; set; }
        public List<SecurityAnalyticsDto> Analytics { get; set; }
        public List<NetworkSecurityConfigDto> NetworkSecurityConfigs { get; set; }
        public List<NetworkSecurityPolicyDto> NetworkSecurityPolicies { get; set; }
        public bool CanRunReports { get; set; } // Can run reports
        public bool CanModifySettings { get; set; } // Can modify system settings
        public bool CanViewAuditLogs { get; set; } // Can view audit logs
        public bool Requires2FA { get; set; } // Requires two-factor authentication
        public bool CanManage2FA { get; set; } // Can manage 2FA settings
        public bool CanViewSecurityPolicies { get; set; } // Can view security policies
        public bool CanModifySecurityPolicies { get; set; } // Can modify security policies
        public bool CanManageEncryption { get; set; } // Can manage encryption settings
        public bool CanViewEncryption { get; set; } // Can view encryption data
        public bool CanManageMasking { get; set; } // Can manage data masking
        public bool CanViewMaskedData { get; set; } // Can view masked data
        public bool CanManageDLP { get; set; } // Can manage DLP rules
        public bool CanViewDLPAlerts { get; set; } // Can view DLP alerts
        public bool CanViewSecurityAnalytics { get; set; } // Can view security analytics
        public bool CanModifyAnalyticsSettings { get; set; } // Can modify analytics settings
        public bool CanViewNetworkMetrics { get; set; } // Can view network metrics
        public bool CanManageNetworkSecurity { get; set; } // Can manage network security
        public bool CanConfigureSecuritySettings { get; set; } // Can configure security settings
        public bool CanViewSecurityConfig { get; set; } // Can view security configuration
        public bool CanModifySecurityConfig { get; set; } // Can modify security configuration
    }

    public class NetworkSecurityConfigDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public dynamic CurrentValue { get; set; }
        public dynamic DefaultValue { get; set; }
        public List<string> Options { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public bool RequiresApproval { get; set; }
        public string ApprovalLevel { get; set; }
    }

    public class NetworkSecurityPolicyDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public List<string> Rules { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public bool RequiresApproval { get; set; }
        public string ApprovalLevel { get; set; }
        public List<string> TargetModules { get; set; }
    }

    public class DLPRuleDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string RuleType { get; set; }
        public string Pattern { get; set; }
        public List<string> Keywords { get; set; }
        public List<string> FileTypes { get; set; }
        public List<string> Actions { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public bool RequiresApproval { get; set; }
        public string ApprovalLevel { get; set; }
        public List<string> TargetModules { get; set; }
    }

    public class SecurityAnalyticsDto
    {
        public string Id { get; set; }
        public string MetricName { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public double Value { get; set; }
        public double Trend { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public List<string> RelatedModules { get; set; }
        public bool IsAlert { get; set; }
        public string AlertLevel { get; set; }
    }

    public class EncryptionPolicyDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Algorithm { get; set; }
        public int KeyLength { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public List<string> TargetDataTypes { get; set; }
        public bool IsDefault { get; set; }
        public bool RequiresApproval { get; set; }
        public string ApprovalLevel { get; set; }
    }

    public class DataMaskingPolicyDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TargetType { get; set; }
        public string MaskingType { get; set; }
        public string MaskFormat { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public List<string> TargetFields { get; set; }
        public bool IsDefault { get; set; }
        public bool RequiresApproval { get; set; }
        public string ApprovalLevel { get; set; }
    }

    public class SecurityPolicyDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string ComplianceLevel { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public List<PolicyRequirementDto> Requirements { get; set; }
    }

    public class PolicyRequirementDto
    {
        public string Id { get; set; }
        public string PolicyId { get; set; }
        public string Requirement { get; set; }
        public string ComplianceLevel { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class TwoFactorAuthPolicyDto
    {
        public string Id { get; set; }
        public string Module { get; set; }
        public bool IsRequired { get; set; }
        public int RecoveryCodeCount { get; set; }
        public int RecoveryCodeExpiryDays { get; set; }
        public bool AllowBackupCodes { get; set; }
        public bool RequirePeriodicVerification { get; set; }
        public int VerificationIntervalDays { get; set; }
    }

    public class BusinessOperationPermissionDto
    {
        public string Id { get; set; }
        public string Operation { get; set; }
        public string Module { get; set; }
        public string Category { get; set; }
        public bool IsGranted { get; set; }
        public bool RequiresApproval { get; set; }
        public string ApprovalLevel { get; set; }
        public string Status { get; set; }
        public DateTime LastModified { get; set; }
        public string ModifiedBy { get; set; }
    }
}
