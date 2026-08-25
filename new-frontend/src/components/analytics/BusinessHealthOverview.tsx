import { Grid, Paper, Typography, Box, LinearProgress } from '@mui/material';
import HealthAndSafetyIcon from '@mui/icons-material/HealthAndSafety';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import GroupIcon from '@mui/icons-material/Group';
import { FinancialMetricsCard } from './FinancialMetricsCard';
import { useBusinessHealth } from '../../api/hooks';
import { RiskLevel, BusinessHealth } from '../../types';

const riskLevelColors: Record<RiskLevel, string> = {
  Low: '#4caf50',
  Moderate: '#ff9800',
  High: '#f44336',
  Critical: '#d32f2f',
};

function HealthScoreIndicator({ score, label }: { score: number; label: string }) {
  return (
    <Box mb={2}>
      <Box display="flex" justifyContent="space-between" mb={0.5}>
        <Typography variant="body2" color="textSecondary">
          {label}
        </Typography>
        <Typography variant="body2" color="textPrimary">
          {score.toFixed(1)}%
        </Typography>
      </Box>
      <LinearProgress
        variant="determinate"
        value={score}
        sx={{
          height: 8,
          borderRadius: 4,
          bgcolor: 'background.paper',
          '& .MuiLinearProgress-bar': {
            borderRadius: 4,
            backgroundColor: score > 66 ? 'success.main' 
              : score > 33 ? 'warning.main' 
              : 'error.main',
          },
        }}
      />
    </Box>
  );
}

export function BusinessHealthOverview() {
  const { data, isLoading, error } = useBusinessHealth();

  if (isLoading) {
    return <Box p={3}><LinearProgress /></Box>;
  }

  if (error || !data) {
    return (
      <Box p={3}>
        <Typography color="error">
          Failed to load business health data
        </Typography>
      </Box>
    );
  }

  const health = data.data;

  return (
    <Box p={3}>
      <Typography variant="h5" gutterBottom>
        Business Health Overview
      </Typography>
      
      <Grid container spacing={3}>
        {/* Key Metrics Cards */}
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Overall Health"
            value={{
              value: health.financialHealthScore,
              unit: '%',
              trend: health.financialHealthScore >= 70 ? 'up' : 'down',
            }}
            icon={<HealthAndSafetyIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Cash Runway"
            value={{
              value: health.cashRunway,
              unit: 'months',
              trend: health.cashRunway > 12 ? 'up' : 'down',
            }}
            subtitle="Months of operation possible"
            icon={<AccountBalanceIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Market Growth"
            value={{
              value: health.marketHealthScore,
              unit: '%',
              change: health.marketSharePercentage,
              trend: health.marketSharePercentage > 0 ? 'up' : 'down',
            }}
            icon={<TrendingUpIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Customer Health"
            value={{
              value: health.customerHealthScore,
              unit: '%',
              change: health.customerRetentionRate - 100,
              trend: health.customerRetentionRate >= 100 ? 'up' : 'down',
            }}
            icon={<GroupIcon />}
          />
        </Grid>

        {/* Detailed Health Scores */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Health Scores Breakdown
            </Typography>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <HealthScoreIndicator 
                  score={health.financialHealthScore}
                  label="Financial Health"
                />
                <HealthScoreIndicator 
                  score={health.operationalHealthScore}
                  label="Operational Health"
                />
                <HealthScoreIndicator 
                  score={health.marketHealthScore}
                  label="Market Health"
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <HealthScoreIndicator 
                  score={health.customerHealthScore}
                  label="Customer Health"
                />
                <HealthScoreIndicator 
                  score={health.growthHealthScore}
                  label="Growth Health"
                />
              </Grid>
            </Grid>
          </Paper>
        </Grid>

        {/* Warnings and Recommendations */}
        {(health.warnings.length > 0 || health.recommendations.length > 0) && (
          <Grid item xs={12}>
            <Paper sx={{ p: 3 }}>
              {health.warnings.length > 0 && (
                <Box mb={3}>
                  <Typography variant="h6" color="error" gutterBottom>
                    Warnings
                  </Typography>
                  {health.warnings.map((warning, index) => (
                    <Typography 
                      key={index}
                      variant="body2" 
                      color="error"
                      sx={{ mt: 1 }}
                    >
                      • {warning}
                    </Typography>
                  ))}
                </Box>
              )}
              
              {health.recommendations.length > 0 && (
                <Box>
                  <Typography variant="h6" color="primary" gutterBottom>
                    Recommendations
                  </Typography>
                  {health.recommendations.map((recommendation, index) => (
                    <Typography 
                      key={index}
                      variant="body2"
                      sx={{ mt: 1 }}
                    >
                      • {recommendation}
                    </Typography>
                  ))}
                </Box>
              )}
            </Paper>
          </Grid>
        )}
      </Grid>
    </Box>
  );
}