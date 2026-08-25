export enum RiskSeverity {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum RiskProbability {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High'
}

export type MitigationStatus = 'Not Started' | 'In Progress' | 'Completed';

export interface StrategyRisk {
  id: string;
  name: string;
  description: string;
  severity: RiskSeverity;
  probability: RiskProbability;
  category: string;
  owner: string;
  isActive: boolean;
  affectedAreas: string[];
  mitigationStrategy: string;
  mitigationCost: number;
  mitigationStatus: MitigationStatus;
  createdAt: string;
  updatedAt: string;
}

export interface StrategyOpportunity {
  id: string;
  name: string;
  description: string;
  potentialImpact: number;
  probabilityOfSuccess: number;
  timeframe: string;
  resourceRequirements: string[];
  status: string;
  category: string;
  owner: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface PerformanceTrend {
  id: string;
  metricName: string;
  currentValue: number;
  previousValue: number;
  changePercentage: number;
  trend: 'up' | 'down' | 'stable';
  period: string;
  category: string;
  createdAt: string;
}