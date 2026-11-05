using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SmartsMatcher"/>.
/// </summary>
public interface ISmartsMatcher
{
    /// <summary>
    /// Finds the first.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="smarts">The smarts.</param>
    /// <returns>The resulting subgraph match.</returns>
    SubgraphMatch FindFirst(Molecule target, string smarts);

    /// <summary>
    /// Finds the first.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting subgraph match.</returns>
    SubgraphMatch FindFirstQuery(Molecule target, QueryMolecule query);

    /// <summary>
    /// Returns every unique atom mapping that satisfies the target and query graphs.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="smarts">The smarts.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<SubgraphMatch> FindAll(Molecule target, string smarts);

    /// <summary>
    /// Returns every unique atom mapping that satisfies the target and query graphs.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    List<SubgraphMatch> FindAllQuery(Molecule target, QueryMolecule query);
}

