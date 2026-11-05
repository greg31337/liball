using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one ordered explicit migration operation and its provenance.</summary>
public class MigrationStep
{
    /// <summary>The zero-based deterministic step position.</summary>
    public int Sequence;
    /// <summary>The explicit migration operation.</summary>
    public MigrationActionKind Action;
    /// <summary>The source paths consumed by this step.</summary>
    public List<XmlPathData> SourcePaths = new List<XmlPathData>();
    /// <summary>The target paths produced by this step.</summary>
    public List<XmlPathData> TargetPaths = new List<XmlPathData>();
    /// <summary>The source repository entity identifier when applicable.</summary>
    public string SourceIdentifier = string.Empty;
    /// <summary>The target repository entity identifier when applicable.</summary>
    public string TargetIdentifier = string.Empty;
    /// <summary>The source logical or schema type.</summary>
    public string SourceType = string.Empty;
    /// <summary>The target logical or schema type.</summary>
    public string TargetType = string.Empty;
    /// <summary>The explicit value used by a default-producing step.</summary>
    public string DefaultValue = string.Empty;
    /// <summary>The stable identifier of a built-in or caller conversion.</summary>
    public string ConverterIdentifier = string.Empty;
    /// <summary>The aligned source values of an explicit code mapping.</summary>
    public List<string> SourceCodeValues = new List<string>();
    /// <summary>The aligned target values of an explicit code mapping.</summary>
    public List<string> TargetCodeValues = new List<string>();
    /// <summary>Whether absence or failure of this step makes migration fail.</summary>
    public bool Required;
    /// <summary>Whether this step intentionally loses source information.</summary>
    public bool Lossy;
    /// <summary>The difference that caused this step when generated automatically.</summary>
    public DifferenceItem? Difference;
    /// <summary>The explicit mapping rule that supplied or overrode this step.</summary>
    public MappingRuleDefinition? MappingRule;
    /// <summary>The inspectable business or technical rationale for the step.</summary>
    public string Rationale = string.Empty;
}
