import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { QueryProvider } from '@/providers/QueryProvider';
import { ErrorBoundary } from '@/components/ErrorBoundary';
import { LoadingState } from '@/components/LoadingState';
import { Layout } from '@/components/Layout/Layout';
import { useAuth } from '@/hooks/useAuth';
import { theme } from '@/config/theme';
import { Router } from '@/Router';

const AppContent = () => {
  const { isCheckingAuth } = useAuth();

  if (isCheckingAuth) {
    return <LoadingState message="Initializing application..." />;
  }

  return (
    <Layout>
      <Router />
    </Layout>
  );
};

const App = () => {
  return (
    <ErrorBoundary>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <LocalizationProvider dateAdapter={AdapterDayjs}>
          <QueryProvider>
            <AppContent />
          </QueryProvider>
        </LocalizationProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
};

export default App;