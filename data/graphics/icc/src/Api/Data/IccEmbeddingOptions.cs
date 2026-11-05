namespace icc;

/// <summary>Controls embedded-profile discovery and carrier mutation.</summary>
public class IccEmbeddingOptions
{
    /// <summary>Gets or sets the expected or requested carrier format.</summary>
    public IccEmbeddedFormat Format;
    /// <summary>Gets or sets the zero-based profile index.</summary>
    public int ProfileIndex;
    /// <summary>Gets or sets whether compressed carrier representations are permitted.</summary>
    public bool AllowCompression;
    /// <summary>Gets or sets whether malformed unrelated carrier data is rejected.</summary>
    public bool StrictCarrierValidation;
    /// <summary>Gets or sets resource limits.</summary>
    public IccResourceLimits ResourceLimits;

    /// <summary>Initializes automatic strict carrier handling.</summary>
    public IccEmbeddingOptions()
    {
        Format = IccEmbeddedFormat.Unknown;
        ProfileIndex = 0;
        AllowCompression = true;
        StrictCarrierValidation = true;
        ResourceLimits = new IccResourceLimits();
    }
}
