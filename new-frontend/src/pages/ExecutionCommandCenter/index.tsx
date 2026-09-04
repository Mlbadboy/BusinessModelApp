import React, { useState, useCallback } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Chip,
  LinearProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Button,
  Tabs,
  Tab,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  Tooltip,
  Avatar,
  Stack,
  Divider,
  Badge,
} from '@mui/material';
import {
  PlayCircleOutlined,
  PauseCircleOutlined,
  StopCircleOutlined,
  CheckCircleOutlined,
  ErrorOutlined,
  WarningAmberOutlined,
  GavelRounded,
  AccountBalanceWalletOutlined,
  VerifiedUserOutlined,
  HistoryOutlined,
  PowerSettingsNew,
  VisibilityOutlined,
  ThumbUpOutlined,
  ThumbDownOutlined,
  AccessTimeOutlined,
  SpeedOutlined,
} from '@mui/icons-material';

// ─── Mock Data ───────────────────────────────────────────────────────────────

const MISSIONS = [
  {
    id: 'M-2024-001',
    name: 'Receivables Recovery Q4',
    agent: 'FinanceAgent-α',
    status: 'RUNNING',
    step: 'Sending payment reminder — 14/38 contacts',
    progress: 37,
    startedAt: '2024-01-15T09:12:00Z',
    riskTier: 'R2',
    budgetUsed: 1240,
    budgetTotal: 5000,
  },
  {
    id: 'M-2024-002',
    name: 'Q1 Customer Onboarding',
    agent: 'CRMAgent-β',
    status: 'AWAITING_APPROVAL',
    step: 'Waiting for approval: Send contract to Apex Innovations',
    progress: 52,
    startedAt: '2024-01-15T08:00:00Z',
    riskTier: 'R4',
    budgetUsed: 0,
    budgetTotal: 2000,
  },
  {
    id: 'M-2024-003',
    name: 'Churn Prevention — Tier 1',
    agent: 'RetentionAgent-γ',
    status: 'SUCCEEDED',
    step: 'Completed — 6 customers retained',
    progress: 100,
    startedAt: '2024-01-14T14:30:00Z',
    riskTier: 'R1',
    budgetUsed: 480,
    budgetTotal: 1500,
  },
  {
    id: 'M-2024-004',
    name: 'Vendor Invoice Reconciliation',
    agent: 'FinanceAgent-α',
    status: 'FAILED',
    step: 'Failed: Budget limit exceeded — ₹8,200 > ₹8,000 cap',
    progress: 78,
    startedAt: '2024-01-15T07:00:00Z',
    riskTier: 'R3',
    budgetUsed: 8000,
    budgetTotal: 8000,
  },
];

const APPROVAL_QUEUE = [
  {
    id: 'APQ-001',
    missionId: 'M-2024-002',
    action: 'Dispatch Contract — Apex Innovations Ltd.',
    agent: 'CRMAgent-β',
    riskTier: 'R4',
    amount: '₹4,25,000',
    payloadDigest: 'sha256:a3f2c8d19e4b7f6a...',
    requestedAt: '2024-01-15T08:52:00Z',
    expiresAt: '2024-01-15T09:52:00Z',
    details: 'Send signed service agreement to legal@apexinnovations.com — 12-month SaaS contract.',
  },
  {
    id: 'APQ-002',
    missionId: 'M-2024-005',
    action: 'Wire Transfer — Supplier Payment',
    agent: 'FinanceAgent-α',
    riskTier: 'R3',
    amount: '₹72,000',
    payloadDigest: 'sha256:b9d4e21f3c7a8b5e...',
    requestedAt: '2024-01-15T09:05:00Z',
    expiresAt: '2024-01-15T10:05:00Z',
    details: 'Process pending invoice INV-2024-0087 to Infra Supply Co. — overdue 14 days.',
  },
];

const EXECUTION_HISTORY = [
  { id: 'EX-1901', mission: 'Receivables Recovery Q4', action: 'Email — payment reminder', status: 'SUCCEEDED', ts: '09:14:33', risk: 'R2', permit: 'sha256:7f3a...' },
  { id: 'EX-1900', mission: 'Receivables Recovery Q4', action: 'Email — payment reminder', status: 'SUCCEEDED', ts: '09:13:58', risk: 'R2', permit: 'sha256:2b8c...' },
  { id: 'EX-1899', mission: 'Churn Prevention — Tier 1', action: 'CRM update — churn flag cleared', status: 'SUCCEEDED', ts: '14:58:02', risk: 'R1', permit: 'sha256:9d1e...' },
  { id: 'EX-1898', mission: 'Vendor Invoice Reconciliation', action: 'Payment wire attempt', status: 'DENIED', ts: '07:44:21', risk: 'R3', permit: '—' },
  { id: 'EX-1897', mission: 'Q1 Customer Onboarding', action: 'CRM contact created', status: 'SUCCEEDED', ts: '08:03:11', risk: 'R1', permit: 'sha256:5c4f...' },
];

const BUDGET_SUMMARY = {
  totalDelegated: 50000,
  totalUtilized: 9720,
  totalPending: 4970,
  dailyExposure: 14690,
  dailyLimit: 25000,
};

// ─── Helpers ─────────────────────────────────────────────────────────────────

const statusColor = (status: string) => {
  switch (status) {
    case 'RUNNING': return '#00F0FF';
    case 'SUCCEEDED': return '#22C55E';
    case 'FAILED': return '#EF4444';
    case 'AWAITING_APPROVAL': return '#F59E0B';
    case 'DENIED': return '#EF4444';
    default: return '#94A3B8';
  }
};

const riskColor = (risk: string) => {
  switch (risk) {
    case 'R0': return '#22C55E';
    case 'R1': return '#84CC16';
    case 'R2': return '#F59E0B';
    case 'R3': return '#F97316';
    case 'R4': return '#EF4444';
    case 'R5': return '#DC2626';
    default: return '#94A3B8';
  }
};

const StatusIcon = ({ status }: { status: string }) => {
  switch (status) {
    case 'RUNNING': return <PlayCircleOutlined sx={{ color: '#00F0FF', fontSize: 18 }} />;
    case 'SUCCEEDED': return <CheckCircleOutlined sx={{ color: '#22C55E', fontSize: 18 }} />;
    case 'FAILED': return <ErrorOutlined sx={{ color: '#EF4444', fontSize: 18 }} />;
    case 'AWAITING_APPROVAL': return <WarningAmberOutlined sx={{ color: '#F59E0B', fontSize: 18 }} />;
    case 'DENIED': return <StopCircleOutlined sx={{ color: '#EF4444', fontSize: 18 }} />;
    default: return <PauseCircleOutlined sx={{ color: '#94A3B8', fontSize: 18 }} />;
  }
};

// ─── Sub-components ───────────────────────────────────────────────────────────

const MetricCard = ({
  icon,
  label,
  value,
  sub,
  color = '#00F0FF',
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub?: string;
  color?: string;
}) => (
  <Card
    sx={{
      background: 'linear-gradient(135deg, rgba(15,23,42,0.95) 0%, rgba(22,33,57,0.9) 100%)',
      border: `1px solid ${color}22`,
      borderRadius: 3,
      transition: 'border-color 0.2s, box-shadow 0.2s',
      '&:hover': { borderColor: `${color}55`, boxShadow: `0 0 20px ${color}18` },
    }}
  >
    <CardContent sx={{ p: 2.5 }}>
      <Stack direction="row" alignItems="center" spacing={1.5} mb={1}>
        <Avatar sx={{ width: 36, height: 36, backgroundColor: `${color}18`, color }}>{icon}</Avatar>
        <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase' }}>
          {label}
        </Typography>
      </Stack>
      <Typography variant="h5" fontWeight="bold" sx={{ color: '#F8FAFC' }}>{value}</Typography>
      {sub && <Typography variant="caption" sx={{ color: '#64748B' }}>{sub}</Typography>}
    </CardContent>
  </Card>
);

const KillSwitchButton = ({
  label,
  scope,
  color,
  onKill,
}: {
  label: string;
  scope: string;
  color: string;
  onKill: (scope: string) => void;
}) => (
  <Button
    variant="outlined"
    size="small"
    startIcon={<PowerSettingsNew />}
    onClick={() => onKill(scope)}
    sx={{
      borderColor: color,
      color,
      borderRadius: 2,
      fontWeight: 700,
      fontSize: '0.7rem',
      letterSpacing: '0.05em',
      '&:hover': { backgroundColor: `${color}18`, borderColor: color },
    }}
  >
    {label}
  </Button>
);

// ─── Main Component ───────────────────────────────────────────────────────────

export default function ExecutionCommandCenter() {
  const [tab, setTab] = useState(0);
  const [approvalDialog, setApprovalDialog] = useState<(typeof APPROVAL_QUEUE)[0] | null>(null);
  const [killDialog, setKillDialog] = useState<string | null>(null);
  const [approvalResult, setApprovalResult] = useState<{ id: string; result: 'approved' | 'rejected' } | null>(null);

  const handleApprovalAction = useCallback((id: string, result: 'approved' | 'rejected') => {
    setApprovalResult({ id, result });
    setApprovalDialog(null);
  }, []);

  const handleKill = useCallback((scope: string) => {
    setKillDialog(scope);
  }, []);

  const confirmKill = useCallback(() => {
    // In production this would call the ExecutionCommandCenter API
    setKillDialog(null);
  }, []);

  const exposurePct = Math.round((BUDGET_SUMMARY.dailyExposure / BUDGET_SUMMARY.dailyLimit) * 100);

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1400, mx: 'auto' }}>
      {/* ── Header ── */}
      <Stack direction="row" alignItems="center" spacing={2} mb={3}>
        <Avatar sx={{ width: 44, height: 44, background: 'linear-gradient(135deg, #00F0FF, #8B5CF6)', boxShadow: '0 0 18px rgba(0,240,255,0.35)' }}>
          <GavelRounded sx={{ color: '#0F172A', fontSize: 24 }} />
        </Avatar>
        <Box>
          <Typography variant="h5" fontWeight="bold" sx={{ color: '#F8FAFC' }}>
            Execution Command Center
          </Typography>
          <Typography variant="caption" sx={{ color: '#64748B', letterSpacing: '0.06em', fontWeight: 600 }}>
            GOVERNED AUTONOMOUS EXECUTION — ALL ACTIONS REQUIRE FIREWALL CLEARANCE
          </Typography>
        </Box>
        <Box flex={1} />
        <Badge badgeContent={APPROVAL_QUEUE.length} color="warning">
          <Chip
            icon={<WarningAmberOutlined />}
            label="Pending Approvals"
            size="small"
            onClick={() => setTab(2)}
            sx={{ backgroundColor: '#F59E0B18', color: '#F59E0B', border: '1px solid #F59E0B44', fontWeight: 700, cursor: 'pointer' }}
          />
        </Badge>
      </Stack>

      {/* ── Kill Switch Bar ── */}
      <Card
        sx={{
          background: 'linear-gradient(90deg, rgba(239,68,68,0.08) 0%, rgba(15,23,42,0.95) 100%)',
          border: '1px solid rgba(239,68,68,0.25)',
          borderRadius: 3,
          mb: 3,
        }}
      >
        <CardContent sx={{ py: 1.5, px: 2.5 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} alignItems="center" spacing={1.5} flexWrap="wrap">
            <Stack direction="row" alignItems="center" spacing={1}>
              <PowerSettingsNew sx={{ color: '#EF4444', fontSize: 18 }} />
              <Typography variant="caption" fontWeight={700} sx={{ color: '#EF4444', letterSpacing: '0.06em' }}>
                EMERGENCY KILL CONTROLS
              </Typography>
            </Stack>
            <Divider orientation="vertical" flexItem sx={{ borderColor: 'rgba(239,68,68,0.2)' }} />
            <KillSwitchButton label="⚠ STOP ALL" scope="PLATFORM" color="#EF4444" onKill={handleKill} />
            <KillSwitchButton label="Stop Tenant" scope="TENANT" color="#F97316" onKill={handleKill} />
            <KillSwitchButton label="Stop Mission" scope="MISSION" color="#F59E0B" onKill={handleKill} />
            <KillSwitchButton label="Stop Agent" scope="AGENT" color="#EAB308" onKill={handleKill} />
            <KillSwitchButton label="Stop Capability" scope="CAPABILITY" color="#84CC16" onKill={handleKill} />
          </Stack>
        </CardContent>
      </Card>

      {/* ── Metrics Row ── */}
      <Grid container spacing={2} mb={3}>
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            icon={<PlayCircleOutlined fontSize="small" />}
            label="Active Missions"
            value={`${MISSIONS.filter(m => m.status === 'RUNNING').length}`}
            sub={`${MISSIONS.length} total missions today`}
            color="#00F0FF"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            icon={<AccountBalanceWalletOutlined fontSize="small" />}
            label="Budget Utilized"
            value={`₹${BUDGET_SUMMARY.totalUtilized.toLocaleString()}`}
            sub={`₹${BUDGET_SUMMARY.totalDelegated.toLocaleString()} total delegated`}
            color="#8B5CF6"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            icon={<SpeedOutlined fontSize="small" />}
            label="Daily Exposure"
            value={`${exposurePct}%`}
            sub={`₹${BUDGET_SUMMARY.dailyExposure.toLocaleString()} / ₹${BUDGET_SUMMARY.dailyLimit.toLocaleString()}`}
            color={exposurePct > 80 ? '#EF4444' : '#F59E0B'}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            icon={<VerifiedUserOutlined fontSize="small" />}
            label="Firewall Permits Issued"
            value="1,901"
            sub="0 unauthorized bypass attempts"
            color="#22C55E"
          />
        </Grid>
      </Grid>

      {/* ── Daily Exposure Gauge ── */}
      <Card
        sx={{
          background: 'rgba(15,23,42,0.95)',
          border: '1px solid rgba(255,255,255,0.07)',
          borderRadius: 3,
          mb: 3,
        }}
      >
        <CardContent sx={{ p: 2.5 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
            <Typography variant="caption" fontWeight={700} sx={{ color: '#64748B', letterSpacing: '0.06em', textTransform: 'uppercase' }}>
              Daily Financial Exposure Gauge
            </Typography>
            <Chip
              size="small"
              label={`${exposurePct}% utilized`}
              sx={{
                backgroundColor: exposurePct > 80 ? '#EF444418' : '#F59E0B18',
                color: exposurePct > 80 ? '#EF4444' : '#F59E0B',
                fontWeight: 700,
                fontSize: '0.7rem',
              }}
            />
          </Stack>
          <LinearProgress
            variant="determinate"
            value={Math.min(exposurePct, 100)}
            sx={{
              height: 10,
              borderRadius: 5,
              backgroundColor: 'rgba(255,255,255,0.07)',
              '& .MuiLinearProgress-bar': {
                borderRadius: 5,
                background: exposurePct > 80
                  ? 'linear-gradient(90deg, #F97316, #EF4444)'
                  : 'linear-gradient(90deg, #00F0FF, #8B5CF6)',
              },
            }}
          />
          <Stack direction="row" justifyContent="space-between" mt={0.75}>
            <Typography variant="caption" sx={{ color: '#64748B' }}>₹0</Typography>
            <Typography variant="caption" sx={{ color: '#64748B' }}>Limit: ₹{BUDGET_SUMMARY.dailyLimit.toLocaleString()}</Typography>
          </Stack>
        </CardContent>
      </Card>

      {/* ── Tabs ── */}
      <Box sx={{ borderBottom: '1px solid rgba(255,255,255,0.08)', mb: 3 }}>
        <Tabs
          value={tab}
          onChange={(_, v) => setTab(v)}
          sx={{
            '& .MuiTab-root': { color: '#64748B', fontWeight: 600, textTransform: 'none', minWidth: 130 },
            '& .Mui-selected': { color: '#00F0FF' },
            '& .MuiTabs-indicator': { backgroundColor: '#00F0FF' },
          }}
        >
          <Tab icon={<PlayCircleOutlined fontSize="small" />} iconPosition="start" label="Live Missions" id="ecc-tab-0" />
          <Tab icon={<HistoryOutlined fontSize="small" />} iconPosition="start" label="Execution History" id="ecc-tab-1" />
          <Tab
            icon={
              <Badge badgeContent={APPROVAL_QUEUE.length} color="warning">
                <VerifiedUserOutlined fontSize="small" />
              </Badge>
            }
            iconPosition="start"
            label="Approval Queue"
            id="ecc-tab-2"
          />
        </Tabs>
      </Box>

      {/* ── Tab: Live Missions ── */}
      {tab === 0 && (
        <Box>
          {approvalResult && (
            <Alert
              severity={approvalResult.result === 'approved' ? 'success' : 'warning'}
              sx={{ mb: 2, borderRadius: 2, backgroundColor: approvalResult.result === 'approved' ? '#22C55E18' : '#F59E0B18', border: 'none' }}
              onClose={() => setApprovalResult(null)}
            >
              Action <strong>{approvalResult.id}</strong> — {approvalResult.result === 'approved' ? 'Approved and forwarded to Execution Firewall.' : 'Rejected. Mission step cancelled.'}
            </Alert>
          )}
          <Stack spacing={2}>
            {MISSIONS.map(m => (
              <Card
                key={m.id}
                sx={{
                  background: 'linear-gradient(135deg, rgba(15,23,42,0.97) 0%, rgba(22,33,57,0.92) 100%)',
                  border: `1px solid ${statusColor(m.status)}22`,
                  borderRadius: 3,
                  '&:hover': { borderColor: `${statusColor(m.status)}44`, boxShadow: `0 0 18px ${statusColor(m.status)}14` },
                  transition: 'all 0.2s',
                }}
              >
                <CardContent sx={{ p: 2.5 }}>
                  <Stack direction={{ xs: 'column', md: 'row' }} alignItems={{ md: 'center' }} spacing={2}>
                    <Box flex={1}>
                      <Stack direction="row" alignItems="center" spacing={1} mb={0.5}>
                        <StatusIcon status={m.status} />
                        <Typography variant="subtitle2" fontWeight={700} sx={{ color: '#F8FAFC' }}>
                          {m.name}
                        </Typography>
                        <Chip
                          size="small"
                          label={m.riskTier}
                          sx={{
                            backgroundColor: `${riskColor(m.riskTier)}18`,
                            color: riskColor(m.riskTier),
                            fontWeight: 700,
                            fontSize: '0.65rem',
                            height: 20,
                          }}
                        />
                      </Stack>
                      <Typography variant="caption" sx={{ color: '#64748B' }}>
                        {m.agent} · {m.id} · {m.step}
                      </Typography>
                      <Box mt={1.5}>
                        <LinearProgress
                          variant="determinate"
                          value={m.progress}
                          sx={{
                            height: 6,
                            borderRadius: 3,
                            backgroundColor: 'rgba(255,255,255,0.06)',
                            '& .MuiLinearProgress-bar': {
                              borderRadius: 3,
                              backgroundColor: statusColor(m.status),
                            },
                          }}
                        />
                        <Typography variant="caption" sx={{ color: '#475569', mt: 0.5, display: 'block' }}>
                          {m.progress}% complete · Budget ₹{m.budgetUsed.toLocaleString()} / ₹{m.budgetTotal.toLocaleString()}
                        </Typography>
                      </Box>
                    </Box>
                    <Stack direction="row" spacing={1} alignItems="center">
                      {m.status === 'AWAITING_APPROVAL' && (
                        <Button
                          variant="contained"
                          size="small"
                          startIcon={<VerifiedUserOutlined />}
                          onClick={() => {
                            const item = APPROVAL_QUEUE.find(a => a.missionId === m.id);
                            if (item) setApprovalDialog(item);
                          }}
                          sx={{
                            background: 'linear-gradient(135deg, #F59E0B, #F97316)',
                            color: '#0F172A',
                            fontWeight: 700,
                            fontSize: '0.7rem',
                            borderRadius: 2,
                          }}
                        >
                          Review
                        </Button>
                      )}
                      {m.status === 'RUNNING' && (
                        <Tooltip title="Halt this mission">
                          <IconButton size="small" onClick={() => handleKill(`MISSION:${m.id}`)} sx={{ color: '#EF4444' }}>
                            <StopCircleOutlined fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                      <Chip
                        size="small"
                        label={m.status.replace('_', ' ')}
                        sx={{
                          backgroundColor: `${statusColor(m.status)}18`,
                          color: statusColor(m.status),
                          fontWeight: 700,
                          fontSize: '0.65rem',
                          height: 22,
                        }}
                      />
                    </Stack>
                  </Stack>
                </CardContent>
              </Card>
            ))}
          </Stack>
        </Box>
      )}

      {/* ── Tab: Execution History ── */}
      {tab === 1 && (
        <TableContainer component={Card} sx={{ background: 'rgba(15,23,42,0.97)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 3 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                {['Execution ID', 'Mission', 'Action', 'Risk', 'Status', 'Time', 'Permit Hash'].map(h => (
                  <TableCell key={h} sx={{ color: '#64748B', fontWeight: 700, fontSize: '0.7rem', letterSpacing: '0.06em', borderBottom: '1px solid rgba(255,255,255,0.07)', textTransform: 'uppercase' }}>
                    {h}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {EXECUTION_HISTORY.map(row => (
                <TableRow key={row.id} sx={{ '&:hover': { backgroundColor: 'rgba(0,240,255,0.03)' } }}>
                  <TableCell sx={{ color: '#00F0FF', fontFamily: 'monospace', fontSize: '0.72rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    {row.id}
                  </TableCell>
                  <TableCell sx={{ color: '#CBD5E1', fontSize: '0.78rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    {row.mission}
                  </TableCell>
                  <TableCell sx={{ color: '#94A3B8', fontSize: '0.75rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    {row.action}
                  </TableCell>
                  <TableCell sx={{ borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    <Chip size="small" label={row.risk}
                      sx={{ backgroundColor: `${riskColor(row.risk)}18`, color: riskColor(row.risk), fontWeight: 700, fontSize: '0.65rem', height: 20 }} />
                  </TableCell>
                  <TableCell sx={{ borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    <Stack direction="row" spacing={0.5} alignItems="center">
                      <StatusIcon status={row.status} />
                      <Typography variant="caption" sx={{ color: statusColor(row.status), fontWeight: 600 }}>{row.status}</Typography>
                    </Stack>
                  </TableCell>
                  <TableCell sx={{ color: '#64748B', fontFamily: 'monospace', fontSize: '0.72rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    <Stack direction="row" spacing={0.5} alignItems="center">
                      <AccessTimeOutlined sx={{ fontSize: 12 }} />
                      {row.ts}
                    </Stack>
                  </TableCell>
                  <TableCell sx={{ color: '#334155', fontFamily: 'monospace', fontSize: '0.68rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                    {row.permit}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* ── Tab: Approval Queue ── */}
      {tab === 2 && (
        <Stack spacing={2}>
          {APPROVAL_QUEUE.map(item => (
            <Card
              key={item.id}
              sx={{
                background: 'linear-gradient(135deg, rgba(245,158,11,0.06) 0%, rgba(15,23,42,0.97) 60%)',
                border: '1px solid rgba(245,158,11,0.25)',
                borderRadius: 3,
              }}
            >
              <CardContent sx={{ p: 3 }}>
                <Stack direction="row" alignItems="flex-start" spacing={2}>
                  <Avatar sx={{ backgroundColor: '#F59E0B18', color: '#F59E0B', width: 40, height: 40 }}>
                    <WarningAmberOutlined />
                  </Avatar>
                  <Box flex={1}>
                    <Stack direction={{ xs: 'column', sm: 'row' }} alignItems={{ sm: 'center' }} spacing={1} mb={1}>
                      <Typography variant="subtitle2" fontWeight={700} sx={{ color: '#F8FAFC' }}>
                        {item.action}
                      </Typography>
                      <Chip size="small" label={item.riskTier}
                        sx={{ backgroundColor: `${riskColor(item.riskTier)}18`, color: riskColor(item.riskTier), fontWeight: 700, fontSize: '0.65rem', height: 20 }} />
                      <Chip size="small" label={item.amount}
                        sx={{ backgroundColor: '#22C55E18', color: '#22C55E', fontWeight: 700, fontSize: '0.65rem', height: 20 }} />
                    </Stack>
                    <Typography variant="caption" sx={{ color: '#94A3B8', display: 'block', mb: 1.5 }}>
                      {item.details}
                    </Typography>
                    <Stack direction="row" spacing={3}>
                      <Box>
                        <Typography variant="caption" sx={{ color: '#475569', fontWeight: 600 }}>Agent</Typography>
                        <Typography variant="caption" sx={{ color: '#CBD5E1', display: 'block' }}>{item.agent}</Typography>
                      </Box>
                      <Box>
                        <Typography variant="caption" sx={{ color: '#475569', fontWeight: 600 }}>Payload Digest</Typography>
                        <Typography variant="caption" sx={{ color: '#334155', fontFamily: 'monospace', display: 'block' }}>{item.payloadDigest}</Typography>
                      </Box>
                      <Box>
                        <Typography variant="caption" sx={{ color: '#475569', fontWeight: 600 }}>Expires</Typography>
                        <Typography variant="caption" sx={{ color: '#F59E0B', display: 'block' }}>{new Date(item.expiresAt).toLocaleTimeString()}</Typography>
                      </Box>
                    </Stack>
                  </Box>
                  <Stack direction="column" spacing={1}>
                    <Button
                      variant="contained"
                      size="small"
                      startIcon={<ThumbUpOutlined />}
                      onClick={() => handleApprovalAction(item.id, 'approved')}
                      sx={{ background: 'linear-gradient(135deg, #22C55E, #16A34A)', color: '#fff', fontWeight: 700, borderRadius: 2, fontSize: '0.72rem' }}
                    >
                      Approve
                    </Button>
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<ThumbDownOutlined />}
                      onClick={() => handleApprovalAction(item.id, 'rejected')}
                      sx={{ borderColor: '#EF4444', color: '#EF4444', fontWeight: 700, borderRadius: 2, fontSize: '0.72rem', '&:hover': { backgroundColor: '#EF444418' } }}
                    >
                      Reject
                    </Button>
                    <Tooltip title="View full payload">
                      <IconButton size="small" onClick={() => setApprovalDialog(item)} sx={{ color: '#64748B' }}>
                        <VisibilityOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          ))}
          {APPROVAL_QUEUE.length === 0 && (
            <Box textAlign="center" py={8}>
              <CheckCircleOutlined sx={{ fontSize: 48, color: '#22C55E', mb: 2 }} />
              <Typography sx={{ color: '#64748B' }}>No pending approvals. All actions are cleared.</Typography>
            </Box>
          )}
        </Stack>
      )}

      {/* ── Approval Detail Dialog ── */}
      <Dialog open={!!approvalDialog} onClose={() => setApprovalDialog(null)} maxWidth="sm" fullWidth
        PaperProps={{ sx: { background: '#0F172A', border: '1px solid rgba(245,158,11,0.3)', borderRadius: 3 } }}>
        <DialogTitle sx={{ color: '#F8FAFC', borderBottom: '1px solid rgba(255,255,255,0.07)', pb: 2 }}>
          <Stack direction="row" spacing={1} alignItems="center">
            <VerifiedUserOutlined sx={{ color: '#F59E0B' }} />
            <span>Approval Request — {approvalDialog?.id}</span>
          </Stack>
        </DialogTitle>
        <DialogContent sx={{ pt: 2.5 }}>
          {approvalDialog && (
            <Stack spacing={2}>
              <Alert severity="warning" sx={{ backgroundColor: '#F59E0B12', border: '1px solid #F59E0B33', color: '#FCD34D' }}>
                This is a <strong>{approvalDialog.riskTier}</strong> risk action. Review the payload digest carefully. If the payload changes after this review, the approval is automatically invalidated.
              </Alert>
              <Box>
                <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.06em' }}>Action</Typography>
                <Typography sx={{ color: '#F8FAFC', fontWeight: 600, mt: 0.5 }}>{approvalDialog.action}</Typography>
              </Box>
              <Box>
                <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.06em' }}>Details</Typography>
                <Typography sx={{ color: '#CBD5E1', mt: 0.5, fontSize: '0.875rem' }}>{approvalDialog.details}</Typography>
              </Box>
              <Box>
                <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.06em' }}>Cryptographic Payload Digest</Typography>
                <Typography sx={{ color: '#64748B', fontFamily: 'monospace', fontSize: '0.75rem', mt: 0.5, wordBreak: 'break-all' }}>
                  {approvalDialog.payloadDigest}
                </Typography>
              </Box>
              <Divider sx={{ borderColor: 'rgba(255,255,255,0.07)' }} />
              <Stack direction="row" justifyContent="space-between">
                <Box>
                  <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700 }}>Financial Impact</Typography>
                  <Typography sx={{ color: '#22C55E', fontWeight: 700 }}>{approvalDialog.amount}</Typography>
                </Box>
                <Box textAlign="right">
                  <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700 }}>Approval Expires</Typography>
                  <Typography sx={{ color: '#F59E0B', fontWeight: 600, fontSize: '0.875rem' }}>
                    {new Date(approvalDialog.expiresAt).toLocaleTimeString()}
                  </Typography>
                </Box>
              </Stack>
            </Stack>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 2.5, borderTop: '1px solid rgba(255,255,255,0.07)', gap: 1 }}>
          <Button onClick={() => setApprovalDialog(null)} sx={{ color: '#64748B' }}>Cancel</Button>
          <Button
            variant="outlined"
            startIcon={<ThumbDownOutlined />}
            onClick={() => approvalDialog && handleApprovalAction(approvalDialog.id, 'rejected')}
            sx={{ borderColor: '#EF4444', color: '#EF4444', fontWeight: 700, borderRadius: 2, '&:hover': { backgroundColor: '#EF444418' } }}
          >
            Reject
          </Button>
          <Button
            variant="contained"
            startIcon={<ThumbUpOutlined />}
            onClick={() => approvalDialog && handleApprovalAction(approvalDialog.id, 'approved')}
            sx={{ background: 'linear-gradient(135deg, #22C55E, #16A34A)', fontWeight: 700, borderRadius: 2 }}
          >
            Approve &amp; Sign
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Kill Switch Confirm Dialog ── */}
      <Dialog open={!!killDialog} onClose={() => setKillDialog(null)} maxWidth="xs" fullWidth
        PaperProps={{ sx: { background: '#0F172A', border: '1px solid rgba(239,68,68,0.4)', borderRadius: 3 } }}>
        <DialogTitle sx={{ color: '#EF4444' }}>
          <Stack direction="row" spacing={1} alignItems="center">
            <PowerSettingsNew />
            <span>Confirm Kill: {killDialog}</span>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Alert severity="error" sx={{ backgroundColor: '#EF444412', border: '1px solid #EF444433', color: '#FCA5A5', mt: 1 }}>
            This will immediately halt all execution within the <strong>{killDialog}</strong> scope. The action is logged with your identity and timestamp. It cannot be undone without re-authorization.
          </Alert>
        </DialogContent>
        <DialogActions sx={{ p: 2.5, gap: 1 }}>
          <Button onClick={() => setKillDialog(null)} sx={{ color: '#64748B' }}>Cancel</Button>
          <Button
            variant="contained"
            startIcon={<PowerSettingsNew />}
            onClick={confirmKill}
            sx={{ backgroundColor: '#EF4444', '&:hover': { backgroundColor: '#DC2626' }, fontWeight: 700, borderRadius: 2 }}
          >
            Confirm Kill
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
