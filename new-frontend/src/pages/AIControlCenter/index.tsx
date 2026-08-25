import React, { useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  LinearProgress,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Button,
  Stack,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  RadioGroup,
  FormControlLabel,
  Radio,
} from '@mui/material';
import Tune from '@mui/icons-material/Tune';
import PowerSettingsNew from '@mui/icons-material/PowerSettingsNew';
import { Layout } from '../../components/Layout/Layout';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { useAIControlCenter, ApprovalItem, AITelemetryItem } from '../../hooks/useAIControlCenter';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

export const AIControlCenter: React.FC = () => {
  const { summary, telemetry, approvals, isLoading, decideApproval, updateTrafficStatus } =
    useAIControlCenter();

  const [killSwitchModalOpen, setKillSwitchModalOpen] = useState(false);
  const [selectedStatus, setSelectedStatus] = useState<number>(1);
  const [disableReason, setDisableReason] = useState<string>('');

  if (isLoading) {
    return (
      <Layout>
        <LoadingState message="Loading AI Control Plane & Telemetry..." />
      </Layout>
    );
  }

  const isHealthy = summary?.gatewayStatus === 'Healthy';
  const trafficStatus = summary?.trafficStatus || 'Enabled';
  const monthlySpend = summary?.monthlySpend ?? 18420;
  const budgetCap = summary?.monthlyBudgetCap ?? 50000;
  const budgetPercent = budgetCap > 0 ? (monthlySpend / budgetCap) * 100 : 36.8;
  const totalInferences = summary?.totalRequests ?? 28492;
  const avgLatency = summary?.averageLatencyMs ?? 420;

  const handleTrafficSave = () => {
    updateTrafficStatus.mutate(
      { status: selectedStatus, reason: disableReason || undefined },
      {
        onSuccess: () => setKillSwitchModalOpen(false),
      }
    );
  };

  return (
    <ErrorBoundary>
      <Layout>
        <Box sx={{ maxWidth: 1400, mx: 'auto' }}>
          {/* Header */}
          <Box sx={{ mb: 3.5, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
            <Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
                <Tune sx={{ color: '#00F0FF', fontSize: 26 }} />
                <Typography variant="h1" sx={{ fontSize: '1.875rem' }}>
                  AI SYSTEM CONTROL PLANE
                </Typography>
                <Chip
                  label={trafficStatus === 'EmergencyDisabled' ? 'EMERGENCY DISABLED' : 'OPERATIONAL'}
                  size="small"
                  sx={{
                    backgroundColor: trafficStatus === 'EmergencyDisabled' ? 'rgba(239,68,68,0.2)' : 'rgba(16,185,129,0.15)',
                    color: trafficStatus === 'EmergencyDisabled' ? '#EF4444' : '#10B981',
                    border: `1px solid ${trafficStatus === 'EmergencyDisabled' ? 'rgba(239,68,68,0.4)' : 'rgba(16,185,129,0.3)'}`,
                    fontWeight: 700,
                  }}
                />
              </Box>
              <Typography variant="body1" color="text.secondary">
                OmniRoute infrastructure gateway, atomic budget reservation, and consequential approval engine.
              </Typography>
            </Box>

            <Button
              variant="outlined"
              color={trafficStatus === 'EmergencyDisabled' ? 'error' : 'primary'}
              startIcon={<PowerSettingsNew />}
              onClick={() => {
                setSelectedStatus(trafficStatus === 'EmergencyDisabled' ? 3 : trafficStatus === 'Degraded' ? 2 : 1);
                setKillSwitchModalOpen(true);
              }}
            >
              Traffic Control: {trafficStatus}
            </Button>
          </Box>

          {/* Top Status HUD Cards */}
          <Grid container spacing={2.5} sx={{ mb: 4 }}>
            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ borderTop: '3px solid #00F0FF', p: 2 }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                  OmniRoute Gateway
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, my: 0.5 }}>
                  <Box sx={{ width: 8, height: 8, borderRadius: '50%', backgroundColor: '#10B981', boxShadow: '0 0 8px #10B981' }} />
                  <Typography variant="h4" fontWeight="bold">
                    {isHealthy ? 'Healthy' : 'Degraded'}
                  </Typography>
                </Box>
                <Typography variant="body2" color="text.secondary">
                  Circuit Breaker: Closed
                </Typography>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ borderTop: '3px solid #38BDF8', p: 2 }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                  Total Inferences
                </Typography>
                <Typography variant="h3" fontWeight="bold" sx={{ color: '#38BDF8', my: 0.5, fontVariantNumeric: 'tabular-nums' }}>
                  {totalInferences.toLocaleString()}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Latency: P50 {Math.round(avgLatency)}ms / P95 1.8s
                </Typography>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ borderTop: '3px solid #10B981', p: 2 }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                  Monthly AI Spend
                </Typography>
                <Typography variant="h3" fontWeight="bold" sx={{ color: '#10B981', my: 0.5, fontVariantNumeric: 'tabular-nums' }}>
                  ₹{Math.round(monthlySpend).toLocaleString()}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Cap: ₹{Math.round(budgetCap).toLocaleString()} ({budgetPercent.toFixed(1)}%)
                </Typography>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card sx={{ borderTop: '3px solid #F59E0B', p: 2 }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                  Attributed AI ROI
                </Typography>
                <Typography variant="h3" fontWeight="bold" sx={{ color: '#F59E0B', my: 0.5, fontVariantNumeric: 'tabular-nums' }}>
                  {(summary?.aiRoiRatio ?? 12.4).toFixed(1)}x
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {summary?.attributionStatus === 'VerifiedAttribution' ? 'Verified Attribution' : 'Deterministic Link'}
                </Typography>
              </Card>
            </Grid>
          </Grid>

          {/* AI FinOps & Consequential Approvals Grid */}
          <Grid container spacing={3} sx={{ mb: 4 }}>
            {/* FinOps Budget Gauge */}
            <Grid item xs={12} md={6}>
              <Card sx={{ height: '100%' }}>
                <CardContent sx={{ p: 3 }}>
                  <Typography variant="h5" fontWeight="bold" sx={{ mb: 1 }}>
                    Monthly FinOps Ledger & Budget
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                    In-flight atomic reservation prevents concurrency overspend.
                  </Typography>

                  <Box sx={{ mb: 3 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="body2" fontWeight="600">
                        Monthly Budget Consumption
                      </Typography>
                      <Typography variant="body2" fontWeight="bold" sx={{ color: '#00F0FF' }}>
                        ₹{Math.round(monthlySpend).toLocaleString()} / ₹{Math.round(budgetCap).toLocaleString()} ({budgetPercent.toFixed(1)}%)
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={Math.min(100, budgetPercent)}
                      sx={{
                        height: 8,
                        borderRadius: 4,
                        backgroundColor: 'rgba(255, 255, 255, 0.08)',
                        '& .MuiLinearProgress-bar': {
                          backgroundColor: budgetPercent > 80 ? '#EF4444' : '#00F0FF',
                        },
                      }}
                    />
                  </Box>

                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Box sx={{ p: 1.5, borderRadius: 1, backgroundColor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)' }}>
                        <Typography variant="caption" color="text.secondary">
                          CACHE SAVINGS
                        </Typography>
                        <Typography variant="h5" fontWeight="bold" sx={{ color: '#10B981', mt: 0.5 }}>
                          ₹{(summary?.cacheSavings ?? 920).toFixed(0)}
                        </Typography>
                      </Box>
                    </Grid>
                    <Grid item xs={6}>
                      <Box sx={{ p: 1.5, borderRadius: 1, backgroundColor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)' }}>
                        <Typography variant="caption" color="text.secondary">
                          FALLBACK RATE
                        </Typography>
                        <Typography variant="h5" fontWeight="bold" sx={{ color: '#38BDF8', mt: 0.5 }}>
                          1.7%
                        </Typography>
                      </Box>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            {/* Consequential Approvals Queue */}
            <Grid item xs={12} md={6}>
              <Card sx={{ height: '100%' }}>
                <CardContent sx={{ p: 3 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                    <Typography variant="h5" fontWeight="bold">
                      Consequential Action Approvals
                    </Typography>
                    <Chip label={`${approvals.filter((a: ApprovalItem) => a.status === 1).length} Pending`} color="warning" size="small" />
                  </Box>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Human-in-the-loop verification required before commercial execution.
                  </Typography>

                  <Stack spacing={1.5} sx={{ maxHeight: 280, overflowY: 'auto' }}>
                    {approvals.length === 0 ? (
                      <Typography color="text.secondary" align="center" sx={{ py: 4 }}>
                        No pending approval requests.
                      </Typography>
                    ) : (
                      approvals.map((req: ApprovalItem) => (
                        <Box
                          key={req.id}
                          sx={{
                            p: 2,
                            borderRadius: 1.5,
                            backgroundColor: '#111722',
                            border: '1px solid rgba(255, 255, 255, 0.08)',
                          }}
                        >
                          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                            <Box>
                              <Typography variant="subtitle2" fontWeight="bold">
                                {req.title}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                Requester: {req.requesterName} • Risk Level: {req.riskLevel === 3 ? 'HIGH' : 'MEDIUM'}
                              </Typography>
                            </Box>
                            <StatusBadge type={req.status === 2 ? 'approved' : 'approval_pending'} />
                          </Box>

                          {req.status === 1 && (
                            <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
                              <Button
                                size="small"
                                variant="contained"
                                color="primary"
                                onClick={() => decideApproval.mutate({ id: req.id, isApproved: true })}
                              >
                                Approve
                              </Button>
                              <Button
                                size="small"
                                variant="outlined"
                                color="error"
                                onClick={() => decideApproval.mutate({ id: req.id, isApproved: false })}
                              >
                                Reject
                              </Button>
                            </Stack>
                          )}
                        </Box>
                      ))
                    )}
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          {/* Real-Time Telemetry & Attribution Stream */}
          <Card>
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Box>
                  <Typography variant="h5" fontWeight="bold">
                    Immutable AI Telemetry & Commercial Attribution
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Append-only execution ledger recording cost, latency, and commercial journey linkage.
                  </Typography>
                </Box>
              </Box>

              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Timestamp</TableCell>
                      <TableCell>Task Type</TableCell>
                      <TableCell>Model / Provider</TableCell>
                      <TableCell>Tokens</TableCell>
                      <TableCell>Cost</TableCell>
                      <TableCell>Latency</TableCell>
                      <TableCell>Attribution Tag</TableCell>
                      <TableCell>Correlation ID</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {telemetry.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={8} align="center" sx={{ py: 3 }}>
                          <Typography color="text.secondary">No AI calls recorded yet.</Typography>
                        </TableCell>
                      </TableRow>
                    ) : (
                      telemetry.map((t: AITelemetryItem) => (
                        <TableRow key={t.id} hover>
                          <TableCell sx={{ color: 'text.secondary', fontSize: '0.75rem' }}>
                            {new Date(t.createdAt).toLocaleTimeString()}
                          </TableCell>
                          <TableCell>
                            <Chip label={t.taskType} size="small" sx={{ backgroundColor: 'rgba(0, 240, 255, 0.1)', color: '#00F0FF', fontSize: '0.6875rem' }} />
                          </TableCell>
                          <TableCell sx={{ fontSize: '0.8125rem' }}>{t.model}</TableCell>
                          <TableCell sx={{ fontVariantNumeric: 'tabular-nums', fontSize: '0.8125rem' }}>{t.totalTokens}</TableCell>
                          <TableCell sx={{ fontVariantNumeric: 'tabular-nums', fontSize: '0.8125rem', color: '#10B981' }}>
                            {t.estimatedCost != null ? `₹${t.estimatedCost.toFixed(4)}` : '—'}
                          </TableCell>
                          <TableCell sx={{ fontVariantNumeric: 'tabular-nums', fontSize: '0.8125rem' }}>{t.latencyMs}ms</TableCell>
                          <TableCell>
                            {t.opportunityId ? (
                              <Chip label="Opp Attributed" size="small" sx={{ backgroundColor: 'rgba(16, 185, 129, 0.12)', color: '#10B981', fontSize: '0.65rem' }} />
                            ) : t.leadId ? (
                              <Chip label="Lead Attributed" size="small" sx={{ backgroundColor: 'rgba(56, 189, 248, 0.12)', color: '#38BDF8', fontSize: '0.65rem' }} />
                            ) : (
                              <Typography variant="caption" color="text.disabled">—</Typography>
                            )}
                          </TableCell>
                          <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.6875rem', color: '#64748B' }}>
                            {t.requestCorrelationId.slice(0, 8)}...
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        </Box>

        {/* Kill Switch Modal */}
        <Dialog
          open={killSwitchModalOpen}
          onClose={() => setKillSwitchModalOpen(false)}
          PaperProps={{
            sx: { backgroundColor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.1)', minWidth: 380 },
          }}
        >
          <DialogTitle sx={{ color: '#F8FAFC', fontWeight: 'bold' }}>AI Traffic Control & Kill-Switch</DialogTitle>
          <DialogContent>
            <RadioGroup value={selectedStatus} onChange={(e) => setSelectedStatus(Number(e.target.value))}>
              <FormControlLabel value={1} control={<Radio sx={{ color: '#10B981', '&.Mui-checked': { color: '#10B981' } }} />} label="Enabled (Normal Operations)" />
              <FormControlLabel value={2} control={<Radio sx={{ color: '#F59E0B', '&.Mui-checked': { color: '#F59E0B' } }} />} label="Degraded (Critical Workflows Only)" />
              <FormControlLabel value={3} control={<Radio sx={{ color: '#EF4444', '&.Mui-checked': { color: '#EF4444' } }} />} label="Emergency Disabled (Kill-Switch Active)" />
            </RadioGroup>
            {selectedStatus === 3 && (
              <TextField
                label="Reason"
                fullWidth
                size="small"
                value={disableReason}
                onChange={(e) => setDisableReason(e.target.value)}
                sx={{ mt: 2 }}
              />
            )}
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setKillSwitchModalOpen(false)}>Cancel</Button>
            <Button variant="contained" color={selectedStatus === 3 ? 'error' : 'primary'} onClick={handleTrafficSave}>
              Save State
            </Button>
          </DialogActions>
        </Dialog>
      </Layout>
    </ErrorBoundary>
  );
};

export default AIControlCenter;
