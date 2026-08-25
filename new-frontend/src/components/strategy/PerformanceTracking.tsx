import { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  CircularProgress,
  Alert,
  Chip,
} from '@mui/material';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import TrendingFlatIcon from '@mui/icons-material/TrendingFlat';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { usePerformanceTrends } from '../../api/hooks/useStrategy';
import { PerformanceTrend } from '../../types/strategy';

interface MetricCardProps {
  trend: PerformanceTrend;
}

function MetricCard({ trend }: MetricCardProps) {
  const getTrendIcon = () => {
    switch (trend.trend) {
      case 'up':
        return <TrendingUpIcon color="success" />;
      case 'down':
        return <TrendingDownIcon color="error" />;
      default:
        return <TrendingFlatIcon color="info" />;
    }
  };

  const getChangeColor = () => {
    if (trend.changePercentage > 0) return 'success.main';
    if (trend.changePercentage < 0) return 'error.main';
    return 'info.main';
  };

  return (
    <Card>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="subtitle1" color="text.secondary">
            {trend.metricName}
          </Typography>
          {getTrendIcon()}
        </Box>
        
        <Box mt={2}>
          <Typography variant="h4">
            {trend.currentValue.toLocaleString()}
          </Typography>
          <Box display="flex" alignItems="center" mt={1}>
            <Typography
              variant="body2"
              sx={{ color: getChangeColor() }}
            >
              {trend.changePercentage > 0 ? '+' : ''}
              {trend.changePercentage.toFixed(1)}%
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ ml: 1 }}>
              vs previous {trend.period}
            </Typography>
          </Box>
        </Box>

        <Box mt={2}>
          <Chip
            label={trend.category}
            size="small"
            variant="outlined"
          />
        </Box>
      </CardContent>
    </Card>
  );
}

interface ChartDataPoint {
  name: string;
  current: number;
  previous: number;
}

export function PerformanceTracking() {
  const [selectedPeriod, setSelectedPeriod] = useState<string>('month');
  const { data: performanceData, isLoading, error } = usePerformanceTrends();

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={3}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !performanceData) {
    return (
      <Box p={3}>
        <Alert severity="error">
          Failed to load performance trends
        </Alert>
      </Box>
    );
  }

  const trends = performanceData.data.data;
  const categories = [...new Set(trends.map((trend: PerformanceTrend) => trend.category))] as string[];
  
  const trendsByCategory = categories.reduce((acc, category) => {
    acc[category] = trends.filter((trend: PerformanceTrend) => trend.category === category);
    return acc;
  }, {} as Record<string, PerformanceTrend[]>);

  const chartData: ChartDataPoint[] = trends.map((trend: PerformanceTrend) => ({
    name: trend.metricName,
    current: trend.currentValue,
    previous: trend.previousValue,
  }));

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5">Performance Tracking</Typography>
        <FormControl sx={{ minWidth: 120 }}>
          <InputLabel>Period</InputLabel>
          <Select
            value={selectedPeriod}
            onChange={(e) => setSelectedPeriod(e.target.value)}
            label="Period"
            size="small"
          >
            <MenuItem value="week">Weekly</MenuItem>
            <MenuItem value="month">Monthly</MenuItem>
            <MenuItem value="quarter">Quarterly</MenuItem>
            <MenuItem value="year">Yearly</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {categories.map((category: string) => (
        <Box key={category} mb={4}>
          <Typography variant="h6" gutterBottom>
            {category}
          </Typography>
          <Grid container spacing={3}>
            {trendsByCategory[category].map((trend: PerformanceTrend) => (
              <Grid item xs={12} sm={6} md={4} key={trend.id}>
                <MetricCard trend={trend} />
              </Grid>
            ))}
          </Grid>
        </Box>
      ))}

      <Card sx={{ mt: 4 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Performance Trends
          </Typography>
          <Box sx={{ height: 400 }}>
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="current"
                  name="Current Period"
                  stroke="#8884d8"
                />
                <Line
                  type="monotone"
                  dataKey="previous"
                  name="Previous Period"
                  stroke="#82ca9d"
                  strokeDasharray="5 5"
                />
              </LineChart>
            </ResponsiveContainer>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}