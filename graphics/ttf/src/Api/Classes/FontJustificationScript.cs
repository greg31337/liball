namespace ttf;

/// <summary>Represents one JSTF script.</summary>
public class FontJustificationScript : IFontJustificationScript
{
    SfntJstfScript script;
    internal FontJustificationScript(SfntJstfScript script)
    {
        this.script = script;
    }
    /// <summary>Gets the script tag.</summary><returns>The tag.</returns>
    public int GetTag() { return script.GetScriptTag(); }
    /// <summary>Gets the extender glyph count.</summary><returns>The count.</returns>
    public int GetExtenderGlyphCount() { return script.GetExtenderGlyphCount(); }
    /// <summary>Gets an extender glyph.</summary><param name="index">The index.</param><returns>The glyph ID.</returns>
    public int GetExtenderGlyphId(int index)
    {
        return script.GetExtenderGlyphId(index);
    }
    /// <summary>Gets the default language system.</summary><returns>The language system, or null.</returns>
    public FontJustificationLanguageSystem? GetDefaultLanguageSystem()
    {
        SfntJstfLanguageSystem? language = script.GetDefaultLanguageSystem();
        return language == null ? null :
            new FontJustificationLanguageSystem(language);
    }
    /// <summary>Gets the tagged language count.</summary><returns>The count.</returns>
    public int GetLanguageSystemCount() { return script.GetLanguageSystemCount(); }
    /// <summary>Gets a tagged language.</summary><param name="index">The index.</param><returns>The language system.</returns>
    public FontJustificationLanguageSystem GetLanguageSystem(int index)
    {
        return new FontJustificationLanguageSystem(
            script.GetLanguageSystem(index));
    }
    /// <summary>Finds a tagged language.</summary><param name="languageTag">The tag.</param><returns>The language system, or null.</returns>
    public FontJustificationLanguageSystem? FindLanguageSystem(int languageTag)
    {
        SfntJstfLanguageSystem? language = script.FindLanguageSystem(languageTag);
        return language == null ? null :
            new FontJustificationLanguageSystem(language);
    }
}
