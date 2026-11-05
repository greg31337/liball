using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides count fingerprint data and operations for molecular graphs.
/// </summary>
public class CountFingerprint
{
    /// <summary>
    /// The size.
    /// </summary>
    public int Size;
    /// <summary>
    /// The indices.
    /// </summary>
    public List<int> Indices;
    /// <summary>
    /// The counts.
    /// </summary>
    public List<int> Counts;

    /// <summary>
    /// Creates a count fingerprint with the default size.
    /// </summary>
    /// <returns>An empty 1024-bit count fingerprint.</returns>
    internal static CountFingerprint CreateDefault()
    {
        return new CountFingerprint(1024);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CountFingerprint"/> class.
    /// </summary>
    /// <param name="size">The size.</param>
    public CountFingerprint(int size)
    {
        if (size < 1)
        {
            size = 1;
        }

        Size = size;
        Indices = new List<int>();
        Counts = new List<int>();
    }

    /// <summary>
    /// Increment.
    /// </summary>
    /// <param name="index">The index.</param>
    internal void Increment(int index)
    {
        AddCount(index, 1);
    }

    /// <summary>
    /// Adds the count.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="count">The count.</param>
    internal void AddCount(int index, int count)
    {
        if (index < 0 || index >= Size || count <= 0)
        {
            return;
        }

        int position = FindIndexPosition(index);
        if (position >= 0)
        {
            Counts[position] = Counts[position] + count;
            return;
        }

        Indices.Add(index);
        Counts.Add(count);
        SortEntries();
    }

    /// <summary>
    /// Returns the count.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetCount(int index)
    {
        int position = FindIndexPosition(index);
        if (position >= 0)
        {
            return Counts[position];
        }

        return 0;
    }

    /// <summary>
    /// Non zero count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int NonZeroCount()
    {
        return Indices.Count;
    }

    /// <summary>
    /// Converts this value to tal count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int TotalCount()
    {
        int total = 0;
        int index = 0;
        while (index < Counts.Count)
        {
            total = total + Counts[index];
            index = index + 1;
        }

        return total;
    }

    /// <summary>
    /// Converts this value to bit fingerprint.
    /// </summary>
    /// <returns>The resulting bit fingerprint.</returns>
    internal BitFingerprint ToBitFingerprint()
    {
        BitFingerprint fingerprint = new BitFingerprint(Size);
        int index = 0;
        while (index < Indices.Count)
        {
            fingerprint.SetBit(Indices[index]);
            index = index + 1;
        }

        return fingerprint;
    }

    /// <summary>
    /// Converts this value to sparse fingerprint.
    /// </summary>
    /// <returns>The resulting sparse fingerprint.</returns>
    internal SparseFingerprint ToSparseFingerprint()
    {
        SparseFingerprint sparse = new SparseFingerprint(Size);
        int index = 0;
        while (index < Indices.Count)
        {
            sparse.AddIndex(Indices[index]);
            index = index + 1;
        }

        return sparse;
    }

    /// <summary>
    /// From bit fingerprint.
    /// </summary>
    /// <param name="fingerprint">The fingerprint.</param>
    /// <returns>The resulting count fingerprint.</returns>
    internal static CountFingerprint FromBitFingerprint(BitFingerprint? fingerprint)
    {
        if (fingerprint == null)
        {
            return CountFingerprint.CreateDefault();
        }

        CountFingerprint counts = new CountFingerprint(fingerprint.Size);
        int index = 0;
        while (index < fingerprint.Size)
        {
            if (fingerprint.GetBit(index))
            {
                counts.Increment(index);
            }

            index = index + 1;
        }

        return counts;
    }

    internal int FindIndexPosition(int index)
    {
        int scan = 0;
        while (scan < Indices.Count)
        {
            if (Indices[scan] == index)
            {
                return scan;
            }

            scan = scan + 1;
        }

        return -1;
    }

    internal void SortEntries()
    {
        int index = 1;
        while (index < Indices.Count)
        {
            int valueIndex = Indices[index];
            int valueCount = Counts[index];
            int scan = index - 1;
            while (scan >= 0 && Indices[scan] > valueIndex)
            {
                Indices[scan + 1] = Indices[scan];
                Counts[scan + 1] = Counts[scan];
                scan = scan - 1;
            }

            Indices[scan + 1] = valueIndex;
            Counts[scan + 1] = valueCount;
            index = index + 1;
        }
    }
}

