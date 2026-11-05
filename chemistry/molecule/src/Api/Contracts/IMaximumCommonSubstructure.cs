namespace molecule;


/// <summary>
/// Defines bounded maximum-common-substructure operations provided by <see cref="MaximumCommonSubstructure"/>.
/// </summary>
public interface IMaximumCommonSubstructure
{
    /// <summary>
    /// Finds a deterministic maximum common connected substructure within caller-provided work and time limits.
    /// </summary>
    /// <param name="left">The first molecule.</param>
    /// <param name="right">The second molecule.</param>
    /// <param name="options">The atom, bond, connectivity, and search-limit options; <c>null</c> uses bounded defaults.</param>
    /// <returns>The best mapping found, its common graph, search counts, limits, and diagnostics.</returns>
    MaximumCommonSubstructureResult Find(Molecule? left, Molecule? right, MaximumCommonSubstructureOptions? options);
}
