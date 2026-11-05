namespace unicode;

/// <summary>
/// Provides immutable Unicode code-point range-set operations.
/// </summary>
public interface IUnicodeRangeSet
{
    /// <summary>
    /// Reports whether the set contains a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the code point is contained; otherwise, <c>false</c>.</returns>
    bool Contains(int codePoint);

    /// <summary>
    /// Returns the number of normalized ranges in the set.
    /// </summary>
    /// <returns>The number of ranges.</returns>
    int GetRangeCount();

    /// <summary>
    /// Returns a defensive copy of the normalized ranges.
    /// </summary>
    /// <returns>A new array containing new range objects.</returns>
    UnicodeRange[] GetRanges();

    /// <summary>
    /// Returns a normalized copy of this set.
    /// </summary>
    /// <returns>A new immutable range set.</returns>
    UnicodeRangeSet Normalize();

    /// <summary>
    /// Returns the union of this set and another set.
    /// </summary>
    /// <param name="other">The other immutable range set.</param>
    /// <returns>A new immutable range set.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>other</c> is <c>null</c>.</exception>
    UnicodeRangeSet Union(UnicodeRangeSet other);

    /// <summary>
    /// Returns the intersection of this set and another set.
    /// </summary>
    /// <param name="other">The other immutable range set.</param>
    /// <returns>A new immutable range set.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>other</c> is <c>null</c>.</exception>
    UnicodeRangeSet Intersect(UnicodeRangeSet other);

    /// <summary>
    /// Returns the code points in this set that are absent from another set.
    /// </summary>
    /// <param name="other">The set whose code points are removed.</param>
    /// <returns>A new immutable range set.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>other</c> is <c>null</c>.</exception>
    UnicodeRangeSet Difference(UnicodeRangeSet other);

    /// <summary>
    /// Returns the complement within the Unicode code-point range U+0000 through U+10FFFF.
    /// </summary>
    /// <returns>A new immutable range set.</returns>
    UnicodeRangeSet Complement();
}
