import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../utils/api';

export interface ConnectorSummary {
  provider: string;
  displayName: string;
  category: string;
  status: 'Disconnected' | 'Configured' | 'Authenticated' | 'Healthy' | 'Degraded' | 'Expired' | 'Revoked' | 'Error';
  accountIdentifier?: string | null;
  capabilities: Record<string, string>;
  probesPassed: number;
  totalProbes: number;
  isHealthy: boolean;
  lastHealthCheckAt?: string | null;
  tokenExpiresAt?: string | null;
}

export interface ConnectorHealthProbe {
  probeId: string;
  name: string;
  passed: boolean;
  details?: string | null;
  latencyMs: number;
}

export interface ConnectorHealthReport {
  provider: string | number;
  status: string | number;
  probesPassed: number;
  totalProbes: number;
  isHealthy: boolean;
  probes: ConnectorHealthProbe[];
  checkedAt: string;
}

export interface ConfigureKeysPayload {
  apiKey?: string;
  apiSecret?: string;
  accountIdentifier?: string;
  initialCapabilities?: Record<string, number>;
}

export interface UpdateCapabilitiesPayload {
  capabilities: Record<string, number>; // 0 = Denied, 1 = RequireApproval, 2 = Allowed
}

export const useConnect = () => {
  const queryClient = useQueryClient();

  const connectorsQuery = useQuery<ConnectorSummary[]>({
    queryKey: ['connectors'],
    queryFn: async () => {
      const response = await api.get('/connectors');
      return response.data;
    },
    refetchInterval: 15000,
  });

  const testConnector = useMutation<ConnectorHealthReport, Error, string>({
    mutationFn: async (provider: string) => {
      const response = await api.post(`/connectors/${provider}/test`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['connectors'] });
    },
  });

  const configureKeys = useMutation<any, Error, { provider: string; data: ConfigureKeysPayload }>({
    mutationFn: async ({ provider, data }) => {
      const response = await api.post(`/connectors/${provider}/configure-keys`, data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['connectors'] });
    },
  });

  const handleOAuthCallback = useMutation<any, Error, { provider: string; code: string; state?: string }>({
    mutationFn: async ({ provider, code, state }) => {
      const response = await api.post(`/connectors/${provider}/oauth/callback`, { code, state });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['connectors'] });
    },
  });

  const updateCapabilities = useMutation<any, Error, { provider: string; capabilities: Record<string, number> }>({
    mutationFn: async ({ provider, capabilities }) => {
      const response = await api.patch(`/connectors/${provider}/capabilities`, { capabilities });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['connectors'] });
    },
  });

  const disconnectConnector = useMutation<any, Error, string>({
    mutationFn: async (provider: string) => {
      const response = await api.post(`/connectors/${provider}/disconnect`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['connectors'] });
    },
  });

  return {
    connectors: connectorsQuery.data || [],
    isLoading: connectorsQuery.isLoading,
    isError: connectorsQuery.isError,
    refetch: connectorsQuery.refetch,
    testConnector,
    configureKeys,
    handleOAuthCallback,
    updateCapabilities,
    disconnectConnector,
  };
};
