import {
  Box,
  Button,
  Typography,
  Container,
  Paper,
} from '@mui/material';
import LockIcon from '@mui/icons-material/Lock';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

export function UnauthorizedPage() {
  const navigate = useNavigate();
  const { logout } = useAuth();

  const handleBack = () => {
    navigate(-1);
  };

  const handleDashboard = () => {
    navigate('/dashboard');
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <Container maxWidth="sm">
      <Box
        sx={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Paper
          elevation={3}
          sx={{
            p: 4,
            textAlign: 'center',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 3,
          }}
        >
          <LockIcon
            sx={{
              fontSize: 64,
              color: 'error.main',
            }}
          />

          <Typography variant="h4" gutterBottom>
            Access Denied
          </Typography>

          <Typography variant="body1" color="text.secondary" paragraph>
            You don't have permission to access this page. Please contact your
            administrator if you believe this is an error.
          </Typography>

          <Box
            sx={{
              display: 'flex',
              gap: 2,
              flexWrap: 'wrap',
              justifyContent: 'center',
            }}
          >
            <Button
              variant="outlined"
              onClick={handleBack}
            >
              Go Back
            </Button>

            <Button
              variant="outlined"
              onClick={handleDashboard}
            >
              Go to Dashboard
            </Button>

            <Button
              variant="contained"
              color="primary"
              onClick={handleLogout}
            >
              Logout
            </Button>
          </Box>
        </Paper>
      </Box>
    </Container>
  );
}