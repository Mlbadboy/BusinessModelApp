using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch448CommercialRealityPilotTests
    {
        private readonly InMemoryCommercialKernelStore _kernelStore;
        private readonly CommercialKernelService _kernelService;

        private readonly InMemoryOpportunityDiscoveryStore _oppStore;
        private readonly OpportunityDiscoveryService _oppService;

        private readonly InMemoryAccountIntelligenceStore _accountStore;
        private readonly AccountIntelligenceService _accountService;

        private readonly InMemorySalesIntelligenceStore _salesStore;
        private readonly SalesIntelligenceService _salesService;

        private readonly InMemoryOutreachEngineStore _outreachStore;
        private readonly OutreachEngineService _outreachService;

        private readonly InMemoryInboundResponseStore _inboundStore;
        private readonly InboundResponseService _inboundService;

        private readonly InMemoryMeetingIntelligenceStore _meetingStore;
        private readonly MeetingIntelligenceService _meetingService;

        private readonly InMemoryProposalAndDealStore _dealStore;
        private readonly ProposalAndDealService _dealService;

        private readonly InMemoryDeliveryAndInvoiceStore _deliveryStore;
        private readonly DeliveryAndInvoiceService _deliveryService;

        private readonly CommercialWatchtowerService _watchtowerService;

        public Phase4Batch448CommercialRealityPilotTests()
        {
            _kernelStore = new InMemoryCommercialKernelStore();
            _kernelService = new CommercialKernelService(_kernelStore);

            _oppStore = new InMemoryOpportunityDiscoveryStore();
            _oppService = new OpportunityDiscoveryService(_oppStore);

            _accountStore = new InMemoryAccountIntelligenceStore();
            _accountService = new AccountIntelligenceService(_accountStore);

            _salesStore = new InMemorySalesIntelligenceStore();
            _salesService = new SalesIntelligenceService(_salesStore);

            _outreachStore = new InMemoryOutreachEngineStore();
            _outreachService = new OutreachEngineService(_outreachStore);

            _inboundStore = new InMemoryInboundResponseStore();
            _inboundService = new InboundResponseService(_inboundStore);

            _meetingStore = new InMemoryMeetingIntelligenceStore();
            _meetingService = new MeetingIntelligenceService(_meetingStore);

            _dealStore = new InMemoryProposalAndDealStore();
            _dealService = new ProposalAndDealService(_dealStore);

            _deliveryStore = new InMemoryDeliveryAndInvoiceStore();
            _deliveryService = new DeliveryAndInvoiceService(_deliveryStore);

            _watchtowerService = new CommercialWatchtowerService(_deliveryStore, _dealStore, _oppStore);
        }

        private async Task TransitionStepAsync(
            string tenantId,
            string entityId,
            CommercialStage targetStage,
            string agentId,
            string evidenceId,
            string evidenceDigest = "digest-ok",
            string? humanSignoff = null,
            bool batch6 = false,
            string? batch6Permit = null)
        {
            var res = await _kernelService.AttemptTransitionAsync(new CommercialTransitionRequest
            {
                TenantId = tenantId,
                CommercialEntityId = entityId,
                TargetStage = targetStage,
                InitiatingAgentId = agentId,
                AuthoritativeEvidenceId = evidenceId,
                EvidenceDigest = evidenceDigest,
                HumanSignoffId = humanSignoff,
                IsBatch6Authorized = batch6,
                Batch6PermitId = batch6Permit,
                TransitionRationale = $"Transitioning to {targetStage}"
            });

            Assert.True(res.IsSuccess, $"Failed transition to {targetStage}: {res.RejectionReason} (Violations: {string.Join(", ", res.ConstitutionalViolations)})");
        }

        [Fact]
        public async Task FullCommercialOperationsLoop_FromOpportunityToRealizedCash_SucceedsWithUnbrokenLineage()
        {
            const string tenantId = "tenant-commercial-reality-pilot";

            // ─── STEP 1: Opportunity Discovery & Market Signal Ingestion ─────
            var signal = new MarketSignalItem
            {
                TenantId = tenantId,
                CompanyName = "Apex Health Logistics",
                Domain = "apexhealth.in",
                Industry = "Healthcare Supply Chain",
                Geography = "India",
                Source = "Healthcare Regulatory Filing 2026",
                RawContent = "Apex Health reported 28% delivery SLA slippages due to legacy manual dispatch routing."
            };
            var ingestedSignal = await _oppService.IngestMarketSignalAsync(signal);
            Assert.NotNull(ingestedSignal);

            var icp = new ICPProfile
            {
                TenantId = tenantId,
                ProfileName = "Mid-Enterprise Health Logistics",
                TargetIndustries = new List<string> { "Healthcare Supply Chain" },
                MinTargetRevenueINR = 1000000m
            };
            await _oppService.CreateIcpProfileAsync(icp);

            var opp = await _oppService.EvaluateSignalAgainstIcpAsync(tenantId, ingestedSignal.SignalId, icp.ProfileId);
            Assert.True(opp.IsGrounded);
            Assert.True(opp.ICPScore >= 0.7m);

            // Initialize canonical state machine in Commercial Kernel
            var kernelEntity = await _kernelService.InitializeCommercialEntityAsync(tenantId, opp.OpportunityId, "apex-acc-01");
            Assert.Equal(CommercialStage.LEAD, kernelEntity.CurrentStage);

            // Stage 0 -> 1: QUALIFYING
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.QUALIFYING, "opp-agent-1", ingestedSignal.SignalId);

            // Stage 1 -> 2: QUALIFIED
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.QUALIFIED, "opp-agent-1", opp.OpportunityId);

            // ─── STEP 2: Account & Buying Center Intelligence ────────────────
            var accountGraph = new EnterpriseAccountGraph
            {
                TenantId = tenantId,
                CompanyName = opp.CompanyName,
                Domain = signal.Domain,
                Industry = signal.Industry
            };
            var savedGraph = await _accountService.UpsertAccountGraphAsync(accountGraph);

            var buyingCenterContact = new BuyingCenterContact
            {
                ContactId = "contact-cto-apex",
                FullName = "Rajesh Sharma",
                Email = "rajesh.sharma@apexhealth.in",
                Title = "Chief Technology Officer",
                Role = BuyingCenterPersonaRole.ECONOMIC_BUYER,
                AuthoritativeEvidenceId = ingestedSignal.SignalId,
                ConfidenceScore = 0.95m
            };
            await _accountService.AddBuyingCenterContactAsync(tenantId, savedGraph.AccountId, buyingCenterContact);

            // ─── STEP 3: Sales Intelligence & Strategy Formulated ────────────
            var strategy = await _salesService.FormulateAccountStrategyAsync(tenantId, savedGraph.AccountId, opp.OpportunityId, "charlie-sales-agent");
            Assert.NotNull(strategy);
            Assert.False(strategy.IsAgentSelfAuthorized); // Law I40: Agent cannot self-authorize

            // ─── STEP 4: Governed Outreach Engine & PRG-1 Approval ───────────
            var outreachIntent = new CommercialCommunicationIntent
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                InitiatingAgentId = "charlie-sales-agent",
                Channel = CommercialOutreachChannel.EMAIL,
                RecipientAddress = buyingCenterContact.Email,
                Subject = "Optimizing Apex Health dispatch routing SLAs via Autonomous Operations",
                BodyContent = "Dear Mr. Sharma, We noticed Apex's recent expansion and have operational benchmarks to improve dispatch SLAs by 40%...",
                RiskTier = 3 // Mandates PRG-1 human approval
            };
            var submittedIntent = await _outreachService.SubmitOutboundIntentAsync(outreachIntent);
            Assert.True(submittedIntent.RequiresHumanApproval);

            // PRG-1 Human Executive Signoff
            bool approvedOutreach = await _outreachService.ApproveOutboundIntentAsync(tenantId, submittedIntent.IntentId, "human-exec-signoff-prg1");
            Assert.True(approvedOutreach);

            // Batch 6 Execution Firewall Permit Dispatch
            bool dispatched = await _outreachService.DispatchOutboundCommunicationAsync(tenantId, submittedIntent.IntentId, "batch6-permit-outreach-8821");
            Assert.True(dispatched);

            // Stage 2 -> 3: ENGAGED
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.ENGAGED, "charlie-sales-agent", submittedIntent.IntentId);

            // ─── STEP 5: Inbound Response & Meeting Intelligence ─────────────
            var inboundMsg = await _inboundService.ProcessInboundMessageAsync(
                tenantId,
                opp.OpportunityId,
                buyingCenterContact.Email,
                "Re: Optimizing Apex Health dispatch routing SLAs",
                "Hi Charlie team, this looks promising. Let's schedule a 30-minute discovery session this Thursday.");

            Assert.Equal(InboundMessageClassification.MEETING_REQUEST, inboundMsg.Classification);
            Assert.False(inboundMsg.IsPromptInjectionDetected);

            // Stage 3 -> 4: MEETING_REQUESTED
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.MEETING_REQUESTED, "inbound-agent", inboundMsg.MessageId);

            var brief = await _meetingService.PrepareMeetingBriefAsync(
                tenantId,
                opp.OpportunityId,
                opp.CompanyName,
                new List<string> { buyingCenterContact.FullName });
            Assert.NotNull(brief);

            // Stage 4 -> 5: MEETING_CONFIRMED
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.MEETING_CONFIRMED, "meeting-agent", brief.BriefId);

            // Conduct Meeting & Ingest Transcript
            var transcript = "Rajesh Sharma: We need an automated routing engine by next month. Our target budget is 12-15 lakhs INR. Deliverables must include EHR integration.";
            var analysis = await _meetingService.IngestAndAnalyzeTranscriptAsync(tenantId, "meeting-apex-01", opp.OpportunityId, transcript);
            Assert.False(analysis.IsLegallyBindingCommitment); // Law I40: Meeting transcript is not a binding contract

            // Stage 5 -> 6: DISCOVERY_COMPLETE
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.DISCOVERY_COMPLETE, "meeting-agent", analysis.AnalysisId);

            // ─── STEP 6: Proposal Factory & Governance Gate ──────────────────
            var proposal = new CommercialProposal
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                Title = "Autonomous Dispatch & Supply Chain Orchestrator",
                ScopeSummary = "Autonomous vehicle routing optimization and real-time inventory tracking",
                Deliverables = new List<string> { "Routing Algorithm Service", "EHR Connector", "Production Deployment" },
                EstimatedTimelineWeeks = 6,
                BasePriceINR = 1200000m,
                DiscountPercent = 0m, // net = 1,200,000
                EstimatedDeliveryCostINR = 480000m // Margin: (1,200,000 - 480,000) / 1,200,000 = 60.0% >= 35.0%
            };
            var createdProposal = await _dealService.CreateProposalAsync(proposal);
            Assert.True(createdProposal.IsMarginCompliant);
            Assert.Equal(60.0m, createdProposal.ExpectedGrossMarginPercent);

            // Stage 6 -> 7: SOLUTION_PROPOSED
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.SOLUTION_PROPOSED, "proposal-agent", createdProposal.ProposalId);

            // Human PRG-1 signoff on proposal
            bool approvedProp = await _dealService.ApproveProposalForSubmissionAsync(tenantId, createdProposal.ProposalId, "human-exec-signoff-prg1");
            Assert.True(approvedProp);

            // Stage 7 -> 8: PROPOSAL_SUBMITTED
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.PROPOSAL_SUBMITTED, "human-exec-signoff-prg1", createdProposal.ProposalId);

            // ─── STEP 7: Negotiation Intelligence ────────────────────────────
            var negotiation = await _dealService.AnalyzeNegotiationCounterOfferAsync(tenantId, createdProposal.ProposalId, 1100000m);
            Assert.False(negotiation.HasExecutiveAuthorityToCommit); // Law I40 invariant
            Assert.True(negotiation.CustomerOfferedPriceINR >= negotiation.FloorPriceINR);

            // Stage 8 -> 9: NEGOTIATION
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.NEGOTIATION, "negotiation-agent", negotiation.NegotiationId);

            // Stage 9 -> 10: COMMERCIAL_APPROVAL
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.COMMERCIAL_APPROVAL, "human-exec-signoff-prg1", negotiation.NegotiationId, humanSignoff: "human-exec-signoff-prg1");

            // Stage 10 -> 11: DEAL_WON (requires human PRG-1 signoff)
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.DEAL_WON, "human-exec-signoff-prg1", negotiation.NegotiationId, humanSignoff: "human-exec-signoff-prg1");

            // ─── STEP 8: Authoritative Contract Execution ────────────────────
            var contract = new AuthoritativeDealContract
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                ProposalId = createdProposal.ProposalId,
                CustomerSignerName = "Rajesh Sharma, CTO",
                CustomerSignerEmail = buyingCenterContact.Email,
                SignatureDigestSha256 = "c89b3f17d23a78921e428ba9012cd3198e09f87214902187fa92019487cbae31",
                VerificationSourceSystem = "DocuSign_Enterprise_v2",
                BindingDealValueINR = 1100000m
            };
            var recordedContract = await _dealService.RecordAuthoritativeContractAsync(contract);
            Assert.True(recordedContract.IsCryptographicallyVerified);

            // Stage 11 -> 12: CONTRACT_VERIFIED (requires EvidenceDigest)
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.CONTRACT_VERIFIED, "docusign-connector", recordedContract.ContractId, evidenceDigest: recordedContract.SignatureDigestSha256, humanSignoff: "human-legal-001");

            // ─── STEP 9: Delivery Operations & Customer Acceptance ───────────
            var workOrder = new CommercialWorkOrder
            {
                TenantId = tenantId,
                OpportunityId = opp.OpportunityId,
                ContractId = recordedContract.ContractId,
                Title = "Autonomous Dispatch Implementation",
                Deliverables = new List<string> { "Routing Service", "EHR Connector", "UAT Pass Certificate" }
            };
            var createdOrder = await _deliveryService.CreateWorkOrderAsync(workOrder);

            // Stage 12 -> 13: DELIVERY
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.DELIVERY, "delivery-swarm", createdOrder.WorkOrderId);

            // Customer tests and issues cryptographic acceptance
            var acceptedOrder = await _deliveryService.RecordCustomerDeliveryAcceptanceAsync(
                tenantId,
                createdOrder.WorkOrderId,
                buyingCenterContact.Email,
                "f98210bcda987123ef6129841029471928374198273419823741982374198237");
            Assert.True(acceptedOrder.IsCustomerAccepted);

            // Stage 13 -> 14: ACCEPTANCE
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.ACCEPTANCE, "customer-signoff", acceptedOrder.WorkOrderId, evidenceDigest: acceptedOrder.CustomerSignoffDigestSha256);

            // ─── STEP 10: Invoicing & Batch 6 Execution Firewall ─────────────
            var invoice = await _deliveryService.GenerateInvoiceAsync(tenantId, recordedContract.ContractId, acceptedOrder.WorkOrderId, 1100000m);
            Assert.Equal(InvoiceStatus.DRAFT, invoice.Status);
            Assert.Equal(198000m, invoice.TaxAmountINR); // 18% GST
            Assert.Equal(1298000m, invoice.TotalAmountINR);

            var issuedInvoice = await _deliveryService.IssueInvoiceWithBatch6PermitAsync(tenantId, invoice.InvoiceId, "batch6-permit-invoicing-apex-991");
            Assert.Equal(InvoiceStatus.ISSUED, issuedInvoice.Status);
            Assert.True(issuedInvoice.IsBatch6Authorized);

            // Stage 14 -> 15: INVOICED (subordinate to Batch 6)
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.INVOICED, "batch6-firewall", issuedInvoice.InvoiceId, batch6: true, batch6Permit: issuedInvoice.Batch6PermitId);

            // ─── STEP 11: Cash Collection Realization (Law I40 Peak) ──────────
            // Stage 15 -> 16: PAYMENT_PENDING
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.PAYMENT_PENDING, "ar-engine", issuedInvoice.InvoiceId, batch6: true, batch6Permit: issuedInvoice.Batch6PermitId);

            var receipt = await _deliveryService.ProcessCashCollectionAsync(
                tenantId,
                issuedInvoice.InvoiceId,
                issuedInvoice.TotalAmountINR,
                "HDFCN99281726354",
                "HDFC_CORPORATE_RTGS",
                "bank-crypto-clearing-confirmation-digest-sha256-verified");

            Assert.True(receipt.IsBankVerified);
            Assert.Equal(1298000m, receipt.CollectedAmountINR);

            var paidInvoice = await _deliveryService.GetInvoiceAsync(tenantId, issuedInvoice.InvoiceId);
            Assert.NotNull(paidInvoice);
            Assert.Equal(InvoiceStatus.PAID, paidInvoice.Status);

            // Stage 16 -> 17: PAYMENT_VERIFIED
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.PAYMENT_VERIFIED, "bank-connector", receipt.ReceiptId, evidenceDigest: receipt.BankConfirmationDigestSha256, batch6: true, batch6Permit: issuedInvoice.Batch6PermitId);

            // Stage 17 -> 18: REVENUE_REALIZED (Peak canonical stage)
            await TransitionStepAsync(tenantId, kernelEntity.CommercialEntityId, CommercialStage.REVENUE_REALIZED, "treasury-connector", receipt.ReceiptId, evidenceDigest: receipt.BankConfirmationDigestSha256, batch6: true, batch6Permit: issuedInvoice.Batch6PermitId);

            // ─── STEP 12: Causal Revenue Lineage Backward Tracing ────────────
            var lineageAudit = await _watchtowerService.TraceRevenueLineageAsync(tenantId, receipt.ReceiptId);

            Assert.True(lineageAudit.IsLineageUnbroken);
            Assert.Empty(lineageAudit.Defects);
            Assert.Equal(6, lineageAudit.TraceNodes.Count);
            Assert.All(lineageAudit.TraceNodes, n => Assert.True(n.IsVerified));

            // Verify canonical state block chain integrity
            var isAuditIntegrityValid = await _kernelService.VerifyAuditTrailIntegrityAsync(tenantId, kernelEntity.CommercialEntityId);
            Assert.True(isAuditIntegrityValid);

            var finalEntity = await _kernelService.GetCommercialEntityAsync(tenantId, kernelEntity.CommercialEntityId);
            Assert.NotNull(finalEntity);
            Assert.Equal(CommercialStage.REVENUE_REALIZED, finalEntity.CurrentStage);
            Assert.Equal(19, finalEntity.History.Count); // Genesis + 18 transitions

            for (int i = 1; i < finalEntity.History.Count; i++)
            {
                Assert.Equal(finalEntity.History[i - 1].CurrentBlockHash, finalEntity.History[i].PreviousBlockHash);
            }
        }
    }
}
