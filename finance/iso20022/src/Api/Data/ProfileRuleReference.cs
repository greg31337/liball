namespace iso20022;

/// <summary>Enables, removes, or overrides one semantic rule in a profile overlay.</summary>
public class ProfileRuleReference
{
    /// <summary>The stable identifier of the referenced semantic rule.</summary>
    public string RuleIdentifier = string.Empty;
    /// <summary>An inline descriptor when the profile defines the referenced rule.</summary>
    public SemanticRuleDescriptor? Descriptor;
    /// <summary>Whether the referenced rule is enabled by this profile.</summary>
    public bool Enabled = true;
    /// <summary>Whether the referenced inherited rule is removed by this profile.</summary>
    public bool RemoveInheritedRule;
    /// <summary>Whether this profile overrides the descriptor diagnostic severity.</summary>
    public bool HasSeverityOverride;
    /// <summary>The profile-specific diagnostic severity when an override is supplied.</summary>
    public ValidationSeverity Severity = ValidationSeverity.Error;
    /// <summary>Whether this profile overrides the descriptor execution order.</summary>
    public bool HasOrderOverride;
    /// <summary>The profile-specific deterministic execution order when supplied.</summary>
    public int Order;
}
