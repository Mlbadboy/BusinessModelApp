import { useState } from 'react';
import { Box, Tab, Tabs, Typography, Stack, Paper } from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import dayjs, { Dayjs } from 'dayjs';
import { BusinessHealthOverview } from './BusinessHealthOverview';
import { FinancialPerformance } from './FinancialPerformance';

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
      id={`analytics-tabpanel-${index}`}
      aria-labelledby={`analytics-tab-${index}`}
      {...other}
    >
      {value === index && <Box>{children}</Box>}
    </div>
  );
}

function a11yProps(index: number) {
  return {
    id: `analytics-tab-${index}`,
    'aria-controls': `analytics-tabpanel-${index}`,
  };
}

interface DateRangeState {
  startDate: Dayjs;
  endDate: Dayjs;
}

export function AnalyticsDashboard() {
  const [tabValue, setTabValue] = useState(0);
  const [dateRange, setDateRange] = useState<DateRangeState>({
    startDate: dayjs().subtract(1, 'month'),
    endDate: dayjs(),
  });

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  return (
    <Box>
      {/* Header */}
      <Paper 
        sx={{ 
          p: 3,
          mb: 3,
          borderRadius: 0,
          borderBottom: 1,
          borderColor: 'divider'
        }}
      >
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', md: 'center' }}
          spacing={2}
        >
          <Box>
            <Typography variant="h4" gutterBottom>
              Analytics Dashboard
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Track and analyze your business performance metrics
            </Typography>
          </Box>

          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            alignItems="center"
          >
            <DatePicker
              label="Start Date"
              value={dateRange.startDate}
              onChange={(newValue: Dayjs | null) => {
                if (newValue?.isValid()) {
                  setDateRange(prev => ({
                    ...prev,
                    startDate: newValue,
                  }));
                }
              }}
              slotProps={{
                textField: {
                  size: "small",
                  sx: { width: 170 }
                }
              }}
            />
            <DatePicker
              label="End Date"
              value={dateRange.endDate}
              onChange={(newValue: Dayjs | null) => {
                if (newValue?.isValid()) {
                  setDateRange(prev => ({
                    ...prev,
                    endDate: newValue,
                  }));
                }
              }}
              slotProps={{
                textField: {
                  size: "small",
                  sx: { width: 170 }
                }
              }}
            />
          </Stack>
        </Stack>
      </Paper>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="analytics dashboard tabs"
        >
          <Tab label="Business Health" {...a11yProps(0)} />
          <Tab label="Financial Performance" {...a11yProps(1)} />
        </Tabs>
      </Box>

      {/* Tab Panels */}
      <TabPanel value={tabValue} index={0}>
        <BusinessHealthOverview />
      </TabPanel>
      <TabPanel value={tabValue} index={1}>
        <FinancialPerformance />
      </TabPanel>
    </Box>
  );
}