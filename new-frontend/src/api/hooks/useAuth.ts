import { useMutation, useQuery } from '@tanstack/react-query';
import { apiClient, ApiResponse } from '../client';
import { AuthResponse, LoginCredentials, AuthUser } from '../../types/auth';

const AUTH_STORAGE_KEY = 'auth_token';

export const useLogin = () => {
  return useMutation({
    mutationFn: async (credentials: LoginCredentials) => {
      const response = await apiClient.post<AuthResponse>('/api/auth/login', credentials);
      // Store token on successful login
      if (response.data.token) {
        localStorage.setItem(AUTH_STORAGE_KEY, response.data.token);
      }
      return response.data;
    },
  });
};

export const useLogout = () => {
  return useMutation({
    mutationFn: async () => {
      await apiClient.post('/api/auth/logout', {});
      localStorage.removeItem(AUTH_STORAGE_KEY);
    },
  });
};

export const useCurrentUser = () => {
  return useQuery<ApiResponse<AuthUser>>({
    queryKey: ['auth', 'currentUser'],
    queryFn: () => apiClient.get('/api/auth/me'),
    retry: false,
    enabled: !!localStorage.getItem(AUTH_STORAGE_KEY),
  });
};

// Helper function to get stored token
export const getStoredToken = (): string | null => {
  return localStorage.getItem(AUTH_STORAGE_KEY);
};

// Helper function to check if user is authenticated
export const isAuthenticated = (): boolean => {
  return !!getStoredToken();
};

// Helper function to check if user has required permission
export const hasPermission = (user: AuthUser | null, permission: string): boolean => {
  if (!user) return false;
  return user.permissions.includes(permission as any);
};

// Helper function to check if user has required role
export const hasRole = (user: AuthUser | null, role: string): boolean => {
  if (!user) return false;
  return user.role === role;
};