namespace uri;

/// <summary>
/// Defines options shared by IRI conversion operations.
/// </summary>
public class IriConversionOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The requested Unicode normalization behavior.</summary>
    public UnicodeNormalizationMode NormalizationMode;

    /// <summary>The requested DNS-oriented registered-name mapping behavior.</summary>
    public IriIdnMode IdnMode;

    /// <summary>Initializes IRI conversion options with default processing limits.</summary>
    public IriConversionOptions()
    {
        Limits = new OperationLimits();
        NormalizationMode = UnicodeNormalizationMode.Preserve;
        IdnMode = IriIdnMode.None;
    }
}
