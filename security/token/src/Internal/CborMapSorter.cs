using System;
namespace token;

/// <summary>
/// Sorts CBOR map entries by deterministic encoded key order.
/// </summary>
internal static class CborMapSorter
{
    /// <summary>
    /// Returns entries ordered first by encoded key length and then by encoded key bytes, with a maximum depth of 64.
    /// </summary>
    /// <param name="entries">The map entries to sort without modifying the input array.</param>
    /// <returns>A result containing the sorted entries, or failure details for an invalid key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>entries</c> is null.</exception>
    internal static CborMapSortResult Sort(CborMapEntry[] entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        return Sort(entries, 64);
    }

    /// <summary>
    /// Returns entries in deterministic encoded key order within a nesting depth limit.
    /// </summary>
    /// <param name="entries">The map entries to sort without modifying the input array.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>A result containing the sorted entries, or failure details for an invalid or overly deep key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>entries</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static CborMapSortResult Sort(CborMapEntry[] entries, int maximumDepth)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        return SortCore(entries, int.MaxValue, maximumDepth, 1);
    }

    internal static CborMapSortResult SortCore(
        CborMapEntry[] entries,
        int maximumLength,
        int maximumDepth,
        int depth)
    {
        CborEncodedMapEntry[] sortable = new CborEncodedMapEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            CborMapEntry entry = entries[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                return Failure("A CBOR map entry must contain a key and value.", index);
            }

            TokenBytesResult encoded = CborWriter.EncodeDeterministicItem(
                entry.Key,
                maximumLength,
                maximumDepth,
                depth);
            if (!encoded.Success)
            {
                CborMapSortResult failure = new CborMapSortResult();
                failure.Error = encoded.Error;
                return failure;
            }

            CborEncodedMapEntry sortableEntry = new CborEncodedMapEntry();
            sortableEntry.Entry = entry;
            sortableEntry.EncodedKey = encoded.Bytes;
            sortableEntry.OriginalIndex = index;
            sortable[index] = sortableEntry;
            index += 1;
        }

        index = 1;
        while (index < sortable.Length)
        {
            CborEncodedMapEntry current = sortable[index];
            int insertion = index;
            while (insertion > 0 && Compare(sortable[insertion - 1], current) > 0)
            {
                sortable[insertion] = sortable[insertion - 1];
                insertion -= 1;
            }

            sortable[insertion] = current;
            index += 1;
        }

        CborMapEntry[] sorted = new CborMapEntry[entries.Length];
        index = 0;
        while (index < sortable.Length)
        {
            sorted[index] = (sortable[index].Entry ?? throw new InvalidOperationException("A required value is null."));
            index += 1;
        }

        CborMapSortResult result = new CborMapSortResult();
        result.Entries = sorted;
        result.Success = true;
        return result;
    }

    static int Compare(CborEncodedMapEntry left, CborEncodedMapEntry right)
    {
        if ((left.EncodedKey ?? throw new InvalidOperationException("A required value is null.")).Length < (right.EncodedKey ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            return -1;
        }

        if (left.EncodedKey.Length > right.EncodedKey.Length)
        {
            return 1;
        }

        int index = 0;
        while (index < left.EncodedKey.Length)
        {
            if (left.EncodedKey[index] < right.EncodedKey[index])
            {
                return -1;
            }

            if (left.EncodedKey[index] > right.EncodedKey[index])
            {
                return 1;
            }

            index += 1;
        }

        if (left.OriginalIndex < right.OriginalIndex)
        {
            return -1;
        }

        if (left.OriginalIndex > right.OriginalIndex)
        {
            return 1;
        }

        return 0;
    }

    static CborMapSortResult Failure(string message, int position)
    {
        CborMapSortResult result = new CborMapSortResult();
        result.Error = new TokenError("invalid_cbor_map", message, position);
        return result;
    }
}
