namespace version;

/// <summary>
/// Represents the outcome of a nuget dependency resolution operation.
/// </summary>
/// <remarks>
/// Check the success state before requesting the produced value. Failure results retain diagnostics that explain why processing stopped.
/// </remarks>
public class NuGetDependencyResolutionResult
{
    /// <summary>
    /// Stores the <c>nodes</c> value.
    /// </summary>
    public NuGetDependencyGraphNode[] nodes;
    /// <summary>
    /// Stores the <c>edges</c> value.
    /// </summary>
    public NuGetDependencyGraphEdge[] edges;
    /// <summary>
    /// Stores the <c>conflicts</c> value.
    /// </summary>
    public NuGetDependencyConflict[] conflicts;

    /// <summary>
    /// Initializes a new NuGet Dependency Resolution Result instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nodeValues">The node values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="edgeValues">The edge values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="conflictValues">The conflict values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetDependencyResolutionResult(
        NuGetDependencyGraphNode[] nodeValues,
        NuGetDependencyGraphEdge[] edgeValues,
        NuGetDependencyConflict[] conflictValues)
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
    /// <returns>The resulting nuget dependency graph node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal NuGetDependencyGraphNode GetNode(int nodeIndex)
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
    internal NuGetDependencyGraphNode[] GetNodes()
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
    /// <returns>The resulting nuget dependency graph edge.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal NuGetDependencyGraphEdge GetEdge(int edgeIndex)
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
    internal NuGetDependencyGraphEdge[] GetEdges()
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
    /// <returns>The resulting nuget dependency conflict.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal NuGetDependencyConflict GetConflict(int conflictIndex)
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
    internal NuGetDependencyConflict[] GetConflicts()
    {
        return CopyConflicts(conflicts);
    }

    /// <summary>
    /// Reports whether this value has selected package.
    /// </summary>
    /// <param name="packageIdText">The package id text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSelectedPackage(string packageIdText)
    {
        return FindNode(packageIdText) != null;
    }

    /// <summary>
    /// Gets a selected package.
    /// </summary>
    /// <param name="packageIdText">The package id text to process.</param>
    /// <returns>The resulting nuget dependency graph node.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal NuGetDependencyGraphNode GetSelectedPackage(string packageIdText)
    {
        NuGetDependencyGraphNode? node = FindNode(packageIdText);
        if (node == null)
        {
            throw new System.InvalidOperationException("Dependency resolution did not select the requested package.");
        }

        return node;
    }

    internal NuGetDependencyGraphNode? FindNode(string packageIdText)
    {
        NuGetPackageId packageId = new NuGetPackageId(packageIdText);
        int index = 0;

        while (index < nodes.Length)
        {
            if (nodes[index].GetIdentity().GetPackageId().EqualsId(packageId))
            {
                return nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static NuGetDependencyGraphNode[] CopyNodes(NuGetDependencyGraphNode[] nodeValues)
    {
        NuGetDependencyGraphNode[] copy = new NuGetDependencyGraphNode[nodeValues.Length];
        int index = 0;

        while (index < nodeValues.Length)
        {
            if (nodeValues[index] == null)
            {
                throw new System.ArgumentException("Dependency graph node arrays cannot contain null values.");
            }

            copy[index] = nodeValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static NuGetDependencyGraphEdge[] CopyEdges(NuGetDependencyGraphEdge[] edgeValues)
    {
        NuGetDependencyGraphEdge[] copy = new NuGetDependencyGraphEdge[edgeValues.Length];
        int index = 0;

        while (index < edgeValues.Length)
        {
            if (edgeValues[index] == null)
            {
                throw new System.ArgumentException("Dependency graph edge arrays cannot contain null values.");
            }

            copy[index] = edgeValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static NuGetDependencyConflict[] CopyConflicts(NuGetDependencyConflict[] conflictValues)
    {
        NuGetDependencyConflict[] copy = new NuGetDependencyConflict[conflictValues.Length];
        int index = 0;

        while (index < conflictValues.Length)
        {
            if (conflictValues[index] == null)
            {
                throw new System.ArgumentException("Dependency conflict arrays cannot contain null values.");
            }

            copy[index] = conflictValues[index];
            index = index + 1;
        }

        return copy;
    }
}
