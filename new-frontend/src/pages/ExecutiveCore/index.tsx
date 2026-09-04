import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Button,
  Chip,
  TextField,
  Divider,
  Drawer,
  IconButton,
  Alert,
  CircularProgress,
  Stack,
  Paper,
  Tabs,
  Tab
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import ShieldIcon from '@mui/icons-material/Shield';
import PsychologyIcon from '@mui/icons-material/Psychology';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import PublicIcon from '@mui/icons-material/Public';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import ScienceIcon from '@mui/icons-material/Science';
import GavelIcon from '@mui/icons-material/Gavel';
import AccountTreeIcon from '@mui/icons-material/AccountTree';
import GroupsIcon from '@mui/icons-material/Groups';
import { DigitalTwinExplorer } from '../../components/DigitalTwin/DigitalTwinExplorer';

interface StrategicAssumption {
  key: string;
  assumptionDescription?: string;
  description?: string;
  assumedValue: string;
  source?: number;
  origin?: number;
  confidence: number;
}

interface StrategyRoute {
  id: string;
  strategyName: string;
  targetMarket?: string;
  targetACV_INR: number;
  expectedWinRate: number;
  requiredClosedDeals: number;
  requiredPipelineINR: number;
  requiredQualifiedOppsCount?: number;
  requiredMeetingsCount: number;
  requiredProspectsCount: number;
  deliveryCapacitySlotsRequired: number;
  expectedGrossMarginPercent: number;
  feasibilityState: number; // 1=Feasible, 2=ConditionallyFeasible, 3=CapacityBlocked, 4=EvidenceInsufficient
  feasibilityReason?: string;
  isRecommended: boolean;
  assumptions?: StrategicAssumption[];
  isDeterministic?: boolean;
}

interface WhyCharlieData {
  decisionId: string;
  objectiveTitle: string;
  revenueGapINR: number;
  selectedStrategyName: string;
  selectedAlternative: string;
  alternativesConsidered: string[];
  expectedRevenueINR: number;
  expectedMarginPercent: number;
  winProbability: number;
  deliveryFeasibilityScore: number;
  decisionRationale: string;
  groundingEvidenceHashes: string[];
  constitutionRulesEvaluated: string[];
  decidedAt: string;
  cryptographicHash?: string;
}

export const ExecutiveCore: React.FC = () => {
  const [prompt, setPrompt] = useState('Generate ₹50L revenue in 60 days through Enterprise AI and Custom Software');
  const [loading, setLoading] = useState(false);
  const [activeObjective, setActiveObjective] = useState<any>(null);
  const [snapshot, setSnapshot] = useState<any>(null);
  const [strategies, setStrategies] = useState<StrategyRoute[]>([]);
  const [activeDecision, setActiveDecision] = useState<any>(null);
  const [activeMission, setActiveMission] = useState<any>(null);
  const [activeTab, setActiveTab] = useState<number>(0);
  const [whyDrawerOpen, setWhyDrawerOpen] = useState(false);
  const [whyData, setWhyData] = useState<WhyCharlieData | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const fetchActiveState = async () => {
    try {
      const token = localStorage.getItem('token');
      const headers = { 'Authorization': `Bearer ${token}` };

      // Fetch snapshot
      const snapRes = await fetch('/api/world-model/snapshot', { headers });
      if (snapRes.ok) {
        const snapData = await snapRes.json();
        setSnapshot(snapData);
      }

      // Fetch active objective
      const objRes = await fetch('/api/objectives/active', { headers });
      if (objRes.ok) {
        const objData = await objRes.json();
        if (objData.activeObjective) {
          setActiveObjective(objData.activeObjective);
          setStrategies(objData.strategies || []);
          setActiveDecision(objData.activeDecision || null);
          setActiveMission(objData.activeMission || null);
        }
      }
    } catch (err: any) {
      console.error('Failed to load active executive state', err);
    }
  };

  useEffect(() => {
    fetchActiveState();
  }, []);

  const handleIngestPrompt = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    setErrorMsg(null);
    try {
      const token = localStorage.getItem('token');
      const res = await fetch('/api/objectives', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ prompt })
      });

      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || 'Failed to ingest executive objective.');
      }

      const data = await res.json();
      setActiveObjective(data.objective);
      setSnapshot(data.snapshot);
      setStrategies(data.strategies || []);
      setActiveDecision(null);
      setActiveMission(null);
      setActiveTab(2); // Jump to Strategy Lab
    } catch (err: any) {
      setErrorMsg(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectStrategy = async (strategyId: string) => {
    setLoading(true);
    setErrorMsg(null);
    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`/api/objectives/${activeObjective.id}/strategies/${strategyId}/select`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || 'Failed to select strategy and formulate mission.');
      }

      const data = await res.json();
      setActiveDecision(data.decision);
      setActiveMission(data.mission);
      setActiveTab(4); // Jump to Mission Graph
    } catch (err: any) {
      setErrorMsg(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleOpenWhyDrawer = async () => {
    if (!activeDecision?.id) return;
    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`/api/decisions/${activeDecision.id}/why`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setWhyData(data);
        setWhyDrawerOpen(true);
      }
    } catch (err: any) {
      console.error('Failed to load Why Charlie explainability', err);
    }
  };

  const formatINR = (val: number) => {
    return '₹' + Number(val || 0).toLocaleString('en-IN', { maximumFractionDigits: 0 });
  };

  const getFeasibilityBadge = (state: number) => {
    switch (state) {
      case 1:
        return <Chip label="FEASIBLE (EVIDENCED)" color="success" size="small" sx={{ fontWeight: 700 }} />;
      case 2:
        return <Chip label="HYPOTHETICAL UNVERIFIED" color="warning" size="small" sx={{ fontWeight: 700 }} />;
      case 3:
        return <Chip label="CAPACITY BLOCKED" color="error" size="small" sx={{ fontWeight: 700 }} />;
      case 4:
        return <Chip label="EVIDENCE INSUFFICIENT" color="error" size="small" sx={{ fontWeight: 700 }} />;
      default:
        return <Chip label="PENDING" size="small" />;
    }
  };

  const getProvenanceBadge = (origin: number = 4) => {
    switch (origin) {
      case 1:
        return <Chip label="VERIFIED FACT" color="success" size="small" sx={{ fontSize: '0.7rem', fontWeight: 700 }} />;
      case 2:
        return <Chip label="EXPLICIT CEO INPUT" color="info" size="small" sx={{ fontSize: '0.7rem', fontWeight: 700 }} />;
      case 3:
        return <Chip label="HISTORICAL COMPANY DATA" color="primary" size="small" sx={{ fontSize: '0.7rem', fontWeight: 700 }} />;
      case 4:
        return <Chip label="AI ESTIMATE (UNVERIFIED)" color="warning" size="small" sx={{ fontSize: '0.7rem', fontWeight: 700 }} />;
      default:
        return <Chip label="UNKNOWN" sx={{ fontSize: '0.7rem' }} size="small" />;
    }
  };

  const getBaselineBadge = (state: number = 0) => {
    switch (state) {
      case 0:
        return <Chip label="BASELINE: UNAVAILABLE" sx={{ bgcolor: '#fef2f2', color: '#dc2626', border: '1px solid #fca5a5', fontWeight: 700 }} size="small" />;
      case 1:
        return <Chip label="BASELINE: VERIFIED ₹0" color="info" size="small" sx={{ fontWeight: 700 }} />;
      case 2:
        return <Chip label="BASELINE: VERIFIED" color="success" size="small" sx={{ fontWeight: 700 }} />;
      case 3:
        return <Chip label="BASELINE: PARTIALLY VERIFIED" color="warning" size="small" sx={{ fontWeight: 700 }} />;
      default:
        return <Chip label="UNVERIFIED" size="small" />;
    }
  };

  return (
    <Box sx={{ p: 4, bgcolor: '#f8fafc', minHeight: '100vh' }}>
      {/* Header */}
      <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
            <Typography variant="h4" sx={{ fontWeight: 800, color: '#0f172a', letterSpacing: '-0.02em' }}>
              Charlie Executive Mission Control
            </Typography>
            <Chip
              icon={<ShieldIcon sx={{ fontSize: 16 }} />}
              label="Phase 1 v1.2 Active"
              color="primary"
              size="small"
              sx={{ fontWeight: 700 }}
            />
          </Box>
          <Typography variant="body2" sx={{ color: '#64748b' }}>
            Autonomous Commercial AI Officer — Grounded Reality, Four-State Revenue Math, Strategy Lab, Hive Workforce & Durable Mission Linchpin
          </Typography>
        </Box>

        {activeDecision && (
          <Button
            variant="outlined"
            color="primary"
            startIcon={<PsychologyIcon />}
            onClick={handleOpenWhyDrawer}
            sx={{ fontWeight: 700, borderRadius: 2 }}
          >
            Why Charlie? [Audit Trace]
          </Button>
        )}
      </Box>

      {errorMsg && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setErrorMsg(null)}>
          {errorMsg}
        </Alert>
      )}

      {/* CEO Strategic Mandate Prompt Card */}
      <Card sx={{ mb: 3, borderRadius: 3, border: '1px solid #e2e8f0', boxShadow: '0 4px 6px -1px rgba(0,0,0,0.05)' }}>
        <CardContent sx={{ p: 2.5 }}>
          <Typography variant="caption" sx={{ fontWeight: 800, color: '#475569', textTransform: 'uppercase', letterSpacing: '0.08em', display: 'block', mb: 1 }}>
            Direct CEO Revenue Mandate
          </Typography>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <TextField
              fullWidth
              variant="outlined"
              size="small"
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              placeholder="e.g. Generate ₹50L revenue in 60 days through Enterprise AI and Custom Software"
              sx={{ bgcolor: '#fff' }}
            />
            <Button
              variant="contained"
              color="primary"
              disabled={loading}
              onClick={handleIngestPrompt}
              startIcon={loading ? <CircularProgress size={18} color="inherit" /> : <PlayArrowIcon />}
              sx={{ px: 4, fontWeight: 700, borderRadius: 2, whiteSpace: 'nowrap' }}
            >
              Simulate & Formulate
            </Button>
          </Box>
        </CardContent>
      </Card>

      {/* Navigation Tabs for the 6 Executive Mission Control Screens */}
      <Paper sx={{ mb: 3, borderRadius: 2, border: '1px solid #e2e8f0' }}>
        <Tabs
          value={activeTab}
          onChange={(_, val) => setActiveTab(val)}
          indicatorColor="primary"
          textColor="primary"
          variant="scrollable"
          scrollButtons="auto"
          sx={{ '& .MuiTab-root': { fontWeight: 700, textTransform: 'none', py: 1.5 } }}
        >
          <Tab icon={<PublicIcon fontSize="small" />} iconPosition="start" label="1. Company Digital Twin" />
          <Tab icon={<AccountBalanceIcon fontSize="small" />} iconPosition="start" label="2. Revenue Reality" />
          <Tab icon={<ScienceIcon fontSize="small" />} iconPosition="start" label="3. Strategy Lab" />
          <Tab icon={<GavelIcon fontSize="small" />} iconPosition="start" label="4. Decision Trace" />
          <Tab icon={<AccountTreeIcon fontSize="small" />} iconPosition="start" label="5. Mission Graph" />
          <Tab icon={<GroupsIcon fontSize="small" />} iconPosition="start" label="6. Agent Hive" />
        </Tabs>
      </Paper>

      {/* SCREEN 1: COMPANY DIGITAL TWIN */}
      {activeTab === 0 && (
        <Box sx={{ width: '100%' }}>
          <DigitalTwinExplorer />
        </Box>
      )}

      {/* SCREEN 2: REVENUE REALITY */}
      {activeTab === 1 && (
        <Card sx={{ borderRadius: 3, border: '1px solid #e2e8f0', p: 3 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
            <Typography variant="h6" sx={{ fontWeight: 800 }}>Four-State Revenue Baseline Matrix</Typography>
            {getBaselineBadge(snapshot?.revenueBaselineState)}
          </Box>
          <Grid container spacing={3}>
            <Grid item xs={12} md={3}>
              <Paper sx={{ p: 2.5, bgcolor: '#f1f5f9', borderRadius: 2 }}>
                <Typography variant="caption" sx={{ fontWeight: 700, color: '#475569' }}>STATE 1: CONTRACTED</Typography>
                <Typography variant="h5" sx={{ fontWeight: 800, my: 1 }}>{formatINR(snapshot?.verifiedRevenueINR?.value || 0)}</Typography>
                <Typography variant="caption" color="textSecondary">Signed contracts & active retainers</Typography>
              </Paper>
            </Grid>
            <Grid item xs={12} md={3}>
              <Paper sx={{ p: 2.5, bgcolor: '#f1f5f9', borderRadius: 2 }}>
                <Typography variant="caption" sx={{ fontWeight: 700, color: '#475569' }}>STATE 2: WEIGHTED PIPELINE</Typography>
                <Typography variant="h5" sx={{ fontWeight: 800, my: 1 }}>{formatINR(snapshot?.qualifiedPipelineINR?.value || 0)}</Typography>
                <Typography variant="caption" color="textSecondary">Validated CRM deals * stage probability</Typography>
              </Paper>
            </Grid>
            <Grid item xs={12} md={3}>
              <Paper sx={{ p: 2.5, bgcolor: '#f1f5f9', borderRadius: 2 }}>
                <Typography variant="caption" sx={{ fontWeight: 700, color: '#475569' }}>STATE 3: RUN RATE</Typography>
                <Typography variant="h5" sx={{ fontWeight: 800, my: 1 }}>{formatINR(0)}</Typography>
                <Typography variant="caption" color="textSecondary">Empirical historical recurring run rate</Typography>
              </Paper>
            </Grid>
            <Grid item xs={12} md={3}>
              <Paper sx={{ p: 2.5, bgcolor: '#eff6ff', border: '1px solid #bfdbfe', borderRadius: 2 }}>
                <Typography variant="caption" sx={{ fontWeight: 800, color: '#1d4ed8' }}>STATE 4: AUTONOMOUS GAP</Typography>
                <Typography variant="h5" sx={{ fontWeight: 800, my: 1, color: '#1e40af' }}>
                  {formatINR(activeObjective?.targetRevenueINR || 5000000)}
                </Typography>
                <Typography variant="caption" sx={{ color: '#1e40af', fontWeight: 600 }}>
                  Planning gap to capture. NOT guaranteed revenue.
                </Typography>
              </Paper>
            </Grid>
          </Grid>
        </Card>
      )}

      {/* SCREEN 3: STRATEGY LAB */}
      {activeTab === 2 && (
        <Grid container spacing={3}>
          {strategies.map((strat) => (
            <Grid item xs={12} md={4} key={strat.id}>
              <Card sx={{
                height: '100%',
                borderRadius: 3,
                border: strat.isRecommended ? '2px solid #2563eb' : '1px solid #e2e8f0',
                position: 'relative'
              }}>
                {strat.isRecommended && (
                  <Chip
                    label="RECOMMENDED ROUTE"
                    color="primary"
                    size="small"
                    sx={{ position: 'absolute', top: 12, right: 12, fontWeight: 800 }}
                  />
                )}
                <CardContent sx={{ p: 3 }}>
                  <Typography variant="h6" sx={{ fontWeight: 800, pr: 8, mb: 1 }}>
                    {strat.strategyName}
                  </Typography>
                  <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                    {getFeasibilityBadge(strat.feasibilityState)}
                    <Chip label={strat.isDeterministic !== false ? 'DETERMINISTIC' : 'AI HYPOTHESIS'} size="small" variant="outlined" />
                  </Box>
                  <Divider sx={{ my: 1.5 }} />
                  <Stack spacing={1} sx={{ mb: 2 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="caption" color="textSecondary">Target ACV</Typography>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>{formatINR(strat.targetACV_INR)}</Typography>
                    </Box>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="caption" color="textSecondary">Required Closed Deals</Typography>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>{strat.requiredClosedDeals} deals</Typography>
                    </Box>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="caption" color="textSecondary">Required Pipeline</Typography>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>{formatINR(strat.requiredPipelineINR)}</Typography>
                    </Box>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="caption" color="textSecondary">Required Accounts</Typography>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>{strat.requiredProspectsCount} accounts</Typography>
                    </Box>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="caption" color="textSecondary">Delivery Slots</Typography>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>{strat.deliveryCapacitySlotsRequired} slots</Typography>
                    </Box>
                  </Stack>

                  <Typography variant="caption" sx={{ fontWeight: 700, color: '#334155', display: 'block', mb: 1 }}>
                    Assumption Provenance Labels:
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 2.5 }}>
                    {strat.assumptions?.map((a, i) => (
                      <Box key={i}>{getProvenanceBadge(a.origin || a.source || 4)}</Box>
                    ))}
                  </Box>

                  <Button
                    fullWidth
                    variant={strat.isRecommended ? 'contained' : 'outlined'}
                    color="primary"
                    disabled={loading || strat.feasibilityState === 4}
                    onClick={() => handleSelectStrategy(strat.id)}
                    sx={{ fontWeight: 700, borderRadius: 2 }}
                  >
                    Select & Prepare Mission
                  </Button>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      {/* SCREEN 4: DECISION TRACE */}
      {activeTab === 3 && (
        <Card sx={{ borderRadius: 3, border: '1px solid #e2e8f0', p: 3 }}>
          <Typography variant="h6" sx={{ fontWeight: 800, mb: 2 }}>
            Autonomous Decision Record & Constitution Verification
          </Typography>
          {activeDecision ? (
            <Box>
              <Paper sx={{ p: 2, bgcolor: '#eff6ff', border: '1px solid #bfdbfe', borderRadius: 2, mb: 3 }}>
                <Typography variant="caption" sx={{ color: '#1e40af', fontWeight: 700 }}>DECISION COMMITMENT</Typography>
                <Typography variant="h6" sx={{ fontWeight: 800, color: '#1e3a8a', my: 0.5 }}>
                  Selected: {activeDecision.selectedAlternative}
                </Typography>
                <Typography variant="body2" sx={{ color: '#334155' }}>
                  {activeDecision.decisionRationale}
                </Typography>
              </Paper>
              <Grid container spacing={2}>
                <Grid item xs={12} md={4}>
                  <Typography variant="caption" color="textSecondary">Risk Score</Typography>
                  <Typography variant="h6" sx={{ fontWeight: 700 }}>{(activeDecision.riskScore * 100).toFixed(0)}%</Typography>
                </Grid>
                <Grid item xs={12} md={4}>
                  <Typography variant="caption" color="textSecondary">Autonomy Level</Typography>
                  <Typography variant="h6" sx={{ fontWeight: 700 }}>Level 3 (Controlled Autonomy)</Typography>
                </Grid>
                <Grid item xs={12} md={4}>
                  <Typography variant="caption" color="textSecondary">Approval Requirement</Typography>
                  <Typography variant="h6" sx={{ fontWeight: 700, color: activeDecision.humanApprovalRequired ? '#dc2626' : '#059669' }}>
                    {activeDecision.humanApprovalRequired ? 'MANDATORY HUMAN SIGN-OFF' : 'AUTONOMOUS PREPARATION OK'}
                  </Typography>
                </Grid>
              </Grid>
            </Box>
          ) : (
            <Typography variant="body2" color="textSecondary">
              No decision committed yet. Select a strategy from the Strategy Lab to commit an immutable DecisionRecord.
            </Typography>
          )}
        </Card>
      )}

      {/* SCREEN 5: MISSION GRAPH */}
      {activeTab === 4 && (
        <Card sx={{ borderRadius: 3, border: '1px solid #e2e8f0', p: 3 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 800 }}>Durable Mission Lineage & Checkpoints</Typography>
              <Typography variant="caption" sx={{ color: '#059669', fontWeight: 700 }}>
                PHASE 1 EXECUTION WALL ACTIVE — Side-effects safely gated at READY_FOR_EXECUTION
              </Typography>
            </Box>
            <Chip label="CHECKPOINT RECOVERY ENGINE ACTIVE" color="secondary" size="small" sx={{ fontWeight: 700 }} />
          </Box>

          <Grid container spacing={2}>
            {activeMission?.checkpoints?.map((cp: any) => (
              <Grid item xs={12} md={3} key={cp.id}>
                <Paper sx={{
                  p: 2,
                  borderRadius: 2,
                  border: cp.isCompleted ? '1px solid #10b981' : '1px solid #e2e8f0',
                  bgcolor: cp.isCompleted ? '#ecfdf5' : '#fff'
                }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                    {cp.isCompleted ? (
                      <CheckCircleIcon color="success" sx={{ fontSize: 18 }} />
                    ) : (
                      <Chip label={`Step ${cp.stepIndex}`} size="small" sx={{ fontWeight: 700 }} />
                    )}
                    <Typography variant="caption" sx={{ fontWeight: 700, color: '#334155' }}>
                      {cp.agentRole}
                    </Typography>
                  </Box>
                  <Typography variant="body2" sx={{ fontWeight: 600, color: '#0f172a', fontSize: '0.8rem' }}>
                    {cp.stepName}
                  </Typography>
                  <Typography variant="caption" sx={{ color: '#64748b', display: 'block', mt: 1 }}>
                    {cp.isCompleted ? '✓ Checkpointed' : cp.isBlockedOnApproval ? 'Waiting for Sign-Off' : 'Pending Execution'}
                  </Typography>
                </Paper>
              </Grid>
            ))}
          </Grid>
        </Card>
      )}

      {/* SCREEN 6: AGENT HIVE */}
      {activeTab === 5 && (
        <Grid container spacing={3}>
          {[
            { role: 'Charlie CEO', name: 'Executive Supervisor', status: 'Active', tasks: 1, inbox: 0 },
            { role: 'Market Intelligence', name: 'Demand & ICP Scout', status: 'Active', tasks: 1, inbox: 0 },
            { role: 'Prospect Discovery', name: 'Account Graph Agent', status: 'Idle', tasks: 0, inbox: 1 },
            { role: 'Lead Qualification', name: 'ICP Scoring Agent', status: 'Idle', tasks: 0, inbox: 0 },
            { role: 'Outreach & Comms', name: 'Engagement Agent', status: 'Gated (Phase 2)', tasks: 0, inbox: 0 },
            { role: 'Delivery Planning', name: 'Capacity & FinOps', status: 'Active', tasks: 1, inbox: 0 }
          ].map((agent, i) => (
            <Grid item xs={12} md={4} key={i}>
              <Paper sx={{ p: 2.5, borderRadius: 2, border: '1px solid #e2e8f0', bgcolor: '#fff' }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>{agent.role}</Typography>
                  <Chip
                    label={agent.status}
                    color={agent.status === 'Active' ? 'success' : agent.status === 'Gated (Phase 2)' ? 'secondary' : 'default'}
                    size="small"
                    sx={{ fontSize: '0.68rem', fontWeight: 700 }}
                  />
                </Box>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 2 }}>{agent.name}</Typography>
                <Divider sx={{ my: 1 }} />
                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Typography variant="caption">Blackboard Tasks: <strong>{agent.tasks}</strong></Typography>
                  <Typography variant="caption">Mailbox Unread: <strong>{agent.inbox}</strong></Typography>
                </Box>
              </Paper>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Why Charlie Audit Slide-Over Drawer */}
      <Drawer
        anchor="right"
        open={whyDrawerOpen}
        onClose={() => setWhyDrawerOpen(false)}
        PaperProps={{ sx: { width: { xs: '100%', md: 540 }, p: 4 } }}
      >
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <PsychologyIcon color="primary" sx={{ fontSize: 28 }} />
            <Typography variant="h6" sx={{ fontWeight: 800, color: '#0f172a' }}>
              Why Charlie? Explainability Audit
            </Typography>
          </Box>
          <IconButton onClick={() => setWhyDrawerOpen(false)}>
            <CloseIcon />
          </IconButton>
        </Box>

        {whyData ? (
          <Box>
            <Typography variant="subtitle2" sx={{ color: '#64748b', fontWeight: 600 }}>OBJECTIVE GAP</Typography>
            <Typography variant="h6" sx={{ fontWeight: 800, color: '#0f172a', mb: 2 }}>
              {whyData.objectiveTitle} — Gap: {formatINR(whyData.revenueGapINR)}
            </Typography>

            <Paper sx={{ p: 2, bgcolor: '#eff6ff', border: '1px solid #bfdbfe', borderRadius: 2, mb: 3 }}>
              <Typography variant="caption" sx={{ color: '#1e40af', fontWeight: 700 }}>SELECTED STRATEGY ROUTE</Typography>
              <Typography variant="body1" sx={{ fontWeight: 800, color: '#1e3a8a' }}>
                {whyData.selectedStrategyName}
              </Typography>
              <Typography variant="body2" sx={{ color: '#334155', mt: 1 }}>
                {whyData.decisionRationale}
              </Typography>
            </Paper>

            <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#334155', mb: 1 }}>
              Competing Alternatives Evaluated
            </Typography>
            <Stack spacing={1} sx={{ mb: 3 }}>
              {whyData.alternativesConsidered.map((alt, i) => (
                <Paper key={i} sx={{ p: 1.5, bgcolor: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: 1.5 }}>
                  <Typography variant="body2" sx={{ color: '#475569', fontSize: '0.82rem' }}>
                    {alt}
                  </Typography>
                </Paper>
              ))}
            </Stack>

            <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#334155', mb: 1 }}>
              Constitution Compliance Rules Checked
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 3 }}>
              {whyData.constitutionRulesEvaluated.map((rule, i) => (
                <Chip key={i} icon={<CheckCircleIcon sx={{ fontSize: 14 }} />} label={rule} color="success" size="small" variant="outlined" />
              ))}
            </Box>

            <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#334155', mb: 1 }}>
              Grounding Cryptographic Evidence Hashes
            </Typography>
            <Stack spacing={1}>
              {whyData.groundingEvidenceHashes.map((hash, i) => (
                <Paper key={i} sx={{ p: 1.5, bgcolor: '#0f172a', color: '#f8fafc', borderRadius: 1.5 }}>
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', fontSize: '0.72rem', wordBreak: 'break-all' }}>
                    {hash}
                  </Typography>
                </Paper>
              ))}
            </Stack>
          </Box>
        ) : (
          <CircularProgress />
        )}
      </Drawer>
    </Box>
  );
};

export default ExecutiveCore;
