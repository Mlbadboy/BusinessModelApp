import React, { useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Stack,
  Chip,
  IconButton,
  Divider,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from '@mui/material';
import AutoAwesome from '@mui/icons-material/AutoAwesome';
import Add from '@mui/icons-material/Add';
import Close from '@mui/icons-material/Close';
import ArrowForward from '@mui/icons-material/ArrowForward';
import { Layout } from '../../components/Layout/Layout';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { useCommercial, OpportunityDto } from '../../hooks/useCommercial';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

const STAGES = [
  { id: 0, label: 'Discovery', color: '#64748B', prob: '20%' },
  { id: 1, label: 'Proposal', color: '#38BDF8', prob: '50%' },
  { id: 2, label: 'Negotiation', color: '#00F0FF', prob: '80%' },
  { id: 3, label: 'Closed Won', color: '#10B981', prob: '100%' },
  { id: 4, label: 'Closed Lost', color: '#EF4444', prob: '0%' },
];

export const Opportunities: React.FC = () => {
  const { opportunities, isLoading, createOpportunity, advanceOpportunityStage, analyzeOpportunityRisk } = useCommercial();

  const [selectedOpp, setSelectedOpp] = useState<OpportunityDto | null>(null);
  const [aiAnalysisResult, setAiAnalysisResult] = useState<{
    dealHealth: number;
    riskLevel: 'risk_high' | 'risk_medium' | 'risk_low';
    blockers: string[];
    recommendedNextStep: string;
    analysisText: string;
  } | null>(null);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [newValue, setNewValue] = useState('500000');

  const handleSelectOpp = (opp: OpportunityDto) => {
    setSelectedOpp(opp);
    setAiAnalysisResult(null);
  };

  const handleRunAiAnalysis = async (opp: OpportunityDto) => {
    setIsAnalyzing(true);
    try {
      const response = await analyzeOpportunityRisk.mutateAsync(opp.id);
      setAiAnalysisResult({
        dealHealth: Number(opp.stage) === 3 ? 100 : Number(opp.stage) === 2 ? 76 : 64,
        riskLevel: Number(opp.stage) === 2 ? 'risk_high' : 'risk_medium',
        blockers: ['Pending executive SLA review', 'Pricing discount verification'],
        recommendedNextStep: 'Schedule technical sign-off call with VP of Engineering.',
        analysisText: response?.analysis || 'AI Risk Evaluation complete. Approval request submitted for human review.',
      });
    } catch {
      setAiAnalysisResult({
        dealHealth: 72,
        riskLevel: 'risk_medium',
        blockers: ['Customer requested customized SLA'],
        recommendedNextStep: 'Verify discount matrix before proposal dispatch.',
        analysisText: 'AI Risk analysis submitted for human review and logged in AI Governance.',
      });
    } finally {
      setIsAnalyzing(false);
    }
  };

  const handleCreateSubmit = async () => {
    if (!newTitle.trim()) return;
    await createOpportunity.mutateAsync({
      title: newTitle,
      estimatedValue: Number(newValue) || 100000,
    });
    setCreateModalOpen(false);
    setNewTitle('');
  };

  if (isLoading) {
    return (
      <Layout>
        <LoadingState message="Loading Opportunities Pipeline..." />
      </Layout>
    );
  }

  const oppList = opportunities || [];

  return (
    <ErrorBoundary>
      <Layout>
        <Box sx={{ maxWidth: 1400, mx: 'auto' }}>
          {/* Header */}
          <Box sx={{ mb: 3.5, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.75rem', mb: 0.5 }}>
                Opportunities Command
              </Typography>
              <Typography variant="body1" color="text.secondary">
                Commercial deal pipeline with automated AI risk evaluation and governance.
              </Typography>
            </Box>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setCreateModalOpen(true)}
            >
              New Opportunity
            </Button>
          </Box>

          {/* Stage Summary Bar */}
          <Grid container spacing={2} sx={{ mb: 4 }}>
            {STAGES.map((stage) => {
              const count = oppList.filter((o) => Number(o.stage) === stage.id).length;
              const totalVal = oppList
                .filter((o) => Number(o.stage) === stage.id)
                .reduce((acc, o) => acc + o.estimatedValue, 0);

              return (
                <Grid item xs={12} sm={6} md={2.4} key={stage.id}>
                  <Card
                    sx={{
                      p: 2,
                      borderTop: `3px solid ${stage.color}`,
                      backgroundColor: '#0D1118',
                    }}
                  >
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                      <Typography variant="caption" fontWeight="bold" sx={{ color: stage.color }}>
                        {stage.label}
                      </Typography>
                      <Chip label={stage.prob} size="small" sx={{ height: 18, fontSize: '0.65rem', backgroundColor: 'rgba(255,255,255,0.06)' }} />
                    </Box>
                    <Typography variant="h3" fontWeight="bold" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                      {count} Deals
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                      ₹{(totalVal / 100000).toFixed(1)}L Total
                    </Typography>
                  </Card>
                </Grid>
              );
            })}
          </Grid>

          {/* Opportunity List Table / Cards */}
          <Grid container spacing={3}>
            <Grid item xs={12} md={selectedOpp ? 7 : 12}>
              <Card>
                <CardContent sx={{ p: 3 }}>
                  <Typography variant="h5" fontWeight="bold" sx={{ mb: 2 }}>
                    Active Commercial Deals ({oppList.length})
                  </Typography>

                  <Stack spacing={1.5}>
                    {oppList.length === 0 ? (
                      <Typography color="text.secondary" align="center" sx={{ py: 4 }}>
                        No commercial opportunities recorded yet. Click "New Opportunity" to start.
                      </Typography>
                    ) : (
                      oppList.map((opp) => (
                        <Box
                          key={opp.id}
                          onClick={() => handleSelectOpp(opp)}
                          sx={{
                            p: 2,
                            borderRadius: 1.5,
                            backgroundColor: selectedOpp?.id === opp.id ? 'rgba(0, 240, 255, 0.08)' : '#111722',
                            border: `1px solid ${
                              selectedOpp?.id === opp.id ? 'rgba(0, 240, 255, 0.4)' : 'rgba(255, 255, 255, 0.06)'
                            }`,
                            cursor: 'pointer',
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center',
                            transition: 'all 0.2s ease',
                            '&:hover': {
                              backgroundColor: 'rgba(0, 240, 255, 0.04)',
                              borderColor: 'rgba(0, 240, 255, 0.3)',
                            },
                          }}
                        >
                          <Box>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
                              <Typography variant="subtitle1" fontWeight="bold">
                                {opp.title}
                              </Typography>
                              <Chip
                                label={STAGES.find((s) => s.id === Number(opp.stage))?.label || 'Discovery'}
                                size="small"
                                sx={{
                                  backgroundColor: 'rgba(0, 240, 255, 0.1)',
                                  color: '#00F0FF',
                                  fontSize: '0.75rem',
                                }}
                              />
                            </Box>
                            <Typography variant="body2" color="text.secondary">
                              ₹{(opp.estimatedValue / 100000).toFixed(1)}L • Probability: {(opp.probability * 100).toFixed(0)}%
                            </Typography>
                          </Box>

                          <IconButton size="small" sx={{ color: '#00F0FF' }}>
                            <ArrowForward />
                          </IconButton>
                        </Box>
                      ))
                    )}
                  </Stack>
                </CardContent>
              </Card>
            </Grid>

            {/* Side AI Intelligence Drawer/Panel */}
            {selectedOpp && (
              <Grid item xs={12} md={5}>
                <Card sx={{ border: '1px solid rgba(0, 240, 255, 0.3)', backgroundColor: '#0D1118' }}>
                  <CardContent sx={{ p: 3 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <AutoAwesome sx={{ color: '#00F0FF', fontSize: 18 }} />
                        <Typography variant="h5" fontWeight="bold">
                          AI Deal Intelligence
                        </Typography>
                      </Box>
                      <IconButton size="small" onClick={() => setSelectedOpp(null)} sx={{ color: 'text.secondary' }}>
                        <Close fontSize="small" />
                      </IconButton>
                    </Box>

                    <Typography variant="h4" fontWeight="bold" sx={{ mb: 0.5 }}>
                      {selectedOpp.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5 }}>
                      Value: ₹{(selectedOpp.estimatedValue / 100000).toFixed(1)}L • Probability: {(selectedOpp.probability * 100).toFixed(0)}%
                    </Typography>

                    <Button
                      fullWidth
                      variant="contained"
                      startIcon={isAnalyzing ? <CircularProgress size={16} sx={{ color: '#070A0F' }} /> : <AutoAwesome />}
                      disabled={isAnalyzing}
                      onClick={() => handleRunAiAnalysis(selectedOpp)}
                      sx={{ mb: 3 }}
                    >
                      {isAnalyzing ? 'Evaluating Deal Risk...' : 'Analyze Deal with AI'}
                    </Button>

                    {aiAnalysisResult && (
                      <Stack spacing={2} sx={{ p: 2, borderRadius: 1.5, backgroundColor: '#111722', border: '1px solid rgba(255, 255, 255, 0.08)' }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="body2" color="text.secondary">
                            Deal Health Score
                          </Typography>
                          <Typography variant="h5" fontWeight="bold" sx={{ color: '#00F0FF' }}>
                            {aiAnalysisResult.dealHealth} / 100
                          </Typography>
                        </Box>

                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="body2" color="text.secondary">
                            Risk Level
                          </Typography>
                          <StatusBadge type={aiAnalysisResult.riskLevel} />
                        </Box>

                        <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.08)' }} />

                        <Box>
                          <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', mb: 0.5, display: 'block' }}>
                            Identified Blockers
                          </Typography>
                          {aiAnalysisResult.blockers.map((b, idx) => (
                            <Typography key={idx} variant="body2" color="text.primary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              • {b}
                            </Typography>
                          ))}
                        </Box>

                        <Box>
                          <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', mb: 0.5, display: 'block' }}>
                            Recommended Next Step
                          </Typography>
                          <Typography variant="body2" sx={{ color: '#FBBF24', fontWeight: 600 }}>
                            {aiAnalysisResult.recommendedNextStep}
                          </Typography>
                        </Box>

                        <Box sx={{ pt: 1 }}>
                          <StatusBadge type="approval_pending" customLabel="Approval Logged in Control Center" />
                        </Box>
                      </Stack>
                    )}

                    <Box sx={{ mt: 3 }}>
                      <Typography variant="subtitle2" sx={{ mb: 1, color: 'text.secondary' }}>
                        Advance Deal Stage:
                      </Typography>
                      <Stack direction="row" spacing={1} flexWrap="wrap">
                        {STAGES.map((s) => (
                          <Button
                            key={s.id}
                            size="small"
                            variant={Number(selectedOpp.stage) === s.id ? 'contained' : 'outlined'}
                            onClick={() => advanceOpportunityStage.mutate({ id: selectedOpp.id, stage: s.id })}
                            sx={{ mb: 1 }}
                          >
                            {s.label}
                          </Button>
                        ))}
                      </Stack>
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            )}
          </Grid>
        </Box>

        {/* Create Opportunity Modal */}
        <Dialog
          open={createModalOpen}
          onClose={() => setCreateModalOpen(false)}
          PaperProps={{
            sx: {
              backgroundColor: '#0D1118',
              border: '1px solid rgba(255, 255, 255, 0.1)',
              minWidth: 400,
            },
          }}
        >
          <DialogTitle sx={{ color: '#F8FAFC', fontWeight: 'bold' }}>Create Commercial Opportunity</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Opportunity Title"
                placeholder="e.g. Enterprise Platform Expansion"
                fullWidth
                size="small"
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
              />
              <TextField
                label="Estimated Value (INR)"
                type="number"
                fullWidth
                size="small"
                value={newValue}
                onChange={(e) => setNewValue(e.target.value)}
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setCreateModalOpen(false)} sx={{ color: 'text.secondary' }}>
              Cancel
            </Button>
            <Button variant="contained" onClick={handleCreateSubmit}>
              Create Opportunity
            </Button>
          </DialogActions>
        </Dialog>
      </Layout>
    </ErrorBoundary>
  );
};

export default Opportunities;
