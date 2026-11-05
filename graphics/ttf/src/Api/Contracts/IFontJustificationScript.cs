namespace ttf;

/// <summary>Defines one JSTF script.</summary>
public interface IFontJustificationScript
{
    /// <summary>Gets the script tag.</summary><returns>The tag.</returns>
    int GetTag();
    /// <summary>Gets the extender glyph count.</summary><returns>The count.</returns>
    int GetExtenderGlyphCount();
    /// <summary>Gets an extender glyph.</summary><param name="index">The index.</param><returns>The glyph ID.</returns>
    int GetExtenderGlyphId(int index);
    /// <summary>Gets the default language system.</summary><returns>The language system, or null.</returns>
    FontJustificationLanguageSystem? GetDefaultLanguageSystem();
    /// <summary>Gets the tagged language count.</summary><returns>The count.</returns>
    int GetLanguageSystemCount();
    /// <summary>Gets a tagged language.</summary><param name="index">The index.</param><returns>The language system.</returns>
    FontJustificationLanguageSystem GetLanguageSystem(int index);
    /// <summary>Finds a tagged language.</summary><param name="languageTag">The tag.</param><returns>The language system, or null.</returns>
    FontJustificationLanguageSystem? FindLanguageSystem(int languageTag);
}
