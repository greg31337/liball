namespace molecule;


/// <summary>
/// Provides graph isomorphism data and operations for molecular graphs.
/// </summary>
public class GraphIsomorphism : IGraphIsomorphism
{
    internal SubgraphMatcher Matcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphIsomorphism"/> class.
    /// </summary>
    public GraphIsomorphism()
    {
        Matcher = new SubgraphMatcher();
    }

    /// <summary>
    /// Determines whether two molecules have the same atom and bond graph.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AreIsomorphic(Molecule? left, Molecule? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left.Atoms.Count != right.Atoms.Count || left.Bonds.Count != right.Bonds.Count)
        {
            return false;
        }

        return Matcher.ContainsSubgraph(left, right);
    }
}

