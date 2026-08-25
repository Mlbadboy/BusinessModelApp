using System;
using BusinessModelApp.Core.DTOs.Revenue;
using BusinessModelApp.Core.DTOs.Expense;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Dtos
{
    public class BusinessModelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BusinessType { get; set; }
        public string Industry { get; set; }
        public string MarketSegment { get; set; }
        public string TargetMarket { get; set; }
        public string ValueProposition { get; set; }
        public string RevenueModel { get; set; }
        public string CostStructure { get; set; }
        public string KeyResources { get; set; }
        public string KeyActivities { get; set; }
        public string KeyPartners { get; set; }
        public string CustomerSegments { get; set; }
        public string Channels { get; set; }
        public string CustomerRelationships { get; set; }
        public string ValuePropositions { get; set; }
        public string RevenueStreams { get; set; }
        public string CostStructureDetails { get; set; }
        public string KeyMetrics { get; set; }
        public string RiskFactors { get; set; }
        public string GrowthStrategy { get; set; }
        public string CompetitiveAdvantage { get; set; }
        public string MarketOpportunities { get; set; }
        public decimal TargetCustomerAcquisitionCost { get; set; } // Target CAC
        public decimal CurrentCustomerAcquisitionCost { get; set; } // Current CAC
        public decimal TargetCustomerLifetimeValue { get; set; } // Target CLTV
        public decimal CurrentCustomerLifetimeValue { get; set; } // Current CLTV
        public decimal TargetChurnRate { get; set; } // Target churn rate
        public decimal CurrentChurnRate { get; set; } // Current churn rate
        public decimal TargetCustomerCount { get; set; } // Target number of customers
        public decimal CurrentCustomerCount { get; set; } // Current number of customers
        public decimal TargetAverageRevenuePerUser { get; set; } // Target ARPU
        public decimal CurrentAverageRevenuePerUser { get; set; } // Current ARPU
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; } // User who created the business model
        public string UpdatedBy { get; set; } // User who last updated the business model
        public ICollection<RevenueSourceDto> RevenueSources { get; set; } = new List<RevenueSourceDto>();
        public ICollection<ExpenseDto> Expenses { get; set; } = new List<ExpenseDto>();
        public ICollection<string> Tags { get; set; } = new List<string>(); // Tags for categorization
        public ICollection<string> Keywords { get; set; } = new List<string>(); // Search keywords
        public string Status { get; set; } // Business model status (Draft, Active, Archived)
        public string Type { get; set; } // Business model type (B2B, B2C, etc.)
        public string Region { get; set; } // Geographic region
        public string Currency { get; set; } // Base currency
        public decimal TargetOperatingMargin { get; set; } // Target operating margin
        public decimal CurrentOperatingMargin { get; set; } // Current operating margin
        public decimal TargetNetMargin { get; set; } // Target net margin
        public decimal CurrentNetMargin { get; set; } // Current net margin
        public decimal TargetReturnOnInvestment { get; set; } // Target ROI
        public decimal CurrentReturnOnInvestment { get; set; } // Current ROI
        public decimal TargetPaybackPeriod { get; set; } // Target payback period (months)
        public decimal CurrentPaybackPeriod { get; set; } // Current payback period (months)
        public string RiskLevel { get; set; } // Risk assessment level
        public string ProfitabilityTrend { get; set; } // Profitability trend (Up, Down, Stable)
        public string GrowthTrend { get; set; } // Growth trend (Up, Down, Stable)
        public string HealthStatus { get; set; } // Overall health status
    }
}
