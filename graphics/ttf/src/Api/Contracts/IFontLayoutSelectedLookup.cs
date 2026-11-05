namespace ttf;

/// <summary>Defines one selected lookup and its effective UTF-16 ranges.</summary>
public interface IFontLayoutSelectedLookup
{
    /// <summary>Gets the lookup-list index.</summary>
    /// <returns>The lookup index.</returns>
    int GetLookupIndex();

    /// <summary>Gets the available lookup metadata.</summary>
    /// <returns>The lookup model.</returns>
    FontLayoutLookup GetLookup();

    /// <summary>Gets the effective range count.</summary>
    /// <returns>The range count.</returns>
    int GetRangeCount();

    /// <summary>Gets one effective range.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The range model.</returns>
    FontLayoutFeatureRange GetRange(int index);

    /// <summary>Gets the selected value at a UTF-16 offset.</summary>
    /// <param name="textIndex">The zero-based UTF-16 offset.</param>
    /// <returns>The selected value, or zero.</returns>
    int GetValueAt(int textIndex);
}
