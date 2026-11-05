namespace chart;
/// <summary>
/// Represents a hierarchy data used by the chart model or rendering pipeline.
/// </summary>
public class ChartHierarchyData : IChartHierarchyData
{
    internal ChartHierarchyNode[] Nodes;
    internal int NodeCount;
    internal int RootIndex;
    /// <summary>
    /// Initializes a hierarchy data from the supplied nodes.
    /// </summary>
    /// <param name = "nodes">The nodes.</param>
    public ChartHierarchyData(ChartHierarchyNode[] nodes)
    {
        ValidateNodes(nodes);
        Nodes = CopyNodes(nodes);
        NodeCount = nodes.Length;
        RootIndex = FindRoot(nodes);
    }

    /// <summary>
    /// Returns the requested node.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting hierarchy node.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartHierarchyNode NodeAt(int index)
    {
        if (index < 0 || index >= NodeCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Nodes[index];
    }

    /// <summary>
    /// Applies the child count operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "parentIndex">The zero-based parent index.</param>
    /// <returns>The resulting int.</returns>
    public int ChildCount(int parentIndex)
    {
        ValidateNodeIndex(parentIndex);
        int count = 0;
        int index = 0;
        while (index < NodeCount)
        {
            if (Nodes[index].ParentIndex == parentIndex)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Returns the zero-based child index.
    /// </summary>
    /// <param name = "parentIndex">The zero-based parent index.</param>
    /// <param name = "childOrdinal">The child ordinal.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public int ChildIndex(int parentIndex, int childOrdinal)
    {
        ValidateNodeIndex(parentIndex);
        if (childOrdinal < 0)
        {
            throw new System.ArgumentOutOfRangeException("childOrdinal");
        }

        int ordinal = 0;
        int index = 0;
        while (index < NodeCount)
        {
            if (Nodes[index].ParentIndex == parentIndex)
            {
                if (ordinal == childOrdinal)
                {
                    return index;
                }

                ordinal = ordinal + 1;
            }

            index = index + 1;
        }

        throw new System.ArgumentOutOfRangeException("childOrdinal");
    }

    /// <summary>
    /// Applies the node total operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <returns>The resulting double.</returns>
    public double NodeTotal(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        double total = 0.0;
        int childCount = 0;
        int index = 0;
        while (index < NodeCount)
        {
            if (Nodes[index].ParentIndex == nodeIndex)
            {
                total = total + NodeTotal(index);
                childCount = childCount + 1;
            }

            index = index + 1;
        }

        if (childCount == 0)
        {
            return Nodes[nodeIndex].Value;
        }

        return total;
    }

    /// <summary>
    /// Applies the node depth operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <returns>The resulting int.</returns>
    public int NodeDepth(int nodeIndex)
    {
        ValidateNodeIndex(nodeIndex);
        int depth = 0;
        int parent = Nodes[nodeIndex].ParentIndex;
        while (parent >= 0)
        {
            depth = depth + 1;
            parent = Nodes[parent].ParentIndex;
        }

        return depth;
    }

    /// <summary>
    /// Applies the maximum depth operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting int.</returns>
    public int MaximumDepth()
    {
        int maximum = 0;
        int index = 0;
        while (index < NodeCount)
        {
            int depth = NodeDepth(index);
            if (depth > maximum)
            {
                maximum = depth;
            }

            index = index + 1;
        }

        return maximum;
    }

    /// <summary>
    /// Validates node index and returns structured issues without rendering.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void ValidateNodeIndex(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= NodeCount)
        {
            throw new System.ArgumentOutOfRangeException("nodeIndex");
        }
    }

    internal static ChartHierarchyNode[] CopyNodes(ChartHierarchyNode[] nodes)
    {
        if (nodes == null)
        {
            throw new System.ArgumentNullException("nodes");
        }

        ChartHierarchyNode[] copy = new ChartHierarchyNode[nodes.Length];
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == null)
            {
                throw new System.ArgumentException("Hierarchy nodes cannot contain null values.");
            }

            copy[index] = nodes[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateNodes(ChartHierarchyNode[] nodes)
    {
        if (nodes == null)
        {
            throw new System.ArgumentNullException("nodes");
        }

        if (nodes.Length == 0)
        {
            throw new System.ArgumentException("Hierarchy data requires at least one node.");
        }

        int rootCount = 0;
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == null)
            {
                throw new System.ArgumentException("Hierarchy nodes cannot contain null values.");
            }

            if (nodes[index].ParentIndex == -1)
            {
                rootCount = rootCount + 1;
            }
            else if (nodes[index].ParentIndex < 0 || nodes[index].ParentIndex >= index)
            {
                throw new System.ArgumentException("Hierarchy parent indexes must refer to an earlier node.");
            }

            index = index + 1;
        }

        if (rootCount != 1)
        {
            throw new System.ArgumentException("Hierarchy data requires exactly one root node.");
        }
    }

    internal static int FindRoot(ChartHierarchyNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index].ParentIndex == -1)
            {
                return index;
            }

            index = index + 1;
        }

        throw new System.ArgumentException("Hierarchy root node was not found.");
    }

    /// <summary>Returns the current nodes.</summary>
    /// <returns>The current field value.</returns>
    public ChartHierarchyNode[] GetNodes()
    {
        return Nodes;
    }

    /// <summary>Replaces the current nodes.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetNodes(ChartHierarchyNode[] value)
    {
        Nodes = value;
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

    /// <summary>Returns the current root index.</summary>
    /// <returns>The current field value.</returns>
    public int GetRootIndex()
    {
        return RootIndex;
    }

    /// <summary>Replaces the current root index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRootIndex(int value)
    {
        RootIndex = value;
    }
}
