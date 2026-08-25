import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../utils/api';

export interface AISummary {
  gatewayStatus: string;
  monthlySpend: number;
  monthlyBudgetCap: number;
  budgetPercentConsumed: number;
  totalRequests: number;
  totalTokens: number;
  averageLatencyMs: number;
  fallbackCount: number;
  cacheHits: number;
  cacheSavings: number;
  attributedWonRevenue: number;
  aiRoiRatio: number;
}

export interface AITelemetryItem {
  id: string;
  taskType: string;
  provider: string;
  model: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  estimatedCost: number | null;
  latencyMs: number;
  cacheHit: boolean;
  fallbackAttempts: number;
  requestCorrelationId: string;
  createdAt: string;
}

export interface ApprovalItem {
  id: string;
  actionType: string;
  title: string;
  requesterName: string;
  contextDataJson: string;
  riskLevel: number;
  status: number;
  createdAt: string;
}

export const useAIControlCenter = () => {
  const queryClient = useQueryClient();

  const summaryQuery = useQuery({
    queryKey: ['ai-control-center', 'summary'],
    queryFn: async () => {
      const response = await api.get<AISummary>('/ai-control-center/summary');
      return response.data;
    },
    refetchInterval: 10000,
  });

  const telemetryQuery = useQuery({
    queryKey: ['ai-control-center', 'telemetry'],
    queryFn: async () => {
      const response = await api.get<AITelemetryItem[]>('/ai-control-center/telemetry?limit=25');
      return response.data;
    },
    refetchInterval: 10000,
  });

  const approvalsQuery = useQuery({
    queryKey: ['ai-control-center', 'approvals'],
    queryFn: async () => {
      const response = await api.get<ApprovalItem[]>('/ai-control-center/approvals');
      return response.data;
    },
    refetchInterval: 10000,
  });

  const decideApproval = useMutation({
    mutationFn: async ({ id, isApproved, decisionNote }: { id: string; isApproved: boolean; decisionNote?: string }) => {
      const response = await api.post(`/ai-control-center/approvals/${id}/decide`, { isApproved, decisionNote });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-control-center', 'approvals'] });
    },
  });

  return {
    summary: summaryQuery.data,
    telemetry: telemetryQuery.data ?? [],
    approvals: approvalsQuery.data ?? [],
    isLoading: summaryQuery.isLoading || telemetryQuery.isLoading,
    decideApproval,
  };
};
