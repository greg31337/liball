namespace ttf;

/// <summary>Represents an immutable snapshot of one typographic font family.</summary>
/// <remarks>
/// Family membership is derived only from names and style metadata in faces owned
/// by the containing <see cref="FontSet" />. No operating-system font registry is
/// queried. Family and face order follow the first face added to the set.
/// </remarks>
public class FontFamily : IFontFaceCollection, IFontFamily
{
    FontFace[] faces;

    internal FontFamily(FontFace[] faces)
    {
        if (faces == null)
        {
            throw new System.ArgumentNullException("faces");
        }

        if (faces.Length == 0)
        {
            throw new System.ArgumentException("A font family must contain a face.", "faces");
        }

        this.faces = faces.AsSpan().ToArray();
    }

    /// <summary>Gets the number of faces in set insertion order.</summary>
    /// <returns>The face count.</returns>
    public int GetFaceCount()
    {
        return faces.Length;
    }

    /// <summary>Gets one face in set insertion order.</summary>
    /// <param name="index">The zero-based face index.</param>
    /// <returns>The retained face.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the family face range.
    /// </exception>
    public FontFace GetFace(int index)
    {
        if (index < 0 || index >= faces.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return faces[index];
    }

    /// <summary>Gets the deterministic default typographic family name.</summary>
    /// <returns>The selected localized name, or null when the family is unnamed.</returns>
    public FontNameValue? GetFamilyName()
    {
        return FontFamilyNameResolver.SelectFamilyName(faces, null);
    }

    /// <summary>Gets the typographic family name for a preferred language.</summary>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>The selected localized name, or null when the family is unnamed.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    public FontNameValue? GetLocalizedFamilyName(string languageTag)
    {
        SfntNameSelector.ValidateLanguageTag(languageTag);
        return FontFamilyNameResolver.SelectFamilyName(faces, languageTag);
    }

    /// <summary>Gets the deterministic default style name for one face.</summary>
    /// <param name="faceIndex">The zero-based family face index.</param>
    /// <returns>The typographic or legacy subfamily name, or null.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>faceIndex</c> is outside the family face range.
    /// </exception>
    public FontNameValue? GetFaceStyleName(int faceIndex)
    {
        FontFace face = GetFace(faceIndex);
        return FontFamilyNameResolver.SelectStyleName(face, null);
    }

    /// <summary>Gets the localized style name for one face.</summary>
    /// <param name="faceIndex">The zero-based family face index.</param>
    /// <param name="languageTag">The preferred BCP 47 language tag.</param>
    /// <returns>The typographic or legacy subfamily name, or null.</returns>
    /// <exception cref="System.ArgumentNullException"><c>languageTag</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>languageTag</c> is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>faceIndex</c> is outside the family face range.
    /// </exception>
    public FontNameValue? GetLocalizedFaceStyleName(
        int faceIndex,
        string languageTag)
    {
        FontFace face = GetFace(faceIndex);
        SfntNameSelector.ValidateLanguageTag(languageTag);
        return FontFamilyNameResolver.SelectStyleName(face, languageTag);
    }

    internal bool MatchesName(string normalizedName)
    {
        int faceIndex = 0;

        while (faceIndex < faces.Length)
        {
            int nameId = FontFamilyNameResolver.GetPreferredFamilyNameId(
                faces[faceIndex]);
            string[] names = FontFamilyNameResolver.CopyNormalizedNames(
                faces[faceIndex],
                nameId);
            int nameIndex = 0;

            while (nameIndex < names.Length)
            {
                if (string.Equals(
                    names[nameIndex],
                    normalizedName,
                    System.StringComparison.Ordinal))
                {
                    return true;
                }

                nameIndex += 1;
            }

            faceIndex += 1;
        }

        return false;
    }
}
