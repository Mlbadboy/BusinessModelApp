import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api, { endpoints, handleApiError } from '../utils/api';

interface PerformanceTrend {
  id: string;
  metric: string;
  current: number;
  target: number;
  historical: Array<{
    period: string;
    value: number;
  }>;
  status: 'ahead' | 'on-track' | 'behind';
  analysis: string;
}

interface StrategyRisk {
  id: string;
  name: string;
  description: string;
  category: 'operational' | 'financial' | 'strategic' | 'external';
  impact: {
    severity: 'high' | 'medium' | 'low';
    description: string;
    potentialLoss: number;
  };
  probability: number;
  mitigationPlan: {
    steps: string[];
    cost: number;
    timeframe: string;
    status: 'planned' | 'in-progress' | 'completed';
  };
}

interface StrategyOpportunity {
  id: string;
  name: string;
  description: string;
  category: 'market' | 'operational' | 'technological' | 'strategic';
  impact: {
    benefit: string;
    potentialGain: number;
    timeToRealize: string;
  };
  feasibility: number;
  requirements: {
    resources: string[];
    investment: number;
    dependencies: string[];
  };
  status: 'identified' | 'evaluating' | 'pursuing' | 'realized';
}

export const useStrategy = () => {
  const queryClient = useQueryClient();

  // Fetch performance trends
  const {
    data: performanceTrends,
    isLoading: isLoadingTrends,
    error: trendsError,
  } = useQuery<PerformanceTrend[]>({
    queryKey: ['strategy-performance-trends'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.strategy.performanceTrends);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch strategy risks
  const {
    data: risks,
    isLoading: isLoadingRisks,
    error: risksError,
  } = useQuery<StrategyRisk[]>({
    queryKey: ['strategy-risks'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.strategy.risks);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch strategy opportunities
  const {
    data: opportunities,
    isLoading: isLoadingOpportunities,
    error: opportunitiesError,
  } = useQuery<StrategyOpportunity[]>({
    queryKey: ['strategy-opportunities'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.strategy.opportunities);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Update risk mitigation status
  const updateRiskMitigation = useMutation<
    StrategyRisk,
    Error,
    { id: string; status: StrategyRisk['mitigationPlan']['status'] }
  >({
    mutationFn: async ({ id, status }) => {
      const { data } = await api.patch(`${endpoints.strategy.risks}/${id}/mitigation`, { status });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['strategy-risks']);
    },
    onError: (error) => {
      throw handleApiError(error);
    },
  });

  // Update opportunity status
  const updateOpportunityStatus = useMutation<
    StrategyOpportunity,
    Error,
    { id: string; status: StrategyOpportunity['status'] }
  >({
    mutationFn: async ({ id, status }) => {
      const { data } = await api.patch(`${endpoints.strategy.opportunities}/${id}`, { status });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['strategy-opportunities']);
    },
    onError: (error) => {
      throw handleApiError(error);
    },
  });

  // Helper function to calculate risk score
  const calculateRiskScore = (risk: StrategyRisk) => {
    const severityScore = {
      high: 3,
      medium: 2,
      low: 1,
    }[risk.impact.severity];
    return severityScore * risk.probability;
  };

  // Helper function to calculate opportunity score
  const calculateOpportunityScore = (opportunity: StrategyOpportunity) => {
    return (opportunity.impact.potentialGain * opportunity.feasibility) / 100;
  };

  return {
    performanceTrends,
    risks,
    opportunities,
    updateRiskMitigation,
    updateOpportunityStatus,
    calculateRiskScore,
    calculateOpportunityScore,
    isLoading: isLoadingTrends || isLoadingRisks || isLoadingOpportunities,
    error: trendsError || risksError || opportunitiesError,
  };
};