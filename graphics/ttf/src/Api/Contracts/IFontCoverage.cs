namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontCoverage"/>.</summary>
public interface IFontCoverage
{
    /// <summary>Gets the number of disjoint coverage ranges.</summary>
    /// <returns>The range count.</returns>
    int GetRangeCount();

    /// <summary>Gets one coverage range.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the range collection.
    /// </exception>
    FontCoverageRange GetRange(int index);

    /// <summary>Gets the total number of scalar values represented by the ranges.</summary>
    /// <returns>The covered value count.</returns>
    long GetCoveredValueCount();

    /// <summary>Reports whether the selected cmap uses Windows symbol semantics.</summary>
    /// <returns>True for a symbol mapping.</returns>
    bool IsSymbolMapping();

    /// <summary>Reports whether a Unicode scalar value is covered.</summary>
    /// <param name="unicodeValue">The value to query.</param>
    /// <returns>True when the value occurs in a coverage range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>unicodeValue</c> is not a Unicode scalar.
    /// </exception>
    bool Covers(int unicodeValue);
}
