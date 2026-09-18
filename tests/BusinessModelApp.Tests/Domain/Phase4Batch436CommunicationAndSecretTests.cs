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
    public sealed class Phase4Batch436CommunicationAndSecretTests
    {
        private readonly InMemoryCommunicationAndSecretStore _store;
        private readonly UnifiedCommunicationFabric _communicationFabric;
        private readonly WorkforceSecretBroker _secretBroker;

        public Phase4Batch436CommunicationAndSecretTests()
        {
            _store = new InMemoryCommunicationAndSecretStore();
            _communicationFabric = new UnifiedCommunicationFabric(_store);
            _secretBroker = new WorkforceSecretBroker(_store);
        }

        // =========================================================================
        // Family 1: Unified Communication Governance (PRG-1 & Risk Tiers)
        // =========================================================================

        [Fact]
        public async Task COMM436_01_HighRiskCommunication_RequiresHumanApprovalBeforeDispatch()
        {
            var intent = new CommunicationIntent
            {
                AgentId = "agent-sales-01",
                Channel = CommunicationChannel.EMAIL,
                RecipientAddress = "cfo@enterprise.com",
                Subject = "Enterprise License Agreement & Pricing Proposal",
                BodyContent = "Enclosed is our binding commercial agreement...",
                RiskTier = 3 // High-consequence commercial contract commitment
            };

            var submitted = await _communicationFabric.SubmitCommunicationIntentAsync("tenant-comm-436", intent);

            submitted.IsApproved.Should().BeFalse();
            submitted.IsDispatched.Should().BeFalse();

            // Attempt unapproved dispatch -> Rejected
            var actUnapproved = () => _communicationFabric.DispatchCommunicationAsync("tenant-comm-436", submitted.IntentId);
            await actUnapproved.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Governance Violation*PRG-1 human approval*");

            // Human approves via PRG-1
            var approved = await _communicationFabric.ApproveCommunicationIntentAsync("tenant-comm-436", submitted.IntentId, "HumanExecutive-01");
            approved.Should().BeTrue();

            // Dispatch now succeeds
            var dispatched = await _communicationFabric.DispatchCommunicationAsync("tenant-comm-436", submitted.IntentId);
            dispatched.Should().BeTrue();
        }

        [Fact]
        public async Task COMM436_02_LowRiskNotification_AutoApproves()
        {
            var intent = new CommunicationIntent
            {
                AgentId = "agent-ops-01",
                Channel = CommunicationChannel.SLACK,
                RecipientAddress = "#internal-alerts",
                Subject = "Nightly Backup Finished",
                RiskTier = 1 // Low risk internal
            };

            var submitted = await _communicationFabric.SubmitCommunicationIntentAsync("tenant-comm-436", intent);

            submitted.IsApproved.Should().BeTrue();
            submitted.ApprovedBy.Should().Be("SystemAutoApproval");
        }

        // =========================================================================
        // Family 2: Scoped Credential Broker & Token Isolation
        // =========================================================================

        [Fact]
        public async Task SEC436_03_SecretBroker_IssuesAndValidatesScopedTokens()
        {
            var cred = await _secretBroker.IssueScopedCredentialAsync("tenant-comm-436", "cap-crm-read", 60);

            cred.TenantId.Should().Be("tenant-comm-436");
            cred.CapabilityId.Should().Be("cap-crm-read");
            cred.IsValid.Should().BeTrue();

            // Valid capability & tenant check
            var isValid = await _secretBroker.ValidateCredentialAsync("tenant-comm-436", cred.CredentialId, "cap-crm-read");
            isValid.Should().BeTrue();

            // Mismatched capability check
            var isInvalidCap = await _secretBroker.ValidateCredentialAsync("tenant-comm-436", cred.CredentialId, "cap-email-send");
            isInvalidCap.Should().BeFalse();

            // Cross-tenant check
            var isCrossTenant = await _secretBroker.ValidateCredentialAsync("tenant-other", cred.CredentialId, "cap-crm-read");
            isCrossTenant.Should().BeFalse();

            // Revocation check
            await _secretBroker.RevokeCredentialAsync("tenant-comm-436", cred.CredentialId);
            var isAfterRevoke = await _secretBroker.ValidateCredentialAsync("tenant-comm-436", cred.CredentialId, "cap-crm-read");
            isAfterRevoke.Should().BeFalse();
        }
    }
}
