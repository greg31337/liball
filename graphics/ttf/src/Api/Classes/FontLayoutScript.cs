namespace ttf;

/// <summary>Represents one OpenType layout script.</summary>
public class FontLayoutScript : IFontLayoutScript
{
    SfntLayoutScript script;
    FontLayoutTable owner;

    internal FontLayoutScript(SfntLayoutScript script, FontLayoutTable owner)
    {
        this.script = script;
        this.owner = owner;
    }

    /// <summary>Gets the script tag.</summary>
    /// <returns>The big-endian script tag.</returns>
    public int GetTag() { return script.GetScriptTag(); }

    /// <summary>Gets the default language system.</summary>
    /// <returns>The default language system, or null.</returns>
    public FontLayoutLanguageSystem? GetDefaultLanguageSystem()
    {
        SfntLayoutLanguageSystem? language = script.GetDefaultLanguageSystem();
        return language == null ? null :
            new FontLayoutLanguageSystem(language, owner);
    }

    /// <summary>Gets the tagged language-system count.</summary>
    /// <returns>The language-system count.</returns>
    public int GetLanguageSystemCount()
    {
        return script.GetLanguageSystemCount();
    }

    /// <summary>Gets one tagged language system.</summary>
    /// <param name="index">The zero-based language-system index.</param>
    /// <returns>The language-system model.</returns>
    public FontLayoutLanguageSystem GetLanguageSystem(int index)
    {
        if (index < 0 || index >= script.GetLanguageSystemCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontLayoutLanguageSystem(
            script.GetLanguageSystem(index),
            owner);
    }

    /// <summary>Finds a tagged language system.</summary>
    /// <param name="languageTag">The big-endian language tag.</param>
    /// <returns>The language system, or null.</returns>
    public FontLayoutLanguageSystem? FindLanguageSystem(int languageTag)
    {
        SfntLayoutLanguageSystem? language =
            script.FindLanguageSystem(languageTag);
        return language == null ? null :
            new FontLayoutLanguageSystem(language, owner);
    }
}
