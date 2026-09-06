using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Governance
{
    public enum BusinessPriorityHierarchy
    {
        Survival = 1,
        Cash = 2,
        Customer = 3,
        Revenue = 4,
        Margin = 5,
        Growth = 6,
        Optimization = 7
    }

    public class BusinessConstitution
    {
        public Guid WorkspaceId { get; init; }
        public string CompanyName { get; set; } = string.Empty;
        public BusinessControlMode DefaultControlMode { get; set; } = BusinessControlMode.EXECUTE_WITH_APPROVAL;
        public decimal MaxAutonomousSpendPerDay { get; set; } = 500.00m;
        public decimal MaxAutonomousSpendPerAction { get; set; } = 50.00m;
        public int MaxPermittedAutonomousRiskTier { get; set; } = 1; // R1 reversible by default
        public decimal MinimumGrossMarginThreshold { get; set; } = 0.20m;
        public int MinimumDaysCashRunway { get; set; } = 60;
        public List<BusinessPriorityHierarchy> PriorityOrder { get; set; } = new()
        {
            BusinessPriorityHierarchy.Survival,
            BusinessPriorityHierarchy.Cash,
            BusinessPriorityHierarchy.Customer,
            BusinessPriorityHierarchy.Revenue,
            BusinessPriorityHierarchy.Margin,
            BusinessPriorityHierarchy.Growth,
            BusinessPriorityHierarchy.Optimization
        };

        public bool RequiresHumanApproval(decimal amount, int riskTier, BusinessControlMode mode)
        {
            if (mode == BusinessControlMode.OBSERVE || mode == BusinessControlMode.ADVISE || mode == BusinessControlMode.SIMULATE)
                return false; // Zero side-effects

            if (mode == BusinessControlMode.EXECUTE_WITH_APPROVAL || mode == BusinessControlMode.PREPARE)
                return true;

            if (amount > MaxAutonomousSpendPerAction || riskTier > MaxPermittedAutonomousRiskTier)
                return true;

            return false;
        }
    }
}
