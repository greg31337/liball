namespace molecule;


/// <summary>
/// Defines fingerprint screening followed by exact substructure matching.
/// </summary>
public interface IFingerprintScreening
{
    /// <summary>
    /// Screens an exact molecular query with path fingerprints and runs exact matching only when the screen passes.
    /// </summary>
    /// <param name="target">The molecule that may contain the query.</param>
    /// <param name="query">The exact molecular substructure to find.</param>
    /// <param name="options">The fingerprint options used by the conservative screen.</param>
    /// <returns>The screening and exact-match result.</returns>
    FingerprintScreeningResult FindFirst(Molecule? target, Molecule? query, FingerprintOptions? options);
}
