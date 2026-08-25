import React from 'react';
import { Chip, ChipProps } from '@mui/material';
import CheckCircleOutline from '@mui/icons-material/CheckCircleOutline';
import Psychology from '@mui/icons-material/Psychology';
import LightbulbOutlined from '@mui/icons-material/LightbulbOutlined';
import WarningAmber from '@mui/icons-material/WarningAmber';

export type BadgeType = 
  | 'fact' 
  | 'interpretation' 
  | 'recommendation' 
  | 'approval_pending' 
  | 'approved'
  | 'risk_high'
  | 'risk_medium'
  | 'risk_low';

interface StatusBadgeProps extends Omit<ChipProps, 'color'> {
  type: BadgeType;
  customLabel?: string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ type, customLabel, sx, ...props }) => {
  switch (type) {
    case 'fact':
      return (
        <Chip
          icon={<CheckCircleOutline sx={{ fontSize: '14px !important', color: '#10B981 !important' }} />}
          label={customLabel || 'Verified Fact'}
          size="small"
          sx={{
            backgroundColor: 'rgba(16, 185, 129, 0.12)',
            color: '#10B981',
            border: '1px solid rgba(16, 185, 129, 0.3)',
            ...sx,
          }}
          {...props}
        />
      );

    case 'interpretation':
      return (
        <Chip
          icon={<Psychology sx={{ fontSize: '14px !important', color: '#38BDF8 !important' }} />}
          label={customLabel || 'AI Interpretation'}
          size="small"
          sx={{
            backgroundColor: 'rgba(56, 189, 248, 0.12)',
            color: '#38BDF8',
            border: '1px solid rgba(56, 189, 248, 0.3)',
            ...sx,
          }}
          {...props}
        />
      );

    case 'recommendation':
      return (
        <Chip
          icon={<LightbulbOutlined sx={{ fontSize: '14px !important', color: '#F59E0B !important' }} />}
          label={customLabel || 'Recommendation'}
          size="small"
          sx={{
            backgroundColor: 'rgba(245, 158, 11, 0.12)',
            color: '#F59E0B',
            border: '1px solid rgba(245, 158, 11, 0.3)',
            ...sx,
          }}
          {...props}
        />
      );

    case 'approval_pending':
      return (
        <Chip
          icon={<WarningAmber sx={{ fontSize: '14px !important', color: '#F59E0B !important' }} />}
          label={customLabel || 'Requires Approval'}
          size="small"
          sx={{
            backgroundColor: 'rgba(245, 158, 11, 0.15)',
            color: '#FBBF24',
            border: '1px solid rgba(245, 158, 11, 0.4)',
            animation: 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
            ...sx,
          }}
          {...props}
        />
      );

    case 'approved':
      return (
        <Chip
          label={customLabel || 'Approved'}
          size="small"
          sx={{
            backgroundColor: 'rgba(16, 185, 129, 0.15)',
            color: '#34D399',
            border: '1px solid rgba(16, 185, 129, 0.3)',
            ...sx,
          }}
          {...props}
        />
      );

    case 'risk_high':
      return (
        <Chip
          label={customLabel || 'High Risk'}
          size="small"
          sx={{
            backgroundColor: 'rgba(239, 68, 68, 0.15)',
            color: '#F87171',
            border: '1px solid rgba(239, 68, 68, 0.4)',
            ...sx,
          }}
          {...props}
        />
      );

    case 'risk_medium':
      return (
        <Chip
          label={customLabel || 'Medium Risk'}
          size="small"
          sx={{
            backgroundColor: 'rgba(245, 158, 11, 0.12)',
            color: '#FBBF24',
            border: '1px solid rgba(245, 158, 11, 0.3)',
            ...sx,
          }}
          {...props}
        />
      );

    case 'risk_low':
      return (
        <Chip
          label={customLabel || 'Low Risk'}
          size="small"
          sx={{
            backgroundColor: 'rgba(16, 185, 129, 0.12)',
            color: '#34D399',
            border: '1px solid rgba(16, 185, 129, 0.3)',
            ...sx,
          }}
          {...props}
        />
      );
  }
};
