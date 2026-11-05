namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHierarchyData"/>.</summary>
public interface IChartHierarchyData
{
    /// <summary>
    /// Returns the requested node.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting hierarchy node.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartHierarchyNode NodeAt(int index);
    /// <summary>
    /// Applies the child count operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "parentIndex">The zero-based parent index.</param>
    /// <returns>The resulting int.</returns>
    int ChildCount(int parentIndex);
    /// <summary>
    /// Returns the zero-based child index.
    /// </summary>
    /// <param name = "parentIndex">The zero-based parent index.</param>
    /// <param name = "childOrdinal">The child ordinal.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    int ChildIndex(int parentIndex, int childOrdinal);
    /// <summary>
    /// Applies the node total operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <returns>The resulting double.</returns>
    double NodeTotal(int nodeIndex);
    /// <summary>
    /// Applies the node depth operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <returns>The resulting int.</returns>
    int NodeDepth(int nodeIndex);
    /// <summary>
    /// Applies the maximum depth operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting int.</returns>
    int MaximumDepth();
    /// <summary>
    /// Validates node index and returns structured issues without rendering.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void ValidateNodeIndex(int nodeIndex);
    /// <summary>Returns the current nodes.</summary>
    /// <returns>The current field value.</returns>
    ChartHierarchyNode[] GetNodes();
    /// <summary>Replaces the current nodes.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetNodes(ChartHierarchyNode[] value);
    /// <summary>Returns the current node count.</summary>
    /// <returns>The current field value.</returns>
    int GetNodeCount();
    /// <summary>Replaces the current node count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetNodeCount(int value);
    /// <summary>Returns the current root index.</summary>
    /// <returns>The current field value.</returns>
    int GetRootIndex();
    /// <summary>Replaces the current root index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRootIndex(int value);
}
