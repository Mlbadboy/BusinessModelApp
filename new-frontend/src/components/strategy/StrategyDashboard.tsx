import { useState } from 'react';
import {
  Box,
  Tabs,
  Tab,
  Typography,
  Paper,
  Container,
} from '@mui/material';
import { RiskManagement } from './RiskManagement';
import { PerformanceTracking } from './PerformanceTracking';

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
      id={`strategy-tabpanel-${index}`}
      aria-labelledby={`strategy-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

export function StrategyDashboard() {
  const [activeTab, setActiveTab] = useState(0);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Strategy Management
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Monitor and manage your business strategy, risks, and performance metrics
        </Typography>
      </Box>

      <Paper sx={{ width: '100%' }}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs
            value={activeTab}
            onChange={handleTabChange}
            aria-label="strategy management tabs"
          >
            <Tab label="Performance Tracking" />
            <Tab label="Risk Management" />
          </Tabs>
        </Box>

        <TabPanel value={activeTab} index={0}>
          <PerformanceTracking />
        </TabPanel>

        <TabPanel value={activeTab} index={1}>
          <RiskManagement />
        </TabPanel>
      </Paper>
    </Container>
  );
}