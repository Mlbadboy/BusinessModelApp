import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient, ApiResponse } from '../client';
import { StrategyRisk, MitigationStatus, PerformanceTrend } from '../../types/strategy';

const RISKS_QUERY_KEY = ['strategy', 'risks'];

interface RisksResponse {
  data: StrategyRisk[];
  total: number;
}

export const useStrategyRisks = () => {
  return useQuery<ApiResponse<RisksResponse>>({
    queryKey: RISKS_QUERY_KEY,
    queryFn: () => apiClient.get('/api/strategy/risks'),
  });
};

export const useCreateRisk = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (risk: Partial<StrategyRisk>) =>
      apiClient.post<StrategyRisk>('/api/strategy/risks', risk),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: RISKS_QUERY_KEY });
    },
  });
};

export const useUpdateRisk = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, updates }: { id: string; updates: Partial<StrategyRisk> }) =>
      apiClient.put<StrategyRisk>(`/api/strategy/risks/${id}`, updates),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: RISKS_QUERY_KEY });
    },
  });
};

export const useUpdateMitigationStatus = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: MitigationStatus }) =>
      apiClient.patch<StrategyRisk>(`/api/strategy/risks/${id}/mitigation-status`, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: RISKS_QUERY_KEY });
    },
  });
};

interface OpportunitiesResponse {
  data: StrategyRisk[];
  total: number;
}

const OPPORTUNITIES_QUERY_KEY = ['strategy', 'opportunities'];

export const useStrategyOpportunities = () => {
  return useQuery<ApiResponse<OpportunitiesResponse>>({
    queryKey: OPPORTUNITIES_QUERY_KEY,
    queryFn: () => apiClient.get('/api/strategy/opportunities'),
  });
};

export const useCreateOpportunity = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (opportunity: Partial<StrategyRisk>) =>
      apiClient.post<StrategyRisk>('/api/strategy/opportunities', opportunity),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: OPPORTUNITIES_QUERY_KEY });
    },
  });
};

export const useUpdateOpportunity = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, updates }: { id: string; updates: Partial<StrategyRisk> }) =>
      apiClient.put<StrategyRisk>(`/api/strategy/opportunities/${id}`, updates),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: OPPORTUNITIES_QUERY_KEY });
    },
  });
};

interface PerformanceTrendsResponse {
  data: PerformanceTrend[];
  total: number;
}

const PERFORMANCE_TRENDS_QUERY_KEY = ['strategy', 'performance-trends'];

export const usePerformanceTrends = () => {
  return useQuery<ApiResponse<PerformanceTrendsResponse>>({
    queryKey: PERFORMANCE_TRENDS_QUERY_KEY,
    queryFn: () => apiClient.get('/api/strategy/performance-trends'),
  });
};