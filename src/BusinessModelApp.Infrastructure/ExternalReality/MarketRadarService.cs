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
