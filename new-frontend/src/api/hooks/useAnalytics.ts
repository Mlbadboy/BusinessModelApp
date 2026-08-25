import { useMutation, useQuery } from '@tanstack/react-query';
import { apiClient, ApiResponse } from '../client';
import { BusinessHealth, FinancialPerformance, DateRange } from '../../types';

const ANALYTICS_KEYS = {
  all: ['analytics'] as const,
  financialPerformance: () => [...ANALYTICS_KEYS.all, 'financial-performance'] as const,
  businessHealth: () => [...ANALYTICS_KEYS.all, 'business-health'] as const,
};

export function useFinancialPerformance(dateRange?: DateRange) {
  return useQuery<ApiResponse<FinancialPerformance>>({
    queryKey: [...ANALYTICS_KEYS.financialPerformance(), dateRange],
    queryFn: async () => {
      const params = dateRange ? {
        startDate: dateRange.startDate.toISOString(),
        endDate: dateRange.endDate.toISOString(),
      } : {};
      
      const { data } = await apiClient.get('/analytics/financial-performance', { params });
      return data;
    },
  });
}

export function useBusinessHealth() {
  return useQuery<ApiResponse<BusinessHealth>>({
    queryKey: ANALYTICS_KEYS.businessHealth(),
    queryFn: async () => {
      const { data } = await apiClient.get('/analytics/business-health');
      return data;
    },
  });
}

export function useUpdateBusinessHealth() {
  return useMutation<
    ApiResponse<BusinessHealth>,
    Error,
    Partial<BusinessHealth>
  >({
    mutationFn: async (updates) => {
      const { data } = await apiClient.patch('/analytics/business-health', updates);
      return data;
    },
  });
}

// Financial performance comparison across different periods
export function useFinancialComparison(periods: DateRange[]) {
  return useQuery<ApiResponse<FinancialPerformance[]>>({
    queryKey: [...ANALYTICS_KEYS.financialPerformance(), 'comparison', periods],
    queryFn: async () => {
      const { data } = await apiClient.post('/analytics/financial-comparison', { periods });
      return data;
    },
  });
}

// Custom metrics tracking
export function useCustomMetrics(metricKeys: string[]) {
  return useQuery<ApiResponse<Record<string, number>>>({
    queryKey: [...ANALYTICS_KEYS.all, 'custom-metrics', metricKeys],
    queryFn: async () => {
      const { data } = await apiClient.get('/analytics/custom-metrics', {
        params: { metrics: metricKeys.join(',') },
      });
      return data;
    },
  });
}