namespace chart;
/// <summary>
/// Represents a graph layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartGraphLayout : IChartGraphLayout
{
    internal ChartGraphNodeLayout[] Nodes;
    internal ChartGraphLinkLayout[] Links;
    internal int NodeCount;
    internal int LinkCount;
    /// <summary>
    /// Initializes a graph layout from the supplied node capacity and link capacity.
    /// </summary>
    /// <param name = "nodeCapacity">The node capacity.</param>
    /// <param name = "linkCapacity">The link capacity.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartGraphLayout(int nodeCapacity, int linkCapacity)
    {
        if (nodeCapacity <= 0)
        {
            throw new System.ArgumentOutOfRangeException("nodeCapacity");
        }

        if (linkCapacity < 0)
        {
            throw new System.ArgumentOutOfRangeException("linkCapacity");
        }

        Nodes = new ChartGraphNodeLayout[nodeCapacity];
        Links = new ChartGraphLinkLayout[linkCapacity];
        NodeCount = 0;
        LinkCount = 0;
    }

    /// <summary>
    /// Adds the supplied node to this object.
    /// </summary>
    /// <param name = "node">The node.</param>
    /// <returns>The resulting graph node layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartGraphNodeLayout AddNode(ChartGraphNodeLayout node)
    {
        if (node == null)
        {
            throw new System.ArgumentNullException("node");
        }

        Nodes[NodeCount] = node.Copy();
        NodeCount = NodeCount + 1;
        return Nodes[NodeCount - 1];
    }

    /// <summary>
    /// Adds the supplied link to this object.
    /// </summary>
    /// <param name = "link">The link.</param>
    /// <returns>The resulting graph link layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartGraphLinkLayout AddLink(ChartGraphLinkLayout link)
    {
        if (link == null)
        {
            throw new System.ArgumentNullException("link");
        }

        Links[LinkCount] = link.Copy();
        LinkCount = LinkCount + 1;
        return Links[LinkCount - 1];
    }

    /// <summary>
    /// Returns the requested node.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting graph node layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartGraphNodeLayout NodeAt(int index)
    {
        if (index < 0 || index >= NodeCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Nodes[index];
    }

    /// <summary>
    /// Returns the requested link.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting graph link layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartGraphLinkLayout LinkAt(int index)
    {
        if (index < 0 || index >= LinkCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Links[index];
    }

    /// <summary>Returns the current nodes.</summary>
    /// <returns>The current field value.</returns>
    public ChartGraphNodeLayout[] GetNodes()
    {
        return Nodes;
    }

    /// <summary>Replaces the current nodes.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetNodes(ChartGraphNodeLayout[] value)
    {
        Nodes = value;
    }

    /// <summary>Returns the current links.</summary>
    /// <returns>The current field value.</returns>
    public ChartGraphLinkLayout[] GetLinks()
    {
        return Links;
    }

    /// <summary>Replaces the current links.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLinks(ChartGraphLinkLayout[] value)
    {
        Links = value;
    }

    /// <summary>Returns the current node count.</summary>
    /// <returns>The current field value.</returns>
    public int GetNodeCount()
    {
        return NodeCount;
    }

    /// <summary>Replaces the current node count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetNodeCount(int value)
    {
        NodeCount = value;
    }

    /// <summary>Returns the current link count.</summary>
    /// <returns>The current field value.</returns>
    public int GetLinkCount()
    {
        return LinkCount;
    }

    /// <summary>Replaces the current link count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLinkCount(int value)
    {
        LinkCount = value;
    }
}
