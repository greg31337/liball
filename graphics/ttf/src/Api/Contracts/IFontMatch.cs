namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontMatch"/>.</summary>
public interface IFontMatch
{
    /// <summary>Gets the matched typographic family snapshot.</summary>
    /// <returns>The matched family.</returns>
    FontFamily GetFamily();

    /// <summary>Gets the selected source face.</summary>
    /// <returns>The selected face.</returns>
    FontFace GetFace();

    /// <summary>Gets the resolved static or variable font instance.</summary>
    /// <returns>The resolved instance.</returns>
    FontInstance GetInstance();

    /// <summary>Gets the clamped and normalized resolved coordinates.</summary>
    /// <returns>The immutable resolved coordinates.</returns>
    NormalizedFontCoordinates GetCoordinates();

    /// <summary>Gets the configured source that supplied this match.</summary>
    /// <returns>The requested, generic, script, or set-wide source kind.</returns>
    FontFallbackMatchKind GetFallbackMatchKind();

    /// <summary>Gets the concrete configured family name used during fallback.</summary>
    /// <returns>The configured name, or an empty string for a direct match.</returns>
    string GetConfiguredFamilyName();
}
