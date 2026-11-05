namespace ttf;

/// <summary>Defines one JSTF shrinkage or extension opportunity.</summary>
public interface IFontJustificationOpportunity
{
    /// <summary>Reports whether these are shrinkage actions.</summary><returns>True for shrinkage.</returns>
    bool IsShrinkage();
    /// <summary>Reports whether these are extension actions.</summary><returns>True for extension.</returns>
    bool IsExtension();
    /// <summary>Reports whether any actions exist.</summary><returns>True when nonempty.</returns>
    bool HasActions();
    /// <summary>Gets substitution enable count.</summary><returns>The count.</returns>
    int GetSubstitutionEnableCount();
    /// <summary>Gets substitution enable lookup.</summary><param name="index">The index.</param><returns>The GSUB lookup index.</returns>
    int GetSubstitutionEnableLookup(int index);
    /// <summary>Gets substitution disable count.</summary><returns>The count.</returns>
    int GetSubstitutionDisableCount();
    /// <summary>Gets substitution disable lookup.</summary><param name="index">The index.</param><returns>The GSUB lookup index.</returns>
    int GetSubstitutionDisableLookup(int index);
    /// <summary>Gets positioning enable count.</summary><returns>The count.</returns>
    int GetPositioningEnableCount();
    /// <summary>Gets positioning enable lookup.</summary><param name="index">The index.</param><returns>The GPOS lookup index.</returns>
    int GetPositioningEnableLookup(int index);
    /// <summary>Gets positioning disable count.</summary><returns>The count.</returns>
    int GetPositioningDisableCount();
    /// <summary>Gets positioning disable lookup.</summary><param name="index">The index.</param><returns>The GPOS lookup index.</returns>
    int GetPositioningDisableLookup(int index);
    /// <summary>Gets maximum-adjustment lookup count.</summary><returns>The count.</returns>
    int GetMaximumLookupCount();
    /// <summary>Gets a maximum-adjustment lookup.</summary><param name="index">The index.</param><returns>The private GPOS lookup.</returns>
    FontLayoutLookup GetMaximumLookup(int index);
}
