using System.Collections.Generic;

namespace cbor;

internal class CddlGenerationProductivity
{
    internal CddlGenerationState State;

    internal CddlGenerationProductivity(CddlGenerationState state)
    {
        State = state;
    }

    internal bool CanGenerateRuleValue(CddlRule rule)
    {
        return EvaluateRule(
            rule,
            new List<CddlRule>()
        );
    }

    internal bool CanGenerateNodeWithoutRules(
        CddlNode node,
        List<CddlRule> blocked)
    {
        List<CddlRule> visiting = new List<CddlRule>();
        int index = 0;
        while (index < blocked.Count)
        {
            if (!ContainsProductivityRule(visiting, blocked[index]))
            {
                visiting.Add(blocked[index]);
            }
            index = index + 1;
        }
        return EvaluateNode(node, visiting);
    }

    internal bool EvaluateRule(
        CddlRule rule,
        List<CddlRule> visiting)
    {
        if (rule.Value == null || ContainsProductivityRule(visiting, rule))
        {
            return false;
        }
        visiting.Add(rule);
        bool productive = EvaluateNode(rule.Value, visiting);
        visiting.RemoveAt(visiting.Count - 1);
        return productive;
    }

    internal bool EvaluateNode(
        CddlNode node,
        List<CddlRule> visiting)
    {
        if (node.Kind == CddlNodeKind.Literal)
        {
            return true;
        }
        if (node.Kind == CddlNodeKind.Representation)
        {
            return node.Children.Count == 0
                || EvaluateAllChildren(node, visiting);
        }
        if (node.Kind == CddlNodeKind.RuleReference)
        {
            return EvaluateRuleReference(node, visiting);
        }
        if (node.Kind == CddlNodeKind.TypeChoice
            || node.Kind == CddlNodeKind.GroupChoice)
        {
            return EvaluateAnyChildren(node, visiting);
        }
        if (node.Kind == CddlNodeKind.Group)
        {
            return EvaluateAllChildren(node, visiting);
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            return EvaluateGroupEntry(node, visiting);
        }
        if (node.Kind == CddlNodeKind.Array
            || node.Kind == CddlNodeKind.Map
            || node.Kind == CddlNodeKind.ParenthesizedType
            || node.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            return node.Children.Count == 1
                && EvaluateNode(node.Children[0], visiting);
        }
        if (node.Kind == CddlNodeKind.Range
            || node.Kind == CddlNodeKind.Tag)
        {
            return node.Children.Count > 0
                && EvaluateAllChildren(node, visiting);
        }
        if (node.Kind == CddlNodeKind.Control)
        {
            return node.Children.Count > 0
                && EvaluateNode(node.Children[0], visiting);
        }
        if (node.Kind == CddlNodeKind.Unwrap)
        {
            return EvaluateUnwrap(node, visiting);
        }
        if (node.Kind == CddlNodeKind.GroupEnumeration)
        {
            return EvaluateEnumeration(node, visiting);
        }
        return true;
    }

    internal bool EvaluateRuleReference(
        CddlNode node,
        List<CddlRule> visiting)
    {
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null)
        {
            return IsEmptyGroupSocket(node.Name);
        }
        return EvaluateRule(rule, visiting);
    }

    internal bool EvaluateGroupEntry(
        CddlNode node,
        List<CddlRule> visiting)
    {
        if (node.MinimumOccurrence == 0)
        {
            return true;
        }
        if (node.Children.Count == 0)
        {
            return false;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            if (!EvaluateNode(node.Children[index], visiting))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool EvaluateAllChildren(
        CddlNode node,
        List<CddlRule> visiting)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            if (!EvaluateNode(node.Children[index], visiting))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool EvaluateAnyChildren(
        CddlNode node,
        List<CddlRule> visiting)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            if (EvaluateNode(node.Children[index], visiting))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool EvaluateUnwrap(
        CddlNode node,
        List<CddlRule> visiting)
    {
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null)
        {
            return false;
        }
        return EvaluateRule(rule, visiting);
    }

    internal bool EvaluateEnumeration(
        CddlNode node,
        List<CddlRule> visiting)
    {
        if (node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule == null || rule.Value == null
                || ContainsProductivityRule(visiting, rule))
            {
                return false;
            }
            visiting.Add(rule);
            bool named = EvaluateEnumerationNode(rule.Value, visiting);
            visiting.RemoveAt(visiting.Count - 1);
            return named;
        }
        if (node.Children.Count == 0)
        {
            return false;
        }
        return EvaluateEnumerationNode(node.Children[0], visiting);
    }

    internal bool EvaluateEnumerationNode(
        CddlNode node,
        List<CddlRule> visiting)
    {
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            return EvaluateAnyChildren(node, visiting);
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            return node.Children.Count > 0
                && EvaluateEnumerationNode(
                    node.Children[node.Children.Count - 1],
                    visiting
                );
        }
        if (node.Kind == CddlNodeKind.RuleReference)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null && rule.Kind == CddlRuleKind.Group
                && rule.Value != null
                && !ContainsProductivityRule(visiting, rule))
            {
                visiting.Add(rule);
                bool included = EvaluateEnumerationNode(
                    rule.Value,
                    visiting
                );
                visiting.RemoveAt(visiting.Count - 1);
                return included;
            }
        }
        return EvaluateNode(node, visiting);
    }

    internal bool ContainsProductivityRule(
        List<CddlRule> rules,
        CddlRule rule)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index] == rule)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool IsEmptyGroupSocket(string? name)
    {
        return name != null && name.Length > 1
            && name[0] == '$' && name[1] == '$';
    }
}
