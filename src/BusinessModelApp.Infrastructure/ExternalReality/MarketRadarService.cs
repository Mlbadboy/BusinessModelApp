using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.ExternalReality
{
    public class MarketRadarService : IMarketRadarService
    {
        private readonly AppDbContext _dbContext;

        public MarketRadarService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ExternalSignal> DetectSignalAsync(ExternalSignal signal, CancellationToken ct = default)
        {
            if (signal.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be set for signal detection.", nameof(signal));

            // Deterministic Prioritization Calculation
            // PriorityScore = (Magnitude * 0.35) + (Confidence * 0.25) + (Freshness * 0.20) + (Novelty * 0.20)
            double priorityScore = (signal.Magnitude * 0.35) +
                                  (signal.Confidence * 0.25) +
                                  (signal.FreshnessScore * 0.20) +
                                  (signal.NoveltyScore * 0.20);
            signal.PriorityScore = Math.Clamp(Math.Round(priorityScore, 3), 0.0, 1.0);

            signal.Priority = signal.PriorityScore switch
            {
                >= 0.75 => ExternalSignalPriority.Critical,
                >= 0.55 => ExternalSignalPriority.High,
                >= 0.35 => ExternalSignalPriority.Medium,
                _ => ExternalSignalPriority.Low
            };

            // INVARIANT: Signal is strictly Observation or Estimate. NEVER Fact!
            if (signal.Classification == TruthClassification.Fact)
            {
                signal.Classification = TruthClassification.Observation;
            }

            signal.CreatedAt = DateTime.UtcNow;
            _dbContext.Set<ExternalSignal>().Add(signal);
            await _dbContext.SaveChangesAsync(ct);

            // If signal concerns a competitor, update or trigger CompetitorProfile update
            if (signal.SignalType == ExternalSignalType.CompetitorMove ||
                signal.SignalType == ExternalSignalType.PriceChange ||
                signal.SignalType == ExternalSignalType.ProductLaunch)
            {
                await UpdateCompetitorFromSignalAsync(signal, ct);
            }

            return signal;
        }

        public async Task<IReadOnlyList<ExternalSignal>> GetActiveSignalsAsync(Guid workspaceId, ExternalSignalPriority? minPriority = null, CancellationToken ct = default)
        {
            var query = _dbContext.Set<ExternalSignal>()
                .Where(s => s.WorkspaceId == workspaceId && s.Status != ExternalSignalStatus.Dismissed);

            if (minPriority.HasValue)
            {
                query = query.Where(s => s.Priority >= minPriority.Value);
            }

            return await query
                .OrderByDescending(s => s.PriorityScore)
                .ThenByDescending(s => s.DetectedAt)
                .ToListAsync(ct);
        }

        public async Task<ExternalSignal?> GetSignalAsync(Guid signalId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<ExternalSignal>()
                .FirstOrDefaultAsync(s => s.Id == signalId && s.WorkspaceId == workspaceId, ct);
        }

        public async Task<CompetitorProfile> UpdateCompetitorProfileAsync(CompetitorProfile profile, CancellationToken ct = default)
        {
            if (profile.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be set for competitor profile.", nameof(profile));

            var existing = await _dbContext.Set<CompetitorProfile>()
                .FirstOrDefaultAsync(c => c.WorkspaceId == profile.WorkspaceId && c.CompetitorName == profile.CompetitorName, ct);

            if (existing != null)
            {
                existing.Domain = profile.Domain;
                existing.ProductsJson = profile.ProductsJson;
                existing.PricingSummary = profile.PricingSummary;
                existing.ObservedPositioning = profile.ObservedPositioning;
                existing.ObservedStrategy = profile.ObservedStrategy;
                existing.HiringActivity = profile.HiringActivity;
                existing.StrengthsJson = profile.StrengthsJson;
                existing.WeaknessesJson = profile.WeaknessesJson;
                existing.MarketPresence = profile.MarketPresence;
                existing.UncertaintyScore = profile.UncertaintyScore;
                existing.LastSignalObservedAt = profile.LastSignalObservedAt ?? DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
                return existing;
            }

            profile.UpdatedAt = DateTime.UtcNow;
            _dbContext.Set<CompetitorProfile>().Add(profile);
            await _dbContext.SaveChangesAsync(ct);
            return profile;
        }

        public async Task<IReadOnlyList<CompetitorProfile>> ListCompetitorsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<CompetitorProfile>()
                .Where(c => c.WorkspaceId == workspaceId)
                .OrderBy(c => c.CompetitorName)
                .ToListAsync(ct);
        }

        public async Task<CompetitorProfile?> GetCompetitorAsync(Guid competitorId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<CompetitorProfile>()
                .FirstOrDefaultAsync(c => c.Id == competitorId && c.WorkspaceId == workspaceId, ct);
        }

        public async Task<MarketRegimeAssessment> AssessMarketRegimeAsync(Guid workspaceId, CancellationToken ct = default)
        {
            var signals = await _dbContext.Set<ExternalSignal>()
                .Where(s => s.WorkspaceId == workspaceId && s.Status != ExternalSignalStatus.Dismissed)
                .OrderByDescending(s => s.DetectedAt)
                .ToListAsync(ct);

            var assessment = new MarketRegimeAssessment
            {
                WorkspaceId = workspaceId,
                CurrentRegime = MarketRegimeState.Stable,
                Description = "Market operating within normal stochastic parameters.",
                RegimeConfidence = 0.85,
                AssessedAt = DateTime.UtcNow
            };

            if (!signals.Any()) return assessment;

            // 1. Detect Price War Regime: Cascade of repeated price cuts
            var priceDrops = signals.Where(s => s.SignalType == ExternalSignalType.PriceChange &&
                                                (s.Direction == "Negative" || s.Title.Contains("discount", StringComparison.OrdinalIgnoreCase) || s.Title.Contains("cut", StringComparison.OrdinalIgnoreCase) || s.Title.Contains("price drop", StringComparison.OrdinalIgnoreCase)))
                                    .ToList();

            if (priceDrops.Count >= 3)
            {
                assessment.CurrentRegime = MarketRegimeState.PriceWar;
                assessment.Description = $"Aggressive Price War detected across {priceDrops.Count} sequential price reduction signals. Margins under structural pressure.";
                assessment.RegimeConfidence = 0.92;
                assessment.TriggeringPatterns.Add($"Sequential competitor discounting cascade ({priceDrops.Count} events)");
                assessment.SignalCascadeIds.AddRange(priceDrops.Select(p => p.Id));
                return assessment;
            }

            // 2. Detect Category Disruption: Repeated product launches or technology shifts
            var disruptionSignals = signals.Where(s => s.SignalType == ExternalSignalType.ProductLaunch ||
                                                       s.SignalType == ExternalSignalType.TechnologyShift ||
                                                       s.SignalType == ExternalSignalType.DistributionChange)
                                           .ToList();

            if (disruptionSignals.Count >= 2)
            {
                assessment.CurrentRegime = MarketRegimeState.CategoryDisruption;
                assessment.Description = $"Category Disruption detected: {disruptionSignals.Count} rapid product launches or technology shifts disrupting incumbent positioning.";
                assessment.RegimeConfidence = 0.88;
                assessment.TriggeringPatterns.Add($"High-frequency product introduction / tech disruption cascade ({disruptionSignals.Count} events)");
                assessment.SignalCascadeIds.AddRange(disruptionSignals.Select(d => d.Id));
                return assessment;
            }

            // 3. Detect Regulatory Transition
            var regSignals = signals.Where(s => s.SignalType == ExternalSignalType.RegulatoryChange && s.Magnitude >= 0.70).ToList();
            if (regSignals.Any())
            {
                assessment.CurrentRegime = MarketRegimeState.RegulatoryShift;
                assessment.Description = "Regulatory Shift detected: High-impact policy or compliance change altering operating envelope.";
                assessment.RegimeConfidence = 0.90;
                assessment.TriggeringPatterns.Add("High-magnitude regulatory intervention");
                assessment.SignalCascadeIds.AddRange(regSignals.Select(r => r.Id));
                return assessment;
            }

            // 4. Detect Supply Disruption
            var supplySignals = signals.Where(s => s.SignalType == ExternalSignalType.SupplyDisruption).ToList();
            if (supplySignals.Any())
            {
                assessment.CurrentRegime = MarketRegimeState.SupplyShock;
                assessment.Description = "Supply Shock detected: External supply chain disruption impacting capacity or cost structure.";
                assessment.RegimeConfidence = 0.85;
                assessment.TriggeringPatterns.Add("Supply disruption signal detected");
                assessment.SignalCascadeIds.AddRange(supplySignals.Select(s => s.Id));
                return assessment;
            }

            return assessment;
        }

        private async Task UpdateCompetitorFromSignalAsync(ExternalSignal signal, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(signal.EntityName)) return;

            var comp = await _dbContext.Set<CompetitorProfile>()
                .FirstOrDefaultAsync(c => c.WorkspaceId == signal.WorkspaceId && c.CompetitorName == signal.EntityName, ct);

            if (comp == null)
            {
                comp = new CompetitorProfile
                {
                    WorkspaceId = signal.WorkspaceId,
                    CompetitorName = signal.EntityName,
                    Domain = signal.EntityName.ToLowerInvariant().Replace(" ", "") + ".com",
                    PricingSummary = signal.SignalType == ExternalSignalType.PriceChange ? signal.Description : "Standard tier pricing",
                    ObservedStrategy = signal.Title,
                    LastSignalObservedAt = signal.DetectedAt,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Set<CompetitorProfile>().Add(comp);
            }
            else
            {
                comp.LastSignalObservedAt = signal.DetectedAt;
                if (signal.SignalType == ExternalSignalType.PriceChange)
                {
                    comp.PricingSummary = signal.Description;
                }
                comp.ObservedStrategy = signal.Title;
                comp.UpdatedAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
