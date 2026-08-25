import { useMutation, useQuery } from '@tanstack/react-query';
import { apiClient, ApiResponse } from '../client';
import { RevenueAnalysis, RevenueTrend, RevenueSourcePerformance, DateRange } from '../../types';

const REVENUE_KEYS = {
  all: ['revenue'] as const,
  analysis: () => [...REVENUE_KEYS.all, 'analysis'] as const,
  trends: () => [...REVENUE_KEYS.all, 'trends'] as const,
  sources: () => [...REVENUE_KEYS.all, 'sources'] as const,
};

export function useRevenueAnalysis(dateRange?: DateRange) {
  return useQuery<ApiResponse<RevenueAnalysis>>({
    queryKey: [...REVENUE_KEYS.analysis(), dateRange],
    queryFn: async () => {
      const params = dateRange ? {
        startDate: dateRange.startDate.toISOString(),
        endDate: dateRange.endDate.toISOString(),
      } : {};
      
      const { data } = await apiClient.get('/revenue/analysis', { params });
      return data;
    },
  });
}

export function useRevenueTrends(period: string = 'monthly') {
  return useQuery<ApiResponse<RevenueTrend[]>>({
    queryKey: [...REVENUE_KEYS.trends(), period],
    queryFn: async () => {
      const { data } = await apiClient.get('/revenue/trends', {
        params: { period },
      });
      return data;
    },
  });
}

export function useRevenueSourcePerformance() {
  return useQuery<ApiResponse<RevenueSourcePerformance[]>>({
    queryKey: REVENUE_KEYS.sources(),
    queryFn: async () => {
      const { data } = await apiClient.get('/revenue/sources/performance');
      return data;
    },
  });
}

export function useUpdateRevenueAnalysis() {
  return useMutation<
    ApiResponse<RevenueAnalysis>,
    Error,
    Partial<RevenueAnalysis>
  >({
    mutationFn: async (updates) => {
      const { data } = await apiClient.patch('/revenue/analysis', updates);
      return data;
    },
  });
}

// Add new revenue source
export function useAddRevenueSource() {
  return useMutation<
    ApiResponse<RevenueSourcePerformance>,
    Error,
    Partial<RevenueSourcePerformance>
  >({
    mutationFn: async (newSource) => {
      const { data } = await apiClient.post('/revenue/sources', newSource);
      return data;
    },
  });
}

// Update revenue source
export function useUpdateRevenueSource() {
  return useMutation<
    ApiResponse<RevenueSourcePerformance>,
    Error,
    { id: number; updates: Partial<RevenueSourcePerformance> }
  >({
    mutationFn: async ({ id, updates }) => {
      const { data } = await apiClient.patch(`/revenue/sources/${id}`, updates);
      return data;
    },
  });
}

// Revenue forecasting
export function useRevenueForecast(months: number = 12) {
  return useQuery<ApiResponse<Record<string, number>>>({
    queryKey: [...REVENUE_KEYS.all, 'forecast', months],
    queryFn: async () => {
      const { data } = await apiClient.get('/revenue/forecast', {
        params: { months },
      });
      return data;
    },
  });
}