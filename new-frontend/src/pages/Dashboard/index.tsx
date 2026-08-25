import { useState } from 'react';
import {
  Grid,
  Paper,
  Typography,
  Box,
  Button,
  Chip,
  ChipProps,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  MenuItem,
  Card,
  CardContent,
  Stack,
  Divider,
  Alert,
  LinearProgress,
} from '@mui/material';
import { useCommercial, Lead, Opportunity } from '../../hooks/useCommercial';
import { useAnalytics, EvidenceRecord } from '../../hooks/useAnalytics';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

const formatINR = (amount: number) => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  }).format(amount);
};

const getStageColor = (stage: string): ChipProps['color'] => {
  switch (stage) {
    case 'Discovery':
      return 'info';
    case 'Proposal':
      return 'primary';
    case 'Negotiation':
      return 'warning';
    case 'ClosedWon':
      return 'success';
    case 'ClosedLost':
      return 'error';
    default:
      return 'default';
  }
};

const Dashboard = () => {
  const { leads, opportunities, isLoadingLeads, isLoadingOpportunities, createLead, qualifyLead, updateStage } =
    useCommercial();
  const { businessHealth, executiveBrief, isLoading: isLoadingAnalytics } = useAnalytics();

  // Create Lead Dialog State
  const [isLeadDialogOpen, setLeadDialogOpen] = useState(false);
  const [newLead, setNewLead] = useState({
    contactName: '',
    companyName: '',
    email: '',
    phone: '',
    source: 0,
    notes: '',
  });

  // Qualify Lead Dialog State
  const [selectedLeadForQualify, setSelectedLeadForQualify] = useState<Lead | null>(null);
  const [oppData, setOppData] = useState({
    title: '',
    estimatedValue: 500000,
    primaryConcern: '',
    nextStep: '',
  });

  // Selected Opportunity Detail Modal
  const [selectedOpportunity, setSelectedOpportunity] = useState<Opportunity | null>(null);

  // Selected Evidence Record Modal (Level 3 Drill-Down)
  const [selectedEvidence, setSelectedEvidence] = useState<EvidenceRecord | null>(null);

  if (isLoadingLeads || isLoadingOpportunities || isLoadingAnalytics) {
    return <LoadingState message="Loading business operating system..." />;
  }

  // Financial KPIs
  const totalPipelineValue = businessHealth?.totalPipelineValue ?? 0;
  const weightedPipelineValue = businessHealth?.weightedForecastValue ?? 0;
  const wonValue = businessHealth?.closedWonRevenue ?? 0;
  const healthScore = businessHealth?.overallHealthScore ?? 75;
  const confidenceLevel = businessHealth?.confidenceLevel ?? 'Medium';
  const confidenceScore = businessHealth?.confidenceScore ?? 0.65;

  const handleCreateLead = async () => {
    if (!newLead.contactName || !newLead.companyName) return;
    await createLead.mutateAsync(newLead);
    setLeadDialogOpen(false);
    setNewLead({ contactName: '', companyName: '', email: '', phone: '', source: 0, notes: '' });
  };

  const handleQualifyLead = async () => {
    if (!selectedLeadForQualify) return;
    await qualifyLead.mutateAsync({
      leadId: selectedLeadForQualify.id,
      input: {
        title: oppData.title || `${selectedLeadForQualify.companyName} - Solution`,
        estimatedValue: Number(oppData.estimatedValue) || 500000,
        primaryConcern: oppData.primaryConcern,
        nextStep: oppData.nextStep,
      },
    });
    setSelectedLeadForQualify(null);
    setOppData({ title: '', estimatedValue: 500000, primaryConcern: '', nextStep: '' });
  };

  const handleAdvanceStage = async (oppId: string, nextStageNumber: number) => {
    await updateStage.mutateAsync({
      opportunityId: oppId,
      stage: nextStageNumber,
      reasonOrNote: `Stage progressed by user via Executive Dashboard`,
    });
    if (selectedOpportunity && selectedOpportunity.id === oppId) {
      setSelectedOpportunity(null);
    }
  };

  const openEvidenceById = (evidenceId: string) => {
    const found = businessHealth?.evidenceRecords.find((e) => e.evidenceId === evidenceId);
    if (found) {
      setSelectedEvidence(found);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Top Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <div>
          <Typography variant="h4" fontWeight="bold">
            AI Executive Operating System
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Deterministic Engine: Verified Facts → Evidence → AI Interpretation → Human Action
          </Typography>
        </div>
        <Button variant="contained" color="primary" onClick={() => setLeadDialogOpen(true)}>
          + Add Inbound Lead
        </Button>
      </Box>

      {/* LEVEL 1: Executive Morning Brief Card */}
      <Paper
        elevation={3}
        sx={{
          p: 3,
          mb: 4,
          background: 'linear-gradient(135deg, rgba(25, 118, 210, 0.05) 0%, rgba(156, 39, 176, 0.05) 100%)',
          border: '1px solid rgba(25, 118, 210, 0.2)',
          borderRadius: 2,
        }}
      >
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <div>
            <Typography variant="overline" color="primary.main" fontWeight="bold" letterSpacing={1}>
              Level 1 — Executive Morning Brief ({businessHealth?.calculationVersion || 'HealthEngine:v1.0'})
            </Typography>
            <Typography variant="h6" fontWeight="bold" sx={{ mt: 0.5 }}>
              {executiveBrief?.summary || 'Business operations are operating within expected quarterly targets.'}
            </Typography>
          </div>
          <Stack direction="row" spacing={1}>
            <Chip
              label={`Health: ${healthScore.toFixed(0)}/100`}
              color={healthScore >= 75 ? 'success' : healthScore >= 50 ? 'warning' : 'error'}
              sx={{ fontWeight: 'bold' }}
            />
            <Chip
              label={`Confidence: ${confidenceLevel} (${(confidenceScore * 100).toFixed(0)}%)`}
              variant="outlined"
              color={confidenceLevel === 'High' ? 'success' : confidenceLevel === 'Medium' ? 'primary' : 'warning'}
            />
            {!executiveBrief?.isActionRequired ? (
              <Chip label="No Intervention Required" color="success" variant="outlined" />
            ) : (
              <Chip label="Action Recommended" color="warning" />
            )}
          </Stack>
        </Box>

        {/* Actionable Recommendations with Cited Evidence Links */}
        {executiveBrief?.recommendations && executiveBrief.recommendations.length > 0 && (
          <Stack spacing={1} sx={{ mt: 2 }}>
            {executiveBrief.recommendations.map((rec, idx) => (
              <Alert
                key={idx}
                severity="info"
                action={
                  rec.citedEvidenceId ? (
                    <Button
                      color="inherit"
                      size="small"
                      variant="outlined"
                      onClick={() => openEvidenceById(rec.citedEvidenceId)}
                    >
                      View Evidence #{rec.citedEvidenceId}
                    </Button>
                  ) : undefined
                }
              >
                <strong>{rec.title}</strong> — {rec.rationale}
              </Alert>
            ))}
          </Stack>
        )}
      </Paper>

      {/* LEVEL 2: Deterministic Business Health Breakdown (Why) */}
      <Typography variant="h6" fontWeight="bold" sx={{ mb: 2 }}>
        Level 2 — Business Health Component Breakdown
      </Typography>
      <Grid container spacing={2.5} sx={{ mb: 4 }}>
        {businessHealth?.componentBreakdown.map((comp, idx) => (
          <Grid item xs={12} sm={6} md={3} key={idx}>
            <Paper elevation={2} sx={{ p: 2.5, height: '100%', display: 'flex', flexDirection: 'column' }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                <Typography variant="subtitle2" fontWeight="bold">
                  {comp.componentName}
                </Typography>
                <Chip label={`Weight ${comp.weightPercent}%`} size="small" variant="outlined" />
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'baseline', my: 1 }}>
                <Typography variant="h4" fontWeight="bold" color="primary.main">
                  {comp.rawScore.toFixed(0)}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ ml: 1 }}>
                  / 100 ({comp.weightedContribution.toFixed(1)} pts)
                </Typography>
              </Box>
              <LinearProgress
                variant="determinate"
                value={Math.min(100, comp.rawScore)}
                sx={{ height: 6, borderRadius: 3, mb: 1.5 }}
              />
              <Typography variant="caption" color="text.secondary" sx={{ flexGrow: 1 }}>
                {comp.explanation}
              </Typography>
            </Paper>
          </Grid>
        ))}
      </Grid>

      {/* Financial & Commercial KPI Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Paper elevation={2} sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Total Pipeline Value
            </Typography>
            <Typography variant="h5" color="primary.main" fontWeight="bold" sx={{ mt: 1 }}>
              {formatINR(totalPipelineValue)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Across {opportunities.length} active opportunities
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Paper elevation={2} sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Weighted Forecast
            </Typography>
            <Typography variant="h5" color="warning.main" fontWeight="bold" sx={{ mt: 1 }}>
              {formatINR(weightedPipelineValue)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Target: {formatINR(businessHealth?.quarterlyTarget ?? 5000000)} ({businessHealth?.pipelineCoverageRatio ?? 0}x)
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Paper elevation={2} sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Closed Won Revenue
            </Typography>
            <Typography variant="h5" color="success.main" fontWeight="bold" sx={{ mt: 1 }}>
              {formatINR(wonValue)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Win Rate: {((businessHealth?.winRate ?? 0.5) * 100).toFixed(0)}%
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Paper elevation={2} sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Inbound Leads Pool
            </Typography>
            <Typography variant="h5" color="info.main" fontWeight="bold" sx={{ mt: 1 }}>
              {leads.length}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Qual Rate: {((businessHealth?.leadQualificationRate ?? 0) * 100).toFixed(0)}%
            </Typography>
          </Paper>
        </Grid>
      </Grid>

      {/* Main Workspaces: Opportunities Pipeline + Leads Feed */}
      <Grid container spacing={3}>
        {/* Active Opportunities Table */}
        <Grid item xs={12} lg={8}>
          <Paper elevation={2} sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              Commercial Opportunities Pipeline
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Verified lifecycle with server-side authorization and append-only audit tracking.
            </Typography>

            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Opportunity</TableCell>
                    <TableCell>Account</TableCell>
                    <TableCell>Value</TableCell>
                    <TableCell>Stage</TableCell>
                    <TableCell>Probability</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {opportunities.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} align="center" sx={{ py: 3 }}>
                        <Typography color="text.secondary">No opportunities created yet.</Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    opportunities.map((opp) => (
                      <TableRow key={opp.id} hover sx={{ cursor: 'pointer' }}>
                        <TableCell onClick={() => setSelectedOpportunity(opp)}>
                          <Typography variant="subtitle2" fontWeight="600">
                            {opp.title}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Next: {opp.nextStep || 'Review commercial terms'}
                          </Typography>
                        </TableCell>
                        <TableCell onClick={() => setSelectedOpportunity(opp)}>
                          {opp.leadCompanyName || 'Enterprise'}
                        </TableCell>
                        <TableCell onClick={() => setSelectedOpportunity(opp)} sx={{ fontWeight: 'bold' }}>
                          {formatINR(opp.estimatedValue)}
                        </TableCell>
                        <TableCell onClick={() => setSelectedOpportunity(opp)}>
                          <Chip label={opp.stage} size="small" color={getStageColor(opp.stage)} />
                        </TableCell>
                        <TableCell onClick={() => setSelectedOpportunity(opp)}>
                          {((opp.probability || 0) * 100).toFixed(0)}%
                        </TableCell>
                        <TableCell align="right">
                          {opp.stage === 'Discovery' && (
                            <Button size="small" variant="outlined" onClick={() => handleAdvanceStage(opp.id, 1)}>
                              → Proposal
                            </Button>
                          )}
                          {opp.stage === 'Proposal' && (
                            <Button size="small" variant="outlined" color="warning" onClick={() => handleAdvanceStage(opp.id, 2)}>
                              → Negotiate
                            </Button>
                          )}
                          {opp.stage === 'Negotiation' && (
                            <Button size="small" variant="contained" color="success" onClick={() => handleAdvanceStage(opp.id, 3)}>
                              ✓ Close Won
                            </Button>
                          )}
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </Grid>

        {/* Inbound Leads Queue */}
        <Grid item xs={12} lg={4}>
          <Paper elevation={2} sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              Inbound Leads & Qualification
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Omnichannel Leads (Voice AI, Web, WhatsApp, Referral)
            </Typography>

            <Stack spacing={2}>
              {leads.length === 0 ? (
                <Typography color="text.secondary" align="center" sx={{ py: 2 }}>
                  No leads received.
                </Typography>
              ) : (
                leads.map((lead) => (
                  <Card key={lead.id} variant="outlined" sx={{ borderRadius: 2 }}>
                    <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                        <div>
                          <Typography variant="subtitle2" fontWeight="bold">
                            {lead.contactName}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {lead.companyName} • Source: {lead.source}
                          </Typography>
                        </div>
                        <Chip
                          label={`AI Score: ${lead.qualityScore?.toFixed(0) || 75}`}
                          size="small"
                          color={lead.qualityScore >= 80 ? 'success' : 'default'}
                        />
                      </Box>

                      {lead.notes && (
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1, fontSize: '0.85rem' }}>
                          "{lead.notes}"
                        </Typography>
                      )}

                      <Box sx={{ mt: 1.5, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Chip label={lead.status} size="small" variant="outlined" />
                        {!lead.hasOpportunity && (
                          <Button
                            size="small"
                            variant="contained"
                            color="primary"
                            onClick={() => {
                              setSelectedLeadForQualify(lead);
                              setOppData({
                                title: `${lead.companyName} - Commercial Opportunity`,
                                estimatedValue: 750000,
                                primaryConcern: 'Implementation timeline & SLAs',
                                nextStep: 'Schedule technical discovery demo',
                              });
                            }}
                          >
                            Qualify → Opp
                          </Button>
                        )}
                      </Box>
                    </CardContent>
                  </Card>
                ))
              )}
            </Stack>
          </Paper>
        </Grid>
      </Grid>

      {/* LEVEL 3: Evidence Drill-Down Dialog */}
      <Dialog open={Boolean(selectedEvidence)} onClose={() => setSelectedEvidence(null)} maxWidth="sm" fullWidth>
        <DialogTitle>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>Evidence #{selectedEvidence?.evidenceId}</span>
            <Chip label={selectedEvidence?.evidenceType} color="primary" size="small" />
          </Box>
        </DialogTitle>
        <DialogContent>
          <Box sx={{ mb: 2 }}>
            <Typography variant="caption" color="text.secondary">
              Metric & Value
            </Typography>
            <Typography variant="h5" fontWeight="bold" color="primary.main">
              {selectedEvidence?.displayName}: {selectedEvidence?.formattedValue}
            </Typography>
          </Box>
          <Grid container spacing={2} sx={{ mb: 2 }}>
            <Grid item xs={6}>
              <Typography variant="caption" color="text.secondary">
                Mathematical Formula
              </Typography>
              <Typography variant="body2" fontFamily="monospace">
                {selectedEvidence?.formula}
              </Typography>
            </Grid>
            <Grid item xs={6}>
              <Typography variant="caption" color="text.secondary">
                Version & Period
              </Typography>
              <Typography variant="body2">
                {selectedEvidence?.calculationVersion} ({selectedEvidence?.period})
              </Typography>
            </Grid>
          </Grid>
          <Divider sx={{ my: 2 }} />
          <Typography variant="subtitle2" fontWeight="bold" gutterBottom>
            Contributing Source Records
          </Typography>
          <Stack spacing={1} sx={{ mt: 1 }}>
            {selectedEvidence?.contributors.map((contrib, idx) => (
              <Paper key={idx} variant="outlined" sx={{ p: 1.5, borderRadius: 1.5 }}>
                <Typography variant="subtitle2" fontWeight="bold">
                  {contrib.name}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {contrib.contributionDetails}
                </Typography>
              </Paper>
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelectedEvidence(null)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Dialog: Create Inbound Lead */}
      <Dialog open={isLeadDialogOpen} onClose={() => setLeadDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Inbound Lead</DialogTitle>
        <DialogContent>
          <TextField
            margin="dense"
            label="Contact Name"
            fullWidth
            required
            value={newLead.contactName}
            onChange={(e) => setNewLead({ ...newLead, contactName: e.target.value })}
          />
          <TextField
            margin="dense"
            label="Company Name"
            fullWidth
            required
            value={newLead.companyName}
            onChange={(e) => setNewLead({ ...newLead, companyName: e.target.value })}
          />
          <TextField
            margin="dense"
            label="Email"
            fullWidth
            type="email"
            value={newLead.email}
            onChange={(e) => setNewLead({ ...newLead, email: e.target.value })}
          />
          <TextField
            margin="dense"
            label="Phone"
            fullWidth
            value={newLead.phone}
            onChange={(e) => setNewLead({ ...newLead, phone: e.target.value })}
          />
          <TextField
            select
            margin="dense"
            label="Lead Source"
            fullWidth
            value={newLead.source}
            onChange={(e) => setNewLead({ ...newLead, source: Number(e.target.value) })}
          >
            <MenuItem value={0}>Inbound Web</MenuItem>
            <MenuItem value={1}>Voice AI Agent</MenuItem>
            <MenuItem value={2}>WhatsApp</MenuItem>
            <MenuItem value={3}>Email</MenuItem>
            <MenuItem value={4}>Referral</MenuItem>
            <MenuItem value={5}>Manual</MenuItem>
          </TextField>
          <TextField
            margin="dense"
            label="Notes / Intent"
            fullWidth
            multiline
            rows={2}
            value={newLead.notes}
            onChange={(e) => setNewLead({ ...newLead, notes: e.target.value })}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLeadDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleCreateLead} variant="contained" disabled={!newLead.contactName || !newLead.companyName}>
            Create Lead
          </Button>
        </DialogActions>
      </Dialog>

      {/* Dialog: Qualify Lead to Opportunity */}
      <Dialog
        open={Boolean(selectedLeadForQualify)}
        onClose={() => setSelectedLeadForQualify(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Qualify Lead to Opportunity</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Converting <strong>{selectedLeadForQualify?.contactName}</strong> ({selectedLeadForQualify?.companyName}) into a commercial opportunity.
          </Typography>
          <TextField
            margin="dense"
            label="Opportunity Title"
            fullWidth
            required
            value={oppData.title}
            onChange={(e) => setOppData({ ...oppData, title: e.target.value })}
          />
          <TextField
            margin="dense"
            label="Estimated Deal Value (INR)"
            fullWidth
            type="number"
            value={oppData.estimatedValue}
            onChange={(e) => setOppData({ ...oppData, estimatedValue: Number(e.target.value) })}
          />
          <TextField
            margin="dense"
            label="Primary Concern / Blocker"
            fullWidth
            value={oppData.primaryConcern}
            onChange={(e) => setOppData({ ...oppData, primaryConcern: e.target.value })}
          />
          <TextField
            margin="dense"
            label="Immediate Next Step"
            fullWidth
            value={oppData.nextStep}
            onChange={(e) => setOppData({ ...oppData, nextStep: e.target.value })}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelectedLeadForQualify(null)}>Cancel</Button>
          <Button onClick={handleQualifyLead} variant="contained" color="primary">
            Convert to Opportunity
          </Button>
        </DialogActions>
      </Dialog>

      {/* Dialog: Opportunity Details & Audit History */}
      <Dialog open={Boolean(selectedOpportunity)} onClose={() => setSelectedOpportunity(null)} maxWidth="md" fullWidth>
        <DialogTitle>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>{selectedOpportunity?.title}</span>
            <Chip label={selectedOpportunity?.stage} color={getStageColor(selectedOpportunity?.stage || '')} />
          </Box>
        </DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid item xs={6}>
              <Typography variant="caption" color="text.secondary">
                Account
              </Typography>
              <Typography variant="subtitle1" fontWeight="bold">
                {selectedOpportunity?.leadCompanyName} ({selectedOpportunity?.leadContactName})
              </Typography>
            </Grid>
            <Grid item xs={6}>
              <Typography variant="caption" color="text.secondary">
                Deal Value
              </Typography>
              <Typography variant="subtitle1" fontWeight="bold" color="primary.main">
                {formatINR(selectedOpportunity?.estimatedValue || 0)}
              </Typography>
            </Grid>
            <Grid item xs={6}>
              <Typography variant="caption" color="text.secondary">
                Primary Concern
              </Typography>
              <Typography variant="body2">{selectedOpportunity?.primaryConcern || 'None reported'}</Typography>
            </Grid>
            <Grid item xs={6}>
              <Typography variant="caption" color="text.secondary">
                Next Step
              </Typography>
              <Typography variant="body2">{selectedOpportunity?.nextStep || 'Follow up with executive'}</Typography>
            </Grid>
          </Grid>

          <Divider sx={{ my: 2 }} />
          <Typography variant="h6" fontWeight="bold" gutterBottom>
            Activity & Audit History
          </Typography>

          <Stack spacing={1.5} sx={{ mt: 1 }}>
            {selectedOpportunity?.recentActivities?.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No recorded activities.
              </Typography>
            ) : (
              selectedOpportunity?.recentActivities?.map((act) => (
                <Paper key={act.id} variant="outlined" sx={{ p: 1.5, borderRadius: 1.5 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="subtitle2" fontWeight="bold">
                      {act.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {new Date(act.createdAt).toLocaleString()}
                    </Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    {act.description}
                  </Typography>
                  <Typography variant="caption" color="primary">
                    By: {act.performedByName}
                  </Typography>
                </Paper>
              ))
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelectedOpportunity(null)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default function DashboardWithErrorBoundary() {
  return (
    <ErrorBoundary>
      <Dashboard />
    </ErrorBoundary>
  );
}
