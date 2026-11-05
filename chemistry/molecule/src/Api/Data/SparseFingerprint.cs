using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides sparse fingerprint data and operations for molecular graphs.
/// </summary>
public class SparseFingerprint
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
    /// Creates a sparse fingerprint with the default size.
    /// </summary>
    /// <returns>An empty 1024-bit sparse fingerprint.</returns>
    internal static SparseFingerprint CreateDefault()
    {
        return new SparseFingerprint(1024);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SparseFingerprint"/> class.
    /// </summary>
    /// <param name="size">The size.</param>
    public SparseFingerprint(int size)
    {
        if (size < 1)
        {
            size = 1;
        }

        Size = size;
        Indices = new List<int>();
    }

    /// <summary>
    /// Adds the index.
    /// </summary>
    /// <param name="index">The index.</param>
    internal void AddIndex(int index)
    {
        if (index < 0 || index >= Size)
        {
            return;
        }

        if (!ContainsIndex(index))
        {
            Indices.Add(index);
            SortIndices();
        }
    }

    /// <summary>
    /// Determines whether the value contains index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool ContainsIndex(int index)
    {
        int scan = 0;
        while (scan < Indices.Count)
        {
            if (Indices[scan] == index)
            {
                return true;
            }

            scan = scan + 1;
        }

        return false;
    }

    /// <summary>
    /// Counts the on bits.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int CountOnBits()
    {
        return Indices.Count;
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
    /// From bit fingerprint.
    /// </summary>
    /// <param name="fingerprint">The fingerprint.</param>
    /// <returns>The resulting sparse fingerprint.</returns>
    internal static SparseFingerprint FromBitFingerprint(BitFingerprint? fingerprint)
    {
        if (fingerprint == null)
        {
            return SparseFingerprint.CreateDefault();
        }

        SparseFingerprint sparse = new SparseFingerprint(fingerprint.Size);
        int index = 0;
        while (index < fingerprint.Size)
        {
            if (fingerprint.GetBit(index))
            {
                sparse.AddIndex(index);
            }

            index = index + 1;
        }

        return sparse;
    }

    internal void SortIndices()
    {
        int index = 1;
        while (index < Indices.Count)
        {
            int value = Indices[index];
            int scan = index - 1;
            while (scan >= 0 && Indices[scan] > value)
            {
                Indices[scan + 1] = Indices[scan];
                scan = scan - 1;
            }

            Indices[scan + 1] = value;
            index = index + 1;
        }
    }
}

