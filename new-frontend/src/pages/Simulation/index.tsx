import React, { useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Stack,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Alert
} from '@mui/material';
import ScienceIcon from '@mui/icons-material/Science';
import AccountTreeIcon from '@mui/icons-material/AccountTree';
import ShieldIcon from '@mui/icons-material/Shield';
import TuneIcon from '@mui/icons-material/Tune';
import { Layout } from '../../components/Layout/Layout';
import { MetricCard } from '../../components/ui/MetricCard';

interface ScenarioItem {
  id: string;
  name: string;
  agents: number;
  horizonDays: number;
  status: string;
  projectedRevenue: string;
  projectedChurn: string;
  projectedMargin: string;
}

export const SimulationPage: React.FC = () => {
  const [selectedScenario, setSelectedScenario] = useState<string>('scen-1');

  const scenarios: ScenarioItem[] = [
    {
      id: 'scen-1',
      name: 'Baseline Reality Snapshot',
      agents: 250,
      horizonDays: 90,
      status: 'VALIDATED',
      projectedRevenue: '$5,000,000',
      projectedChurn: '2.8%',
      projectedMargin: '22.0%'
    },
    {
      id: 'scen-2',
      name: 'Hypothesis A: +10% Enterprise Pricing',
      agents: 500,
      horizonDays: 180,
      status: 'COMPLETED',
      projectedRevenue: '$5,450,000',
      projectedChurn: '3.4%',
      projectedMargin: '25.8%'
    },
    {
      id: 'scen-3',
      name: 'Hypothesis B: Aggressive Competitor Counter-move',
      agents: 1000,
      horizonDays: 180,
      status: 'COMPLETED',
      projectedRevenue: '$4,820,000',
      projectedChurn: '4.2%',
      projectedMargin: '20.5%'
    }
  ];

  const active = scenarios.find(s => s.id === selectedScenario) || scenarios[0];

  return (
    <Layout>
      <Box sx={{ p: 3, maxWidth: 1600, margin: '0 auto' }}>
        {/* Header with Epistemic State Badges */}
        <Box sx={{ mb: 4, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
          <Box>
            <Typography variant="h4" sx={{ fontWeight: 700, color: 'text.primary', display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <ScienceIcon sx={{ color: '#8B5CF6', fontSize: 36 }} />
              Organizational Simulation & Digital Sandbox
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5 }}>
              Batch 3.9.9 — Governed Counterfactual Scenario Lab & Emergent Multi-Agent World Modeling
            </Typography>
          </Box>
          <Stack direction="row" spacing={1.5}>
            <Chip
              label="Truth: SIMULATION"
              sx={{
                bgcolor: 'rgba(139, 92, 246, 0.15)',
                color: '#8B5CF6',
                border: '1px solid #8B5CF6',
                fontWeight: 700,
                fontSize: '0.85rem'
              }}
            />
            <Chip
              label="Reality Boundary: Sandboxed"
              sx={{
                bgcolor: 'rgba(16, 185, 129, 0.15)',
                color: '#10B981',
                border: '1px solid #10B981',
                fontWeight: 600
              }}
            />
          </Stack>
        </Box>

        {/* Invariant I34 Banner */}
        <Alert
          severity="info"
          icon={<ShieldIcon sx={{ color: '#8B5CF6' }} />}
          sx={{
            mb: 4,
            bgcolor: 'rgba(139, 92, 246, 0.08)',
            border: '1px solid rgba(139, 92, 246, 0.25)',
            color: 'text.primary'
          }}
        >
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
            Constitutional Invariant I34 Enforced:
          </Typography>
          <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block', mt: 0.25 }}>
            SIMULATION ≠ REALITY ≠ TRUTH ≠ FORECAST ≠ SCENARIO ≠ DECISION ≠ ALLOCATION ≠ AUTHORITY ≠ EXECUTION ≠ OUTCOME.
            Simulations operate strictly in an isolated sandbox with zero access to production credentials, execution permits, or live OARA capacities.
          </Typography>
        </Alert>

        {/* Primary Metric Projections */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Projected Revenue"
              value={active.projectedRevenue}
              subtitle={`Horizon: ${active.horizonDays} days`}
              delta={{ value: '+9.0% vs Base', isPositive: true }}
              accentColor="#00F0FF"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Simulated Churn Rate"
              value={active.projectedChurn}
              subtitle="Synthetic Customer Sensitivity"
              delta={{ value: 'Within Tolerance', isPositive: true }}
              accentColor="#F59E0B"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Operating Margin"
              value={active.projectedMargin}
              subtitle="Staffing & Price Optimized"
              delta={{ value: '+3.8% Delta', isPositive: true }}
              accentColor="#10B981"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Synthetic Agents"
              value={active.agents.toString()}
              subtitle="Customer, Competitor, Staff"
              delta={{ value: 'Deterministic Seed', isPositive: true }}
              accentColor="#8B5CF6"
            />
          </Grid>
        </Grid>

        {/* Scenario Lab & Branching */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} md={7}>
            <Card sx={{ bgcolor: 'background.paper', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Typography variant="h6" sx={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: 1 }}>
                    <AccountTreeIcon sx={{ color: '#00F0FF' }} />
                    Counterfactual Scenario Lab
                  </Typography>
                  <Button
                    variant="outlined"
                    size="small"
                    startIcon={<TuneIcon />}
                    sx={{ borderColor: '#8B5CF6', color: '#8B5CF6' }}
                  >
                    Branch Scenario
                  </Button>
                </Box>
                <TableContainer component={Paper} sx={{ bgcolor: 'transparent', boxShadow: 'none' }}>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell sx={{ color: 'text.secondary' }}>Scenario</TableCell>
                        <TableCell sx={{ color: 'text.secondary' }}>Agents</TableCell>
                        <TableCell sx={{ color: 'text.secondary' }}>Horizon</TableCell>
                        <TableCell sx={{ color: 'text.secondary' }}>Status</TableCell>
                        <TableCell align="right" sx={{ color: 'text.secondary' }}>Action</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {scenarios.map((s) => (
                        <TableRow
                          key={s.id}
                          hover
                          selected={s.id === selectedScenario}
                          onClick={() => setSelectedScenario(s.id)}
                          sx={{ cursor: 'pointer' }}
                        >
                          <TableCell sx={{ fontWeight: s.id === selectedScenario ? 700 : 400 }}>
                            {s.name}
                          </TableCell>
                          <TableCell>{s.agents}</TableCell>
                          <TableCell>{s.horizonDays}d</TableCell>
                          <TableCell>
                            <Chip
                              label={s.status}
                              size="small"
                              sx={{
                                bgcolor: s.status === 'VALIDATED' ? 'rgba(16, 185, 129, 0.15)' : 'rgba(0, 240, 255, 0.15)',
                                color: s.status === 'VALIDATED' ? '#10B981' : '#00F0FF',
                                fontSize: '0.75rem'
                              }}
                            />
                          </TableCell>
                          <TableCell align="right">
                            <Button size="small" variant={s.id === selectedScenario ? 'contained' : 'text'}>
                              {s.id === selectedScenario ? 'Active' : 'Inspect'}
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>

          {/* "Why This Simulation?" Provenance Card */}
          <Grid item xs={12} md={5}>
            <Card sx={{ bgcolor: 'background.paper', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}>
              <CardContent>
                <Typography variant="h6" sx={{ fontWeight: 600, mb: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
                  <ShieldIcon sx={{ color: '#10B981' }} />
                  Why This Simulation? (Provenance Trace)
                </Typography>
                <Stack spacing={1.5}>
                  <Box>
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>Hypothesis Goal</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      Explores pricing elasticity tradeoffs and competitive countermeasure probability under Q4 economic parameters.
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>Model & Reproducibility</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      Deterministic Engine v3.9.9 | RandomSeed: 42 | SHA-256 Decision Hash Verified
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>Multi-Agent Population</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {active.agents} Synthetic Agents (Customer segments: Enterprise, MidMarket, SMB; 4 Competitor Personas).
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>Governed Barrier</Typography>
                    <Typography variant="body2" sx={{ color: '#F59E0B', fontWeight: 600 }}>
                      Evidence only. Output requires formal PRG-1 sign-off before entering decision intelligence or OARA arbitration.
                    </Typography>
                  </Box>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Box>
    </Layout>
  );
};

export default SimulationPage;
