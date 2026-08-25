import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api, { endpoints, handleApiError } from '../utils/api';

interface RevenueTrend {
  period: string;
  amount: number;
  growth: number;
  sources: Array<{
    name: string;
    amount: number;
    percentage: number;
  }>;
}

interface RevenueRisk {
  id: string;
  name: string;
  impact: 'high' | 'medium' | 'low';
  probability: number;
  potentialLoss: number;
  mitigationStrategy: string;
}

interface RevenueOpportunity {
  id: string;
  name: string;
  potentialGain: number;
  probability: number;
  implementationCost: number;
  timeframe: string;
  strategy: string;
}

interface RevenueAnalysis {
  currentRevenue: number;
  projectedRevenue: number;
  growthRate: number;
  diversification: {
    score: number;
    recommendations: string[];
  };
  seasonality: {
    pattern: string;
    peaks: string[];
    troughs: string[];
  };
}

interface RevenueSourcePerformance {
  id: string;
  name: string;
  currentRevenue: number;
  historicalTrend: Array<{
    period: string;
    amount: number;
  }>;
  profitMargin: number;
  customerSatisfaction: number;
}

export const useRevenue = () => {
  const queryClient = useQueryClient();

  // Fetch revenue trends
  const {
    data: revenueTrends,
    isLoading: isLoadingTrends,
    error: trendsError,
  } = useQuery<RevenueTrend[]>({
    queryKey: ['revenue-trends'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.revenue.trends);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch revenue risks
  const {
    data: revenueRisks,
    isLoading: isLoadingRisks,
    error: risksError,
  } = useQuery<RevenueRisk[]>({
    queryKey: ['revenue-risks'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.revenue.risks);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch revenue opportunities
  const {
    data: revenueOpportunities,
    isLoading: isLoadingOpportunities,
    error: opportunitiesError,
  } = useQuery<RevenueOpportunity[]>({
    queryKey: ['revenue-opportunities'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.revenue.opportunities);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch revenue analysis
  const {
    data: revenueAnalysis,
    isLoading: isLoadingAnalysis,
    error: analysisError,
  } = useQuery<RevenueAnalysis>({
    queryKey: ['revenue-analysis'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.revenue.analysis);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Update revenue source performance
  const updateSourcePerformance = useMutation<
    RevenueSourcePerformance,
    Error,
    { id: string; updates: Partial<RevenueSourcePerformance> }
  >({
    mutationFn: async ({ id, updates }) => {
      const { data } = await api.patch(`${endpoints.revenue.sourcePerformance}/${id}`, updates);
      return data;
    },
    onSuccess: () => {
      // Invalidate relevant queries
      queryClient.invalidateQueries(['revenue-trends']);
      queryClient.invalidateQueries(['revenue-analysis']);
    },
    onError: (error) => {
      throw handleApiError(error);
    },
  });

  return {
    revenueTrends,
    revenueRisks,
    revenueOpportunities,
    revenueAnalysis,
    updateSourcePerformance,
    isLoading: isLoadingTrends || isLoadingRisks || isLoadingOpportunities || isLoadingAnalysis,
    error: trendsError || risksError || opportunitiesError || analysisError,
  };
};