namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartDataGroup"/>.</summary>
public interface IChartDataGroup
{
    /// <summary>
    /// Adds the supplied index to this object.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    void AddIndex(int index);
    /// <summary>
    /// Creates an independent copy of the indices.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    int[] CopyIndices();
    /// <summary>Returns the current key.</summary>
    /// <returns>The current field value.</returns>
    string GetKey();
    /// <summary>Replaces the current key.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKey(string value);
    /// <summary>Returns the current indices.</summary>
    /// <returns>The current field value.</returns>
    int[] GetIndices();
    /// <summary>Replaces the current indices.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetIndices(int[] value);
    /// <summary>Returns the current count.</summary>
    /// <returns>The current field value.</returns>
    int GetCount();
    /// <summary>Replaces the current count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCount(int value);
}
