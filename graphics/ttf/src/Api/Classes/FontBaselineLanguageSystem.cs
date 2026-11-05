namespace ttf;

/// <summary>Represents one BASE language-system extent record.</summary>
public class FontBaselineLanguageSystem : IFontBaselineLanguageSystem
{
    SfntBaseLanguageSystem language;
    internal FontBaselineLanguageSystem(SfntBaseLanguageSystem language)
    {
        this.language = language;
    }
    /// <summary>Gets the language tag.</summary><returns>The big-endian tag.</returns>
    public int GetLanguageTag() { return language.GetLanguageTag(); }
    /// <summary>Gets the language-specific extents.</summary><returns>The extents.</returns>
    public FontBaselineMinMax GetMinMax()
    {
        return new FontBaselineMinMax(language.GetMinMax());
    }
}
