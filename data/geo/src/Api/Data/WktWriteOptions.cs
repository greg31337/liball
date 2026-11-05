namespace geo;

/// <summary>Specifies well-known text formatting behavior.</summary>
public class WktWriteOptions
{
    /// <summary>The maximum number of fractional digits written for an ordinate.</summary>
    public int DecimalPrecision = 17;

    /// <summary>Whether insignificant trailing fractional zeros are removed.</summary>
    public bool TrimTrailingZeros = true;

    /// <summary>Whether an EWKT SRID prefix is emitted.</summary>
    public bool IncludeSpatialReferenceId;

    /// <summary>Whether surface and curved extension type names may be emitted.</summary>
    public bool AllowExtendedTypes = true;
}
