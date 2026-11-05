namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="GraphIsomorphism"/>.
/// </summary>
public interface IGraphIsomorphism
{
    /// <summary>
    /// Determines whether two molecules have the same atom and bond graph.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AreIsomorphic(Molecule? left, Molecule? right);
}

