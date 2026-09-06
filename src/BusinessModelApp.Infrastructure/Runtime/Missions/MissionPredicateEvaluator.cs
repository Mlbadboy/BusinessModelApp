using System;
using System.Collections.Generic;
using System.Globalization;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class MissionPredicateEvaluator : IMissionPredicateEvaluator
    {
        public bool EvaluatePredicate(TypedPredicate predicate, IReadOnlyDictionary<string, object>? context)
        {
            if (predicate == null)
                return true;

            context ??= new Dictionary<string, object>();

            switch (predicate.Type)
            {
                case PredicateType.BooleanConstant:
                    return predicate.ConstantValue;

                case PredicateType.MetricComparison:
                    return EvaluateMetricComparison(predicate, context);

                case PredicateType.ArtifactPresence:
                    return EvaluateArtifactPresence(predicate, context);

                case PredicateType.NodeStateCheck:
                    return EvaluateNodeState(predicate, context);

                default:
                    return false;
            }
        }

        private static bool EvaluateMetricComparison(TypedPredicate predicate, IReadOnlyDictionary<string, object> context)
        {
            if (string.IsNullOrWhiteSpace(predicate.MetricName) || predicate.TargetValue == null)
                return false;

            if (!context.TryGetValue(predicate.MetricName, out var actualVal) || actualVal == null)
                return false;

            // Numeric comparison
            if (double.TryParse(actualVal.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var actualNum) &&
                double.TryParse(predicate.TargetValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var targetNum))
            {
                return predicate.Operator switch
                {
                    "==" => Math.Abs(actualNum - targetNum) < 0.00001,
                    "!=" => Math.Abs(actualNum - targetNum) >= 0.00001,
                    "<" => actualNum < targetNum,
                    "<=" => actualNum <= targetNum,
                    ">" => actualNum > targetNum,
                    ">=" => actualNum >= targetNum,
                    _ => false
                };
            }

            // String comparison (only == and !=)
            var actualStr = actualVal.ToString()?.Trim() ?? string.Empty;
            var targetStr = predicate.TargetValue.Trim();

            return predicate.Operator switch
            {
                "==" => string.Equals(actualStr, targetStr, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(actualStr, targetStr, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static bool EvaluateArtifactPresence(TypedPredicate predicate, IReadOnlyDictionary<string, object> context)
        {
            if (string.IsNullOrWhiteSpace(predicate.ArtifactName))
                return false;

            if (context.TryGetValue($"artifact:{predicate.ArtifactName}", out var present) && present is bool b)
                return b;

            return context.ContainsKey(predicate.ArtifactName);
        }

        private static bool EvaluateNodeState(TypedPredicate predicate, IReadOnlyDictionary<string, object> context)
        {
            if (predicate.CheckedNodeId == null || predicate.ExpectedNodeState == null)
                return false;

            var key = $"nodestate:{predicate.CheckedNodeId.Value.Value}";
            if (context.TryGetValue(key, out var stateObj) && stateObj is MissionNodeState actualState)
            {
                return actualState == predicate.ExpectedNodeState.Value;
            }

            return false;
        }
    }
}
