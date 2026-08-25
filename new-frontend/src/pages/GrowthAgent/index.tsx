import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Stack,
  Chip,
  Dialog,
  DialogContent,
  DialogActions,
  IconButton,
} from '@mui/material';
import SmartToy from '@mui/icons-material/SmartToy';
import Mic from '@mui/icons-material/Mic';
import PlayArrow from '@mui/icons-material/PlayArrow';
import CheckCircle from '@mui/icons-material/CheckCircle';
import Close from '@mui/icons-material/Close';
import AutoAwesome from '@mui/icons-material/AutoAwesome';
import { Layout } from '../../components/Layout/Layout';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { ErrorBoundary } from '../../components/ErrorBoundary';

interface MissionStep {
  id: number;
  label: string;
  detail: string;
  status: 'pending' | 'in_progress' | 'completed';
}

export const GrowthAgent: React.FC = () => {
  const [isMissionRunning, setIsMissionRunning] = useState(false);
  const [missionStep, setMissionStep] = useState(0);
  const [voiceModalOpen, setVoiceModalOpen] = useState(false);
  const [isListening, setIsListening] = useState(false);
  const [voiceTranscript, setVoiceTranscript] = useState('');

  const steps: MissionStep[] = [
    { id: 1, label: 'Scanning Commercial Context', detail: 'Evaluating 14 active opportunities and 42 inbound leads.', status: missionStep >= 1 ? 'completed' : isMissionRunning && missionStep === 0 ? 'in_progress' : 'pending' },
    { id: 2, label: 'Lead Qualification Inference', detail: 'Identified 3 high-intent enterprise contacts ready for discovery.', status: missionStep >= 2 ? 'completed' : isMissionRunning && missionStep === 1 ? 'in_progress' : 'pending' },
    { id: 3, label: 'Synthesizing Action Recommendations', detail: 'Prepared follow-up proposal with 7% volume discount for Acme Corp.', status: missionStep >= 3 ? 'completed' : isMissionRunning && missionStep === 2 ? 'in_progress' : 'pending' },
    { id: 4, label: 'Consequential Approval Gate', detail: 'Submitted pricing proposal to Human Approval Queue in Control Center.', status: missionStep >= 4 ? 'completed' : isMissionRunning && missionStep === 3 ? 'in_progress' : 'pending' },
  ];

  const handleStartMission = () => {
    setIsMissionRunning(true);
    setMissionStep(0);

    const interval = setInterval(() => {
      setMissionStep((prev) => {
        if (prev >= 4) {
          clearInterval(interval);
          setIsMissionRunning(false);
          return 4;
        }
        return prev + 1;
      });
    }, 1200);
  };

  const handleStartVoice = () => {
    setVoiceModalOpen(true);
    setIsListening(true);
    setVoiceTranscript('Listening to your instructions...');

    setTimeout(() => {
      setVoiceTranscript('"How are our high-value opportunities performing this quarter?"');
    }, 1500);

    setTimeout(() => {
      setIsListening(false);
      setVoiceTranscript('Growth Agent: "Your top 3 opportunities represent ₹27.4L in weighted forecast. Acme Corp requires attention due to 17 days of inactivity."');
    }, 3500);
  };

  return (
    <ErrorBoundary>
      <Layout>
        <Box sx={{ maxWidth: 1200, mx: 'auto' }}>
          {/* Header */}
          <Box sx={{ mb: 4, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
            <Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
                <SmartToy sx={{ color: '#00F0FF', fontSize: 28 }} />
                <Typography variant="h1" sx={{ fontSize: '1.875rem' }}>
                  GROWTH AGENT
                </Typography>
                <Chip
                  label="Ready"
                  size="small"
                  sx={{
                    backgroundColor: 'rgba(16, 185, 129, 0.15)',
                    color: '#10B981',
                    border: '1px solid rgba(16, 185, 129, 0.3)',
                    fontWeight: 600,
                  }}
                />
              </Box>
              <Typography variant="body1" color="text.secondary">
                Autonomous commercial execution engine governed by OmniRoute AI policies.
              </Typography>
            </Box>

            <Button
              variant="outlined"
              startIcon={<Mic />}
              onClick={handleStartVoice}
              sx={{ borderColor: '#00F0FF', color: '#00F0FF' }}
            >
              Talk to Growth Agent
            </Button>
          </Box>

          {/* Today's Mission Briefing Card */}
          <Card sx={{ mb: 4, backgroundColor: '#0D1118', border: '1px solid rgba(0, 240, 255, 0.25)' }}>
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Box>
                  <Typography variant="h5" fontWeight="bold">
                    Today's Commercial Mission
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Prioritized autonomous commercial tasks for workspace growth.
                  </Typography>
                </Box>

                <Button
                  variant="contained"
                  startIcon={<PlayArrow />}
                  disabled={isMissionRunning}
                  onClick={handleStartMission}
                >
                  {isMissionRunning ? 'Executing Mission...' : 'Start Mission'}
                </Button>
              </Box>

              <Grid container spacing={2} sx={{ my: 1 }}>
                <Grid item xs={12} sm={4}>
                  <Box sx={{ p: 2, borderRadius: 1.5, backgroundColor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)' }}>
                    <Typography variant="caption" color="text.secondary">
                      LEAD QUALIFICATION
                    </Typography>
                    <Typography variant="h4" fontWeight="bold" sx={{ color: '#00F0FF', my: 0.5 }}>
                      12 Leads
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Pending automated scoring
                    </Typography>
                  </Box>
                </Grid>

                <Grid item xs={12} sm={4}>
                  <Box sx={{ p: 2, borderRadius: 1.5, backgroundColor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)' }}>
                    <Typography variant="caption" color="text.secondary">
                      OPPORTUNITY FOLLOW-UP
                    </Typography>
                    <Typography variant="h4" fontWeight="bold" sx={{ color: '#38BDF8', my: 0.5 }}>
                      4 Deals
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Exceeding 7 days in stage
                    </Typography>
                  </Box>
                </Grid>

                <Grid item xs={12} sm={4}>
                  <Box sx={{ p: 2, borderRadius: 1.5, backgroundColor: '#111722', border: '1px solid rgba(255, 255, 255, 0.06)' }}>
                    <Typography variant="caption" color="text.secondary">
                      AT-RISK INTERVENTIONS
                    </Typography>
                    <Typography variant="h4" fontWeight="bold" sx={{ color: '#EF4444', my: 0.5 }}>
                      2 Blockers
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Pending SLA reviews
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </CardContent>
          </Card>

          {/* Mission Execution Pipeline Tracker */}
          <Card sx={{ mb: 4 }}>
            <CardContent sx={{ p: 3 }}>
              <Typography variant="h5" fontWeight="bold" sx={{ mb: 2 }}>
                Agent Execution Pipeline
              </Typography>

              <Stack spacing={2}>
                {steps.map((step) => (
                  <Box
                    key={step.id}
                    sx={{
                      p: 2,
                      borderRadius: 1.5,
                      backgroundColor: '#111722',
                      border: `1px solid ${
                        step.status === 'completed'
                          ? 'rgba(16, 185, 129, 0.3)'
                          : step.status === 'in_progress'
                          ? 'rgba(0, 240, 255, 0.4)'
                          : 'rgba(255, 255, 255, 0.06)'
                      }`,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      flexWrap: 'wrap',
                      gap: 1.5,
                    }}
                  >
                    <Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
                        <Typography variant="subtitle1" fontWeight="bold">
                          {step.id}. {step.label}
                        </Typography>
                        {step.status === 'completed' && (
                          <StatusBadge type="approved" customLabel="Completed" />
                        )}
                        {step.status === 'in_progress' && (
                          <StatusBadge type="interpretation" customLabel="Analyzing..." />
                        )}
                      </Box>
                      <Typography variant="body2" color="text.secondary">
                        {step.detail}
                      </Typography>
                    </Box>

                    {step.status === 'completed' ? (
                      <CheckCircle sx={{ color: '#10B981' }} />
                    ) : step.status === 'in_progress' ? (
                      <AutoAwesome sx={{ color: '#00F0FF', animation: 'spin 2s linear infinite' }} />
                    ) : null}
                  </Box>
                ))}
              </Stack>
            </CardContent>
          </Card>
        </Box>

        {/* Voice AI Interaction Modal */}
        <Dialog
          open={voiceModalOpen}
          onClose={() => setVoiceModalOpen(false)}
          PaperProps={{
            sx: {
              backgroundColor: '#070A0F',
              border: '1px solid rgba(0, 240, 255, 0.4)',
              boxShadow: '0 0 50px rgba(0, 240, 255, 0.2)',
              minWidth: 440,
              textAlign: 'center',
              p: 3,
            },
          }}
        >
          <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
            <IconButton onClick={() => setVoiceModalOpen(false)} sx={{ color: 'text.secondary' }}>
              <Close fontSize="small" />
            </IconButton>
          </Box>

          <DialogContent>
            {/* Glowing HUD Circle */}
            <Box
              sx={{
                width: 90,
                height: 90,
                borderRadius: '50%',
                backgroundColor: 'rgba(0, 240, 255, 0.1)',
                border: '2px solid #00F0FF',
                boxShadow: isListening ? '0 0 30px #00F0FF' : '0 0 10px #00F0FF',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                mx: 'auto',
                mb: 3,
                animation: isListening ? 'pulse 1.5s ease-in-out infinite' : 'none',
              }}
            >
              <Mic sx={{ fontSize: 36, color: '#00F0FF' }} />
            </Box>

            <Typography variant="h5" fontWeight="bold" sx={{ color: '#F8FAFC', mb: 1 }}>
              {isListening ? 'Growth Agent Listening...' : 'Growth Agent Response'}
            </Typography>

            <Typography variant="body1" sx={{ color: '#94A3B8', minHeight: 60 }}>
              {voiceTranscript}
            </Typography>
          </DialogContent>
          <DialogActions sx={{ justifyContent: 'center' }}>
            <Button variant="outlined" onClick={() => setVoiceModalOpen(false)}>
              End Conversation
            </Button>
          </DialogActions>
        </Dialog>
      </Layout>
    </ErrorBoundary>
  );
};

export default GrowthAgent;
