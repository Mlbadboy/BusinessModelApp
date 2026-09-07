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
} from '@mui/material';
import {
  EngineeringOutlined,
  ShieldOutlined,
  LockOutlined,
  CheckCircleOutlined,
  WarningAmberOutlined,
  BlockOutlined,
  RefreshOutlined,
  ComputerOutlined,
  LanguageOutlined,
  CableOutlined,
  HubOutlined,
  HealthAndSafetyOutlined,
  GavelOutlined,
} from '@mui/icons-material';

// ─── Domain Models ─────────────────────────────────────────────────────────

type WorkerModality = 'Api' | 'Mcp' | 'Browser' | 'Desktop';
type WorkerCircuitState = 'Healthy' | 'Degraded' | 'CircuitOpen' | 'Quarantined' | 'Recovering';

interface WorkerStatusRecord {
  id: string;
  name: string;
  modality: WorkerModality;
  supportedCapabilities: string[];
  circuitState: WorkerCircuitState;
  failureRate: number;
  totalExecutions: number;
  uptimePct: number;
  sandboxProfile: {
    memoryLimitMB: number;
    cpuCores: number;
    maxTimeoutSec: number;
    allowShell: boolean;
    networkRestricted: boolean;
  };
}

interface ActionProposalRecord {
  proposalId: string;
  capabilityId: string;
  modality: WorkerModality;
  targetSystem: string;
  actionType: string;
  payloadDigest: string;
  idempotencyKey: string;
  status: 'PendingAdmission' | 'FirewallApproved' | 'Executed';
}

const MOCK_WORKERS: WorkerStatusRecord[] = [
  {
    id: 'wk-api-analytics-01',
    name: 'Sovereign API Analytics Worker',
    modality: 'Api',
    supportedCapabilities: ['market_analytics/v1', 'financial_health/v1'],
    circuitState: 'Healthy',
    failureRate: 0.00,
    totalExecutions: 1420,
    uptimePct: 99.98,
    sandboxProfile: {
      memoryLimitMB: 1024,
      cpuCores: 2.0,
      maxTimeoutSec: 60,
      allowShell: false,
      networkRestricted: true,
    },
  },
  {
    id: 'wk-mcp-synthesis-02',
    name: 'Governed MCP Tool Host',
    modality: 'Mcp',
    supportedCapabilities: ['data_synthesis/v1', 'vector_search/v1'],
    circuitState: 'Healthy',
    failureRate: 0.01,
    totalExecutions: 854,
    uptimePct: 99.92,
    sandboxProfile: {
      memoryLimitMB: 2048,
      cpuCores: 4.0,
      maxTimeoutSec: 120,
      allowShell: false,
      networkRestricted: true,
    },
  },
  {
    id: 'wk-browser-portal-03',
    name: 'Headless Browser Automation Pod',
    modality: 'Browser',
    supportedCapabilities: ['portal_scrape/v1', 'compliance_verification/v1'],
    circuitState: 'Healthy',
    failureRate: 0.03,
    totalExecutions: 412,
    uptimePct: 99.50,
    sandboxProfile: {
      memoryLimitMB: 4096,
      cpuCores: 4.0,
      maxTimeoutSec: 180,
      allowShell: false,
      networkRestricted: true,
    },
  },
  {
    id: 'wk-desktop-legacy-04',
    name: 'Secured Legacy Terminal Worker',
    modality: 'Desktop',
    supportedCapabilities: ['legacy_erp_sync/v1'],
    circuitState: 'Healthy',
    failureRate: 0.02,
    totalExecutions: 298,
    uptimePct: 99.75,
    sandboxProfile: {
      memoryLimitMB: 2048,
      cpuCores: 2.0,
      maxTimeoutSec: 300,
      allowShell: false,
      networkRestricted: true,
    },
  },
];

const MOCK_PROPOSALS: ActionProposalRecord[] = [
  {
    proposalId: 'prop-88102a-9f12',
    capabilityId: 'submit_order/v1',
    modality: 'Api',
    targetSystem: 'SaaS_Connector_Gateway',
    actionType: 'API_POST',
    payloadDigest: 'A9B1C3D4E5F60123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123',
    idempotencyKey: 'api_wk-api-analytics-01_att-901',
    status: 'FirewallApproved',
  },
  {
    proposalId: 'prop-77215b-4c33',
    capabilityId: 'compliance_filing/v1',
    modality: 'Browser',
    targetSystem: 'MCA_Portal_Government',
    actionType: 'BROWSER_FORM_SUBMIT',
    payloadDigest: '7F6E5D4C3B2A109876543210FEDCBA9876543210FEDCBA9876543210FEDCBA98',
    idempotencyKey: 'brw_wk-browser-portal-03_att-902',
    status: 'PendingAdmission',
  },
];

export const WorkerFabric = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [workers] = useState<WorkerStatusRecord[]>(MOCK_WORKERS);
  const [proposals] = useState<ActionProposalRecord[]>(MOCK_PROPOSALS);
  const [testResult, setTestResult] = useState<string | null>(null);

  const handleTestResolution = () => {
    setTestResult(
      'RESOLUTION PASS: Resolved to [wk-api-analytics-01] (Modality: Api, Weight: 1.0, Health: Healthy). Pre-flight Autonomy Check: L5 ceiling >= L3 required (PASSED). Invariant I16-A Sandbox Container Initialized.'
    );
  };

  const getCircuitChip = (state: WorkerCircuitState) => {
    switch (state) {
      case 'Healthy':
        return <Chip icon={<CheckCircleOutlined />} label="HEALTHY" size="small" sx={{ bgcolor: 'rgba(57, 255, 20, 0.15)', color: '#39FF14', border: '1px solid #39FF14' }} />;
      case 'Degraded':
        return <Chip icon={<WarningAmberOutlined />} label="DEGRADED" size="small" sx={{ bgcolor: 'rgba(255, 170, 0, 0.15)', color: '#FFAA00', border: '1px solid #FFAA00' }} />;
      case 'CircuitOpen':
        return <Chip icon={<BlockOutlined />} label="CIRCUIT OPEN" size="small" sx={{ bgcolor: 'rgba(255, 59, 48, 0.15)', color: '#FF3B30', border: '1px solid #FF3B30' }} />;
      case 'Quarantined':
        return <Chip icon={<LockOutlined />} label="QUARANTINED" size="small" sx={{ bgcolor: 'rgba(188, 0, 255, 0.15)', color: '#BC00FF', border: '1px solid #BC00FF' }} />;
      default:
        return <Chip label={state} size="small" />;
    }
  };

  const getModalityIcon = (mod: WorkerModality) => {
    switch (mod) {
      case 'Api':
        return <CableOutlined sx={{ color: '#00FFFF' }} />;
      case 'Mcp':
        return <HubOutlined sx={{ color: '#39FF14' }} />;
      case 'Browser':
        return <LanguageOutlined sx={{ color: '#BC00FF' }} />;
      case 'Desktop':
        return <ComputerOutlined sx={{ color: '#FFB800' }} />;
    }
  };

  return (
    <Box sx={{ p: 4, maxWidth: 1600, margin: '0 auto', color: '#E0E6ED' }}>
      {/* Top Banner */}
      <Card sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(0, 255, 255, 0.3)', mb: 4, borderRadius: 2 }}>
        <CardContent sx={{ p: 3 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Box>
              <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 1 }}>
                <EngineeringOutlined sx={{ color: '#00FFFF', fontSize: 32 }} />
                <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: '0.05em', color: '#FFFFFF' }}>
                  UNIVERSAL BUSINESS WORKER FABRIC
                </Typography>
                <Chip label="PHASE 3 BATCH 3.6" size="small" sx={{ bgcolor: 'rgba(0, 255, 255, 0.15)', color: '#00FFFF', border: '1px solid #00FFFF' }} />
                <Chip label="I16 / I16-A / I16-B / I16-C CERTIFIED" size="small" sx={{ bgcolor: 'rgba(57, 255, 20, 0.15)', color: '#39FF14', border: '1px solid #39FF14' }} />
              </Stack>
              <Typography variant="body2" sx={{ color: '#8E9BAE', maxWidth: 1100 }}>
                Deterministic worker containment, capability-to-modality resolution, and resource sandboxing.
                Enforces zero direct consequentiality: all state-mutating actions emit cryptographic ActionProposals submitted to Runtime Admission and the Batch 6 Execution Firewall.
              </Typography>
            </Box>
            <Button
              variant="outlined"
              startIcon={<RefreshOutlined />}
              onClick={handleTestResolution}
              sx={{ borderColor: '#00FFFF', color: '#00FFFF', '&:hover': { bgcolor: 'rgba(0, 255, 255, 0.1)' } }}
            >
              Simulate Resolution
            </Button>
          </Stack>
        </CardContent>
      </Card>

      {testResult && (
        <Alert severity="info" sx={{ mb: 3, bgcolor: 'rgba(0, 255, 255, 0.08)', color: '#00FFFF', border: '1px solid #00FFFF' }}>
          {testResult}
        </Alert>
      )}

      {/* KPI Overview */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="overline" sx={{ color: '#8E9BAE' }}>Active Workers</Typography>
                <HubOutlined sx={{ color: '#00FFFF' }} />
              </Stack>
              <Typography variant="h3" sx={{ fontWeight: 800, color: '#FFFFFF', my: 1 }}>{workers.length}</Typography>
              <Typography variant="caption" sx={{ color: '#39FF14' }}>4 Governed Modalities Deployed</Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="overline" sx={{ color: '#8E9BAE' }}>Circuit Breakers</Typography>
                <HealthAndSafetyOutlined sx={{ color: '#39FF14' }} />
              </Stack>
              <Typography variant="h3" sx={{ fontWeight: 800, color: '#39FF14', my: 1 }}>100%</Typography>
              <Typography variant="caption" sx={{ color: '#8E9BAE' }}>All Circuits in Healthy State</Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="overline" sx={{ color: '#8E9BAE' }}>Action Proposals</Typography>
                <GavelOutlined sx={{ color: '#BC00FF' }} />
              </Stack>
              <Typography variant="h3" sx={{ fontWeight: 800, color: '#FFFFFF', my: 1 }}>{proposals.length}</Typography>
              <Typography variant="caption" sx={{ color: '#00FFFF' }}>Subordinated to Batch 6 Firewall</Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="overline" sx={{ color: '#8E9BAE' }}>External Consequentiality</Typography>
                <ShieldOutlined sx={{ color: '#39FF14' }} />
              </Stack>
              <Typography variant="h3" sx={{ fontWeight: 800, color: '#39FF14', my: 1 }}>ZERO</Typography>
              <Typography variant="caption" sx={{ color: '#8E9BAE' }}>Unmediated Actions Blocked (I16-C)</Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={(_, val) => setActiveTab(val)} sx={{ '& .MuiTab-root': { color: '#8E9BAE', '&.Mui-selected': { color: '#00FFFF' } } }}>
          <Tab label="Governed Worker Fleet" />
          <Tab label="Sandbox Profiles & Resource Limits" />
          <Tab label="Batch 6 ActionProposals" />
          <Tab label="Architecture & Invariants" />
        </Tabs>
      </Box>

      {/* Tab 0: Worker Fleet */}
      {activeTab === 0 && (
        <TableContainer component={Paper} sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
          <Table>
            <TableHead>
              <TableRow sx={{ bgcolor: 'rgba(255,255,255,0.02)' }}>
                <TableCell sx={{ color: '#8E9BAE' }}>WORKER IDENTIFIER</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>MODALITY</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>SUPPORTED CAPABILITIES</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>CIRCUIT STATE</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>EXECUTIONS</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>UPTIME</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {workers.map((w) => (
                <TableRow key={w.id} sx={{ '&:hover': { bgcolor: 'rgba(255,255,255,0.03)' } }}>
                  <TableCell sx={{ color: '#FFFFFF', fontWeight: 600 }}>
                    <Stack direction="row" spacing={1.5} alignItems="center">
                      {getModalityIcon(w.modality)}
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>{w.name}</Typography>
                        <Typography variant="caption" sx={{ color: '#8E9BAE' }}>{w.id}</Typography>
                      </Box>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Chip label={w.modality.toUpperCase()} size="small" sx={{ bgcolor: 'rgba(255,255,255,0.05)', color: '#FFFFFF' }} />
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={0.5} flexWrap="wrap">
                      {w.supportedCapabilities.map((c) => (
                        <Chip key={c} label={c} size="small" sx={{ bgcolor: 'rgba(0, 255, 255, 0.1)', color: '#00FFFF', fontSize: '0.7rem' }} />
                      ))}
                    </Stack>
                  </TableCell>
                  <TableCell>{getCircuitChip(w.circuitState)}</TableCell>
                  <TableCell sx={{ color: '#FFFFFF' }}>{w.totalExecutions.toLocaleString()}</TableCell>
                  <TableCell sx={{ color: '#39FF14' }}>{w.uptimePct.toFixed(2)}%</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Tab 1: Sandbox Profiles */}
      {activeTab === 1 && (
        <Grid container spacing={3}>
          {workers.map((w) => (
            <Grid item xs={12} md={6} key={w.id}>
              <Card sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
                <CardContent sx={{ p: 3 }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                    <Typography variant="h6" sx={{ color: '#FFFFFF', fontWeight: 700 }}>
                      {w.name} ({w.modality})
                    </Typography>
                    <Chip label="SANDBOX ACTIVE" size="small" sx={{ bgcolor: 'rgba(57, 255, 20, 0.15)', color: '#39FF14' }} />
                  </Stack>
                  <Divider sx={{ mb: 2, borderColor: 'rgba(255,255,255,0.08)' }} />
                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Typography variant="caption" sx={{ color: '#8E9BAE' }}>Memory Ceiling</Typography>
                      <Typography variant="body1" sx={{ color: '#00FFFF', fontWeight: 600 }}>{w.sandboxProfile.memoryLimitMB} MB</Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="caption" sx={{ color: '#8E9BAE' }}>CPU Cores Limit</Typography>
                      <Typography variant="body1" sx={{ color: '#00FFFF', fontWeight: 600 }}>{w.sandboxProfile.cpuCores} Cores</Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="caption" sx={{ color: '#8E9BAE' }}>Execution Timeout</Typography>
                      <Typography variant="body1" sx={{ color: '#FFFFFF', fontWeight: 600 }}>{w.sandboxProfile.maxTimeoutSec} Seconds</Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="caption" sx={{ color: '#8E9BAE' }}>Shell Access</Typography>
                      <Typography variant="body1" sx={{ color: '#FF3B30', fontWeight: 600 }}>DEFAULT-DENY (False)</Typography>
                    </Grid>
                    <Grid item xs={12}>
                      <Typography variant="caption" sx={{ color: '#8E9BAE' }}>Network Containment</Typography>
                      <Typography variant="body2" sx={{ color: '#39FF14' }}>
                        RESTRICTED (Allowlist Only — Zero Unmediated Outbound Traversal)
                      </Typography>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Tab 2: Action Proposals */}
      {activeTab === 2 && (
        <TableContainer component={Paper} sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
          <Table>
            <TableHead>
              <TableRow sx={{ bgcolor: 'rgba(255,255,255,0.02)' }}>
                <TableCell sx={{ color: '#8E9BAE' }}>PROPOSAL ID</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>CAPABILITY</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>MODALITY</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>TARGET SYSTEM</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>SHA-256 DIGEST</TableCell>
                <TableCell sx={{ color: '#8E9BAE' }}>STATUS</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {proposals.map((p) => (
                <TableRow key={p.proposalId} sx={{ '&:hover': { bgcolor: 'rgba(255,255,255,0.03)' } }}>
                  <TableCell sx={{ color: '#00FFFF', fontFamily: 'monospace' }}>{p.proposalId}</TableCell>
                  <TableCell sx={{ color: '#FFFFFF' }}>{p.capabilityId}</TableCell>
                  <TableCell>
                    <Chip label={p.modality} size="small" sx={{ bgcolor: 'rgba(255,255,255,0.05)', color: '#FFFFFF' }} />
                  </TableCell>
                  <TableCell sx={{ color: '#8E9BAE' }}>{p.targetSystem}</TableCell>
                  <TableCell sx={{ color: '#8E9BAE', fontFamily: 'monospace', fontSize: '0.75rem' }}>
                    {p.payloadDigest.substring(0, 16)}...
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={p.status === 'FirewallApproved' ? 'FIREWALL APPROVED' : 'PENDING ADMISSION'}
                      size="small"
                      sx={{
                        bgcolor: p.status === 'FirewallApproved' ? 'rgba(57, 255, 20, 0.15)' : 'rgba(0, 255, 255, 0.15)',
                        color: p.status === 'FirewallApproved' ? '#39FF14' : '#00FFFF',
                        border: `1px solid ${p.status === 'FirewallApproved' ? '#39FF14' : '#00FFFF'}`,
                      }}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Tab 3: Architecture & Invariants */}
      {activeTab === 3 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" sx={{ color: '#00FFFF', fontWeight: 700, mb: 1 }}>
                  Invariant Enforcement Matrix
                </Typography>
                <Divider sx={{ mb: 2, borderColor: 'rgba(255,255,255,0.08)' }} />
                <Stack spacing={2}>
                  <Box>
                    <Typography variant="subtitle2" sx={{ color: '#FFFFFF', fontWeight: 600 }}>
                      I16: Worker Modality Isolation
                    </Typography>
                    <Typography variant="body2" sx={{ color: '#8E9BAE' }}>
                      Cross-modality traversal without a separate capability lease is strictly blocked. An API worker cannot execute under MCP, Browser, or Desktop adapters.
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" sx={{ color: '#FFFFFF', fontWeight: 600 }}>
                      I16-A: Sandbox & Resource Hard Ceilings
                    </Typography>
                    <Typography variant="body2" sx={{ color: '#8E9BAE' }}>
                      Immutable ceilings for CPU, memory, timeout, token allowance, process spawning, and network egress. Violations immediately halt execution.
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" sx={{ color: '#FFFFFF', fontWeight: 600 }}>
                      I16-B: MCP Tool Isolation
                    </Typography>
                    <Typography variant="body2" sx={{ color: '#8E9BAE' }}>
                      Prevents leakage of system prompts, master secrets, API keys, environment variables, or cross-tenant state. Breaches trigger permanent quarantine.
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" sx={{ color: '#FFFFFF', fontWeight: 600 }}>
                      I16-C: Zero Direct External Consequentiality
                    </Typography>
                    <Typography variant="body2" sx={{ color: '#8E9BAE' }}>
                      Workers never directly mutate external systems. All state-mutating actions emit cryptographic ActionProposals routed to Batch 6 Firewall.
                    </Typography>
                  </Box>
                </Stack>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card sx={{ bgcolor: '#0B0F19', border: '1px solid rgba(255,255,255,0.08)' }}>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" sx={{ color: '#39FF14', fontWeight: 700, mb: 1 }}>
                  Firewall Path Sovereignty (User Mandatory Law)
                </Typography>
                <Divider sx={{ mb: 2, borderColor: 'rgba(255,255,255,0.08)' }} />
                <Paper sx={{ p: 2, bgcolor: '#040508', border: '1px solid rgba(0, 255, 255, 0.2)', fontFamily: 'monospace', color: '#00FFFF', fontSize: '0.85rem' }}>
                  Worker<br />
                  &nbsp;&nbsp;↓<br />
                  ActionProposal (SHA-256 Digest)<br />
                  &nbsp;&nbsp;↓<br />
                  Worker Action Proposal Gateway<br />
                  &nbsp;&nbsp;↓<br />
                  Runtime Admission Gate<br />
                  &nbsp;&nbsp;↓<br />
                  Batch 6 Execution Firewall<br />
                  &nbsp;&nbsp;↓<br />
                  Connector Gateway<br />
                  &nbsp;&nbsp;↓<br />
                  External World
                </Paper>
                <Typography variant="body2" sx={{ color: '#8E9BAE', mt: 2 }}>
                  The Worker Fabric submits to Batch 6; it does NOT become an alternate authorization layer or grant execution permits.
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}
    </Box>
  );
};

export default WorkerFabric;
