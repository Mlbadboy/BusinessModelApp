import React, { useState } from 'react';
import {
  Box,
  Typography,
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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
} from '@mui/material';
import AutoAwesome from '@mui/icons-material/AutoAwesome';
import Add from '@mui/icons-material/Add';
import { Layout } from '../../components/Layout/Layout';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { useCommercial, LeadDto } from '../../hooks/useCommercial';
import { LoadingState } from '../../components/LoadingState';
import { ErrorBoundary } from '../../components/ErrorBoundary';

const LEAD_STATUS_LABELS: Record<number, string> = {
  0: 'New',
  1: 'Contacted',
  2: 'Qualified',
  3: 'Unqualified',
  4: 'Converted',
};

export const Leads: React.FC = () => {
  const { leads, isLoading, createLead, scoreLeadWithAI } = useCommercial();

  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [contactName, setContactName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [companyName, setCompanyName] = useState('');
  const [scoringLeadId, setScoringLeadId] = useState<string | null>(null);

  const handleScoreLead = async (leadId: string) => {
    setScoringLeadId(leadId);
    try {
      await scoreLeadWithAI.mutateAsync(leadId);
    } finally {
      setScoringLeadId(null);
    }
  };

  const handleCreateSubmit = async () => {
    if (!contactName.trim()) return;
    await createLead.mutateAsync({
      contactName,
      email,
      phone,
      companyName,
      source: 0,
    });
    setCreateModalOpen(false);
    setContactName('');
    setEmail('');
    setPhone('');
    setCompanyName('');
  };

  if (isLoading) {
    return (
      <Layout>
        <LoadingState message="Loading Inbound Leads..." />
      </Layout>
    );
  }

  const leadList = leads || [];

  return (
    <ErrorBoundary>
      <Layout>
        <Box sx={{ maxWidth: 1400, mx: 'auto' }}>
          {/* Header */}
          <Box sx={{ mb: 3.5, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.75rem', mb: 0.5 }}>
                Leads Command Center
              </Typography>
              <Typography variant="body1" color="text.secondary">
                Inbound commercial inquiries with automated AI intent qualification.
              </Typography>
            </Box>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setCreateModalOpen(true)}
            >
              New Lead
            </Button>
          </Box>

          <Card>
            <CardContent sx={{ p: 3 }}>
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Contact Name</TableCell>
                      <TableCell>Company</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>AI Quality Score</TableCell>
                      <TableCell>Contact Details</TableCell>
                      <TableCell align="right">Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {leadList.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                          <Typography color="text.secondary">No inbound leads recorded yet.</Typography>
                        </TableCell>
                      </TableRow>
                    ) : (
                      leadList.map((lead: LeadDto) => (
                        <TableRow key={lead.id} hover>
                          <TableCell sx={{ fontWeight: 600, color: '#F8FAFC' }}>
                            {lead.contactName}
                          </TableCell>
                          <TableCell>{lead.companyName || 'Enterprise'}</TableCell>
                          <TableCell>
                            <Chip
                              label={LEAD_STATUS_LABELS[lead.status] || 'New'}
                              size="small"
                              sx={{
                                backgroundColor: lead.status === 2 ? 'rgba(16, 185, 129, 0.12)' : 'rgba(255, 255, 255, 0.06)',
                                color: lead.status === 2 ? '#10B981' : '#94A3B8',
                              }}
                            />
                          </TableCell>
                          <TableCell>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Typography variant="body2" fontWeight="bold" sx={{ color: lead.qualityScore > 70 ? '#00F0FF' : '#94A3B8', fontVariantNumeric: 'tabular-nums' }}>
                                {lead.qualityScore ? `${Math.round(lead.qualityScore)} / 100` : 'Not Scored'}
                              </Typography>
                              {lead.qualityScore > 75 && (
                                <StatusBadge type="fact" customLabel="High Intent" sx={{ height: 20 }} />
                              )}
                            </Box>
                          </TableCell>
                          <TableCell>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                              {lead.email || 'No email'}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {lead.phone || 'No phone'}
                            </Typography>
                          </TableCell>
                          <TableCell align="right">
                            <Button
                              size="small"
                              variant="outlined"
                              startIcon={scoringLeadId === lead.id ? <CircularProgress size={14} /> : <AutoAwesome />}
                              disabled={scoringLeadId === lead.id}
                              onClick={() => handleScoreLead(lead.id)}
                            >
                              AI Qualify
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        </Box>

        {/* Create Lead Modal */}
        <Dialog
          open={createModalOpen}
          onClose={() => setCreateModalOpen(false)}
          PaperProps={{
            sx: {
              backgroundColor: '#0D1118',
              border: '1px solid rgba(255, 255, 255, 0.1)',
              minWidth: 400,
            },
          }}
        >
          <DialogTitle sx={{ color: '#F8FAFC', fontWeight: 'bold' }}>Create Inbound Lead</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Contact Name"
                placeholder="e.g. Sarah Connor"
                fullWidth
                size="small"
                value={contactName}
                onChange={(e) => setContactName(e.target.value)}
              />
              <TextField
                label="Company Name"
                placeholder="e.g. Cyberdyne Systems"
                fullWidth
                size="small"
                value={companyName}
                onChange={(e) => setCompanyName(e.target.value)}
              />
              <TextField
                label="Email"
                placeholder="sarah@cyberdyne.com"
                fullWidth
                size="small"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
              <TextField
                label="Phone"
                placeholder="+1 555 0199"
                fullWidth
                size="small"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ p: 2 }}>
            <Button onClick={() => setCreateModalOpen(false)} sx={{ color: 'text.secondary' }}>
              Cancel
            </Button>
            <Button variant="contained" onClick={handleCreateSubmit}>
              Create Lead
            </Button>
          </DialogActions>
        </Dialog>
      </Layout>
    </ErrorBoundary>
  );
};

export default Leads;
