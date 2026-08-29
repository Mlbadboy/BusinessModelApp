import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormGroup,
  FormControlLabel,
  Checkbox,
  CircularProgress,
  Divider,
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import LinkOffIcon from '@mui/icons-material/LinkOff';
import SecurityIcon from '@mui/icons-material/Security';
import SpeedIcon from '@mui/icons-material/Speed';
import HubIcon from '@mui/icons-material/Hub';
import api from '../../utils/api';

interface CharlieConnection {
  id: string;
  provider: number;
  providerName: string;
  status: number;
  accountIdentifier: string;
  grantedScopes: string[];
  lastTestedAt: string | null;
  isHealthy: boolean;
  dailyCallQuota: number;
  consumedDailyQuota: number;
}

interface CapabilityRule {
  provider: number;
  canRead: boolean;
  canDraft: boolean;
  canSend: boolean;
  canManageCalendar: boolean;
  canSearchPublicData: boolean;
  canCreateCRMLeads: boolean;
  canCollectPayments: boolean;
  isDeletePermanentlyBlocked: boolean;
  permissionDescription: string;
}

export const CharlieConnectPage: React.FC = () => {
  const [connections, setConnections] = useState<CharlieConnection[]>([]);
  const [capabilities, setCapabilities] = useState<Record<string, CapabilityRule>>({});
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedProvider, setSelectedProvider] = useState<CharlieConnection | null>(null);
  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [testingProvider, setTestingProvider] = useState<number | null>(null);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [connRes, capRes] = await Promise.all([
        api.get('/charlieconnect'),
        api.get('/charlieconnect/capabilities')
      ]);
      setConnections(connRes.data.connections || []);
      setCapabilities(capRes.data.capabilities || {});
    } catch (err) {
      console.error('Failed to load Charlie Connect data', err);
    } finally {
      setLoading(false);
    }
  };

  const handleOpenOAuthModal = (conn: CharlieConnection) => {
    setSelectedProvider(conn);
    setModalOpen(true);
  };

  const handleAuthorizeOAuth = async () => {
    if (!selectedProvider) return;
    try {
      await api.post('/charlieconnect/connect', {
        provider: selectedProvider.provider,
        accountIdentifier: 'mayur@bitbloom.in',
        scopes: ['read', 'draft', 'send', 'calendar', 'payments']
      });
      setModalOpen(false);
      fetchData();
    } catch (err) {
      console.error('OAuth authorization failed', err);
    }
  };

  const handleTestConnection = async (provider: number) => {
    setTestingProvider(provider);
    try {
      await api.post(`/charlieconnect/test/${provider}`);
      await fetchData();
    } catch (err) {
      console.error('Connection test failed', err);
    } finally {
      setTestingProvider(null);
    }
  };

  const getProviderIcon = (name: string) => {
    if (name.includes('Google') || name.includes('Workspace')) return '🌐';
    if (name.includes('Microsoft') || name.includes('Graph')) return '📧';
    if (name.includes('Places')) return '📍';
    if (name.includes('Razorpay') || name.includes('Stripe')) return '💳';
    if (name.includes('Salesforce') || name.includes('HubSpot')) return '💼';
    return '🔗';
  };

  return (
    <Box sx={{ p: 4 }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
          <HubIcon sx={{ fontSize: 32, color: 'primary.main' }} />
          <Typography variant="h4" fontWeight={700} sx={{ letterSpacing: '-0.5px' }}>
            Charlie Connect — Authority & Integration Hub
          </Typography>
        </Box>
        <Typography variant="body1" color="text.secondary">
          Connect Charlie to the tools your organization already uses. Each connector enforces zero-trust authority scopes, daily call quotas, and permanent deletion locks.
        </Typography>
      </Box>

      {/* Security Banner */}
      <Card sx={{ mb: 4, bgcolor: 'rgba(37, 99, 235, 0.05)', border: '1px solid rgba(37, 99, 235, 0.2)' }}>
        <CardContent sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <SecurityIcon color="primary" sx={{ fontSize: 36 }} />
            <Box>
              <Typography variant="subtitle1" fontWeight={700}>
                Zero-Trust Connector & Consent Plane Active
              </Typography>
              <Typography variant="body2" color="text.secondary">
                OAuth 2.0 / OIDC scoped access. Destructive actions (<code style={{ color: '#ef4444' }}>DeleteRecords</code>, <code style={{ color: '#ef4444' }}>Purge</code>) are permanently rejected by the connector kernel.
              </Typography>
            </Box>
          </Box>
          <Chip label="Zero-Delete Enforced" color="success" size="small" />
        </CardContent>
      </Card>

      {/* Connectors Grid */}
      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {connections.map((conn) => {
            const isConnected = conn.status === 2; // ConnectedActive
            const cap = capabilities[conn.provider.toString()] || {};

            return (
              <Grid item xs={12} sm={6} md={4} key={conn.provider}>
                <Card
                  sx={{
                    height: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'space-between',
                    border: '1px solid',
                    borderColor: isConnected ? 'rgba(16, 185, 129, 0.3)' : 'divider',
                    bgcolor: isConnected ? 'rgba(16, 185, 129, 0.02)' : 'background.paper',
                    transition: 'transform 0.2s',
                    '&:hover': { transform: 'translateY(-2px)' }
                  }}
                >
                  <CardContent>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                        <Typography variant="h5">{getProviderIcon(conn.providerName)}</Typography>
                        <Box>
                          <Typography variant="h6" fontWeight={700}>
                            {conn.providerName}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {conn.accountIdentifier}
                          </Typography>
                        </Box>
                      </Box>
                      {isConnected ? (
                        <Chip
                          icon={<CheckCircleIcon sx={{ '&&': { color: '#10b981' } }} />}
                          label="Active"
                          size="small"
                          sx={{ bgcolor: 'rgba(16, 185, 129, 0.1)', color: '#10b981', fontWeight: 600 }}
                        />
                      ) : (
                        <Chip label="Disconnected" size="small" variant="outlined" />
                      )}
                    </Box>

                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2, minHeight: 40 }}>
                      {cap.permissionDescription || 'Standard authorized business API connection with scoped permissions.'}
                    </Typography>

                    <Divider sx={{ my: 1.5 }} />

                    {/* Capability Tags */}
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75, mb: 1 }}>
                      {cap.canRead && <Chip label="Read" size="small" variant="outlined" />}
                      {cap.canDraft && <Chip label="Draft" size="small" variant="outlined" color="primary" />}
                      {cap.canSend && <Chip label="Send" size="small" variant="outlined" color="secondary" />}
                      {cap.canManageCalendar && <Chip label="Calendar" size="small" variant="outlined" color="info" />}
                      {cap.canSearchPublicData && <Chip label="Public Discovery" size="small" variant="outlined" />}
                      {cap.canCreateCRMLeads && <Chip label="CRM Sync" size="small" variant="outlined" color="success" />}
                      {cap.canCollectPayments && <Chip label="Payments (INR)" size="small" variant="outlined" color="warning" />}
                      <Chip label="Delete Blocked" size="small" sx={{ bgcolor: 'rgba(239, 68, 68, 0.1)', color: '#ef4444' }} />
                    </Box>

                    {conn.lastTestedAt && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                        Last health check: {new Date(conn.lastTestedAt).toLocaleTimeString()}
                      </Typography>
                    )}
                  </CardContent>

                  <Box sx={{ p: 2, pt: 0, display: 'flex', gap: 1 }}>
                    {isConnected ? (
                      <>
                        <Button
                          fullWidth
                          size="small"
                          variant="outlined"
                          startIcon={<SpeedIcon />}
                          disabled={testingProvider === conn.provider}
                          onClick={() => handleTestConnection(conn.provider)}
                        >
                          {testingProvider === conn.provider ? 'Testing...' : 'Test Health'}
                        </Button>
                        <Button
                          size="small"
                          color="error"
                          variant="text"
                          startIcon={<LinkOffIcon />}
                          onClick={() => handleOpenOAuthModal(conn)}
                        >
                          Revoke
                        </Button>
                      </>
                    ) : (
                      <Button
                        fullWidth
                        size="small"
                        variant="contained"
                        onClick={() => handleOpenOAuthModal(conn)}
                      >
                        Connect with OAuth 2.0
                      </Button>
                    )}
                  </Box>
                </Card>
              </Grid>
            );
          })}
        </Grid>
      )}

      {/* OAuth Authorization Dialog */}
      <Dialog open={modalOpen} onClose={() => setModalOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 700 }}>
          Authorize {selectedProvider?.providerName} via OAuth 2.0
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Charlie will request granular scopes through Google / Microsoft / Provider standard OAuth consent. Select the capabilities you wish to grant:
          </Typography>

          <FormGroup>
            <FormControlLabel control={<Checkbox defaultChecked />} label="Read business intelligence and communication signals" />
            <FormControlLabel control={<Checkbox defaultChecked />} label="Draft personalized responses & consultative proposals" />
            <FormControlLabel control={<Checkbox defaultChecked />} label="Send outreach within daily rate limits" />
            <FormControlLabel control={<Checkbox defaultChecked />} label="Schedule calendar discovery meetings" />
            <FormControlLabel control={<Checkbox disabled checked />} label="Permanently Block Deletion of Emails, Events & Records (Mandatory)" />
          </FormGroup>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setModalOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleAuthorizeOAuth}>
            Authorize Scopes & Activate
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default CharlieConnectPage;
