import React from 'react';
import { Card, CardContent, Typography, Box, IconButton, Tooltip } from '@mui/material';
import TrendingUp from '@mui/icons-material/TrendingUp';
import TrendingDown from '@mui/icons-material/TrendingDown';
import HelpOutline from '@mui/icons-material/HelpOutline';

interface MetricCardProps {
  title: string;
  value: string | number;
  delta?: {
    value: string;
    isPositive: boolean;
  };
  subtitle?: string;
  onExplain?: () => void;
  accentColor?: string;
}

export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  delta,
  subtitle,
  onExplain,
  accentColor = '#00F0FF',
}) => {
  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        position: 'relative',
        overflow: 'hidden',
        '&::before': {
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          height: '2px',
          backgroundColor: accentColor,
          opacity: 0.8,
        },
      }}
    >
      <CardContent sx={{ p: 2.5, display: 'flex', flexDirection: 'column', height: '100%' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1.5 }}>
          <Typography variant="h6" color="text.secondary">
            {title}
          </Typography>
          {onExplain && (
            <Tooltip title="Explain calculation & evidence" arrow>
              <IconButton
                size="small"
                onClick={onExplain}
                sx={{
                  color: 'rgba(255, 255, 255, 0.4)',
                  p: 0.5,
                  '&:hover': { color: '#00F0FF', backgroundColor: 'rgba(0, 240, 255, 0.08)' },
                }}
              >
                <HelpOutline sx={{ fontSize: 16 }} />
              </IconButton>
            </Tooltip>
          )}
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1.5, mb: 1 }}>
          <Typography
            variant="h2"
            sx={{
              fontVariantNumeric: 'tabular-nums',
              fontWeight: 700,
              letterSpacing: '-0.03em',
            }}
          >
            {value}
          </Typography>

          {delta && (
            <Box
              sx={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 0.25,
                px: 0.75,
                py: 0.25,
                borderRadius: 1,
                fontSize: '0.75rem',
                fontWeight: 600,
                backgroundColor: delta.isPositive
                  ? 'rgba(16, 185, 129, 0.12)'
                  : 'rgba(239, 68, 68, 0.12)',
                color: delta.isPositive ? '#10B981' : '#F87171',
                border: `1px solid ${
                  delta.isPositive ? 'rgba(16, 185, 129, 0.3)' : 'rgba(239, 68, 68, 0.3)'
                }`,
              }}
            >
              {delta.isPositive ? (
                <TrendingUp sx={{ fontSize: 14 }} />
              ) : (
                <TrendingDown sx={{ fontSize: 14 }} />
              )}
              {delta.value}
            </Box>
          )}
        </Box>

        {subtitle && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 'auto' }}>
            {subtitle}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
};
