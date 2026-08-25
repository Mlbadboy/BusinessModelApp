import { ReactNode } from 'react';
import { LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { QueryProvider } from '../api/hooks';
import { AuthProvider } from '../contexts/AuthContext';
import { ErrorBoundary } from '../components/common/ErrorBoundary';

interface AppProvidersProps {
  children: ReactNode;
}

export function AppProviders({ children }: AppProvidersProps) {
  return (
    <ErrorBoundary>
      <QueryProvider>
        <AuthProvider>
          <LocalizationProvider dateAdapter={AdapterDayjs}>
            {children}
          </LocalizationProvider>
        </AuthProvider>
      </QueryProvider>
    </ErrorBoundary>
  );
}