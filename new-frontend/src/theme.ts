import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#00F0FF', // Electric Cyan AI Accent
      light: '#67F5FF',
      dark: '#00B4D8',
      contrastText: '#070A0F',
    },
    secondary: {
      main: '#38BDF8', // Sky Blue
      light: '#7DD3FC',
      dark: '#0284C7',
      contrastText: '#070A0F',
    },
    success: {
      main: '#10B981', // Emerald - Verified Fact
      light: '#34D399',
      dark: '#059669',
      contrastText: '#FFFFFF',
    },
    warning: {
      main: '#F59E0B', // Amber - Recommendations / Approvals
      light: '#FBBF24',
      dark: '#D97706',
      contrastText: '#070A0F',
    },
    info: {
      main: '#6366F1', // Indigo - AI Interpretation
      light: '#818CF8',
      dark: '#4F46E5',
      contrastText: '#FFFFFF',
    },
    error: {
      main: '#EF4444',
      light: '#F87171',
      dark: '#DC2626',
      contrastText: '#FFFFFF',
    },
    background: {
      default: '#070A0F', // Primary Dark Command Background
      paper: '#0D1118',   // Elevated Card Surface
    },
    text: {
      primary: '#F8FAFC',
      secondary: '#94A3B8',
      disabled: '#475569',
    },
    divider: 'rgba(255, 255, 255, 0.08)',
  },
  typography: {
    fontFamily: '"Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    h1: {
      fontSize: '2rem',
      fontWeight: 700,
      letterSpacing: '-0.025em',
      color: '#F8FAFC',
    },
    h2: {
      fontSize: '1.5rem',
      fontWeight: 600,
      letterSpacing: '-0.02em',
      color: '#F8FAFC',
    },
    h3: {
      fontSize: '1.25rem',
      fontWeight: 600,
      letterSpacing: '-0.015em',
      color: '#F8FAFC',
    },
    h4: {
      fontSize: '1.125rem',
      fontWeight: 600,
      color: '#F8FAFC',
    },
    h5: {
      fontSize: '1rem',
      fontWeight: 600,
      color: '#F8FAFC',
    },
    h6: {
      fontSize: '0.875rem',
      fontWeight: 600,
      textTransform: 'uppercase',
      letterSpacing: '0.05em',
      color: '#94A3B8',
    },
    body1: {
      fontSize: '0.875rem',
      lineHeight: 1.5,
      color: '#F8FAFC',
    },
    body2: {
      fontSize: '0.8125rem',
      lineHeight: 1.4,
      color: '#94A3B8',
    },
    caption: {
      fontSize: '0.75rem',
      letterSpacing: '0.02em',
      color: '#64748B',
    },
  },
  shape: {
    borderRadius: 8,
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: '#070A0F',
          color: '#F8FAFC',
          scrollbarWidth: 'thin',
          '&::-webkit-scrollbar': {
            width: '6px',
            height: '6px',
          },
          '&::-webkit-scrollbar-track': {
            background: '#070A0F',
          },
          '&::-webkit-scrollbar-thumb': {
            background: 'rgba(255, 255, 255, 0.12)',
            borderRadius: '3px',
          },
          '&::-webkit-scrollbar-thumb:hover': {
            background: 'rgba(255, 255, 255, 0.2)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundColor: '#0D1118',
          backgroundImage: 'none',
          border: '1px solid rgba(255, 255, 255, 0.08)',
          borderRadius: 10,
          boxShadow: '0 4px 20px rgba(0, 0, 0, 0.4)',
          transition: 'border-color 0.2s ease, box-shadow 0.2s ease',
          '&:hover': {
            borderColor: 'rgba(0, 240, 255, 0.25)',
          },
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 600,
          borderRadius: 6,
          padding: '6px 16px',
          transition: 'all 0.2s ease',
        },
        containedPrimary: {
          backgroundColor: '#00F0FF',
          color: '#070A0F',
          boxShadow: '0 0 15px rgba(0, 240, 255, 0.3)',
          '&:hover': {
            backgroundColor: '#67F5FF',
            boxShadow: '0 0 20px rgba(0, 240, 255, 0.5)',
          },
        },
        outlinedPrimary: {
          borderColor: 'rgba(0, 240, 255, 0.4)',
          color: '#00F0FF',
          '&:hover': {
            borderColor: '#00F0FF',
            backgroundColor: 'rgba(0, 240, 255, 0.06)',
          },
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundColor: '#0D1118',
          backgroundImage: 'none',
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          borderColor: 'rgba(255, 255, 255, 0.06)',
          padding: '12px 16px',
          fontVariantNumeric: 'tabular-nums',
        },
        head: {
          backgroundColor: '#111722',
          color: '#94A3B8',
          fontWeight: 600,
          fontSize: '0.75rem',
          textTransform: 'uppercase',
          letterSpacing: '0.05em',
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 600,
          fontSize: '0.75rem',
          borderRadius: 4,
        },
      },
    },
  },
});

export default theme;