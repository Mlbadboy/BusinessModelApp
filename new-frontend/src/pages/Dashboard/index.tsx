import { Grid, Paper, Typography, Box } from '@mui/material';
import { useAnalytics } from '../../hooks/useAnalytics';
import { useRevenue } from '../../hooks/useRevenue';
import { useExpenses } from '../../hooks/useExpenses';
import { useStrategy } from '../../hooks/useStrategy';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

// Helper function to format currency
const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount);
};

// Helper function to format percentage
const formatPercentage = (value: number) => {
  return `${(value * 100).toFixed(1)}%`;
};

const MetricCard = ({
  title,
  value,
  trend,
  color = 'primary.main',
}: {
  title: string;
  value: string;
  trend?: string;
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
    <Typography variant="h4" component="div" color={color} sx={{ mb: 1 }}>
      {value}
    </Typography>
    {trend && (
      <Typography variant="body2" color="text.secondary">
        {trend}
      </Typography>
    )}
  </Paper>
);

const Dashboard = () => {
  const { financialPerformance, businessHealth, kpis, isLoading: isLoadingAnalytics } = useAnalytics();
  const { revenueTrends, isLoading: isLoadingRevenue } = useRevenue();
  const { trends: expenseTrends, isLoading: isLoadingExpenses } = useExpenses();
  const { performanceTrends, isLoading: isLoadingStrategy } = useStrategy();

  if (isLoadingAnalytics || isLoadingRevenue || isLoadingExpenses || isLoadingStrategy) {
    return <LoadingState message="Loading dashboard data..." />;
  }

  // Default values for metrics
  const revenue = financialPerformance?.revenue || 0;
  const expenses = financialPerformance?.expenses || 0;
  const profit = financialPerformance?.profit || 0;
  const profitMargin = financialPerformance?.profitMargin || 0;
  const revenueGrowth = financialPerformance?.trends?.revenueGrowth || 0;
  const expenseGrowth = financialPerformance?.trends?.expenseGrowth || 0;
  const profitGrowth = financialPerformance?.trends?.profitGrowth || 0;

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Business Overview
      </Typography>

      <Grid container spacing={3}>
        {/* Financial Performance */}
        <Grid item xs={12} md={6} lg={3}>
          <MetricCard
            title="Revenue"
            value={formatCurrency(revenue)}
            trend={`${formatPercentage(revenueGrowth)} vs last period`}
            color={revenueGrowth > 0 ? 'success.main' : 'error.main'}
          />
        </Grid>

        <Grid item xs={12} md={6} lg={3}>
          <MetricCard
            title="Expenses"
            value={formatCurrency(expenses)}
            trend={`${formatPercentage(expenseGrowth)} vs last period`}
            color={expenseGrowth < 0 ? 'success.main' : 'error.main'}
          />
        </Grid>

        <Grid item xs={12} md={6} lg={3}>
          <MetricCard
            title="Net Profit"
            value={formatCurrency(profit)}
            trend={`${formatPercentage(profitGrowth)} vs last period`}
            color={profitGrowth > 0 ? 'success.main' : 'error.main'}
          />
        </Grid>

        <Grid item xs={12} md={6} lg={3}>
          <MetricCard
            title="Profit Margin"
            value={formatPercentage(profitMargin)}
            color={profitMargin > 0.2 ? 'success.main' : 'warning.main'}
          />
        </Grid>

        {/* Business Health Metrics */}
        <Grid item xs={12} md={6} lg={4}>
          <MetricCard
            title="Cash Flow"
            value={formatCurrency(businessHealth?.cashflow?.current || 0)}
            trend={`Projected: ${formatCurrency(businessHealth?.cashflow?.projected || 0)}`}
            color={businessHealth?.cashflow?.trend === 'up' ? 'success.main' : 'warning.main'}
          />
        </Grid>

        <Grid item xs={12} md={6} lg={4}>
          <MetricCard
            title="Customer Lifetime Value"
            value={formatCurrency(businessHealth?.customerMetrics?.lifetimeValue || 0)}
            trend={`Retention Rate: ${formatPercentage(
              businessHealth?.customerMetrics?.retentionRate || 0
            )}`}
          />
        </Grid>

        <Grid item xs={12} md={6} lg={4}>
          <MetricCard
            title="Operational Efficiency"
            value={`${(businessHealth?.operationalEfficiency?.processEfficiency || 0).toFixed(1)}%`}
            trend={`Productivity Score: ${(
              businessHealth?.operationalEfficiency?.productivityScore || 0
            ).toFixed(1)}`}
          />
        </Grid>
      </Grid>
    </Box>
  );
};

// Wrap with ErrorBoundary
export default function DashboardWithErrorBoundary() {
  return (
    <ErrorBoundary>
      <Dashboard />
    </ErrorBoundary>
  );
}