namespace molecule;


/// <summary>
/// Controls aromaticity perception and dekekulization behavior.
/// </summary>
public class AromaticityOptions
{
    /// <summary>
    /// The aromaticity model to apply.
    /// </summary>
    public AromaticityModelKind Model;
    /// <summary>
    /// Whether rejected conjugated systems use a deterministic nonaromatic fallback instead of failing.
    /// </summary>
    public bool UseDeterministicFallback;

    /// <summary>
    /// Initializes Daylight-like options with deterministic fallback enabled.
    /// </summary>
    public AromaticityOptions()
    {
        Model = AromaticityModelKind.DaylightLike;
        UseDeterministicFallback = true;
    }
}
