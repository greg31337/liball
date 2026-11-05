namespace ttf;

/// <summary>Represents one deterministic resolved font match.</summary>
public class FontMatch : IFontMatch
{
    FontFamily family;
    FontInstance instance;
    FontFallbackMatchKind fallbackMatchKind;
    string configuredFamilyName;

    internal FontMatch(FontFamily family, FontInstance instance)
    {
        this.family = family;
        this.instance = instance;
        fallbackMatchKind = FontFallbackMatchKind.RequestedFamily;
        configuredFamilyName = "";
    }

    /// <summary>Gets the matched typographic family snapshot.</summary>
    /// <returns>The matched family.</returns>
    public FontFamily GetFamily()
    {
        return family;
    }

    /// <summary>Gets the selected source face.</summary>
    /// <returns>The selected face.</returns>
    public FontFace GetFace()
    {
        return instance.GetFace();
    }

    /// <summary>Gets the resolved static or variable font instance.</summary>
    /// <returns>The resolved instance.</returns>
    public FontInstance GetInstance()
    {
        return instance;
    }

    /// <summary>Gets the clamped and normalized resolved coordinates.</summary>
    /// <returns>The immutable resolved coordinates.</returns>
    public NormalizedFontCoordinates GetCoordinates()
    {
        return instance.GetCoordinates();
    }

    internal void SetFallbackDetails(
        FontFallbackMatchKind kind,
        string familyName)
    {
        fallbackMatchKind = kind;
        configuredFamilyName = familyName;
    }

    /// <summary>Gets the configured source that supplied this match.</summary>
    /// <returns>The requested, generic, script, or set-wide source kind.</returns>
    public FontFallbackMatchKind GetFallbackMatchKind()
    {
        return fallbackMatchKind;
    }

    /// <summary>Gets the concrete configured family name used during fallback.</summary>
    /// <returns>The configured name, or an empty string for a direct match.</returns>
    public string GetConfiguredFamilyName()
    {
        return configuredFamilyName;
    }
}
