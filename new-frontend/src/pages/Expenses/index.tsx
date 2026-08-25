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
  Button,
} from '@mui/material';
import { useExpenses } from '../../hooks/useExpenses';
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
      id={`expenses-tabpanel-${index}`}
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

const Expenses = () => {
  const [tabValue, setTabValue] = useState(0);
  const {
    categories,
    trends,
    optimizations,
    risks,
    updateOptimizationStatus,
    isLoading,
    error,
  } = useExpenses();

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  if (isLoading) {
    return <LoadingState message="Loading expense data..." />;
  }

  if (error) {
    throw error;
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Expense Management
      </Typography>

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange}>
          <Tab label="Categories" />
          <Tab label="Trends" />
          <Tab label="Optimizations" />
          <Tab label="Risks" />
        </Tabs>
      </Box>

      <TabPanel value={tabValue} index={0}>
        <Grid container spacing={3}>
          {categories?.map((category) => (
            <Grid item xs={12} md={6} key={category.id}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {category.name}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" paragraph>
                    {category.description}
                  </Typography>
                  <Box sx={{ mb: 2 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="subtitle2">Budget vs Actual</Typography>
                      <Typography variant="subtitle2">
                        {formatCurrency(category.actualSpend)} / {formatCurrency(category.budget)}
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={(category.actualSpend / category.budget) * 100}
                      color={category.variance > 0 ? 'error' : 'success'}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Variance: {category.variance > 0 ? '+' : ''}
                    {formatCurrency(category.variance)}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Expense Trends Overview
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12} md={4}>
                    <Typography variant="subtitle2">Total Expenses</Typography>
                    <Typography variant="h4" color="error">
                      {formatCurrency(trends?.totalExpenses || 0)}
                    </Typography>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Typography variant="subtitle2">Monthly Burn Rate</Typography>
                    <Typography variant="h4">
                      {formatCurrency(trends?.averageMonthlyBurn || 0)}
                    </Typography>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Typography variant="subtitle2">Projected Annual</Typography>
                    <Typography variant="h4">
                      {formatCurrency(trends?.projectedAnnualExpense || 0)}
                    </Typography>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>
          {trends?.byCategory.map((category) => (
            <Grid item xs={12} md={6} key={category.category}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {category.category}
                  </Typography>
                  <Typography variant="h4" color="error">
                    {formatCurrency(category.amount)}
                  </Typography>
                  <Typography
                    variant="body2"
                    color={category.trend > 0 ? 'error.main' : 'success.main'}
                  >
                    {category.trend > 0 ? '+' : ''}
                    {(category.trend * 100).toFixed(1)}% vs previous period
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Grid container spacing={3}>
          {optimizations?.map((optimization) => (
            <Grid item xs={12} md={6} key={optimization.id}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  {optimization.category}
                </Typography>
                <Typography variant="body1" paragraph>
                  {optimization.description}
                </Typography>
                <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                  <Chip
                    label={`Potential Savings: ${formatCurrency(optimization.potentialSavings)}`}
                    color="success"
                  />
                  <Chip label={`ROI: ${optimization.roi}x`} color="primary" />
                  <Chip label={optimization.status} color="secondary" />
                </Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Cost: {formatCurrency(optimization.implementationCost)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Timeline: {optimization.timeToImplement}
                  </Typography>
                </Box>
                {optimization.status !== 'implemented' && (
                  <Button
                    variant="contained"
                    color="primary"
                    sx={{ mt: 2 }}
                    onClick={() =>
                      updateOptimizationStatus.mutate({
                        id: optimization.id,
                        status:
                          optimization.status === 'proposed' ? 'in-progress' : 'implemented',
                      })
                    }
                  >
                    {optimization.status === 'proposed' ? 'Start Implementation' : 'Mark Complete'}
                  </Button>
                )}
              </Paper>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={3}>
        <Grid container spacing={3}>
          {risks?.map((risk) => (
            <Grid item xs={12} md={6} key={risk.id}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  {risk.description}
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
                  Potential Cost: {formatCurrency(risk.potentialCost)}
                </Typography>
                <Typography variant="body1" sx={{ mt: 2 }}>
                  Mitigation Plan:
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {risk.mitigationPlan}
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
export default function ExpensesWithErrorBoundary() {
  return (
    <ErrorBoundary>
      <Expenses />
    </ErrorBoundary>
  );
}
