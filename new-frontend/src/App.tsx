import { ErrorBoundary } from '@/components/ErrorBoundary';
import { LoadingState } from '@/components/LoadingState';
import { Layout } from '@/components/Layout/Layout';
import { useAuth } from '@/hooks/useAuth';
import { Router } from '@/Router';

const AppContent = () => {
  const { isAuthenticated, isCheckingAuth } = useAuth();

  if (isCheckingAuth) {
    return <LoadingState message="Initializing application..." />;
  }

  if (!isAuthenticated) {
    return <Router />;
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
      <AppContent />
    </ErrorBoundary>
  );
};

export default App;
