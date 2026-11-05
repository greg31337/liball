namespace ttf;

/// <summary>Defines one BASE language-system extent record.</summary>
public interface IFontBaselineLanguageSystem
{
    /// <summary>Gets the language tag.</summary><returns>The big-endian tag.</returns>
    int GetLanguageTag();
    /// <summary>Gets the language-specific extents.</summary><returns>The extents.</returns>
    FontBaselineMinMax GetMinMax();
}
