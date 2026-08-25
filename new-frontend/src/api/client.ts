import axios, { AxiosResponse, AxiosError, AxiosRequestConfig } from 'axios';

export interface ApiResponse<T> {
  data: T;
  status: number;
  message?: string;
}

export interface ApiError {
  status: number;
  message: string;
  errors?: Record<string, string[]>;
}

const client = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor for authentication
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('auth_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Add response interceptor for error handling
client.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error: AxiosError<ApiError>) => {
    if (error.response?.status === 401) {
      // Handle unauthorized access
      localStorage.removeItem('auth_token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

interface RequestConfig extends Omit<AxiosRequestConfig, 'url' | 'method' | 'data'> {}

export const apiClient = {
  get: <T>(url: string, config?: RequestConfig) => 
    client.get<ApiResponse<T>>(url, config).then(res => res.data),
    
  post: <T>(url: string, data: unknown, config?: RequestConfig) =>
    client.post<ApiResponse<T>>(url, data, config).then(res => res.data),
    
  put: <T>(url: string, data: unknown, config?: RequestConfig) =>
    client.put<ApiResponse<T>>(url, data, config).then(res => res.data),
    
  patch: <T>(url: string, data: unknown, config?: RequestConfig) =>
    client.patch<ApiResponse<T>>(url, data, config).then(res => res.data),
    
  delete: <T>(url: string, config?: RequestConfig) =>
    client.delete<ApiResponse<T>>(url, config).then(res => res.data),
};

// Re-export everything
export type { AxiosError, AxiosResponse, AxiosRequestConfig };