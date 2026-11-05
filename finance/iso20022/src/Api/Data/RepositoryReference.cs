namespace iso20022;

/// <summary>Records one ordered repository relationship before or after XMI target resolution.</summary>
public class RepositoryReference
{
    /// <summary>The XMI identifier of the source entity.</summary>
    public string SourceIdentifier = string.Empty;
    /// <summary>The normalized kind of the source entity.</summary>
    public RepositoryEntityKind SourceKind;
    /// <summary>The source feature that declared the relationship.</summary>
    public string PropertyName = string.Empty;
    /// <summary>The zero-based position within a multi-valued relationship.</summary>
    public int Position;
    /// <summary>The target XMI identifier exactly as referenced.</summary>
    public string TargetIdentifier = string.Empty;
    /// <summary>The normalized kind of the target entity when known.</summary>
    public RepositoryEntityKind TargetKind;
    /// <summary>The complete source lexical value when preservation is required.</summary>
    public string RawValue = string.Empty;
    /// <summary>Whether the relationship represents containment.</summary>
    public bool IsContainment;
    /// <summary>Whether the target was resolved unambiguously.</summary>
    public bool IsResolved;
    /// <summary>A stable diagnostic code when target resolution failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable resolution diagnostic.</summary>
    public string ErrorMessage = string.Empty;
    /// <summary>The source location of the reference.</summary>
    public SourceLocation Location = new SourceLocation();
}
