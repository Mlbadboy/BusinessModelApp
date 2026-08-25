import { Grid, Paper, Typography, Box, Chip, LinearProgress } from '@mui/material';
import { useAnalytics } from '../../hooks/useAnalytics';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

const formatINR = (amount: number) => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  }).format(amount);
};

const formatPercentage = (value: number) => {
  return `${(value * 100).toFixed(1)}%`;
};

const AnalyticsMetric = ({
  title,
  value,
  description,
  color = 'primary.main',
}: {
  title: string;
  value: string;
  description: string;
  color?: string;
}) => (
  <Paper
    elevation={2}
    sx={{
      p: 3,
      height: '100%',
      display: 'flex',
      flexDirection: 'column',
      justifyContent: 'space-between',
    }}
  >
    <Typography variant="h6" color="text.secondary" gutterBottom>
      {title}
    </Typography>
    <Typography variant="h4" component="div" color={color} sx={{ my: 1 }}>
      {value}
    </Typography>
    <Typography variant="body2" color="text.secondary">
      {description}
    </Typography>
  </Paper>
);

const Analytics = () => {
  const { businessHealth, isLoading, error } = useAnalytics();

  if (isLoading) {
    return <LoadingState message="Loading business analytics..." />;
  }

  if (error) {
    throw error;
  }

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <div>
          <Typography variant="h4" fontWeight="bold">
            Deterministic Business Analytics
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Mathematical Engine ({businessHealth?.calculationVersion || 'HealthEngine:v1.0'})
          </Typography>
        </div>
        <Chip
          label={`Confidence: ${businessHealth?.confidenceLevel || 'Medium'} (${((businessHealth?.confidenceScore || 0.6) * 100).toFixed(0)}%)`}
          color={businessHealth?.confidenceLevel === 'High' ? 'success' : 'primary'}
        />
      </Box>

      <Grid container spacing={3}>
        {/* Core Mathematical Indices */}
        <Grid item xs={12}>
          <Typography variant="h5" fontWeight="bold" gutterBottom>
            Core Operational Indices
          </Typography>
        </Grid>
        <Grid item xs={12} md={3}>
          <AnalyticsMetric
            title="Overall Health Score"
            value={`${businessHealth?.overallHealthScore.toFixed(0) || '75'}/100`}
            description={businessHealth?.confidenceReason || 'Verified arithmetic composite'}
            color={businessHealth && businessHealth.overallHealthScore >= 75 ? 'success.main' : 'warning.main'}
          />
        </Grid>
        <Grid item xs={12} md={3}>
          <AnalyticsMetric
            title="Pipeline Coverage Ratio"
            value={`${businessHealth?.pipelineCoverageRatio.toFixed(2) || '0.00'}x`}
            description={`Target: ${formatINR(businessHealth?.quarterlyTarget || 5000000)}`}
          />
        </Grid>
        <Grid item xs={12} md={3}>
          <AnalyticsMetric
            title="Commercial Win Rate"
            value={formatPercentage(businessHealth?.winRate || 0.5)}
            description="Closed Won / Total Resolved Deals"
          />
        </Grid>
        <Grid item xs={12} md={3}>
          <AnalyticsMetric
            title="Deal Cycle Velocity"
            value={`${businessHealth?.avgVelocityDays.toFixed(1) || '0'} Days`}
            description="Average cycle duration from creation"
          />
        </Grid>

        {/* Financial Aggregations */}
        <Grid item xs={12} sx={{ mt: 3 }}>
          <Typography variant="h5" fontWeight="bold" gutterBottom>
            Commercial Revenue Aggregations
          </Typography>
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Total Active Pipeline"
            value={formatINR(businessHealth?.totalPipelineValue || 0)}
            description="Active opportunities value"
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Probability Weighted Forecast"
            value={formatINR(businessHealth?.weightedForecastValue || 0)}
            description="Formula: Σ(EstimatedValue × Probability)"
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Closed Won Recognized Value"
            value={formatINR(businessHealth?.closedWonRevenue || 0)}
            description="Attributed to commercial execution"
            color="success.main"
          />
        </Grid>

        {/* Component Weight Breakdown */}
        <Grid item xs={12} sx={{ mt: 3 }}>
          <Typography variant="h5" fontWeight="bold" gutterBottom>
            Weighted Composite Breakdown
          </Typography>
        </Grid>
        {businessHealth?.componentBreakdown.map((comp, idx) => (
          <Grid item xs={12} md={6} key={idx}>
            <Paper elevation={2} sx={{ p: 2.5 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                <Typography variant="subtitle1" fontWeight="bold">
                  {comp.componentName}
                </Typography>
                <Chip label={`Weight: ${comp.weightPercent}% (${comp.weightedContribution.toFixed(1)} pts)`} size="small" />
              </Box>
              <LinearProgress
                variant="determinate"
                value={Math.min(100, comp.rawScore)}
                sx={{ height: 8, borderRadius: 4, my: 1.5 }}
              />
              <Typography variant="body2" color="text.secondary">
                {comp.explanation}
              </Typography>
            </Paper>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default function AnalyticsWithErrorBoundary() {
  return (
    <ErrorBoundary>
      <Analytics />
    </ErrorBoundary>
  );
}