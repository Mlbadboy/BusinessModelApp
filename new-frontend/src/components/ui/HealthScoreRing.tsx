import React from 'react';
import { Box, Typography, Grid, Tooltip, IconButton } from '@mui/material';
import HelpOutline from '@mui/icons-material/HelpOutline';
import TrendingUp from '@mui/icons-material/TrendingUp';

export interface HealthDimension {
  id: string;
  name: string;
  score: number;
  weight: string;
  color: string;
}

interface HealthScoreRingProps {
  score: number;
  delta?: string;
  confidenceScore?: number;
  dimensions: HealthDimension[];
  onSelectDimension: (dimensionId: string) => void;
}

export const HealthScoreRing: React.FC<HealthScoreRingProps> = ({
  score,
  delta = '+6.2%',
  confidenceScore = 0.94,
  dimensions,
  onSelectDimension,
}) => {
  // SVG circular gauge geometry
  const radius = 64;
  const strokeWidth = 8;
  const circumference = 2 * Math.PI * radius;
  const strokeDashoffset = circumference - (score / 100) * circumference;

  const getScoreColor = (val: number) => {
    if (val >= 75) return '#00F0FF'; // Electric Cyan
    if (val >= 50) return '#38BDF8'; // Sky Blue
    if (val >= 35) return '#F59E0B'; // Amber
    return '#EF4444'; // Red
  };

  const scoreColor = getScoreColor(score);

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: { xs: 'column', md: 'row' },
        alignItems: 'center',
        gap: { xs: 3, md: 5 },
      }}
    >
      {/* Central Radial HUD Ring */}
      <Box sx={{ position: 'relative', width: 170, height: 170, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <svg width="170" height="170" style={{ transform: 'rotate(-90deg)' }}>
          {/* Background Track */}
          <circle
            cx="85"
            cy="85"
            r={radius}
            stroke="rgba(255, 255, 255, 0.08)"
            strokeWidth={strokeWidth}
            fill="transparent"
          />
          {/* Active Progress */}
          <circle
            cx="85"
            cy="85"
            r={radius}
            stroke={scoreColor}
            strokeWidth={strokeWidth}
            strokeDasharray={circumference}
            strokeDashoffset={strokeDashoffset}
            strokeLinecap="round"
            fill="transparent"
            style={{
              transition: 'stroke-dashoffset 1s ease-in-out',
              filter: `drop-shadow(0 0 8px ${scoreColor}88)`,
            }}
          />
        </svg>

        {/* Center Label */}
        <Box sx={{ position: 'absolute', textAlign: 'center' }}>
          <Typography
            variant="h1"
            sx={{
              fontSize: '2.5rem',
              fontWeight: 800,
              letterSpacing: '-0.04em',
              color: '#F8FAFC',
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            {Math.round(score)}
          </Typography>
          <Typography variant="caption" sx={{ color: '#94A3B8', textTransform: 'uppercase', letterSpacing: '0.1em', display: 'block' }}>
            {(confidenceScore * 100).toFixed(0)}% CONFIDENCE
          </Typography>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 0.5, mt: 0.5 }}>
            <TrendingUp sx={{ fontSize: 13, color: '#10B981' }} />
            <Typography variant="caption" sx={{ color: '#10B981', fontWeight: 700 }}>
              {delta}
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* Dimensions Breakdown Grid */}
      <Box sx={{ flex: 1, width: '100%' }}>
        <Typography variant="h6" color="text.secondary" sx={{ mb: 2 }}>
          Operating Dimensions & Explainability
        </Typography>

        <Grid container spacing={1.5}>
          {dimensions.map((dim) => (
            <Grid item xs={12} sm={6} key={dim.id}>
              <Box
                onClick={() => onSelectDimension(dim.id)}
                sx={{
                  p: 1.5,
                  borderRadius: 1.5,
                  backgroundColor: '#111722',
                  border: '1px solid rgba(255, 255, 255, 0.06)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  cursor: 'pointer',
                  transition: 'all 0.2s ease',
                  '&:hover': {
                    borderColor: 'rgba(0, 240, 255, 0.4)',
                    backgroundColor: 'rgba(0, 240, 255, 0.04)',
                    transform: 'translateY(-1px)',
                  },
                }}
              >
                <Box>
                  <Typography variant="body2" fontWeight="600" color="text.primary">
                    {dim.name}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Weight: {dim.weight}
                  </Typography>
                </Box>

                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Typography
                    variant="body1"
                    fontWeight="700"
                    sx={{ color: dim.color, fontVariantNumeric: 'tabular-nums' }}
                  >
                    {dim.score}
                  </Typography>
                  <Tooltip title="View formula & evidence" arrow>
                    <IconButton size="small" sx={{ color: 'text.secondary', p: 0.5 }}>
                      <HelpOutline sx={{ fontSize: 15 }} />
                    </IconButton>
                  </Tooltip>
                </Box>
              </Box>
            </Grid>
          ))}
        </Grid>
      </Box>
    </Box>
  );
};
