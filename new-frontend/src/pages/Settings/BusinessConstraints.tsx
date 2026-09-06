import { useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Stack,
  Chip,
  Button,
  Divider,
  Alert,
  Tabs,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableContainer,
  Paper,
  LinearProgress,
} from '@mui/material';
import {
  AccountBalanceOutlined,
  ShieldOutlined,
  BalanceOutlined,
  TrendingUpOutlined,
  SpeedOutlined,
  LockOutlined,
  CheckCircleOutlined,
  WarningAmberOutlined,
  BlockOutlined,
  HelpOutlineOutlined,
  UpdateOutlined,
  CompareArrowsOutlined,
  RefreshOutlined,
} from '@mui/icons-material';

// ─── Domain Models ─────────────────────────────────────────────────────────

type ConstraintEvaluationState =
  | 'Satisfied'
  | 'Warning'
  | 'Constrained'
  | 'Blocked'
  | 'Unknown'
  | 'Stale'
  | 'Conflicted';

interface ConstraintViewItem {
  id: string;
  category: 'Liquidity' | 'UnitEconomics' | 'Operations' | 'Risk' | 'Compliance' | 'Capacity' | 'Strategy';
  title: string;
  metric: string;
  observed: string;
  threshold: string;
  unit: string;
  state: ConstraintEvaluationState;
  freshness: string;
  authority: string;
  versionHash: string;
}

interface StrategicObjectiveView {
  rank: number;
  objective: string;
  weight: number;
  description: string;
}

interface ArbitrationDecisionView {
  decisionId: string;
  resourceClass: string;
  availableAmount: number;
  winningMission: string;
  winningPriority: string;
  allocatedAmount: number;
  strategicRegime: string;
  tieBreakReason: string;
  auditHash: string;
  candidates: {
    missionId: string;
    priority: string;
    requestedAmount: number;
    riskScore: number;
    roi: number;
    passesHard: boolean;
    ineligibilityReason?: string;
    strategicUtilityScore: number;
    riskAdjustedScore: number;
  }[];
}

interface BlockedMissionInspection {
  missionId: string;
  actionTitle: string;
  blockedConstraint: string;
  observedReality: string;
  requiredThreshold: string;
  evidenceSource: string;
  evidenceFreshness: string;
  projectedImpact: string;
  activePolicy: string;
  safeAlternative: {
    proposalId: string;
    title: string;
    reducedSpend: string;
    riskReduction: string;
    strategicAlignment: string;
  };
}

interface ResourceReservationView {
  reservationId: string;
  missionId: string;
  resourceClass: string;
  requested: number;
  reserved: number;
  consumed: number;
  state: 'Reserved' | 'Committed' | 'Consumed' | 'Released' | 'Expired';
  expiresInMinutes: number;
  version: number;
  idempotencyKey: string;
}

// ─── Component ─────────────────────────────────────────────────────────────

export const BusinessConstraints = () => {
  const [activeTab, setActiveTab] = useState(0);

  // Mock Active Strategic Regime
  const activeRegime = {
    name: 'CashPreservation_Distressed',
    authority: 'Board of Directors / Finance Committee',
    policyVersion: 4,
    lastUpdated: '2026-09-06 11:20:00 UTC',
    primaryObjective: 'Preserve Liquidity & Working Capital Floor',
    policyHash: '9E3D5F21A78B4C01D2E689AA47B1F34C89D012B',
  };

  const objectives: StrategicObjectiveView[] = [
    { rank: 1, objective: 'Preserve Liquidity', weight: 0.40, description: 'Guarantee minimum ₹10L liquid cash reserve and 90-day runway.' },
    { rank: 2, objective: 'Preserve Solvency', weight: 0.25, description: 'Maintain current liabilities below accessible assets floor.' },
    { rank: 3, objective: 'Protect Mandatory Operations', weight: 0.15, description: 'Core infrastructure and service payroll receive non-negotiable allocation.' },
    { rank: 4, objective: 'Preserve Positive Unit Economics', weight: 0.10, description: 'Gross margins must not fall below 25% floor.' },
    { rank: 5, objective: 'Protect Strategic Revenue', weight: 0.05, description: 'Preserve top 20% high-margin enterprise recurring contracts.' },
    { rank: 6, objective: 'Grow Revenue', weight: 0.05, description: 'Controlled incremental revenue expansion permitted only if cash reserves pass.' },
  ];

  const constraints: ConstraintViewItem[] = [
    {
      id: 'c-01',
      category: 'Liquidity',
      title: 'Minimum Liquid Cash Floor',
      metric: 'CashBalance',
      observed: '₹12,00,000',
      threshold: '≥ ₹10,00,000',
      unit: 'INR',
      state: 'Warning',
      freshness: '4m ago (Req: ≤ 15m)',
      authority: 'Board',
      versionHash: 'A1B2C3D4...E5F6',
    },
    {
      id: 'c-02',
      category: 'Liquidity',
      title: 'Runway Day Floor',
      metric: 'RunwayDays',
      observed: '110 Days',
      threshold: '≥ 90 Days',
      unit: 'Days',
      state: 'Satisfied',
      freshness: '10m ago (Req: ≤ 30m)',
      authority: 'CFO',
      versionHash: 'B2C3D4E5...F6A1',
    },
    {
      id: 'c-03',
      category: 'UnitEconomics',
      title: 'Gross Margin Floor',
      metric: 'GrossMarginPercent',
      observed: '28.4%',
      threshold: '≥ 25.0%',
      unit: 'Percent',
      state: 'Satisfied',
      freshness: '1h ago (Req: ≤ 6h)',
      authority: 'PricingCommittee',
      versionHash: 'C3D4E5F6...A1B2',
    },
    {
      id: 'c-04',
      category: 'Operations',
      title: 'Pending Human Approvals Cap',
      metric: 'PendingHumanApprovals',
      observed: '9 Approvals',
      threshold: '≤ 10 Approvals',
      unit: 'Count',
      state: 'Constrained',
      freshness: '2m ago (Req: ≤ 5m)',
      authority: 'COO',
      versionHash: 'D4E5F6A1...B2C3',
    },
    {
      id: 'c-05',
      category: 'Risk',
      title: 'Third-Party Vendor Quota',
      metric: 'VendorAPICostUsd',
      observed: '$420.00',
      threshold: '≤ $500.00',
      unit: 'USD',
      state: 'Warning',
      freshness: '15m ago (Req: ≤ 1h)',
      authority: 'CTO',
      versionHash: 'E5F6A1B2...C3D4',
    },
    {
      id: 'c-06',
      category: 'Compliance',
      title: 'Data Residency Sovereignty',
      metric: 'CrossBorderDataTransfers',
      observed: '0 Violations',
      threshold: '== 0 Violations',
      unit: 'Count',
      state: 'Satisfied',
      freshness: 'Realtime telemetry',
      authority: 'ComplianceOfficer',
      versionHash: 'F6A1B2C3...D4E5',
    },
    {
      id: 'c-07',
      category: 'Capacity',
      title: 'Concurrent Autonomous Missions',
      metric: 'ActiveConcurrentMissions',
      observed: '5 Missions',
      threshold: '≤ 5 Missions',
      unit: 'Count',
      state: 'Blocked',
      freshness: 'Realtime lease count',
      authority: 'RuntimeKernel',
      versionHash: '12345678...9ABC',
    },
  ];

  const arbitration: ArbitrationDecisionView = {
    decisionId: 'arb-89241',
    resourceClass: 'Cash',
    availableAmount: 500000.0,
    winningMission: 'Mission B (Customer Retention Automation)',
    winningPriority: 'P1_High',
    allocatedAmount: 300000.0,
    strategicRegime: 'CashPreservation_Distressed',
    tieBreakReason: 'Candidate B passed hard constraints with higher liquidity preservation under active regime',
    auditHash: '311A7713E78708C210B54772994DD7F53D43E8216',
    candidates: [
      {
        missionId: 'Mission A (Paid Growth Sprint)',
        priority: 'P0_Critical',
        requestedAmount: 600000.0,
        riskScore: 0.15,
        roi: 2.2,
        passesHard: false,
        ineligibilityReason: 'Requested ₹6,00,000 exceeds available cash pool ₹5,00,000 (Invariant I15 Fail-Closed)',
        strategicUtilityScore: 0.65,
        riskAdjustedScore: 0.52,
      },
      {
        missionId: 'Mission B (Customer Retention Automation)',
        priority: 'P1_High',
        requestedAmount: 300000.0,
        riskScore: 0.20,
        roi: 1.8,
        passesHard: true,
        strategicUtilityScore: 0.82,
        riskAdjustedScore: 0.74,
      },
    ],
  };

  const blockedInspection: BlockedMissionInspection = {
    missionId: 'Mission A (Paid Growth Sprint)',
    actionTitle: 'Launch Omnichannel Acquisition Campaign (₹5,00,000)',
    blockedConstraint: 'Minimum Cash Reserve Floor (₹10,00,000)',
    observedReality: 'Current verified cash: ₹12,00,000',
    requiredThreshold: 'Post-action projected cash must be ≥ ₹10,00,000',
    evidenceSource: 'Verified Banking Ledger Telemetry (ID: ledger-tx-9941)',
    evidenceFreshness: '4 minutes old (Requirement: ≤ 15 minutes)',
    projectedImpact: 'Simulated post-action cash: ₹7,00,000 (Deficit: ₹3,00,000 below sovereign reserve)',
    activePolicy: 'Regime: CashPreservation_Distressed (Priority 1: Preserve Liquidity)',
    safeAlternative: {
      proposalId: 'alt-pilot-75k',
      title: 'Controlled Retention & Organic Re-engagement Campaign',
      reducedSpend: '₹75,000 (Preserves ₹11,25,000 liquid buffer)',
      riskReduction: '72% lower financial exposure',
      strategicAlignment: 'Passes all hard constraints; aligns with Distressed regime unit economics',
    },
  };

  const reservations: ResourceReservationView[] = [
    {
      reservationId: 'res-4109',
      missionId: 'Mission B (Retention)',
      resourceClass: 'Cash',
      requested: 300000.0,
      reserved: 300000.0,
      consumed: 0.0,
      state: 'Reserved',
      expiresInMinutes: 14,
      version: 1,
      idempotencyKey: 'res_missionB_retention_v1',
    },
    {
      reservationId: 'res-3992',
      missionId: 'Mission Core (Infra)',
      resourceClass: 'Budget',
      requested: 85000.0,
      reserved: 85000.0,
      consumed: 82400.0,
      state: 'Committed',
      expiresInMinutes: 45,
      version: 2,
      idempotencyKey: 'res_core_infra_sept',
    },
    {
      reservationId: 'res-3810',
      missionId: 'Mission Exploratory',
      resourceClass: 'HumanApproval',
      requested: 2,
      reserved: 2,
      consumed: 2,
      state: 'Consumed',
      expiresInMinutes: 0,
      version: 3,
      idempotencyKey: 'res_approval_exp_02',
    },
  ];

  const getStateColor = (state: ConstraintEvaluationState) => {
    switch (state) {
      case 'Satisfied':
        return 'success';
      case 'Warning':
        return 'warning';
      case 'Constrained':
        return 'info';
      case 'Blocked':
        return 'error';
      case 'Unknown':
      case 'Stale':
      case 'Conflicted':
        return 'default';
      default:
        return 'default';
    }
  };

  const getStateIcon = (state: ConstraintEvaluationState) => {
    switch (state) {
      case 'Satisfied':
        return <CheckCircleOutlined fontSize="small" />;
      case 'Warning':
        return <WarningAmberOutlined fontSize="small" />;
      case 'Constrained':
        return <SpeedOutlined fontSize="small" />;
      case 'Blocked':
        return <BlockOutlined fontSize="small" />;
      case 'Unknown':
        return <HelpOutlineOutlined fontSize="small" />;
      case 'Stale':
        return <UpdateOutlined fontSize="small" />;
      case 'Conflicted':
        return <CompareArrowsOutlined fontSize="small" />;
    }
  };

  return (
    <Box sx={{ p: 3, maxWidth: 1400, margin: '0 auto' }}>
      {/* Header Banner */}
      <Box sx={{ mb: 4, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <Box>
          <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 1 }}>
            <AccountBalanceOutlined sx={{ fontSize: 32, color: '#38bdf8' }} />
            <Typography variant="h4" fontWeight={700} sx={{ color: '#F8FAFC' }}>
              Business Constraint & Strategy Engine
            </Typography>
            <Chip
              label="Phase 3 Batch 3.5 Certified"
              color="primary"
              size="small"
              sx={{ fontWeight: 600, backgroundColor: 'rgba(56, 189, 248, 0.15)', color: '#38bdf8' }}
            />
          </Stack>
          <Typography variant="body2" sx={{ color: '#94A3B8' }}>
            Deterministic business sovereignty layer. Evaluates feasibility, enforces hard fail-closed constraints, and deterministically arbitrates finite resources.
          </Typography>
        </Box>
        <Stack direction="row" spacing={2}>
          <Button
            variant="outlined"
            startIcon={<RefreshOutlined />}
            size="small"
            sx={{ borderColor: 'rgba(255,255,255,0.15)', color: '#F8FAFC' }}
          >
            Re-verify Reality
          </Button>
          <Button
            variant="contained"
            startIcon={<LockOutlined />}
            size="small"
            sx={{ backgroundColor: '#0284c7', '&:hover': { backgroundColor: '#0369a1' } }}
          >
            Governed Authority
          </Button>
        </Stack>
      </Box>

      {/* Governance & Sovereignty Notice */}
      <Alert
        severity="info"
        icon={<ShieldOutlined />}
        sx={{
          mb: 4,
          backgroundColor: 'rgba(15, 23, 42, 0.8)',
          border: '1px solid rgba(56, 189, 248, 0.3)',
          color: '#E2E8F0',
        }}
      >
        <Typography variant="body2">
          <strong>Invariant I15 Sovereign Notice:</strong> AI agents, workers, worker reputation scores (Batch 3.4), and mission priorities cannot override or waive active hard business constraints. Control surfaces are strictly governed and read-only without verified board/executive authorization.
        </Typography>
      </Alert>

      {/* Top Metrics / KPIs */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={3}>
          <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
            <CardContent>
              <Typography variant="caption" sx={{ color: '#94A3B8', textTransform: 'uppercase', letterSpacing: 1 }}>
                Active Strategic Regime
              </Typography>
              <Typography variant="h6" fontWeight={700} sx={{ color: '#F8FAFC', mt: 0.5 }}>
                {activeRegime.name}
              </Typography>
              <Typography variant="caption" sx={{ color: '#38bdf8' }}>
                Version {activeRegime.policyVersion} • {activeRegime.authority}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={3}>
          <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
            <CardContent>
              <Typography variant="caption" sx={{ color: '#94A3B8', textTransform: 'uppercase', letterSpacing: 1 }}>
                Hard Constraints Monitored
              </Typography>
              <Typography variant="h6" fontWeight={700} sx={{ color: '#F8FAFC', mt: 0.5 }}>
                7 Defined (1 Warning, 1 Blocked)
              </Typography>
              <Typography variant="caption" sx={{ color: '#4ade80' }}>
                Epistemic Grounding: 100% Digital Twin
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={3}>
          <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
            <CardContent>
              <Typography variant="caption" sx={{ color: '#94A3B8', textTransform: 'uppercase', letterSpacing: 1 }}>
                Resource Reservation Pool
              </Typography>
              <Typography variant="h6" fontWeight={700} sx={{ color: '#F8FAFC', mt: 0.5 }}>
                ₹3,85,000 Reserved
              </Typography>
              <Typography variant="caption" sx={{ color: '#f59e0b' }}>
                ₹8,15,000 Available Liquid Pool
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={3}>
          <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
            <CardContent>
              <Typography variant="caption" sx={{ color: '#94A3B8', textTransform: 'uppercase', letterSpacing: 1 }}>
                Pre-Firewall Invariant Status
              </Typography>
              <Typography variant="h6" fontWeight={700} sx={{ color: '#4ade80', mt: 0.5 }}>
                ZERO Unauthorized Exec
              </Typography>
              <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                Batch 6 Firewall Sovereign & Locked
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Main Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'rgba(255,255,255,0.1)', mb: 3 }}>
        <Tabs
          value={activeTab}
          onChange={(_, val) => setActiveTab(val)}
          textColor="inherit"
          sx={{
            '& .MuiTabs-indicator': { backgroundColor: '#38bdf8' },
            '& .MuiTab-root': { color: '#94A3B8', '&.Mui-selected': { color: '#38bdf8', fontWeight: 600 } },
          }}
        >
          <Tab label="Constraint Overview" icon={<BalanceOutlined />} iconPosition="start" />
          <Tab label="Strategic Regime" icon={<TrendingUpOutlined />} iconPosition="start" />
          <Tab label="Mission Arbitration" icon={<AccountBalanceOutlined />} iconPosition="start" />
          <Tab label="Why Blocked? (Simulation)" icon={<BlockOutlined />} iconPosition="start" />
          <Tab label="Resource Reservations" icon={<LockOutlined />} iconPosition="start" />
        </Tabs>
      </Box>

      {/* Tab 0: Constraint Overview */}
      {activeTab === 0 && (
        <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
          <CardContent sx={{ p: 0 }}>
            <TableContainer component={Paper} sx={{ backgroundColor: 'transparent' }}>
              <Table>
                <TableHead sx={{ backgroundColor: 'rgba(255,255,255,0.02)' }}>
                  <TableRow>
                    <TableCell sx={{ color: '#94A3B8', fontWeight: 600 }}>Constraint</TableCell>
                    <TableCell sx={{ color: '#94A3B8', fontWeight: 600 }}>Category</TableCell>
                    <TableCell sx={{ color: '#94A3B8', fontWeight: 600 }}>Observed Reality</TableCell>
                    <TableCell sx={{ color: '#94A3B8', fontWeight: 600 }}>Required Threshold</TableCell>
                    <TableCell sx={{ color: '#94A3B8', fontWeight: 600 }}>Status</TableCell>
                    <TableCell sx={{ color: '#94A3B8', fontWeight: 600 }}>Freshness</TableCell>
                    <TableCell sx={{ color: '#94A3B8', fontWeight: 600 }}>Authority</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {constraints.map((c) => (
                    <TableRow key={c.id} sx={{ '&:hover': { backgroundColor: 'rgba(255,255,255,0.02)' } }}>
                      <TableCell sx={{ color: '#F8FAFC', fontWeight: 600 }}>
                        {c.title}
                        <Typography variant="caption" display="block" sx={{ color: '#64748B' }}>
                          Hash: {c.versionHash}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip label={c.category} size="small" sx={{ backgroundColor: 'rgba(255,255,255,0.05)', color: '#CBD5E1' }} />
                      </TableCell>
                      <TableCell sx={{ color: '#F8FAFC', fontWeight: 500 }}>{c.observed}</TableCell>
                      <TableCell sx={{ color: '#94A3B8' }}>{c.threshold}</TableCell>
                      <TableCell>
                        <Chip
                          icon={getStateIcon(c.state)}
                          label={c.state}
                          color={getStateColor(c.state)}
                          size="small"
                          sx={{ fontWeight: 600 }}
                        />
                      </TableCell>
                      <TableCell sx={{ color: '#94A3B8', fontSize: '0.825rem' }}>{c.freshness}</TableCell>
                      <TableCell sx={{ color: '#CBD5E1', fontSize: '0.825rem' }}>{c.authority}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      {/* Tab 1: Strategic Regime */}
      {activeTab === 1 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={5}>
            <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
              <CardContent>
                <Typography variant="h6" fontWeight={700} sx={{ color: '#F8FAFC', mb: 2 }}>
                  Active Strategic Regime Details
                </Typography>
                <Stack spacing={2}>
                  <Box>
                    <Typography variant="caption" sx={{ color: '#94A3B8' }}>Regime Identifier</Typography>
                    <Typography variant="body1" fontWeight={600} sx={{ color: '#38bdf8' }}>
                      {activeRegime.name}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" sx={{ color: '#94A3B8' }}>Primary Strategic Directive</Typography>
                    <Typography variant="body2" sx={{ color: '#F8FAFC' }}>
                      {activeRegime.primaryObjective}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" sx={{ color: '#94A3B8' }}>Governance Authority</Typography>
                    <Typography variant="body2" sx={{ color: '#CBD5E1' }}>
                      {activeRegime.authority} (Policy Version {activeRegime.policyVersion})
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" sx={{ color: '#94A3B8' }}>Cryptographic Policy Hash</Typography>
                    <Typography variant="body2" sx={{ color: '#64748B', fontFamily: 'monospace' }}>
                      {activeRegime.policyHash}
                    </Typography>
                  </Box>
                  <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />
                  <Typography variant="caption" sx={{ color: '#f59e0b' }}>
                    Note: A strategic regime changes the priority order of admissible objectives. It NEVER disables hard safety or liquidity constraints.
                  </Typography>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={7}>
            <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
              <CardContent>
                <Typography variant="h6" fontWeight={700} sx={{ color: '#F8FAFC', mb: 2 }}>
                  Deterministic Lexicographic Priority Vector
                </Typography>
                <Stack spacing={2}>
                  {objectives.map((obj) => (
                    <Box
                      key={obj.rank}
                      sx={{
                        p: 1.5,
                        borderRadius: 1,
                        backgroundColor: 'rgba(255,255,255,0.02)',
                        border: '1px solid rgba(255,255,255,0.05)',
                      }}
                    >
                      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 0.5 }}>
                        <Typography variant="subtitle2" fontWeight={700} sx={{ color: '#F8FAFC' }}>
                          Rank #{obj.rank}: {obj.objective}
                        </Typography>
                        <Chip label={`Weight ${(obj.weight * 100).toFixed(0)}%`} size="small" sx={{ backgroundColor: 'rgba(56, 189, 248, 0.15)', color: '#38bdf8' }} />
                      </Stack>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                        {obj.description}
                      </Typography>
                      <LinearProgress
                        variant="determinate"
                        value={obj.weight * 100 * 2.5}
                        sx={{ mt: 1, height: 4, borderRadius: 2, backgroundColor: 'rgba(255,255,255,0.05)', '& .MuiLinearProgress-bar': { backgroundColor: '#38bdf8' } }}
                      />
                    </Box>
                  ))}
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Tab 2: Mission Arbitration */}
      {activeTab === 2 && (
        <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
          <CardContent>
            <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box>
                <Typography variant="h6" fontWeight={700} sx={{ color: '#F8FAFC' }}>
                  Resource Arbitration Log: {arbitration.decisionId}
                </Typography>
                <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                  Target Resource: <strong>{arbitration.resourceClass}</strong> • Pool Available: <strong>₹{arbitration.availableAmount.toLocaleString('en-IN')}</strong> • Regime: <strong>{arbitration.strategicRegime}</strong>
                </Typography>
              </Box>
              <Chip label={`Winner: ${arbitration.winningMission}`} color="success" sx={{ fontWeight: 600 }} />
            </Box>

            <TableContainer component={Paper} sx={{ backgroundColor: 'transparent', mb: 3 }}>
              <Table>
                <TableHead sx={{ backgroundColor: 'rgba(255,255,255,0.02)' }}>
                  <TableRow>
                    <TableCell sx={{ color: '#94A3B8' }}>Competing Mission</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Priority</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Requested Amount</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Hard Constraints</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Strategic Utility</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Risk Adjusted</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Arbitration Outcome</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {arbitration.candidates.map((cand) => (
                    <TableRow key={cand.missionId}>
                      <TableCell sx={{ color: '#F8FAFC', fontWeight: 600 }}>{cand.missionId}</TableCell>
                      <TableCell>
                        <Chip label={cand.priority} size="small" sx={{ backgroundColor: 'rgba(255,255,255,0.05)', color: '#CBD5E1' }} />
                      </TableCell>
                      <TableCell sx={{ color: '#F8FAFC' }}>₹{cand.requestedAmount.toLocaleString('en-IN')}</TableCell>
                      <TableCell>
                        {cand.passesHard ? (
                          <Chip label="PASSED" color="success" size="small" />
                        ) : (
                          <Chip label="BLOCKED" color="error" size="small" />
                        )}
                      </TableCell>
                      <TableCell sx={{ color: '#38bdf8' }}>{cand.strategicUtilityScore.toFixed(3)}</TableCell>
                      <TableCell sx={{ color: '#38bdf8', fontWeight: 600 }}>{cand.riskAdjustedScore.toFixed(3)}</TableCell>
                      <TableCell>
                        {cand.passesHard ? (
                          <Chip label="ALLOCATED ₹3L" color="success" size="small" sx={{ fontWeight: 700 }} />
                        ) : (
                          <Typography variant="caption" sx={{ color: '#f87171', display: 'block', maxWidth: 220 }}>
                            {cand.ineligibilityReason}
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>

            <Box sx={{ p: 2, borderRadius: 1, backgroundColor: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.05)' }}>
              <Typography variant="subtitle2" fontWeight={600} sx={{ color: '#F8FAFC', mb: 0.5 }}>
                Arbitration Rationale & Invariant I15-C Audit
              </Typography>
              <Typography variant="body2" sx={{ color: '#94A3B8', mb: 1 }}>
                {arbitration.tieBreakReason}. Zero agent bidding or LLM negotiation permitted. Lexicographic priority vector strictly enforced.
              </Typography>
              <Typography variant="caption" sx={{ color: '#64748B', fontFamily: 'monospace' }}>
                Audit Hash: {arbitration.auditHash}
              </Typography>
            </Box>
          </CardContent>
        </Card>
      )}

      {/* Tab 3: Why Blocked? (Simulation) */}
      {activeTab === 3 && (
        <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
          <CardContent>
            <Typography variant="h6" fontWeight={700} sx={{ color: '#F8FAFC', mb: 1 }}>
              Why Was This Action Blocked?
            </Typography>
            <Typography variant="body2" sx={{ color: '#94A3B8', mb: 3 }}>
              Every blocked proposal is explained through verified telemetry, Digital Twin simulation projections, and governed business constraints.
            </Typography>

            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Box sx={{ p: 2.5, borderRadius: 2, backgroundColor: 'rgba(239, 68, 68, 0.05)', border: '1px solid rgba(239, 68, 68, 0.2)' }}>
                  <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1.5 }}>
                    <BlockOutlined sx={{ color: '#f87171' }} />
                    <Typography variant="subtitle1" fontWeight={700} sx={{ color: '#f87171' }}>
                      Primary Hard Constraint Violation
                    </Typography>
                  </Stack>
                  <Typography variant="body2" fontWeight={600} sx={{ color: '#F8FAFC', mb: 1 }}>
                    {blockedInspection.actionTitle}
                  </Typography>
                  <Stack spacing={1.5}>
                    <Box>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>Violated Constraint</Typography>
                      <Typography variant="body2" sx={{ color: '#F8FAFC' }}>{blockedInspection.blockedConstraint}</Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>Current Reality vs Required</Typography>
                      <Typography variant="body2" sx={{ color: '#CBD5E1' }}>
                        {blockedInspection.observedReality} (Threshold: {blockedInspection.requiredThreshold})
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>Pre-Flight Simulated Impact</Typography>
                      <Typography variant="body2" sx={{ color: '#fca5a5' }}>
                        {blockedInspection.projectedImpact}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>Telemetry Evidence Provenance</Typography>
                      <Typography variant="caption" display="block" sx={{ color: '#64748B' }}>
                        {blockedInspection.evidenceSource} • Freshness: {blockedInspection.evidenceFreshness}
                      </Typography>
                    </Box>
                  </Stack>
                </Box>
              </Grid>

              <Grid item xs={12} md={6}>
                <Box sx={{ p: 2.5, borderRadius: 2, backgroundColor: 'rgba(34, 197, 94, 0.05)', border: '1px solid rgba(34, 197, 94, 0.2)' }}>
                  <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1.5 }}>
                    <CheckCircleOutlined sx={{ color: '#4ade80' }} />
                    <Typography variant="subtitle1" fontWeight={700} sx={{ color: '#4ade80' }}>
                      Safe Admissible Alternative Generated
                    </Typography>
                  </Stack>
                  <Typography variant="body2" fontWeight={600} sx={{ color: '#F8FAFC', mb: 1 }}>
                    {blockedInspection.safeAlternative.title}
                  </Typography>
                  <Stack spacing={1.5}>
                    <Box>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>Proposed Spend Allocation</Typography>
                      <Typography variant="body2" sx={{ color: '#4ade80', fontWeight: 600 }}>
                        {blockedInspection.safeAlternative.reducedSpend}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>Risk Reduction</Typography>
                      <Typography variant="body2" sx={{ color: '#CBD5E1' }}>
                        {blockedInspection.safeAlternative.riskReduction}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>Strategic Feasibility</Typography>
                      <Typography variant="body2" sx={{ color: '#94A3B8' }}>
                        {blockedInspection.safeAlternative.strategicAlignment}
                      </Typography>
                    </Box>
                    <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />
                    <Button
                      variant="outlined"
                      size="small"
                      sx={{ borderColor: '#22c55e', color: '#4ade80', alignSelf: 'flex-start' }}
                    >
                      Propose Safe Alternative for Admission
                    </Button>
                  </Stack>
                </Box>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Tab 4: Resource Reservations */}
      {activeTab === 4 && (
        <Card sx={{ backgroundColor: '#0F172A', border: '1px solid rgba(255,255,255,0.08)' }}>
          <CardContent sx={{ p: 0 }}>
            <TableContainer component={Paper} sx={{ backgroundColor: 'transparent' }}>
              <Table>
                <TableHead sx={{ backgroundColor: 'rgba(255,255,255,0.02)' }}>
                  <TableRow>
                    <TableCell sx={{ color: '#94A3B8' }}>Reservation ID</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Mission / Purpose</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Resource Class</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Reserved Amount</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Consumed Amount</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>State</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>TTL Expiry</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Version</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {reservations.map((r) => (
                    <TableRow key={r.reservationId} sx={{ '&:hover': { backgroundColor: 'rgba(255,255,255,0.02)' } }}>
                      <TableCell sx={{ color: '#F8FAFC', fontWeight: 600 }}>{r.reservationId}</TableCell>
                      <TableCell sx={{ color: '#CBD5E1' }}>{r.missionId}</TableCell>
                      <TableCell>
                        <Chip label={r.resourceClass} size="small" sx={{ backgroundColor: 'rgba(255,255,255,0.05)', color: '#CBD5E1' }} />
                      </TableCell>
                      <TableCell sx={{ color: '#F8FAFC', fontWeight: 500 }}>
                        {r.resourceClass === 'Cash' || r.resourceClass === 'Budget'
                          ? `₹${r.reserved.toLocaleString('en-IN')}`
                          : r.reserved}
                      </TableCell>
                      <TableCell sx={{ color: '#94A3B8' }}>
                        {r.resourceClass === 'Cash' || r.resourceClass === 'Budget'
                          ? `₹${r.consumed.toLocaleString('en-IN')}`
                          : r.consumed}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={r.state}
                          color={r.state === 'Committed' ? 'success' : r.state === 'Reserved' ? 'warning' : 'default'}
                          size="small"
                          sx={{ fontWeight: 600 }}
                        />
                      </TableCell>
                      <TableCell sx={{ color: '#94A3B8' }}>
                        {r.expiresInMinutes > 0 ? `${r.expiresInMinutes}m remaining` : 'Expired / Released'}
                      </TableCell>
                      <TableCell sx={{ color: '#64748B' }}>v{r.version}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};

export default BusinessConstraints;
