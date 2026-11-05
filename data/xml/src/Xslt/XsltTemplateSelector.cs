using System.Collections.Generic;

namespace xml;

internal class XsltTemplateSelector
{
    internal XsltTemplateSelectionResult SelectXsltTemplateRule(XsltStylesheetPlan stylesheet, string mode, XdmSequence candidate, XPathDynamicContext context)
    {
        Guard.NotNull(stylesheet, "stylesheet");
        Guard.NotNull(mode, "mode");
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(context, "context");
        XsltTemplateSelectionResult result = new XsltTemplateSelectionResult();
        XsltTemplateRuleIndex? ruleIndex = FindXsltTemplateRuleIndex(stylesheet, mode);
        if (ruleIndex == null || candidate.Count != 1)
        {
            return result;
        }

        List<XsltTemplateRulePlan> candidates = ReadXsltIndexedCandidates(ruleIndex, candidate);
        result.InspectedRuleCount = candidates.Count;
        XsltTemplateRulePlan? selected = null;
        int equalRankCount = 0;
        XsltTemplateRulePlan? related = null;
        int index = 0;
        while (index < candidates.Count)
        {
            XsltTemplateRulePlan current = candidates[index];
            if (new XsltPatternMatcher().MatchesXsltPattern(current.Pattern, candidate, context))
            {
                if (selected == null)
                {
                    selected = current;
                    equalRankCount = 1;
                }
                else
                {
                    int rank = CompareXsltTemplateRuleRank(current, selected);
                    if (rank > 0)
                    {
                        selected = current;
                        equalRankCount = 1;
                        related = null;
                    }
                    else if (rank == 0)
                    {
                        equalRankCount = equalRankCount + 1;
                        related = selected;
                        if (CompareXsltTemplateRuleDeclarationOrder(current, selected) > 0)
                        {
                            selected = current;
                        }
                    }
                }
            }

            index = index + 1;
        }

        result.Rule = selected;
        result.Template = selected == null ? null : selected.Template;
        result.Ambiguous = equalRankCount > 1;
        if (result.Ambiguous)
        {
            XmlDiagnostic diagnostic = CreateXsltTemplateConflictDiagnostic(selected, related, XmlDiagnosticSeverity.Warning);
            if (ruleIndex.ModePlan.OnMultipleMatch == XsltOnMultipleMatchBehavior.Fail)
            {
                diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
                throw new XsltDynamicException(diagnostic.Message == null ? "Multiple template rules match the selected item." : diagnostic.Message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
            }

            if (ruleIndex.ModePlan.WarningOnMultipleMatch)
            {
                result.Diagnostics = [diagnostic];
            }
        }

        return result;
    }

    internal XsltTemplateRuleIndex? FindXsltTemplateRuleIndex(XsltStylesheetPlan stylesheet, string mode)
    {
        int index = 0;
        while (index < stylesheet.TemplateRuleIndexes.Length)
        {
            if (stylesheet.TemplateRuleIndexes[index].Mode == mode)
            {
                return stylesheet.TemplateRuleIndexes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltTemplateSelectionResult SelectXsltTemplateRuleBelow(XsltStylesheetPlan stylesheet, string mode, XdmSequence candidate, XPathDynamicContext context, XsltTemplateRulePlan current, bool importsOnly)
    {
        Guard.NotNull(stylesheet, "stylesheet");
        Guard.NotNull(mode, "mode");
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(context, "context");
        Guard.NotNull(current, "current");
        XsltTemplateSelectionResult result = new XsltTemplateSelectionResult();
        XsltTemplateRuleIndex? ruleIndex = FindXsltTemplateRuleIndex(stylesheet, mode);
        if (ruleIndex == null || candidate.Count != 1)
        {
            return result;
        }

        List<XsltTemplateRulePlan> candidates = ReadXsltIndexedCandidates(ruleIndex, candidate);
        XsltTemplateRulePlan? selected = null;
        XsltTemplateRulePlan? related = null;
        int equalRankCount = 0;
        int index = 0;
        while (index < candidates.Count)
        {
            XsltTemplateRulePlan candidateRule = candidates[index];
            bool lower = importsOnly
                ? candidateRule.ImportPrecedence < current.ImportPrecedence
                : CompareXsltTemplateRuleRank(candidateRule, current) < 0 || CompareXsltTemplateRuleRank(candidateRule, current) == 0 && CompareXsltTemplateRuleDeclarationOrder(candidateRule, current) < 0;
            if (lower && new XsltPatternMatcher().MatchesXsltPattern(candidateRule.Pattern, candidate, context))
            {
                if (selected == null || CompareXsltTemplateRuleRank(candidateRule, selected) > 0)
                {
                    selected = candidateRule;
                    related = null;
                    equalRankCount = 1;
                }
                else if (CompareXsltTemplateRuleRank(candidateRule, selected) == 0)
                {
                    related = selected;
                    equalRankCount = equalRankCount + 1;
                    if (CompareXsltTemplateRuleDeclarationOrder(candidateRule, selected) > 0)
                    {
                        selected = candidateRule;
                    }
                }
            }

            index = index + 1;
        }

        result.Rule = selected;
        result.Template = selected == null ? null : selected.Template;
        result.InspectedRuleCount = candidates.Count;
        result.Ambiguous = equalRankCount > 1;
        if (result.Ambiguous && selected != null)
        {
            XmlDiagnostic diagnostic = CreateXsltTemplateConflictDiagnostic(selected, related, XmlDiagnosticSeverity.Warning);
            if (ruleIndex.ModePlan.OnMultipleMatch == XsltOnMultipleMatchBehavior.Fail)
            {
                diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
                throw new XsltDynamicException(diagnostic.Message == null ? "Multiple template rules match the selected item." : diagnostic.Message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
            }

            if (ruleIndex.ModePlan.WarningOnMultipleMatch)
            {
                result.Diagnostics = [diagnostic];
            }
        }

        return result;
    }

    internal List<XsltTemplateRulePlan> ReadXsltIndexedCandidates(XsltTemplateRuleIndex index, XdmSequence candidate)
    {
        List<XsltTemplateRulePlan> result = new List<XsltTemplateRulePlan>();
        AddXsltTemplateBucket(index, "*", result);
        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(candidate, 0);
        XmlNode? node = item.Node;
        if (node == null)
        {
            return result;
        }

        string kind = CreateXsltCandidateKindKey(node);
        AddXsltTemplateBucket(index, kind, result);
        if ((kind == "E" || kind == "A" || kind == "N") && node.Name != null)
        {
            string name = kind + "|Q{" + (node.Name.NamespaceUri == null ? "" : node.Name.NamespaceUri) + "}" + (node.Name.LocalName == null ? "" : node.Name.LocalName);
            AddXsltTemplateBucket(index, name, result);
        }

        return result;
    }

    internal void AddXsltTemplateBucket(XsltTemplateRuleIndex index, string key, List<XsltTemplateRulePlan> result)
    {
        if (!index.Buckets.ContainsKey(key))
        {
            return;
        }

        XsltTemplateRulePlan[] rules = index.Buckets[key];
        int itemIndex = 0;
        while (itemIndex < rules.Length)
        {
            result.Add(rules[itemIndex]);
            itemIndex = itemIndex + 1;
        }
    }

    internal string CreateXsltCandidateKindKey(XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Document)
        {
            return "D";
        }

        if (node.Kind == XmlNodeKind.Element)
        {
            return "E";
        }

        if (node.Kind == XmlNodeKind.Attribute)
        {
            return "A";
        }

        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            return "T";
        }

        if (node.Kind == XmlNodeKind.Comment)
        {
            return "C";
        }

        if (node.Kind == XmlNodeKind.ProcessingInstruction)
        {
            return "P";
        }

        if (node.Kind == XmlNodeKind.Namespace)
        {
            return "N";
        }

        return "*";
    }

    internal int CompareXsltTemplateRuleRank(XsltTemplateRulePlan left, XsltTemplateRulePlan right)
    {
        if (left.ImportPrecedence != right.ImportPrecedence)
        {
            return left.ImportPrecedence.CompareTo(right.ImportPrecedence);
        }

        return new XdmNumericDatatype().CompareXdmNumeric(left.PriorityValue, right.PriorityValue);
    }

    internal int CompareXsltTemplateRuleDeclarationOrder(XsltTemplateRulePlan left, XsltTemplateRulePlan right)
    {
        if (left.DeclarationOrder != right.DeclarationOrder)
        {
            return left.DeclarationOrder.CompareTo(right.DeclarationOrder);
        }

        return left.AlternativeOrder.CompareTo(right.AlternativeOrder);
    }

    internal XmlDiagnostic CreateXsltTemplateConflictDiagnostic(XsltTemplateRulePlan? selected, XsltTemplateRulePlan? related, XmlDiagnosticSeverity severity)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XsltErrorCodes.XTDE0540;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = severity;
        diagnostic.Message = "Multiple template rules with the same import precedence and priority match the selected item.";
        diagnostic.Range = selected == null ? null : selected.Template.Declaration.Range;
        SourceRange? relatedRange = related == null ? null : related.Template.Declaration.Range;
        diagnostic.RelatedLocation = relatedRange == null ? null : relatedRange.Start;
        return diagnostic;
    }
}
