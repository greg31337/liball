using System.Collections.Generic;

namespace iso20022;

/// <summary>Defines one ordered explicit mapping rule without embedded executable code.</summary>
public class MappingRuleDefinition
{
    /// <summary>The stable rule identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The zero-based deterministic rule position.</summary>
    public int Sequence;
    /// <summary>The explicit mapping operation.</summary>
    public MappingActionKind Action;
    /// <summary>The source repository entity identifier when this rule overrides entity matching.</summary>
    public string SourceIdentifier = string.Empty;
    /// <summary>The target repository entity identifier when this rule overrides entity matching.</summary>
    public string TargetIdentifier = string.Empty;
    /// <summary>The repository entity kind constrained by an identity override.</summary>
    public RepositoryEntityKind EntityKind;
    /// <summary>The source paths consumed by this rule.</summary>
    public List<XmlPathData> SourcePaths = new List<XmlPathData>();
    /// <summary>The target paths produced by this rule.</summary>
    public List<XmlPathData> TargetPaths = new List<XmlPathData>();
    /// <summary>The source generated-model field name when path-free model mapping is selected.</summary>
    public string SourceMember = string.Empty;
    /// <summary>The target generated-model field name when path-free model mapping is selected.</summary>
    public string TargetMember = string.Empty;
    /// <summary>The explicit value used by a constant or default-producing rule.</summary>
    public string ConstantValue = string.Empty;
    /// <summary>The stable identifier of the selected built-in or caller conversion.</summary>
    public string ConverterIdentifier = string.Empty;
    /// <summary>The aligned source values of an explicit code mapping.</summary>
    public List<string> SourceCodeValues = new List<string>();
    /// <summary>The aligned target values of an explicit code mapping.</summary>
    public List<string> TargetCodeValues = new List<string>();
    /// <summary>Whether absence of required source content makes mapping fail.</summary>
    public bool Required;
    /// <summary>Whether the rule intentionally loses source information.</summary>
    public bool Lossy;
    /// <summary>The inspectable business or technical rationale for the rule.</summary>
    public string Rationale = string.Empty;
}
