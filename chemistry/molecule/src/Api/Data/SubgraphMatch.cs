using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides subgraph match data and operations for molecular graphs.
/// </summary>
public class SubgraphMatch
{
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The atom mapping.
    /// </summary>
    public int[] AtomMapping;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubgraphMatch"/> class.
    /// </summary>
    public SubgraphMatch()
    {
        Success = false;
        AtomMapping = new int[0];
    }

    /// <summary>
    /// Returns the target atom identifier.
    /// </summary>
    /// <param name="queryAtomId">The query atom identifier.</param>
    /// <returns>The zero-based identifier, or <c>-1</c> when the operation cannot resolve one.</returns>
    internal int GetTargetAtomId(int queryAtomId)
    {
        if (queryAtomId < 0 || queryAtomId >= AtomMapping.Length)
        {
            return -1;
        }

        return AtomMapping[queryAtomId];
    }
}

