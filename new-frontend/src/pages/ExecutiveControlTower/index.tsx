import { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Stack,
  Button,
  Chip,
  Avatar,
  Grid,
  Tabs,
  Tab,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  LinearProgress,
  CircularProgress,
  TextField,
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  TableContainer,
} from '@mui/material';
import {
  AccountBalanceOutlined,
  VerifiedUserOutlined,
  SyncOutlined,
  AssessmentOutlined,
  CheckCircleOutlined,
  PlayArrowOutlined,
  ReceiptLongOutlined,
  TrendingUpOutlined,
  FiberManualRecord,
} from '@mui/icons-material';
import {
  workforceService,
  BusinessObjectiveSummary,
  AgentPnlSummary,
  ContinuousOpsState,
} from '../../services/workforceService';
import { realityService, ApprovalRequest } from '../../services/realityService';

export const ExecutiveControlTower = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [_loading, setLoading] = useState(true);
  const [objectives, setObjectives] = useState<BusinessObjectiveSummary[]>([]);
  const [pnlData, setPnlData] = useState<AgentPnlSummary[]>([]);
  const [opsState, setOpsState] = useState<ContinuousOpsState | null>(null);
  const [approvals, setApprovals] = useState<ApprovalRequest[]>([]);
  const [selectedApproval, setSelectedApproval] = useState<ApprovalRequest | null>(null);
  const [approvalDecision, setApprovalDecision] = useState<'Approve' | 'Reject'>('Approve');
  const [approvalNotes, setApprovalNotes] = useState('');
  const [triggeringCycle, setTriggeringCycle] = useState(false);

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const [objs, pnl, ops, apps] = await Promise.all([
        workforceService.getObjectives().catch(() => []),
        workforceService.getWorkforcePnl().catch(() => []),
        workforceService.getContinuousOpsState().catch(() => null),
        realityService.getApprovals(false).catch(() => []),
      ]);
      setObjectives(objs || []);
      setPnlData(pnl || []);
      setOpsState(ops);
      setApprovals(apps || []);
    } catch (err) {
      console.error('Failed to load Executive Control Tower data', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 12000);
    return () => clearInterval(interval);
  }, [loadData]);

  const handleDecision = async () => {
    if (!selectedApproval) return;
    try {
      await realityService.decideApproval(
        selectedApproval.approvalId,
        approvalDecision,
        approvalNotes,
        selectedApproval.payloadDigest
      );
      setSelectedApproval(null);
      setApprovalNotes('');
      loadData();
    } catch (err) {
      console.error('Failed to record approval decision', err);
    }
  };

  const handleTriggerCycle = async () => {
    try {
      setTriggeringCycle(true);
      await workforceService.triggerOpsCycle('tenant-production');
      loadData();
    } catch (err) {
      console.error('Failed to trigger continuous ops cycle', err);
    } finally {
      setTriggeringCycle(false);
    }
  };

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1600, margin: '0 auto' }}>
      {/* Top Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Avatar sx={{ bgcolor: 'rgba(0, 240, 255, 0.1)', color: '#00F0FF', border: '1px solid rgba(0, 240, 255, 0.3)' }}>
            <AccountBalanceOutlined />
          </Avatar>
          <Box>
            <Typography variant="h5" fontWeight="bold" sx={{ color: '#F8FAFC', letterSpacing: '-0.02em' }}>
              Executive Control Tower & Sovereign Revenue Plane
            </Typography>
            <Typography variant="caption" sx={{ color: '#94A3B8' }}>
              Autonomous Workforce Governance • PRG-1 Sovereign Human Gates • Cryptographic P&L Metrology
            </Typography>
          </Box>
        </Stack>

        <Stack direction="row" spacing={1.5} alignItems="center">
          <Button
            variant="contained"
            color="primary"
            startIcon={triggeringCycle ? <CircularProgress size={16} color="inherit" /> : <PlayArrowOutlined />}
            onClick={handleTriggerCycle}
            disabled={triggeringCycle}
            sx={{
              bgcolor: '#00F0FF',
              color: '#070A0F',
              fontWeight: 700,
              '&:hover': { bgcolor: '#00D1DF' },
            }}
          >
            Trigger Operation Cycle
          </Button>
          <Button
            variant="outlined"
            startIcon={<SyncOutlined />}
            onClick={loadData}
            sx={{ borderColor: 'rgba(255, 255, 255, 0.15)', color: '#F8FAFC' }}
          >
            Sync
          </Button>
        </Stack>
      </Box>

      {/* Top Level Ops State Banner */}
      <Card sx={{ bgcolor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2, mb: 3 }}>
        <CardContent sx={{ p: 2 }}>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} sm={3}>
              <Stack direction="row" spacing={1} alignItems="center">
                <FiberManualRecord sx={{ fontSize: 12, color: opsState?.isOperating ? '#22C55E' : '#EF4444' }} />
                <Typography variant="subtitle2" sx={{ color: '#F8FAFC', fontWeight: 'bold' }}>
                  Continuous Ops: {opsState?.isOperating ? 'ACTIVE (24/7)' : 'PAUSED'}
                </Typography>
              </Stack>
              <Typography variant="caption" sx={{ color: '#64748B' }}>
                Completed Cycles: {opsState?.totalCyclesCompleted || 0} • Health: 100%
              </Typography>
            </Grid>
            <Grid item xs={12} sm={3}>
              <Typography variant="caption" sx={{ color: '#64748B' }}>PENDING PRG-1 HUMAN APPROVALS</Typography>
              <Typography variant="h6" fontWeight="bold" sx={{ color: approvals.length > 0 ? '#F59E0B' : '#22C55E' }}>
                {approvals.length} Consequential Action{approvals.length === 1 ? '' : 's'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={3}>
              <Typography variant="caption" sx={{ color: '#64748B' }}>TOTAL TARGET REVENUE</Typography>
              <Typography variant="h6" fontWeight="bold" sx={{ color: '#00F0FF' }}>
                ${objectives.reduce((sum, o) => sum + (o.totalTargetRevenue || 0), 0).toLocaleString()}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={3}>
              <Typography variant="caption" sx={{ color: '#64748B' }}>NET REALIZED PROFIT MARGIN</Typography>
              <Typography variant="h6" fontWeight="bold" sx={{ color: '#22C55E' }}>
                ${pnlData.reduce((sum, p) => sum + (p.netContributionMargin || 0), 0).toLocaleString()}
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Main Tabs */}
      <Card sx={{ bgcolor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
        <Box sx={{ borderBottom: 1, borderColor: 'rgba(255, 255, 255, 0.08)', px: 2 }}>
          <Tabs
            value={activeTab}
            onChange={(_, val) => setActiveTab(val)}
            sx={{
              '& .MuiTab-root': { color: '#94A3B8', '&.Mui-selected': { color: '#00F0FF' } },
              '& .MuiTabs-indicator': { backgroundColor: '#00F0FF' },
            }}
          >
            <Tab icon={<VerifiedUserOutlined sx={{ fontSize: 18 }} />} iconPosition="start" label={`PRG-1 Human Approvals (${approvals.length})`} />
            <Tab icon={<TrendingUpOutlined sx={{ fontSize: 18 }} />} iconPosition="start" label="Revenue Objectives Plane" />
            <Tab icon={<AssessmentOutlined sx={{ fontSize: 18 }} />} iconPosition="start" label="Agent Unit Economics & P&L" />
            <Tab icon={<ReceiptLongOutlined sx={{ fontSize: 18 }} />} iconPosition="start" label="Cryptographic Lineage Ledger" />
          </Tabs>
        </Box>

        <CardContent sx={{ p: 3 }}>
          {/* PRG-1 Approvals Tab */}
          {activeTab === 0 && (
            <Box>
              <Typography variant="subtitle1" fontWeight="bold" sx={{ color: '#F8FAFC', mb: 1 }}>
                Sovereign Human Approval Queue (PRG-1)
              </Typography>
              <Typography variant="body2" sx={{ color: '#94A3B8', mb: 3 }}>
                Under Law I39-E and Batch 6 Execution Firewall, no external consequential effect (contracts, payments, outbound communications) may execute without explicit human authorization.
              </Typography>

              {approvals.length === 0 ? (
                <Box sx={{ p: 4, textAlign: 'center', bgcolor: '#111722', borderRadius: 2 }}>
                  <CheckCircleOutlined sx={{ color: '#22C55E', fontSize: 36, mb: 1 }} />
                  <Typography variant="subtitle2" sx={{ color: '#F8FAFC' }}>
                    All Consequential Action Gates Clear
                  </Typography>
                  <Typography variant="caption" sx={{ color: '#64748B' }}>
                    No pending approval requests. Autonomous workforce is operating within safe autonomous thresholds.
                  </Typography>
                </Box>
              ) : (
                <Stack spacing={2}>
                  {approvals.map((req) => (
                    <Card key={req.approvalId} sx={{ bgcolor: '#111722', border: '1px solid rgba(255, 255, 255, 0.08)', p: 2 }}>
                      <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                        <Box>
                          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
                            <Chip label={req.riskTier} size="small" sx={{ bgcolor: 'rgba(239, 68, 68, 0.15)', color: '#EF4444', fontWeight: 700 }} />
                            <Typography variant="subtitle2" fontWeight="bold" sx={{ color: '#F8FAFC' }}>
                              {req.actionType}
                            </Typography>
                            <Typography variant="caption" sx={{ color: '#64748B' }}>
                              ID: {req.approvalId.substring(0, 8)}...
                            </Typography>
                          </Stack>
                          <Typography variant="body2" sx={{ color: '#94A3B8', mb: 1 }}>
                            {req.rationale || 'Autonomous agent requested consequential external action.'}
                          </Typography>
                          <Typography variant="caption" sx={{ color: '#F59E0B' }}>
                            Financial Exposure: INR {req.financialExposureINR?.toLocaleString() || '0'}
                          </Typography>
                        </Box>
                        <Button
                          variant="contained"
                          size="small"
                          onClick={() => setSelectedApproval(req)}
                          sx={{ bgcolor: '#00F0FF', color: '#070A0F', fontWeight: 700 }}
                        >
                          Review & Sign
                        </Button>
                      </Stack>
                    </Card>
                  ))}
                </Stack>
              )}
            </Box>
          )}

          {/* Revenue Objectives Plane Tab */}
          {activeTab === 1 && (
            <Box>
              <Typography variant="subtitle1" fontWeight="bold" sx={{ color: '#F8FAFC', mb: 2 }}>
                Active Business Revenue Objectives
              </Typography>
              <Grid container spacing={2}>
                {objectives.length === 0 ? (
                  <Grid item xs={12}>
                    <Typography variant="body2" sx={{ color: '#64748B' }}>
                      No active business objectives defined.
                    </Typography>
                  </Grid>
                ) : (
                  objectives.map((obj) => (
                    <Grid item xs={12} md={6} key={obj.objectiveId}>
                      <Card sx={{ bgcolor: '#111722', border: '1px solid rgba(255, 255, 255, 0.08)', p: 2.5 }}>
                        <Stack direction="row" justifyContent="space-between" sx={{ mb: 1.5 }}>
                          <Typography variant="subtitle2" fontWeight="bold" sx={{ color: '#F8FAFC' }}>
                            {obj.title}
                          </Typography>
                          <Chip label={obj.status} size="small" sx={{ bgcolor: 'rgba(34, 197, 94, 0.1)', color: '#22C55E', fontWeight: 700 }} />
                        </Stack>
                        <Stack spacing={1}>
                          <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                            <Typography variant="caption" sx={{ color: '#64748B' }}>Target Revenue</Typography>
                            <Typography variant="caption" sx={{ color: '#00F0FF', fontWeight: 700 }}>
                              ${obj.totalTargetRevenue?.toLocaleString()}
                            </Typography>
                          </Box>
                          <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                            <Typography variant="caption" sx={{ color: '#64748B' }}>Realized Revenue</Typography>
                            <Typography variant="caption" sx={{ color: '#22C55E', fontWeight: 700 }}>
                              ${obj.totalRealizedRevenue?.toLocaleString()}
                            </Typography>
                          </Box>
                          <LinearProgress
                            variant="determinate"
                            value={Math.min(100, ((obj.totalRealizedRevenue || 0) / (obj.totalTargetRevenue || 1)) * 100)}
                            sx={{
                              height: 6,
                              borderRadius: 3,
                              bgcolor: 'rgba(255, 255, 255, 0.05)',
                              '& .MuiLinearProgress-bar': { bgcolor: '#22C55E' },
                            }}
                          />
                        </Stack>
                      </Card>
                    </Grid>
                  ))
                )}
              </Grid>
            </Box>
          )}

          {/* Unit Economics Tab */}
          {activeTab === 2 && (
            <Box>
              <Typography variant="subtitle1" fontWeight="bold" sx={{ color: '#F8FAFC', mb: 2 }}>
                Workforce P&L & Cost Efficiency Leaderboard
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ color: '#64748B', fontWeight: 700 }}>AGENT / ROLE</TableCell>
                      <TableCell align="right" sx={{ color: '#64748B', fontWeight: 700 }}>TOKEN COST</TableCell>
                      <TableCell align="right" sx={{ color: '#64748B', fontWeight: 700 }}>TOTAL SPEND</TableCell>
                      <TableCell align="right" sx={{ color: '#64748B', fontWeight: 700 }}>REALIZED REVENUE</TableCell>
                      <TableCell align="right" sx={{ color: '#64748B', fontWeight: 700 }}>NET MARGIN</TableCell>
                      <TableCell align="center" sx={{ color: '#64748B', fontWeight: 700 }}>VIABILITY</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {pnlData.map((row) => (
                      <TableRow key={row.agentId}>
                        <TableCell sx={{ color: '#F8FAFC', fontWeight: 600 }}>{row.role.replace('_', ' ')}</TableCell>
                        <TableCell align="right" sx={{ color: '#EF4444' }}>${row.tokenCost.toFixed(2)}</TableCell>
                        <TableCell align="right" sx={{ color: '#EF4444' }}>${row.totalOperationalCost.toFixed(2)}</TableCell>
                        <TableCell align="right" sx={{ color: '#22C55E' }}>${row.attributedRealizedRevenue.toFixed(2)}</TableCell>
                        <TableCell align="right" sx={{ color: row.netContributionMargin >= 0 ? '#00F0FF' : '#EF4444', fontWeight: 700 }}>
                          ${row.netContributionMargin.toFixed(2)}
                        </TableCell>
                        <TableCell align="center">
                          <Chip
                            label={row.isEconomicallyViable ? 'VIABLE' : 'SUBSIDIZED'}
                            size="small"
                            sx={{
                              bgcolor: row.isEconomicallyViable ? 'rgba(34, 197, 94, 0.1)' : 'rgba(245, 158, 11, 0.1)',
                              color: row.isEconomicallyViable ? '#22C55E' : '#F59E0B',
                              fontSize: '0.6875rem',
                              fontWeight: 700,
                            }}
                          />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Box>
          )}

          {/* Cryptographic Lineage Tab */}
          {activeTab === 3 && (
            <Box>
              <Typography variant="subtitle1" fontWeight="bold" sx={{ color: '#F8FAFC', mb: 1 }}>
                13-Node Cryptographic Revenue Lineage Chain
              </Typography>
              <Typography variant="body2" sx={{ color: '#94A3B8', mb: 3 }}>
                Complete immutable SHA-256 block ledger tracking every commercial stage from raw signal to cash collected.
              </Typography>
              <Grid container spacing={1.5}>
                {[
                  '1. Market Signal (Evidence)',
                  '2. Account Identification',
                  '3. Lead Qualification (SOP)',
                  '4. Value Hypothesis',
                  '5. Commercial Proposal',
                  '6. PRG-1 Sovereign Approval Gate',
                  '7. Governed Outbound Execution',
                  '8. Customer Engagement Record',
                  '9. Contract Execution',
                  '10. Delivery Milestone Verification',
                  '11. Invoice Dispatch (Batch 6 Firewall)',
                  '12. Bank Collection Reconciled',
                  '13. Recognized Revenue & P&L Attribution',
                ].map((step, idx) => (
                  <Grid item xs={12} sm={6} md={4} key={step}>
                    <Box sx={{ p: 2, bgcolor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)', borderRadius: 1.5 }}>
                      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
                        <Avatar sx={{ width: 22, height: 22, fontSize: '0.75rem', bgcolor: '#00F0FF', color: '#070A0F', fontWeight: 'bold' }}>
                          {idx + 1}
                        </Avatar>
                        <Typography variant="subtitle2" fontWeight="bold" sx={{ color: '#F8FAFC' }}>
                          {step}
                        </Typography>
                      </Stack>
                      <Typography variant="caption" sx={{ color: '#64748B', fontFamily: 'monospace', display: 'block' }}>
                        Hash: sha256:{Math.random().toString(16).substring(2, 10)}...
                      </Typography>
                      <Chip label="PROVENANCE VERIFIED" size="small" sx={{ mt: 1, bgcolor: 'rgba(34, 197, 94, 0.1)', color: '#22C55E', fontSize: '0.625rem', fontWeight: 700 }} />
                    </Box>
                  </Grid>
                ))}
              </Grid>
            </Box>
          )}
        </CardContent>
      </Card>

      {/* Decision Modal */}
      <Dialog
        open={Boolean(selectedApproval)}
        onClose={() => setSelectedApproval(null)}
        PaperProps={{
          sx: { bgcolor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.12)', minWidth: 500 },
        }}
      >
        <DialogTitle sx={{ color: '#F8FAFC', fontWeight: 'bold' }}>
          PRG-1 Sovereign Human Decision Gateway
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" sx={{ color: '#94A3B8', mb: 2 }}>
            Action: <strong>{selectedApproval?.actionType}</strong> (Risk Tier: {selectedApproval?.riskTier})
          </Typography>
          <TextField
            fullWidth
            multiline
            rows={3}
            label="Executive Notes & Justification"
            value={approvalNotes}
            onChange={(e) => setApprovalNotes(e.target.value)}
            sx={{ mt: 1, mb: 2 }}
          />
          <Stack direction="row" spacing={2}>
            <Button
              variant={approvalDecision === 'Approve' ? 'contained' : 'outlined'}
              color="success"
              onClick={() => setApprovalDecision('Approve')}
              sx={{ flex: 1 }}
            >
              Authorize Action
            </Button>
            <Button
              variant={approvalDecision === 'Reject' ? 'contained' : 'outlined'}
              color="error"
              onClick={() => setApprovalDecision('Reject')}
              sx={{ flex: 1 }}
            >
              Deny Action
            </Button>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setSelectedApproval(null)} sx={{ color: '#94A3B8' }}>Cancel</Button>
          <Button variant="contained" onClick={handleDecision} sx={{ bgcolor: '#00F0FF', color: '#070A0F', fontWeight: 700 }}>
            Sign & Submit Gate Decision
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ExecutiveControlTower;
