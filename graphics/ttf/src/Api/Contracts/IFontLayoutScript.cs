namespace ttf;

/// <summary>Defines one OpenType layout script.</summary>
public interface IFontLayoutScript
{
    /// <summary>Gets the script tag.</summary>
    /// <returns>The big-endian script tag.</returns>
    int GetTag();

    /// <summary>Gets the default language system.</summary>
    /// <returns>The default language system, or null.</returns>
    FontLayoutLanguageSystem? GetDefaultLanguageSystem();

    /// <summary>Gets the tagged language-system count.</summary>
    /// <returns>The language-system count.</returns>
    int GetLanguageSystemCount();

    /// <summary>Gets one tagged language system.</summary>
    /// <param name="index">The zero-based language-system index.</param>
    /// <returns>The language-system model.</returns>
    FontLayoutLanguageSystem GetLanguageSystem(int index);

    /// <summary>Finds a tagged language system.</summary>
    /// <param name="languageTag">The big-endian language tag.</param>
    /// <returns>The language system, or null.</returns>
    FontLayoutLanguageSystem? FindLanguageSystem(int languageTag);
}
