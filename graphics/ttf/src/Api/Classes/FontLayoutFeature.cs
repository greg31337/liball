namespace ttf;

/// <summary>Represents one default or alternate OpenType layout feature.</summary>
public class FontLayoutFeature : IFontLayoutFeature
{
    int index;
    int tag;
    int parameterOffset;
    int[] lookupIndices;
    FontDataSlice data;

    internal FontLayoutFeature(
        int index,
        int tag,
        int parameterOffset,
        int[] lookupIndices,
        FontDataSlice data)
    {
        this.index = index;
        this.tag = tag;
        this.parameterOffset = parameterOffset;
        this.lookupIndices = lookupIndices;
        this.data = data;
    }

    /// <summary>Gets the feature-list index.</summary>
    /// <returns>The zero-based feature index.</returns>
    public int GetIndex() { return index; }

    /// <summary>Gets the feature tag.</summary>
    /// <returns>The big-endian feature tag.</returns>
    public int GetTag() { return tag; }

    /// <summary>Reports whether feature parameters are present.</summary>
    /// <returns>True when present; otherwise false.</returns>
    public bool HasParameters() { return parameterOffset != 0; }

    /// <summary>Gets the absolute parameter offset within the layout table.</summary>
    /// <returns>The table-relative offset, or zero when absent.</returns>
    public int GetParameterOffset() { return parameterOffset; }

    /// <summary>Copies a bounded prefix of the feature-parameter data.</summary>
    /// <param name="length">The requested nonnegative byte count.</param>
    /// <returns>A new byte array.</returns>
    /// <exception cref="System.InvalidOperationException">No parameters are present.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>length</c> is negative or exceeds the remaining table data.
    /// </exception>
    public byte[] CopyParameterBytes(int length)
    {
        if (parameterOffset == 0)
        {
            throw new System.InvalidOperationException(
                "The feature does not contain a parameter table.");
        }
        if (length < 0 || parameterOffset > data.GetLength() - length)
        {
            throw new System.ArgumentOutOfRangeException("length");
        }
        return data.GetSlice(parameterOffset, length).CopyBytes();
    }

    /// <summary>Gets the referenced lookup count.</summary>
    /// <returns>The lookup-index count.</returns>
    public int GetLookupCount() { return lookupIndices.Length; }

    /// <summary>Gets one referenced lookup index.</summary>
    /// <param name="lookupIndex">The zero-based reference index.</param>
    /// <returns>The lookup-list index.</returns>
    public int GetLookupIndex(int lookupIndex)
    {
        if (lookupIndex < 0 || lookupIndex >= lookupIndices.Length)
        {
            throw new System.ArgumentOutOfRangeException("lookupIndex");
        }
        return lookupIndices[lookupIndex];
    }
}
