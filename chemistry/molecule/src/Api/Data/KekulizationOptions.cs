namespace molecule;


/// <summary>
/// Controls aromaticity validation and fallback during Kekulé bond assignment.
/// </summary>
public class KekulizationOptions
{
    /// <summary>
    /// The aromaticity model used to validate aromatic systems.
    /// </summary>
    public AromaticityModelKind Model;
    /// <summary>
    /// Whether systems without an exact matching use a deterministic alternating fallback.
    /// </summary>
    public bool UseDeterministicFallback;

    /// <summary>
    /// Initializes Daylight-like options with deterministic fallback enabled.
    /// </summary>
    public KekulizationOptions()
    {
        Model = AromaticityModelKind.DaylightLike;
        UseDeterministicFallback = true;
    }
}
