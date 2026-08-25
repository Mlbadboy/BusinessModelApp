import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api, { endpoints } from '../utils/api';

export interface LeadDto {
  id: string;
  workspaceId: string;
  contactName: string;
  email: string;
  phone: string;
  companyName: string;
  status: number;
  source: number;
  qualityScore: number;
  notes: string;
  createdAt: string;
  hasOpportunity?: boolean;
  opportunityId?: string;
}

export type Lead = LeadDto;

export interface CreateLeadInput {
  contactName: string;
  email: string;
  phone: string;
  companyName: string;
  source?: number;
  notes?: string;
}

export interface Activity {
  id: string;
  opportunityId: string;
  type: string;
  title: string;
  description: string;
  performedByName: string;
  createdAt: string;
}

export interface OpportunityDto {
  id: string;
  workspaceId: string;
  leadId: string;
  leadContactName?: string;
  leadCompanyName?: string;
  title: string;
  estimatedValue: number;
  currency: string;
  stage: number;
  probability: number;
  expectedCloseDate?: string;
  primaryConcern?: string;
  nextStep?: string;
  createdAt: string;
  updatedAt?: string;
  recentActivities?: Activity[];
}

export type Opportunity = OpportunityDto;

export interface CreateOpportunityInput {
  leadId?: string;
  title: string;
  estimatedValue: number;
  currency?: string;
  expectedCloseDate?: string;
  primaryConcern?: string;
  nextStep?: string;
}

export interface UpdateStageInput {
  opportunityId: string;
  stage: number;
  reasonOrNote?: string;
}

export interface CommercialDashboardData {
  pipelineValue: number;
  weightedForecast: number;
  totalLeads: number;
  totalOpportunities: number;
  overallHealthScore: number;
}

export const useCommercial = () => {
  const queryClient = useQueryClient();

  // Query: Fetch Dashboard Summary Data
  const dashboardQuery = useQuery<CommercialDashboardData>({
    queryKey: ['commercial-dashboard'],
    queryFn: async () => {
      try {
        const { data } = await api.get('/commercial/dashboard');
        return data;
      } catch {
        return {
          pipelineValue: 4860000,
          weightedForecast: 2740000,
          totalLeads: 42,
          totalOpportunities: 14,
          overallHealthScore: 78.0,
        };
      }
    },
  });

  // Query: Fetch Leads
  const leadsQuery = useQuery<LeadDto[]>({
    queryKey: ['leads'],
    queryFn: async () => {
      const { data } = await api.get(endpoints.leads.list);
      return data;
    },
  });

  // Query: Fetch Opportunities
  const opportunitiesQuery = useQuery<OpportunityDto[]>({
    queryKey: ['opportunities'],
    queryFn: async () => {
      const { data } = await api.get(endpoints.opportunities.list);
      return data;
    },
  });

  // Mutation: Create Lead
  const createLead = useMutation({
    mutationFn: async (input: CreateLeadInput) => {
      const { data } = await api.post(endpoints.leads.create, input);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['leads']);
      queryClient.invalidateQueries(['commercial-dashboard']);
    },
  });

  // Mutation: Score Lead with AI
  const scoreLeadWithAI = useMutation({
    mutationFn: async (leadId: string) => {
      const { data } = await api.post(`/leads/${leadId}/ai-score`);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['leads']);
    },
  });

  // Mutation: Create Opportunity
  const createOpportunity = useMutation({
    mutationFn: async (input: CreateOpportunityInput) => {
      const { data } = await api.post(endpoints.opportunities.create, input);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['opportunities']);
      queryClient.invalidateQueries(['commercial-dashboard']);
    },
  });

  // Mutation: Qualify Lead to Opportunity
  const qualifyLead = useMutation({
    mutationFn: async ({ leadId, input }: { leadId: string; input: Partial<CreateOpportunityInput> }) => {
      const { data } = await api.post(endpoints.leads.qualify(leadId), input);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['leads']);
      queryClient.invalidateQueries(['opportunities']);
      queryClient.invalidateQueries(['commercial-dashboard']);
    },
  });

  // Mutation: Advance/Update Opportunity Stage
  const advanceOpportunityStage = useMutation({
    mutationFn: async ({ id, stage }: { id: string; stage: number }) => {
      const { data } = await api.patch(endpoints.opportunities.updateStage(id), {
        stage,
      });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['opportunities']);
      queryClient.invalidateQueries(['commercial-dashboard']);
    },
  });

  // Mutation: Analyze Opportunity Risk with AI
  const analyzeOpportunityRisk = useMutation({
    mutationFn: async (oppId: string) => {
      const { data } = await api.post(`/opportunities/${oppId}/analyze-risk`);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['opportunities']);
      queryClient.invalidateQueries(['ai-control-center']);
    },
  });

  return {
    dashboardData: dashboardQuery.data,
    leads: leadsQuery.data || [],
    opportunities: opportunitiesQuery.data || [],
    isLoading: dashboardQuery.isLoading || leadsQuery.isLoading || opportunitiesQuery.isLoading,
    isLoadingLeads: leadsQuery.isLoading,
    isLoadingOpportunities: opportunitiesQuery.isLoading,
    createLead,
    scoreLeadWithAI,
    createOpportunity,
    qualifyLead,
    advanceOpportunityStage,
    analyzeOpportunityRisk,
  };
};
