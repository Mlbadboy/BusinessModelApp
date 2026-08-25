import { useState } from 'react';
import {
  Grid,
  Paper,
  Typography,
  Box,
  Tabs,
  Tab,
  Card,
  CardContent,
  LinearProgress,
  Chip,
} from '@mui/material';
import { useRevenue } from '../../hooks/useRevenue';
import { ErrorBoundary } from '../../components/ErrorBoundary';
import { LoadingState } from '../../components/LoadingState';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel = (props: TabPanelProps) => {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`revenue-tabpanel-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
};

const Revenue = () => {
  const [tabValue, setTabValue] = useState(0);
  const { revenueTrends, revenueOpportunities, revenueRisks, isLoading, error } = useRevenue();

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  if (isLoading) {
    return <LoadingState message="Loading revenue data..." />;
  }

  if (error) {
    throw error;
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Revenue Management
      </Typography>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange}>
          <Tab label="Revenue Trends" />
          <Tab label="Opportunities" />
          <Tab label="Risks" />
        </Tabs>
      </Box>

      <TabPanel value={tabValue} index={0}>
        <Grid container spacing={3}>
          {revenueTrends?.map((trend) => (
            <Grid item xs={12} md={6} key={trend.period}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {trend.period}
                  </Typography>
                  <Typography variant="h4" color="primary" gutterBottom>
                    {formatCurrency(trend.amount)}
                  </Typography>
                  <Typography color="text.secondary" gutterBottom>
                    Growth: {(trend.growth * 100).toFixed(1)}%
                  </Typography>
                  <Typography variant="subtitle2" gutterBottom>
                    Revenue Sources:
                  </Typography>
                  {trend.sources.map((source) => (
                    <Box key={source.name} sx={{ mt: 1 }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                        <Typography variant="body2">{source.name}</Typography>
                        <Typography variant="body2">
                          {formatCurrency(source.amount)} ({source.percentage.toFixed(1)}%)
                        </Typography>
                      </Box>
                      <LinearProgress
                        variant="determinate"
                        value={source.percentage}
                        sx={{ height: 8, borderRadius: 4 }}
                      />
                    </Box>
                  ))}
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Grid container spacing={3}>
          {revenueOpportunities?.map((opportunity) => (
            <Grid item xs={12} md={6} key={opportunity.id}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  {opportunity.name}
                </Typography>
                <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                  <Chip
                    label={`Potential Gain: ${formatCurrency(opportunity.potentialGain)}`}
                    color="success"
                  />
                  <Chip
                    label={`Probability: ${(opportunity.probability * 100).toFixed(0)}%`}
                    color="primary"
                  />
                </Box>
                <Typography variant="body1" paragraph>
                  {opportunity.strategy}
                </Typography>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Implementation Cost: {formatCurrency(opportunity.implementationCost)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Timeframe: {opportunity.timeframe}
                  </Typography>
                </Box>
              </Paper>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Grid container spacing={3}>
          {revenueRisks?.map((risk) => (
            <Grid item xs={12} md={6} key={risk.id}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  {risk.name}
                </Typography>
                <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                  <Chip
                    label={`Impact: ${risk.impact}`}
                    color={
                      risk.impact === 'high'
                        ? 'error'
                        : risk.impact === 'medium'
                        ? 'warning'
                        : 'info'
                    }
                  />
                  <Chip
                    label={`Probability: ${(risk.probability * 100).toFixed(0)}%`}
                    color="primary"
                  />
                </Box>
                <Typography variant="body2" color="error" gutterBottom>
                  Potential Loss: {formatCurrency(risk.potentialLoss)}
                </Typography>
                <Typography variant="body1" sx={{ mt: 2 }}>
                  Mitigation Strategy:
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {risk.mitigationStrategy}
                </Typography>
              </Paper>
            </Grid>
          ))}
        </Grid>
      </TabPanel>
    </Box>
  );
};

// Wrap with ErrorBoundary
export default function RevenueWithErrorBoundary() {
  return (
    <ErrorBoundary>
      <Revenue />
    </ErrorBoundary>
  );
}