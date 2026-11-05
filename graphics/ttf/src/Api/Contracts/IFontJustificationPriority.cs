namespace ttf;

/// <summary>Defines one JSTF priority level.</summary>
public interface IFontJustificationPriority
{
    /// <summary>Gets shrinkage actions.</summary><returns>The actions.</returns>
    FontJustificationOpportunity GetShrinkage();
    /// <summary>Gets extension actions.</summary><returns>The actions.</returns>
    FontJustificationOpportunity GetExtension();
}
