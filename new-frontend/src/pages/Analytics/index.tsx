import { Grid, Paper, Typography, Box } from '@mui/material';
import { useAnalytics } from '../../hooks/useAnalytics';
import { ErrorBoundary } from '../../components/ErrorBoundary';
import { LoadingState } from '../../components/LoadingState';

// Helper function to format percentage
const formatPercentage = (value: number) => {
  return `${(value * 100).toFixed(1)}%`;
};

const AnalyticsMetric = ({
  title,
  value,
  description,
}: {
  title: string;
  value: string;
  description?: string;
}) => (
  <Paper
    elevation={2}
    sx={{
      p: 3,
      height: '100%',
      display: 'flex',
      flexDirection: 'column',
      gap: 1,
    }}
  >
    <Typography variant="h6" color="text.secondary">
      {title}
    </Typography>
    <Typography variant="h4">{value}</Typography>
    {description && (
      <Typography variant="body2" color="text.secondary">
        {description}
      </Typography>
    )}
  </Paper>
);

const Analytics = () => {
  const { businessHealth, isLoading, error } = useAnalytics();

  if (isLoading) {
    return <LoadingState message="Loading analytics data..." />;
  }

  if (error) {
    throw error;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Business Analytics
      </Typography>

      <Grid container spacing={3}>
        {/* Customer Metrics */}
        <Grid item xs={12}>
          <Typography variant="h5" gutterBottom>
            Customer Metrics
          </Typography>
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Customer Lifetime Value"
            value={`$${businessHealth?.customerMetrics.lifetimeValue.toFixed(2) || '0'}`}
            description="Average revenue per customer"
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Customer Retention Rate"
            value={formatPercentage(businessHealth?.customerMetrics.retentionRate || 0)}
            description="Percentage of returning customers"
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Customer Acquisition Cost"
            value={`$${businessHealth?.customerMetrics.acquisitionCost.toFixed(2) || '0'}`}
            description="Cost to acquire new customers"
          />
        </Grid>

        {/* Operational Metrics */}
        <Grid item xs={12} sx={{ mt: 4 }}>
          <Typography variant="h5" gutterBottom>
            Operational Metrics
          </Typography>
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Process Efficiency"
            value={formatPercentage(businessHealth?.operationalEfficiency.processEfficiency || 0)}
            description="Overall operational efficiency score"
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Resource Utilization"
            value={formatPercentage(businessHealth?.operationalEfficiency.resourceUtilization || 0)}
            description="Resource usage optimization"
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <AnalyticsMetric
            title="Productivity Score"
            value={`${(businessHealth?.operationalEfficiency.productivityScore || 0).toFixed(1)}`}
            description="Overall productivity rating"
          />
        </Grid>

        {/* Cashflow Analysis */}
        <Grid item xs={12} sx={{ mt: 4 }}>
          <Typography variant="h5" gutterBottom>
            Cashflow Analysis
          </Typography>
        </Grid>
        <Grid item xs={12} md={6}>
          <AnalyticsMetric
            title="Current Cashflow"
            value={`$${businessHealth?.cashflow.current.toFixed(2) || '0'}`}
            description={`Trend: ${businessHealth?.cashflow.trend || 'stable'}`}
          />
        </Grid>
        <Grid item xs={12} md={6}>
          <AnalyticsMetric
            title="Projected Cashflow"
            value={`$${businessHealth?.cashflow.projected.toFixed(2) || '0'}`}
            description="Next quarter projection"
          />
        </Grid>
      </Grid>
    </Box>
  );
};

// Wrap with ErrorBoundary
export default function AnalyticsWithErrorBoundary() {
  return (
    <ErrorBoundary>
      <Analytics />
    </ErrorBoundary>
  );
}