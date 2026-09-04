import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
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
  TableRow,
  LinearProgress
} from '@mui/material';
import PsychologyIcon from '@mui/icons-material/Psychology';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ScienceIcon from '@mui/icons-material/Science';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import VerifiedUserIcon from '@mui/icons-material/VerifiedUser';
import ReportProblemIcon from '@mui/icons-material/ReportProblem';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import RefreshIcon from '@mui/icons-material/Refresh';

interface LearningItem {
  id: string;
  statement: string;
  domain: string;
  tier: number; // 0=L0_Session, 1=L1_Mission, 2=L2_Agent, 3=L3_Org, 4=L4_Strategic, 5=L5_Institutional
  state: number; // 0=Candidate, 1=Validating, 2=Quarantined, 3=Approved, 4=Promoted, 5=Active, 6=Aging, 7=Stale, 8=Superseded, 9=Rejected
  classification: number;
  confidence: number;
  causalConfidence: number;
  validationCount: number;
  contradictionCount: number;
  freshness: number;
  applicabilityScope: string;
  integrityHash?: string;
  isProcedureCandidate?: boolean;
}

interface ContradictionItem {
  id: string;
  learningRecordAId: string;
  learningRecordBId: string;
  status: number;
  details: string;
  detectedAt: string;
}

interface FailureItem {
  id: string;
  missionId: string;
  rootCause: number;
  diagnosis: string;
  impact: string;
  hasCorrectiveAction: boolean;
  observedAt: string;
}

const TIER_NAMES = [
  'L0 Session',
  'L1 Mission',
  'L2 Agent',
  'L3 Organizational',
  'L4 Strategic',
  'L5 Validated Institutional'
];

const STATE_NAMES = [
  'Candidate',
  'Validating',
  'Quarantined',
  'Approved',
  'Promoted',
  'Active',
  'Aging',
  'Stale',
  'Superseded',
  'Rejected'
];

const ROOT_CAUSE_NAMES = [
  'None',
  'BadEvidence',
  'StaleEvidence',
  'IncorrectAssumption',
  'ModelReasoning',
  'AgentBehavior',
  'ToolBehavior',
  'Strategy',
  'MarketChange',
  'HumanIntervention',
  'PolicyRestriction',
  'DataQuality',
  'ExecutionFailure',
  'Unknown'
];

export const LearningCenter: React.FC = () => {
  const [activeSubTab, setActiveSubTab] = useState(0);
  const [learningRecords, setLearningRecords] = useState<LearningItem[]>([]);
  const [contradictions, setContradictions] = useState<ContradictionItem[]>([]);
  const [failures, setFailures] = useState<FailureItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [explanationDialogOpen, setExplanationDialogOpen] = useState(false);
  const [selectedExplanation, setSelectedExplanation] = useState<any>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const fetchData = async () => {
    setLoading(true);
    setErrorMsg(null);
    try {
      const token = localStorage.getItem('token');
      const headers = { Authorization: `Bearer ${token}` };

      const [resLearning, resContra, resFailures] = await Promise.all([
        fetch('/api/learning/active', { headers }),
        fetch('/api/learning/contradictions', { headers }),
        fetch('/api/learning/failing-assumptions', { headers })
      ]);

      if (resLearning.ok) {
        const data = await resLearning.json();
        setLearningRecords(Array.isArray(data) ? data : []);
      }
      if (resContra.ok) {
        const data = await resContra.json();
        setContradictions(Array.isArray(data) ? data : []);
      }
      if (resFailures.ok) {
        const data = await resFailures.json();
        setFailures(Array.isArray(data) ? data : []);
      }
    } catch (err: any) {
      setErrorMsg(err?.message || 'Failed to load institutional learning repository.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleExplain = async (id: string) => {
    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`/api/learning/explain?id=${id}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setSelectedExplanation(data);
        setExplanationDialogOpen(true);
      }
    } catch {
      // Fallback local explanation
      const item = learningRecords.find(r => r.id === id);
      if (item) {
        setSelectedExplanation({
          statement: item.statement,
          tier: TIER_NAMES[item.tier] || 'L1 Mission',
          confidence: item.confidence,
          causalConfidence: item.causalConfidence,
          validationCount: item.validationCount,
          contradictionCount: item.contradictionCount,
          rationale: `Derived from mission outcomes in domain '${item.domain}'. Passed ${item.validationCount} validation cycles. Governed causal confidence decoupled from verified factual certainty.`
        });
        setExplanationDialogOpen(true);
      }
    }
  };

  const handlePromote = async (id: string) => {
    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`/api/learning/${id}/promote?isDirectAiCall=false`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        fetchData();
      } else {
        const err = await res.json();
        setErrorMsg(err.message || 'Promotion rejected by deterministic governance guard.');
      }
    } catch (err: any) {
      setErrorMsg(err?.message || 'Promotion failed.');
    }
  };

  const activeLessons = learningRecords.filter(r => r.state === 5 || r.state === 4 || r.state === 3);
  const candidateLessons = learningRecords.filter(r => r.state === 0 || r.state === 1 || r.state === 2 || r.state === 6 || r.state === 7);

  return (
    <Box sx={{ width: '100%' }}>
      {/* Governance Invariant Banner */}
      <Paper sx={{ p: 2.5, mb: 3, bgcolor: '#0f172a', color: '#fff', borderRadius: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <PsychologyIcon sx={{ color: '#38bdf8' }} />
            <Typography variant="h6" sx={{ fontWeight: 800, letterSpacing: '-0.01em' }}>
              Institutional Learning & Causal Intelligence Engine (Batch 3)
            </Typography>
          </Box>
          <Button
            size="small"
            variant="outlined"
            onClick={fetchData}
            startIcon={<RefreshIcon />}
            sx={{ color: '#38bdf8', borderColor: '#38bdf8' }}
          >
            Sync Learning
          </Button>
        </Box>
        <Typography variant="body2" sx={{ color: '#94a3b8', mb: 2 }}>
          Master Charlie Invariant: <strong style={{ color: '#f8fafc' }}>MEMORY ≠ LEARNING ≠ KNOWLEDGE ≠ TRUTH ≠ POLICY</strong>. Learning records are empirical, advisory hypotheses with decoupled causal confidence.
        </Typography>

        {/* Epistemological Distinction Badges */}
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, alignItems: 'center' }}>
          <Typography variant="caption" sx={{ color: '#64748b', fontWeight: 800 }}>GOVERNED REALITY CLASSIFICATIONS:</Typography>
          <Chip label="FACT: Grounded in Digital Twin" size="small" sx={{ bgcolor: '#065f46', color: '#6ee7b7', fontWeight: 700 }} icon={<VerifiedUserIcon sx={{ color: '#6ee7b7 !important', fontSize: 14 }} />} />
          <Chip label="LEARNING: Empirical Advisory Insight" size="small" sx={{ bgcolor: '#1e3a8a', color: '#93c5fd', fontWeight: 700 }} icon={<PsychologyIcon sx={{ color: '#93c5fd !important', fontSize: 14 }} />} />
          <Chip label="HYPOTHESIS: Unverified Candidate" size="small" sx={{ bgcolor: '#78350f', color: '#fcd34d', fontWeight: 700 }} icon={<ScienceIcon sx={{ color: '#fcd34d !important', fontSize: 14 }} />} />
          <Chip label="UNKNOWN: Legitimate Epistemic State" size="small" sx={{ bgcolor: '#334155', color: '#cbd5e1', fontWeight: 700 }} icon={<HelpOutlineIcon sx={{ color: '#cbd5e1 !important', fontSize: 14 }} />} />
        </Box>
      </Paper>

      {errorMsg && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setErrorMsg(null)}>
          {errorMsg}
        </Alert>
      )}

      {/* Sub-Tabs */}
      <Paper sx={{ mb: 3, borderRadius: 2, border: '1px solid #e2e8f0' }}>
        <Tabs
          value={activeSubTab}
          onChange={(_, val) => setActiveSubTab(val)}
          indicatorColor="primary"
          textColor="primary"
          sx={{ '& .MuiTab-root': { fontWeight: 700, textTransform: 'none' } }}
        >
          <Tab label={`Active Knowledge (${activeLessons.length})`} />
          <Tab label={`Candidates & Quarantined (${candidateLessons.length})`} />
          <Tab label={`Contradiction Radar (${contradictions.length})`} />
          <Tab label={`Failing Assumptions & Taxonomy (${failures.length})`} />
        </Tabs>
      </Paper>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}>
          <CircularProgress />
        </Box>
      ) : (
        <>
          {/* TAB 0: ACTIVE KNOWLEDGE */}
          {activeSubTab === 0 && (
            <Grid container spacing={2.5}>
              {activeLessons.length === 0 ? (
                <Grid item xs={12}>
                  <Card sx={{ p: 4, textAlign: 'center', borderRadius: 2, border: '1px dashed #cbd5e1' }}>
                    <Typography variant="body1" color="textSecondary">
                      No active institutional knowledge records promoted yet. Completed mission outcomes will generate candidates.
                    </Typography>
                  </Card>
                </Grid>
              ) : (
                activeLessons.map((item) => (
                  <Grid item xs={12} md={6} key={item.id}>
                    <Card sx={{ borderRadius: 2.5, border: '1px solid #e2e8f0', height: '100%', display: 'flex', flexDirection: 'column' }}>
                      <CardContent sx={{ flex: 1 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1.5 }}>
                          <Chip
                            label={TIER_NAMES[item.tier] || 'L1 Mission'}
                            size="small"
                            color="primary"
                            sx={{ fontWeight: 700, fontSize: '0.72rem' }}
                          />
                          <Box sx={{ display: 'flex', gap: 0.5 }}>
                            <Chip
                              label={STATE_NAMES[item.state] || 'Active'}
                              size="small"
                              variant="outlined"
                              color={item.state === 5 ? 'success' : 'info'}
                              sx={{ fontWeight: 700 }}
                            />
                            {item.isProcedureCandidate && (
                              <Chip
                                icon={<AutoFixHighIcon sx={{ fontSize: 12 }} />}
                                label="Procedure Candidate"
                                size="small"
                                sx={{ bgcolor: '#ede9fe', color: '#6d28d9', fontWeight: 700 }}
                              />
                            )}
                          </Box>
                        </Box>

                        <Typography variant="subtitle1" sx={{ fontWeight: 700, color: '#0f172a', mb: 1 }}>
                          "{item.statement}"
                        </Typography>

                        <Typography variant="caption" sx={{ color: '#64748b', display: 'block', mb: 2 }}>
                          Domain: <strong>{item.domain}</strong> | Scope: {item.applicabilityScope || 'General Enterprise'}
                        </Typography>

                        {/* Metrology Metrics */}
                        <Box sx={{ bgcolor: '#f8fafc', p: 1.5, borderRadius: 2, mb: 2, border: '1px solid #f1f5f9' }}>
                          <Grid container spacing={1.5}>
                            <Grid item xs={6}>
                              <Typography variant="caption" sx={{ color: '#64748b', display: 'block' }}>
                                Truth Confidence: <strong>{(item.confidence * 100).toFixed(0)}%</strong>
                              </Typography>
                              <LinearProgress variant="determinate" value={item.confidence * 100} sx={{ height: 6, borderRadius: 1, my: 0.5, bgcolor: '#e2e8f0' }} />
                            </Grid>
                            <Grid item xs={6}>
                              <Typography variant="caption" sx={{ color: '#64748b', display: 'block' }}>
                                Causal Confidence: <strong>{(item.causalConfidence * 100).toFixed(0)}%</strong>
                              </Typography>
                              <LinearProgress variant="determinate" value={item.causalConfidence * 100} color="secondary" sx={{ height: 6, borderRadius: 1, my: 0.5, bgcolor: '#e2e8f0' }} />
                            </Grid>
                          </Grid>
                          <Typography variant="caption" sx={{ color: '#94a3b8', fontStyle: 'italic', display: 'block', mt: 0.5 }}>
                            *Causal confidence measures "why" it occurred; strictly decoupled from truth accuracy.
                          </Typography>
                        </Box>

                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="caption" sx={{ color: '#64748b' }}>
                            Validations: <strong>{item.validationCount}</strong> | Contradictions: <strong style={{ color: item.contradictionCount > 0 ? '#ef4444' : '#10b981' }}>{item.contradictionCount}</strong>
                          </Typography>
                          <Button
                            size="small"
                            variant="outlined"
                            onClick={() => handleExplain(item.id)}
                            sx={{ textTransform: 'none', fontWeight: 700 }}
                          >
                            Why Charlie Believes This
                          </Button>
                        </Box>
                      </CardContent>
                    </Card>
                  </Grid>
                ))
              )}
            </Grid>
          )}

          {/* TAB 1: CANDIDATES & QUARANTINED */}
          {activeSubTab === 1 && (
            <TableContainer component={Paper} sx={{ borderRadius: 2, border: '1px solid #e2e8f0' }}>
              <Table size="small">
                <TableHead sx={{ bgcolor: '#f1f5f9' }}>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 800 }}>Statement</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Domain</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Tier</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>State</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Causal Conf.</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Validations</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Action</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {candidateLessons.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} sx={{ textAlign: 'center', py: 3, color: '#64748b' }}>
                        No candidate or quarantined lessons found.
                      </TableCell>
                    </TableRow>
                  ) : (
                    candidateLessons.map((item) => (
                      <TableRow key={item.id} hover>
                        <TableCell sx={{ fontWeight: 600, maxWidth: 300 }}>{item.statement}</TableCell>
                        <TableCell>{item.domain}</TableCell>
                        <TableCell>
                          <Chip label={TIER_NAMES[item.tier] || 'L1'} size="small" />
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={STATE_NAMES[item.state] || 'Candidate'}
                            size="small"
                            color={item.state === 2 ? 'warning' : 'default'}
                            sx={{ fontWeight: 700 }}
                          />
                        </TableCell>
                        <TableCell>{(item.causalConfidence * 100).toFixed(0)}%</TableCell>
                        <TableCell>{item.validationCount}</TableCell>
                        <TableCell>
                          <Button
                            size="small"
                            variant="contained"
                            color="primary"
                            onClick={() => handlePromote(item.id)}
                            disabled={item.state === 2} // Quarantined cannot promote
                            sx={{ textTransform: 'none', fontWeight: 700, fontSize: '0.75rem' }}
                          >
                            Promote
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {/* TAB 2: CONTRADICTION RADAR */}
          {activeSubTab === 2 && (
            <Grid container spacing={2}>
              {contradictions.length === 0 ? (
                <Grid item xs={12}>
                  <Card sx={{ p: 4, textAlign: 'center', borderRadius: 2, border: '1px dashed #cbd5e1' }}>
                    <CheckCircleIcon sx={{ color: '#10b981', fontSize: 36, mb: 1 }} />
                    <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                      No Unresolved Contradictions Detected
                    </Typography>
                    <Typography variant="body2" color="textSecondary">
                      All validated learning hypotheses remain contextually harmonious across scopes.
                    </Typography>
                  </Card>
                </Grid>
              ) : (
                contradictions.map((c) => (
                  <Grid item xs={12} key={c.id}>
                    <Card sx={{ borderRadius: 2, border: '1px solid #fecaca', bgcolor: '#fff5f5', p: 2 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                        <ReportProblemIcon sx={{ color: '#dc2626' }} />
                        <Typography variant="subtitle2" sx={{ fontWeight: 800, color: '#991b1b' }}>
                          DIRECT HYPOTHESIS CONTRADICTION DETECTED
                        </Typography>
                      </Box>
                      <Typography variant="body2" sx={{ color: '#7f1d1d', mb: 1 }}>
                        {c.details}
                      </Typography>
                      <Typography variant="caption" sx={{ color: '#991b1b' }}>
                        Detected: {new Date(c.detectedAt).toLocaleString()} | Status: Unresolved (Cannot silently coexist as universally applicable facts)
                      </Typography>
                    </Card>
                  </Grid>
                ))
              )}
            </Grid>
          )}

          {/* TAB 3: FAILING ASSUMPTIONS & ROOT-CAUSE TAXONOMY */}
          {activeSubTab === 3 && (
            <TableContainer component={Paper} sx={{ borderRadius: 2, border: '1px solid #e2e8f0' }}>
              <Table size="small">
                <TableHead sx={{ bgcolor: '#f1f5f9' }}>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 800 }}>Observed At</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Root-Cause Taxonomy</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Diagnosis</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Impact</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>Corrective Action</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {failures.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} sx={{ textAlign: 'center', py: 3, color: '#64748b' }}>
                        No failure records diagnosed.
                      </TableCell>
                    </TableRow>
                  ) : (
                    failures.map((f) => (
                      <TableRow key={f.id} hover>
                        <TableCell sx={{ whiteSpace: 'nowrap' }}>
                          {new Date(f.observedAt).toLocaleDateString()}
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={ROOT_CAUSE_NAMES[f.rootCause] || 'Unknown'}
                            size="small"
                            color={f.rootCause === 13 ? 'default' : 'warning'}
                            sx={{ fontWeight: 700 }}
                          />
                        </TableCell>
                        <TableCell sx={{ fontWeight: 600 }}>{f.diagnosis}</TableCell>
                        <TableCell>{f.impact || 'Execution deviation'}</TableCell>
                        <TableCell>
                          {f.hasCorrectiveAction ? (
                            <Chip label="Implemented" size="small" color="success" />
                          ) : (
                            <Chip label="Pending" size="small" variant="outlined" />
                          )}
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </>
      )}

      {/* WHY CHARLIE BELIEVES THIS MODAL */}
      <Dialog
        open={explanationDialogOpen}
        onClose={() => setExplanationDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle sx={{ fontWeight: 800, bgcolor: '#0f172a', color: '#fff', display: 'flex', alignItems: 'center', gap: 1 }}>
          <PsychologyIcon sx={{ color: '#38bdf8' }} />
          Why Does Charlie Believe This?
        </DialogTitle>
        <DialogContent sx={{ mt: 2 }}>
          {selectedExplanation && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box>
                <Typography variant="caption" color="textSecondary" sx={{ fontWeight: 700 }}>STATEMENT:</Typography>
                <Typography variant="subtitle1" sx={{ fontWeight: 800, color: '#0f172a' }}>
                  "{selectedExplanation.statement}"
                </Typography>
              </Box>

              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">LEARNING TIER:</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>{selectedExplanation.tier}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">CAUSAL CONFIDENCE:</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>{((selectedExplanation.causalConfidence || 0) * 100).toFixed(0)}%</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">INDEPENDENT VALIDATIONS:</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>{selectedExplanation.validationCount}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">CONTRADICTIONS:</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 700 }}>{selectedExplanation.contradictionCount}</Typography>
                </Grid>
              </Grid>

              <Box sx={{ bgcolor: '#f8fafc', p: 2, borderRadius: 2, border: '1px solid #e2e8f0' }}>
                <Typography variant="caption" sx={{ fontWeight: 800, color: '#475569', display: 'block', mb: 0.5 }}>
                  GOVERNED RATIONALE & PROVENANCE:
                </Typography>
                <Typography variant="body2" sx={{ color: '#334155' }}>
                  {selectedExplanation.rationale}
                </Typography>
              </Box>

              <Alert severity="info" icon={<WarningAmberIcon fontSize="inherit" />}>
                This insight is advisory institutional knowledge and does not independently define financial truth or policy authority.
              </Alert>
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setExplanationDialogOpen(false)} variant="contained" color="primary">
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
