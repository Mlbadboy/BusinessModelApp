import axios from 'axios';

export interface CommercialPipelineMetrics {
  tenantId: string;
  totalOpportunities: number;
  totalPipelineValueINR: number;
  totalWonDealsValueINR: number;
  totalInvoicedValueINR: number;
  totalCollectedCashINR: number;
  averageGrossMarginPercent: number;
  stalledOpportunitiesCount: number;
  activeProposalsCount: number;
  pendingInvoicesCount: number;
  computedAtUtc: string;
}

export interface GroundedOpportunity {
  opportunityId: string;
  tenantId: string;
  accountId: string;
  companyName: string;
  corroboratedEvidenceIds: string[];
  identifiedProblem: string;
  quantifiedBusinessImpactINR: number;
  icpScore: number;
  commercialFitScore: number;
  estimatedDealValueINR: number;
  riskScore: number;
  confidenceScore: number;
  recommendedNextAction: string;
  evaluationExplanation: string;
  isGrounded: boolean;
  discoveredAtUtc: string;
}

export interface CommercialProposal {
  proposalId: string;
  tenantId: string;
  opportunityId: string;
  title: string;
  scopeSummary: string;
  deliverables: string[];
  estimatedTimelineWeeks: number;
  basePriceINR: number;
  discountPercent: number;
  netPriceINR: number;
  estimatedDeliveryCostINR: number;
  expectedGrossMarginPercent: number;
  isMarginCompliant: boolean;
  isApprovedForSubmission: boolean;
  createdAtUtc: string;
}

export interface AuthoritativeDealContract {
  contractId: string;
  tenantId: string;
  opportunityId: string;
  proposalId: string;
  customerSignerName: string;
  customerSignerEmail: string;
  signatureDigestSha256: string;
  verificationSourceSystem: string;
  bindingDealValueINR: number;
  isCryptographicallyVerified: boolean;
  executedAtUtc: string;
}

export interface CommercialInvoice {
  invoiceId: string;
  tenantId: string;
  opportunityId: string;
  contractId: string;
  workOrderId: string;
  customerId: string;
  invoiceNumber: string;
  subtotalINR: number;
  taxRatePercent: number;
  taxAmountINR: number;
  totalAmountINR: number;
  status: string;
  isBatch6Authorized: boolean;
  batch6PermitId: string;
  createdAtUtc: string;
  issuedAtUtc?: string;
  dueDateUtc: string;
}

export interface CashCollectionReceipt {
  receiptId: string;
  tenantId: string;
  invoiceId: string;
  contractId: string;
  opportunityId: string;
  collectedAmountINR: number;
  bankReferenceNumber: string;
  gatewayOrRailId: string;
  bankConfirmationDigestSha256: string;
  verifiedAtUtc: string;
  isBankVerified: boolean;
}

export interface RevenueLineageNode {
  stage: string;
  entityId: string;
  evidenceDigestSha256: string;
  recordedAtUtc: string;
  isVerified: boolean;
}

export interface RevenueLineageAuditReport {
  lineageId: string;
  tenantId: string;
  receiptId: string;
  realizedAmountINR: number;
  isLineageUnbroken: boolean;
  traceNodes: RevenueLineageNode[];
  defects: string[];
  auditedAtUtc: string;
}

export const commercialOperationsService = {
  async getMetrics(): Promise<CommercialPipelineMetrics> {
    const res = await axios.get<CommercialPipelineMetrics>('/api/commercial/operations/metrics');
    return res.data;
  },

  async listOpportunities(): Promise<GroundedOpportunity[]> {
    const res = await axios.get<GroundedOpportunity[]>('/api/commercial/operations/opportunities');
    return res.data;
  },

  async listProposals(): Promise<CommercialProposal[]> {
    const res = await axios.get<CommercialProposal[]>('/api/commercial/operations/proposals');
    return res.data;
  },

  async approveProposal(proposalId: string, humanSignoffId: string): Promise<boolean> {
    const res = await axios.post<{ success: boolean }>(`/api/commercial/operations/proposals/${proposalId}/approve`, {
      humanSignoffId
    });
    return res.data.success;
  },

  async listContracts(): Promise<AuthoritativeDealContract[]> {
    const res = await axios.get<AuthoritativeDealContract[]>('/api/commercial/operations/contracts');
    return res.data;
  },

  async listInvoices(): Promise<CommercialInvoice[]> {
    const res = await axios.get<CommercialInvoice[]>('/api/commercial/operations/invoices');
    return res.data;
  },

  async issueInvoice(invoiceId: string, batch6PermitId: string): Promise<CommercialInvoice> {
    const res = await axios.post<CommercialInvoice>(`/api/commercial/operations/invoices/${invoiceId}/issue`, {
      batch6PermitId
    });
    return res.data;
  },

  async listReceipts(): Promise<CashCollectionReceipt[]> {
    const res = await axios.get<CashCollectionReceipt[]>('/api/commercial/operations/receipts');
    return res.data;
  },

  async traceRevenueLineage(receiptId: string): Promise<RevenueLineageAuditReport> {
    const res = await axios.get<RevenueLineageAuditReport>(`/api/commercial/operations/lineage/${receiptId}`);
    return res.data;
  }
};
