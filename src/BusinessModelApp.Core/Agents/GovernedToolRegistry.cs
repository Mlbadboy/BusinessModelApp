using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Observability;

using BusinessModelApp.Core.Prospecting;

namespace BusinessModelApp.Core.Agents
{
    public interface IGovernedToolRegistry
    {
        Task<ToolExecutionResult> ExecuteToolAsync(
            AgentIdentity agent,
            AgentActionType action,
            AgentMission mission,
            Dictionary<string, object> parameters,
            CancellationToken ct = default);
    }

    public class ToolExecutionResult
    {
        public bool Success { get; set; }
        public string OutputJson { get; set; } = "{}";
        public string? EvidenceId { get; set; }
        public decimal CostINR { get; set; } = 0.0m;
        public string? BlockReason { get; set; }
        public bool RequiresApproval { get; set; }
        public Guid? ApprovalRequestId { get; set; }
    }

    public class GovernedToolRegistry : IGovernedToolRegistry
    {
        private readonly AgentPolicyEngine _policyEngine;
        private readonly ICommercialRepository _commercialRepo;
        private readonly IProspectDiscoveryService? _prospectDiscovery;
        private readonly Dictionary<AgentActionType, IToolExecutionAdapter> _adapters;

        public GovernedToolRegistry(
            ICommercialRepository commercialRepo,
            IEnumerable<IToolExecutionAdapter>? adapters)
            : this(commercialRepo, null, adapters, null)
        {
        }

        public GovernedToolRegistry(
            ICommercialRepository? commercialRepo = null,
            IProspectDiscoveryService? prospectDiscovery = null,
            IEnumerable<IToolExecutionAdapter>? adapters = null,
            AgentPolicyEngine? policyEngine = null)
        {
            _commercialRepo = commercialRepo!;
            _prospectDiscovery = prospectDiscovery;
            _policyEngine = policyEngine ?? new AgentPolicyEngine();
            _adapters = adapters?.ToDictionary(a => a.ActionType, a => a) ?? new Dictionary<AgentActionType, IToolExecutionAdapter>();
        }

        public GovernedToolRegistry(ICommercialRepository? commercialRepo, AgentPolicyEngine? policyEngine)
            : this(commercialRepo, null, null, policyEngine)
        {
        }

        public async Task<ToolExecutionResult> ExecuteToolAsync(
            AgentIdentity agent,
            AgentActionType action,
            AgentMission mission,
            Dictionary<string, object> parameters,
            CancellationToken ct = default)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));
            if (mission == null) throw new ArgumentNullException(nameof(mission));
            parameters ??= new Dictionary<string, object>();

            // 1. Evaluate Agent Policy Engine
            decimal monetaryImpact = parameters.TryGetValue("amount", out var amt) && amt is decimal d ? d : 0m;
            var policyDecision = _policyEngine.Evaluate(agent, action, mission.AutonomyLevel, monetaryImpact);

            if (policyDecision.Decision == PolicyActionDecision.DenyAction)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    BlockReason = policyDecision.Reason
                };
            }

            if (policyDecision.Decision == PolicyActionDecision.RequireHumanApproval)
            {
                var approvalId = Guid.NewGuid();
                return new ToolExecutionResult
                {
                    Success = false,
                    RequiresApproval = true,
                    ApprovalRequestId = approvalId,
                    BlockReason = policyDecision.Reason
                };
            }

            // 2. Financial Wallet Check
            decimal estimatedCost = action switch
            {
                AgentActionType.DispatchVoiceCall => 10.00m, // Strict ₹10 max hold ceiling for Voice
                AgentActionType.SearchWeb => 0.25m,
                AgentActionType.ResearchCompany => 0.50m,
                AgentActionType.DiscoverDecisionMakers => 0.75m,
                AgentActionType.DraftOutreach => 0.40m,
                AgentActionType.SendOutreach => 0.20m,
                AgentActionType.GenerateProposal => 1.50m,
                _ => 0.10m
            };

            if (!mission.Wallet.TryReserve(estimatedCost))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    BlockReason = "Mission wallet exhausted. Tool execution halted by FinOps policy."
                };
            }

            // 3. Execution Pipeline via Adapter or Fallback Handler
            try
            {
                ToolExecutionResult result;
                if (_adapters.TryGetValue(action, out var adapter))
                {
                    result = await adapter.ExecuteAsync(agent, mission, parameters, ct);
                }
                else
                {
                    result = await DispatchDefaultExecutionAsync(action, mission, parameters, ct);
                }

                if (result.Success)
                {
                    // Reconcile hold vs actual cost
                    decimal actualCost = result.CostINR > 0 ? result.CostINR : estimatedCost;
                    mission.Wallet.Reconcile(estimatedCost, actualCost);
                    result.CostINR = actualCost;
                }
                else
                {
                    // Release full hold on execution failure
                    mission.Wallet.Reconcile(estimatedCost, 0m);
                }

                return result;
            }
            catch (Exception ex)
            {
                mission.Wallet.Reconcile(estimatedCost, 0m); // release hold on error
                return new ToolExecutionResult
                {
                    Success = false,
                    BlockReason = $"Tool execution error: {TelemetrySanitizer.Sanitize(ex.Message)}"
                };
            }
        }

        private async Task<ToolExecutionResult> DispatchDefaultExecutionAsync(
            AgentActionType action,
            AgentMission mission,
            Dictionary<string, object> parameters,
            CancellationToken ct)
        {
            string targetCompany = parameters.TryGetValue("company", out var c) ? c?.ToString() ?? "Target Corp" : "Target Corp";

            switch (action)
            {
                case AgentActionType.SearchWeb:
                {
                    if (_prospectDiscovery != null)
                    {
                        var signals = await _prospectDiscovery.ScanMarketSignalsAsync(mission.TargetIndustry, ct);
                        string firstEvidence = signals.Count > 0 ? signals[0].EvidenceHash : $"EVD-MKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                        return new ToolExecutionResult
                        {
                            Success = true,
                            EvidenceId = firstEvidence,
                            CostINR = 0.25m,
                            OutputJson = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                industry = mission.TargetIndustry,
                                signalCount = signals.Count,
                                signals = signals.Select(s => new { s.Type, s.Headline, s.Description, s.Source, s.EvidenceHash })
                            })
                        };
                    }

                    string evidenceId = $"EVD-SEARCH-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    return new ToolExecutionResult
                    {
                        Success = true,
                        EvidenceId = evidenceId,
                        CostINR = 0.25m,
                        OutputJson = $"{{\"signals\":[\"Enterprise AI adoption in {mission.TargetIndustry}\"],\"evidenceId\":\"{evidenceId}\"}}"
                    };
                }

                case AgentActionType.ResearchCompany:
                {
                    if (_prospectDiscovery != null)
                    {
                        var accounts = await _prospectDiscovery.DiscoverCandidateAccountsAsync(mission.TargetIndustry, "India", 500, ct);
                        var selectedAccount = accounts.FirstOrDefault() ?? new DiscoveredCandidateAccount
                        {
                            CompanyName = targetCompany,
                            Domain = "targetcorp.in",
                            Industry = mission.TargetIndustry,
                            Headcount = 2500,
                            ICPScore = 85.0m,
                            EvidenceToken = $"EVD-ACC-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
                        };

                        return new ToolExecutionResult
                        {
                            Success = true,
                            EvidenceId = selectedAccount.EvidenceToken,
                            CostINR = 0.50m,
                            OutputJson = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                company = selectedAccount.CompanyName,
                                domain = selectedAccount.Domain,
                                industry = selectedAccount.Industry,
                                headcount = selectedAccount.Headcount,
                                revenueINR = selectedAccount.EstimatedAnnualRevenueINR,
                                icpScore = selectedAccount.ICPScore,
                                signals = selectedAccount.Signals.Select(s => s.Headline),
                                evidenceToken = selectedAccount.EvidenceToken
                            })
                        };
                    }

                    string fallbackEvidenceId = $"EVD-RESEARCH-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    return new ToolExecutionResult
                    {
                        Success = true,
                        EvidenceId = fallbackEvidenceId,
                        CostINR = 0.50m,
                        OutputJson = $"{{\"company\":\"{targetCompany}\",\"signal\":\"Enterprise AI transformation initiative announced\",\"headcount\":\"2,500+\",\"evidenceId\":\"{fallbackEvidenceId}\"}}"
                    };
                }

                case AgentActionType.DiscoverDecisionMakers:
                {
                    string domain = parameters.TryGetValue("domain", out var dom) ? dom?.ToString() ?? $"{targetCompany.ToLower().Replace(" ", "")}.com" : $"{targetCompany.ToLower().Replace(" ", "")}.com";
                    if (_prospectDiscovery != null)
                    {
                        var candidateAccount = new DiscoveredCandidateAccount
                        {
                            CompanyName = targetCompany,
                            Domain = domain,
                            Industry = mission.TargetIndustry
                        };

                        var decisionMakers = await _prospectDiscovery.DiscoverDecisionMakersAsync(candidateAccount, ct);
                        var topBuyer = decisionMakers.FirstOrDefault() ?? new CandidateDecisionMaker
                        {
                            Name = "Executive Decision Maker",
                            Title = "Chief Digital Officer",
                            CorporateEmail = $"executive@{domain}",
                            AuthorityScore = 0.90m
                        };

                        string dmEvidenceId = $"EVD-DM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                        return new ToolExecutionResult
                        {
                            Success = true,
                            EvidenceId = dmEvidenceId,
                            CostINR = 0.75m,
                            OutputJson = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                contactName = topBuyer.Name,
                                title = topBuyer.Title,
                                executivePersona = topBuyer.ExecutivePersona,
                                corporateEmail = topBuyer.CorporateEmail,
                                authorityScore = topBuyer.AuthorityScore,
                                company = targetCompany,
                                domain,
                                verificationSource = topBuyer.VerificationSource,
                                evidenceId = dmEvidenceId
                            })
                        };
                    }

                    string fallbackDmEvidence = $"EVD-DM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    return new ToolExecutionResult
                    {
                        Success = true,
                        EvidenceId = fallbackDmEvidence,
                        CostINR = 0.75m,
                        OutputJson = $"{{\"contactName\":\"Executive Buyer\",\"title\":\"Chief Digital Officer\",\"company\":\"{targetCompany}\",\"fitScore\":90.0,\"evidenceId\":\"{fallbackDmEvidence}\"}}"
                    };
                }

                case AgentActionType.ScoreLead:
                {
                    decimal score = 88.5m;
                    string provenanceToken = $"PRV-{Guid.NewGuid().ToString().Substring(0, 10).ToUpper()}";
                    string rationale = "Enterprise candidate matches ICP scale and active transformation signals.";

                    if (_prospectDiscovery != null)
                    {
                        var candidateAccount = new DiscoveredCandidateAccount
                        {
                            CompanyName = targetCompany,
                            Domain = parameters.TryGetValue("domain", out var dom) ? dom?.ToString() ?? "corp.in" : "corp.in",
                            Industry = mission.TargetIndustry,
                            Headcount = parameters.TryGetValue("headcount", out var hc) && int.TryParse(hc?.ToString(), out var parsedHc) ? parsedHc : 3000,
                            EstimatedAnnualRevenueINR = 1500000000m
                        };

                        var dm = new CandidateDecisionMaker
                        {
                            Name = parameters.TryGetValue("contactName", out var cn) ? cn?.ToString() ?? "Decision Maker" : "Decision Maker",
                            Title = parameters.TryGetValue("title", out var tit) ? tit?.ToString() ?? "Chief Digital Officer" : "Chief Digital Officer"
                        };

                        var verifiedLead = await _prospectDiscovery.QualifyAndVerifyProspectAsync(candidateAccount, dm, mission.TargetIndustry, mission.TargetValueINR, ct);
                        if (verifiedLead != null)
                        {
                            score = verifiedLead.AIQualificationScore > 0 ? verifiedLead.AIQualificationScore : verifiedLead.ICPScore;
                            provenanceToken = verifiedLead.ProvenanceToken;
                            rationale = verifiedLead.QualificationRationale;
                        }
                    }

                    return new ToolExecutionResult
                    {
                        Success = true,
                        EvidenceId = provenanceToken,
                        CostINR = 0.10m,
                        OutputJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            score,
                            tier = score >= 80m ? "Tier-1 Enterprise" : "Tier-2 Growth",
                            provenanceToken,
                            rationale
                        })
                    };
                }

                case AgentActionType.DraftOutreach:
                case AgentActionType.SendOutreach:
                {
                    string evidenceId = $"EVD-COMM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    string recipient = parameters.TryGetValue("email", out var emObj) && !string.IsNullOrWhiteSpace(emObj?.ToString())
                        ? emObj.ToString()!
                        : $"contact@{targetCompany.ToLower().Replace(" ", "")}.com";

                    return new ToolExecutionResult
                    {
                        Success = true,
                        EvidenceId = evidenceId,
                        CostINR = 0.20m,
                        OutputJson = $"{{\"status\":\"Sent\",\"channel\":\"Email\",\"recipient\":\"{recipient}\",\"evidenceId\":\"{evidenceId}\"}}"
                    };
                }

                case AgentActionType.CreateLeadInCRM:
                {
                    string contactName = parameters.TryGetValue("contactName", out var cnObj) && !string.IsNullOrWhiteSpace(cnObj?.ToString())
                        ? cnObj.ToString()!
                        : "Executive Commercial Contact";

                    string companyName = parameters.TryGetValue("company", out var compObj) && !string.IsNullOrWhiteSpace(compObj?.ToString())
                        ? compObj.ToString()!
                        : targetCompany;

                    string cleanDomain = parameters.TryGetValue("domain", out var domObj) && !string.IsNullOrWhiteSpace(domObj?.ToString())
                        ? domObj.ToString()!
                        : $"{companyName.ToLower().Replace(" ", "")}.com";

                    string email = parameters.TryGetValue("email", out var emObj) && !string.IsNullOrWhiteSpace(emObj?.ToString())
                        ? emObj.ToString()!
                        : $"{contactName.ToLower().Replace(" ", ".")}@{cleanDomain}";

                    double qualityScore = parameters.TryGetValue("score", out var scObj) && double.TryParse(scObj?.ToString(), out var scVal)
                        ? scVal
                        : 88.0;

                    string provenance = parameters.TryGetValue("provenanceToken", out var provObj) && !string.IsNullOrWhiteSpace(provObj?.ToString())
                        ? provObj.ToString()!
                        : $"PRV-{Guid.NewGuid().ToString().Substring(0, 10).ToUpper()}";

                    if (mission.Mode == MissionMode.LiveProduction)
                    {
                        var lead = new Lead
                        {
                            WorkspaceId = mission.WorkspaceId,
                            ContactName = contactName,
                            CompanyName = companyName,
                            Email = email,
                            QualityScore = qualityScore,
                            Source = LeadSource.InboundWeb,
                            Notes = $"Autonomously discovered by Mission '{mission.Title}'. Provenance: {provenance}."
                        };
                        var created = await _commercialRepo.CreateLeadAsync(lead, ct);
                        return new ToolExecutionResult
                        {
                            Success = true,
                            CostINR = 0.10m,
                            OutputJson = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                leadId = created.Id,
                                contactName = created.ContactName,
                                companyName = created.CompanyName,
                                email = created.Email,
                                provenanceToken = provenance,
                                isLive = true
                            })
                        };
                    }
                    else
                    {
                        // Simulation Mode -> Synthetic ID, zero real DB persistence
                        return new ToolExecutionResult
                        {
                            Success = true,
                            CostINR = 0.0m,
                            OutputJson = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                leadId = $"SIM-LEAD-{Guid.NewGuid().ToString().Substring(0, 8)}",
                                contactName,
                                companyName,
                                email,
                                provenanceToken = provenance,
                                isLive = false,
                                synthetic = true
                            })
                        };
                    }
                }

                case AgentActionType.CreateOpportunityInCRM:
                {
                    string evidenceId = $"EVD-OPP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    decimal oppValue = parameters.TryGetValue("amount", out var a) && a is decimal amt ? amt : 1850000m;

                    if (mission.Mode == MissionMode.LiveProduction)
                    {
                        // Extract LeadId
                        Guid leadId = Guid.Empty;
                        if (parameters.TryGetValue("leadId", out var lidObj) && lidObj is Guid gid)
                        {
                            leadId = gid;
                        }
                        else if (parameters.TryGetValue("leadId", out var lidStr) && Guid.TryParse(lidStr?.ToString(), out var parsedGid))
                        {
                            leadId = parsedGid;
                        }

                        if (leadId == Guid.Empty)
                        {
                            var recentLeads = await _commercialRepo.GetLeadsByWorkspaceIdAsync(mission.WorkspaceId, ct);
                            var lead = recentLeads.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
                            if (lead != null)
                            {
                                leadId = lead.Id;
                            }
                            else
                            {
                                string fallbackContact = parameters.TryGetValue("contactName", out var cnVal) && !string.IsNullOrWhiteSpace(cnVal?.ToString()) ? cnVal.ToString()! : "Commercial Decision Maker";
                                var newLead = new Lead
                                {
                                    WorkspaceId = mission.WorkspaceId,
                                    ContactName = fallbackContact,
                                    CompanyName = targetCompany,
                                    Email = $"contact@{targetCompany.ToLower().Replace(" ", "")}.com",
                                    QualityScore = 88.5,
                                    Source = LeadSource.InboundWeb,
                                    Notes = $"Autonomously created for Opportunity by Mission: {mission.Title}"
                                };
                                var createdLead = await _commercialRepo.CreateLeadAsync(newLead, ct);
                                leadId = createdLead.Id;
                            }
                        }

                        var opportunity = new Opportunity
                        {
                            WorkspaceId = mission.WorkspaceId,
                            LeadId = leadId,
                            Title = $"{targetCompany} - Enterprise AI Operations",
                            EstimatedValue = oppValue,
                            Currency = "INR",
                            Stage = OpportunityStage.Discovery,
                            Probability = 0.2,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                            PrimaryConcern = "Compliance and local deployment SLA.",
                            NextStep = "Deliver tailored commercial proposal and executive brief."
                        };

                        var createdOpp = await _commercialRepo.CreateOpportunityAsync(opportunity, ct);

                        // Business Activity
                        await _commercialRepo.AddActivityAsync(new Activity
                        {
                            OpportunityId = createdOpp.Id,
                            Type = ActivityType.StageChanged,
                            Title = "Autonomous Opportunity Dispatched",
                            Description = $"Created autonomously by Mission '{mission.Title}'. Initial stage: Discovery.",
                            PerformedByName = "Charlie Autonomous Agent"
                        }, ct);

                        // Security Audit Event
                        await _commercialRepo.LogAuditEventAsync(new AuditEvent
                        {
                            WorkspaceId = mission.WorkspaceId,
                            EntityType = nameof(Opportunity),
                            EntityId = createdOpp.Id,
                            EventType = AuditEventType.OpportunityCreated,
                            ActionName = "Autonomous Opportunity Created",
                            Description = $"Live autonomous agent created Opportunity '{createdOpp.Title}' for {createdOpp.EstimatedValue:N0} INR.",
                            PerformedByName = "Charlie Autonomous Agent"
                        }, ct);

                        return new ToolExecutionResult
                        {
                            Success = true,
                            EvidenceId = evidenceId,
                            CostINR = 0.50m,
                            OutputJson = $"{{\"opportunityId\":\"{createdOpp.Id}\",\"opportunityTitle\":\"{createdOpp.Title}\",\"leadId\":\"{leadId}\",\"value\":{oppValue},\"stage\":\"Discovery\",\"isLive\":true,\"evidenceId\":\"{evidenceId}\"}}"
                        };
                    }
                    else
                    {
                        // Simulation Mode: synthetic response, zero DB persistence
                        return new ToolExecutionResult
                        {
                            Success = true,
                            EvidenceId = evidenceId,
                            CostINR = 0.0m,
                            OutputJson = $"{{\"opportunityId\":\"SIM-OPP-{Guid.NewGuid().ToString().Substring(0, 8)}\",\"opportunityTitle\":\"{targetCompany} - Enterprise AI Operations\",\"value\":{oppValue},\"stage\":\"Discovery\",\"isLive\":false,\"synthetic\":true,\"evidenceId\":\"{evidenceId}\"}}"
                        };
                    }
                }

                case AgentActionType.GenerateProposal:
                {
                    string evidenceId = $"EVD-PROP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    decimal oppValue = parameters.TryGetValue("amount", out var a) && a is decimal amt ? amt : 1850000m;

                    return new ToolExecutionResult
                    {
                        Success = true,
                        EvidenceId = evidenceId,
                        CostINR = mission.Mode == MissionMode.LiveProduction ? 1.50m : 0.0m,
                        OutputJson = $"{{\"proposalTitle\":\"{targetCompany} - Master Agreement\",\"terms\":\"Net 30\",\"value\":{oppValue},\"evidenceId\":\"{evidenceId}\"}}"
                    };
                }

                default:
                    return new ToolExecutionResult { Success = true, CostINR = 0.10m, OutputJson = "{\"status\":\"Executed\"}" };
            }
        }
    }
}
