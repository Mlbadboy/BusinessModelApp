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
  Divider,
  Grid,
  Tabs,
  Tab,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  LinearProgress,
  CircularProgress,
} from '@mui/material';
import {
  HubOutlined,
  SpeedOutlined,
  ShieldOutlined,
  AttachMoneyOutlined,
  RefreshOutlined,
  PsychologyAltOutlined,
  DeviceHubOutlined,
  AccountTreeOutlined,
  SecurityOutlined,
  FiberManualRecord,
} from '@mui/icons-material';
import {
  workforceService,
  BrainSpaceTelemetry,
  BrainSpaceAgentNode,
} from '../../services/workforceService';

// ─── Color & Status Helpers ──────────────────────────────────────────────────

const stateColor = (state: string) => {
  switch (state.toUpperCase()) {
    case 'ACTIVE':
    case 'RUNNING':
    case 'EXECUTING': return '#00F0FF';
    case 'IDLE':
    case 'WAITING': return '#94A3B8';
    case 'SUSPENDED':
    case 'THROTTLED': return '#F59E0B';
    case 'DECOMMISSIONED':
    case 'TERMINATED':
    case 'FAILED': return '#EF4444';
    default: return '#64748B';
  }
};

export const BrainSpace = () => {
  const [telemetry, setTelemetry] = useState<BrainSpaceTelemetry | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedAgent, setSelectedAgent] = useState<BrainSpaceAgentNode | null>(null);
  const [filterRole, setFilterRole] = useState('ALL');
  const [activeTab, setActiveTab] = useState(0);

  const fetchTelemetry = useCallback(async () => {
    try {
      setLoading(true);
      const data = await workforceService.getBrainSpaceSnapshot();
      setTelemetry(data);
    } catch (err) {
      console.error('Failed to fetch Brain Space telemetry', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTelemetry();
    const interval = setInterval(fetchTelemetry, 10000);
    return () => clearInterval(interval);
  }, [fetchTelemetry]);

  const agents = telemetry?.workforceTopology || [];
  const filteredAgents = filterRole === 'ALL'
    ? agents
    : agents.filter(a => a.role.toUpperCase() === filterRole.toUpperCase());

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1600, margin: '0 auto' }}>
      {/* Header Bar */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Avatar sx={{ bgcolor: 'rgba(0, 240, 255, 0.1)', color: '#00F0FF', border: '1px solid rgba(0, 240, 255, 0.3)' }}>
              <PsychologyAltOutlined />
            </Avatar>
            <Box>
              <Typography variant="h5" fontWeight="bold" sx={{ color: '#F8FAFC', letterSpacing: '-0.02em' }}>
                Brain Space 4.3 — Workforce Topology & Real-Time Telemetry
              </Typography>
              <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                Governed Autonomous Agent Fabric • Batch 6 Firewall Enforced • Constitutional Invariants I39-A..Z Active
              </Typography>
            </Box>
          </Stack>
        </Box>
        <Stack direction="row" spacing={1.5}>
          <Chip
            icon={<FiberManualRecord sx={{ fontSize: '10px !important', color: '#22C55E' }} />}
            label="Continuous Telemetry Active"
            sx={{
              backgroundColor: 'rgba(34, 197, 94, 0.1)',
              color: '#22C55E',
              border: '1px solid rgba(34, 197, 94, 0.3)',
              fontWeight: 600,
            }}
          />
          <Button
            variant="outlined"
            size="small"
            startIcon={<RefreshOutlined />}
            onClick={fetchTelemetry}
            sx={{
              borderColor: 'rgba(255, 255, 255, 0.15)',
              color: '#F8FAFC',
              '&:hover': { borderColor: '#00F0FF', bgcolor: 'rgba(0, 240, 255, 0.05)' },
            }}
          >
            Refresh
          </Button>
        </Stack>
      </Box>

      {/* Top Telemetry Grid */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        {/* System Telemetry */}
        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ bgcolor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
            <CardContent sx={{ p: 2 }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600 }}>SYSTEM COMPUTE</Typography>
                <SpeedOutlined sx={{ color: '#00F0FF', fontSize: 18 }} />
              </Stack>
              <Typography variant="h6" fontWeight="bold" sx={{ color: '#F8FAFC', mt: 0.5 }}>
                {telemetry?.systemTelemetry?.cpuUtilizationPercent?.toFixed(1) || '0.0'}% CPU
              </Typography>
              <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                Memory: {telemetry?.systemTelemetry?.ramAllocatedMb?.toFixed(0) || '0'} MB • Q-Depth: {telemetry?.systemTelemetry?.eventQueueDepth || 0}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ bgcolor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
            <CardContent sx={{ p: 2 }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600 }}>GOVERNED AGENTS</Typography>
                <HubOutlined sx={{ color: '#8B5CF6', fontSize: 18 }} />
              </Stack>
              <Typography variant="h6" fontWeight="bold" sx={{ color: '#F8FAFC', mt: 0.5 }}>
                {telemetry?.systemTelemetry?.activeAgentsCount || agents.length} Active / 32 Max
              </Typography>
              <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                Tool Execution RPS: {telemetry?.systemTelemetry?.governedToolsExecutionRps?.toFixed(1) || '0.0'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ bgcolor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
            <CardContent sx={{ p: 2 }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600 }}>FIREWALL STATUS</Typography>
                <ShieldOutlined sx={{ color: '#10B981', fontSize: 18 }} />
              </Stack>
              <Typography variant="h6" fontWeight="bold" sx={{ color: '#10B981', mt: 0.5 }}>
                {telemetry?.systemTelemetry?.isExecutionFirewallArmed ? 'ARMED & ENFORCING' : 'READY'}
              </Typography>
              <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                Uptime: {((telemetry?.systemTelemetry?.uptimeSeconds || 0) / 3600).toFixed(1)} hrs • Error: 0.0%
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ bgcolor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
            <CardContent sx={{ p: 2 }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600 }}>REALIZED VALUE</Typography>
                <AttachMoneyOutlined sx={{ color: '#22C55E', fontSize: 18 }} />
              </Stack>
              <Typography variant="h6" fontWeight="bold" sx={{ color: '#22C55E', mt: 0.5 }}>
                ${(telemetry?.businessTelemetry?.totalRealizedRevenue || 0).toLocaleString()}
              </Typography>
              <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                Pipeline: ${(telemetry?.businessTelemetry?.totalPipelineValue || 0).toLocaleString()} • Net Margin: ${(telemetry?.businessTelemetry?.netRealizedMargin || 0).toLocaleString()}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Main Section */}
      <Card sx={{ bgcolor: '#0D1118', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2, mb: 3 }}>
        <Box sx={{ borderBottom: 1, borderColor: 'rgba(255, 255, 255, 0.08)', px: 2 }}>
          <Tabs
            value={activeTab}
            onChange={(_, val) => setActiveTab(val)}
            sx={{
              '& .MuiTab-root': { color: '#94A3B8', '&.Mui-selected': { color: '#00F0FF' } },
              '& .MuiTabs-indicator': { backgroundColor: '#00F0FF' },
            }}
          >
            <Tab icon={<DeviceHubOutlined sx={{ fontSize: 18 }} />} iconPosition="start" label="Workforce Topology" />
            <Tab icon={<AccountTreeOutlined sx={{ fontSize: 18 }} />} iconPosition="start" label="Economics & Metrology" />
            <Tab icon={<SecurityOutlined sx={{ fontSize: 18 }} />} iconPosition="start" label="Constitutional Boundary (I39)" />
          </Tabs>
        </Box>

        <CardContent sx={{ p: 3 }}>
          {activeTab === 0 && (
            <Box>
              {/* Filter bar */}
              <Stack direction="row" spacing={1} sx={{ mb: 2.5 }} alignItems="center">
                <Typography variant="caption" sx={{ color: '#64748B', mr: 1, fontWeight: 600 }}>FILTER ROLE:</Typography>
                {['ALL', 'CHIEF_EXECUTIVE', 'REVENUE_OFFICER', 'COMMERCIAL_DIRECTOR', 'DISCOVERY_AGENT', 'DELIVERY_ORCHESTRATOR', 'VALUE_ENGINEER'].map((r) => (
                  <Chip
                    key={r}
                    label={r.replace('_', ' ')}
                    size="small"
                    onClick={() => setFilterRole(r)}
                    sx={{
                      bgcolor: filterRole === r ? 'rgba(0, 240, 255, 0.15)' : 'rgba(255, 255, 255, 0.03)',
                      color: filterRole === r ? '#00F0FF' : '#94A3B8',
                      border: filterRole === r ? '1px solid rgba(0, 240, 255, 0.4)' : '1px solid transparent',
                      cursor: 'pointer',
                      fontSize: '0.75rem',
                      fontWeight: 600,
                    }}
                  />
                ))}
              </Stack>

              {/* Agent Grid */}
              {loading && !telemetry ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
                  <CircularProgress sx={{ color: '#00F0FF' }} />
                </Box>
              ) : filteredAgents.length === 0 ? (
                <Box sx={{ textAlign: 'center', py: 6 }}>
                  <Typography variant="body2" sx={{ color: '#64748B' }}>
                    No autonomous agents currently provisioned for this filter.
                  </Typography>
                </Box>
              ) : (
                <Grid container spacing={2}>
                  {filteredAgents.map((agent) => (
                    <Grid item xs={12} sm={6} md={4} key={agent.agentId}>
                      <Card
                        sx={{
                          bgcolor: '#111722',
                          border: '1px solid rgba(255, 255, 255, 0.06)',
                          borderRadius: 2,
                          transition: 'all 0.2s ease',
                          cursor: 'pointer',
                          '&:hover': {
                            borderColor: 'rgba(0, 240, 255, 0.3)',
                            transform: 'translateY(-2px)',
                            boxShadow: '0 4px 20px rgba(0, 240, 255, 0.08)',
                          },
                        }}
                        onClick={() => setSelectedAgent(agent)}
                      >
                        <CardContent sx={{ p: 2 }}>
                          <Stack direction="row" justifyContent="space-between" alignItems="flex-start" sx={{ mb: 1.5 }}>
                            <Box>
                              <Typography variant="subtitle2" fontWeight="bold" sx={{ color: '#F8FAFC' }}>
                                {agent.role.replace('_', ' ')}
                              </Typography>
                              <Typography variant="caption" sx={{ color: '#64748B', fontFamily: 'monospace' }}>
                                {agent.agentId.substring(0, 16)}...
                              </Typography>
                            </Box>
                            <Chip
                              label={agent.operationalState}
                              size="small"
                              sx={{
                                bgcolor: `${stateColor(agent.operationalState)}15`,
                                color: stateColor(agent.operationalState),
                                border: `1px solid ${stateColor(agent.operationalState)}40`,
                                fontSize: '0.6875rem',
                                fontWeight: 700,
                              }}
                            />
                          </Stack>

                          {agent.currentTask && (
                            <Box sx={{ p: 1, bgcolor: 'rgba(0, 0, 0, 0.2)', borderRadius: 1, mb: 1.5 }}>
                              <Typography variant="caption" noWrap sx={{ color: '#94A3B8', display: 'block' }}>
                                Task: {agent.currentTask}
                              </Typography>
                            </Box>
                          )}

                          <Stack spacing={1}>
                            <Box>
                              <Stack direction="row" justifyContent="space-between">
                                <Typography variant="caption" sx={{ color: '#64748B' }}>Budget Burn</Typography>
                                <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                                  ${agent.spentBudget.toFixed(2)} / ${agent.allocatedBudget.toFixed(2)}
                                </Typography>
                              </Stack>
                              <LinearProgress
                                variant="determinate"
                                value={Math.min(100, (agent.spentBudget / (agent.allocatedBudget || 1)) * 100)}
                                sx={{
                                  height: 4,
                                  borderRadius: 2,
                                  bgcolor: 'rgba(255, 255, 255, 0.05)',
                                  '& .MuiLinearProgress-bar': { bgcolor: '#00F0FF' },
                                }}
                              />
                            </Box>

                            <Stack direction="row" justifyContent="space-between" alignItems="center">
                              <Typography variant="caption" sx={{ color: '#64748B' }}>Health Score</Typography>
                              <Chip
                                label={`${(agent.healthScore * 100).toFixed(0)}%`}
                                size="small"
                                sx={{
                                  height: 20,
                                  bgcolor: agent.healthScore > 0.8 ? 'rgba(34, 197, 94, 0.1)' : 'rgba(245, 158, 11, 0.1)',
                                  color: agent.healthScore > 0.8 ? '#22C55E' : '#F59E0B',
                                  fontSize: '0.6875rem',
                                  fontWeight: 700,
                                }}
                              />
                            </Stack>
                          </Stack>
                        </CardContent>
                      </Card>
                    </Grid>
                  ))}
                </Grid>
              )}
            </Box>
          )}

          {activeTab === 1 && (
            <Box>
              <Typography variant="subtitle1" fontWeight="bold" sx={{ color: '#F8FAFC', mb: 2 }}>
                Autonomous P&L Metrology & Economic Ledger
              </Typography>
              <Typography variant="body2" sx={{ color: '#94A3B8', mb: 3 }}>
                Each agent operates as a self-accounting economic unit. Revenue is strictly recognized when certified by evidence lineage. No speculative tokens.
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Card sx={{ bgcolor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)', p: 2 }}>
                    <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600 }}>ECONOMIC INVARIANTS</Typography>
                    <Stack spacing={1.5} sx={{ mt: 1.5 }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="body2" sx={{ color: '#F8FAFC' }}>Total Operational Spend (Tokens + Compute)</Typography>
                        <Typography variant="body2" fontWeight="bold" sx={{ color: '#EF4444' }}>
                          ${(telemetry?.businessTelemetry?.totalOperationalCost || 0).toLocaleString()}
                        </Typography>
                      </Box>
                      <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.06)' }} />
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="body2" sx={{ color: '#F8FAFC' }}>Cryptographically Verified Realized Revenue</Typography>
                        <Typography variant="body2" fontWeight="bold" sx={{ color: '#22C55E' }}>
                          ${(telemetry?.businessTelemetry?.totalRealizedRevenue || 0).toLocaleString()}
                        </Typography>
                      </Box>
                      <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.06)' }} />
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="body2" sx={{ color: '#F8FAFC' }}>Net Contribution Margin</Typography>
                        <Typography variant="body2" fontWeight="bold" sx={{ color: '#00F0FF' }}>
                          ${(telemetry?.businessTelemetry?.netRealizedMargin || 0).toLocaleString()}
                        </Typography>
                      </Box>
                    </Stack>
                  </Card>
                </Grid>

                <Grid item xs={12} md={6}>
                  <Card sx={{ bgcolor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)', p: 2 }}>
                    <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600 }}>REVENUE PURITY METRICS</Typography>
                    <Stack spacing={1.5} sx={{ mt: 1.5 }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="body2" sx={{ color: '#F8FAFC' }}>Unverified Claimed Pipeline</Typography>
                        <Typography variant="body2" fontWeight="bold" sx={{ color: '#F59E0B' }}>
                          ${(telemetry?.businessTelemetry?.unverifiedClaimedRevenue || 0).toLocaleString()}
                        </Typography>
                      </Box>
                      <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.06)' }} />
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="body2" sx={{ color: '#F8FAFC' }}>Cryptographic Proof Blocks</Typography>
                        <Typography variant="body2" fontWeight="bold" sx={{ color: '#8B5CF6' }}>
                          {telemetry?.businessTelemetry?.cryptographicLineageCount || 0} Blocks
                        </Typography>
                      </Box>
                      <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.06)' }} />
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="body2" sx={{ color: '#F8FAFC' }}>PRG-1 Sovereign Approvals Pending</Typography>
                        <Typography variant="body2" fontWeight="bold" sx={{ color: '#EC4899' }}>
                          {telemetry?.businessTelemetry?.sovereignApprovalPendingCount || 0} Gates
                        </Typography>
                      </Box>
                    </Stack>
                  </Card>
                </Grid>
              </Grid>
            </Box>
          )}

          {activeTab === 2 && (
            <Box>
              <Typography variant="subtitle1" fontWeight="bold" sx={{ color: '#F8FAFC', mb: 2 }}>
                Workforce Constitution Law I39 (A–Z)
              </Typography>
              <Typography variant="body2" sx={{ color: '#94A3B8', mb: 3 }}>
                Non-negotiable invariants governing all AI agent operations, memory boundaries, tool certifications, and sovereign execution gates.
              </Typography>
              <Grid container spacing={1.5}>
                {[
                  { code: 'I39-A', rule: 'AGENT != ROLE != SKILL != TOOL != AUTHORITY', desc: 'Entities cannot conflate identity with authority or capability' },
                  { code: 'I39-B', rule: 'HIERARCHICAL GOVERNANCE', desc: 'Max spawn depth 2, max 3 children, supervisor holds termination sovereignty' },
                  { code: 'I39-C', rule: 'TENANT MEMORY ISOLATION', desc: 'Cross-tenant memory contamination results in immediate agent decommission' },
                  { code: 'I39-D', rule: 'TOOL CERTIFICATION REQUIRED', desc: 'No uncertified tools may be invoked by any autonomous agent' },
                  { code: 'I39-E', rule: 'PRG-1 HUMAN SOVEREIGNTY', desc: 'Consequential external actions (R3+) strictly require human signature' },
                  { code: 'I39-F', rule: 'CRYPTOGRAPHIC REVENUE LINEAGE', desc: 'Revenue is only real when backed by 13-node cryptographic block chain' },
                  { code: 'I39-G', rule: 'RECURSIVE DRIFT DETECTION', desc: 'Goal divergence > 0.3 triggers automated suspension and triage' },
                  { code: 'I39-H', rule: 'HARDENED CRASH RECOVERY', desc: 'Durable checkpointing guarantees zero state corruption on restart' },
                ].map((inv) => (
                  <Grid item xs={12} sm={6} key={inv.code}>
                    <Box sx={{ p: 1.5, bgcolor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)', borderRadius: 1.5 }}>
                      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                        <Chip label={inv.code} size="small" sx={{ bgcolor: 'rgba(0, 240, 255, 0.1)', color: '#00F0FF', fontWeight: 700 }} />
                        <Typography variant="subtitle2" fontWeight="bold" sx={{ color: '#F8FAFC' }}>{inv.rule}</Typography>
                      </Stack>
                      <Typography variant="caption" sx={{ color: '#94A3B8' }}>{inv.desc}</Typography>
                    </Box>
                  </Grid>
                ))}
              </Grid>
            </Box>
          )}
        </CardContent>
      </Card>

      {/* Agent Detail Modal */}
      <Dialog
        open={Boolean(selectedAgent)}
        onClose={() => setSelectedAgent(null)}
        PaperProps={{
          sx: {
            bgcolor: '#0D1118',
            border: '1px solid rgba(255, 255, 255, 0.12)',
            minWidth: 460,
          },
        }}
      >
        <DialogTitle sx={{ color: '#F8FAFC', fontWeight: 'bold' }}>
          Agent Telemetry: {selectedAgent?.role.replace('_', ' ')}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Box>
              <Typography variant="caption" sx={{ color: '#64748B' }}>AGENT ID</Typography>
              <Typography variant="body2" sx={{ color: '#F8FAFC', fontFamily: 'monospace' }}>{selectedAgent?.agentId}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" sx={{ color: '#64748B' }}>OPERATIONAL STATE</Typography>
              <Typography variant="body2" sx={{ color: stateColor(selectedAgent?.operationalState || '') }}>{selectedAgent?.operationalState}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" sx={{ color: '#64748B' }}>ASSIGNED MISSION</Typography>
              <Typography variant="body2" sx={{ color: '#F8FAFC' }}>{selectedAgent?.assignedMissionId || 'None (General Standing Pool)'}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" sx={{ color: '#64748B' }}>MEMORY ALLOCATION</Typography>
              <Typography variant="body2" sx={{ color: '#F8FAFC' }}>{((selectedAgent?.activeMemoryAllocatedBytes || 0) / 1024).toFixed(1)} KB</Typography>
            </Box>
            <Box>
              <Typography variant="caption" sx={{ color: '#64748B' }}>ECONOMIC CONTRIBUTION</Typography>
              <Typography variant="body2" sx={{ color: '#22C55E' }}>${selectedAgent?.revenueGenerated.toFixed(2)}</Typography>
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setSelectedAgent(null)} sx={{ color: '#94A3B8' }}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default BrainSpace;
