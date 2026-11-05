using System.Collections.Generic;

namespace cbor;

internal class CddlValidationRuleStack
{
    internal List<CddlRule> Rules = new List<CddlRule>();

    internal CddlValidationRuleStack()
    {
    }

    internal void PushRule(CddlRule rule)
    {
        Rules.Add(rule);
    }

    internal void PopRule()
    {
        if (Rules.Count > 0)
        {
            Rules.RemoveAt(Rules.Count - 1);
        }
    }

    internal CddlRule? PeekRule()
    {
        if (Rules.Count == 0)
        {
            return null;
        }
        return Rules[Rules.Count - 1];
    }

    internal bool ContainsRule(CddlRule rule)
    {
        int index = 0;
        while (index < Rules.Count)
        {
            if (Rules[index] == rule)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
