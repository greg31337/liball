namespace ttf;

/// <summary>Defines one JSTF language system.</summary>
public interface IFontJustificationLanguageSystem
{
    /// <summary>Gets the language tag, or zero for default.</summary><returns>The tag.</returns>
    int GetTag();
    /// <summary>Gets the priority count.</summary><returns>The count.</returns>
    int GetPriorityCount();
    /// <summary>Gets a priority.</summary><param name="index">The index.</param><returns>The priority.</returns>
    FontJustificationPriority GetPriority(int index);
}
