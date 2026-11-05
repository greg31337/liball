namespace molecule;


/// <summary>
/// Provides bit fingerprint data and operations for molecular graphs.
/// </summary>
public class BitFingerprint
{
    /// <summary>
    /// The size.
    /// </summary>
    public int Size;
    /// <summary>
    /// The bits.
    /// </summary>
    public bool[] Bits;

    /// <summary>
    /// Creates a bit fingerprint with the default size.
    /// </summary>
    /// <returns>An empty 1024-bit fingerprint.</returns>
    internal static BitFingerprint CreateDefault()
    {
        return new BitFingerprint(1024);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitFingerprint"/> class.
    /// </summary>
    /// <param name="size">The size.</param>
    public BitFingerprint(int size)
    {
        if (size < 1)
        {
            size = 1;
        }

        Size = size;
        Bits = new bool[Size];
    }

    /// <summary>
    /// Sets the bit.
    /// </summary>
    /// <param name="index">The index.</param>
    internal void SetBit(int index)
    {
        if (index < 0 || index >= Size)
        {
            return;
        }

        Bits[index] = true;
    }

    /// <summary>
    /// Returns the bit.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool GetBit(int index)
    {
        if (index < 0 || index >= Size)
        {
            return false;
        }

        return Bits[index];
    }

    /// <summary>
    /// Counts the on bits.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int CountOnBits()
    {
        int count = 0;
        int index = 0;
        while (index < Size)
        {
            if (Bits[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Resets this object to its empty state.
    /// </summary>
    internal void Clear()
    {
        int index = 0;
        while (index < Size)
        {
            Bits[index] = false;
            index = index + 1;
        }
    }

    /// <summary>
    /// Converts this value to bit string.
    /// </summary>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string ToBitString()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < Size)
        {
            if (Bits[index])
            {
                builder.Append("1");
            }
            else
            {
                builder.Append("0");
            }

            index = index + 1;
        }

        return builder.ToString();
    }
}

