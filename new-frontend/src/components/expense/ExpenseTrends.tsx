import {
  Box,
  Paper,
  Typography,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  CircularProgress,
} from '@mui/material';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  Line,
  ComposedChart,
} from 'recharts';
import { useState } from 'react';
import { useExpenseTrends } from '../../api/hooks';
import { ExpenseTrend } from '../../types';

type PeriodType = 'daily' | 'weekly' | 'monthly' | 'quarterly' | 'yearly';

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

interface TrendDataItem extends ExpenseTrend {
  formattedAmount: string;
  formattedGrowth: string;
}

export function ExpenseTrends() {
  const [period, setPeriod] = useState<PeriodType>('monthly');
  const { data: trends, isLoading, error } = useExpenseTrends(period);

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={3}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !trends) {
    return (
      <Box p={3}>
        <Typography color="error">
          Failed to load expense trends
        </Typography>
      </Box>
    );
  }

  const trendData: TrendDataItem[] = trends.data.map(trend => ({
    ...trend,
    formattedAmount: formatCurrency(trend.amount),
    formattedGrowth: formatPercentage(trend.growthRate),
  }));

  return (
    <Box>
      <Grid container spacing={3}>
        {/* Period Selector */}
        <Grid item xs={12}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6">Expense Trends</Typography>
            <FormControl sx={{ minWidth: 200 }} size="small">
              <InputLabel id="period-select-label">Time Period</InputLabel>
              <Select
                labelId="period-select-label"
                value={period}
                label="Time Period"
                onChange={(e) => setPeriod(e.target.value as PeriodType)}
              >
                <MenuItem value="daily">Daily</MenuItem>
                <MenuItem value="weekly">Weekly</MenuItem>
                <MenuItem value="monthly">Monthly</MenuItem>
                <MenuItem value="quarterly">Quarterly</MenuItem>
                <MenuItem value="yearly">Yearly</MenuItem>
              </Select>
            </FormControl>
          </Box>
        </Grid>

        {/* Expense Trend Charts */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="subtitle1" gutterBottom>
              Expense Amount and Growth Rate
            </Typography>
            <Box height={400}>
              <ResponsiveContainer width="100%" height="100%">
                <ComposedChart
                  data={trendData}
                  margin={{ top: 10, right: 30, left: 0, bottom: 0 }}
                >
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="period" />
                  <YAxis
                    yAxisId="left"
                    tickFormatter={formatCurrency}
                  />
                  <YAxis
                    yAxisId="right"
                    orientation="right"
                    tickFormatter={(value) => `${value}%`}
                  />
                  <Tooltip
                    formatter={(value: any, name: string) => {
                      if (name === 'Amount') return formatCurrency(value as number);
                      if (name === 'Growth Rate') return `${value}%`;
                      return value;
                    }}
                  />
                  <Legend />
                  <Bar
                    yAxisId="left"
                    dataKey="amount"
                    name="Amount"
                    fill="#8884d8"
                  />
                  <Line
                    yAxisId="right"
                    type="monotone"
                    dataKey="growthRate"
                    name="Growth Rate"
                    stroke="#82ca9d"
                    dot={{ r: 4 }}
                  />
                </ComposedChart>
              </ResponsiveContainer>
            </Box>
          </Paper>
        </Grid>

        {/* Trend Details Table */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="subtitle1" gutterBottom>
              Trend Details
            </Typography>
            <Box sx={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr>
                    <th style={{ textAlign: 'left', padding: '8px' }}>Period</th>
                    <th style={{ textAlign: 'right', padding: '8px' }}>Amount</th>
                    <th style={{ textAlign: 'right', padding: '8px' }}>Growth Rate</th>
                    <th style={{ textAlign: 'left', padding: '8px' }}>Trend</th>
                  </tr>
                </thead>
                <tbody>
                  {trendData.map((item, index) => (
                    <tr key={index} style={{ borderBottom: '1px solid rgba(224, 224, 224, 1)' }}>
                      <td style={{ padding: '8px' }}>{item.period}</td>
                      <td style={{ textAlign: 'right', padding: '8px' }}>{item.formattedAmount}</td>
                      <td style={{ textAlign: 'right', padding: '8px' }}>{item.formattedGrowth}</td>
                      <td style={{ padding: '8px' }}>
                        <Typography
                          component="span"
                          color={item.trend === 'increasing' ? 'error.main' 
                            : item.trend === 'decreasing' ? 'success.main' 
                            : 'text.secondary'}
                        >
                          {item.trend.charAt(0).toUpperCase() + item.trend.slice(1)}
                        </Typography>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
}