import {
  Drawer,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  ListItemButton,
  Toolbar,
  useTheme,
} from '@mui/material';
import { useLocation, useNavigate } from 'react-router-dom';
import DashboardIcon from '@mui/icons-material/Dashboard';
import AnalyticsIcon from '@mui/icons-material/Analytics';
import BusinessIcon from '@mui/icons-material/Business';
import SettingsIcon from '@mui/icons-material/Settings';
import { useAuth } from '../../contexts/AuthContext';
import { Permission, UserRole } from '../../types/auth';

const drawerWidth = 240;

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

interface NavigationItem {
  text: string;
  icon: JSX.Element;
  path: string;
  requiredPermissions?: Permission[];
  requiredRoles?: UserRole[];
}

const navigationItems: NavigationItem[] = [
  {
    text: 'Dashboard',
    icon: <DashboardIcon />,
    path: '/dashboard',
    requiredPermissions: [],
  },
  {
    text: 'Business Models',
    icon: <BusinessIcon />,
    path: '/business-models',
    requiredPermissions: [Permission.ManageStrategy],
  },
  {
    text: 'Analytics',
    icon: <AnalyticsIcon />,
    path: '/analytics',
    requiredPermissions: [Permission.ViewAnalytics],
  },
  {
    text: 'Settings',
    icon: <SettingsIcon />,
    path: '/settings',
    requiredRoles: [UserRole.Admin],
  },
];

export default function Sidebar({ open, onClose }: SidebarProps) {
  const theme = useTheme();
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuth();

  const handleNavigate = (path: string) => {
    navigate(path);
    onClose();
  };

  const canAccess = (item: NavigationItem): boolean => {
    if (!user) return false;

    if (item.requiredRoles?.length) {
      if (!item.requiredRoles.includes(user.role)) {
        return false;
      }
    }

    if (item.requiredPermissions?.length) {
      if (!item.requiredPermissions.every(permission => 
        user.permissions.includes(permission)
      )) {
        return false;
      }
    }

    return true;
  };

  return (
    <Drawer
      variant="persistent"
      anchor="left"
      open={open}
      onClose={onClose}
      sx={{
        width: drawerWidth,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: drawerWidth,
          boxSizing: 'border-box',
          backgroundColor: theme.palette.background.paper,
          borderRight: `1px solid ${theme.palette.divider}`,
        },
      }}
    >
      <Toolbar />
      <List>
        {navigationItems.map((item) => 
          canAccess(item) ? (
            <ListItem key={item.text} disablePadding>
              <ListItemButton
                selected={location.pathname.startsWith(item.path)}
                onClick={() => handleNavigate(item.path)}
              >
                <ListItemIcon sx={{ color: 'inherit' }}>
                  {item.icon}
                </ListItemIcon>
                <ListItemText primary={item.text} />
              </ListItemButton>
            </ListItem>
          ) : null
        )}
      </List>
    </Drawer>
  );
}