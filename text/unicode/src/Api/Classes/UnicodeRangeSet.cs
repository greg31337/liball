namespace unicode;

/// <summary>
/// Stores an immutable normalized set of Unicode code-point ranges.
/// </summary>
public class UnicodeRangeSet : IUnicodeRangeSet
{
    internal UnicodeRange[] Ranges;

    /// <summary>
    /// Initializes a set by sorting, merging, and defensively copying the supplied ranges.
    /// </summary>
    /// <param name="ranges">The ranges to normalize.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>ranges</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an array element is <c>null</c>.</exception>
    public UnicodeRangeSet(UnicodeRange[] ranges)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);

        Ranges = NormalizeRangesCopy(ranges);
    }

    /// <summary>
    /// Reports whether the set contains a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the code point is contained; otherwise, <c>false</c>.</returns>
    public bool Contains(int codePoint)
    {
        return ContainsRanges(Ranges, codePoint);
    }

    /// <summary>
    /// Returns the number of normalized ranges in the set.
    /// </summary>
    /// <returns>The number of ranges.</returns>
    public int GetRangeCount()
    {
        return Ranges.Length;
    }

    /// <summary>
    /// Returns a defensive copy of the normalized ranges.
    /// </summary>
    /// <returns>A new array containing new range objects.</returns>
    public UnicodeRange[] GetRanges()
    {
        return CopyRanges(Ranges);
    }

    /// <summary>
    /// Returns a normalized copy of this set.
    /// </summary>
    /// <returns>A new immutable range set.</returns>
    public UnicodeRangeSet Normalize()
    {
        return new UnicodeRangeSet(Ranges);
    }

    /// <summary>
    /// Returns the union of this set and another set.
    /// </summary>
    /// <param name="other">The other immutable range set.</param>
    /// <returns>A new immutable range set.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>other</c> is <c>null</c>.</exception>
    public UnicodeRangeSet Union(UnicodeRangeSet other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        UnicodeRange[] combined = new UnicodeRange[Ranges.Length + other.Ranges.Length];
        int index = 0;
        while (index < Ranges.Length)
        {
            combined[index] = Ranges[index];
            index = index + 1;
        }

        int otherIndex = 0;
        while (otherIndex < other.Ranges.Length)
        {
            combined[index] = other.Ranges[otherIndex];
            index = index + 1;
            otherIndex = otherIndex + 1;
        }

        return new UnicodeRangeSet(combined);
    }

    /// <summary>
    /// Returns the intersection of this set and another set.
    /// </summary>
    /// <param name="other">The other immutable range set.</param>
    /// <returns>A new immutable range set.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>other</c> is <c>null</c>.</exception>
    public UnicodeRangeSet Intersect(UnicodeRangeSet other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        System.Collections.Generic.List<UnicodeRange> result = new System.Collections.Generic.List<UnicodeRange>();
        int leftIndex = 0;
        int rightIndex = 0;
        while (leftIndex < Ranges.Length && rightIndex < other.Ranges.Length)
        {
            UnicodeRange left = Ranges[leftIndex];
            UnicodeRange right = other.Ranges[rightIndex];
            int start = left.Start > right.Start ? left.Start : right.Start;
            int end = left.End < right.End ? left.End : right.End;
            if (start <= end)
            {
                result.Add(new UnicodeRange(start, end));
            }

            if (left.End < right.End)
            {
                leftIndex = leftIndex + 1;
            }
            else
            {
                rightIndex = rightIndex + 1;
            }
        }

        return new UnicodeRangeSet(result.ToArray());
    }

    /// <summary>
    /// Returns the code points in this set that are absent from another set.
    /// </summary>
    /// <param name="other">The set whose code points are removed.</param>
    /// <returns>A new immutable range set.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>other</c> is <c>null</c>.</exception>
    public UnicodeRangeSet Difference(UnicodeRangeSet other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        System.Collections.Generic.List<UnicodeRange> result = new System.Collections.Generic.List<UnicodeRange>();
        int leftIndex = 0;
        int rightIndex = 0;
        while (leftIndex < Ranges.Length)
        {
            UnicodeRange left = Ranges[leftIndex];
            int cursor = left.Start;
            while (rightIndex < other.Ranges.Length && other.Ranges[rightIndex].End < cursor)
            {
                rightIndex = rightIndex + 1;
            }

            int scanIndex = rightIndex;
            while (scanIndex < other.Ranges.Length && other.Ranges[scanIndex].Start <= left.End)
            {
                UnicodeRange removed = other.Ranges[scanIndex];
                if (removed.Start > cursor)
                {
                    int end = removed.Start - 1;
                    if (end > left.End)
                    {
                        end = left.End;
                    }

                    result.Add(new UnicodeRange(cursor, end));
                }

                if (removed.End >= left.End)
                {
                    cursor = left.End + 1;
                    break;
                }

                if (removed.End >= cursor)
                {
                    cursor = removed.End + 1;
                }

                scanIndex = scanIndex + 1;
            }

            if (cursor <= left.End)
            {
                result.Add(new UnicodeRange(cursor, left.End));
            }

            leftIndex = leftIndex + 1;
        }

        return new UnicodeRangeSet(result.ToArray());
    }

    /// <summary>
    /// Returns the complement within the Unicode code-point range U+0000 through U+10FFFF.
    /// </summary>
    /// <returns>A new immutable range set.</returns>
    public UnicodeRangeSet Complement()
    {
        System.Collections.Generic.List<UnicodeRange> result = new System.Collections.Generic.List<UnicodeRange>();
        int cursor = 0;
        int index = 0;
        while (index < Ranges.Length)
        {
            if (cursor < Ranges[index].Start)
            {
                result.Add(new UnicodeRange(cursor, Ranges[index].Start - 1));
            }

            cursor = Ranges[index].End + 1;
            index = index + 1;
        }

        if (cursor <= 0x10FFFF)
        {
            result.Add(new UnicodeRange(cursor, 0x10FFFF));
        }

        return new UnicodeRangeSet(result.ToArray());
    }

    internal static bool ContainsRanges(UnicodeRange[] ranges, int codePoint)
    {
        return FindRangeIndexInRanges(ranges, codePoint) >= 0;
    }

    internal static int FindRangeIndexInRanges(UnicodeRange[] ranges, int codePoint)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);

        int low = 0;
        int high = ranges.Length - 1;
        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            UnicodeRange range = ranges[mid];
            if (codePoint < range.Start)
            {
                high = mid - 1;
                continue;
            }

            if (codePoint > range.End)
            {
                low = mid + 1;
                continue;
            }

            return mid;
        }

        return -1;
    }

    internal static bool AreRangesSortedAndNonOverlapping(UnicodeRange[] ranges)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);

        int index = 1;
        while (index < ranges.Length)
        {
            if (ranges[index].Start <= ranges[index - 1].End)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static UnicodeRange[] NormalizeRangesCopy(UnicodeRange[] ranges)
    {
        UnicodeRange[] sorted = CopyRanges(ranges);
        int index = 1;
        while (index < sorted.Length)
        {
            UnicodeRange value = sorted[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 &&
                (sorted[insertionIndex].Start > value.Start ||
                (sorted[insertionIndex].Start == value.Start && sorted[insertionIndex].End > value.End)))
            {
                sorted[insertionIndex + 1] = sorted[insertionIndex];
                insertionIndex = insertionIndex - 1;
            }

            sorted[insertionIndex + 1] = value;
            index = index + 1;
        }

        System.Collections.Generic.List<UnicodeRange> normalized = new System.Collections.Generic.List<UnicodeRange>();
        index = 0;
        while (index < sorted.Length)
        {
            UnicodeRange current = sorted[index];
            if (normalized.Count == 0)
            {
                normalized.Add(current);
            }
            else
            {
                UnicodeRange previous = normalized[normalized.Count - 1];
                if (current.Start <= previous.End + 1)
                {
                    if (current.End > previous.End)
                    {
                        previous.End = current.End;
                    }
                }
                else
                {
                    normalized.Add(current);
                }
            }

            index = index + 1;
        }

        return normalized.ToArray();
    }

    internal static UnicodeRange[] CopyRanges(UnicodeRange[] ranges)
    {
        UnicodeRange[] copy = new UnicodeRange[ranges.Length];
        int index = 0;
        while (index < ranges.Length)
        {
            if (ranges[index] == null)
            {
                throw new System.ArgumentException("Range arrays cannot contain null elements.");
            }

            copy[index] = new UnicodeRange(ranges[index].Start, ranges[index].End);
            index = index + 1;
        }

        return copy;
    }
}
