import { useQuery } from '@tanstack/react-query';
import api, { endpoints, handleApiError } from '../utils/api';

export interface ScoreComponentDetail {
  componentName: string;
  weightPercent: number;
  rawScore: number;
  weightedContribution: number;
  explanation: string;
}

export interface EvidenceContributor {
  entityType: string;
  entityId: string;
  name: string;
  contributionValue: number;
  contributionDetails: string;
}

export interface EvidenceRecord {
  evidenceId: string;
  evidenceType: string;
  metricKey: string;
  displayName: string;
  formattedValue: string;
  numericValue: number;
  calculationVersion: string;
  formula: string;
  period: string;
  confidenceScore: number;
  impactLevel: string;
  generatedAt: string;
  contributors: EvidenceContributor[];
}

export interface HealthSubScores {
  pipelineScore: number;
  conversionScore: number;
  velocityScore: number;
  riskScore: number;
}

export interface BusinessHealthResult {
  overallHealthScore: number;
  confidenceScore: number;
  confidenceLevel: string;
  confidenceReason: string;
  calculationVersion: string;
  generatedAt: string;
  totalPipelineValue: number;
  weightedForecastValue: number;
  closedWonRevenue: number;
  quarterlyTarget: number;
  pipelineCoverageRatio: number;
  winRate: number;
  leadQualificationRate: number;
  avgVelocityDays: number;
  stalledRiskIndex: number;
  subScores: HealthSubScores;
  componentBreakdown: ScoreComponentDetail[];
  evidenceRecords: EvidenceRecord[];
}

export interface ExecutiveRecommendation {
  title: string;
  rationale: string;
  citedEvidenceId: string;
  actionType: string;
  targetEntity: string;
  targetEntityId?: string;
}

export interface ExecutiveBriefContext {
  health: BusinessHealthResult;
  criticalRiskAlerts: string[];
  positiveMomentumSignals: string[];
  isActionRequired: boolean;
  summary: string;
  recommendations: ExecutiveRecommendation[];
}

export const useAnalytics = () => {
  // Fetch deterministic business health data
  const healthQuery = useQuery<BusinessHealthResult>({
    queryKey: ['business-health'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.analytics.businessHealth);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
    refetchInterval: 60000,
  });

  // Fetch AI Executive Brief
  const briefQuery = useQuery<ExecutiveBriefContext>({
    queryKey: ['executive-brief'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.analytics.executiveBrief);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
    refetchInterval: 120000,
  });

  return {
    businessHealth: healthQuery.data,
    isLoadingHealth: healthQuery.isLoading,
    executiveBrief: briefQuery.data,
    isLoadingBrief: briefQuery.isLoading,
    isLoading: healthQuery.isLoading || briefQuery.isLoading,
    error: healthQuery.error || briefQuery.error,
  };
};