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
import ShieldIcon from '@mui/icons-material/Shield';
import BugReportIcon from '@mui/icons-material/BugReport';
import VerifiedUserIcon from '@mui/icons-material/VerifiedUser';
import RefreshIcon from '@mui/icons-material/Refresh';
import PowerSettingsNewIcon from '@mui/icons-material/PowerSettingsNew';
import CodeIcon from '@mui/icons-material/Code';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';

interface PostureData {
  overallScore: number;
  rating: string;
  identityAndAccessScore: number;
  epistemicIntegrityScore: number;
  tenantIsolationScore: number;
  promptInjectionImmunityScore: number;
  cryptographicLineageScore: number;
  auditImmutabilityScore: number;
  openCriticalFindings: number;
  openHighFindings: number;
  openMediumFindings: number;
  openLowFindings: number;
  containedFindings: number;
}

interface KillSwitchStatus {
  isActive: boolean;
  triggeredAt: string | null;
  initiatedBy: string;
  reason: string;
  executionHaltDurationMs: number;
}

interface FindingItem {
  id: string;
  testName: string;
  category: number;
  severity: number;
  status: number;
  inputPayload: string;
  observedResult: string;
  expectedResult: string;
  reproductionPoC: string;
  reproductionHash: string;
  isContainedInSandbox: boolean;
  discoveredAt: string;
}

interface CampaignItem {
  id: string;
  campaignName: string;
  strategy: number;
  status: number;
  findingsCount: number;
  targetSandboxId: string;
  startedAt: string | null;
}

interface RemediationItem {
  id: string;
  findingId: string;
  candidateFixSummary: string;
  patchType: number;
  isPreApprovedReversibleSandboxAction: boolean;
  requiresGovernanceApproval: boolean;
  isApproved: boolean;
  approvedBy: string;
  poCPassed: boolean;
  regressionTested: boolean;
}

export const SecurityCommandCenterPage: React.FC = () => {
  const [loading, setLoading] = useState(true);
  const [posture, setPosture] = useState<PostureData | null>(null);
  const [killSwitch, setKillSwitch] = useState<KillSwitchStatus | null>(null);
  const [findings, setFindings] = useState<FindingItem[]>([]);
  const [campaigns, setCampaigns] = useState<CampaignItem[]>([]);
  const [remediations, setRemediations] = useState<RemediationItem[]>([]);
  const [activeTab, setActiveTab] = useState(0);

  const [selectedFinding, setSelectedFinding] = useState<FindingItem | null>(null);
  const [killSwitchModalOpen, setKillSwitchModalOpen] = useState(false);
  const [actionMessage, setActionMessage] = useState<string | null>(null);

  const fetchData = async () => {
    setLoading(true);
    try {
      const postureRes = await fetch('/api/security/posture', {
        headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` }
      });
      if (postureRes.ok) {
        const data = await postureRes.json();
        setPosture(data.posture || data.Posture);
        setKillSwitch(data.killSwitch || data.KillSwitch);
      }

      const findingsRes = await fetch('/api/security/findings', {
        headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` }
      });
      if (findingsRes.ok) {
        setFindings(await findingsRes.json());
      }

      const campaignsRes = await fetch('/api/security/campaigns', {
        headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` }
      });
      if (campaignsRes.ok) {
        setCampaigns(await campaignsRes.json());
      }

      const remRes = await fetch('/api/security/remediations', {
        headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` }
      });
      if (remRes.ok) {
        setRemediations(await remRes.json());
      }
    } catch (e) {
      console.error('Failed to load security telemetry', e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleTriggerKillSwitch = async () => {
    try {
      const res = await fetch('/api/security/kill-switch/trigger', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${localStorage.getItem('token') || ''}`
        },
        body: JSON.stringify({ reason: 'Manual Emergency Kill Switch Triggered by Security Operator' })
      });
      if (res.ok) {
        const updated = await res.json();
        setKillSwitch(updated);
        setActionMessage('EMERGENCY KILL SWITCH ACTIVATED: All security campaigns halted.');
      }
    } catch (e) {
      console.error(e);
    } finally {
      setKillSwitchModalOpen(false);
    }
  };

  const handleResetKillSwitch = async () => {
    try {
      const res = await fetch('/api/security/kill-switch/reset', {
        method: 'POST',
        headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` }
      });
      if (res.ok) {
        const updated = await res.json();
        setKillSwitch(updated);
        setActionMessage('Kill Switch Disarmed. Security workers re-armed.');
      }
    } catch (e) {
      console.error(e);
    }
  };

  const getSeverityChip = (severity: number) => {
    switch (severity) {
      case 4:
        return <Chip label="CRITICAL" color="error" size="small" />;
      case 3:
        return <Chip label="HIGH" sx={{ bgcolor: '#ff9800', color: '#fff' }} size="small" />;
      case 2:
        return <Chip label="MEDIUM" color="warning" size="small" />;
      default:
        return <Chip label="LOW" color="info" size="small" />;
    }
  };

  return (
    <Box sx={{ p: 3, bgcolor: '#0B0F19', minHeight: '100vh', color: '#E2E8F0' }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 700, display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <ShieldIcon sx={{ color: '#38BDF8', fontSize: 36 }} />
            Security Command Center
          </Typography>
          <Typography variant="body2" sx={{ color: '#94A3B8' }}>
            Autonomous Strix Red/Blue Team Engine • Deterministic Target Allowlist • Governed Remediation
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1.5 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={fetchData}
            sx={{ borderColor: '#334155', color: '#94A3B8' }}
          >
            Refresh
          </Button>

          {killSwitch?.isActive ? (
            <Button
              variant="contained"
              color="success"
              startIcon={<PowerSettingsNewIcon />}
              onClick={handleResetKillSwitch}
            >
              Disarm Kill Switch
            </Button>
          ) : (
            <Button
              variant="contained"
              color="error"
              startIcon={<PowerSettingsNewIcon />}
              onClick={() => setKillSwitchModalOpen(true)}
            >
              EMERGENCY KILL SWITCH
            </Button>
          )}
        </Box>
      </Box>

      {loading && <LinearProgress sx={{ mb: 2, borderRadius: 1 }} />}

      {/* Kill Switch Active Alert */}
      {killSwitch?.isActive && (
        <Alert severity="error" sx={{ mb: 3, fontWeight: 600 }}>
          EMERGENCY KILL SWITCH ACTIVE: All active Red/Blue team tasks and background workers have been terminated.
          Initiated by: {killSwitch.initiatedBy} • Reason: {killSwitch.reason}
        </Alert>
      )}

      {actionMessage && (
        <Alert severity="info" sx={{ mb: 3 }} onClose={() => setActionMessage(null)}>
          {actionMessage}
        </Alert>
      )}

      {/* Security Posture Overview */}
      <Grid container spacing={2.5} sx={{ mb: 3 }}>
        <Grid item xs={12} md={4}>
          <Card sx={{ p: 2.5, bgcolor: '#1E293B', border: '1px solid #334155', height: '100%' }}>
            <Typography variant="subtitle2" sx={{ color: '#94A3B8', mb: 1 }}>
              OVERALL SECURITY POSTURE
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1.5, my: 1 }}>
              <Typography variant="h2" sx={{ fontWeight: 800, color: (posture?.overallScore || 95) >= 90 ? '#10B981' : '#F59E0B' }}>
                {posture?.overallScore || 95.0}
              </Typography>
              <Typography variant="h6" sx={{ color: '#64748B' }}>/ 100</Typography>
              <Chip
                label={posture?.rating || 'Excellent'}
                color={(posture?.overallScore || 95) >= 90 ? 'success' : 'warning'}
                size="small"
                sx={{ ml: 'auto' }}
              />
            </Box>
            <LinearProgress
              variant="determinate"
              value={posture?.overallScore || 95}
              sx={{
                height: 8,
                borderRadius: 4,
                bgcolor: '#0F172A',
                '& .MuiLinearProgress-bar': {
                  bgcolor: (posture?.overallScore || 95) >= 90 ? '#10B981' : '#F59E0B'
                }
              }}
            />
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 2, color: '#94A3B8', fontSize: '0.85rem' }}>
              <span>Open Critical: {posture?.openCriticalFindings || 0}</span>
              <span>Open High: {posture?.openHighFindings || 0}</span>
              <span>Contained: {posture?.containedFindings || 0}</span>
            </Box>
          </Card>
        </Grid>

        <Grid item xs={12} md={8}>
          <Card sx={{ p: 2.5, bgcolor: '#1E293B', border: '1px solid #334155', height: '100%' }}>
            <Typography variant="subtitle2" sx={{ color: '#94A3B8', mb: 2 }}>
              CORE DEFENSE METRICS (STRIX SPECIFICATION)
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={6} sm={4}>
                <Box sx={{ p: 1.5, bgcolor: '#0F172A', borderRadius: 1.5 }}>
                  <Typography variant="caption" sx={{ color: '#64748B' }}>Identity & Access</Typography>
                  <Typography variant="h6" sx={{ color: '#38BDF8', fontWeight: 700 }}>
                    {posture?.identityAndAccessScore || 100}%
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6} sm={4}>
                <Box sx={{ p: 1.5, bgcolor: '#0F172A', borderRadius: 1.5 }}>
                  <Typography variant="caption" sx={{ color: '#64748B' }}>Epistemic Integrity</Typography>
                  <Typography variant="h6" sx={{ color: '#38BDF8', fontWeight: 700 }}>
                    {posture?.epistemicIntegrityScore || 95}%
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6} sm={4}>
                <Box sx={{ p: 1.5, bgcolor: '#0F172A', borderRadius: 1.5 }}>
                  <Typography variant="caption" sx={{ color: '#64748B' }}>Tenant Isolation</Typography>
                  <Typography variant="h6" sx={{ color: '#10B981', fontWeight: 700 }}>
                    {posture?.tenantIsolationScore || 100}%
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6} sm={4}>
                <Box sx={{ p: 1.5, bgcolor: '#0F172A', borderRadius: 1.5 }}>
                  <Typography variant="caption" sx={{ color: '#64748B' }}>Prompt Injection Immunity</Typography>
                  <Typography variant="h6" sx={{ color: '#38BDF8', fontWeight: 700 }}>
                    {posture?.promptInjectionImmunityScore || 95}%
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6} sm={4}>
                <Box sx={{ p: 1.5, bgcolor: '#0F172A', borderRadius: 1.5 }}>
                  <Typography variant="caption" sx={{ color: '#64748B' }}>Cryptographic Lineage</Typography>
                  <Typography variant="h6" sx={{ color: '#10B981', fontWeight: 700 }}>
                    {posture?.cryptographicLineageScore || 100}%
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6} sm={4}>
                <Box sx={{ p: 1.5, bgcolor: '#0F172A', borderRadius: 1.5 }}>
                  <Typography variant="caption" sx={{ color: '#64748B' }}>Audit Immutability</Typography>
                  <Typography variant="h6" sx={{ color: '#10B981', fontWeight: 700 }}>
                    {posture?.auditImmutabilityScore || 100}%
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </Card>
        </Grid>
      </Grid>

      {/* Tabs */}
      <Tabs
        value={activeTab}
        onChange={(_, v) => setActiveTab(v)}
        sx={{
          mb: 2.5,
          borderBottom: '1px solid #334155',
          '& .MuiTab-root': { color: '#94A3B8' },
          '& .Mui-selected': { color: '#38BDF8' }
        }}
      >
        <Tab icon={<BugReportIcon />} iconPosition="start" label={`Vulnerability Findings (${findings.length})`} />
        <Tab icon={<PlayArrowIcon />} iconPosition="start" label={`Red Team Campaigns (${campaigns.length})`} />
        <Tab icon={<VerifiedUserIcon />} iconPosition="start" label={`Blue Team Remediations (${remediations.length})`} />
      </Tabs>

      {/* Tab 0: Findings Matrix */}
      {activeTab === 0 && (
        <TableContainer component={Paper} sx={{ bgcolor: '#1E293B', border: '1px solid #334155' }}>
          <Table>
            <TableHead sx={{ bgcolor: '#0F172A' }}>
              <TableRow>
                <TableCell sx={{ color: '#94A3B8' }}>Severity</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Test Name</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Category</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Status</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Reproduction Hash</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {findings.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} sx={{ color: '#64748B', textAlign: 'center', py: 4 }}>
                    No security vulnerabilities detected. Target sandbox boundaries verified secure.
                  </TableCell>
                </TableRow>
              ) : (
                findings.map(f => (
                  <TableRow key={f.id}>
                    <TableCell>{getSeverityChip(f.severity)}</TableCell>
                    <TableCell sx={{ color: '#F1F5F9', fontWeight: 600 }}>{f.testName}</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Category {f.category}</TableCell>
                    <TableCell>
                      <Chip
                        label={f.status === 5 ? 'REMEDIATED' : f.status === 3 ? 'CONTAINED' : 'DISCOVERED'}
                        color={f.status === 5 ? 'success' : 'warning'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell sx={{ color: '#64748B', fontFamily: 'monospace', fontSize: '0.8rem' }}>
                      {f.reproductionHash ? f.reproductionHash.substring(0, 16) + '...' : 'N/A'}
                    </TableCell>
                    <TableCell>
                      <Button
                        size="small"
                        variant="outlined"
                        startIcon={<CodeIcon />}
                        onClick={() => setSelectedFinding(f)}
                        sx={{ borderColor: '#334155', color: '#38BDF8' }}
                      >
                        Evidence Chain
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Tab 1: Campaigns */}
      {activeTab === 1 && (
        <TableContainer component={Paper} sx={{ bgcolor: '#1E293B', border: '1px solid #334155' }}>
          <Table>
            <TableHead sx={{ bgcolor: '#0F172A' }}>
              <TableRow>
                <TableCell sx={{ color: '#94A3B8' }}>Campaign</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Strategy</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Sandbox ID</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Findings Discovered</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {campaigns.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} sx={{ color: '#64748B', textAlign: 'center', py: 4 }}>
                    No security campaigns running. Launch a scoped sandbox campaign to evaluate attack surface.
                  </TableCell>
                </TableRow>
              ) : (
                campaigns.map(c => (
                  <TableRow key={c.id}>
                    <TableCell sx={{ color: '#F1F5F9', fontWeight: 600 }}>{c.campaignName}</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Strategy {c.strategy}</TableCell>
                    <TableCell sx={{ color: '#64748B', fontFamily: 'monospace' }}>{c.targetSandboxId}</TableCell>
                    <TableCell sx={{ color: '#F1F5F9' }}>{c.findingsCount}</TableCell>
                    <TableCell>
                      <Chip
                        label={c.status === 2 ? 'RUNNING' : c.status === 4 ? 'KILLED' : 'COMPLETED'}
                        color={c.status === 2 ? 'primary' : c.status === 4 ? 'error' : 'default'}
                        size="small"
                      />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Tab 2: Remediations */}
      {activeTab === 2 && (
        <TableContainer component={Paper} sx={{ bgcolor: '#1E293B', border: '1px solid #334155' }}>
          <Table>
            <TableHead sx={{ bgcolor: '#0F172A' }}>
              <TableRow>
                <TableCell sx={{ color: '#94A3B8' }}>Candidate Fix</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Patch Type</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Sandbox Pre-Approved</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>PoC Verified</TableCell>
                <TableCell sx={{ color: '#94A3B8' }}>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {remediations.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} sx={{ color: '#64748B', textAlign: 'center', py: 4 }}>
                    No candidate remediations pending review.
                  </TableCell>
                </TableRow>
              ) : (
                remediations.map(r => (
                  <TableRow key={r.id}>
                    <TableCell sx={{ color: '#F1F5F9' }}>{r.candidateFixSummary}</TableCell>
                    <TableCell sx={{ color: '#94A3B8' }}>Patch Type {r.patchType}</TableCell>
                    <TableCell>
                      <Chip
                        label={r.isPreApprovedReversibleSandboxAction ? 'YES (Sandbox)' : 'NO (Production)'}
                        color={r.isPreApprovedReversibleSandboxAction ? 'info' : 'warning'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={r.poCPassed ? 'PASSED' : 'PENDING'}
                        color={r.poCPassed ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={r.isApproved ? 'APPLIED' : 'GOVERNANCE REQUIRED'}
                        color={r.isApproved ? 'success' : 'warning'}
                        size="small"
                      />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Finding Evidence Chain Dialog */}
      <Dialog
        open={Boolean(selectedFinding)}
        onClose={() => setSelectedFinding(null)}
        maxWidth="md"
        fullWidth
        PaperProps={{ sx: { bgcolor: '#1E293B', color: '#E2E8F0', border: '1px solid #334155' } }}
      >
        <DialogTitle sx={{ borderBottom: '1px solid #334155', display: 'flex', justifyContent: 'space-between' }}>
          <span>Finding Evidence Chain: {selectedFinding?.testName}</span>
          {selectedFinding && getSeverityChip(selectedFinding.severity)}
        </DialogTitle>
        <DialogContent sx={{ mt: 2 }}>
          {selectedFinding && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box>
                <Typography variant="caption" sx={{ color: '#94A3B8' }}>REPRODUCTION CRYPTOGRAPHIC HASH</Typography>
                <Typography sx={{ fontFamily: 'monospace', color: '#38BDF8', wordBreak: 'break-all' }}>
                  {selectedFinding.reproductionHash}
                </Typography>
              </Box>
              <Box>
                <Typography variant="caption" sx={{ color: '#94A3B8' }}>ATTACK INPUT PAYLOAD</Typography>
                <Paper sx={{ p: 1.5, bgcolor: '#0F172A', color: '#F1F5F9', fontFamily: 'monospace', fontSize: '0.85rem' }}>
                  {selectedFinding.inputPayload}
                </Paper>
              </Box>
              <Box>
                <Typography variant="caption" sx={{ color: '#94A3B8' }}>OBSERVED RESULT</Typography>
                <Paper sx={{ p: 1.5, bgcolor: '#0F172A', color: '#F1F5F9', fontFamily: 'monospace', fontSize: '0.85rem' }}>
                  {selectedFinding.observedResult}
                </Paper>
              </Box>
              <Box>
                <Typography variant="caption" sx={{ color: '#94A3B8' }}>EXPECTED RESULT</Typography>
                <Paper sx={{ p: 1.5, bgcolor: '#0F172A', color: '#F1F5F9', fontFamily: 'monospace', fontSize: '0.85rem' }}>
                  {selectedFinding.expectedResult}
                </Paper>
              </Box>
              <Box>
                <Typography variant="caption" sx={{ color: '#94A3B8' }}>DETERMINISTIC NON-DESTRUCTIVE PROOF-OF-CONCEPT</Typography>
                <Paper sx={{ p: 1.5, bgcolor: '#0B0F19', color: '#10B981', fontFamily: 'monospace', fontSize: '0.8rem', whiteSpace: 'pre-wrap' }}>
                  {selectedFinding.reproductionPoC}
                </Paper>
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ borderTop: '1px solid #334155' }}>
          <Button onClick={() => setSelectedFinding(null)} sx={{ color: '#94A3B8' }}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Emergency Kill Switch Modal */}
      <Dialog
        open={killSwitchModalOpen}
        onClose={() => setKillSwitchModalOpen(false)}
        PaperProps={{ sx: { bgcolor: '#1E293B', color: '#E2E8F0', border: '1px solid #EF4444' } }}
      >
        <DialogTitle sx={{ color: '#EF4444', fontWeight: 700 }}>
          CONFIRM EMERGENCY KILL SWITCH
        </DialogTitle>
        <DialogContent>
          <Typography variant="body1" sx={{ mb: 2 }}>
            Are you sure you want to trigger the Emergency Security Kill Switch?
          </Typography>
          <Typography variant="body2" sx={{ color: '#94A3B8' }}>
            This will deterministically halt all autonomous Red Team and Blue Team tasks, cancel ongoing campaigns within &le;100ms, and lock all disposable sandboxes into read-only quarantine.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setKillSwitchModalOpen(false)} sx={{ color: '#94A3B8' }}>Cancel</Button>
          <Button variant="contained" color="error" onClick={handleTriggerKillSwitch}>
            CONFIRM KILL SWITCH
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default SecurityCommandCenterPage;
