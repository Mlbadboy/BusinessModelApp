import { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Tab,
  Tabs,
  CircularProgress,
  Alert,
  Card,
  CardContent,
} from '@mui/material';
import { ExpenseCategoryList } from './ExpenseCategoryList';
import { ExpenseTrends } from './ExpenseTrends';
import { useExpenseAnalysis } from '../../api/hooks';
import { FinancialMetricsCard } from '../analytics/FinancialMetricsCard';
import PaymentsIcon from '@mui/icons-material/Payments';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import SavingsIcon from '@mui/icons-material/Savings';
import AssessmentIcon from '@mui/icons-material/Assessment';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`expense-tabpanel-${index}`}
      aria-labelledby={`expense-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
}

function a11yProps(index: number) {
  return {
    id: `expense-tab-${index}`,
    'aria-controls': `expense-tabpanel-${index}`,
  };
}

export function ExpenseDashboard() {
  const [tabValue, setTabValue] = useState(0);
  const { data: analysis, isLoading, error } = useExpenseAnalysis();

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={3}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !analysis) {
    return (
      <Box p={3}>
        <Alert severity="error">
          Failed to load expense analysis data
        </Alert>
      </Box>
    );
  }

  const { data: expenseData } = analysis;

  return (
    <Box>
      {/* Header */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h4" gutterBottom>
          Expense Management
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Track, analyze, and optimize your business expenses
        </Typography>
      </Paper>

      {/* Key Metrics */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Total Expenses"
            value={{
              value: expenseData.totalExpenses,
              unit: 'currency',
              change: expenseData.yearOverYearGrowth,
              trend: expenseData.yearOverYearGrowth > 0 ? 'down' : 'up',
            }}
            icon={<PaymentsIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Fixed Expenses"
            value={{
              value: expenseData.fixedExpenses,
              unit: 'currency',
              change: (expenseData.fixedExpenses / expenseData.totalExpenses) * 100,
              trend: 'stable',
            }}
            icon={<TrendingDownIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Budget Utilization"
            value={{
              value: expenseData.budgetUtilization,
              unit: '%',
              change: expenseData.forecastAccuracy,
              trend: expenseData.budgetUtilization <= 100 ? 'up' : 'down',
            }}
            icon={<AssessmentIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Potential Savings"
            value={{
              value: expenseData.potentialSavings,
              unit: 'currency',
              trend: 'up',
            }}
            icon={<SavingsIcon />}
          />
        </Grid>
      </Grid>

      {/* Main Content */}
      <Paper>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="expense management tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Expense Categories" {...a11yProps(0)} />
          <Tab label="Expense Trends" {...a11yProps(1)} />
        </Tabs>

        <TabPanel value={tabValue} index={0}>
          <ExpenseCategoryList />
        </TabPanel>
        <TabPanel value={tabValue} index={1}>
          <ExpenseTrends />
        </TabPanel>
      </Paper>

      {/* Optimizations and Risks */}
      <Grid container spacing={3} sx={{ mt: 3 }}>
        {expenseData.optimizations?.length > 0 && (
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                Cost Optimization Opportunities
              </Typography>
              {expenseData.optimizations.map((optimization) => (
                <Card key={optimization.id} sx={{ mb: 2 }}>
                  <CardContent>
                    <Typography variant="subtitle1" gutterBottom>
                      {optimization.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" paragraph>
                      {optimization.description}
                    </Typography>
                    <Box display="flex" justifyContent="space-between">
                      <Typography variant="body2">
                        Potential Savings: {
                          new Intl.NumberFormat('en-US', {
                            style: 'currency',
                            currency: 'USD',
                          }).format(optimization.potentialSavings)
                        }
                      </Typography>
                      <Typography variant="body2">
                        Priority: {optimization.priority.toUpperCase()}
                      </Typography>
                    </Box>
                  </CardContent>
                </Card>
              ))}
            </Paper>
          </Grid>
        )}

        {expenseData.risks?.length > 0 && (
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                Expense Risks
              </Typography>
              {expenseData.risks.map((risk) => (
                <Alert key={risk.id} severity="warning" sx={{ mb: 2 }}>
                  <Typography variant="subtitle2">{risk.name}</Typography>
                  <Typography variant="body2">{risk.description}</Typography>
                  <Typography variant="caption" display="block" sx={{ mt: 0.5 }}>
                    Impact: {risk.impact.toFixed(1)}% | Probability: {risk.probability.toFixed(1)}%
                  </Typography>
                </Alert>
              ))}
            </Paper>
          </Grid>
        )}
      </Grid>
    </Box>
  );
}