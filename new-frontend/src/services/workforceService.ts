import axios from 'axios';

// ─── Interfaces ──────────────────────────────────────────────────────────────

export interface BrainSpaceAgentNode {
  agentId: string;
  role: string;
  assignedMissionId?: string;
  operationalState: string;
  allocatedBudget: number;
  spentBudget: number;
  riskTier: string;
  healthScore: number;
  activeMemoryAllocatedBytes: number;
  parentAgentId?: string;
  currentTask?: string;
  revenueGenerated: number;
}

export interface BrainSpaceTelemetry {
  tenantId: string;
  systemTelemetry: {
    cpuUtilizationPercent: number;
    ramAllocatedMb: number;
    eventQueueDepth: number;
    activeAgentsCount: number;
    governedToolsExecutionRps: number;
    errorRatePercent: number;
    isExecutionFirewallArmed: boolean;
    uptimeSeconds: number;
  };
  businessTelemetry: {
    activeRevenueMissionsCount: number;
    totalPipelineValue: number;
    totalRealizedRevenue: number;
    totalOperationalCost: number;
    netRealizedMargin: number;
    unverifiedClaimedRevenue: number;
    cryptographicLineageCount: number;
    sovereignApprovalPendingCount: number;
  };
  workforceTopology: BrainSpaceAgentNode[];
  capturedAtUtc: string;
}

export interface BusinessObjectiveSummary {
  objectiveId: string;
  title: string;
  timeHorizon: string;
  status: string;
  totalTargetRevenue: number;
  totalRealizedRevenue: number;
  targetOperatingMargin: number;
  targetGrossMargin: number;
}

export interface AgentPnlSummary {
  agentId: string;
  role: string;
  attributedRealizedRevenue: number;
  tokenCost: number;
  toolCost: number;
  allocatedCost: number;
  totalOperationalCost: number;
  netContributionMargin: number;
  netContributionRatio: number;
  costEfficiencyScore: number;
  isEconomicallyViable: boolean;
}

export interface CryptographicLineageEntry {
  nodeId: string;
  nodeType: string;
  recordId: string;
  hashDigest: string;
  previousHashDigest: string;
  claimedByAgentId: string;
  verifiedByHuman?: string;
  isVerified: boolean;
  revenueImpact: number;
  timestampUtc: string;
}

export interface ContinuousOpsState {
  isOperating: boolean;
  activeCycleNumber: number;
  lastHeartbeatUtc: string;
  consecutiveHealthyCycles: number;
  totalCyclesCompleted: number;
  state: string;
}

// ─── Workforce Service ───────────────────────────────────────────────────────

export const workforceService = {
  // Telemetry / Brain Space
  async getBrainSpaceSnapshot(): Promise<BrainSpaceTelemetry> {
    const res = await axios.get<BrainSpaceTelemetry>('/api/brain-space/snapshot');
    return res.data;
  },

  async getBrainSpaceSummary() {
    const res = await axios.get('/api/brain-space/summary');
    return res.data;
  },

  // Workforce & Objectives
  async getObjectives(): Promise<BusinessObjectiveSummary[]> {
    const res = await axios.get<BusinessObjectiveSummary[]>('/api/workforce/objectives');
    return res.data;
  },

  async getConstitutionInvariants(): Promise<{ invariantCode: string; description: string; nonNegotiable: boolean }[]> {
    const res = await axios.get('/api/workforce/constitution/invariants');
    return res.data;
  },

  // Economics & P&L
  async getWorkforcePnl(timeHorizon = 'CURRENT_CYCLE'): Promise<AgentPnlSummary[]> {
    const res = await axios.get<AgentPnlSummary[]>(`/api/workforce/economics/pnl?timeHorizon=${timeHorizon}`);
    return res.data;
  },

  // Continuous Operations
  async getContinuousOpsState(): Promise<ContinuousOpsState> {
    const res = await axios.get<ContinuousOpsState>('/api/continuous-operations/state');
    return res.data;
  },

  async triggerOpsCycle(tenantId: string) {
    const res = await axios.post('/api/continuous-operations/run-cycle', { tenantId });
    return res.data;
  },

  // Revenue Control Plane
  async getRevenuePlaneSummary() {
    const res = await axios.get('/api/revenue-control-plane/summary');
    return res.data;
  },

  // Lineage
  async getLineageChain(rootId: string): Promise<CryptographicLineageEntry[]> {
    const res = await axios.get<CryptographicLineageEntry[]>(`/api/commercial/lineage/chain/${rootId}`);
    return res.data;
  }
};
