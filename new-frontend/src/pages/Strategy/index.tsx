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
  Chip,
  Button,
  LinearProgress,
} from '@mui/material';
import { useStrategy } from '../../hooks/useStrategy';
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
      id={`strategy-tabpanel-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
};

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount);
};

const Strategy = () => {
  const [tabValue, setTabValue] = useState(0);
  const {
    performanceTrends,
    risks,
    opportunities,
    updateRiskMitigation,
    updateOpportunityStatus,
    calculateRiskScore,
    isLoading,
    error,
  } = useStrategy();

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  if (isLoading) {
    return <LoadingState message="Loading strategy data..." />;
  }

  if (error) {
    throw error;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Strategy Management
      </Typography>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange}>
          <Tab label="Performance Trends" />
          <Tab label="Strategic Opportunities" />
          <Tab label="Strategic Risks" />
        </Tabs>
      </Box>

      <TabPanel value={tabValue} index={0}>
        <Grid container spacing={3}>
          {performanceTrends?.map((trend) => (
            <Grid item xs={12} md={6} key={trend.id}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {trend.metric}
                  </Typography>
                  <Box sx={{ mb: 3 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="body2">Current: {trend.current}</Typography>
                      <Typography variant="body2">Target: {trend.target}</Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={(trend.current / trend.target) * 100}
                      color={trend.status === 'ahead' ? 'success' : 'warning'}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                  </Box>
                  <Chip
                    label={trend.status}
                    color={
                      trend.status === 'ahead'
                        ? 'success'
                        : trend.status === 'on-track'
                        ? 'primary'
                        : 'error'
                    }
                    size="small"
                    sx={{ mb: 2 }}
                  />
                  <Typography variant="body2" color="text.secondary">
                    {trend.analysis}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Grid container spacing={3}>
          {opportunities?.map((opportunity) => (
            <Grid item xs={12} md={6} key={opportunity.id}>
              <Paper sx={{ p: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                  <Typography variant="h6">{opportunity.name}</Typography>
                  <Chip label={opportunity.category} color="primary" />
                </Box>
                <Typography variant="body2" paragraph>
                  {opportunity.description}
                </Typography>
                <Box sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Impact
                  </Typography>
                  <Typography variant="body2" color="text.secondary" paragraph>
                    {opportunity.impact.benefit}
                  </Typography>
                  <Box sx={{ display: 'flex', gap: 1 }}>
                    <Chip
                      label={`Potential Gain: ${formatCurrency(opportunity.impact.potentialGain)}`}
                      color="success"
                    />
                    <Chip label={`Time to Realize: ${opportunity.impact.timeToRealize}`} />
                  </Box>
                </Box>
                <Box sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Requirements
                  </Typography>
                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    {opportunity.requirements.resources.map((resource) => (
                      <Chip key={resource} label={resource} size="small" />
                    ))}
                  </Box>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    Investment Required: {formatCurrency(opportunity.requirements.investment)}
                  </Typography>
                </Box>
                {opportunity.status !== 'realized' && (
                  <Button
                    variant="contained"
                    color="primary"
                    onClick={() =>
                      updateOpportunityStatus.mutate({
                        id: opportunity.id,
                        status:
                          opportunity.status === 'identified'
                            ? 'evaluating'
                            : opportunity.status === 'evaluating'
                            ? 'pursuing'
                            : 'realized',
                      })
                    }
                  >
                    {opportunity.status === 'identified'
                      ? 'Start Evaluation'
                      : opportunity.status === 'evaluating'
                      ? 'Begin Pursuit'
                      : 'Mark as Realized'}
                  </Button>
                )}
              </Paper>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Grid container spacing={3}>
          {risks?.map((risk) => (
            <Grid item xs={12} md={6} key={risk.id}>
              <Paper sx={{ p: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                  <Typography variant="h6">{risk.name}</Typography>
                  <Chip label={risk.category} color="primary" />
                </Box>
                <Typography variant="body2" paragraph>
                  {risk.description}
                </Typography>
                <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                  <Chip
                    label={`Impact: ${risk.impact.severity}`}
                    color={
                      risk.impact.severity === 'high'
                        ? 'error'
                        : risk.impact.severity === 'medium'
                        ? 'warning'
                        : 'info'
                    }
                  />
                  <Chip
                    label={`Probability: ${(risk.probability * 100).toFixed(0)}%`}
                    color="primary"
                  />
                  <Chip
                    label={`Risk Score: ${calculateRiskScore(risk)}`}
                    color="secondary"
                  />
                </Box>
                <Typography variant="body2" color="error" gutterBottom>
                  Potential Loss: {formatCurrency(risk.impact.potentialLoss)}
                </Typography>
                <Box sx={{ mt: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Mitigation Plan
                  </Typography>
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                    {risk.mitigationPlan.steps.map((step, index) => (
                      <Typography key={index} variant="body2" color="text.secondary">
                        {index + 1}. {step}
                      </Typography>
                    ))}
                  </Box>
                  <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="body2">
                      Cost: {formatCurrency(risk.mitigationPlan.cost)}
                    </Typography>
                    <Typography variant="body2">
                      Timeframe: {risk.mitigationPlan.timeframe}
                    </Typography>
                  </Box>
                </Box>
                {risk.mitigationPlan.status !== 'completed' && (
                  <Button
                    variant="contained"
                    color="primary"
                    sx={{ mt: 2 }}
                    onClick={() =>
                      updateRiskMitigation.mutate({
                        id: risk.id,
                        status:
                          risk.mitigationPlan.status === 'planned'
                            ? 'in-progress'
                            : 'completed',
                      })
                    }
                  >
                    {risk.mitigationPlan.status === 'planned'
                      ? 'Start Mitigation'
                      : 'Complete Mitigation'}
                  </Button>
                )}
              </Paper>
            </Grid>
          ))}
        </Grid>
      </TabPanel>
    </Box>
  );
};

// Wrap with ErrorBoundary
export default function StrategyWithErrorBoundary() {
  return (
    <ErrorBoundary>
      <Strategy />
    </ErrorBoundary>
  );
}
