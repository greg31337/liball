namespace ttf;

/// <summary>Represents one selected lookup and its effective UTF-16 ranges.</summary>
public class FontLayoutSelectedLookup : IFontLayoutSelectedLookup
{
    SfntLayoutLookupSelection selection;
    FontLayoutTable owner;

    internal FontLayoutSelectedLookup(
        SfntLayoutLookupSelection selection,
        FontLayoutTable owner)
    {
        this.selection = selection;
        this.owner = owner;
    }

    /// <summary>Gets the lookup-list index.</summary>
    /// <returns>The lookup index.</returns>
    public int GetLookupIndex() { return selection.GetLookupIndex(); }

    /// <summary>Gets the available lookup metadata.</summary>
    /// <returns>The lookup model.</returns>
    public FontLayoutLookup GetLookup()
    {
        return owner.GetLookup(selection.GetLookupIndex());
    }

    /// <summary>Gets the effective range count.</summary>
    /// <returns>The range count.</returns>
    public int GetRangeCount() { return selection.GetRangeCount(); }

    /// <summary>Gets one effective range.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The range model.</returns>
    public FontLayoutFeatureRange GetRange(int index)
    {
        if (index < 0 || index >= selection.GetRangeCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontLayoutFeatureRange(selection.GetRange(index));
    }

    /// <summary>Gets the selected value at a UTF-16 offset.</summary>
    /// <param name="textIndex">The zero-based UTF-16 offset.</param>
    /// <returns>The selected value, or zero.</returns>
    public int GetValueAt(int textIndex)
    {
        return selection.GetValueAt(textIndex);
    }
}
