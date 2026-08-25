import { useQuery } from '@tanstack/react-query';
import api, { endpoints, handleApiError } from '../utils/api';

interface FinancialPerformance {
  revenue: number;
  expenses: number;
  profit: number;
  profitMargin: number;
  period: string;
  trends: {
    revenueGrowth: number;
    expenseGrowth: number;
    profitGrowth: number;
  };
}

interface BusinessHealth {
  cashflow: {
    current: number;
    projected: number;
    trend: 'up' | 'down' | 'stable';
  };
  customerMetrics: {
    acquisitionCost: number;
    lifetimeValue: number;
    retentionRate: number;
  };
  operationalEfficiency: {
    resourceUtilization: number;
    productivityScore: number;
    processEfficiency: number;
  };
}

export const useAnalytics = () => {
  // Fetch financial performance data
  const {
    data: financialPerformance,
    isLoading: isLoadingFinancial,
    error: financialError,
  } = useQuery<FinancialPerformance>({
    queryKey: ['financial-performance'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.analytics.financialPerformance);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
    refetchInterval: 300000, // Refetch every 5 minutes
  });

  // Fetch business health data
  const {
    data: businessHealth,
    isLoading: isLoadingHealth,
    error: healthError,
  } = useQuery<BusinessHealth>({
    queryKey: ['business-health'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.analytics.businessHealth);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
    refetchInterval: 300000, // Refetch every 5 minutes
  });

  // Calculate key performance indicators
  const kpis = financialPerformance
    ? {
        netProfitMargin: (financialPerformance.profit / financialPerformance.revenue) * 100,
        revenueGrowth: financialPerformance.trends.revenueGrowth,
        operatingEfficiency: businessHealth?.operationalEfficiency.processEfficiency,
        customerLifetimeValue: businessHealth?.customerMetrics.lifetimeValue,
      }
    : null;

  return {
    financialPerformance,
    businessHealth,
    kpis,
    isLoading: isLoadingFinancial || isLoadingHealth,
    error: financialError || healthError,
  };
};