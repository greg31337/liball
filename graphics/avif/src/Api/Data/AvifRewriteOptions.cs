namespace avif;

/// <summary>
/// Configures container normalization, opaque-data preservation, and rewrite validation.
/// </summary>
public class AvifRewriteOptions
{
    /// <summary>The output container representation.</summary>
    public AvifHeaderFormat HeaderFormat;

    /// <summary>Whether safe unknown non-essential boxes are preserved.</summary>
    public bool PreserveUnknownBoxes;

    /// <summary>Whether safe unknown non-essential item properties are preserved.</summary>
    public bool PreserveUnknownProperties;

    /// <summary>Whether safe unknown metadata payloads are preserved.</summary>
    public bool PreserveUnknownMetadata;

    /// <summary>Whether protected bytes and associations may be preserved opaquely when still valid.</summary>
    public bool PreserveProtectedData;

    /// <summary>Whether known structures are reordered and rewritten into canonical form.</summary>
    public bool Canonicalize;

    /// <summary>Whether rewritten output is validated before it is returned.</summary>
    public bool ValidateOutput;

    /// <summary>The resource limits applied while parsing and rewriting.</summary>
    public AvifSecurityLimits Limits;

    /// <summary>
    /// Creates preservation-first rewrite options that retain the input representation and validate output.
    /// </summary>
    public AvifRewriteOptions()
    {
        HeaderFormat = AvifHeaderFormat.PreserveExisting;
        PreserveUnknownBoxes = true;
        PreserveUnknownProperties = true;
        PreserveUnknownMetadata = true;
        PreserveProtectedData = true;
        Canonicalize = false;
        ValidateOutput = true;
        Limits = new AvifSecurityLimits();
    }
}
