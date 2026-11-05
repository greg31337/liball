using System.Collections.Generic;

namespace iso20022;

internal class BuiltInSemanticRule : ISemanticRule
{
    internal const int TransactionCount = 1;
    internal const int ControlSum = 2;
    internal const int AmountCurrency = 3;
    internal const int Chronology = 4;
    internal const int ReferenceUniqueness = 5;
    internal const int HeaderBodyMatch = 6;
    internal const int BusinessIdentifierUniqueness = 7;

    int kind;
    SemanticRuleDescriptor descriptor = new SemanticRuleDescriptor();

    internal BuiltInSemanticRule(int ruleKind)
    {
        kind = ruleKind;
        descriptor = BuiltInSemanticRuleEvaluator.CreateDescriptor(ruleKind);
    }

    public SemanticRuleDescriptor GetSemanticRuleDescriptor()
    {
        return descriptor;
    }

    public List<ValidationDiagnostic> ValidateSemanticRule(
        ValidationRequest? request,
        XmlPathData? path)
    {
        return BuiltInSemanticRuleEvaluator.EvaluateRule(request, kind);
    }
}
