using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="RingFinder"/>.
/// </summary>
public interface IRingFinder
{
    /// <summary>
    /// Finds the rings.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting ring set.</returns>
    RingSet FindRings(Molecule? molecule);

    /// <summary>
    /// Finds a configurable cycle basis and bounded set of all small rings.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The ring-finding options; <c>null</c> uses the default cycle-basis policy.</param>
    /// <returns>The requested ring sets, membership, systems, limits, and diagnostics.</returns>
    RingFindingResult FindRingsWithOptions(Molecule? molecule, RingFindingOptions? options);

    /// <summary>
    /// Atom is in ring.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool AtomIsInRing(Molecule? molecule, int atomId);

    /// <summary>
    /// Bond is in ring.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    bool BondIsInRing(Molecule? molecule, int bondId);
}
