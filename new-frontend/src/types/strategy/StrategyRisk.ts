export enum RiskSeverity {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum RiskProbability {
  VeryLow = 'VeryLow',
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  VeryHigh = 'VeryHigh'
}

export type MitigationStatus = 'Not Started' | 'In Progress' | 'Completed';

export interface StrategyRisk {
  id: number;
  name: string;
  description: string;
  severity: RiskSeverity;
  probability: RiskProbability;
  impactScore: number;  // 0-100
  category: string;  // e.g., "Financial", "Operational", "Market", etc.
  
  mitigationStrategy: string;
  mitigationCost: number;
  mitigationStatus: MitigationStatus;
  
  identificationDate: Date;
  resolutionDate?: Date;
  owner: string;
  
  isActive: boolean;
  affectedAreas: string[];
  riskScore: number;  // Calculated based on Severity and Probability
  
  createdAt: Date;
  createdBy: string;
  updatedAt?: Date;
  updatedBy: string;
}