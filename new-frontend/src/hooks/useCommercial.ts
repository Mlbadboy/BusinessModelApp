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

export interface EvidenceRecord {
  evidenceId: string;
  evidenceType: string;
  displayName: string;
  formattedValue: string;
  numericValue: number;
  formula: string;
  confidenceScore: number;
  impactLevel: string;
}

export interface BusinessHealthData {
  overallHealthScore: number;
  confidenceScore: number;
  confidenceLevel: string;
  totalPipelineValue: number;
  weightedForecastValue: number;
  closedWonRevenue: number;
  quarterlyTarget: number;
  pipelineCoverageRatio: number;
  winRate: number;
  leadQualificationRate: number;
  avgVelocityDays: number;
  stalledRiskIndex: number;
  subScores: {
    pipelineScore: number;
    conversionScore: number;
    velocityScore: number;
    riskScore: number;
  };
  evidenceRecords: EvidenceRecord[];
}

export interface CommercialDashboardData {
  pipelineValue: number;
  weightedForecast: number;
  closedWonRevenue: number;
  totalLeads: number;
  totalOpportunities: number;
  overallHealthScore: number;
  healthResult?: BusinessHealthData;
}

export const useCommercial = () => {
  const queryClient = useQueryClient();

  // Query: Fetch Real Dashboard Business Health Data
  const dashboardQuery = useQuery<CommercialDashboardData>({
    queryKey: ['commercial-dashboard'],
    queryFn: async () => {
      try {
        const { data: health } = await api.get<BusinessHealthData>('/analytics/business-health');
        const [{ data: leads }, { data: opps }] = await Promise.all([
          api.get<LeadDto[]>(endpoints.leads.list).catch(() => ({ data: [] as LeadDto[] })),
          api.get<OpportunityDto[]>(endpoints.opportunities.list).catch(() => ({ data: [] as OpportunityDto[] })),
        ]);

        return {
          pipelineValue: health?.totalPipelineValue ?? 0,
          weightedForecast: health?.weightedForecastValue ?? 0,
          closedWonRevenue: health?.closedWonRevenue ?? 0,
          totalLeads: leads?.length ?? 0,
          totalOpportunities: opps?.length ?? 0,
          overallHealthScore: Math.round(health?.overallHealthScore ?? 0),
          healthResult: health,
        };
      } catch {
        return {
          pipelineValue: 0,
          weightedForecast: 0,
          closedWonRevenue: 0,
          totalLeads: 0,
          totalOpportunities: 0,
          overallHealthScore: 0,
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
      const stageMap: Record<string, number> = {
        Discovery: 0,
        Proposal: 1,
        Negotiation: 2,
        ClosedWon: 3,
        ClosedLost: 4,
        '0': 0,
        '1': 1,
        '2': 2,
        '3': 3,
        '4': 4,
      };
      return (data || []).map((opp: any) => ({
        ...opp,
        stage: typeof opp.stage === 'string' && stageMap[opp.stage] !== undefined
          ? stageMap[opp.stage]
          : typeof opp.stage === 'number'
          ? opp.stage
          : 0,
      }));
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
