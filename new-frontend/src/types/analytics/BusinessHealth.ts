export enum RiskLevel {
  Low = 'Low',
  Moderate = 'Moderate',
  High = 'High',
  Critical = 'Critical'
}

export interface BusinessHealth {
  overallHealth: string;
  financialHealthScore: number;
  operationalHealthScore: number;
  marketHealthScore: number;
  customerHealthScore: number;
  growthHealthScore: number;
  
  riskAssessment: RiskLevel;
  warnings: string[];
  recommendations: string[];
  
  cashRunway: number;  // Months of operation possible with current cash
  burnRate: number;
  customerRetentionRate: number;
  customerAcquisitionRate: number;
  marketSharePercentage: number;
  employeeProductivity: number;
  resourceUtilization: number;
  
  assessmentDate: Date;
  assessedBy: string;
  improvementAreas: string[];
  keyMetrics: Record<string, number>;
}