export interface RevenueTrend {
  id: number;
  period: string;
  value: number;
  growthRate: number;
  trend: 'up' | 'down' | 'stable';
}

export interface RevenueSourcePerformance {
  sourceId: number;
  sourceName: string;
  revenue: number;
  growth: number;
  contribution: number;  // Percentage of total revenue
  profitMargin: number;
}

export interface RevenueRisk {
  id: number;
  name: string;
  description: string;
  impact: number;  // 0-100
  probability: number;  // 0-100
  mitigationPlan: string;
}

export interface RevenueOpportunity {
  id: number;
  name: string;
  description: string;
  potentialValue: number;
  probability: number;  // 0-100
  timeframe: string;
  requiredResources: string[];
}

export interface RevenueAnalysis {
  id: number;
  name: string;
  analysisDate: Date;
  
  // Revenue Overview
  totalRevenue: number;
  recurringRevenue: number;
  oneTimeRevenue: number;
  projectedRevenue: number;
  
  // Growth Metrics
  yearOverYearGrowth: number;
  quarterOverQuarterGrowth: number;
  monthOverMonthGrowth: number;
  
  // Revenue Sources
  sourcePerformance: RevenueSourcePerformance[];
  revenueBreakdown: Record<string, number>;
  revenueTrends: RevenueTrend[];
  revenueBySource: Record<string, number>;
  
  // Customer Metrics
  averageRevenuePerUser: number;
  customerLifetimeValue: number;
  churnRate: number;
  
  // Financial Health
  profitMargin: number;
  grossMargin: number;
  operatingMargin: number;
  
  // Trends and Forecasts
  trends: RevenueTrend[];
  forecastAccuracy: number;
  forecasts: Record<string, number>;
  
  // Risk Analysis
  risks: RevenueRisk[];
  riskScore: number;
  
  // Opportunities
  opportunities: RevenueOpportunity[];
  opportunityValue: number;
  
  // Meta Information
  analyzedBy: string;
  createdAt: Date;
  updatedAt?: Date;
  status: string;
}