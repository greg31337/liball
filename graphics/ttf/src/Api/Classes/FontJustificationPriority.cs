namespace ttf;

/// <summary>Represents one JSTF priority level.</summary>
public class FontJustificationPriority : IFontJustificationPriority
{
    SfntJstfPriority priority;

    internal FontJustificationPriority(SfntJstfPriority priority)
    {
        this.priority = priority;
    }
    /// <summary>Gets shrinkage actions.</summary><returns>The actions.</returns>
    public FontJustificationOpportunity GetShrinkage()
    {
        return new FontJustificationOpportunity(priority.GetShrinkage());
    }
    /// <summary>Gets extension actions.</summary><returns>The actions.</returns>
    public FontJustificationOpportunity GetExtension()
    {
        return new FontJustificationOpportunity(priority.GetExtension());
    }
}
