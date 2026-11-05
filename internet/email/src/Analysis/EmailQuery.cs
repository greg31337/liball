using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailQueryEvaluationBudget
{
    int maximumNodeCount;
    int nodeCount;

    internal EmailQueryEvaluationBudget(int maximumNodeCount)
    {
        maximumNodeCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(maximumNodeCount, "maximumNodeCount");
        this.maximumNodeCount = maximumNodeCount;
        nodeCount = 0;
    }

    internal void AddNode()
    {
        if (nodeCount >= maximumNodeCount)
        {
            throw new EmailLimitException("MaxQueryNodeCount", maximumNodeCount, nodeCount + 1);
        }

        nodeCount++;
    }
}
