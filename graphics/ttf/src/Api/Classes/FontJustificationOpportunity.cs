namespace ttf;

/// <summary>Represents one JSTF shrinkage or extension opportunity.</summary>
public class FontJustificationOpportunity : IFontJustificationOpportunity
{
    SfntJstfOpportunity opportunity;

    internal FontJustificationOpportunity(SfntJstfOpportunity opportunity)
    {
        this.opportunity = opportunity;
    }

    /// <summary>Reports whether these are shrinkage actions.</summary><returns>True for shrinkage.</returns>
    public bool IsShrinkage() { return opportunity.IsShrinkage(); }
    /// <summary>Reports whether these are extension actions.</summary><returns>True for extension.</returns>
    public bool IsExtension() { return opportunity.IsExtension(); }
    /// <summary>Reports whether any actions exist.</summary><returns>True when nonempty.</returns>
    public bool HasActions() { return opportunity.HasActions(); }
    /// <summary>Gets substitution enable count.</summary><returns>The count.</returns>
    public int GetSubstitutionEnableCount()
    {
        return GetCount(opportunity.GetSubstitutionEnableList());
    }
    /// <summary>Gets substitution enable lookup.</summary><param name="index">The index.</param><returns>The GSUB lookup index.</returns>
    public int GetSubstitutionEnableLookup(int index)
    {
        return GetLookup(opportunity.GetSubstitutionEnableList(), index);
    }
    /// <summary>Gets substitution disable count.</summary><returns>The count.</returns>
    public int GetSubstitutionDisableCount()
    {
        return GetCount(opportunity.GetSubstitutionDisableList());
    }
    /// <summary>Gets substitution disable lookup.</summary><param name="index">The index.</param><returns>The GSUB lookup index.</returns>
    public int GetSubstitutionDisableLookup(int index)
    {
        return GetLookup(opportunity.GetSubstitutionDisableList(), index);
    }
    /// <summary>Gets positioning enable count.</summary><returns>The count.</returns>
    public int GetPositioningEnableCount()
    {
        return GetCount(opportunity.GetPositioningEnableList());
    }
    /// <summary>Gets positioning enable lookup.</summary><param name="index">The index.</param><returns>The GPOS lookup index.</returns>
    public int GetPositioningEnableLookup(int index)
    {
        return GetLookup(opportunity.GetPositioningEnableList(), index);
    }
    /// <summary>Gets positioning disable count.</summary><returns>The count.</returns>
    public int GetPositioningDisableCount()
    {
        return GetCount(opportunity.GetPositioningDisableList());
    }
    /// <summary>Gets positioning disable lookup.</summary><param name="index">The index.</param><returns>The GPOS lookup index.</returns>
    public int GetPositioningDisableLookup(int index)
    {
        return GetLookup(opportunity.GetPositioningDisableList(), index);
    }
    /// <summary>Gets maximum-adjustment lookup count.</summary><returns>The count.</returns>
    public int GetMaximumLookupCount()
    {
        SfntJstfMaximum? maximum = opportunity.GetMaximum();
        return maximum == null ? 0 : maximum.GetLookupCount();
    }
    /// <summary>Gets a maximum-adjustment lookup.</summary><param name="index">The index.</param><returns>The private GPOS lookup.</returns>
    public FontLayoutLookup GetMaximumLookup(int index)
    {
        SfntJstfMaximum? maximum = opportunity.GetMaximum();
        if (maximum == null || index < 0 || index >= maximum.GetLookupCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        SfntGposLookup lookup = maximum.GetLookup(index);
        return new FontLayoutLookup(
            index,
            lookup.GetLayoutLookup(),
            lookup.GetLookupType());
    }

    int GetCount(SfntJstfLookupList? list)
    {
        return list == null ? 0 : list.GetLookupCount();
    }

    int GetLookup(SfntJstfLookupList? list, int index)
    {
        if (list == null)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return list.GetLookupIndex(index);
    }
}
