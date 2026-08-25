import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Box,
  CssBaseline,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Divider,
  Menu,
  MenuItem,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  RadioGroup,
  FormControlLabel,
  Radio,
  TextField,
} from '@mui/material';
import Dashboard from '@mui/icons-material/Dashboard';
import TrendingUp from '@mui/icons-material/TrendingUp';
import PeopleAlt from '@mui/icons-material/PeopleAlt';
import AttachMoney from '@mui/icons-material/AttachMoney';
import ReceiptLong from '@mui/icons-material/ReceiptLong';
import Psychology from '@mui/icons-material/Psychology';
import Analytics from '@mui/icons-material/Analytics';
import Lightbulb from '@mui/icons-material/Lightbulb';
import SmartToy from '@mui/icons-material/SmartToy';
import Tune from '@mui/icons-material/Tune';
import MenuIcon from '@mui/icons-material/Menu';
import AccountCircle from '@mui/icons-material/AccountCircle';
import Logout from '@mui/icons-material/Logout';
import AutoAwesome from '@mui/icons-material/AutoAwesome';
import PowerSettingsNew from '@mui/icons-material/PowerSettingsNew';
import { useAuth } from '../../hooks/useAuth';
import { CommandBarModal } from '../command/CommandBarModal';
import { useAIControlCenter } from '../../hooks/useAIControlCenter';

const DRAWER_WIDTH = 250;
const COLLAPSED_DRAWER_WIDTH = 68;

interface LayoutProps {
  children: React.ReactNode;
}

export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [commandBarOpen, setCommandBarOpen] = useState(false);
  const [killSwitchModalOpen, setKillSwitchModalOpen] = useState(false);
  const [selectedStatus, setSelectedStatus] = useState<number>(1);
  const [disableReason, setDisableReason] = useState<string>('');

  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();
  const { summary, updateTrafficStatus } = useAIControlCenter();

  // Keyboard shortcut Cmd/Ctrl + K for Global Command Bar
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setCommandBarOpen((prev) => !prev);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleProfileMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = async () => {
    handleProfileMenuClose();
    logout.mutate();
  };

  const handleKillSwitchSave = () => {
    updateTrafficStatus.mutate(
      { status: selectedStatus, reason: disableReason || undefined },
      {
        onSuccess: () => {
          setKillSwitchModalOpen(false);
        },
      }
    );
  };

  const navSections = [
    {
      title: 'COMMAND CENTER',
      items: [{ text: 'Command Center', icon: <Dashboard />, path: '/' }],
    },
    {
      title: 'BUSINESS',
      items: [
        { text: 'Growth Agent', icon: <SmartToy />, path: '/growth-agent' },
        { text: 'Opportunities', icon: <TrendingUp />, path: '/opportunities' },
        { text: 'Leads', icon: <PeopleAlt />, path: '/leads' },
      ],
    },
    {
      title: 'FINANCE',
      items: [
        { text: 'Revenue', icon: <AttachMoney />, path: '/revenue' },
        { text: 'Expenses', icon: <ReceiptLong />, path: '/expenses' },
      ],
    },
    {
      title: 'INTELLIGENCE',
      items: [
        { text: 'Business Brain', icon: <Psychology />, path: '/business-brain' },
        { text: 'Business Health', icon: <Analytics />, path: '/analytics' },
        { text: 'Strategy', icon: <Lightbulb />, path: '/strategy' },
      ],
    },
    {
      title: 'AI SYSTEM',
      items: [{ text: 'AI Control Center', icon: <Tune />, path: '/ai-control-center' }],
    },
  ];

  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', backgroundColor: '#070A0F' }}>
      {/* Brand Header */}
      <Box
        sx={{
          p: 2,
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          borderBottom: '1px solid rgba(255, 255, 255, 0.08)',
          cursor: 'pointer',
        }}
        onClick={() => navigate('/')}
      >
        <Box
          sx={{
            width: 32,
            height: 32,
            borderRadius: 1.5,
            backgroundColor: '#00F0FF',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 0 15px rgba(0, 240, 255, 0.4)',
          }}
        >
          <AutoAwesome sx={{ color: '#070A0F', fontSize: 18 }} />
        </Box>
        {!collapsed && (
          <Box>
            <Typography variant="subtitle1" fontWeight="bold" sx={{ color: '#F8FAFC', lineHeight: 1.2 }}>
              BusinessModelApp
            </Typography>
            <Typography variant="caption" sx={{ color: '#00F0FF', fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.05em' }}>
              AI BUSINESS OS
            </Typography>
          </Box>
        )}
      </Box>

      {/* Navigation Sections */}
      <Box sx={{ flex: 1, overflowY: 'auto', py: 1.5 }}>
        {navSections.map((section, sIdx) => (
          <Box key={sIdx} sx={{ mb: 1.5 }}>
            {!collapsed && (
              <Typography
                variant="caption"
                sx={{
                  px: 2.5,
                  py: 0.5,
                  display: 'block',
                  color: '#475569',
                  fontWeight: 700,
                  fontSize: '0.6875rem',
                  letterSpacing: '0.08em',
                }}
              >
                {section.title}
              </Typography>
            )}
            <List dense disablePadding>
              {section.items.map((item) => {
                const isActive = location.pathname === item.path;
                return (
                  <ListItem key={item.text} disablePadding sx={{ px: 1 }}>
                    <Tooltip title={collapsed ? item.text : ''} placement="right">
                      <ListItemButton
                        onClick={() => navigate(item.path)}
                        selected={isActive}
                        sx={{
                          borderRadius: 1.5,
                          mb: 0.25,
                          px: collapsed ? 1.5 : 2,
                          py: 0.9,
                          color: isActive ? '#00F0FF' : '#94A3B8',
                          backgroundColor: isActive ? 'rgba(0, 240, 255, 0.08) !important' : 'transparent',
                          border: isActive ? '1px solid rgba(0, 240, 255, 0.3)' : '1px solid transparent',
                          '&:hover': {
                            backgroundColor: 'rgba(255, 255, 255, 0.04)',
                            color: '#F8FAFC',
                          },
                        }}
                      >
                        <ListItemIcon
                          sx={{
                            minWidth: collapsed ? 'auto' : 32,
                            color: isActive ? '#00F0FF' : '#64748B',
                          }}
                        >
                          {item.icon}
                        </ListItemIcon>
                        {!collapsed && (
                          <ListItemText
                            primary={item.text}
                            primaryTypographyProps={{
                              fontSize: '0.8125rem',
                              fontWeight: isActive ? 600 : 500,
                            }}
                          />
                        )}
                      </ListItemButton>
                    </Tooltip>
                  </ListItem>
                );
              })}
            </List>
          </Box>
        ))}
      </Box>

      <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.08)' }} />

      {/* Bottom AI Status Pill */}
      <Box
        sx={{
          p: 1.5,
          cursor: 'pointer',
          backgroundColor: '#0D1118',
          borderTop: '1px solid rgba(255, 255, 255, 0.06)',
          '&:hover': { backgroundColor: 'rgba(0, 240, 255, 0.04)' },
        }}
        onClick={() => {
          setSelectedStatus(summary?.trafficStatus === 'EmergencyDisabled' ? 3 : summary?.trafficStatus === 'Degraded' ? 2 : 1);
          setKillSwitchModalOpen(true);
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Box
              sx={{
                width: 8,
                height: 8,
                borderRadius: '50%',
                backgroundColor: summary?.trafficStatus === 'EmergencyDisabled' ? '#EF4444' : summary?.trafficStatus === 'Degraded' ? '#F59E0B' : '#10B981',
                boxShadow: `0 0 8px ${summary?.trafficStatus === 'EmergencyDisabled' ? '#EF4444' : '#10B981'}`,
              }}
            />
            {!collapsed && (
              <Box>
                <Typography variant="caption" sx={{ fontWeight: 600, color: '#F8FAFC', display: 'block' }}>
                  AI Traffic: {summary?.trafficStatus || 'Enabled'}
                </Typography>
                <Typography variant="caption" sx={{ color: '#64748B', fontSize: '0.65rem' }}>
                  OmniRoute {summary?.gatewayStatus || 'Healthy'}
                </Typography>
              </Box>
            )}
          </Box>
          {!collapsed && (
            <IconButton size="small" sx={{ color: '#64748B', p: 0.25 }}>
              <PowerSettingsNew sx={{ fontSize: 14 }} />
            </IconButton>
          )}
        </Box>
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#070A0F' }}>
      <CssBaseline />

      {/* Top Application Bar */}
      <AppBar
        position="fixed"
        sx={{
          width: { sm: `calc(100% - ${collapsed ? COLLAPSED_DRAWER_WIDTH : DRAWER_WIDTH}px)` },
          ml: { sm: `${collapsed ? COLLAPSED_DRAWER_WIDTH : DRAWER_WIDTH}px` },
          backgroundColor: 'rgba(7, 10, 15, 0.85)',
          backdropFilter: 'blur(12px)',
          borderBottom: '1px solid rgba(255, 255, 255, 0.08)',
          boxShadow: 'none',
        }}
      >
        <Toolbar sx={{ display: 'flex', justifyContent: 'space-between', minHeight: 64, px: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <IconButton
              color="inherit"
              edge="start"
              onClick={handleDrawerToggle}
              sx={{ display: { sm: 'none' } }}
            >
              <MenuIcon />
            </IconButton>
            <IconButton
              color="inherit"
              onClick={() => setCollapsed(!collapsed)}
              sx={{ display: { xs: 'none', sm: 'inline-flex' }, color: '#94A3B8' }}
            >
              <MenuIcon />
            </IconButton>
          </Box>

          {/* Persistent Global JARVIS Command Bar Trigger */}
          <Box
            onClick={() => setCommandBarOpen(true)}
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 2,
              px: 2,
              py: 0.8,
              borderRadius: 2,
              backgroundColor: '#0D1118',
              border: '1px solid rgba(255, 255, 255, 0.1)',
              width: { xs: '100%', sm: 380, md: 440 },
              cursor: 'pointer',
              transition: 'all 0.2s ease',
              '&:hover': {
                borderColor: 'rgba(0, 240, 255, 0.4)',
                backgroundColor: '#111722',
                boxShadow: '0 0 15px rgba(0, 240, 255, 0.1)',
              },
            }}
          >
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <AutoAwesome sx={{ color: '#00F0FF', fontSize: 16 }} />
              <Typography variant="body2" sx={{ color: '#64748B', fontWeight: 500 }}>
                Ask BusinessModelApp...
              </Typography>
            </Box>
            <Box
              sx={{
                px: 0.8,
                py: 0.2,
                borderRadius: 1,
                backgroundColor: 'rgba(255, 255, 255, 0.08)',
                fontFamily: 'monospace',
                fontSize: '0.6875rem',
                color: '#94A3B8',
                fontWeight: 600,
              }}
            >
              ⌘K
            </Box>
          </Box>

          {/* User Profile & Workspace Info */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ display: { xs: 'none', md: 'block' }, textAlign: 'right' }}>
              <Typography variant="body2" fontWeight="600" color="text.primary">
                {user?.name || user?.email || 'Mayur (CEO)'}
              </Typography>
              <Typography variant="caption" sx={{ color: '#00F0FF', fontWeight: 600 }}>
                Enterprise Workspace
              </Typography>
            </Box>
            <IconButton onClick={handleProfileMenuOpen} sx={{ p: 0.5, color: '#94A3B8' }}>
              <AccountCircle sx={{ fontSize: 32 }} />
            </IconButton>
          </Box>

          <Menu
            anchorEl={anchorEl}
            open={Boolean(anchorEl)}
            onClose={handleProfileMenuClose}
            PaperProps={{
              sx: {
                backgroundColor: '#0D1118',
                border: '1px solid rgba(255, 255, 255, 0.1)',
                minWidth: 180,
              },
            }}
          >
            <MenuItem onClick={() => { handleProfileMenuClose(); navigate('/ai-control-center'); }}>
              <Tune sx={{ fontSize: 18, mr: 1.5, color: '#00F0FF' }} />
              AI Control Center
            </MenuItem>
            <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.08)' }} />
            <MenuItem onClick={handleLogout} sx={{ color: '#EF4444' }}>
              <Logout sx={{ fontSize: 18, mr: 1.5 }} />
              Sign Out
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      {/* Navigation Drawer */}
      <Box
        component="nav"
        sx={{ width: { sm: collapsed ? COLLAPSED_DRAWER_WIDTH : DRAWER_WIDTH }, flexShrink: { sm: 0 } }}
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', sm: 'none' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: DRAWER_WIDTH, borderRight: '1px solid rgba(255, 255, 255, 0.08)' },
          }}
        >
          {drawerContent}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', sm: 'block' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: collapsed ? COLLAPSED_DRAWER_WIDTH : DRAWER_WIDTH,
              borderRight: '1px solid rgba(255, 255, 255, 0.08)',
              transition: 'width 0.2s ease',
            },
          }}
          open
        >
          {drawerContent}
        </Drawer>
      </Box>

      {/* Main Business Space Viewport */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: { xs: 2, sm: 3, md: 4 },
          width: { sm: `calc(100% - ${collapsed ? COLLAPSED_DRAWER_WIDTH : DRAWER_WIDTH}px)` },
          mt: '64px',
          minHeight: 'calc(100vh - 64px)',
          backgroundColor: '#070A0F',
        }}
      >
        {children}
      </Box>

      {/* Global Command Bar Modal */}
      <CommandBarModal open={commandBarOpen} onClose={() => setCommandBarOpen(false)} />

      {/* AI Traffic Kill-Switch Dialog */}
      <Dialog
        open={killSwitchModalOpen}
        onClose={() => setKillSwitchModalOpen(false)}
        PaperProps={{
          sx: {
            backgroundColor: '#0D1118',
            border: '1px solid rgba(255, 255, 255, 0.1)',
            minWidth: 380,
          },
        }}
      >
        <DialogTitle sx={{ color: '#F8FAFC', fontWeight: 'bold' }}>
          AI Traffic Control & Kill-Switch
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Manage organization-wide AI execution. Disabling stops all AI traffic while preserving deterministic business operations.
          </Typography>
          <RadioGroup
            value={selectedStatus}
            onChange={(e) => setSelectedStatus(Number(e.target.value))}
          >
            <FormControlLabel
              value={1}
              control={<Radio sx={{ color: '#10B981', '&.Mui-checked': { color: '#10B981' } }} />}
              label="Enabled (Normal Operations)"
            />
            <FormControlLabel
              value={2}
              control={<Radio sx={{ color: '#F59E0B', '&.Mui-checked': { color: '#F59E0B' } }} />}
              label="Degraded (Critical Workflows Only)"
            />
            <FormControlLabel
              value={3}
              control={<Radio sx={{ color: '#EF4444', '&.Mui-checked': { color: '#EF4444' } }} />}
              label="Emergency Disabled (Kill-Switch Active)"
            />
          </RadioGroup>

          {selectedStatus === 3 && (
            <TextField
              label="Reason for Emergency Disable"
              placeholder="e.g. Suspected provider anomaly"
              fullWidth
              size="small"
              value={disableReason}
              onChange={(e) => setDisableReason(e.target.value)}
              sx={{ mt: 2 }}
            />
          )}
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setKillSwitchModalOpen(false)} sx={{ color: 'text.secondary' }}>
            Cancel
          </Button>
          <Button
            variant="contained"
            color={selectedStatus === 3 ? 'error' : 'primary'}
            onClick={handleKillSwitchSave}
          >
            Save Traffic State
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
