import { useMutation, useQuery } from '@tanstack/react-query';
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
  user: {
    id: string;
    email: string;
    name: string;
    role: string;
  };
}

export const useAuth = () => {
  const navigate = useNavigate();

  // Login mutation
  const login = useMutation<AuthResponse, Error, LoginCredentials>({
    mutationFn: async (credentials) => {
      const { data } = await api.post(endpoints.auth.login, credentials);
      return data;
    },
    onSuccess: (data) => {
      localStorage.setItem(config.auth.tokenKey, data.token);
      navigate('/dashboard');
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
      navigate('/dashboard');
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
        throw new Error('No token found');
      }
      const { data } = await api.get('/auth/me');
      return data.user;
    },
    retry: false,
    enabled: !!localStorage.getItem(config.auth.tokenKey),
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