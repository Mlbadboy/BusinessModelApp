import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig, AxiosResponse } from 'axios';
import { config } from '../config/env.config';

// Create axios instance with default config
const api: AxiosInstance = axios.create({
  baseURL: config.api.baseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000, // 30 seconds
});

// Request interceptor for adding auth token
api.interceptors.request.use(
  (axiosConfig: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem(config.auth.tokenKey);
    if (token && axiosConfig.headers) {
      axiosConfig.headers.Authorization = `Bearer ${token}`;
    }
    return axiosConfig;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Response interceptor for handling errors
api.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  async (error: AxiosError) => {
    if (error.response) {
      // Handle 401 Unauthorized
      if (error.response.status === 401) {
        localStorage.removeItem(config.auth.tokenKey);
        window.location.href = '/login';
      }

      // Log errors if error reporting is enabled
      if (config.features.errorReporting) {
        try {
          await axios.post(config.api.errorReportingUrl, {
            error: error.message,
            status: error.response.status,
            path: window.location.pathname,
            timestamp: new Date().toISOString(),
          });
        } catch (reportingError) {
          console.error('Error reporting failed:', reportingError);
        }
      }
    }
    return Promise.reject(error);
  }
);

// API endpoints
export const endpoints = {
  auth: {
    login: '/auth/login',
    register: '/auth/register',
    logout: '/auth/logout',
  },
  leads: {
    list: '/leads',
    create: '/leads',
    getById: (id: string) => `/leads/${id}`,
    qualify: (id: string) => `/leads/${id}/qualify`,
  },
  opportunities: {
    list: '/opportunities',
    create: '/opportunities',
    getById: (id: string) => `/opportunities/${id}`,
    updateStage: (id: string) => `/opportunities/${id}/stage`,
    activities: (id: string) => `/opportunities/${id}/activities`,
  },
  analytics: {
    financialPerformance: '/analytics/financial-performance',
    businessHealth: '/analytics/business-health',
  },
  revenue: {
    trends: '/revenue/trends',
    risks: '/revenue/risks',
    opportunities: '/revenue/opportunities',
    analysis: '/revenue/analysis',
    sourcePerformance: '/revenue/source-performance',
  },
  expenses: {
    categories: '/expenses/categories',
    trends: '/expenses/trends',
    risks: '/expenses/risks',
    optimization: '/expenses/optimization',
    analysis: '/expenses/analysis',
  },
  strategy: {
    performanceTrends: '/strategy/performance-trends',
    risks: '/strategy/risks',
    opportunities: '/strategy/opportunities',
  },
};

// Helper function to handle API errors
export const handleApiError = (error: unknown) => {
  if (axios.isAxiosError(error)) {
    return {
      message: error.response?.data?.message || 'An error occurred',
      status: error.response?.status,
    };
  }
  return {
    message: 'An unexpected error occurred',
    status: 500,
  };
};

export default api;