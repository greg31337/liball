namespace fake;

/// <summary>
/// Contains the parsed and canonicalized components of a BCP 47 language tag.
/// </summary>
public class LanguageTag
{
    /// <summary>
    /// The caller-supplied tag before canonicalization.
    /// </summary>
    public string Original = string.Empty;

    /// <summary>
    /// The canonical tag using registry aliases and conventional casing.
    /// </summary>
    public string Normalized = string.Empty;

    /// <summary>
    /// The canonical primary language subtag, or null for an atomic grandfathered or private-use-only tag.
    /// </summary>
    public string? Language;

    /// <summary>
    /// The extended-language subtags present in the original tag.
    /// </summary>
    public string[] Extlangs = new string[0];

    /// <summary>
    /// The canonical script subtag, or null when absent.
    /// </summary>
    public string? Script;

    /// <summary>
    /// The canonical region subtag, or null when absent.
    /// </summary>
    public string? Region;

    /// <summary>
    /// The canonical variant subtags in input order.
    /// </summary>
    public string[] Variants = new string[0];

    /// <summary>
    /// The lowercase extension sequences ordered by singleton.
    /// </summary>
    public string[] Extensions = new string[0];

    /// <summary>
    /// The lowercase private-use subtags following <c>x</c>.
    /// </summary>
    public string[] PrivateUse = new string[0];

    /// <summary>
    /// Whether the original input was a registered grandfathered tag.
    /// </summary>
    public bool Grandfathered;

    /// <summary>
    /// Whether registry preferred-value mappings changed the tag or one of its subtags.
    /// </summary>
    public bool Aliased;

    internal LanguageTag()
    {
        Original = string.Empty;
        Normalized = string.Empty;
        Language = null;
        Extlangs = new string[0];
        Script = null;
        Region = null;
        Variants = new string[0];
        Extensions = new string[0];
        PrivateUse = new string[0];
        Grandfathered = false;
        Aliased = false;
    }
}
