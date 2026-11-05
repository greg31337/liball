namespace chart;
/// <summary>
/// Represents a data group used by the chart model or rendering pipeline.
/// </summary>
public class ChartDataGroup : IChartDataGroup
{
    internal string Key;
    internal int[] Indices;
    internal int Count;
    /// <summary>
    /// Initializes a data group from the supplied key.
    /// </summary>
    /// <param name = "key">The key.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartDataGroup(string key)
    {
        if (key == null)
        {
            throw new System.ArgumentNullException("key");
        }

        Key = key;
        Indices = new int[4];
        Count = 0;
    }

    /// <summary>
    /// Adds the supplied index to this object.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    public void AddIndex(int index)
    {
        EnsureCapacity(Count + 1);
        Indices[Count] = index;
        Count = Count + 1;
    }

    /// <summary>
    /// Creates an independent copy of the indices.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public int[] CopyIndices()
    {
        int[] copy = new int[Count];
        System.Array.Copy(Indices, copy, Count);
        return copy;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Indices.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Indices.Length, capacity);
        int[] indices = new int[newCapacity];
        System.Array.Copy(Indices, indices, Count);
        Indices = indices;
    }

    /// <summary>Returns the current key.</summary>
    /// <returns>The current field value.</returns>
    public string GetKey()
    {
        return Key;
    }

    /// <summary>Replaces the current key.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKey(string value)
    {
        Key = value;
    }

    /// <summary>Returns the current indices.</summary>
    /// <returns>The current field value.</returns>
    public int[] GetIndices()
    {
        return Indices;
    }

    /// <summary>Replaces the current indices.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetIndices(int[] value)
    {
        Indices = value;
    }

    /// <summary>Returns the current count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCount()
    {
        return Count;
    }

    /// <summary>Replaces the current count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCount(int value)
    {
        Count = value;
    }
}
