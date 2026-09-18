import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Stack,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Alert,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField
} from '@mui/material';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import VerifiedUserIcon from '@mui/icons-material/VerifiedUser';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import SecurityIcon from '@mui/icons-material/Security';
import AssignmentTurnedInIcon from '@mui/icons-material/AssignmentTurnedIn';
import { Layout } from '../../components/Layout/Layout';
import {
  commercialOperationsService,
  CommercialPipelineMetrics,
  GroundedOpportunity,
  CommercialProposal,
  AuthoritativeDealContract,
  CommercialInvoice,
  CashCollectionReceipt,
  RevenueLineageAuditReport
} from '../../services/commercialOperationsService';

export const CommercialRevenueOperations: React.FC = () => {
  const [metrics, setMetrics] = useState<CommercialPipelineMetrics | null>(null);
  const [opportunities, setOpportunities] = useState<GroundedOpportunity[]>([]);
  const [proposals, setProposals] = useState<CommercialProposal[]>([]);
  const [contracts, setContracts] = useState<AuthoritativeDealContract[]>([]);
  const [invoices, setInvoices] = useState<CommercialInvoice[]>([]);
  const [receipts, setReceipts] = useState<CashCollectionReceipt[]>([]);
  const [selectedAudit, setSelectedAudit] = useState<RevenueLineageAuditReport | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [approvalModalOpen, setApprovalModalOpen] = useState<boolean>(false);
  const [selectedProposalId, setSelectedProposalId] = useState<string>('');
  const [humanSignoffId, setHumanSignoffId] = useState<string>('PRG1-CHIEF-EXEC');
  const [auditLoading, setAuditLoading] = useState<boolean>(false);

  const loadData = async () => {
    try {
      setLoading(true);
      const [m, opps, props, contrs, invs, recs] = await Promise.all([
        commercialOperationsService.getMetrics().catch(() => null),
        commercialOperationsService.listOpportunities().catch(() => []),
        commercialOperationsService.listProposals().catch(() => []),
        commercialOperationsService.listContracts().catch(() => []),
        commercialOperationsService.listInvoices().catch(() => []),
        commercialOperationsService.listReceipts().catch(() => [])
      ]);
      setMetrics(m);
      setOpportunities(opps);
      setProposals(props);
      setContracts(contrs);
      setInvoices(invs);
      setReceipts(recs);
    } catch (err) {
      console.error('Failed to load commercial operations data', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleApproveProposal = async () => {
    if (!selectedProposalId || !humanSignoffId) return;
    try {
      await commercialOperationsService.approveProposal(selectedProposalId, humanSignoffId);
      setApprovalModalOpen(false);
      loadData();
    } catch (e) {
      alert('Approval failed: Gross margin must be >= 35% and signoff ID must be non-empty.');
    }
  };

  const handleAuditReceipt = async (receiptId: string) => {
    try {
      setAuditLoading(true);
      const audit = await commercialOperationsService.traceRevenueLineage(receiptId);
      setSelectedAudit(audit);
    } catch (e) {
      alert('Lineage tracing failed.');
    } finally {
      setAuditLoading(false);
    }
  };

  if (loading && !metrics) {
    return (
      <Layout>
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '60vh' }}>
          <CircularProgress />
        </Box>
      </Layout>
    );
  }

  return (
    <Layout>
      <Box sx={{ p: 3, maxWidth: 1440, margin: '0 auto' }}>
        {/* Header Banner */}
        <Box sx={{ mb: 4, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Box>
            <Typography variant="h4" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <AccountBalanceIcon color="primary" fontSize="large" />
              Commercial Revenue Operations Control Tower
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Law I40 Governed Autonomous Revenue Engine • Real Business Opportunity to Bank-Verified Cash Lineage
            </Typography>
          </Box>
          <Button variant="outlined" onClick={loadData}>
            Refresh State
          </Button>
        </Box>

        {/* Top KPI Metrics Cards */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ bgcolor: 'background.paper', boxShadow: 2 }}>
              <CardContent>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="overline" color="text.secondary">Total Pipeline</Typography>
                  <TrendingUpIcon color="info" />
                </Stack>
                <Typography variant="h5" sx={{ fontWeight: 700, mt: 1 }}>
                  ₹{(metrics?.totalPipelineValueINR ?? 0).toLocaleString()}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {metrics?.totalOpportunities ?? 0} Active Opportunities
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ bgcolor: 'background.paper', boxShadow: 2 }}>
              <CardContent>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="overline" color="text.secondary">Won Contracts</Typography>
                  <AssignmentTurnedInIcon color="success" />
                </Stack>
                <Typography variant="h5" sx={{ fontWeight: 700, mt: 1 }}>
                  ₹{(metrics?.totalWonDealsValueINR ?? 0).toLocaleString()}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Cryptographically Verified
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ bgcolor: 'background.paper', boxShadow: 2 }}>
              <CardContent>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="overline" color="text.secondary">Collected Cash</Typography>
                  <AccountBalanceIcon color="primary" />
                </Stack>
                <Typography variant="h5" sx={{ fontWeight: 700, mt: 1, color: 'primary.main' }}>
                  ₹{(metrics?.totalCollectedCashINR ?? 0).toLocaleString()}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Authoritative Bank Realized
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ bgcolor: 'background.paper', boxShadow: 2 }}>
              <CardContent>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="overline" color="text.secondary">Avg Gross Margin</Typography>
                  <SecurityIcon color={((metrics?.averageGrossMarginPercent ?? 0) >= 35) ? 'success' : 'error'} />
                </Stack>
                <Typography variant="h5" sx={{ fontWeight: 700, mt: 1 }}>
                  {metrics?.averageGrossMarginPercent ?? 0}%
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Floor: 35.0% Invariant Gate
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Section 1: Grounded Opportunities */}
        <Paper sx={{ p: 3, mb: 4, borderRadius: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
            Factual Grounded Commercial Opportunities
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Company Name</TableCell>
                  <TableCell>Identified Problem</TableCell>
                  <TableCell>Estimated Deal (INR)</TableCell>
                  <TableCell>ICP Score</TableCell>
                  <TableCell>Evidence Grounding</TableCell>
                  <TableCell>Next Action</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {opportunities.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center">No active grounded opportunities found.</TableCell>
                  </TableRow>
                ) : (
                  opportunities.map((opp) => (
                    <TableRow key={opp.opportunityId}>
                      <TableCell sx={{ fontWeight: 600 }}>{opp.companyName}</TableCell>
                      <TableCell>{opp.identifiedProblem}</TableCell>
                      <TableCell>₹{opp.estimatedDealValueINR.toLocaleString()}</TableCell>
                      <TableCell>
                        <Chip
                          label={`${(opp.icpScore * 100).toFixed(0)}%`}
                          color={opp.icpScore >= 0.7 ? 'success' : 'default'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={opp.isGrounded ? `${opp.corroboratedEvidenceIds.length} Citations` : 'Ungrounded'}
                          color={opp.isGrounded ? 'primary' : 'error'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>{opp.recommendedNextAction || 'Formulate Account Strategy'}</TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>

        {/* Section 2: Governed Proposals & Margin Invariants */}
        <Paper sx={{ p: 3, mb: 4, borderRadius: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
            Governed Commercial Proposals & Pricing Governance
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Title</TableCell>
                  <TableCell>Timeline</TableCell>
                  <TableCell>Net Price (INR)</TableCell>
                  <TableCell>Delivery Cost</TableCell>
                  <TableCell>Gross Margin</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Action</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {proposals.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center">No active proposals in current cycle.</TableCell>
                  </TableRow>
                ) : (
                  proposals.map((p) => (
                    <TableRow key={p.proposalId}>
                      <TableCell sx={{ fontWeight: 600 }}>{p.title}</TableCell>
                      <TableCell>{p.estimatedTimelineWeeks} Weeks</TableCell>
                      <TableCell>₹{p.netPriceINR.toLocaleString()}</TableCell>
                      <TableCell>₹{p.estimatedDeliveryCostINR.toLocaleString()}</TableCell>
                      <TableCell>
                        <Chip
                          label={`${p.expectedGrossMarginPercent.toFixed(1)}%`}
                          color={p.isMarginCompliant ? 'success' : 'error'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        {p.isApprovedForSubmission ? (
                          <Chip label="PRG-1 Approved" color="primary" size="small" />
                        ) : (
                          <Chip label="Pending Human Signoff" variant="outlined" size="small" />
                        )}
                      </TableCell>
                      <TableCell align="right">
                        {!p.isApprovedForSubmission && (
                          <Button
                            size="small"
                            variant="contained"
                            disabled={!p.isMarginCompliant}
                            onClick={() => {
                              setSelectedProposalId(p.proposalId);
                              setApprovalModalOpen(true);
                            }}
                          >
                            PRG-1 Signoff
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>

        {/* Section 3: Authoritative Contracts & Issued Invoices */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 3, borderRadius: 2, height: '100%' }}>
              <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
                Cryptographic Contracts ({contracts.length})
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Signer</TableCell>
                      <TableCell>Deal Value</TableCell>
                      <TableCell>System</TableCell>
                      <TableCell>Status</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {contracts.length === 0 ? (
                      <TableRow><TableCell colSpan={4} align="center">No contracts executed.</TableCell></TableRow>
                    ) : (
                      contracts.map(c => (
                        <TableRow key={c.contractId}>
                          <TableCell sx={{ fontWeight: 600 }}>{c.customerSignerName}</TableCell>
                          <TableCell>₹{c.bindingDealValueINR.toLocaleString()}</TableCell>
                          <TableCell>{c.verificationSourceSystem}</TableCell>
                          <TableCell>
                            <Chip label={c.isCryptographicallyVerified ? 'Verified' : 'Invalid'} color={c.isCryptographicallyVerified ? 'success' : 'error'} size="small" />
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </Paper>
          </Grid>

          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 3, borderRadius: 2, height: '100%' }}>
              <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
                Invoices & Batch 6 Permits ({invoices.length})
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Invoice #</TableCell>
                      <TableCell>Total (INR)</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Batch 6 Permit</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {invoices.length === 0 ? (
                      <TableRow><TableCell colSpan={4} align="center">No invoices issued.</TableCell></TableRow>
                    ) : (
                      invoices.map(inv => (
                        <TableRow key={inv.invoiceId}>
                          <TableCell sx={{ fontWeight: 600 }}>{inv.invoiceNumber}</TableCell>
                          <TableCell>₹{inv.totalAmountINR.toLocaleString()}</TableCell>
                          <TableCell><Chip label={inv.status} size="small" /></TableCell>
                          <TableCell sx={{ fontFamily: 'monospace' }}>{inv.batch6PermitId || 'None'}</TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </Paper>
          </Grid>
        </Grid>

        {/* Section 4: Invoices & Cash Collections */}
        <Paper sx={{ p: 3, mb: 4, borderRadius: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
            Invoicing & Authoritative Bank Cash Receipts
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Receipt ID</TableCell>
                  <TableCell>Invoice ID</TableCell>
                  <TableCell>Collected Amount</TableCell>
                  <TableCell>Bank Reference</TableCell>
                  <TableCell>Payment Rail</TableCell>
                  <TableCell>Bank Verified</TableCell>
                  <TableCell align="right">Causal Audit</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {receipts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center">No cash collection receipts yet recorded.</TableCell>
                  </TableRow>
                ) : (
                  receipts.map((r) => (
                    <TableRow key={r.receiptId}>
                      <TableCell sx={{ fontFamily: 'monospace' }}>{r.receiptId.substring(0, 10)}...</TableCell>
                      <TableCell sx={{ fontFamily: 'monospace' }}>{r.invoiceId.substring(0, 10)}...</TableCell>
                      <TableCell sx={{ fontWeight: 700 }}>₹{r.collectedAmountINR.toLocaleString()}</TableCell>
                      <TableCell sx={{ fontFamily: 'monospace' }}>{r.bankReferenceNumber}</TableCell>
                      <TableCell>{r.gatewayOrRailId}</TableCell>
                      <TableCell>
                        {r.isBankVerified ? (
                          <Chip label="Verified" color="success" size="small" icon={<VerifiedUserIcon />} />
                        ) : (
                          <Chip label="Unverified" color="warning" size="small" />
                        )}
                      </TableCell>
                      <TableCell align="right">
                        <Button
                          size="small"
                          variant="outlined"
                          disabled={auditLoading}
                          onClick={() => handleAuditReceipt(r.receiptId)}
                        >
                          {auditLoading ? 'Tracing...' : 'Trace Lineage'}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>

        {/* Section 3: Causal Revenue Lineage Audit View */}
        {selectedAudit && (
          <Paper sx={{ p: 3, mb: 4, borderRadius: 2, bgcolor: 'background.default', border: '1px solid', borderColor: selectedAudit.isLineageUnbroken ? 'success.main' : 'error.main' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                Cryptographic Revenue Lineage Audit: ₹{selectedAudit.realizedAmountINR.toLocaleString()}
              </Typography>
              <Chip
                label={selectedAudit.isLineageUnbroken ? 'UNBROKEN CAUSAL LINEAGE CERTIFIED' : 'LINEAGE BROKEN / DEFECTS DETECTED'}
                color={selectedAudit.isLineageUnbroken ? 'success' : 'error'}
                sx={{ fontWeight: 700 }}
              />
            </Box>

            {selectedAudit.defects.length > 0 && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {selectedAudit.defects.map((d, i) => (
                  <div key={i}>• {d}</div>
                ))}
              </Alert>
            )}

            <Grid container spacing={2}>
              {selectedAudit.traceNodes.map((node, index) => (
                <Grid item xs={12} sm={6} md={2} key={index}>
                  <Card sx={{ p: 1.5, height: '100%', border: '1px solid', borderColor: node.isVerified ? 'success.light' : 'error.light' }}>
                    <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 700 }}>
                      Step {index + 1}: {node.stage}
                    </Typography>
                    <Typography variant="body2" sx={{ fontFamily: 'monospace', fontSize: '0.75rem', mt: 0.5 }}>
                      ID: {node.entityId.substring(0, 8)}...
                    </Typography>
                    <Box sx={{ mt: 1 }}>
                      <Chip
                        label={node.isVerified ? 'VERIFIED' : 'UNVERIFIED'}
                        color={node.isVerified ? 'success' : 'error'}
                        size="small"
                        sx={{ fontSize: '0.65rem' }}
                      />
                    </Box>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </Paper>
        )}

        {/* PRG-1 Signoff Modal */}
        <Dialog open={approvalModalOpen} onClose={() => setApprovalModalOpen(false)}>
          <DialogTitle>PRG-1 Sovereign Human Proposal Signoff</DialogTitle>
          <DialogContent sx={{ minWidth: 400, pt: 2 }}>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Per Constitutional Law I40, commercial proposals cannot be submitted externally without authoritative human executive authorization.
            </Typography>
            <TextField
              label="Human Executive Signoff ID"
              fullWidth
              value={humanSignoffId}
              onChange={(e) => setHumanSignoffId(e.target.value)}
              margin="dense"
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setApprovalModalOpen(false)}>Cancel</Button>
            <Button variant="contained" onClick={handleApproveProposal}>
              Authorize Submission
            </Button>
          </DialogActions>
        </Dialog>
      </Box>
    </Layout>
  );
};

export default CommercialRevenueOperations;
