import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
  Box,
  InputBase,
  Typography,
  Stack,
  Chip,
  IconButton,
  Divider,
  CircularProgress,
} from '@mui/material';
import Search from '@mui/icons-material/Search';
import AutoAwesome from '@mui/icons-material/AutoAwesome';
import ArrowForward from '@mui/icons-material/ArrowForward';
import Close from '@mui/icons-material/Close';
import { useNavigate } from 'react-router-dom';
import { StatusBadge } from '../ui/StatusBadge';

interface CommandBarModalProps {
  open: boolean;
  onClose: () => void;
}

const SUGGESTED_QUERIES = [
  { text: 'Show me pipeline risks and stalled deals', category: 'Risk', route: '/opportunities' },
  { text: 'Explain my business health score and evidence', category: 'Health', route: '/' },
  { text: 'Show AI-generated revenue and ROI attribution', category: 'FinOps', route: '/ai-control-center' },
  { text: 'What changed in commercial operations today?', category: 'Operations', route: '/' },
  { text: 'Review pending consequential action approvals', category: 'Approvals', route: '/ai-control-center' },
  { text: 'Start Growth Agent lead qualification mission', category: 'Growth', route: '/growth-agent' },
];

export const CommandBarModal: React.FC<CommandBarModalProps> = ({ open, onClose }) => {
  const [query, setQuery] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [answer, setAnswer] = useState<{
    text: string;
    badge: 'fact' | 'interpretation' | 'recommendation';
    evidenceId?: string;
  } | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (!open) {
      setQuery('');
      setAnswer(null);
      setIsLoading(false);
    }
  }, [open]);

  const handleExecuteQuery = (textToRun: string, targetRoute?: string) => {
    setQuery(textToRun);
    setIsLoading(true);
    setAnswer(null);

    if (targetRoute && !textToRun) {
      navigate(targetRoute);
      onClose();
      return;
    }

    setTimeout(() => {
      setIsLoading(false);
      if (textToRun.toLowerCase().includes('risk') || textToRun.toLowerCase().includes('pipeline')) {
        setAnswer({
          text: '3 high-value enterprise deals account for 62% of your weighted pipeline. Acme Corp ($750k) has had no activity for 17 days.',
          badge: 'fact',
          evidenceId: 'EVD-PIPE-01',
        });
      } else if (textToRun.toLowerCase().includes('roi') || textToRun.toLowerCase().includes('revenue')) {
        setAnswer({
          text: 'Attributed AI ROI is 12.4x. ₹18,420 spent on qualification directly contributed to ₹2.28L in closed-won commercial revenue.',
          badge: 'fact',
          evidenceId: 'EVD-FIN-03',
        });
      } else if (textToRun.toLowerCase().includes('health')) {
        setAnswer({
          text: 'Overall Business Health is 78/100 (▲ +6.2%). Pipeline coverage (2.4x) and Revenue Momentum (87) are strong, while Activity Velocity (69) requires attention.',
          badge: 'interpretation',
          evidenceId: 'EVD-HLTH-01',
        });
      } else {
        setAnswer({
          text: `Analyzed workspace reality for "${textToRun}". All deterministic commercial systems are synchronized.`,
          badge: 'recommendation',
        });
      }
    }, 650);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && query.trim()) {
      handleExecuteQuery(query);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="sm"
      PaperProps={{
        sx: {
          backgroundColor: '#070A0F',
          border: '1px solid rgba(0, 240, 255, 0.3)',
          boxShadow: '0 0 40px rgba(0, 240, 255, 0.15)',
          borderRadius: 2.5,
          overflow: 'hidden',
        },
      }}
    >
      <DialogContent sx={{ p: 2.5 }}>
        {/* Search Input */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
          <AutoAwesome sx={{ color: '#00F0FF', fontSize: 22 }} />
          <InputBase
            placeholder="Ask BusinessModelApp anything... (e.g. Show pipeline risks)"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            autoFocus
            fullWidth
            sx={{
              color: '#F8FAFC',
              fontSize: '1rem',
              fontWeight: 500,
            }}
          />
          {isLoading ? (
            <CircularProgress size={18} sx={{ color: '#00F0FF' }} />
          ) : (
            <IconButton size="small" onClick={onClose} sx={{ color: 'text.secondary' }}>
              <Close fontSize="small" />
            </IconButton>
          )}
        </Box>

        <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.08)', mb: 2 }} />

        {/* AI Answer Result */}
        {answer && (
          <Box
            sx={{
              p: 2,
              mb: 2.5,
              borderRadius: 1.5,
              backgroundColor: '#0D1118',
              border: '1px solid rgba(0, 240, 255, 0.25)',
            }}
          >
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <StatusBadge type={answer.badge} />
              {answer.evidenceId && (
                <Typography variant="caption" sx={{ color: '#00F0FF', fontFamily: 'monospace' }}>
                  {answer.evidenceId}
                </Typography>
              )}
            </Box>
            <Typography variant="body1" sx={{ color: '#F8FAFC', lineHeight: 1.5 }}>
              {answer.text}
            </Typography>
          </Box>
        )}

        {/* Suggestions List */}
        <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: '0.05em', mb: 1, display: 'block' }}>
          Suggested Inquiries
        </Typography>

        <Stack spacing={1}>
          {SUGGESTED_QUERIES.map((sq, idx) => (
            <Box
              key={idx}
              onClick={() => handleExecuteQuery(sq.text, sq.route)}
              sx={{
                p: 1.25,
                borderRadius: 1,
                backgroundColor: '#111722',
                border: '1px solid rgba(255, 255, 255, 0.04)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                cursor: 'pointer',
                transition: 'all 0.15s ease',
                '&:hover': {
                  backgroundColor: 'rgba(0, 240, 255, 0.06)',
                  borderColor: 'rgba(0, 240, 255, 0.3)',
                  transform: 'translateX(3px)',
                },
              }}
            >
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                <Search sx={{ fontSize: 16, color: '#64748B' }} />
                <Typography variant="body2" color="text.primary">
                  {sq.text}
                </Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Chip label={sq.category} size="small" sx={{ height: 20, fontSize: '0.6875rem', backgroundColor: 'rgba(255, 255, 255, 0.06)' }} />
                <ArrowForward sx={{ fontSize: 14, color: '#64748B' }} />
              </Box>
            </Box>
          ))}
        </Stack>

        <Box sx={{ mt: 2.5, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="caption" color="text.secondary">
            Press <Box component="span" sx={{ px: 0.6, py: 0.2, backgroundColor: 'rgba(255, 255, 255, 0.08)', borderRadius: 0.5, fontFamily: 'monospace' }}>Enter</Box> to query
          </Typography>
          <Typography variant="caption" sx={{ color: '#00F0FF' }}>
            Governed by OmniRoute AI Gateway
          </Typography>
        </Box>
      </DialogContent>
    </Dialog>
  );
};
