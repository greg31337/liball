namespace iso20022;

internal class SemanticRuleSelection
{
    internal ISemanticRule? Rule;
    internal SemanticRuleDescriptor Descriptor = new SemanticRuleDescriptor();
    internal string Identifier = string.Empty;
    internal ValidationSeverity Severity;
    internal int Order;
    internal int SourceOrder;
    internal bool Enabled;
    internal bool HasSeverityOverride;
    internal ProfileDefinition? Profile;
}
