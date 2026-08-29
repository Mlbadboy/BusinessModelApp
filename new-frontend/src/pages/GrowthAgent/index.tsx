import { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Chip,
  LinearProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  MenuItem,
  Alert,
} from '@mui/material';
import { Layout } from '../../components/Layout/Layout';
import { EvidenceDrawer, EvidenceData } from '../../components/ui/EvidenceDrawer';
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch';
import AccountBalanceWalletIcon from '@mui/icons-material/AccountBalanceWallet';
import HubIcon from '@mui/icons-material/Hub';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import PsychologyIcon from '@mui/icons-material/Psychology';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';

interface MissionTaskState {
  id: string;
  title: string;
  role: string;
  status: 'completed' | 'running' | 'blocked' | 'pending';
  costINR: number;
  evidenceId?: string;
  thought: string;
}

export const GrowthAgent = () => {
  // State
  const [mode, setMode] = useState<'simulation' | 'live'>('live');
  const [isLaunchModalOpen, setIsLaunchModalOpen] = useState<boolean>(false);
  const [isGatedApprovalOpen, setIsGatedApprovalOpen] = useState<boolean>(false);
  const [autonomyLevel, setAutonomyLevel] = useState<number>(3);
  const [evidenceDrawerOpen, setEvidenceDrawerOpen] = useState<boolean>(false);
  const [selectedEvidence, setSelectedEvidence] = useState<EvidenceData | null>(null);

  // Mission Success Controller State
  const [trajectory, setTrajectory] = useState<'on_track' | 'at_risk' | 'replanning'>('at_risk');
  const [isReplanned, setIsReplanned] = useState<boolean>(false);

  // Mission Metrics
  const [companiesResearched, setCompaniesResearched] = useState<number>(184);
  const [prospectsDiscovered, setProspectsDiscovered] = useState<number>(73);
  const [qualifiedCount, setQualifiedCount] = useState<number>(21);
  const [outreachSent, setOutreachSent] = useState<number>(18);
  const [responsesReceived, setResponsesReceived] = useState<number>(8);
  const [opportunitiesCreated, setOpportunitiesCreated] = useState<number>(1);
  const [pipelineGeneratedINR, setPipelineGeneratedINR] = useState<number>(2500000);

  // Mission Wallet
  const totalBudgetINR = 5000;
  const consumedINR = isReplanned ? 2680 : 1680;
  const reservedINR = isReplanned ? 0 : 500;
  const remainingINR = totalBudgetINR - consumedINR - reservedINR;

  // Base DAG Tasks
  const initialTasks: MissionTaskState[] = [
    { id: '1', title: 'Market Demand & Macro Signals', role: 'Market Intelligence Agent', status: 'completed', costINR: 0.25, evidenceId: 'EVD-MKT-991', thought: 'Discovered emerging surge in Indian BFSI AI governance transformations.' },
    { id: '2', title: 'Target Company Discovery', role: 'Prospect Discovery Agent', status: 'completed', costINR: 0.50, evidenceId: 'EVD-COMP-1842', thought: 'Identified 184 enterprise BFSI companies matching 500+ headcount ICP.' },
    { id: '3', title: 'Decision Maker Identification', role: 'Prospect Discovery Agent', status: 'completed', costINR: 0.75, evidenceId: 'EVD-DM-401', thought: 'Identified CIO and VP Transformation across 73 accounts.' },
    { id: '4', title: 'ICP Qualification & Scoring', role: 'Qualification Agent', status: 'completed', costINR: 0.40, evidenceId: 'EVD-QUAL-88', thought: 'Qualified 21 tier-1 enterprise prospects with 88.5+ fit score.' },
    { id: '5', title: 'Governed Evidence-Grounded Outreach', role: 'Outreach Agent', status: 'completed', costINR: 0.50, evidenceId: 'EVD-COMM-710', thought: 'Dispatched personalized outreach referencing active transformation initiatives.' },
    { id: '6', title: 'Conversation Intent Analysis', role: 'Conversation Agent', status: 'completed', costINR: 0.30, evidenceId: 'EVD-INTENT-03', thought: 'Analyzed 8 prospect responses. Confirmed 3 positive commercial intents.' },
    { id: '7', title: 'Opportunity Registration', role: 'Commercial Closer', status: 'completed', costINR: 0.50, evidenceId: 'EVD-OPP-992', thought: 'Registered ₹25,00,000 Opportunity: Apex FinCloud Operations.' },
    { id: '8', title: 'Commercial Proposal & Contract Terms', role: 'Proposal Agent', status: isGatedApprovalOpen ? 'blocked' : 'completed', costINR: 1.50, evidenceId: 'EVD-PROP-01', thought: 'Commercial proposal compiled. Gated on human approval under Level 3 autonomy.' },
  ];

  const adaptiveTasks: MissionTaskState[] = [
    { id: '9', title: 'Adaptive Prospecting: L&D & VP Transformation', role: 'Prospect Discovery Agent', status: 'completed', costINR: 0.50, evidenceId: 'EVD-ADAPT-01', thought: 'Identified 14 high-responsiveness transformation & L&D executives.' },
    { id: '10', title: 'Adaptive Governed Outreach: GenAI Governance', role: 'Outreach Agent', status: 'completed', costINR: 0.50, evidenceId: 'EVD-ADAPT-02', thought: 'Dispatched 8 targeted messages. Generated 4 positive meetings and additional ₹25L opportunity.' },
  ];

  const tasks = isReplanned ? [...initialTasks, ...adaptiveTasks] : initialTasks;

  const handleStartMission = () => {
    setIsLaunchModalOpen(false);
    setIsGatedApprovalOpen(true);
  };

  const handleApproveGatedAction = () => {
    setIsGatedApprovalOpen(false);
    setOpportunitiesCreated(2);
    setPipelineGeneratedINR(5000000);
  };

  const handleTriggerReplan = () => {
    setTrajectory('replanning');
    setTimeout(() => {
      setIsReplanned(true);
      setTrajectory('on_track');
      setCompaniesResearched(210);
      setProspectsDiscovered(87);
      setQualifiedCount(29);
      setOutreachSent(26);
      setResponsesReceived(12);
      setPipelineGeneratedINR(5000000);
    }, 600);
  };

  return (
    <Layout>
      <Box sx={{ p: 4, maxWidth: 1400, margin: '0 auto' }}>
        {/* HUD Top Bar */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, flexWrap: 'wrap', gap: 2 }}>
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <Typography variant="h4" sx={{ fontWeight: 800, color: '#F8FAFC', letterSpacing: '-0.02em' }}>
                JARVIS REVENUE MISSION CONTROL
              </Typography>
              <Chip
                label={mode === 'simulation' ? '◉ SIMULATION (SANDBOX)' : '● LIVE AUTONOMY (PRODUCTION)'}
                sx={{
                  bgcolor: mode === 'simulation' ? 'rgba(0, 229, 255, 0.1)' : 'rgba(16, 185, 129, 0.1)',
                  color: mode === 'simulation' ? '#00E5FF' : '#10B981',
                  border: `1px solid ${mode === 'simulation' ? 'rgba(0, 229, 255, 0.3)' : 'rgba(16, 185, 129, 0.3)'}`,
                  fontWeight: 800,
                }}
              />
              <Chip
                label={`TRAJECTORY: ${trajectory === 'on_track' ? 'ON TRACK' : (trajectory === 'replanning' ? 'RE-PLANNING' : 'AT RISK')}`}
                sx={{
                  bgcolor: trajectory === 'on_track' ? 'rgba(16, 185, 129, 0.15)' : 'rgba(245, 158, 11, 0.15)',
                  color: trajectory === 'on_track' ? '#10B981' : '#F59E0B',
                  fontWeight: 800,
                }}
              />
            </Box>
            <Typography variant="body2" sx={{ color: '#94A3B8' }}>
              Mission Controller • Governed Multi-Agent Execution • Autonomous Closed-Loop Re-planning
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', gap: 2 }}>
            <Button
              variant="outlined"
              onClick={() => setMode(mode === 'simulation' ? 'live' : 'simulation')}
              sx={{ color: '#94A3B8', borderColor: 'rgba(255, 255, 255, 0.15)' }}
            >
              Switch to {mode === 'simulation' ? 'Live Autonomy' : 'Simulation Mode'}
            </Button>
            <Button
              variant="contained"
              startIcon={<RocketLaunchIcon />}
              onClick={() => setIsLaunchModalOpen(true)}
              sx={{
                background: 'linear-gradient(135deg, #00E5FF, #00B0FF)',
                color: '#0A0E17',
                fontWeight: 800,
                boxShadow: '0 0 20px rgba(0, 229, 255, 0.4)',
                '&:hover': { background: '#00E5FF' },
              }}
            >
              Launch Mission
            </Button>
          </Box>
        </Box>

        {/* Mission Success Controller: Bottleneck Diagnosis Banner */}
        {trajectory === 'at_risk' && (
          <Alert
            severity="info"
            icon={<AutoFixHighIcon />}
            sx={{
              mb: 3,
              bgcolor: 'rgba(0, 240, 255, 0.08)',
              border: '1px solid rgba(0, 240, 255, 0.3)',
              color: '#F8FAFC',
            }}
            action={
              <Button
                variant="contained"
                size="small"
                onClick={handleTriggerReplan}
                sx={{ bgcolor: '#00E5FF', color: '#0A0E17', fontWeight: 800, '&:hover': { bgcolor: '#38BDF8' } }}
              >
                Execute Autonomous Re-plan
              </Button>
            }
          >
            <strong>MISSION SUCCESS CONTROLLER DIAGNOSIS:</strong> CIO outbound response rate is lagging (2.1%). Transformation & L&D leadership demonstrate 11.8% responsiveness to GenAI governance initiatives.
            <Typography variant="body2" sx={{ color: '#94A3B8', mt: 0.5 }}>
              Recommended Adaptive Plan: Shift 60% of prospecting and outreach toward VP of Digital Transformation and Head of L&D personas (Estimated +₹25L Pipeline yield).
            </Typography>
          </Alert>
        )}

        {/* Gated Approval Banner */}
        {isGatedApprovalOpen && (
          <Alert
            severity="warning"
            sx={{
              mb: 3,
              bgcolor: 'rgba(245, 158, 11, 0.1)',
              border: '1px solid rgba(245, 158, 11, 0.4)',
              color: '#FDE68A',
            }}
            action={
              <Button
                color="warning"
                variant="contained"
                size="small"
                onClick={handleApproveGatedAction}
                sx={{ fontWeight: 800 }}
              >
                Approve Proposal (₹25,00,000)
              </Button>
            }
          >
            <strong>POLICY GATE TRIGGERED:</strong> Proposal Agent drafted a ₹25,00,000 enterprise proposal. Commercial commitments require human executive approval under Autonomy Level 3.
          </Alert>
        )}

        {/* Real-time Telemetry & Wallet Row */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} md={3}>
            <Card sx={{ bgcolor: '#0F172A', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
              <CardContent sx={{ p: 2.5 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, color: '#00E5FF', mb: 1 }}>
                  <TrendingUpIcon fontSize="small" />
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>PIPELINE GENERATED</Typography>
                </Box>
                <Typography variant="h4" sx={{ fontWeight: 800, color: '#F8FAFC' }}>
                  ₹{(pipelineGeneratedINR / 100000).toFixed(1)}L
                </Typography>
                <Typography variant="caption" sx={{ color: '#10B981' }}>
                  Target: ₹50.0L ({((pipelineGeneratedINR / 5000000) * 100).toFixed(0)}% achieved)
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card sx={{ bgcolor: '#0F172A', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
              <CardContent sx={{ p: 2.5 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, color: '#00E5FF', mb: 1 }}>
                  <HubIcon fontSize="small" />
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>DISCOVERY FUNNEL</Typography>
                </Box>
                <Typography variant="h5" sx={{ fontWeight: 800, color: '#F8FAFC' }}>
                  {companiesResearched} Co. → {prospectsDiscovered} DM
                </Typography>
                <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                  {qualifiedCount} Qualified ({((qualifiedCount / prospectsDiscovered) * 100).toFixed(0)}% fit rate)
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card sx={{ bgcolor: '#0F172A', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
              <CardContent sx={{ p: 2.5 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, color: '#00E5FF', mb: 1 }}>
                  <PsychologyIcon fontSize="small" />
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>CONVERSATIONS & MEETINGS</Typography>
                </Box>
                <Typography variant="h5" sx={{ fontWeight: 800, color: '#F8FAFC' }}>
                  {outreachSent} Sent → {responsesReceived} Replies
                </Typography>
                <Typography variant="caption" sx={{ color: '#10B981' }}>
                  {opportunitiesCreated} Opps • 4 Meetings Booked
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card sx={{ bgcolor: '#0F172A', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
              <CardContent sx={{ p: 2.5 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, color: '#00E5FF', mb: 1 }}>
                  <AccountBalanceWalletIcon fontSize="small" />
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>MISSION WALLET</Typography>
                </Box>
                <Typography variant="h5" sx={{ fontWeight: 800, color: '#F8FAFC' }}>
                  ₹{consumedINR} / ₹{totalBudgetINR}
                </Typography>
                <LinearProgress
                  variant="determinate"
                  value={(consumedINR / totalBudgetINR) * 100}
                  sx={{ mt: 1, mb: 0.5, bgcolor: 'rgba(255,255,255,0.1)', '& .MuiLinearProgress-bar': { bgcolor: '#00E5FF' } }}
                />
                <Typography variant="caption" sx={{ color: '#94A3B8' }}>
                  Remaining: ₹{remainingINR} (Reserved: ₹{reservedINR})
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Live Multi-Agent DAG Graph */}
        <Typography variant="h6" sx={{ fontWeight: 700, color: '#F8FAFC', mb: 2 }}>
          Mission #001: Execution DAG & Thought Stream {isReplanned && '• [ADAPTIVE RE-PLAN APPLIED]'}
        </Typography>

        <Grid container spacing={2}>
          {tasks.map((t, idx) => (
            <Grid item xs={12} key={t.id}>
              <Card
                sx={{
                  bgcolor: '#0F172A',
                  border: `1px solid ${t.status === 'blocked' ? 'rgba(245, 158, 11, 0.4)' : (t.id.startsWith('9') || t.id.startsWith('10') ? 'rgba(0, 240, 255, 0.4)' : 'rgba(255, 255, 255, 0.08)')}`,
                  borderRadius: 2,
                  p: 2,
                }}
              >
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <Box
                      sx={{
                        width: 32,
                        height: 32,
                        borderRadius: '50%',
                        bgcolor: t.status === 'completed' ? 'rgba(16, 185, 129, 0.15)' : 'rgba(245, 158, 11, 0.15)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        color: t.status === 'completed' ? '#10B981' : '#F59E0B',
                        fontWeight: 800,
                        fontSize: '0.85rem',
                      }}
                    >
                      {idx + 1}
                    </Box>
                    <Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                        <Typography variant="subtitle1" sx={{ fontWeight: 700, color: '#F8FAFC' }}>
                          {t.title}
                        </Typography>
                        <Chip label={t.role} size="small" sx={{ bgcolor: 'rgba(255,255,255,0.06)', color: '#94A3B8', fontSize: '0.75rem' }} />
                        {t.evidenceId && (
                          <Chip
                            label={t.evidenceId}
                            size="small"
                            onClick={() => {
                              setSelectedEvidence({
                                title: `Evidence Record: ${t.evidenceId}`,
                                score: 88,
                                explanation: t.thought,
                                formula: 'ICP_Match_Score = (Headcount * 0.35) + (Tech_Signal * 0.40) + (Contactability * 0.25)',
                                confidenceScore: 0.94,
                                evidenceItems: [
                                  { id: t.evidenceId || 'EVD-01', label: 'Research Token', value: t.evidenceId || 'EVD-01' },
                                  { id: 'PROV', label: 'Data Provenance', value: 'Verified Public Filing & Transformation Signal' },
                                ],
                                underlyingMetrics: [
                                  { label: 'Agent Role', value: t.role },
                                  { label: 'FinOps Budget Deduction', value: `₹${t.costINR.toFixed(2)}` },
                                ],
                              });
                              setEvidenceDrawerOpen(true);
                            }}
                            sx={{ bgcolor: 'rgba(0, 229, 255, 0.1)', color: '#00E5FF', cursor: 'pointer', fontSize: '0.75rem', fontWeight: 700 }}
                          />
                        )}
                      </Box>
                      <Typography variant="body2" sx={{ color: '#94A3B8', mt: 0.5 }}>
                        {t.thought}
                      </Typography>
                    </Box>
                  </Box>

                  <Box sx={{ textAlign: 'right' }}>
                    <Chip
                      label={t.status.toUpperCase()}
                      size="small"
                      sx={{
                        bgcolor: t.status === 'completed' ? 'rgba(16, 185, 129, 0.1)' : 'rgba(245, 158, 11, 0.1)',
                        color: t.status === 'completed' ? '#10B981' : '#F59E0B',
                        fontWeight: 800,
                        fontSize: '0.75rem',
                      }}
                    />
                    <Typography variant="caption" sx={{ display: 'block', color: '#64748B', mt: 0.5 }}>
                      Cost: ₹{t.costINR.toFixed(2)}
                    </Typography>
                  </Box>
                </Box>
              </Card>
            </Grid>
          ))}
        </Grid>

        {/* Launch Mission Modal */}
        <Dialog open={isLaunchModalOpen} onClose={() => setIsLaunchModalOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle sx={{ bgcolor: '#0A0E17', color: '#F8FAFC', fontWeight: 800 }}>
            Launch Autonomous Revenue Mission
          </DialogTitle>
          <DialogContent sx={{ bgcolor: '#0A0E17', pt: 2 }}>
            <TextField
              label="Mission Objective"
              fullWidth
              defaultValue="Generate ₹50L qualified pipeline in BFSI"
              margin="normal"
              InputLabelProps={{ style: { color: '#94A3B8' } }}
              sx={{ input: { color: '#F8FAFC' } }}
            />
            <TextField
              label="Target Industry"
              fullWidth
              defaultValue="Enterprise BFSI"
              margin="normal"
              InputLabelProps={{ style: { color: '#94A3B8' } }}
              sx={{ input: { color: '#F8FAFC' } }}
            />
            <TextField
              select
              label="Autonomy Level"
              fullWidth
              value={autonomyLevel}
              onChange={(e) => setAutonomyLevel(Number(e.target.value))}
              margin="normal"
              InputLabelProps={{ style: { color: '#94A3B8' } }}
              sx={{ color: '#F8FAFC' }}
            >
              <MenuItem value={0}>Level 0 — Observe (Read-only)</MenuItem>
              <MenuItem value={1}>Level 1 — Recommend (Suggest actions)</MenuItem>
              <MenuItem value={2}>Level 2 — Assisted (Draft outreach, human approves)</MenuItem>
              <MenuItem value={3}>Level 3 — Controlled Autonomy (Autonomous research/outreach, gated proposals)</MenuItem>
              <MenuItem value={4}>Level 4 — Autonomous Operations (Full multi-step operational loop)</MenuItem>
            </TextField>
            <TextField
              label="Mission Wallet Budget (INR)"
              fullWidth
              defaultValue="5000"
              margin="normal"
              InputLabelProps={{ style: { color: '#94A3B8' } }}
              sx={{ input: { color: '#F8FAFC' } }}
            />
          </DialogContent>
          <DialogActions sx={{ bgcolor: '#0A0E17', p: 2 }}>
            <Button onClick={() => setIsLaunchModalOpen(false)} sx={{ color: '#94A3B8' }}>Cancel</Button>
            <Button variant="contained" onClick={handleStartMission} sx={{ background: '#00E5FF', color: '#0A0E17', fontWeight: 800 }}>
              Confirm & Launch
            </Button>
          </DialogActions>
        </Dialog>

        {/* Evidence Drawer */}
        <EvidenceDrawer
          open={evidenceDrawerOpen}
          onClose={() => setEvidenceDrawerOpen(false)}
          data={selectedEvidence}
        />
      </Box>
    </Layout>
  );
};

export default GrowthAgent;
