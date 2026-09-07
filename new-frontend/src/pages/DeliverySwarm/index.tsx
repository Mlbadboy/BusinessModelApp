import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Chip,
  LinearProgress,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import ConstructionIcon from '@mui/icons-material/Construction';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import RadioButtonUncheckedIcon from '@mui/icons-material/RadioButtonUnchecked';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import LaunchIcon from '@mui/icons-material/Launch';
import CodeIcon from '@mui/icons-material/Code';
import api from '../../utils/api';

interface DeliveryTaskItem {
  id: string;
  role: string;
  title: string;
  artifactName: string;
  isCompleted: boolean;
  completedAt: string | null;
}

interface DeliveryMission {
  id: string;
  projectTitle: string;
  clientName: string;
  projectValueINR: number;
  currentPhase: number;
  overallProgressPercentage: number;
  tasks: DeliveryTaskItem[];
  liveDeploymentUrl: string;
  createdAt: string;
  completedAt: string | null;
}

const PHASE_NAMES = [
  'Requirements Gathering',
  'UX/UI Design & Prototyping',
  'Core Engineering (Next.js/React)',
  'QA & Security Audit',
  'Production Edge Deployment',
  'Customer Handover & Success',
  'Project Complete'
];

export const DeliverySwarmPage: React.FC = () => {
  const [missions, setMissions] = useState<DeliveryMission[]>([]);
  const [, setLoading] = useState<boolean>(true);
  const [steppingId, setSteppingId] = useState<string | null>(null);

  useEffect(() => {
    fetchMissions();
  }, []);

  const fetchMissions = async () => {
    setLoading(true);
    try {
      const res = await api.get('/deliveryswarm/missions');
      setMissions(res.data.missions || []);
    } catch (err) {
      console.error('Failed to load delivery missions', err);
    } finally {
      setLoading(false);
    }
  };

  const handleStepMission = async (missionId: string) => {
    setSteppingId(missionId);
    try {
      await api.post(`/deliveryswarm/missions/${missionId}/step`);
      await fetchMissions();
    } catch (err) {
      console.error('Failed to execute delivery step', err);
    } finally {
      setSteppingId(null);
    }
  };

  const getRoleColor = (role: string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
    if (role.includes('Requirements')) return 'info';
    if (role.includes('UX')) return 'secondary';
    if (role.includes('Frontend') || role.includes('Engineer')) return 'primary';
    if (role.includes('QA')) return 'warning';
    if (role.includes('DevOps')) return 'success';
    return 'default';
  };

  return (
    <Box sx={{ p: 4 }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
          <ConstructionIcon sx={{ fontSize: 32, color: 'primary.main' }} />
          <Typography variant="h4" fontWeight={700} sx={{ letterSpacing: '-0.5px' }}>
            Autonomous Delivery Agent Swarm
          </Typography>
        </Box>
        <Typography variant="body1" color="text.secondary">
          Once client payment is verified via Razorpay/Stripe, Charlie automatically initializes a specialized delivery swarm to build, test, and deploy the sold solution.
        </Typography>
      </Box>

      {/* Active Missions Grid */}
      <Grid container spacing={3}>
        {missions.map((mission) => (
          <Grid item xs={12} key={mission.id}>
            <Card sx={{ p: 3, border: '1px solid', borderColor: 'divider' }}>
              <CardContent sx={{ p: 0 }}>
                {/* Header */}
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                  <Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
                      <Typography variant="h5" fontWeight={700}>
                        {mission.projectTitle}
                      </Typography>
                      <Chip label={`₹${(mission.projectValueINR / 100000).toFixed(2)} Lakhs (Paid)`} color="success" size="small" />
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      Client: <strong>{mission.clientName}</strong> • Phase: <strong>{PHASE_NAMES[mission.currentPhase] || 'In Progress'}</strong>
                    </Typography>
                  </Box>

                  {mission.liveDeploymentUrl && (
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<LaunchIcon />}
                      href={mission.liveDeploymentUrl}
                      target="_blank"
                    >
                      View Live Preview
                    </Button>
                  )}
                </Box>

                {/* Progress Bar */}
                <Box sx={{ mb: 3 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                    <Typography variant="caption" color="text.secondary">Overall Delivery Progress</Typography>
                    <Typography variant="caption" fontWeight={700}>{mission.overallProgressPercentage}%</Typography>
                  </Box>
                  <LinearProgress
                    variant="determinate"
                    value={mission.overallProgressPercentage}
                    sx={{ height: 8, borderRadius: 1 }}
                    color={mission.overallProgressPercentage >= 100 ? 'success' : 'primary'}
                  />
                </Box>

                <Divider sx={{ my: 2 }} />

                {/* Tasks List */}
                <Typography variant="subtitle2" fontWeight={700} sx={{ mb: 1.5 }}>
                  Swarm Execution DAG & Deliverable Artifacts:
                </Typography>
                <List dense disablePadding>
                  {mission.tasks.map((task) => (
                    <ListItem
                      key={task.id}
                      sx={{
                        bgcolor: task.isCompleted ? 'rgba(16, 185, 129, 0.04)' : 'transparent',
                        borderRadius: 1,
                        mb: 0.5
                      }}
                    >
                      <ListItemIcon sx={{ minWidth: 36 }}>
                        {task.isCompleted ? (
                          <CheckCircleIcon sx={{ color: '#10b981', fontSize: 20 }} />
                        ) : (
                          <RadioButtonUncheckedIcon sx={{ color: 'text.disabled', fontSize: 20 }} />
                        )}
                      </ListItemIcon>
                      <ListItemText
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Chip label={task.role} size="small" color={getRoleColor(task.role)} variant="outlined" />
                            <Typography variant="body2" fontWeight={600}>
                              {task.title}
                            </Typography>
                          </Box>
                        }
                        secondary={
                          task.artifactName ? (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mt: 0.25 }}>
                              <CodeIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
                              <Typography variant="caption" color="primary.main">
                                Artifact: {task.artifactName}
                              </Typography>
                              {task.completedAt && (
                                <Typography variant="caption" color="text.secondary">
                                  • Completed {new Date(task.completedAt).toLocaleTimeString()}
                                </Typography>
                              )}
                            </Box>
                          ) : null
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </CardContent>

              <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
                <Button
                  variant="contained"
                  startIcon={<PlayArrowIcon />}
                  disabled={mission.overallProgressPercentage >= 100 || steppingId === mission.id}
                  onClick={() => handleStepMission(mission.id)}
                >
                  {mission.overallProgressPercentage >= 100
                    ? 'Delivery Complete'
                    : steppingId === mission.id
                    ? 'Executing Swarm Step...'
                    : 'Execute Next Autonomous Delivery Step'}
                </Button>
              </Box>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default DeliverySwarmPage;
