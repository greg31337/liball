namespace ttf;

/// <summary>Represents one JSTF language system.</summary>
public class FontJustificationLanguageSystem : IFontJustificationLanguageSystem
{
    SfntJstfLanguageSystem language;
    internal FontJustificationLanguageSystem(SfntJstfLanguageSystem language)
    {
        this.language = language;
    }
    /// <summary>Gets the language tag, or zero for default.</summary><returns>The tag.</returns>
    public int GetTag() { return language.GetLanguageTag(); }
    /// <summary>Gets the priority count.</summary><returns>The count.</returns>
    public int GetPriorityCount() { return language.GetPriorityCount(); }
    /// <summary>Gets a priority.</summary><param name="index">The index.</param><returns>The priority.</returns>
    public FontJustificationPriority GetPriority(int index)
    {
        return new FontJustificationPriority(language.GetPriority(index));
    }
}
