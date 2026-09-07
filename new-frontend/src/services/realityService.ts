import axios from 'axios';

const API_BASE = '/api/reality';

export interface SystemOverview {
  tenantId: string;
  database: string;
  digitalTwin: string;
  runtime: string;
  workerFabric: string;
  brainFabric: string;
  executionFirewall: string;
  eventBus: string;
  activeMissionsCount: number;
  pendingApprovalsCount: number;
  lastRealEventAt?: string;
  connectors: ConnectorHealth[];
}

export interface ConnectorHealth {
  type: string;
  name: string;
  status: 'Connected' | 'Degraded' | 'Disconnected' | 'NotConfigured';
  lastPing?: string;
  capabilities: string[];
  diagnostics?: string;
}

export interface ApprovalRequest {
  approvalId: string;
  missionId: string;
  nodeId?: string;
  actionType: string;
  riskTier: string;
  targetResource: string;
  financialExposureINR: number;
  requestedAt: string;
  expiresAt: string;
  isExpired: boolean;
  state: string;
  payloadJson: string;
  payloadDigest: string;
  rationale: string;
  businessConstraintsSummary: string;
  assignedWorkerId?: string;
  connectorType?: string;
}

export interface MissionLedgerNode {
  nodeId: string;
  label: string;
  state: string;
  isHumanGate: boolean;
  executedAt?: string;
  outcomeSummary?: string;
}

export interface MissionLedger {
  missionId: string;
  title: string;
  objective: string;
  status: string;
  progress: string;
  createdAt: string;
  completedNodes: number;
  totalNodes: number;
  nodes: MissionLedgerNode[];
}

export interface WorkProgressSummary {
  activeMissions: number;
  waitingApproval: number;
  waitingData: number;
  running: number;
  completed: number;
  failed: number;
  blocked: number;
  cancelled: number;
}

export interface ValueRealization {
  missionId: string;
  expectedValueINR: number;
  authorizedExposureINR: number;
  actualCostINR: number;
  actualRevenueINR: number;
  actualGrossMarginINR: number;
  netRealizedValueINR?: number;
  realizedValueStatus: 'RealizedVerified' | 'PendingVerification' | 'Unknown';
  varianceINR?: number;
  evidenceLedgerId?: string;
  unverifiedReason?: string;
}

export interface ControlCenterData {
  work: WorkProgressSummary;
  missionLedgers: MissionLedger[];
  pendingApprovals: ApprovalRequest[];
  valueRealizations: ValueRealization[];
}

export const realityService = {
  async getSystemOverview(): Promise<SystemOverview> {
    const res = await axios.get<SystemOverview>(`${API_BASE}/overview`);
    return res.data;
  },

  async getControlCenter(): Promise<ControlCenterData> {
    const res = await axios.get<ControlCenterData>(`${API_BASE}/control-center`);
    return res.data;
  },

  async getApprovals(history = false): Promise<ApprovalRequest[]> {
    const res = await axios.get<ApprovalRequest[]>(`${API_BASE}/approvals?history=${history}`);
    return res.data;
  },

  async decideApproval(
    id: string,
    decision: 'Approve' | 'Reject' | 'RequestChanges',
    notes?: string,
    expectedPayloadDigest?: string
  ) {
    const res = await axios.post(`${API_BASE}/approvals/${id}/decide`, {
      decision,
      notes,
      expectedPayloadDigest,
    });
    return res.data;
  },

  async getConnectors(): Promise<ConnectorHealth[]> {
    const res = await axios.get<ConnectorHealth[]>(`${API_BASE}/connectors`);
    return res.data;
  },

  async getValueRealizations(): Promise<ValueRealization[]> {
    const res = await axios.get<ValueRealization[]>(`${API_BASE}/value`);
    return res.data;
  },
};
