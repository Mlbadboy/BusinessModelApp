import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api, { endpoints, handleApiError } from '../utils/api';

interface ExpenseCategory {
  id: string;
  name: string;
  description: string;
  budget: number;
  actualSpend: number;
  variance: number;
  subCategories: Array<{
    id: string;
    name: string;
    amount: number;
  }>;
}

interface ExpenseTrend {
  period: string;
  totalExpenses: number;
  byCategory: Array<{
    category: string;
    amount: number;
    trend: number;
  }>;
  averageMonthlyBurn: number;
  projectedAnnualExpense: number;
}

interface ExpenseRisk {
  id: string;
  category: string;
  description: string;
  impact: 'high' | 'medium' | 'low';
  probability: number;
  potentialCost: number;
  mitigationPlan: string;
}

interface ExpenseOptimization {
  id: string;
  category: string;
  description: string;
  potentialSavings: number;
  implementationCost: number;
  timeToImplement: string;
  roi: number;
  status: 'proposed' | 'in-progress' | 'implemented';
}

interface ExpenseAnalysis {
  totalExpenses: number;
  fixedCosts: number;
  variableCosts: number;
  largestExpenses: Array<{
    category: string;
    amount: number;
    percentageOfTotal: number;
  }>;
  costDrivers: Array<{
    name: string;
    impact: number;
    trend: 'increasing' | 'decreasing' | 'stable';
  }>;
  efficiencyMetrics: {
    expenseRatio: number;
    costPerEmployee: number;
    overheadPercentage: number;
  };
}

export const useExpenses = () => {
  const queryClient = useQueryClient();

  // Fetch expense categories
  const {
    data: categories,
    isLoading: isLoadingCategories,
    error: categoriesError,
  } = useQuery<ExpenseCategory[]>({
    queryKey: ['expense-categories'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.expenses.categories);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch expense trends
  const {
    data: trends,
    isLoading: isLoadingTrends,
    error: trendsError,
  } = useQuery<ExpenseTrend>({
    queryKey: ['expense-trends'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.expenses.trends);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch expense risks
  const {
    data: risks,
    isLoading: isLoadingRisks,
    error: risksError,
  } = useQuery<ExpenseRisk[]>({
    queryKey: ['expense-risks'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.expenses.risks);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch optimization opportunities
  const {
    data: optimizations,
    isLoading: isLoadingOptimizations,
    error: optimizationsError,
  } = useQuery<ExpenseOptimization[]>({
    queryKey: ['expense-optimizations'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.expenses.optimization);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Fetch expense analysis
  const {
    data: analysis,
    isLoading: isLoadingAnalysis,
    error: analysisError,
  } = useQuery<ExpenseAnalysis>({
    queryKey: ['expense-analysis'],
    queryFn: async () => {
      try {
        const { data } = await api.get(endpoints.expenses.analysis);
        return data;
      } catch (error) {
        throw handleApiError(error);
      }
    },
  });

  // Update expense category
  const updateCategory = useMutation<
    ExpenseCategory,
    Error,
    { id: string; updates: Partial<ExpenseCategory> }
  >({
    mutationFn: async ({ id, updates }) => {
      const { data } = await api.patch(`${endpoints.expenses.categories}/${id}`, updates);
      return data;
    },
    onSuccess: () => {
      // Invalidate relevant queries
      queryClient.invalidateQueries(['expense-categories']);
      queryClient.invalidateQueries(['expense-analysis']);
    },
    onError: (error) => {
      throw handleApiError(error);
    },
  });

  // Update optimization status
  const updateOptimizationStatus = useMutation<
    ExpenseOptimization,
    Error,
    { id: string; status: ExpenseOptimization['status'] }
  >({
    mutationFn: async ({ id, status }) => {
      const { data } = await api.patch(`${endpoints.expenses.optimization}/${id}`, { status });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['expense-optimizations']);
    },
    onError: (error) => {
      throw handleApiError(error);
    },
  });

  return {
    categories,
    trends,
    risks,
    optimizations,
    analysis,
    updateCategory,
    updateOptimizationStatus,
    isLoading:
      isLoadingCategories ||
      isLoadingTrends ||
      isLoadingRisks ||
      isLoadingOptimizations ||
      isLoadingAnalysis,
    error: categoriesError || trendsError || risksError || optimizationsError || analysisError,
  };
};