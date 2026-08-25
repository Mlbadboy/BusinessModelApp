import { Grid, Paper, Typography, Box, CircularProgress } from '@mui/material';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import AttachMoneyIcon from '@mui/icons-material/AttachMoney';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import TimelineIcon from '@mui/icons-material/Timeline';
import { FinancialMetricsCard } from './FinancialMetricsCard';
import { useFinancialPerformance } from '../../api/hooks';

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    notation: 'compact',
    maximumFractionDigits: 1,
  }).format(value);
}

function formatPercentage(value: number): string {
  return `${value.toFixed(1)}%`;
}

export function FinancialPerformance() {
  const { data, isLoading, error } = useFinancialPerformance();

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={3}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !data) {
    return (
      <Box p={3}>
        <Typography color="error">
          Failed to load financial performance data
        </Typography>
      </Box>
    );
  }

  const performance = data.data;
  
  // Prepare chart data
  const chartData = [
    { name: 'Revenue', value: performance.revenue },
    { name: 'Expenses', value: performance.expenses },
    { name: 'Net Profit', value: performance.netProfit },
  ];

  return (
    <Box p={3}>
      <Typography variant="h5" gutterBottom>
        Financial Performance
      </Typography>

      <Grid container spacing={3}>
        {/* Key Metrics */}
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Revenue"
            value={{
              value: performance.revenue,
              unit: 'currency',
              change: performance.yearOverYearGrowth,
              trend: performance.yearOverYearGrowth > 0 ? 'up' : 'down',
            }}
            icon={<AttachMoneyIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Net Profit"
            value={{
              value: performance.netProfit,
              unit: 'currency',
              change: performance.quarterOverQuarterGrowth,
              trend: performance.quarterOverQuarterGrowth > 0 ? 'up' : 'down',
            }}
            icon={<TrendingUpIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Cash Flow"
            value={{
              value: performance.cashFlow,
              unit: 'currency',
              trend: performance.cashFlow > 0 ? 'up' : 'down',
            }}
            icon={<AccountBalanceIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="ROI"
            value={{
              value: performance.roi * 100,
              unit: '%',
              trend: performance.roi > 0.15 ? 'up' : 'down',
            }}
            icon={<TimelineIcon />}
          />
        </Grid>

        {/* Financial Overview Chart */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Financial Overview
            </Typography>
            <Box height={400}>
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart
                  data={chartData}
                  margin={{ top: 10, right: 30, left: 0, bottom: 0 }}
                >
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" />
                  <YAxis
                    tickFormatter={formatCurrency}
                  />
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                  />
                  <Area
                    type="monotone"
                    dataKey="value"
                    stroke="#8884d8"
                    fill="#8884d8"
                    fillOpacity={0.3}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </Box>
          </Paper>
        </Grid>

        {/* Margin Analysis */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Margin Analysis
            </Typography>
            <Grid container spacing={3}>
              <Grid item xs={12} md={4}>
                <Box mb={2}>
                  <Typography variant="subtitle2" color="textSecondary">
                    Gross Margin
                  </Typography>
                  <Typography variant="h4">
                    {formatPercentage(performance.grossMargin)}
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} md={4}>
                <Box mb={2}>
                  <Typography variant="subtitle2" color="textSecondary">
                    Operating Margin
                  </Typography>
                  <Typography variant="h4">
                    {formatPercentage(performance.operatingMargin)}
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={12} md={4}>
                <Box mb={2}>
                  <Typography variant="subtitle2" color="textSecondary">
                    Profit Margin
                  </Typography>
                  <Typography variant="h4">
                    {formatPercentage(performance.profitMargin)}
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
}