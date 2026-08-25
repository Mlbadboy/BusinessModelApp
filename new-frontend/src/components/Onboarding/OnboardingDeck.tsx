import React from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Chip,
} from '@mui/material';
import HubIcon from '@mui/icons-material/Hub';
import EmailIcon from '@mui/icons-material/Email';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import StorageIcon from '@mui/icons-material/Storage';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch';

interface OnboardingDeckProps {
  onLaunchSimulation: () => void;
}

export const OnboardingDeck: React.FC<OnboardingDeckProps> = ({ onLaunchSimulation }) => {
  return (
    <Box sx={{ p: 4, maxWidth: 1200, margin: '0 auto' }}>
      {/* HUD Header */}
      <Box
        sx={{
          p: 4,
          borderRadius: 2,
          border: '1px solid rgba(0, 229, 255, 0.25)',
          background: 'linear-gradient(135deg, rgba(10, 16, 26, 0.95), rgba(15, 23, 42, 0.85))',
          mb: 4,
          boxShadow: '0 0 30px rgba(0, 229, 255, 0.1)',
        }}
      >
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box
              sx={{
                width: 48,
                height: 48,
                borderRadius: '50%',
                bgcolor: 'rgba(0, 229, 255, 0.1)',
                border: '1px solid #00E5FF',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#00E5FF',
              }}
            >
              <HubIcon fontSize="large" />
            </Box>
            <Box>
              <Typography variant="h4" sx={{ fontWeight: 800, color: '#F8FAFC', letterSpacing: '-0.02em' }}>
                WELCOME TO BUSINESSMODELAPP
              </Typography>
              <Typography variant="body2" sx={{ color: '#94A3B8' }}>
                Autonomous Revenue Operating System • Zero commercial data detected in this workspace
              </Typography>
            </Box>
          </Box>
          <Chip
            label="STATUS: IDLE & READY"
            sx={{
              bgcolor: 'rgba(0, 229, 255, 0.1)',
              color: '#00E5FF',
              border: '1px solid rgba(0, 229, 255, 0.3)',
              fontWeight: 700,
            }}
          />
        </Box>

        <Typography variant="body1" sx={{ color: '#CBD5E1', maxWidth: 800, mb: 3 }}>
          To begin generating verified revenue in <strong>Live Autonomy Mode</strong>, connect your enterprise data sources. Alternatively, test the complete multi-agent revenue loop immediately in <strong>Simulation Mode</strong>.
        </Typography>

        <Button
          variant="contained"
          size="large"
          startIcon={<RocketLaunchIcon />}
          onClick={onLaunchSimulation}
          sx={{
            background: 'linear-gradient(135deg, #00E5FF, #00B0FF)',
            color: '#0A0E17',
            fontWeight: 800,
            px: 4,
            py: 1.5,
            boxShadow: '0 0 20px rgba(0, 229, 255, 0.4)',
            '&:hover': { background: '#00E5FF' },
          }}
        >
          Launch Enterprise Simulation Mission
        </Button>
      </Box>

      {/* Connection Grid */}
      <Typography variant="h6" sx={{ fontWeight: 700, color: '#F8FAFC', mb: 2 }}>
        Enterprise Connectors
      </Typography>

      <Grid container spacing={3}>
        {[
          { icon: <StorageIcon />, title: 'CRM & Pipeline', desc: 'Sync leads, accounts, and historical deal outcomes.', status: 'Ready to Connect' },
          { icon: <EmailIcon />, title: 'Business Email', desc: 'Governed outbound messaging and intent detection.', status: 'Ready to Connect' },
          { icon: <CalendarMonthIcon />, title: 'Executive Calendar', desc: 'Autonomous discovery meeting scheduling.', status: 'Ready to Connect' },
          { icon: <MenuBookIcon />, title: 'Product & Pricing Catalog', desc: 'Catalog bounds for deterministic proposals.', status: 'Ready to Connect' },
        ].map((item, idx) => (
          <Grid item xs={12} sm={6} md={3} key={idx}>
            <Card
              sx={{
                bgcolor: '#0F172A',
                border: '1px solid rgba(255, 255, 255, 0.08)',
                borderRadius: 2,
                height: '100%',
                transition: 'all 0.2s ease',
                '&:hover': { borderColor: 'rgba(0, 229, 255, 0.4)', transform: 'translateY(-2px)' },
              }}
            >
              <CardContent sx={{ p: 3 }}>
                <Box sx={{ color: '#00E5FF', mb: 1.5 }}>{item.icon}</Box>
                <Typography variant="subtitle1" sx={{ fontWeight: 700, color: '#F8FAFC', mb: 0.5 }}>
                  {item.title}
                </Typography>
                <Typography variant="body2" sx={{ color: '#94A3B8', mb: 2, fontSize: '0.85rem' }}>
                  {item.desc}
                </Typography>
                <Button size="small" variant="outlined" sx={{ color: '#00E5FF', borderColor: 'rgba(0, 229, 255, 0.3)', width: '100%' }}>
                  Connect
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};
