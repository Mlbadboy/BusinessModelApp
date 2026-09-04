import React, { useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Chip,
  Button,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  Select,
  MenuItem,
  Tabs,
  Tab,
  LinearProgress,
  Tooltip,
  Alert,
  CircularProgress,
  Divider,
} from '@mui/material';
import Cable from '@mui/icons-material/Cable';
import CheckCircle from '@mui/icons-material/CheckCircle';
import ErrorOutline from '@mui/icons-material/ErrorOutline';
import Shield from '@mui/icons-material/Shield';
import Lock from '@mui/icons-material/Lock';
import PlayArrow from '@mui/icons-material/PlayArrow';
import Settings from '@mui/icons-material/Settings';
import LinkOff from '@mui/icons-material/LinkOff';
import Speed from '@mui/icons-material/Speed';
import Key from '@mui/icons-material/Key';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import { Layout } from '../../components/Layout';
import { useConnect, ConnectorSummary, ConnectorHealthReport } from '../../hooks/useConnect';

const CATEGORIES = ['All', 'Communication', 'Telephony', 'Payments', 'Delivery'];

export const ConnectPage: React.FC = () => {
  const {
    connectors,
    isLoading,
    testConnector,
    configureKeys,
    handleOAuthCallback,
    updateCapabilities,
    disconnectConnector,
  } = useConnect();

  const [selectedCategory, setSelectedCategory] = useState('All');
  
  // Diagnostic Report Modal State
  const [reportModalOpen, setReportModalOpen] = useState(false);
  const [activeReport, setActiveReport] = useState<ConnectorHealthReport | null>(null);
  const [testingProvider, setTestingProvider] = useState<string | null>(null);

  // Capability Matrix Modal State
  const [capModalOpen, setCapModalOpen] = useState(false);
  const [selectedConnectorForCap, setSelectedConnectorForCap] = useState<ConnectorSummary | null>(null);
  const [editableCapabilities, setEditableCapabilities] = useState<Record<string, number>>({});

  // Configure Keys Modal State
  const [configModalOpen, setConfigModalOpen] = useState(false);
  const [selectedConnectorForConfig, setSelectedConnectorForConfig] = useState<ConnectorSummary | null>(null);
  const [apiKeyInput, setApiKeyInput] = useState('');
  const [apiSecretInput, setApiSecretInput] = useState('');
  const [accountIdentifierInput, setAccountIdentifierInput] = useState('');

  const filteredConnectors = connectors.filter((c) =>
    selectedCategory === 'All' ? true : c.category.toLowerCase() === selectedCategory.toLowerCase()
  );

  const handleRunTest = async (provider: string) => {
    try {
      setTestingProvider(provider);
      const report = await testConnector.mutateAsync(provider);
      setActiveReport(report);
      setReportModalOpen(true);
    } finally {
      setTestingProvider(null);
    }
  };

  const handleOpenCapabilities = (connector: ConnectorSummary) => {
    setSelectedConnectorForCap(connector);
    const converted: Record<string, number> = {};
    Object.entries(connector.capabilities).forEach(([key, modeStr]) => {
      converted[key] = modeStr === 'Allowed' ? 2 : modeStr === 'RequireApproval' ? 1 : 0;
    });
    setEditableCapabilities(converted);
    setCapModalOpen(true);
  };

  const handleSaveCapabilities = async () => {
    if (!selectedConnectorForCap) return;
    await updateCapabilities.mutateAsync({
      provider: selectedConnectorForCap.provider,
      capabilities: editableCapabilities,
    });
    setCapModalOpen(false);
  };

  const handleOpenConfig = (connector: ConnectorSummary) => {
    setSelectedConnectorForConfig(connector);
    setApiKeyInput('');
    setApiSecretInput('');
    setAccountIdentifierInput(connector.accountIdentifier || '');
    setConfigModalOpen(true);
  };

  const handleSaveConfig = async () => {
    if (!selectedConnectorForConfig) return;
    await configureKeys.mutateAsync({
      provider: selectedConnectorForConfig.provider,
      data: {
        apiKey: apiKeyInput,
        apiSecret: apiSecretInput || undefined,
        accountIdentifier: accountIdentifierInput || undefined,
      },
    });
    setConfigModalOpen(false);
  };

  const handleOAuthConnect = async (provider: string) => {
    // In reference sandbox mode, triggers simulated OAuth code exchange
    await handleOAuthCallback.mutateAsync({
      provider,
      code: `auth_code_${provider.toLowerCase()}_${Date.now()}`,
    });
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Healthy':
        return '#10B981';
      case 'Configured':
      case 'Authenticated':
        return '#3B82F6';
      case 'Degraded':
        return '#F59E0B';
      case 'Disconnected':
        return '#6B7280';
      default:
        return '#EF4444';
    }
  };

  return (
    <Layout>
      <Box sx={{ p: 4, maxWidth: 1400, margin: '0 auto' }}>
        {/* Header & Reality Protocol Banner */}
        <Box sx={{ mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <Box
                sx={{
                  p: 1.5,
                  borderRadius: 2,
                  bgcolor: 'rgba(59, 130, 246, 0.1)',
                  color: '#3B82F6',
                  display: 'flex',
                }}
              >
                <Cable sx={{ fontSize: 32 }} />
              </Box>
              <Box>
                <Typography variant="h4" sx={{ fontWeight: 800, color: '#F9FAFB', letterSpacing: '-0.02em' }}>
                  ⚡ Charlie Connect
                </Typography>
                <Typography variant="body2" sx={{ color: '#9CA3AF' }}>
                  Governed Integration Control Plane & Encrypted Token Vault
                </Typography>
              </Box>
            </Box>

            <Chip
              label="Hybrid Pilot Mode"
              color="primary"
              variant="outlined"
              sx={{ borderColor: '#3B82F6', color: '#60A5FA', fontWeight: 600 }}
            />
          </Box>

          <Alert
            severity="info"
            icon={<InfoOutlined />}
            sx={{
              mt: 2,
              bgcolor: 'rgba(17, 24, 39, 0.8)',
              color: '#D1D5DB',
              border: '1px solid rgba(59, 130, 246, 0.2)',
              borderRadius: 2,
            }}
          >
            <Typography variant="caption" sx={{ display: 'block', color: '#93C5FD', fontWeight: 700, mb: 0.5 }}>
              PROD CERTIFICATION PROTOCOL
            </Typography>
            A <strong>7/7 Diagnostic Health Score</strong> proves cryptographic vault storage, token unexpired state, and capability matrix authorization. Actual revenue attribution requires verified provider webhooks and payment events.
          </Alert>
        </Box>

        {/* Category Tabs */}
        <Box sx={{ borderBottom: 1, borderColor: 'rgba(255, 255, 255, 0.1)', mb: 3 }}>
          <Tabs
            value={selectedCategory}
            onChange={(_, val) => setSelectedCategory(val)}
            textColor="inherit"
            sx={{
              '& .MuiTabs-indicator': { backgroundColor: '#3B82F6' },
              '& .MuiTab-root': { color: '#9CA3AF', '&.Mui-selected': { color: '#F9FAFB', fontWeight: 700 } },
            }}
          >
            {CATEGORIES.map((cat) => (
              <Tab key={cat} label={cat} value={cat} />
            ))}
          </Tabs>
        </Box>

        {/* Connectors Grid */}
        {isLoading ? (
          <LinearProgress sx={{ my: 4, borderRadius: 1 }} />
        ) : (
          <Grid container spacing={3}>
            {filteredConnectors.map((connector) => {
              const statusColor = getStatusColor(connector.status);
              const isTesting = testingProvider === connector.provider;

              return (
                <Grid item xs={12} md={6} lg={4} key={connector.provider}>
                  <Card
                    sx={{
                      bgcolor: '#0F172A',
                      borderRadius: 3,
                      border: '1px solid rgba(255, 255, 255, 0.08)',
                      transition: 'transform 0.2s, border-color 0.2s',
                      '&:hover': {
                        transform: 'translateY(-2px)',
                        borderColor: statusColor,
                      },
                    }}
                  >
                    <CardContent sx={{ p: 3 }}>
                      {/* Top Row: Provider info & Status */}
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                        <Box>
                          <Typography variant="h6" sx={{ fontWeight: 700, color: '#F8FAFC', fontSize: '1.05rem' }}>
                            {connector.displayName}
                          </Typography>
                          <Typography variant="caption" sx={{ color: '#64748B' }}>
                            Category: {connector.category}
                          </Typography>
                        </Box>
                        <Chip
                          label={connector.status}
                          size="small"
                          sx={{
                            bgcolor: `${statusColor}15`,
                            color: statusColor,
                            fontWeight: 700,
                            border: `1px solid ${statusColor}40`,
                          }}
                        />
                      </Box>

                      {/* Account Binding */}
                      <Box
                        sx={{
                          p: 1.5,
                          borderRadius: 2,
                          bgcolor: 'rgba(0, 0, 0, 0.3)',
                          border: '1px solid rgba(255, 255, 255, 0.04)',
                          mb: 2,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                        }}
                      >
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Lock sx={{ fontSize: 16, color: '#94A3B8' }} />
                          <Typography variant="caption" sx={{ color: '#CBD5E1', fontFamily: 'monospace' }}>
                            {connector.accountIdentifier || 'No Account Bound'}
                          </Typography>
                        </Box>
                        <Chip
                          label={`${connector.probesPassed}/${connector.totalProbes} Probes`}
                          size="small"
                          sx={{
                            fontSize: '0.7rem',
                            height: 20,
                            bgcolor: connector.isHealthy ? 'rgba(16, 185, 129, 0.15)' : 'rgba(100, 116, 139, 0.15)',
                            color: connector.isHealthy ? '#10B981' : '#94A3B8',
                          }}
                        />
                      </Box>

                      {/* Capabilities Overview */}
                      <Box sx={{ mb: 3 }}>
                        <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600, display: 'block', mb: 1 }}>
                          CAPABILITY PERMISSIONS
                        </Typography>
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75 }}>
                          {Object.entries(connector.capabilities).map(([capKey, mode]) => {
                            const isAllowed = mode === 'Allowed';
                            const isApproval = mode === 'RequireApproval';
                            return (
                              <Tooltip key={capKey} title={`Status: ${mode}`}>
                                <Chip
                                  icon={
                                    isApproval ? (
                                      <Shield sx={{ fontSize: '12px !important', color: '#F59E0B !important' }} />
                                    ) : undefined
                                  }
                                  label={capKey.replace(/_/g, ' ')}
                                  size="small"
                                  sx={{
                                    fontSize: '0.72rem',
                                    height: 24,
                                    bgcolor: isAllowed
                                      ? 'rgba(16, 185, 129, 0.1)'
                                      : isApproval
                                      ? 'rgba(245, 158, 11, 0.1)'
                                      : 'rgba(239, 68, 68, 0.1)',
                                    color: isAllowed ? '#34D399' : isApproval ? '#FBBF24' : '#F87171',
                                    border: `1px solid ${
                                      isAllowed
                                        ? 'rgba(16, 185, 129, 0.25)'
                                        : isApproval
                                        ? 'rgba(245, 158, 11, 0.25)'
                                        : 'rgba(239, 68, 68, 0.25)'
                                    }`,
                                  }}
                                />
                              </Tooltip>
                            );
                          })}
                        </Box>
                      </Box>

                      <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.06)', mb: 2 }} />

                      {/* Card Actions */}
                      <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                        <Button
                          variant="contained"
                          size="small"
                          startIcon={isTesting ? <CircularProgress size={14} color="inherit" /> : <PlayArrow />}
                          disabled={isTesting}
                          onClick={() => handleRunTest(connector.provider)}
                          sx={{
                            flex: 1,
                            bgcolor: '#1E293B',
                            color: '#F8FAFC',
                            '&:hover': { bgcolor: '#334155' },
                            textTransform: 'none',
                            fontWeight: 600,
                          }}
                        >
                          {isTesting ? 'Testing...' : 'Test 7/7'}
                        </Button>

                        <IconButton
                          size="small"
                          onClick={() => handleOpenCapabilities(connector)}
                          sx={{ color: '#94A3B8', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: 2 }}
                          title="Manage Capabilities"
                        >
                          <Settings fontSize="small" />
                        </IconButton>

                        {connector.status === 'Disconnected' || connector.status === 'Revoked' ? (
                          connector.provider === 'GoogleWorkspace' || connector.provider === 'Microsoft365' ? (
                            <Button
                              variant="contained"
                              size="small"
                              onClick={() => handleOAuthConnect(connector.provider)}
                              sx={{
                                bgcolor: '#3B82F6',
                                color: '#FFFFFF',
                                '&:hover': { bgcolor: '#2563EB' },
                                textTransform: 'none',
                                fontWeight: 600,
                              }}
                            >
                              OAuth
                            </Button>
                          ) : (
                            <IconButton
                              size="small"
                              onClick={() => handleOpenConfig(connector)}
                              sx={{ color: '#3B82F6', border: '1px solid rgba(59, 130, 246, 0.3)', borderRadius: 2 }}
                              title="Configure Keys"
                            >
                              <Key fontSize="small" />
                            </IconButton>
                          )
                        ) : (
                          <IconButton
                            size="small"
                            onClick={() => disconnectConnector.mutate(connector.provider)}
                            sx={{ color: '#EF4444', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: 2 }}
                            title="Disconnect & Purge Vault"
                          >
                            <LinkOff fontSize="small" />
                          </IconButton>
                        )}
                      </Box>
                    </CardContent>
                  </Card>
                </Grid>
              );
            })}
          </Grid>
        )}

        {/* 7/7 Diagnostic Probes Report Dialog */}
        <Dialog
          open={reportModalOpen}
          onClose={() => setReportModalOpen(false)}
          maxWidth="md"
          fullWidth
          PaperProps={{
            sx: { bgcolor: '#0B1120', border: '1px solid rgba(255, 255, 255, 0.1)', borderRadius: 3 },
          }}
        >
          <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <Speed sx={{ color: '#3B82F6' }} />
              <Typography variant="h6" sx={{ color: '#F9FAFB', fontWeight: 700 }}>
                7/7 Diagnostic Probe Report: {activeReport?.provider}
              </Typography>
            </Box>
            <Chip
              label={`${activeReport?.probesPassed}/${activeReport?.totalProbes} Passed`}
              color={activeReport?.isHealthy ? 'success' : 'warning'}
              sx={{ fontWeight: 700 }}
            />
          </DialogTitle>

          <DialogContent dividers sx={{ borderColor: 'rgba(255, 255, 255, 0.08)' }}>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              {activeReport?.probes.map((probe) => (
                <Box
                  key={probe.probeId}
                  sx={{
                    p: 2,
                    borderRadius: 2,
                    bgcolor: 'rgba(15, 23, 42, 0.6)',
                    border: `1px solid ${probe.passed ? 'rgba(16, 185, 129, 0.2)' : 'rgba(239, 68, 68, 0.2)'}`,
                    display: 'flex',
                    alignItems: 'flex-start',
                    justifyContent: 'space-between',
                  }}
                >
                  <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
                    {probe.passed ? (
                      <CheckCircle sx={{ color: '#10B981', mt: 0.25 }} />
                    ) : (
                      <ErrorOutline sx={{ color: '#EF4444', mt: 0.25 }} />
                    )}
                    <Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#F8FAFC' }}>
                          {probe.name}
                        </Typography>
                        <Chip
                          label={probe.probeId}
                          size="small"
                          sx={{ fontSize: '0.65rem', height: 18, bgcolor: 'rgba(255, 255, 255, 0.05)', color: '#94A3B8' }}
                        />
                      </Box>
                      <Typography variant="body2" sx={{ color: '#94A3B8', mt: 0.5 }}>
                        {probe.details}
                      </Typography>
                    </Box>
                  </Box>

                  <Typography variant="caption" sx={{ color: '#64748B', fontFamily: 'monospace' }}>
                    {probe.latencyMs}ms
                  </Typography>
                </Box>
              ))}
            </Box>
          </DialogContent>

          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setReportModalOpen(false)} sx={{ color: '#94A3B8' }}>
              Close
            </Button>
          </DialogActions>
        </Dialog>

        {/* Manage Capabilities Modal */}
        <Dialog
          open={capModalOpen}
          onClose={() => setCapModalOpen(false)}
          maxWidth="sm"
          fullWidth
          PaperProps={{
            sx: { bgcolor: '#0B1120', border: '1px solid rgba(255, 255, 255, 0.1)', borderRadius: 3 },
          }}
        >
          <DialogTitle sx={{ color: '#F9FAFB', fontWeight: 700 }}>
            Manage Capabilities: {selectedConnectorForCap?.displayName}
          </DialogTitle>

          <DialogContent dividers sx={{ borderColor: 'rgba(255, 255, 255, 0.08)' }}>
            <Typography variant="body2" sx={{ color: '#94A3B8', mb: 3 }}>
              Configure autonomous agent permissions. High-consequence actions should remain gated behind CEO/Admin approval.
            </Typography>

            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
              {Object.entries(editableCapabilities).map(([capKey, mode]) => (
                <Box
                  key={capKey}
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    p: 1.5,
                    borderRadius: 2,
                    bgcolor: 'rgba(15, 23, 42, 0.6)',
                  }}
                >
                  <Typography variant="subtitle2" sx={{ color: '#E2E8F0', fontWeight: 600 }}>
                    {capKey.replace(/_/g, ' ')}
                  </Typography>

                  <FormControl size="small" sx={{ minWidth: 160 }}>
                    <Select
                      value={mode}
                      onChange={(e) =>
                        setEditableCapabilities({
                          ...editableCapabilities,
                          [capKey]: Number(e.target.value),
                        })
                      }
                      sx={{
                        color: mode === 2 ? '#34D399' : mode === 1 ? '#FBBF24' : '#F87171',
                        bgcolor: 'rgba(0, 0, 0, 0.4)',
                        '.MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(255, 255, 255, 0.1)' },
                      }}
                    >
                      <MenuItem value={2}>Allowed</MenuItem>
                      <MenuItem value={1}>Require Approval</MenuItem>
                      <MenuItem value={0}>Denied</MenuItem>
                    </Select>
                  </FormControl>
                </Box>
              ))}
            </Box>
          </DialogContent>

          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setCapModalOpen(false)} sx={{ color: '#94A3B8' }}>
              Cancel
            </Button>
            <Button
              variant="contained"
              onClick={handleSaveCapabilities}
              sx={{ bgcolor: '#3B82F6', color: '#FFF', fontWeight: 600 }}
            >
              Save Permissions
            </Button>
          </DialogActions>
        </Dialog>

        {/* Configure Keys Modal */}
        <Dialog
          open={configModalOpen}
          onClose={() => setConfigModalOpen(false)}
          maxWidth="sm"
          fullWidth
          PaperProps={{
            sx: { bgcolor: '#0B1120', border: '1px solid rgba(255, 255, 255, 0.1)', borderRadius: 3 },
          }}
        >
          <DialogTitle sx={{ color: '#F9FAFB', fontWeight: 700 }}>
            Configure Credentials: {selectedConnectorForConfig?.displayName}
          </DialogTitle>

          <DialogContent dividers sx={{ borderColor: 'rgba(255, 255, 255, 0.08)' }}>
            <Alert severity="warning" sx={{ mb: 3, bgcolor: 'rgba(245, 158, 11, 0.1)', color: '#FDE68A' }}>
              Credentials are encrypted at rest with AES-256-GCM using your tenant key salt and will never be displayed again.
            </Alert>

            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <TextField
                label="API Key / Token"
                type="password"
                fullWidth
                value={apiKeyInput}
                onChange={(e) => setApiKeyInput(e.target.value)}
                sx={{ input: { color: '#FFF' }, label: { color: '#94A3B8' } }}
              />

              <TextField
                label="API Secret (Optional)"
                type="password"
                fullWidth
                value={apiSecretInput}
                onChange={(e) => setApiSecretInput(e.target.value)}
                sx={{ input: { color: '#FFF' }, label: { color: '#94A3B8' } }}
              />

              <TextField
                label="Account / Merchant ID"
                fullWidth
                value={accountIdentifierInput}
                onChange={(e) => setAccountIdentifierInput(e.target.value)}
                helperText="e.g. merchant_bitbloom_in or your provider username"
                sx={{ input: { color: '#FFF' }, label: { color: '#94A3B8' }, '.MuiFormHelperText-root': { color: '#64748B' } }}
              />
            </Box>
          </DialogContent>

          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setConfigModalOpen(false)} sx={{ color: '#94A3B8' }}>
              Cancel
            </Button>
            <Button
              variant="contained"
              onClick={handleSaveConfig}
              disabled={!apiKeyInput}
              sx={{ bgcolor: '#3B82F6', color: '#FFF', fontWeight: 600 }}
            >
              Encrypt & Save Vault
            </Button>
          </DialogActions>
        </Dialog>
      </Box>
    </Layout>
  );
};

export default ConnectPage;
