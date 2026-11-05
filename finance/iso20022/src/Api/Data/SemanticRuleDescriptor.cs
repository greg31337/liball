using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes the stable identity, order, severity, and applicability of one semantic rule.</summary>
public class SemanticRuleDescriptor
{
    /// <summary>The globally stable semantic rule identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The human-readable rule name.</summary>
    public string Name = string.Empty;
    /// <summary>The human-readable rule definition.</summary>
    public string Definition = string.Empty;
    /// <summary>The default diagnostic severity when the rule fails.</summary>
    public ValidationSeverity Severity = ValidationSeverity.Error;
    /// <summary>The deterministic rule order within the semantic validation stage.</summary>
    public int Order;
    /// <summary>Whether the rule is enabled when no profile overrides it.</summary>
    public bool Enabled = true;
    /// <summary>The exact message identifiers to which the rule applies, or an empty list for any identifier.</summary>
    public List<MessageIdentifier> MessageIdentifiers = new List<MessageIdentifier>();
    /// <summary>The version policy to which the rule applies when supplied.</summary>
    public VersionSelector? VersionSelector;
    /// <summary>The profile identifiers to which the rule applies, or an empty list for any profile.</summary>
    public List<string> ProfileIdentifiers = new List<string>();
    /// <summary>The qualified paths to which the rule applies, or an empty list for the complete message.</summary>
    public List<XmlPathData> Paths = new List<XmlPathData>();
    /// <summary>The repository, profile, or caller source identifier that supplied the rule.</summary>
    public string SourceIdentifier = string.Empty;
}
