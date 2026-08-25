export * from './analytics/FinancialPerformance';
export * from './analytics/BusinessHealth';
export * from './strategy/StrategyRisk';
export * from './revenue';
export * from './expense';

// Common Types
export type Status = 'draft' | 'pending' | 'active' | 'completed' | 'archived';
export type TimeFrame = 'daily' | 'weekly' | 'monthly' | 'quarterly' | 'yearly';
export type TrendDirection = 'up' | 'down' | 'stable';

// Utility Types
export type DateRange = {
  startDate: Date;
  endDate: Date;
};

export type MetricValue = {
  value: number;
  unit: string;
  change?: number;
  trend?: TrendDirection;
};

export type ValidationError = {
  field: string;
  message: string;
};