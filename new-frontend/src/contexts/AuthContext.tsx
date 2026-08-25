import React, { createContext, useContext, useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useLogin, useLogout, useCurrentUser } from '../api/hooks/useAuth';
import { AuthContextType, AuthUser, LoginCredentials } from '../types/auth';

const AuthContext = createContext<AuthContextType | null>(null);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const navigate = useNavigate();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('auth_token'));
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loginMutation = useLogin();
  const logoutMutation = useLogout();
  const { data: currentUser, isError } = useCurrentUser();

  // Initialize auth state
  useEffect(() => {
    if (currentUser) {
      setUser(currentUser.data);
      setIsLoading(false);
    } else if (isError) {
      setUser(null);
      setToken(null);
      localStorage.removeItem('auth_token');
      setIsLoading(false);
    }
  }, [currentUser, isError]);

  const login = useCallback(async (credentials: LoginCredentials) => {
    try {
      setError(null);
      const response = await loginMutation.mutateAsync(credentials);
      setUser(response.user);
      setToken(response.token);
      navigate('/dashboard');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to login');
      throw err;
    }
  }, [loginMutation, navigate]);

  const logout = useCallback(async () => {
    try {
      await logoutMutation.mutateAsync();
      setUser(null);
      setToken(null);
      navigate('/login');
    } catch (err) {
      console.error('Logout failed:', err);
      // Still clear local state even if server logout fails
      setUser(null);
      setToken(null);
      localStorage.removeItem('auth_token');
      navigate('/login');
    }
  }, [logoutMutation, navigate]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  const value: AuthContextType = {
    user,
    token,
    isAuthenticated: !!user && !!token,
    isLoading,
    error,
    login,
    logout,
    clearError,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}