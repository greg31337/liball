namespace molecule;


/// <summary>
/// Defines coordinate-based bond-order perception provided by <see cref="BondOrderPerceiver"/>.
/// </summary>
public interface IBondOrderPerceiver
{
    /// <summary>
    /// Perceives bond orders from atom or conformer coordinates on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule whose existing connectivity is evaluated.</param>
    /// <param name="options">The coordinate and confidence policy; <c>null</c> uses conservative defaults.</param>
    /// <returns>The copied molecule, per-bond proposals, confidence data, and diagnostics.</returns>
    BondOrderPerceptionResult PerceiveBondOrders(Molecule? molecule, BondOrderPerceptionOptions? options);
}
