import { useMutation, useQuery } from '@tanstack/react-query';
import { apiClient, ApiResponse } from '../client';
import { 
  ExpenseAnalysis, 
  ExpenseCategory, 
  ExpenseTrend, 
  ExpenseOptimization,
  DateRange 
} from '../../types';

const EXPENSE_KEYS = {
  all: ['expense'] as const,
  analysis: () => [...EXPENSE_KEYS.all, 'analysis'] as const,
  categories: () => [...EXPENSE_KEYS.all, 'categories'] as const,
  trends: () => [...EXPENSE_KEYS.all, 'trends'] as const,
  optimizations: () => [...EXPENSE_KEYS.all, 'optimizations'] as const,
};

export function useExpenseAnalysis(dateRange?: DateRange) {
  return useQuery<ApiResponse<ExpenseAnalysis>>({
    queryKey: [...EXPENSE_KEYS.analysis(), dateRange],
    queryFn: async () => {
      const params = dateRange ? {
        startDate: dateRange.startDate.toISOString(),
        endDate: dateRange.endDate.toISOString(),
      } : {};
      
      const { data } = await apiClient.get('/expense/analysis', { params });
      return data;
    },
  });
}

export function useExpenseCategories() {
  return useQuery<ApiResponse<ExpenseCategory[]>>({
    queryKey: EXPENSE_KEYS.categories(),
    queryFn: async () => {
      const { data } = await apiClient.get('/expense/categories');
      return data;
    },
  });
}

export function useExpenseTrends(period: string = 'monthly') {
  return useQuery<ApiResponse<ExpenseTrend[]>>({
    queryKey: [...EXPENSE_KEYS.trends(), period],
    queryFn: async () => {
      const { data } = await apiClient.get('/expense/trends', {
        params: { period },
      });
      return data;
    },
  });
}

export function useExpenseOptimizations() {
  return useQuery<ApiResponse<ExpenseOptimization[]>>({
    queryKey: EXPENSE_KEYS.optimizations(),
    queryFn: async () => {
      const { data } = await apiClient.get('/expense/optimizations');
      return data;
    },
  });
}

// Mutations

export function useAddExpense() {
  return useMutation<
    ApiResponse<ExpenseCategory>,
    Error,
    Partial<ExpenseCategory>
  >({
    mutationFn: async (newExpense) => {
      const { data } = await apiClient.post('/expense/categories', newExpense);
      return data;
    },
  });
}

export function useUpdateExpense() {
  return useMutation<
    ApiResponse<ExpenseCategory>,
    Error,
    { id: number; updates: Partial<ExpenseCategory> }
  >({
    mutationFn: async ({ id, updates }) => {
      const { data } = await apiClient.patch(`/expense/categories/${id}`, updates);
      return data;
    },
  });
}

export function useDeleteExpense() {
  return useMutation<ApiResponse<void>, Error, number>({
    mutationFn: async (id) => {
      const { data } = await apiClient.delete(`/expense/categories/${id}`);
      return data;
    },
  });
}

export function useAddOptimization() {
  return useMutation<
    ApiResponse<ExpenseOptimization>,
    Error,
    Partial<ExpenseOptimization>
  >({
    mutationFn: async (optimization) => {
      const { data } = await apiClient.post('/expense/optimizations', optimization);
      return data;
    },
  });
}

// Budget tracking and forecasting
export function useExpenseForecast(months: number = 12) {
  return useQuery<ApiResponse<Record<string, number>>>({
    queryKey: [...EXPENSE_KEYS.all, 'forecast', months],
    queryFn: async () => {
      const { data } = await apiClient.get('/expense/forecast', {
        params: { months },
      });
      return data;
    },
  });
}

export function useBudgetVariance(categoryId?: number) {
  return useQuery<ApiResponse<Record<string, number>>>({
    queryKey: [...EXPENSE_KEYS.all, 'budget-variance', categoryId],
    queryFn: async () => {
      const params = categoryId ? { categoryId } : {};
      const { data } = await apiClient.get('/expense/budget-variance', { params });
      return data;
    },
  });
}