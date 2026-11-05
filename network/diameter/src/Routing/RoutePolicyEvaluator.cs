using System;
using System.Collections.Generic;

namespace diameter;

internal class RoutePolicyEvaluator
{
    internal List<IRoutePolicy> Policies = new List<IRoutePolicy>();

    internal RoutePolicyEvaluator(List<IRoutePolicy> policies)
    {
        if (policies == null)
        {
            throw new ArgumentException(
                "Route policy storage is required.");
        }

        int index = 0;
        while (index < policies.Count)
        {
            if (policies[index] == null)
            {
                throw new ArgumentException(
                    "Route policy registrations cannot be null.");
            }

            Policies.Add(policies[index]);
            index = index + 1;
        }
    }

    internal DiameterRouteCandidate EvaluateRouteCandidate(
        DiameterRouteRequest request,
        DiameterRouteCandidate candidate)
    {
        DiameterRouteCandidate result =
            RouteTableCloner.CloneRouteCandidate(candidate);
        int index = 0;
        while (index < Policies.Count && !result.PolicyTerminal)
        {
            DiameterRouteRequest requestSnapshot =
                RouteTableCloner.CloneRouteRequest(request);
            DiameterRouteCandidate candidateSnapshot =
                RouteTableCloner.CloneRouteCandidate(result);
            DiameterRouteCandidate evaluated =
                Policies[index].EvaluateDiameterRouteCandidate(
                    requestSnapshot,
                    candidateSnapshot);
            if (evaluated == null || evaluated.Code == null ||
                evaluated.Message == null ||
                ValueCodecs.FindInvalidUtf16Index(evaluated.Code) >= 0 ||
                ValueCodecs.FindInvalidUtf16Index(evaluated.Message) >= 0)
            {
                result.PolicyAccepted = false;
                result.PolicyTerminal = true;
                result.Code = "route-policy-result-invalid";
                result.Message =
                    "A route policy returned an invalid candidate result.";
                return result;
            }

            result.PolicyScore = evaluated.PolicyScore;
            result.PolicyAccepted = result.PolicyAccepted &&
                evaluated.PolicyAccepted;
            result.PolicyTerminal = evaluated.PolicyTerminal;
            if (evaluated.Code.Length > 0)
            {
                result.Code = evaluated.Code;
            }

            if (evaluated.Message.Length > 0)
            {
                result.Message = evaluated.Message;
            }

            if (!result.PolicyAccepted && result.Code == "route-eligible")
            {
                result.Code = "route-policy-rejected";
                result.Message = "A route policy rejected the candidate.";
            }

            index = index + 1;
        }

        return result;
    }
}
