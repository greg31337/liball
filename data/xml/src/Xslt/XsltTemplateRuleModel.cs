using System.Collections.Generic;

namespace xml;

internal class XsltTemplateRulePlan
{
    internal XsltTemplatePlan Template;
    internal XsltMatchPatternPlan Pattern;
    internal string Mode;
    internal string Priority;
    internal XdmAtomicValue PriorityValue;
    internal int ImportPrecedence;
    internal int DeclarationOrder;
    internal int AlternativeOrder;
    internal string IndexKey;

    internal XsltTemplateRulePlan(XsltTemplatePlan template, XsltMatchPatternPlan pattern, string mode, string priority, XdmAtomicValue priorityValue, int alternativeOrder, string indexKey)
    {
        Template = template;
        Pattern = pattern;
        Mode = mode;
        Priority = priority;
        PriorityValue = priorityValue;
        ImportPrecedence = template.ImportPrecedence;
        DeclarationOrder = template.DeclarationOrder;
        AlternativeOrder = alternativeOrder;
        IndexKey = indexKey;
    }
}

internal class XsltTemplateRuleIndex
{
    internal string Mode;
    internal XsltModePlan ModePlan;
    internal XsltTemplateRulePlan[] Rules;
    internal Dictionary<string, XsltTemplateRulePlan[]> Buckets;

    internal XsltTemplateRuleIndex(string mode, XsltModePlan modePlan, XsltTemplateRulePlan[] rules, Dictionary<string, XsltTemplateRulePlan[]> buckets)
    {
        Mode = mode;
        ModePlan = modePlan;
        Rules = rules;
        Buckets = buckets;
    }
}

internal class XsltTemplateSelectionResult
{
    internal XsltTemplateRulePlan? Rule;
    internal XsltTemplatePlan? Template;
    internal XmlDiagnostic[] Diagnostics;
    internal bool Ambiguous;
    internal int InspectedRuleCount;

    internal XsltTemplateSelectionResult()
    {
        Rule = null;
        Template = null;
        Diagnostics = [];
        Ambiguous = false;
        InspectedRuleCount = 0;
    }
}
