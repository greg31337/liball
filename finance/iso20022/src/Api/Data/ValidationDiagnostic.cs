namespace iso20022;

/// <summary>Describes one stable, located, redaction-aware validation finding with source provenance.</summary>
public class ValidationDiagnostic
{
    /// <summary>The stable diagnostic code.</summary>
    public string Code = string.Empty;
    /// <summary>The stable identifier of the rule or constraint that produced the diagnostic.</summary>
    public string RuleIdentifier = string.Empty;
    /// <summary>The effect of the diagnostic on the validation outcome.</summary>
    public ValidationSeverity Severity;
    /// <summary>The validation stage that produced the diagnostic.</summary>
    public ValidationStage Stage;
    /// <summary>The stable diagnostic category.</summary>
    public DiagnosticKind Kind;
    /// <summary>The human-readable diagnostic message.</summary>
    public string Message = string.Empty;
    /// <summary>The qualified path of the affected message content.</summary>
    public XmlPathData Path = new XmlPathData();
    /// <summary>The source line, column, or offset of the affected content when known.</summary>
    public SourceLocation Location = new SourceLocation();
    /// <summary>The offending lexical or logical value when reporting it is permitted.</summary>
    public string OffendingValue = string.Empty;
    /// <summary>Whether the offending value was intentionally omitted by redaction policy.</summary>
    public bool OffendingValueRedacted;
    /// <summary>The expected constraint, value, cardinality, or relationship.</summary>
    public string ExpectedConstraint = string.Empty;
    /// <summary>The exact repository, schema, code-set, or other artifact that supplied the rule when known.</summary>
    public ArtifactManifest? SourceArtifact;
    /// <summary>The repository concept or source identifier that supplied the rule when known.</summary>
    public string SourceIdentifier = string.Empty;
    /// <summary>The profile identifier that supplied or modified the rule when known.</summary>
    public string ProfileIdentifier = string.Empty;
    /// <summary>The exact profile version that supplied or modified the rule when known.</summary>
    public string ProfileVersion = string.Empty;
    /// <summary>A human-readable repair hint when a deterministic repair is known.</summary>
    public string RepairHint = string.Empty;
}
