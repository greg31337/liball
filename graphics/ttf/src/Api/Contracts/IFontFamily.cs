namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontFamily"/>.</summary>
public interface IFontFamily
{
    /// <summary>Gets the deterministic default typographic family name.</summary>
    /// <returns>The selected localized name, or null when the family is unnamed.</returns>
    FontNameValue? GetFamilyName();

    /// <summary>Gets the typographic family name for a preferred language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>The selected localized name, or null when the family is unnamed.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    FontNameValue? GetLocalizedFamilyName(string languageTag);

    /// <summary>Gets the deterministic default style name for one face.</summary>
    /// <param name="faceIndex">The zero-based family face index.</param>
    /// <returns>The typographic or legacy subfamily name, or null.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>faceIndex</c> is outside the family face range.
    /// </exception>
    FontNameValue? GetFaceStyleName(int faceIndex);

    /// <summary>Gets the localized style name for one face.</summary>
    /// <param name="faceIndex">The zero-based family face index.</param>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>The typographic or legacy subfamily name, or null.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>faceIndex</c> is outside the family face range.
    /// </exception>
    FontNameValue? GetLocalizedFaceStyleName(int faceIndex, string languageTag);
}
