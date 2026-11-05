namespace ttf;

/// <summary>Defines name and Unicode identity inspection for one glyph.</summary>
public interface IFontGlyphIdentity
{
    /// <summary>Gets the glyph identifier.</summary>
    /// <returns>The zero-based glyph identifier.</returns>
    int GetGlyphId();

    /// <summary>Gets the parsed or stable synthetic glyph name.</summary>
    /// <returns>The glyph name.</returns>
    string GetName();

    /// <summary>Gets the glyph-name provenance.</summary>
    /// <returns>The name source.</returns>
    FontGlyphNameSource GetNameSource();

    /// <summary>Reports whether the public name was synthesized.</summary>
    /// <returns>True for a stable <c>gidNNNN</c> fallback.</returns>
    bool IsSyntheticName();

    /// <summary>Gets the supported <c>post</c> format used for inspection.</summary>
    /// <returns>The format, or unavailable.</returns>
    FontPostTableFormat GetPostTableFormat();

    /// <summary>Reports whether a numeric <c>post</c> name value is available.</summary>
    /// <returns>True when a name index, offset result, or character code exists.</returns>
    bool HasPostNameIndex();

    /// <summary>Gets the numeric <c>post</c> name value.</summary>
    /// <returns>The name index, offset result, or character code.</returns>
    /// <exception cref="System.InvalidOperationException">No numeric value is available.</exception>
    int GetPostNameIndex();

    /// <summary>Reports whether the numeric <c>post</c> name value is valid.</summary>
    /// <returns>True when the name value is structurally valid.</returns>
    bool HasValidPostNameIndex();

    /// <summary>Reports whether <c>post</c> supplied a valid usable name.</summary>
    /// <returns>True for a valid provided name.</returns>
    bool HasValidPostName();

    /// <summary>Gets the number of nominal and variation Unicode sequences for this glyph.</summary>
    /// <returns>The sequence count.</returns>
    int GetUnicodeSequenceCount();

    /// <summary>Gets one Unicode sequence with cmap provenance.</summary>
    /// <param name="index">The zero-based sequence index.</param>
    /// <returns>The resolved mapping that represents the sequence.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    FontCharacterMapping GetUnicodeSequence(int index);
}
