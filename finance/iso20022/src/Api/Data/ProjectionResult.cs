using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains a task-oriented view with exact source identity, paths, and unmapped data.</summary>
public class ProjectionResult
{
    /// <summary>Whether projection and requested source validation completed successfully.</summary>
    public bool Success;
    /// <summary>The stable task-oriented projection identifier that was applied.</summary>
    public string ProjectionIdentifier = string.Empty;
    /// <summary>The exact projected source message identifier.</summary>
    public MessageIdentifier? SourceMessageIdentifier;
    /// <summary>The exact projected source message namespace URI.</summary>
    public string SourceNamespaceUri = string.Empty;
    /// <summary>The inspectable projection mapping that was applied.</summary>
    public MappingDefinition? ProjectionDefinition;
    /// <summary>The stable task-oriented projected view.</summary>
    public object? View;
    /// <summary>The metadata extracted from the source under the selected policy.</summary>
    public MessageMetadata Metadata = new MessageMetadata();
    /// <summary>The exact source paths consumed by the projection.</summary>
    public List<XmlPathData> SourcePaths = new List<XmlPathData>();
    /// <summary>The projection rules applied successfully in deterministic order.</summary>
    public List<MappingRuleDefinition> AppliedRules = new List<MappingRuleDefinition>();
    /// <summary>The source paths not represented by the task-oriented view.</summary>
    public List<XmlPathData> UnmappedSourcePaths = new List<XmlPathData>();
    /// <summary>The preserved source fragments not represented by the task-oriented view.</summary>
    public List<RawXmlFragment> UnmappedContent = new List<RawXmlFragment>();
    /// <summary>The complete source validation report when validation was requested and reached.</summary>
    public ValidationReport? SourceValidation;
    /// <summary>The ordered projection diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when projection could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when projection could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
