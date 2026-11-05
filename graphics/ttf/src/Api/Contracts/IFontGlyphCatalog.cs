namespace ttf;

/// <summary>Defines safe glyph identity lookup operations for one face.</summary>
public interface IFontGlyphCatalog
{
    /// <summary>Gets the number of glyph identities.</summary>
    /// <returns>The glyph count from <c>maxp</c>.</returns>
    int GetGlyphCount();

    /// <summary>Gets the supported <c>post</c> format.</summary>
    /// <returns>The format, or unavailable.</returns>
    FontPostTableFormat GetPostTableFormat();

    /// <summary>Gets one glyph identity by identifier.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The stable identity.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is invalid.</exception>
    FontGlyphIdentity GetGlyph(int glyphId);

    /// <summary>Finds every glyph whose name exactly matches an ordinal string.</summary>
    /// <param name="name">The glyph name to find.</param>
    /// <returns>All matches in ascending glyph-identifier order.</returns>
    /// <exception cref="System.ArgumentNullException"><c>name</c> is null.</exception>
    FontGlyphIdentity[] FindGlyphsByName(string name);
}
