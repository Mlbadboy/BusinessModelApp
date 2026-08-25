import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import api, { endpoints, handleApiError } from '../utils/api';
import { config } from '../config/env.config';

interface LoginCredentials {
  email: string;
  password: string;
}

interface RegisterData extends LoginCredentials {
  name: string;
  role?: string;
}

interface AuthResponse {
  token: string;
  expiresAt: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
    organizationId?: string;
    organizationName?: string;
    defaultWorkspaceId?: string;
    workspaceName?: string;
    permissions?: string[];
  };
}

export const useAuth = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  // Login mutation
  const login = useMutation<AuthResponse, Error, LoginCredentials>({
    mutationFn: async (credentials) => {
      const { data } = await api.post(endpoints.auth.login, credentials);
      return data;
    },
    onSuccess: (data) => {
      localStorage.setItem(config.auth.tokenKey, data.token);
      queryClient.setQueryData(['auth-status'], data.user);
      navigate('/');
    },
    onError: (error) => {
      const { message } = handleApiError(error);
      throw new Error(message);
    },
  });

  // Register mutation
  const register = useMutation<AuthResponse, Error, RegisterData>({
    mutationFn: async (userData) => {
      const { data } = await api.post(endpoints.auth.register, userData);
      return data;
    },
    onSuccess: (data) => {
      localStorage.setItem(config.auth.tokenKey, data.token);
      queryClient.setQueryData(['auth-status'], data.user);
      navigate('/');
    },
    onError: (error) => {
      const { message } = handleApiError(error);
      throw new Error(message);
    },
  });

  // Logout mutation
  const logout = useMutation({
    mutationFn: async () => {
      await api.post(endpoints.auth.logout);
      localStorage.removeItem(config.auth.tokenKey);
      queryClient.removeQueries(['auth-status']);
      navigate('/login');
    },
    onError: (error) => {
      const { message } = handleApiError(error);
      console.error('Logout error:', message);
    },
  });

  // Check auth status
  const { data: user, isLoading: isCheckingAuth } = useQuery({
    queryKey: ['auth-status'],
    queryFn: async () => {
      const token = localStorage.getItem(config.auth.tokenKey);
      if (!token) {
        return null;
      }
      try {
        const { data } = await api.get('/auth/me');
        return data?.user || data;
      } catch {
        localStorage.removeItem(config.auth.tokenKey);
        return null;
      }
    },
    retry: false,
    enabled: Boolean(localStorage.getItem(config.auth.tokenKey)),
  });

  return {
    user,
    isAuthenticated: !!user,
    isCheckingAuth,
    login,
    register,
    logout,
  };
};
