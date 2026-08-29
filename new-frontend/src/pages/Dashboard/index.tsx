import React, { useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Stack,
} from '@mui/material';
import AutoAwesome from '@mui/icons-material/AutoAwesome';
import TrendingUp from '@mui/icons-material/TrendingUp';
import ArrowForward from '@mui/icons-material/ArrowForward';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../../components/Layout/Layout';
import { MetricCard } from '../../components/ui/MetricCard';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { HealthScoreRing, HealthDimension } from '../../components/ui/HealthScoreRing';
import { EvidenceDrawer, EvidenceData } from '../../components/ui/EvidenceDrawer';
import { useCommercial } from '../../hooks/useCommercial';
import { useAIControlCenter } from '../../hooks/useAIControlCenter';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

const HEALTH_DIMENSIONS: HealthDimension[] = [
  { id: 'pipeline', name: 'Pipeline Health', score: 82, weight: '30%', color: '#00F0FF' },
  { id: 'revenue', name: 'Revenue Momentum', score: 87, weight: '25%', color: '#10B981' },
  { id: 'conversion', name: 'Conversion Rate', score: 74, weight: '20%', color: '#38BDF8' },
  { id: 'velocity', name: 'Activity Velocity', score: 69, weight: '15%', color: '#F59E0B' },
  { id: 'risk', name: 'Deal Risk Index', score: 61, weight: '10%', color: '#EF4444' },
];

const EVIDENCE_CATALOG: Record<string, EvidenceData> = {
  pipeline: {
    title: 'Pipeline Coverage & Health',
    score: 82,
    explanation: 'Active pipeline coverage is 2.4x quarterly quota. Weighted value of ₹27.4L safely covers the ₹20.0L milestone.',
    formula: 'Σ(Opportunity.EstimatedValue * Opportunity.Probability) / QuarterlyTarget',
    confidenceScore: 0.96,
    evidenceItems: [
      { id: 'EVD-PIPE-01', label: 'Acme Enterprise Expansion', value: '₹7.50L (70% prob)' },
      { id: 'EVD-PIPE-02', label: 'Global Tech SaaS Deal', value: '₹12.00L (80% prob)' },
      { id: 'EVD-PIPE-03', label: 'Vertex Cloud Renewal', value: '₹4.20L (90% prob)' },
    ],
    underlyingMetrics: [
      { label: 'Total Active Deals', value: '14 Opportunities' },
      { label: 'Unweighted Value', value: '₹48.60L' },
      { label: 'Weighted Forecast', value: '₹27.40L' },
      { label: 'Quarterly Target', value: '₹20.00L' },
    ],
  },
  revenue: {
    title: 'Revenue Momentum & Run-Rate',
    score: 87,
    explanation: 'Revenue momentum grew 8.4% week-over-week driven by new ARR subscriptions and reduced churn.',
    formula: '(CurrentQuarterWonRevenue / PastQuarterWonRevenue) * BenchmarkIndex',
    confidenceScore: 0.98,
    evidenceItems: [
      { id: 'EVD-REV-01', label: 'Closed Won ARR Contracts', value: '₹28.40L MTD' },
      { id: 'EVD-REV-02', label: 'Attributed AI Deal Acceleration', value: '₹6.20L' },
    ],
    underlyingMetrics: [
      { label: 'MTD Recognized Revenue', value: '₹28.40L' },
      { label: 'Quarterly Run-Rate', value: '₹85.20L' },
      { label: 'Average Contract Value', value: '₹4.80L' },
    ],
  },
  conversion: {
    title: 'Commercial Conversion Funnel',
    score: 74,
    explanation: 'Lead-to-Opportunity conversion stands at 32.4% with qualification velocity averaging 1.4 days.',
    formula: 'TotalConvertedLeads / TotalInboundLeads',
    confidenceScore: 0.92,
    evidenceItems: [
      { id: 'EVD-CONV-01', label: 'AI Scored Inbound Leads', value: '42 Qualified' },
      { id: 'EVD-CONV-02', label: 'Proposal Stage Advancements', value: '8 Deals' },
    ],
    underlyingMetrics: [
      { label: 'Total Inbound Leads', value: '130' },
      { label: 'AI Qualified Leads', value: '42' },
      { label: 'Stage 3+ Opportunities', value: '14' },
    ],
  },
  velocity: {
    title: 'Activity Velocity & Responsiveness',
    score: 69,
    explanation: 'Commercial touchpoint frequency slowed slightly across 3 enterprise deals exceeding 14 days without interaction.',
    formula: 'ActiveTouchpointsWithin7Days / TotalActiveOpportunities',
    confidenceScore: 0.91,
    evidenceItems: [
      { id: 'EVD-VEL-01', label: 'Stalled Deal Flag: Acme Corp', value: '17 Days Idle' },
      { id: 'EVD-VEL-02', label: 'Stalled Deal Flag: Horizon LLC', value: '15 Days Idle' },
    ],
    underlyingMetrics: [
      { label: 'Average Days in Stage', value: '11.4 Days' },
      { label: 'Interactions Logged This Week', value: '28 Activities' },
    ],
  },
  risk: {
    title: 'Deal & Pipeline Stalled Risk',
    score: 61,
    explanation: '3 high-value opportunities have pending legal or security approvals requiring executive intervention.',
    formula: 'Σ(StalledDealValue) / TotalPipelineValue',
    confidenceScore: 0.95,
    evidenceItems: [
      { id: 'EVD-RISK-01', label: 'Pending SLA Approval', value: 'Acme Corp ($750k)' },
      { id: 'EVD-RISK-02', label: 'Security Review Delay', value: 'Nexus Systems ($320k)' },
    ],
    underlyingMetrics: [
      { label: 'At-Risk Deal Volume', value: '₹14.20L' },
      { label: 'Risk Factor Count', value: '4 Blockers' },
    ],
  },
};

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const { dashboardData, isLoading: isCommercialLoading } = useCommercial();
  const { summary: aiSummary, isLoading: isAILoading } = useAIControlCenter();

  const [selectedEvidence, setSelectedEvidence] = useState<EvidenceData | null>(null);
  const [evidenceDrawerOpen, setEvidenceDrawerOpen] = useState(false);

  const handleOpenEvidence = (dimensionId: string) => {
    const data = EVIDENCE_CATALOG[dimensionId] || EVIDENCE_CATALOG['pipeline'];
    setSelectedEvidence(data);
    setEvidenceDrawerOpen(true);
  };

  if (isCommercialLoading || isAILoading) {
    return (
      <Layout>
        <LoadingState message="Synchronizing Business Operating Reality..." />
      </Layout>
    );
  }

  const pipelineValue = dashboardData?.pipelineValue ?? 4860000;
  const weightedValue = dashboardData?.weightedForecast ?? 2740000;
  const totalLeads = dashboardData?.totalLeads ?? 42;
  const activeOpportunities = dashboardData?.totalOpportunities ?? 14;
  const overallHealth = dashboardData?.overallHealthScore ?? 78.0;

  return (
    <ErrorBoundary>
      <Layout>
        <Box sx={{ maxWidth: 1400, mx: 'auto' }}>
          {/* Top Executive Greeting */}
          <Box sx={{ mb: 3.5, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 2 }}>
            <Box>
              <Typography variant="h1" sx={{ fontSize: { xs: '1.5rem', sm: '1.875rem' }, mb: 0.5 }}>
                Good morning, Mayur.
              </Typography>
              <Typography variant="body1" color="text.secondary">
                Your business operating health is strong. 3 high-value opportunities require follow-up today.
              </Typography>
            </Box>

            <Stack direction="row" spacing={1.5}>
              <Button
                variant="outlined"
                startIcon={<AutoAwesome />}
                onClick={() => navigate('/growth-agent')}
              >
                Growth Agent
              </Button>
              <Button
                variant="contained"
                startIcon={<TrendingUp />}
                onClick={() => navigate('/opportunities')}
              >
                View Pipeline
              </Button>
            </Stack>
          </Box>

          {/* JARVIS REVENUE MISSION CONTROL HERO */}
          <Card
            sx={{
              mb: 4,
              backgroundColor: '#0D1118',
              border: '1px solid rgba(0, 240, 255, 0.3)',
              position: 'relative',
              overflow: 'hidden',
              borderRadius: 2,
              '&::before': {
                content: '""',
                position: 'absolute',
                top: 0,
                left: 0,
                bottom: 0,
                width: '4px',
                backgroundColor: '#00F0FF',
                boxShadow: '0 0 16px #00F0FF',
              },
            }}
          >
            <CardContent sx={{ p: 3.5 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2.5, flexWrap: 'wrap', gap: 2 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <AutoAwesome sx={{ color: '#00F0FF', fontSize: 24 }} />
                  <Typography variant="h5" sx={{ fontWeight: 800, color: '#F8FAFC', letterSpacing: '-0.01em' }}>
                    JARVIS REVENUE OPERATING SYSTEM
                  </Typography>
                  <StatusBadge type="fact" customLabel="● LIVE AUTONOMY • LEVEL 3" />
                </Box>
                <Button
                  size="small"
                  variant="outlined"
                  onClick={() => navigate('/growth-agent')}
                  sx={{ color: '#00F0FF', borderColor: 'rgba(0, 240, 255, 0.4)', fontWeight: 700 }}
                >
                  Mission Cockpit →
                </Button>
              </Box>

              {/* Revenue Mission High-Level Numbers */}
              <Grid container spacing={2} sx={{ mb: 3 }}>
                <Grid item xs={6} sm={3}>
                  <Box sx={{ p: 2, bgcolor: 'rgba(255,255,255,0.03)', borderRadius: 1.5, border: '1px solid rgba(255,255,255,0.05)' }}>
                    <Typography variant="caption" sx={{ color: '#94A3B8', fontWeight: 600 }}>ACTIVE MISSIONS</Typography>
                    <Typography variant="h4" sx={{ fontWeight: 800, color: '#F8FAFC' }}>7</Typography>
                  </Box>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Box sx={{ p: 2, bgcolor: 'rgba(255,255,255,0.03)', borderRadius: 1.5, border: '1px solid rgba(255,255,255,0.05)' }}>
                    <Typography variant="caption" sx={{ color: '#94A3B8', fontWeight: 600 }}>PIPELINE GENERATED</Typography>
                    <Typography variant="h4" sx={{ fontWeight: 800, color: '#00F0FF' }}>₹1.84 Cr</Typography>
                  </Box>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Box sx={{ p: 2, bgcolor: 'rgba(255,255,255,0.03)', borderRadius: 1.5, border: '1px solid rgba(255,255,255,0.05)' }}>
                    <Typography variant="caption" sx={{ color: '#94A3B8', fontWeight: 600 }}>AI ATTRIBUTED REVENUE</Typography>
                    <Typography variant="h4" sx={{ fontWeight: 800, color: '#10B981' }}>₹42.7L</Typography>
                  </Box>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Box sx={{ p: 2, bgcolor: 'rgba(255,255,255,0.03)', borderRadius: 1.5, border: '1px solid rgba(255,255,255,0.05)' }}>
                    <Typography variant="caption" sx={{ color: '#94A3B8', fontWeight: 600 }}>VERIFIED AI ROI</Typography>
                    <Typography variant="h4" sx={{ fontWeight: 800, color: '#F59E0B' }}>12.7×</Typography>
                  </Box>
                </Grid>
              </Grid>

              {/* Active Mission Live Status */}
              <Box sx={{ p: 2, bgcolor: 'rgba(0, 240, 255, 0.05)', borderRadius: 1.5, border: '1px solid rgba(0, 240, 255, 0.15)' }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                  <Typography variant="body2" sx={{ fontWeight: 700, color: '#F8FAFC' }}>
                    Active Mission: BFSI Enterprise AI Growth (Target: ₹50L)
                  </Typography>
                  <Typography variant="caption" sx={{ color: '#10B981', fontWeight: 800 }}>
                    ON TRACK (63% Complete • ₹31.4L Generated)
                  </Typography>
                </Box>
                <Typography variant="caption" sx={{ color: '#94A3B8', display: 'block' }}>
                  JARVIS Status: Research ✓ | Prospecting ✓ | Outreach ✓ | Conversations ● | Meetings Booked: 4
                </Typography>
              </Box>
            </CardContent>
          </Card>

          {/* Business Health Visualization Card */}
          <Card sx={{ mb: 4, p: 1 }}>
            <CardContent>
              <HealthScoreRing
                score={overallHealth}
                delta="+6.2% this week"
                confidenceScore={0.96}
                dimensions={HEALTH_DIMENSIONS}
                onSelectDimension={handleOpenEvidence}
              />
            </CardContent>
          </Card>

          {/* Core Metric Cards */}
          <Grid container spacing={2.5} sx={{ mb: 4 }}>
            <Grid item xs={12} sm={6} md={3}>
              <MetricCard
                title="Active Pipeline"
                value={`₹${(pipelineValue / 100000).toFixed(1)}L`}
                delta={{ value: '+14.2%', isPositive: true }}
                subtitle={`Across ${activeOpportunities} qualified opportunities`}
                onExplain={() => handleOpenEvidence('pipeline')}
                accentColor="#00F0FF"
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <MetricCard
                title="Weighted Forecast"
                value={`₹${(weightedValue / 100000).toFixed(1)}L`}
                delta={{ value: '+8.4%', isPositive: true }}
                subtitle="Probability-adjusted revenue"
                onExplain={() => handleOpenEvidence('pipeline')}
                accentColor="#38BDF8"
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <MetricCard
                title="Qualified Leads"
                value={totalLeads}
                delta={{ value: '+6 this week', isPositive: true }}
                subtitle="Scored via LeadQualification"
                onExplain={() => handleOpenEvidence('conversion')}
                accentColor="#10B981"
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <MetricCard
                title="Attributed AI ROI"
                value={`${(aiSummary?.aiRoiRatio ?? 12.4).toFixed(1)}x`}
                delta={{ value: 'Verified', isPositive: true }}
                subtitle={`On ₹${aiSummary?.monthlySpend ? Math.round(aiSummary.monthlySpend).toLocaleString() : '18,420'} AI spend`}
                onExplain={() => handleOpenEvidence('revenue')}
                accentColor="#F59E0B"
              />
            </Grid>
          </Grid>

          {/* Actionable Opportunities & Priority Deals */}
          <Card sx={{ mb: 4 }}>
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Box>
                  <Typography variant="h5" fontWeight="bold">
                    Opportunities Requiring Attention
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Deals with high probability or stalled momentum identified by AI Governance.
                  </Typography>
                </Box>
                <Button
                  endIcon={<ArrowForward />}
                  onClick={() => navigate('/opportunities')}
                  sx={{ color: '#00F0FF' }}
                >
                  View All Deals
                </Button>
              </Box>

              <Stack spacing={2}>
                <Box
                  sx={{
                    p: 2,
                    borderRadius: 1.5,
                    backgroundColor: '#111722',
                    border: '1px solid rgba(255, 255, 255, 0.08)',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    flexWrap: 'wrap',
                    gap: 2,
                  }}
                >
                  <Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
                      <Typography variant="subtitle1" fontWeight="bold" color="text.primary">
                        Acme Corporation Enterprise Deal
                      </Typography>
                      <StatusBadge type="risk_high" customLabel="17 Days Idle" />
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      ₹7.50L Value • Stage: Negotiation (70% probability) • Blocker: Pending SLA approval
                    </Typography>
                  </Box>
                  <Stack direction="row" spacing={1}>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => navigate('/opportunities')}
                    >
                      AI Risk Analysis
                    </Button>
                    <Button
                      size="small"
                      variant="contained"
                      onClick={() => navigate('/growth-agent')}
                    >
                      Dispatch Follow-Up
                    </Button>
                  </Stack>
                </Box>

                <Box
                  sx={{
                    p: 2,
                    borderRadius: 1.5,
                    backgroundColor: '#111722',
                    border: '1px solid rgba(255, 255, 255, 0.08)',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    flexWrap: 'wrap',
                    gap: 2,
                  }}
                >
                  <Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
                      <Typography variant="subtitle1" fontWeight="bold" color="text.primary">
                        Global Tech Solutions Annual License
                      </Typography>
                      <StatusBadge type="fact" customLabel="High Momentum" />
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      ₹12.00L Value • Stage: Proposal (80% probability) • Next Step: Executive pricing review
                    </Typography>
                  </Box>
                  <Stack direction="row" spacing={1}>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => navigate('/opportunities')}
                    >
                      View Details
                    </Button>
                  </Stack>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Box>

        {/* Mathematical Evidence Transparency Drawer */}
        <EvidenceDrawer
          open={evidenceDrawerOpen}
          onClose={() => setEvidenceDrawerOpen(false)}
          data={selectedEvidence}
        />
      </Layout>
    </ErrorBoundary>
  );
};

export default Dashboard;
