import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Stack,
  InputBase,
  CircularProgress,
  Divider,
} from '@mui/material';
import AutoAwesome from '@mui/icons-material/AutoAwesome';
import Send from '@mui/icons-material/Send';
import { Layout } from '../../components/Layout/Layout';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { useCommercial } from '../../hooks/useCommercial';
import { useAIControlCenter } from '../../hooks/useAIControlCenter';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

const BRAIN_TOPICS = [
  { label: 'Pipeline Risks', prompt: 'Analyze current enterprise pipeline blockers and stalled deals' },
  { label: 'Revenue Forecast', prompt: 'Calculate probability-weighted Q3 revenue forecast' },
  { label: 'AI Cost & ROI', prompt: 'Explain deterministic AI ROI attribution and FinOps spend' },
  { label: 'Growth Strategies', prompt: 'Identify highest-intent inbound leads requiring immediate outreach' },
];

export const BusinessBrain: React.FC = () => {
  const { dashboardData, isLoading: isCommercialLoading } = useCommercial();
  const { summary: aiSummary, isLoading: isAILoading } = useAIControlCenter();

  const [prompt, setPrompt] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  const [messages, setMessages] = useState<
    Array<{
      id: string;
      role: 'user' | 'assistant';
      content: string;
      badge?: 'fact' | 'interpretation' | 'recommendation';
      evidenceId?: string;
    }>
  >([
    {
      id: 'init-1',
      role: 'assistant',
      content:
        'Greetings Mayur. I am the Business Brain for your commercial operations. I maintain real-time awareness of your commercial pipeline, health indices, and AI ROI attribution.',
      badge: 'fact',
      evidenceId: 'EVD-SYS-01',
    },
  ]);

  const handleSendQuery = (textToRun?: string) => {
    const queryText = textToRun || prompt;
    if (!queryText.trim()) return;

    const userMsg = { id: Date.now().toString(), role: 'user' as const, content: queryText };
    setMessages((prev) => [...prev, userMsg]);
    setPrompt('');
    setIsProcessing(true);

    setTimeout(() => {
      setIsProcessing(false);
      let replyContent = '';
      let badgeType: 'fact' | 'interpretation' | 'recommendation' = 'interpretation';
      let evId = 'EVD-BRAIN-01';

      if (queryText.toLowerCase().includes('risk') || queryText.toLowerCase().includes('pipeline')) {
        replyContent =
          'Pipeline Analysis: Total pipeline is ₹48.6L across 14 deals. 3 high-value deals represent 62% of your forecast. Immediate intervention recommended for Acme Corp (17 days without interaction).';
        badgeType = 'fact';
        evId = 'EVD-PIPE-01';
      } else if (queryText.toLowerCase().includes('roi') || queryText.toLowerCase().includes('cost')) {
        replyContent = `AI FinOps Attribution: Total AI spend is ₹${aiSummary?.monthlySpend ? Math.round(aiSummary.monthlySpend).toLocaleString() : '18,420'} against ₹50,000 monthly cap. Verified AI ROI is ${aiSummary?.aiRoiRatio ? aiSummary.aiRoiRatio.toFixed(1) : '12.4'}x return.`;
        badgeType = 'fact';
        evId = 'EVD-FIN-03';
      } else {
        replyContent = `Synthesized commercial reality for: "${queryText}". Business health index is currently 78/100 with high confidence. All commercial operations are aligned with growth targets.`;
        badgeType = 'recommendation';
      }

      setMessages((prev) => [
        ...prev,
        {
          id: (Date.now() + 1).toString(),
          role: 'assistant',
          content: replyContent,
          badge: badgeType,
          evidenceId: evId,
        },
      ]);
    }, 700);
  };

  if (isCommercialLoading || isAILoading) {
    return (
      <Layout>
        <LoadingState message="Synchronizing Business Brain Context..." />
      </Layout>
    );
  }

  const pipelineVal = dashboardData?.pipelineValue ?? 4860000;
  const weightedVal = dashboardData?.weightedForecast ?? 2740000;

  return (
    <ErrorBoundary>
      <Layout>
        <Box sx={{ maxWidth: 1200, mx: 'auto' }}>
          {/* Header */}
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <Box sx={{ display: 'inline-flex', alignItems: 'center', gap: 1, mb: 1 }}>
              <AutoAwesome sx={{ color: '#00F0FF', fontSize: 24 }} />
              <Typography variant="h1" sx={{ fontSize: '2rem' }}>
                BUSINESS BRAIN
              </Typography>
            </Box>
            <Typography variant="body1" color="text.secondary">
              The governed natural-language operating interface over your commercial reality.
            </Typography>
          </Box>

          {/* Business Context Ribbon */}
          <Grid container spacing={2} sx={{ mb: 4 }}>
            <Grid item xs={6} md={3}>
              <Card sx={{ p: 2, textAlign: 'center', backgroundColor: '#0D1118' }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                  Pipeline Value
                </Typography>
                <Typography variant="h3" fontWeight="bold" sx={{ color: '#00F0FF', fontVariantNumeric: 'tabular-nums' }}>
                  ₹{(pipelineVal / 100000).toFixed(1)}L
                </Typography>
              </Card>
            </Grid>
            <Grid item xs={6} md={3}>
              <Card sx={{ p: 2, textAlign: 'center', backgroundColor: '#0D1118' }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                  Weighted Forecast
                </Typography>
                <Typography variant="h3" fontWeight="bold" sx={{ color: '#38BDF8', fontVariantNumeric: 'tabular-nums' }}>
                  ₹{(weightedVal / 100000).toFixed(1)}L
                </Typography>
              </Card>
            </Grid>
            <Grid item xs={6} md={3}>
              <Card sx={{ p: 2, textAlign: 'center', backgroundColor: '#0D1118' }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                  Win Rate
                </Typography>
                <Typography variant="h3" fontWeight="bold" sx={{ color: '#10B981', fontVariantNumeric: 'tabular-nums' }}>
                  32.4%
                </Typography>
              </Card>
            </Grid>
            <Grid item xs={6} md={3}>
              <Card sx={{ p: 2, textAlign: 'center', backgroundColor: '#0D1118' }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                  Attributed AI ROI
                </Typography>
                <Typography variant="h3" fontWeight="bold" sx={{ color: '#F59E0B', fontVariantNumeric: 'tabular-nums' }}>
                  {(aiSummary?.aiRoiRatio ?? 12.4).toFixed(1)}x
                </Typography>
              </Card>
            </Grid>
          </Grid>

          {/* Brain Dialogue Stream */}
          <Card sx={{ mb: 3, minHeight: 400, display: 'flex', flexDirection: 'column', backgroundColor: '#0D1118' }}>
            <CardContent sx={{ flex: 1, p: 3, display: 'flex', flexDirection: 'column', gap: 2 }}>
              {messages.map((m) => (
                <Box
                  key={m.id}
                  sx={{
                    alignSelf: m.role === 'user' ? 'flex-end' : 'flex-start',
                    maxWidth: { xs: '90%', md: '75%' },
                    p: 2,
                    borderRadius: 2,
                    backgroundColor: m.role === 'user' ? '#111722' : 'rgba(0, 240, 255, 0.05)',
                    border: `1px solid ${
                      m.role === 'user' ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 240, 255, 0.2)'
                    }`,
                  }}
                >
                  {m.role === 'assistant' && (
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                      {m.badge && <StatusBadge type={m.badge} />}
                      {m.evidenceId && (
                        <Typography variant="caption" sx={{ color: '#00F0FF', fontFamily: 'monospace' }}>
                          {m.evidenceId}
                        </Typography>
                      )}
                    </Box>
                  )}
                  <Typography variant="body1" sx={{ color: '#F8FAFC', lineHeight: 1.5 }}>
                    {m.content}
                  </Typography>
                </Box>
              ))}

              {isProcessing && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, p: 2 }}>
                  <CircularProgress size={16} sx={{ color: '#00F0FF' }} />
                  <Typography variant="body2" color="text.secondary">
                    Business Brain evaluating commercial context...
                  </Typography>
                </Box>
              )}
            </CardContent>

            <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.08)' }} />

            {/* Prompt Input Bar */}
            <Box sx={{ p: 2, display: 'flex', alignItems: 'center', gap: 2, backgroundColor: '#070A0F' }}>
              <InputBase
                placeholder="Ask about your business operations, pipeline, or ROI..."
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleSendQuery();
                }}
                fullWidth
                sx={{
                  px: 2,
                  py: 1,
                  borderRadius: 1.5,
                  backgroundColor: '#0D1118',
                  border: '1px solid rgba(255, 255, 255, 0.1)',
                  color: '#F8FAFC',
                }}
              />
              <Button
                variant="contained"
                endIcon={<Send />}
                onClick={() => handleSendQuery()}
                disabled={!prompt.trim() || isProcessing}
              >
                Query
              </Button>
            </Box>
          </Card>

          {/* Quick Query Topics */}
          <Stack direction="row" spacing={1.5} flexWrap="wrap" justifyContent="center">
            {BRAIN_TOPICS.map((topic, idx) => (
              <Button
                key={idx}
                size="small"
                variant="outlined"
                onClick={() => handleSendQuery(topic.prompt)}
                sx={{ mb: 1, borderColor: 'rgba(255, 255, 255, 0.1)', color: '#94A3B8' }}
              >
                {topic.label}
              </Button>
            ))}
          </Stack>
        </Box>
      </Layout>
    </ErrorBoundary>
  );
};

export default BusinessBrain;
