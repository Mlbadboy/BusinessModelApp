using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.AI
{
    public interface IAIRoutingPolicyService
    {
        AIRoutingPolicy ResolvePolicy(AITaskType taskType, AIRoutingPreference preference = AIRoutingPreference.Balanced);
    }
}
