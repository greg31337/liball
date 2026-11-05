namespace iso20022;

/// <summary>Contains the exception-less outcome and provenance of one code lookup.</summary>
public class CodeLookupResult
{
    /// <summary>Whether the lookup request was valid and evaluated successfully.</summary>
    public bool Success;
    /// <summary>Whether the requested code value was found under the selected policy.</summary>
    public bool Found;
    /// <summary>The resolved internal code set when <see cref="CodeLookupRequest.Kind"/> is internal.</summary>
    public CodeSetDefinition? InternalCodeSet;
    /// <summary>The resolved internal code value when <see cref="CodeLookupRequest.Kind"/> is internal.</summary>
    public CodeValueDefinition? InternalCodeValue;
    /// <summary>The resolved external code set when <see cref="CodeLookupRequest.Kind"/> is external.</summary>
    public ExternalCodeSet? ExternalCodeSet;
    /// <summary>The resolved external code value when <see cref="CodeLookupRequest.Kind"/> is external.</summary>
    public ExternalCodeValue? ExternalCodeValue;
    /// <summary>The artifact manifest proving the selected code-set edition.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The selected artifact edition.</summary>
    public string Edition = string.Empty;
    /// <summary>A stable error code when the lookup request was not evaluated successfully.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when the lookup request was not evaluated successfully.</summary>
    public string ErrorMessage = string.Empty;
}
