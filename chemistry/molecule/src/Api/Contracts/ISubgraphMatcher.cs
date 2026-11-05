using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SubgraphMatcher"/>.
/// </summary>
public interface ISubgraphMatcher
{
    /// <summary>
    /// Determines whether the value contains subgraph.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool ContainsSubgraph(Molecule? target, Molecule? query);

    /// <summary>
    /// Determines whether the value contains query.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool ContainsQuery(Molecule? target, QueryMolecule? query);

    /// <summary>
    /// Finds the first.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting subgraph match.</returns>
    SubgraphMatch FindFirst(Molecule? target, Molecule? query);

    /// <summary>
    /// Returns every unique atom mapping that satisfies the target and query graphs.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<SubgraphMatch> FindAll(Molecule? target, Molecule? query);

    /// <summary>
    /// Finds the first.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting subgraph match.</returns>
    SubgraphMatch FindFirstQuery(Molecule? target, QueryMolecule? query);

    /// <summary>
    /// Returns every unique atom mapping that satisfies the target and query graphs.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<SubgraphMatch> FindAllQuery(Molecule? target, QueryMolecule? query);
}

