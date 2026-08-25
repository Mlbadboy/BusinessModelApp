export interface FinancialPerformance {
  revenue: number;
  expenses: number;
  netProfit: number;
  grossMargin: number;
  operatingMargin: number;
  profitMargin: number;
  cashFlow: number;
  roi: number;
  roce: number;
  periodStart: Date;
  periodEnd: Date;
  yearOverYearGrowth: number;
  quarterOverQuarterGrowth: number;
  trendDirection: string;
  healthIndicator: string;
  breakEvenPoint: number;
  paybackPeriodMonths: number;
}