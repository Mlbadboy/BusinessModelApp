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
} from '@mui/material';
import { RevenueSourceList } from './RevenueSourceList';
import { RevenueTrends } from './RevenueTrends';
import { useRevenueAnalysis } from '../../api/hooks';
import { FinancialMetricsCard } from '../analytics/FinancialMetricsCard';
import MonetizationOnIcon from '@mui/icons-material/MonetizationOn';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import PieChartIcon from '@mui/icons-material/PieChart';
import ShowChartIcon from '@mui/icons-material/ShowChart';

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
      id={`revenue-tabpanel-${index}`}
      aria-labelledby={`revenue-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
}

function a11yProps(index: number) {
  return {
    id: `revenue-tab-${index}`,
    'aria-controls': `revenue-tabpanel-${index}`,
  };
}

export function RevenueDashboard() {
  const [tabValue, setTabValue] = useState(0);
  const { data: analysis, isLoading, error } = useRevenueAnalysis();

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
          Failed to load revenue analysis data
        </Alert>
      </Box>
    );
  }

  const { data: revenueData } = analysis;

  return (
    <Box>
      {/* Header */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h4" gutterBottom>
          Revenue Management
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Monitor and manage your revenue streams, trends, and performance metrics
        </Typography>
      </Paper>

      {/* Key Metrics */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Total Revenue"
            value={{
              value: revenueData.totalRevenue,
              unit: 'currency',
              change: revenueData.yearOverYearGrowth,
              trend: revenueData.yearOverYearGrowth > 0 ? 'up' : 'down',
            }}
            icon={<MonetizationOnIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Recurring Revenue"
            value={{
              value: revenueData.recurringRevenue,
              unit: 'currency',
              change: (revenueData.recurringRevenue / revenueData.totalRevenue) * 100,
              trend: 'up',
            }}
            icon={<TrendingUpIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Revenue Growth"
            value={{
              value: revenueData.monthOverMonthGrowth,
              unit: '%',
              change: revenueData.monthOverMonthGrowth - revenueData.quarterOverQuarterGrowth,
              trend: revenueData.monthOverMonthGrowth > revenueData.quarterOverQuarterGrowth ? 'up' : 'down',
            }}
            icon={<ShowChartIcon />}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <FinancialMetricsCard
            title="Customer LTV"
            value={{
              value: revenueData.customerLifetimeValue,
              unit: 'currency',
              change: 0,
              trend: revenueData.customerLifetimeValue > 0 ? 'up' : 'down',
            }}
            icon={<PieChartIcon />}
          />
        </Grid>
      </Grid>

      {/* Main Content */}
      <Paper>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="revenue management tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Revenue Sources" {...a11yProps(0)} />
          <Tab label="Revenue Trends" {...a11yProps(1)} />
        </Tabs>

        <TabPanel value={tabValue} index={0}>
          <RevenueSourceList />
        </TabPanel>
        <TabPanel value={tabValue} index={1}>
          <RevenueTrends />
        </TabPanel>
      </Paper>

      {/* Revenue Risks and Opportunities */}
      {(revenueData.risks?.length > 0 || revenueData.opportunities?.length > 0) && (
        <Grid container spacing={3} sx={{ mt: 3 }}>
          {revenueData.risks?.length > 0 && (
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Revenue Risks
                </Typography>
                {revenueData.risks.map((risk) => (
                  <Alert key={risk.id} severity="warning" sx={{ mt: 1 }}>
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

          {revenueData.opportunities?.length > 0 && (
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Revenue Opportunities
                </Typography>
                {revenueData.opportunities.map((opportunity) => (
                  <Alert key={opportunity.id} severity="info" sx={{ mt: 1 }}>
                    <Typography variant="subtitle2">{opportunity.name}</Typography>
                    <Typography variant="body2">{opportunity.description}</Typography>
                    <Typography variant="caption" display="block" sx={{ mt: 0.5 }}>
                      Potential Value: {
                        new Intl.NumberFormat('en-US', {
                          style: 'currency',
                          currency: 'USD',
                          notation: 'compact',
                        }).format(opportunity.potentialValue)
                      } | Probability: {opportunity.probability.toFixed(1)}%
                    </Typography>
                  </Alert>
                ))}
              </Paper>
            </Grid>
          )}
        </Grid>
      )}
    </Box>
  );
}