namespace version;

/// <summary>
/// Represents the outcome of a rubygems dependency resolution operation.
/// </summary>
/// <remarks>
/// Check the success state before requesting the produced value. Failure results retain diagnostics that explain why processing stopped.
/// </remarks>
public class RubyGemsDependencyResolutionResult
{
    /// <summary>
    /// Stores the <c>nodes</c> value.
    /// </summary>
    public RubyGemsDependencyGraphNode[] nodes;
    /// <summary>
    /// Stores the <c>edges</c> value.
    /// </summary>
    public RubyGemsDependencyGraphEdge[] edges;
    /// <summary>
    /// Stores the <c>conflicts</c> value.
    /// </summary>
    public RubyGemsDependencyConflict[] conflicts;

    /// <summary>
    /// Initializes a new RubyGems Dependency Resolution Result instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nodeValues">The node values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="edgeValues">The edge values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="conflictValues">The conflict values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public RubyGemsDependencyResolutionResult(
        RubyGemsDependencyGraphNode[] nodeValues,
        RubyGemsDependencyGraphEdge[] edgeValues,
        RubyGemsDependencyConflict[] conflictValues)
    {
        System.ArgumentNullException.ThrowIfNull(nodeValues);
        System.ArgumentNullException.ThrowIfNull(edgeValues);
        System.ArgumentNullException.ThrowIfNull(conflictValues);

        nodes = CopyNodes(nodeValues);
        edges = CopyEdges(edgeValues);
        conflicts = CopyConflicts(conflictValues);
    }

    /// <summary>
    /// Reports whether the operation completed successfully.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSuccess()
    {
        return conflicts.Length == 0;
    }

    /// <summary>
    /// Reports whether the operation failed and produced diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsFailure()
    {
        return conflicts.Length > 0;
    }

    /// <summary>
    /// Gets a node count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetNodeCount()
    {
        return nodes.Length;
    }

    /// <summary>
    /// Gets a node.
    /// </summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>The resulting rubygems dependency graph node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal RubyGemsDependencyGraphNode GetNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= nodes.Length)
        {
            throw new System.ArgumentOutOfRangeException("nodeIndex");
        }

        return nodes[nodeIndex];
    }

    /// <summary>
    /// Gets a nodes.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal RubyGemsDependencyGraphNode[] GetNodes()
    {
        return CopyNodes(nodes);
    }

    /// <summary>
    /// Gets an edge count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetEdgeCount()
    {
        return edges.Length;
    }

    /// <summary>
    /// Gets an edge.
    /// </summary>
    /// <param name="edgeIndex">The zero-based edge index.</param>
    /// <returns>The resulting rubygems dependency graph edge.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal RubyGemsDependencyGraphEdge GetEdge(int edgeIndex)
    {
        if (edgeIndex < 0 || edgeIndex >= edges.Length)
        {
            throw new System.ArgumentOutOfRangeException("edgeIndex");
        }

        return edges[edgeIndex];
    }

    /// <summary>
    /// Gets an edges.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal RubyGemsDependencyGraphEdge[] GetEdges()
    {
        return CopyEdges(edges);
    }

    /// <summary>
    /// Gets a conflict count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetConflictCount()
    {
        return conflicts.Length;
    }

    /// <summary>
    /// Gets a conflict.
    /// </summary>
    /// <param name="conflictIndex">The zero-based conflict index.</param>
    /// <returns>The resulting rubygems dependency conflict.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal RubyGemsDependencyConflict GetConflict(int conflictIndex)
    {
        if (conflictIndex < 0 || conflictIndex >= conflicts.Length)
        {
            throw new System.ArgumentOutOfRangeException("conflictIndex");
        }

        return conflicts[conflictIndex];
    }

    /// <summary>
    /// Gets a conflicts.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal RubyGemsDependencyConflict[] GetConflicts()
    {
        return CopyConflicts(conflicts);
    }

    /// <summary>
    /// Reports whether this value has selected specification.
    /// </summary>
    /// <param name="nameText">The name text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSelectedSpecification(string nameText)
    {
        return FindNode(nameText) != null;
    }

    /// <summary>
    /// Gets a selected specification.
    /// </summary>
    /// <param name="nameText">The name text to process.</param>
    /// <returns>The resulting rubygems dependency graph node.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal RubyGemsDependencyGraphNode GetSelectedSpecification(string nameText)
    {
        RubyGemsDependencyGraphNode? node = FindNode(nameText);
        if (node == null)
        {
            throw new System.InvalidOperationException("RubyGems dependency resolution did not select the requested specification.");
        }

        return node;
    }

    internal RubyGemsDependencyGraphNode? FindNode(string nameText)
    {
        RubyGemsDependency.ValidateName(nameText, "RubyGems dependency resolution lookup name cannot be empty or contain invalid characters.");
        int index = 0;

        while (index < nodes.Length)
        {
            if (nodes[index].GetSpecification().MatchesName(nameText))
            {
                return nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static RubyGemsDependencyGraphNode[] CopyNodes(RubyGemsDependencyGraphNode[] nodeValues)
    {
        RubyGemsDependencyGraphNode[] copy = new RubyGemsDependencyGraphNode[nodeValues.Length];
        int index = 0;

        while (index < nodeValues.Length)
        {
            if (nodeValues[index] == null)
            {
                throw new System.ArgumentException("RubyGems dependency graph node arrays cannot contain null values.");
            }

            copy[index] = nodeValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static RubyGemsDependencyGraphEdge[] CopyEdges(RubyGemsDependencyGraphEdge[] edgeValues)
    {
        RubyGemsDependencyGraphEdge[] copy = new RubyGemsDependencyGraphEdge[edgeValues.Length];
        int index = 0;

        while (index < edgeValues.Length)
        {
            if (edgeValues[index] == null)
            {
                throw new System.ArgumentException("RubyGems dependency graph edge arrays cannot contain null values.");
            }

            copy[index] = edgeValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static RubyGemsDependencyConflict[] CopyConflicts(RubyGemsDependencyConflict[] conflictValues)
    {
        RubyGemsDependencyConflict[] copy = new RubyGemsDependencyConflict[conflictValues.Length];
        int index = 0;

        while (index < conflictValues.Length)
        {
            if (conflictValues[index] == null)
            {
                throw new System.ArgumentException("RubyGems dependency conflict arrays cannot contain null values.");
            }

            copy[index] = conflictValues[index];
            index = index + 1;
        }

        return copy;
    }
}
