namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartGraphLayout"/>.</summary>
public interface IChartGraphLayout
{
    /// <summary>
    /// Adds the supplied node to this object.
    /// </summary>
    /// <param name = "node">The node.</param>
    /// <returns>The resulting graph node layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartGraphNodeLayout AddNode(ChartGraphNodeLayout node);
    /// <summary>
    /// Adds the supplied link to this object.
    /// </summary>
    /// <param name = "link">The link.</param>
    /// <returns>The resulting graph link layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartGraphLinkLayout AddLink(ChartGraphLinkLayout link);
    /// <summary>
    /// Returns the requested node.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting graph node layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartGraphNodeLayout NodeAt(int index);
    /// <summary>
    /// Returns the requested link.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting graph link layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartGraphLinkLayout LinkAt(int index);
    /// <summary>Returns the current nodes.</summary>
    /// <returns>The current field value.</returns>
    ChartGraphNodeLayout[] GetNodes();
    /// <summary>Replaces the current nodes.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetNodes(ChartGraphNodeLayout[] value);
    /// <summary>Returns the current links.</summary>
    /// <returns>The current field value.</returns>
    ChartGraphLinkLayout[] GetLinks();
    /// <summary>Replaces the current links.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLinks(ChartGraphLinkLayout[] value);
    /// <summary>Returns the current node count.</summary>
    /// <returns>The current field value.</returns>
    int GetNodeCount();
    /// <summary>Replaces the current node count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetNodeCount(int value);
    /// <summary>Returns the current link count.</summary>
    /// <returns>The current field value.</returns>
    int GetLinkCount();
    /// <summary>Replaces the current link count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLinkCount(int value);
}
