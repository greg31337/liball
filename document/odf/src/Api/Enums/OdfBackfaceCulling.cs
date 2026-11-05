namespace odf;

/// <summary>
/// Identifies whether back-facing surfaces are culled.
/// </summary>
public enum OdfBackfaceCulling
{
    /// <summary>The backface-culling value is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>Backface culling is enabled.</summary>
    Enabled = 1,
    /// <summary>Backface culling is disabled.</summary>
    Disabled = 2
}
