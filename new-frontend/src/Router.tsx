import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuth';
import { ErrorBoundary } from './components/ErrorBoundary';
import { LoadingState } from './components/LoadingState';
import { lazy, Suspense } from 'react';

// Lazy load components
const Login = lazy(() => import('./pages/Auth/Login'));
const Register = lazy(() => import('./pages/Auth/Register'));
const Dashboard = lazy(() => import('./pages/Dashboard'));
const Opportunities = lazy(() => import('./pages/Opportunities'));
const Leads = lazy(() => import('./pages/Leads'));
const BusinessBrain = lazy(() => import('./pages/BusinessBrain'));
const GrowthAgent = lazy(() => import('./pages/GrowthAgent'));
const AIControlCenter = lazy(() => import('./pages/AIControlCenter'));
const Analytics = lazy(() => import('./pages/Analytics'));
const Revenue = lazy(() => import('./pages/Revenue'));
const Expenses = lazy(() => import('./pages/Expenses'));
const ExecutiveCore = lazy(() => import('./pages/ExecutiveCore'));
const Connect = lazy(() => import('./pages/Connect'));
const MarketRadar = lazy(() => import('./pages/MarketRadar'));
const SecurityCommandCenter = lazy(() => import('./pages/SecurityCommandCenter'));
const ExecutionCommandCenter = lazy(() => import('./pages/ExecutionCommandCenter'));
const AIBrainSettings = lazy(() => import('./pages/Settings/AIBrainSettings'));
const BusinessConstraints = lazy(() => import('./pages/Settings/BusinessConstraints'));

interface ProtectedRouteProps {
  children: React.ReactNode;
}

const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  const { isAuthenticated, isCheckingAuth } = useAuth();

  if (isCheckingAuth) {
    return <LoadingState message="Checking authentication..." />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
};

export const Router = () => {
  return (
    <ErrorBoundary>
      <Suspense fallback={<LoadingState message="Loading command space..." />}>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />

          {/* Protected routes */}
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            }
          />

          <Route
            path="/opportunities"
            element={
              <ProtectedRoute>
                <Opportunities />
              </ProtectedRoute>
            }
          />

          <Route
            path="/leads"
            element={
              <ProtectedRoute>
                <Leads />
              </ProtectedRoute>
            }
          />

          <Route
            path="/business-brain"
            element={
              <ProtectedRoute>
                <BusinessBrain />
              </ProtectedRoute>
            }
          />

          <Route
            path="/growth-agent"
            element={
              <ProtectedRoute>
                <GrowthAgent />
              </ProtectedRoute>
            }
          />

          <Route
            path="/ai-control-center"
            element={
              <ProtectedRoute>
                <AIControlCenter />
              </ProtectedRoute>
            }
          />

          <Route
            path="/analytics"
            element={
              <ProtectedRoute>
                <Analytics />
              </ProtectedRoute>
            }
          />

          <Route
            path="/revenue/*"
            element={
              <ProtectedRoute>
                <Revenue />
              </ProtectedRoute>
            }
          />

          <Route
            path="/expenses/*"
            element={
              <ProtectedRoute>
                <Expenses />
              </ProtectedRoute>
            }
          />

          <Route
            path="/strategy/*"
            element={
              <ProtectedRoute>
                <ExecutiveCore />
              </ProtectedRoute>
            }
          />

          <Route
            path="/executive-core"
            element={
              <ProtectedRoute>
                <ExecutiveCore />
              </ProtectedRoute>
            }
          />

          <Route
            path="/connect"
            element={
              <ProtectedRoute>
                <Connect />
              </ProtectedRoute>
            }
          />

          <Route
            path="/market-radar"
            element={
              <ProtectedRoute>
                <MarketRadar />
              </ProtectedRoute>
            }
          />

          <Route
            path="/security"
            element={
              <ProtectedRoute>
                <SecurityCommandCenter />
              </ProtectedRoute>
            }
          />

          <Route
            path="/execution"
            element={
              <ProtectedRoute>
                <ExecutionCommandCenter />
              </ProtectedRoute>
            }
          />

          <Route
            path="/settings/ai-brain"
            element={
              <ProtectedRoute>
                <AIBrainSettings />
              </ProtectedRoute>
            }
          />

          <Route
            path="/settings/business-constraints"
            element={
              <ProtectedRoute>
                <BusinessConstraints />
              </ProtectedRoute>
            }
          />

          {/* Fallback route */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Suspense>
    </ErrorBoundary>
  );
};