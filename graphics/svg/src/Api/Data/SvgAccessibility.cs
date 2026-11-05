namespace svg;

/// <summary>Contains one normalized ARIA state or property.</summary>
public class SvgAccessibilityValue
{
    /// <summary>The lowercase ARIA attribute name.</summary>
    public string Name;
    /// <summary>The normalized or preserved attribute value.</summary>
    public string? Value;
    /// <summary>Whether this is a state or property.</summary>
    public SvgAccessibilityValueKind Kind;
    /// <summary>The authored attribute.</summary>
    public SvgAttribute Source;
    /// <summary>Whether the mapped value is valid.</summary>
    public bool Valid;

    internal SvgAccessibilityValue(string name, string? value, SvgAccessibilityValueKind kind, SvgAttribute source, bool valid)
    {
        Name = name;
        Value = value;
        Kind = kind;
        Source = source;
        Valid = valid;
    }
}

/// <summary>Configures bounded accessible semantics calculation.</summary>
public class SvgAccessibilityOptions
{
    /// <summary>The maximum nodes visited while extracting referenced text.</summary>
    public int MaximumNodes;
    /// <summary>The maximum accumulated accessible text characters.</summary>
    public int MaximumTextCharacters;
    /// <summary>The maximum ID references accepted by one ARIA relationship.</summary>
    public int MaximumReferences;

    /// <summary>Creates secure accessibility calculation defaults.</summary>
    public SvgAccessibilityOptions()
    {
        MaximumNodes = 1000000;
        MaximumTextCharacters = 1000000;
        MaximumReferences = 100000;
    }
}

/// <summary>Contains the accessible semantics calculated for one resolved node.</summary>
public class SvgAccessibleSemantics
{
    /// <summary>The resolved source or use-instance node.</summary>
    public SvgResolvedNode? Node;
    /// <summary>The corresponding authored source node.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source node version captured during calculation.</summary>
    public long SourceVersion;
    /// <summary>The stable authored source path.</summary>
    public string SourcePath;
    /// <summary>The stable use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>The calculated accessible name.</summary>
    public string Name;
    /// <summary>The selected accessible-name source.</summary>
    public SvgAccessibleNameSource NameSource;
    /// <summary>The calculated accessible description.</summary>
    public string Description;
    /// <summary>The selected accessible-description source.</summary>
    public SvgAccessibleDescriptionSource DescriptionSource;
    /// <summary>The effective explicit or implicit semantic role.</summary>
    public string Role;
    /// <summary>Whether a recognized authored role supplied the effective role.</summary>
    public bool ExplicitRole;
    /// <summary>Whether this node is excluded by rendering or <c>aria-hidden</c>.</summary>
    public bool Hidden;
    /// <summary>Whether a valid presentation role removes this node's semantics.</summary>
    public bool Presentational;
    /// <summary>Whether the node can receive focus directly.</summary>
    public bool Focusable;
    /// <summary>Nodes used by the selected accessible name.</summary>
    public System.Collections.Generic.List<SvgNode> NameReferences;
    /// <summary>Nodes used by the selected accessible description.</summary>
    public System.Collections.Generic.List<SvgNode> DescriptionReferences;
    /// <summary>Valid and invalid mapped ARIA states.</summary>
    public System.Collections.Generic.List<SvgAccessibilityValue> States;
    /// <summary>Valid and invalid mapped ARIA properties.</summary>
    public System.Collections.Generic.List<SvgAccessibilityValue> Properties;
    /// <summary>Located recoverable and bounded semantic diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether bounded semantic calculation completed.</summary>
    public bool Success;

    internal SvgAccessibleSemantics(SvgResolvedNode? node)
    {
        Node = node;
        SourceNode = node == null ? null : node.SourceNode;
        SourceVersion = SourceNode == null ? 0 : SourceNode.Version;
        SourcePath = node == null ? "" : node.SourcePath;
        InstancePath = node == null ? "" : node.InstancePath;
        Name = "";
        NameSource = SvgAccessibleNameSource.None;
        Description = "";
        DescriptionSource = SvgAccessibleDescriptionSource.None;
        Role = "";
        ExplicitRole = false;
        Hidden = false;
        Presentational = false;
        Focusable = false;
        NameReferences = new System.Collections.Generic.List<SvgNode>();
        DescriptionReferences = new System.Collections.Generic.List<SvgNode>();
        States = new System.Collections.Generic.List<SvgAccessibilityValue>();
        Properties = new System.Collections.Generic.List<SvgAccessibilityValue>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Checks whether the authored source retains its captured version.</summary>
    /// <returns><c>true</c> when the semantic snapshot is current.</returns>
    internal bool IsCurrent()
    {
        return SourceNode != null && SourceNode.Version == SourceVersion;
    }
}
