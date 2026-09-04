import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  Typography,
  Grid,
  Button,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
  Alert,
  Tabs,
  Tab,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow
} from '@mui/material';
import RadarIcon from '@mui/icons-material/Radar';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import VerifiedUserIcon from '@mui/icons-material/VerifiedUser';
import ScienceIcon from '@mui/icons-material/Science';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import PsychologyIcon from '@mui/icons-material/Psychology';
import ShieldIcon from '@mui/icons-material/Shield';
import RefreshIcon from '@mui/icons-material/Refresh';
import VisibilityIcon from '@mui/icons-material/Visibility';
import InsightsIcon from '@mui/icons-material/Insights';

interface ExternalSignalItem {
  id: string;
  signalType: number;
  entityName: string;
  title: string;
  description: string;
  priority: number; // 1=Low, 2=Medium, 3=High, 4=Critical
  priorityScore: number;
  magnitude: number;
  direction: string;
  confidence: number;
  freshnessScore: number;
  detectedAt: string;
  status: number;
}

interface CompetitorItem {
  id: string;
  competitorName: string;
  domain: string;
  pricingSummary: string;
  observedPositioning: string;
  observedStrategy: string;
  hiringActivity: string;
  marketPresence: string;
  uncertaintyScore: number;
  lastSignalObservedAt?: string;
}

interface OpportunityItem {
  id: string;
  title: string;
  customerProblem: string;
  targetSegment: string;
  strategicFitScore: number;
  revenuePotentialINR: number;
  marginPotentialPercent: number;
  timeToValueDays: number;
  confidence: number;
  contaminationRisk: number;
  status: number;
}

interface ThreatItem {
  id: string;
  category: number;
  title: string;
  description: string;
  targetArea: string;
  severityScore: number;
  urgencyScore: number;
  confidence: number;
  status: string;
}

interface RecommendationItem {
  id: string;
  opportunityId?: string;
  summary: string;
  recommendedStrategicAction: string;
  commercialScore: number;
  primaryHypothesis: string;
  whyNotAnalysis: string;
  expectedValueINR: number;
  downsideRiskINR: number;
  confidence: number;
  status: string;
}

const SIGNAL_TYPE_NAMES = [
  'None',
  'Price Change',
  'Product Launch',
  'Product Discontinuation',
  'Competitor Move',
  'Market Growth',
  'Market Decline',
  'Customer Complaint',
  'Customer Preference',
  'Regulatory Change',
  'Hiring Surge',
  'Hiring Decline',
  'Funding Event',
  'Partnership',
  'Distribution Change',
  'Technology Shift',
  'Demand Spike',
  'Demand Drop',
  'Supply Disruption',
  'Sentiment Shift'
];

const PRIORITY_NAMES = ['Unknown', 'Low', 'Medium', 'High', 'CRITICAL'];
const PRIORITY_COLORS: ('default' | 'info' | 'warning' | 'error')[] = ['default', 'default', 'info', 'warning', 'error'];

export const MarketRadar: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [signals, setSignals] = useState<ExternalSignalItem[]>([]);
  const [competitors, setCompetitors] = useState<CompetitorItem[]>([]);
  const [opportunities, setOpportunities] = useState<OpportunityItem[]>([]);
  const [threats, setThreats] = useState<ThreatItem[]>([]);
  const [recommendations, setRecommendations] = useState<RecommendationItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Dialog state
  const [whyCareDialogOpen, setWhyCareDialogOpen] = useState(false);
  const [selectedSignal, setSelectedSignal] = useState<ExternalSignalItem | null>(null);
  const [whyNotDialogOpen, setWhyNotDialogOpen] = useState(false);
  const [selectedRec, setSelectedRec] = useState<RecommendationItem | null>(null);

  const fetchData = async () => {
    setLoading(true);
    setErrorMsg(null);
    try {
      const token = localStorage.getItem('token');
      const headers = { Authorization: `Bearer ${token}` };

      const [resRadar, resOpps, resThreats, resRecs] = await Promise.all([
        fetch('/api/market/radar', { headers }),
        fetch('/api/market/opportunities', { headers }),
        fetch('/api/market/threats', { headers }),
        fetch('/api/market/recommendations', { headers })
      ]);

      if (resRadar.ok) {
        const data = await resRadar.json();
        setSignals(data.signals || []);
        setCompetitors(data.competitors || []);
      }
      if (resOpps.ok) {
        const data = await resOpps.json();
        setOpportunities(Array.isArray(data) ? data : []);
      }
      if (resThreats.ok) {
        const data = await resThreats.json();
        setThreats(Array.isArray(data) ? data : []);
      }
      if (resRecs.ok) {
        const data = await resRecs.json();
        setRecommendations(Array.isArray(data) ? data : []);
      }
    } catch (err: any) {
      setErrorMsg(err?.message || 'Failed to sync Market Radar intelligence.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleWhyCare = (signal: ExternalSignalItem) => {
    setSelectedSignal(signal);
    setWhyCareDialogOpen(true);
  };

  const handleWhyNot = (rec: RecommendationItem) => {
    setSelectedRec(rec);
    setWhyNotDialogOpen(true);
  };

  return (
    <Box sx={{ width: '100%', p: 3 }}>
      {/* Header Banner */}
      <Paper sx={{ p: 3, mb: 3, bgcolor: '#090d16', color: '#fff', borderRadius: 2.5, border: '1px solid #1e293b' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1.5 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <RadarIcon sx={{ color: '#06b6d4', fontSize: 32 }} />
            <Box>
              <Typography variant="h5" sx={{ fontWeight: 800, letterSpacing: '-0.02em', color: '#f8fafc' }}>
                External Reality Fabric & Market Radar (Batch 4)
              </Typography>
              <Typography variant="caption" sx={{ color: '#94a3b8' }}>
                Evidence-grounded external intelligence with deterministic commercial scoring, Why-NOT competitor hypotheses, and governed recommendations.
              </Typography>
            </Box>
          </Box>
          <Button
            size="small"
            variant="outlined"
            onClick={fetchData}
            startIcon={<RefreshIcon />}
            sx={{ color: '#06b6d4', borderColor: '#0891b2', fontWeight: 700 }}
          >
            Sync Radar
          </Button>
        </Box>

        {/* Master Invariant & Classification Chips */}
        <Typography variant="body2" sx={{ color: '#64748b', mb: 2 }}>
          Master Charlie Invariant: <strong style={{ color: '#f1f5f9' }}>EXTERNAL INTELLIGENCE: SIGNAL ≠ EVIDENCE ≠ TRUTH ≠ HYPOTHESIS ≠ OPPORTUNITY ≠ DECISION</strong>. Terminates at governed strategic recommendation; execution wall strictly preserved.
        </Typography>

        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.2, alignItems: 'center' }}>
          <Typography variant="caption" sx={{ color: '#64748b', fontWeight: 800 }}>EPISTEMIC BOUNDARIES:</Typography>
          <Chip label="FACT: Grounded in Verified System Proof" size="small" sx={{ bgcolor: '#065f46', color: '#6ee7b7', fontWeight: 700 }} icon={<VerifiedUserIcon sx={{ color: '#6ee7b7 !important', fontSize: 13 }} />} />
          <Chip label="OBSERVATION: Raw External Telemetry" size="small" sx={{ bgcolor: '#1e3a8a', color: '#93c5fd', fontWeight: 700 }} icon={<VisibilityIcon sx={{ color: '#93c5fd !important', fontSize: 13 }} />} />
          <Chip label="ESTIMATE: Formulaic Statistical Projection" size="small" sx={{ bgcolor: '#4c1d95', color: '#c4b5fd', fontWeight: 700 }} icon={<TrendingUpIcon sx={{ color: '#c4b5fd !important', fontSize: 13 }} />} />
          <Chip label="HYPOTHESIS: Unverified / Why-NOT Alternative" size="small" sx={{ bgcolor: '#78350f', color: '#fcd34d', fontWeight: 700 }} icon={<ScienceIcon sx={{ color: '#fcd34d !important', fontSize: 13 }} />} />
          <Chip label="SIMULATION: What-If Counterfactual" size="small" sx={{ bgcolor: '#701a75', color: '#f0abfc', fontWeight: 700 }} icon={<PsychologyIcon sx={{ color: '#f0abfc !important', fontSize: 13 }} />} />
          <Chip label="UNKNOWN: Epistemic Missing State" size="small" sx={{ bgcolor: '#334155', color: '#cbd5e1', fontWeight: 700 }} icon={<HelpOutlineIcon sx={{ color: '#cbd5e1 !important', fontSize: 13 }} />} />
          <Chip label="POLICY: Deterministic Sovereign Gate" size="small" sx={{ bgcolor: '#831843', color: '#fbcfe8', fontWeight: 700 }} icon={<ShieldIcon sx={{ color: '#fbcfe8 !important', fontSize: 13 }} />} />
        </Box>
      </Paper>

      {errorMsg && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setErrorMsg(null)}>
          {errorMsg}
        </Alert>
      )}

      {/* KPI Cards */}
      <Grid container spacing={2.5} sx={{ mb: 3 }}>
        {[
          { label: 'Active Signals', count: signals.length, color: '#06b6d4', icon: <RadarIcon fontSize="small" /> },
          { label: 'Competitors Tracked', count: competitors.length, color: '#3b82f6', icon: <VisibilityIcon fontSize="small" /> },
          { label: 'Market Opportunities', count: opportunities.length, color: '#10b981', icon: <TrendingUpIcon fontSize="small" /> },
          { label: 'Active Threats', count: threats.length, color: '#ef4444', icon: <WarningAmberIcon fontSize="small" /> },
          { label: 'Governed Recommendations', count: recommendations.length, color: '#8b5cf6', icon: <InsightsIcon fontSize="small" /> },
        ].map((kpi) => (
          <Grid item xs={12} sm={2.4} key={kpi.label}>
            <Card sx={{ p: 2, borderRadius: 2, border: '1px solid #e2e8f0', height: '100%' }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                <Typography variant="caption" sx={{ fontWeight: 700, color: '#64748b' }}>{kpi.label}</Typography>
                <Box sx={{ color: kpi.color }}>{kpi.icon}</Box>
              </Box>
              <Typography variant="h4" sx={{ fontWeight: 900, color: '#0f172a' }}>{kpi.count}</Typography>
            </Card>
          </Grid>
        ))}
      </Grid>

      {/* Sub-Tabs */}
      <Paper sx={{ mb: 3, borderRadius: 2, border: '1px solid #e2e8f0' }}>
        <Tabs
          value={activeTab}
          onChange={(_, val) => setActiveTab(val)}
          indicatorColor="primary"
          textColor="primary"
          variant="scrollable"
          scrollButtons="auto"
          sx={{ '& .MuiTab-root': { fontWeight: 700, textTransform: 'none' } }}
        >
          <Tab label={`Live Signals (${signals.length})`} />
          <Tab label={`Competitor Radar (${competitors.length})`} />
          <Tab label={`Opportunity Explorer (${opportunities.length})`} />
          <Tab label={`Threat Radar (${threats.length})`} />
          <Tab label={`Strategic Recommendations (${recommendations.length})`} />
        </Tabs>
      </Paper>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}>
          <CircularProgress />
        </Box>
      ) : (
        <>
          {/* TAB 0: LIVE SIGNALS */}
          {activeTab === 0 && (
            <TableContainer component={Paper} sx={{ borderRadius: 2, border: '1px solid #e2e8f0' }}>
              <Table size="small">
                <TableHead sx={{ bgcolor: '#f1f5f9' }}>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 800 }}>Priority</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Signal Type</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Entity</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Title & Rationale</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Magnitude</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Confidence</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Freshness</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Executive Analysis</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {signals.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={8} sx={{ textAlign: 'center', py: 4, color: '#64748b' }}>
                        No external signals active. Feed ingested signals to begin continuous radar monitoring.
                      </TableCell>
                    </TableRow>
                  ) : (
                    signals.map((s) => (
                      <TableRow key={s.id} hover>
                        <TableCell>
                          <Chip
                            label={PRIORITY_NAMES[s.priority] || 'Low'}
                            size="small"
                            color={PRIORITY_COLORS[s.priority] || 'default'}
                            sx={{ fontWeight: 800, fontSize: '0.72rem' }}
                          />
                        </TableCell>
                        <TableCell sx={{ fontWeight: 600 }}>{SIGNAL_TYPE_NAMES[s.signalType] || 'External Signal'}</TableCell>
                        <TableCell sx={{ fontWeight: 700, color: '#0f172a' }}>{s.entityName || 'Market'}</TableCell>
                        <TableCell sx={{ maxWidth: 300 }}>
                          <Typography variant="body2" sx={{ fontWeight: 700, color: '#1e293b' }}>{s.title}</Typography>
                          <Typography variant="caption" color="textSecondary">{s.description}</Typography>
                        </TableCell>
                        <TableCell>{(s.magnitude * 100).toFixed(0)}%</TableCell>
                        <TableCell>{(s.confidence * 100).toFixed(0)}%</TableCell>
                        <TableCell>{(s.freshnessScore * 100).toFixed(0)}%</TableCell>
                        <TableCell>
                          <Button
                            size="small"
                            variant="outlined"
                            onClick={() => handleWhyCare(s)}
                            sx={{ textTransform: 'none', fontWeight: 700, fontSize: '0.75rem' }}
                          >
                            Why Care?
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {/* TAB 1: COMPETITOR RADAR */}
          {activeTab === 1 && (
            <Grid container spacing={2.5}>
              {competitors.length === 0 ? (
                <Grid item xs={12}>
                  <Card sx={{ p: 4, textAlign: 'center', borderRadius: 2, border: '1px dashed #cbd5e1' }}>
                    <Typography variant="body1" color="textSecondary">
                      No competitor profiles tracked. External competitor moves will register here automatically.
                    </Typography>
                  </Card>
                </Grid>
              ) : (
                competitors.map((comp) => (
                  <Grid item xs={12} md={6} key={comp.id}>
                    <Card sx={{ borderRadius: 2.5, border: '1px solid #e2e8f0', p: 2 }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1.5 }}>
                        <Box>
                          <Typography variant="h6" sx={{ fontWeight: 800, color: '#0f172a' }}>{comp.competitorName}</Typography>
                          <Typography variant="caption" sx={{ color: '#0891b2', fontWeight: 600 }}>{comp.domain}</Typography>
                        </Box>
                        <Chip label={comp.marketPresence} size="small" color="primary" sx={{ fontWeight: 700 }} />
                      </Box>
                      <Typography variant="body2" sx={{ mb: 1, color: '#334155' }}>
                        <strong>Pricing Sheet:</strong> {comp.pricingSummary}
                      </Typography>
                      <Typography variant="body2" sx={{ mb: 1.5, color: '#475569' }}>
                        <strong>Observed Strategy:</strong> {comp.observedStrategy}
                      </Typography>
                      <Box sx={{ p: 1.5, bgcolor: '#f8fafc', borderRadius: 2, border: '1px solid #f1f5f9', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="caption" sx={{ color: '#64748b' }}>Hiring Signal: <strong>{comp.hiringActivity}</strong></Typography>
                        <Typography variant="caption" sx={{ color: '#64748b' }}>Uncertainty: <strong>{((comp.uncertaintyScore || 0.3) * 100).toFixed(0)}%</strong></Typography>
                      </Box>
                    </Card>
                  </Grid>
                ))
              )}
            </Grid>
          )}

          {/* TAB 2: OPPORTUNITY EXPLORER */}
          {activeTab === 2 && (
            <Grid container spacing={2.5}>
              {opportunities.length === 0 ? (
                <Grid item xs={12}>
                  <Card sx={{ p: 4, textAlign: 'center', borderRadius: 2, border: '1px dashed #cbd5e1' }}>
                    <Typography variant="body1" color="textSecondary">
                      No commercial opportunities synthesized yet. Verified market signals will generate qualified opportunities.
                    </Typography>
                  </Card>
                </Grid>
              ) : (
                opportunities.map((opp) => (
                  <Grid item xs={12} md={6} key={opp.id}>
                    <Card sx={{ borderRadius: 2.5, border: '1px solid #e2e8f0', p: 2.5 }}>
                      <Typography variant="subtitle1" sx={{ fontWeight: 800, color: '#0f172a', mb: 1 }}>
                        {opp.title}
                      </Typography>
                      <Typography variant="caption" sx={{ color: '#64748b', display: 'block', mb: 2 }}>
                        Segment: <strong>{opp.targetSegment}</strong> | Problem: {opp.customerProblem}
                      </Typography>

                      <Grid container spacing={1.5} sx={{ mb: 2 }}>
                        <Grid item xs={6}>
                          <Paper sx={{ p: 1.5, bgcolor: '#f0fdf4', border: '1px solid #bbf7d0' }}>
                            <Typography variant="caption" sx={{ color: '#166534', fontWeight: 700 }}>Revenue Potential</Typography>
                            <Typography variant="subtitle2" sx={{ fontWeight: 900, color: '#15803d' }}>
                              ₹{(opp.revenuePotentialINR / 100000).toFixed(1)} Lakhs
                            </Typography>
                          </Paper>
                        </Grid>
                        <Grid item xs={6}>
                          <Paper sx={{ p: 1.5, bgcolor: '#f8fafc', border: '1px solid #e2e8f0' }}>
                            <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700 }}>Gross Margin</Typography>
                            <Typography variant="subtitle2" sx={{ fontWeight: 900, color: '#0f172a' }}>
                              {opp.marginPotentialPercent}%
                            </Typography>
                          </Paper>
                        </Grid>
                      </Grid>

                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="caption" sx={{ color: '#64748b' }}>
                          Strategic Fit: <strong>{(opp.strategicFitScore * 100).toFixed(0)}%</strong> | TTV: <strong>{opp.timeToValueDays}d</strong>
                        </Typography>
                        <Chip
                          label={opp.contaminationRisk > 0.30 ? 'High Contamination' : 'Verified Evidence'}
                          size="small"
                          color={opp.contaminationRisk > 0.30 ? 'error' : 'success'}
                          sx={{ fontWeight: 700 }}
                        />
                      </Box>
                    </Card>
                  </Grid>
                ))
              )}
            </Grid>
          )}

          {/* TAB 3: THREAT RADAR */}
          {activeTab === 3 && (
            <Grid container spacing={2}>
              {threats.length === 0 ? (
                <Grid item xs={12}>
                  <Card sx={{ p: 4, textAlign: 'center', borderRadius: 2, border: '1px dashed #cbd5e1' }}>
                    <Typography variant="body1" color="textSecondary">
                      No active commercial threats detected. External competitor pricing cuts and regulatory risks appear here.
                    </Typography>
                  </Card>
                </Grid>
              ) : (
                threats.map((t) => (
                  <Grid item xs={12} key={t.id}>
                    <Card sx={{ p: 2, borderRadius: 2, border: '1px solid #fecaca', bgcolor: '#fff5f5' }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <WarningAmberIcon sx={{ color: '#dc2626' }} />
                          <Typography variant="subtitle1" sx={{ fontWeight: 800, color: '#991b1b' }}>{t.title}</Typography>
                        </Box>
                        <Chip label={`Severity: ${(t.severityScore * 100).toFixed(0)}%`} size="small" color="error" sx={{ fontWeight: 800 }} />
                      </Box>
                      <Typography variant="body2" sx={{ color: '#7f1d1d', mb: 1.5 }}>{t.description}</Typography>
                      <Typography variant="caption" sx={{ color: '#991b1b' }}>
                        Target Area: <strong>{t.targetArea}</strong> | Confidence: {(t.confidence * 100).toFixed(0)}%
                      </Typography>
                    </Card>
                  </Grid>
                ))
              )}
            </Grid>
          )}

          {/* TAB 4: STRATEGIC RECOMMENDATIONS & WHY-NOT */}
          {activeTab === 4 && (
            <Grid container spacing={2.5}>
              {recommendations.length === 0 ? (
                <Grid item xs={12}>
                  <Card sx={{ p: 4, textAlign: 'center', borderRadius: 2, border: '1px dashed #cbd5e1' }}>
                    <Typography variant="body1" color="textSecondary">
                      No strategic recommendations formulated yet.
                    </Typography>
                  </Card>
                </Grid>
              ) : (
                recommendations.map((rec) => (
                  <Grid item xs={12} key={rec.id}>
                    <Card sx={{ p: 2.5, borderRadius: 2.5, border: '1px solid #e2e8f0' }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1.5 }}>
                        <Box>
                          <Typography variant="h6" sx={{ fontWeight: 800, color: '#0f172a' }}>{rec.summary}</Typography>
                          <Typography variant="caption" color="textSecondary">Status: <strong>{rec.status}</strong></Typography>
                        </Box>
                        <Chip label={`Commercial Score: ${(rec.commercialScore * 100).toFixed(0)}%`} color="secondary" sx={{ fontWeight: 800 }} />
                      </Box>

                      <Typography variant="body2" sx={{ color: '#334155', mb: 2 }}>
                        <strong>Action:</strong> {rec.recommendedStrategicAction}
                      </Typography>

                      <Grid container spacing={2} sx={{ mb: 2 }}>
                        <Grid item xs={6}>
                          <Paper sx={{ p: 1.5, bgcolor: '#f8fafc', border: '1px solid #f1f5f9' }}>
                            <Typography variant="caption" sx={{ color: '#64748b' }}>Expected Value</Typography>
                            <Typography variant="subtitle2" sx={{ fontWeight: 800, color: '#16a34a' }}>
                              ₹{(rec.expectedValueINR / 100000).toFixed(1)} Lakhs
                            </Typography>
                          </Paper>
                        </Grid>
                        <Grid item xs={6}>
                          <Paper sx={{ p: 1.5, bgcolor: '#f8fafc', border: '1px solid #f1f5f9' }}>
                            <Typography variant="caption" sx={{ color: '#64748b' }}>Estimated Downside Risk</Typography>
                            <Typography variant="subtitle2" sx={{ fontWeight: 800, color: '#dc2626' }}>
                              ₹{(rec.downsideRiskINR / 100000).toFixed(1)} Lakhs
                            </Typography>
                          </Paper>
                        </Grid>
                      </Grid>

                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="caption" sx={{ color: '#64748b' }}>
                          Confidence: <strong>{(rec.confidence * 100).toFixed(0)}%</strong> | Required Governance: <strong>CEO Approval</strong>
                        </Typography>
                        <Button
                          size="small"
                          variant="outlined"
                          onClick={() => handleWhyNot(rec)}
                          sx={{ textTransform: 'none', fontWeight: 700 }}
                        >
                          Why NOT Alternatives?
                        </Button>
                      </Box>
                    </Card>
                  </Grid>
                ))
              )}
            </Grid>
          )}
        </>
      )}

      {/* WHY SHOULD I CARE DIALOG */}
      <Dialog
        open={whyCareDialogOpen}
        onClose={() => setWhyCareDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle sx={{ fontWeight: 800, bgcolor: '#090d16', color: '#fff' }}>
          Why Should Executives Care About This Signal?
        </DialogTitle>
        <DialogContent sx={{ mt: 2 }}>
          {selectedSignal && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 800, color: '#0f172a' }}>
                "{selectedSignal.title}"
              </Typography>
              <Typography variant="body2" sx={{ color: '#334155' }}>
                {selectedSignal.description}
              </Typography>
              <Paper sx={{ p: 2, bgcolor: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: 2 }}>
                <Typography variant="caption" sx={{ fontWeight: 800, color: '#475569', display: 'block', mb: 0.5 }}>
                  GOVERNED EXECUTIVE BRIEFING:
                </Typography>
                <Typography variant="body2" sx={{ color: '#1e293b' }}>
                  This signal carries a <strong>{(selectedSignal.magnitude * 100).toFixed(0)}%</strong> magnitude score in sector '{selectedSignal.entityName}'. Charlie has quarantined it from autonomous action. Immediate next step: evaluate counterfactual pricing response in the Opportunity Explorer.
                </Typography>
              </Paper>
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setWhyCareDialogOpen(false)} variant="contained">
            Close
          </Button>
        </DialogActions>
      </Dialog>

      {/* WHY NOT ALTERNATIVE HYPOTHESIS DIALOG */}
      <Dialog
        open={whyNotDialogOpen}
        onClose={() => setWhyNotDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle sx={{ fontWeight: 800, bgcolor: '#090d16', color: '#fff' }}>
          Why-NOT? Alternative Strategic Hypotheses
        </DialogTitle>
        <DialogContent sx={{ mt: 2 }}>
          {selectedRec && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box sx={{ p: 2, bgcolor: '#f0fdf4', borderRadius: 2, border: '1px solid #bbf7d0' }}>
                <Typography variant="caption" sx={{ color: '#166534', fontWeight: 800 }}>PRIMARY HYPOTHESIS:</Typography>
                <Typography variant="subtitle1" sx={{ fontWeight: 800, color: '#14532d' }}>
                  {selectedRec.primaryHypothesis}
                </Typography>
              </Box>
              <Box sx={{ p: 2, bgcolor: '#f8fafc', borderRadius: 2, border: '1px solid #e2e8f0' }}>
                <Typography variant="caption" sx={{ color: '#475569', fontWeight: 800 }}>WHY NOT ALTERNATIVES (ELIMINATION PROOF):</Typography>
                <Typography variant="body2" sx={{ color: '#1e293b', mt: 0.5 }}>
                  {selectedRec.whyNotAnalysis}
                </Typography>
              </Box>
              <Alert severity="info" sx={{ fontSize: '0.8rem' }}>
                Charlie does not force a single explanation. Competing hypotheses are preserved until disconfirmed by empirical market telemetry.
              </Alert>
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setWhyNotDialogOpen(false)} variant="contained">
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
export default MarketRadar;
