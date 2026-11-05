using System;
using System.Collections.Generic;

namespace xml;

internal class XsltTemplateRuleCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltTemplateRuleCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltTemplateRules(XsltStylesheetPlan stylesheet)
    {
        List<XsltTemplateRuleIndex> indexes = new List<XsltTemplateRuleIndex>();
        int modeIndex = 0;
        while (modeIndex < stylesheet.Modes.Length)
        {
            XsltModePlan mode = stylesheet.Modes[modeIndex];
            List<XsltTemplateRulePlan> rules = new List<XsltTemplateRulePlan>();
            int templateIndex = 0;
            while (templateIndex < stylesheet.Templates.Length)
            {
                XsltTemplatePlan template = stylesheet.Templates[templateIndex];
                if (template.IsTemplateRule && IsXsltTemplateRuleInMode(template, mode.Name))
                {
                    AddXsltTemplateRules(template, mode.Name, rules);
                }

                templateIndex = templateIndex + 1;
            }

            indexes.Add(CreateXsltTemplateRuleIndex(mode, rules));
            modeIndex = modeIndex + 1;
        }

        stylesheet.TemplateRuleIndexes = indexes.ToArray();
    }

    internal bool IsXsltTemplateRuleInMode(XsltTemplatePlan template, string mode)
    {
        int index = 0;
        while (index < template.Modes.Length)
        {
            if (template.Modes[index].IsAll || template.Modes[index].Name == mode)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddXsltTemplateRules(XsltTemplatePlan template, string mode, List<XsltTemplateRulePlan> rules)
    {
        XsltMatchPatternPlan? pattern = template.MatchPlan;
        if (pattern == null || pattern.Expression == null)
        {
            return;
        }

        if (template.HasExplicitPriority || pattern.Expression.Kind != XPathAstKind.Union && !IsXsltParenthesizedUnion(pattern.Expression))
        {
            rules.Add(CreateXsltTemplateRule(template, pattern, mode, 0));
            return;
        }

        List<XPathAstNode> alternatives = new List<XPathAstNode>();
        CollectXsltUnionAlternatives(pattern.Expression, alternatives);
        XPathStaticContext context = new XsltVariableCompiler(Loader).CreateXsltVariableXPathContext(template.StaticContext, []);
        new XsltPatternCompiler(Loader).AddXsltPatternKeyBindings(context);
        int index = 0;
        while (index < alternatives.Count)
        {
            string source = ReadXsltPatternFragment(pattern.Source, alternatives[index]);
            XsltMatchPatternPlan alternative = new XsltMatchPatternPlan(source, alternatives[index].Range);
            new XsltPatternCompiler(Loader).CompileXsltPattern(alternative, context, template.Module.BaseUri);
            rules.Add(CreateXsltTemplateRule(template, alternative, mode, index));
            index = index + 1;
        }
    }

    internal bool IsXsltParenthesizedUnion(XPathAstNode expression)
    {
        XPathAstNode current = expression;
        while (current.Kind == XPathAstKind.Parenthesized && current.Operand != null)
        {
            current = current.Operand;
        }

        return current.Kind == XPathAstKind.Union;
    }

    internal void CollectXsltUnionAlternatives(XPathAstNode expression, List<XPathAstNode> alternatives)
    {
        if (expression.Kind == XPathAstKind.Parenthesized && expression.Operand != null)
        {
            CollectXsltUnionAlternatives(expression.Operand, alternatives);
            return;
        }

        if (expression.Kind == XPathAstKind.Union && expression.Left != null && expression.Right != null)
        {
            CollectXsltUnionAlternatives(expression.Left, alternatives);
            CollectXsltUnionAlternatives(expression.Right, alternatives);
            return;
        }

        alternatives.Add(expression);
    }

    internal string ReadXsltPatternFragment(string source, XPathAstNode expression)
    {
        SourceLocation? startLocation = expression.Range.Start;
        SourceLocation? endLocation = expression.Range.End;
        long startOffset = startLocation == null ? 0 : startLocation.CharacterOffset;
        long endOffset = endLocation == null ? source.Length : endLocation.CharacterOffset;
        if (startOffset < 0 || endOffset < startOffset || endOffset > source.Length)
        {
            return source;
        }

        return source.Substring((int)startOffset, (int)(endOffset - startOffset));
    }

    internal XsltTemplateRulePlan CreateXsltTemplateRule(XsltTemplatePlan template, XsltMatchPatternPlan pattern, string mode, int alternativeOrder)
    {
        string priority = template.HasExplicitPriority && template.Priority != null ? template.Priority : CalculateXsltDefaultPriority(pattern);
        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmDecimal(priority);
        if (!parsed.Success || parsed.Value == null)
        {
            throw new InvalidOperationException("A compiled template rule has an invalid priority.");
        }

        string canonical = parsed.Value.CanonicalValue == null ? priority : parsed.Value.CanonicalValue;
        string indexKey = CreateXsltPatternIndexKey(pattern.Expression);
        return new XsltTemplateRulePlan(template, pattern, mode, canonical, parsed.Value, alternativeOrder, indexKey);
    }

    internal string CalculateXsltDefaultPriority(XsltMatchPatternPlan pattern)
    {
        XPathAstNode? expression = pattern.Expression;
        if (expression == null)
        {
            return "0.5";
        }

        if (pattern.IsPredicatePattern)
        {
            return expression.Kind == XPathAstKind.ContextItem ? "-1" : "1";
        }

        return CalculateXsltExpressionDefaultPriority(expression);
    }

    internal string CalculateXsltExpressionDefaultPriority(XPathAstNode expression)
    {
        XPathAstNode current = expression;
        while (current.Kind == XPathAstKind.Parenthesized && current.Operand != null)
        {
            current = current.Operand;
        }

        while ((current.Kind == XPathAstKind.Intersect || current.Kind == XPathAstKind.Except) && current.Left != null)
        {
            current = current.Left;
            while (current.Kind == XPathAstKind.Parenthesized && current.Operand != null)
            {
                current = current.Operand;
            }
        }

        if (current.Kind == XPathAstKind.Path)
        {
            return current.Absolute && current.Children.Length == 0 ? "-0.5" : "0.5";
        }

        if (current.Kind == XPathAstKind.Predicate)
        {
            return "0.5";
        }

        XPathNodeTest? test = current.NodeTest;
        if (test == null)
        {
            return "0.5";
        }

        return CalculateXsltNodeTestDefaultPriority(test);
    }

    internal string CalculateXsltNodeTestDefaultPriority(XPathNodeTest test)
    {
        if (test.Kind == XPathNodeTestKind.Name)
        {
            return "0";
        }

        if (test.Kind == XPathNodeTestKind.NamespaceWildcard || test.Kind == XPathNodeTestKind.LocalNameWildcard || test.Kind == XPathNodeTestKind.UriWildcard)
        {
            return "-0.25";
        }

        if (test.Kind == XPathNodeTestKind.SchemaElement || test.Kind == XPathNodeTestKind.SchemaAttribute)
        {
            return "0.25";
        }

        if (test.Kind == XPathNodeTestKind.Element || test.Kind == XPathNodeTestKind.Attribute)
        {
            bool named = test.LocalName != null && test.LocalName != "*";
            if (named && test.Type != null)
            {
                return "0.25";
            }

            if (named || test.Type != null)
            {
                return "0";
            }

            return "-0.5";
        }

        if (test.Kind == XPathNodeTestKind.Document)
        {
            return test.InnerTest == null ? "-0.5" : CalculateXsltNodeTestDefaultPriority(test.InnerTest);
        }

        if (test.Kind == XPathNodeTestKind.ProcessingInstruction && test.Value != null)
        {
            return "0";
        }

        return "-0.5";
    }

    internal XsltTemplateRuleIndex CreateXsltTemplateRuleIndex(XsltModePlan mode, List<XsltTemplateRulePlan> rules)
    {
        Dictionary<string, List<XsltTemplateRulePlan>> mutable = new Dictionary<string, List<XsltTemplateRulePlan>>();
        int index = 0;
        while (index < rules.Count)
        {
            XsltTemplateRulePlan rule = rules[index];
            if (!mutable.ContainsKey(rule.IndexKey))
            {
                mutable[rule.IndexKey] = new List<XsltTemplateRulePlan>();
            }

            mutable[rule.IndexKey].Add(rule);
            index = index + 1;
        }

        Dictionary<string, XsltTemplateRulePlan[]> buckets = new Dictionary<string, XsltTemplateRulePlan[]>();
        foreach (KeyValuePair<string, List<XsltTemplateRulePlan>> entry in mutable)
        {
            buckets[entry.Key] = entry.Value.ToArray();
        }

        return new XsltTemplateRuleIndex(mode.Name, mode, rules.ToArray(), buckets);
    }

    internal string CreateXsltPatternIndexKey(XPathAstNode? expression)
    {
        if (expression == null)
        {
            return "*";
        }

        XPathAstNode current = expression;
        while (current.Kind == XPathAstKind.Parenthesized && current.Operand != null)
        {
            current = current.Operand;
        }

        if (current.Kind == XPathAstKind.Intersect || current.Kind == XPathAstKind.Except)
        {
            return CreateXsltPatternIndexKey(current.Left);
        }

        if (current.Kind == XPathAstKind.Path)
        {
            if (current.Children.Length == 0)
            {
                return "D";
            }

            current = current.Children[current.Children.Length - 1];
        }

        while (current.Kind == XPathAstKind.Predicate && current.Left != null)
        {
            current = current.Left;
        }

        XPathNodeTest? test = current.NodeTest;
        if (test == null)
        {
            return "*";
        }

        string principal = current.Axis == XPathAxis.Attribute ? "A" : current.Axis == XPathAxis.Namespace ? "N" : "E";
        if (test.Kind == XPathNodeTestKind.Name && test.ResolvedName != null)
        {
            return principal + "|Q{" + (test.ResolvedName.NamespaceUri == null ? "" : test.ResolvedName.NamespaceUri) + "}" + (test.ResolvedName.LocalName == null ? "" : test.ResolvedName.LocalName);
        }

        if (test.Kind == XPathNodeTestKind.Element)
        {
            if (test.ResolvedName != null && test.LocalName != "*")
            {
                return "E|Q{" + (test.ResolvedName.NamespaceUri == null ? "" : test.ResolvedName.NamespaceUri) + "}" + (test.ResolvedName.LocalName == null ? "" : test.ResolvedName.LocalName);
            }

            return "E";
        }

        if (test.Kind == XPathNodeTestKind.Attribute)
        {
            if (test.ResolvedName != null && test.LocalName != "*")
            {
                return "A|Q{" + (test.ResolvedName.NamespaceUri == null ? "" : test.ResolvedName.NamespaceUri) + "}" + (test.ResolvedName.LocalName == null ? "" : test.ResolvedName.LocalName);
            }

            return "A";
        }

        if (test.Kind == XPathNodeTestKind.SchemaElement || test.Kind == XPathNodeTestKind.Wildcard || test.Kind == XPathNodeTestKind.NamespaceWildcard || test.Kind == XPathNodeTestKind.LocalNameWildcard || test.Kind == XPathNodeTestKind.UriWildcard)
        {
            return principal;
        }

        if (test.Kind == XPathNodeTestKind.SchemaAttribute)
        {
            return "A";
        }

        if (test.Kind == XPathNodeTestKind.Document)
        {
            return "D";
        }

        if (test.Kind == XPathNodeTestKind.Text)
        {
            return "T";
        }

        if (test.Kind == XPathNodeTestKind.Comment)
        {
            return "C";
        }

        if (test.Kind == XPathNodeTestKind.ProcessingInstruction)
        {
            return "P";
        }

        if (test.Kind == XPathNodeTestKind.NamespaceNode)
        {
            return "N";
        }

        return "*";
    }
}
