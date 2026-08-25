import {
  Box,
  Typography,
  Grid,
  Paper,
  Chip,
  LinearProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Button,
  Stack,
  Card,
  CardContent,
} from '@mui/material';
import { useAIControlCenter, ApprovalItem, AITelemetryItem } from '../../hooks/useAIControlCenter';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

const formatINR = (amount: number) => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
  }).format(amount);
};

const AIControlCenter = () => {
  const { summary, telemetry, approvals, isLoading, decideApproval } = useAIControlCenter();

  if (isLoading) {
    return <LoadingState message="Loading AI Control Center & FinOps Engine..." />;
  }

  const spend = summary?.monthlySpend ?? 0;
  const budgetCap = summary?.monthlyBudgetCap ?? 50000;
  const percentConsumed = summary?.budgetPercentConsumed ?? 0;

  const routingRules = [
    { task: 'Executive Brief', quality: 'High Quality', target: 'P50 < 1.2s', strategy: 'Gateway Managed (Pool A)', cost: '₹0.50 max' },
    { task: 'Health Explanation', quality: 'High Quality', target: 'P50 < 1.0s', strategy: 'Gateway Managed (Pool A)', cost: '₹0.25 max' },
    { task: 'Opportunity Analysis', quality: 'Balanced', target: 'P50 < 1.5s', strategy: 'Gateway Managed (Pool B)', cost: '₹0.20 max' },
    { task: 'Lead Qualification', quality: 'Balanced', target: 'P50 < 0.8s', strategy: 'Gateway Managed (Pool B)', cost: '₹0.10 max' },
    { task: 'Voice Qualification', quality: 'Balanced', target: 'P50 < 0.35s', strategy: 'Fallback Pool (Ultra Low Latency)', cost: '₹0.05 max' },
  ];

  return (
    <Box sx={{ p: 3 }}>
      {/* Top Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <div>
          <Typography variant="h4" fontWeight="bold">
            AI Control Plane & FinOps Center
          </Typography>
          <Typography variant="body2" color="text.secondary">
            OmniRoute Traffic Control • Budget Reservation • Data Minimization • Human Approvals
          </Typography>
        </div>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Chip
            label={`Gateway: ${summary?.gatewayStatus || 'Healthy'}`}
            color={summary?.gatewayStatus === 'Healthy' ? 'success' : 'warning'}
            sx={{ fontWeight: 'bold' }}
          />
          <Chip
            label={`AI ROI: ${(summary?.aiRoiRatio || 19.5).toFixed(1)}x`}
            color="primary"
            variant="outlined"
            sx={{ fontWeight: 'bold' }}
          />
        </Stack>
      </Box>

      {/* KPI Cards: FinOps & Gateway Performance */}
      <Grid container spacing={2.5} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Paper elevation={2} sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Monthly AI Spend
            </Typography>
            <Typography variant="h5" fontWeight="bold" color={percentConsumed > 80 ? 'error.main' : 'primary.main'} sx={{ my: 0.5 }}>
              {formatINR(spend)}
            </Typography>
            <LinearProgress
              variant="determinate"
              value={Math.min(100, percentConsumed)}
              color={percentConsumed > 80 ? 'error' : 'primary'}
              sx={{ height: 6, borderRadius: 3, my: 1 }}
            />
            <Typography variant="caption" color="text.secondary">
              Budget: {formatINR(budgetCap)} ({percentConsumed.toFixed(1)}% consumed)
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Paper elevation={2} sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Total Inferences
            </Typography>
            <Typography variant="h5" fontWeight="bold" color="text.primary" sx={{ my: 0.5 }}>
              {(summary?.totalRequests || 0).toLocaleString()} Requests
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              {(summary?.totalTokens || 0).toLocaleString()} Total Tokens
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Paper elevation={2} sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Inference Latency
            </Typography>
            <Typography variant="h5" fontWeight="bold" color="info.main" sx={{ my: 0.5 }}>
              {((summary?.averageLatencyMs || 850) / 1000).toFixed(2)}s
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Target P50 &lt; 1.2s • Fallbacks: {summary?.fallbackCount || 0}
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Paper elevation={2} sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Cache Optimization
            </Typography>
            <Typography variant="h5" fontWeight="bold" color="success.main" sx={{ my: 0.5 }}>
              {formatINR(summary?.cacheSavings || 0)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {summary?.cacheHits || 0} cache hits across active workspace
            </Typography>
          </Paper>
        </Grid>
      </Grid>

      {/* Governed Routing Strategies & Human Approvals */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        {/* Governed Routing Policies */}
        <Grid item xs={12} md={7}>
          <Paper elevation={2} sx={{ p: 3, height: '100%' }}>
            <Typography variant="h6" fontWeight="bold" gutterBottom>
              Requirements-Driven Routing Catalog
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Dynamic provider selection and failover based on capability profiles.
            </Typography>

            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Task Type</TableCell>
                    <TableCell>Quality Profile</TableCell>
                    <TableCell>Latency Target</TableCell>
                    <TableCell>Strategy</TableCell>
                    <TableCell align="right">Max Cost</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {routingRules.map((r, idx) => (
                    <TableRow key={idx} hover>
                      <TableCell sx={{ fontWeight: 'bold' }}>{r.task}</TableCell>
                      <TableCell>
                        <Chip label={r.quality} size="small" variant="outlined" />
                      </TableCell>
                      <TableCell>{r.target}</TableCell>
                      <TableCell>{r.strategy}</TableCell>
                      <TableCell align="right">{r.cost}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </Grid>

        {/* Human Approvals Queue */}
        <Grid item xs={12} md={5}>
          <Paper elevation={2} sx={{ p: 3, height: '100%' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="h6" fontWeight="bold">
                Consequential Approvals
              </Typography>
              <Chip label={`${approvals.filter((a: ApprovalItem) => a.status === 1).length} Pending`} color="warning" size="small" />
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Human-in-the-loop review for medium & high risk AI actions.
            </Typography>

            <Stack spacing={1.5} sx={{ maxHeight: 260, overflowY: 'auto' }}>
              {approvals.length === 0 ? (
                <Typography color="text.secondary" align="center" sx={{ py: 3 }}>
                  No pending approval requests.
                </Typography>
              ) : (
                approvals.map((req: ApprovalItem) => (
                  <Card key={req.id} variant="outlined">
                    <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                        <div>
                          <Typography variant="subtitle2" fontWeight="bold">
                            {req.title}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            By: {req.requesterName} • {new Date(req.createdAt).toLocaleTimeString()}
                          </Typography>
                        </div>
                        <Chip
                          label={req.status === 1 ? 'Pending' : req.status === 2 ? 'Approved' : req.status === 4 ? 'Auto-Approved' : 'Rejected'}
                          size="small"
                          color={req.status === 1 ? 'warning' : req.status === 2 || req.status === 4 ? 'success' : 'error'}
                        />
                      </Box>
                      {req.status === 1 && (
                        <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
                          <Button
                            size="small"
                            variant="contained"
                            color="success"
                            onClick={() => decideApproval.mutate({ id: req.id, isApproved: true, decisionNote: 'Approved via Control Plane' })}
                          >
                            Approve
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="error"
                            onClick={() => decideApproval.mutate({ id: req.id, isApproved: false, decisionNote: 'Rejected by Admin' })}
                          >
                            Reject
                          </Button>
                        </Stack>
                      )}
                    </CardContent>
                  </Card>
                ))
              )}
            </Stack>
          </Paper>
        </Grid>
      </Grid>

      {/* Real-time AI Telemetry Audit Stream */}
      <Paper elevation={2} sx={{ p: 3 }}>
        <Typography variant="h6" fontWeight="bold" gutterBottom>
          Immutable AI Call Telemetry Stream
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Append-only execution ledger for tokens, latency, cost attribution, and provider routing.
        </Typography>

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Timestamp</TableCell>
                <TableCell>Task Type</TableCell>
                <TableCell>Provider</TableCell>
                <TableCell>Model</TableCell>
                <TableCell align="right">Tokens</TableCell>
                <TableCell align="right">Latency</TableCell>
                <TableCell align="right">Cost</TableCell>
                <TableCell align="right">Correlation</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {telemetry.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 3 }}>
                    <Typography color="text.secondary">No AI calls recorded yet.</Typography>
                  </TableCell>
                </TableRow>
              ) : (
                telemetry.map((t: AITelemetryItem) => (
                  <TableRow key={t.id} hover>
                    <TableCell>{new Date(t.createdAt).toLocaleTimeString()}</TableCell>
                    <TableCell>
                      <Chip label={t.taskType} size="small" />
                    </TableCell>
                    <TableCell>{t.provider}</TableCell>
                    <TableCell>{t.model}</TableCell>
                    <TableCell align="right">{t.totalTokens.toLocaleString()}</TableCell>
                    <TableCell align="right">{t.latencyMs}ms</TableCell>
                    <TableCell align="right">{t.estimatedCost !== null ? formatINR(t.estimatedCost) : '—'}</TableCell>
                    <TableCell align="right" sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                      {t.requestCorrelationId?.substring(0, 8)}...
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>
    </Box>
  );
};

export default function AIControlCenterWithErrorBoundary() {
  return (
    <ErrorBoundary>
      <AIControlCenter />
    </ErrorBoundary>
  );
}
