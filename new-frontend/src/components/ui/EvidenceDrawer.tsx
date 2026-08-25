import React from 'react';
import {
  Drawer,
  Box,
  Typography,
  IconButton,
  Divider,
  Stack,
  Card,
  CardContent,
} from '@mui/material';
import Close from '@mui/icons-material/Close';
import AutoAwesome from '@mui/icons-material/AutoAwesome';
import Calculate from '@mui/icons-material/Calculate';
import Storage from '@mui/icons-material/Storage';
import VerifiedUser from '@mui/icons-material/VerifiedUser';
import { StatusBadge } from './StatusBadge';

export interface EvidenceData {
  title: string;
  score: number;
  explanation: string;
  formula: string;
  confidenceScore: number;
  evidenceItems: {
    id: string;
    label: string;
    value: string;
  }[];
  underlyingMetrics: {
    label: string;
    value: string;
  }[];
}

interface EvidenceDrawerProps {
  open: boolean;
  onClose: () => void;
  data: EvidenceData | null;
}

export const EvidenceDrawer: React.FC<EvidenceDrawerProps> = ({ open, onClose, data }) => {
  if (!data) return null;

  return (
    <Drawer
      anchor="right"
      open={open}
      onClose={onClose}
      PaperProps={{
        sx: {
          width: { xs: '100%', sm: 460 },
          backgroundColor: '#070A0F',
          borderLeft: '1px solid rgba(255, 255, 255, 0.1)',
          p: 3,
        },
      }}
    >
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <AutoAwesome sx={{ color: '#00F0FF', fontSize: 20 }} />
          <Typography variant="h5" fontWeight="bold">
            Mathematical Evidence
          </Typography>
        </Box>
        <IconButton
          onClick={onClose}
          size="small"
          sx={{ color: 'text.secondary', '&:hover': { color: 'text.primary' } }}
        >
          <Close />
        </IconButton>
      </Box>

      <Stack spacing={3}>
        {/* Metric Header */}
        <Card variant="outlined" sx={{ backgroundColor: '#0D1118', borderColor: 'rgba(0, 240, 255, 0.3)' }}>
          <CardContent>
            <Typography variant="h6" color="text.secondary" gutterBottom>
              {data.title}
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 2 }}>
              <Typography variant="h1" sx={{ color: '#00F0FF', fontVariantNumeric: 'tabular-nums' }}>
                {data.score}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                / 100 Health Rating
              </Typography>
            </Box>
            <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <StatusBadge type="fact" customLabel="Deterministic Formula" />
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                <VerifiedUser sx={{ fontSize: 14, color: '#10B981' }} />
                <Typography variant="caption" color="success.main" fontWeight={600}>
                  {(data.confidenceScore * 100).toFixed(0)}% Confidence
                </Typography>
              </Box>
            </Box>
          </CardContent>
        </Card>

        {/* Why / Explanation */}
        <Box>
          <Typography variant="h6" color="text.secondary" sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
            <AutoAwesome sx={{ fontSize: 16, color: '#00F0FF' }} />
            Intelligence Summary
          </Typography>
          <Typography variant="body1" sx={{ lineHeight: 1.6, color: '#E2E8F0' }}>
            {data.explanation}
          </Typography>
        </Box>

        <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.08)' }} />

        {/* Mathematical Formula */}
        <Box>
          <Typography variant="h6" color="text.secondary" sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Calculate sx={{ fontSize: 16, color: '#38BDF8' }} />
            Governed Formula
          </Typography>
          <Box
            sx={{
              p: 1.5,
              borderRadius: 1,
              backgroundColor: '#111722',
              border: '1px solid rgba(255, 255, 255, 0.08)',
              fontFamily: 'monospace',
              fontSize: '0.8125rem',
              color: '#38BDF8',
            }}
          >
            {data.formula}
          </Box>
        </Box>

        {/* Evidence Linkages */}
        <Box>
          <Typography variant="h6" color="text.secondary" sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Storage sx={{ fontSize: 16, color: '#10B981' }} />
            Verifiable Evidence IDs
          </Typography>
          <Stack spacing={1}>
            {data.evidenceItems.map((item) => (
              <Box
                key={item.id}
                sx={{
                  p: 1.5,
                  borderRadius: 1,
                  backgroundColor: '#0D1118',
                  border: '1px solid rgba(255, 255, 255, 0.06)',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <Box>
                  <Typography variant="caption" sx={{ color: '#00F0FF', fontFamily: 'monospace' }}>
                    {item.id}
                  </Typography>
                  <Typography variant="body2" color="text.primary">
                    {item.label}
                  </Typography>
                </Box>
                <Typography variant="body2" fontWeight="bold" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {item.value}
                </Typography>
              </Box>
            ))}
          </Stack>
        </Box>

        {/* Underlying Data Points */}
        <Box>
          <Typography variant="h6" color="text.secondary" sx={{ mb: 1 }}>
            Underlying Real-Time Inputs
          </Typography>
          <Stack spacing={1}>
            {data.underlyingMetrics.map((m, idx) => (
              <Box key={idx} sx={{ display: 'flex', justifyContent: 'space-between', py: 0.5 }}>
                <Typography variant="body2" color="text.secondary">
                  {m.label}
                </Typography>
                <Typography variant="body2" fontWeight="600" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {m.value}
                </Typography>
              </Box>
            ))}
          </Stack>
        </Box>
      </Stack>
    </Drawer>
  );
};
