import { ErrorBoundary } from '@/components/ErrorBoundary';
import { Router } from '@/Router';

const App = () => {
  return (
    <ErrorBoundary>
      <Router />
    </ErrorBoundary>
  );
};

export default App;
