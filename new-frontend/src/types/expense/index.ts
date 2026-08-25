export interface ExpenseCategory {
  id: number;
  name: string;
  description: string;
  totalAmount: number;
  budget: number;
  variance: number;
  percentageOfTotal: number;
}

export interface ExpenseTrend {
  id: number;
  period: string;
  amount: number;
  growthRate: number;
  trend: 'increasing' | 'decreasing' | 'stable';
}

export interface ExpenseRisk {
  id: number;
  name: string;
  description: string;
  impact: number;  // 0-100
  probability: number;  // 0-100
  mitigationPlan: string;
}

export interface ExpenseOptimization {
  id: number;
  name: string;
  description: string;
  potentialSavings: number;
  implementationCost: number;
  timeToImplement: string;
  priority: 'low' | 'medium' | 'high';
}

export interface ExpenseAnalysis {
  id: number;
  name: string;
  analysisDate: Date;
  
  // Expense Overview
  totalExpenses: number;
  fixedExpenses: number;
  variableExpenses: number;
  projectedExpenses: number;
  
  // Growth Metrics
  yearOverYearGrowth: number;
  quarterOverQuarterGrowth: number;
  monthOverMonthGrowth: number;
  
  // Expense Categories
  categories: ExpenseCategory[];
  expenseBreakdown: Record<string, number>;
  expenseTrends: ExpenseTrend[];
  categoryPerformance: ExpenseCategory[];
  expensesByCategory: Record<string, number>;
  
  // Budget Analysis
  budgetVariance: number;
  budgetUtilization: number;  // Percentage
  forecastAccuracy: number;  // Percentage
  
  // Efficiency Metrics
  costPerRevenueDollar: number;
  operatingEfficiencyRatio: number;
  expenseToIncomeRatio: number;
  
  // Trends and Forecasts
  trends: ExpenseTrend[];
  forecasts: Record<string, number>;
  
  // Risk Analysis
  risks: ExpenseRisk[];
  riskScore: number;
  
  // Optimization Opportunities
  optimizations: ExpenseOptimization[];
  potentialSavings: number;
  
  // Compliance and Policy
  isCompliant: boolean;
  complianceIssues: string[];
  policyViolations: string[];
  
  // Meta Information
  analyzedBy: string;
  createdAt: Date;
  updatedAt?: Date;
  status: string;
}