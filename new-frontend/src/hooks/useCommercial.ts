import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api, { endpoints } from '../utils/api';

export interface Lead {
  id: string;
  workspaceId: string;
  contactName: string;
  email: string;
  phone: string;
  companyName: string;
  status: string;
  source: string;
  qualityScore: number;
  notes: string;
  createdAt: string;
  hasOpportunity: boolean;
  opportunityId?: string;
}

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

export interface Opportunity {
  id: string;
  workspaceId: string;
  leadId: string;
  leadContactName: string;
  leadCompanyName: string;
  title: string;
  estimatedValue: number;
  currency: string;
  stage: string;
  probability: number;
  expectedCloseDate?: string;
  primaryConcern: string;
  nextStep: string;
  createdAt: string;
  updatedAt: string;
  recentActivities: Activity[];
}

export interface CreateOpportunityInput {
  leadId: string;
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

export const useCommercial = () => {
  const queryClient = useQueryClient();

  // Query: Fetch Leads
  const leadsQuery = useQuery<Lead[]>({
    queryKey: ['leads'],
    queryFn: async () => {
      const { data } = await api.get(endpoints.leads.list);
      return data;
    },
  });

  // Query: Fetch Opportunities
  const opportunitiesQuery = useQuery<Opportunity[]>({
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
    },
  });

  // Mutation: Update Opportunity Stage
  const updateStage = useMutation({
    mutationFn: async ({ opportunityId, stage, reasonOrNote }: UpdateStageInput) => {
      const { data } = await api.patch(endpoints.opportunities.updateStage(opportunityId), {
        stage,
        reasonOrNote,
      });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['opportunities']);
    },
  });

  return {
    leads: leadsQuery.data || [],
    isLoadingLeads: leadsQuery.isLoading,
    opportunities: opportunitiesQuery.data || [],
    isLoadingOpportunities: opportunitiesQuery.isLoading,
    createLead,
    qualifyLead,
    updateStage,
  };
};
