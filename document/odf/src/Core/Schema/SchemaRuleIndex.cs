using System.Collections.Generic;

namespace odf;

internal class SchemaRuleIndex
{
    internal SchemaRuleSet RuleSet;

    internal SchemaRuleIndex(SchemaRuleSet ruleSet)
    {
        if (ruleSet == null)
        {
            throw new OdfValidationException("A schema rule set is required.");
        }
        RuleSet = ruleSet;
    }

    internal SchemaRule[] FindDefinitionRules(string name)
    {
        List<SchemaRule> result = new List<SchemaRule>();
        foreach (SchemaRule rule in RuleSet.Rules)
        {
            if (!rule.IsStart && rule.Name == name)
            {
                result.Add(rule);
            }
        }
        return result.ToArray();
    }

    internal SchemaRule[] FindStartRules()
    {
        List<SchemaRule> result = new List<SchemaRule>();
        foreach (SchemaRule rule in RuleSet.Rules)
        {
            if (rule.IsStart)
            {
                result.Add(rule);
            }
        }
        return result.ToArray();
    }

    internal SchemaElementRule[] FindElementRules(
        string namespaceName,
        string localName)
    {
        List<SchemaElementRule> result = new List<SchemaElementRule>();
        foreach (SchemaElementRule rule in RuleSet.Elements)
        {
            if (rule.NamespaceName == namespaceName
                && rule.LocalName == localName)
            {
                result.Add(rule);
            }
        }
        return result.ToArray();
    }

    internal SchemaAttributeRule[] FindAttributeRules(
        string namespaceName,
        string localName)
    {
        List<SchemaAttributeRule> result = new List<SchemaAttributeRule>();
        foreach (SchemaAttributeRule rule in RuleSet.Attributes)
        {
            if (rule.NamespaceName == namespaceName
                && rule.LocalName == localName)
            {
                result.Add(rule);
            }
        }
        return result.ToArray();
    }

    internal SchemaPattern PatternAt(int index)
    {
        if (index < 0 || index >= RuleSet.Patterns.Length)
        {
            throw new OdfValidationException(
                "The schema pattern index is outside the rule table.");
        }
        return RuleSet.Patterns[index];
    }

    internal int[] ChildPatterns(SchemaPattern pattern)
    {
        if (pattern == null)
        {
            throw new OdfValidationException("A schema pattern is required.");
        }
        int[] result = new int[pattern.ChildCount];
        int index = 0;
        while (index < pattern.ChildCount)
        {
            result[index] = RuleSet.PatternChildIndexes[
                pattern.FirstChildIndex + index];
            index = index + 1;
        }
        return result;
    }
}
