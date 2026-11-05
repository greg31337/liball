namespace molecule;


/// <summary>
/// Defines atomic mass lookup operations.
/// </summary>
public interface IAtomicMasses
{
    /// <summary>
    /// Returns the average mass.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns>The calculated numeric value.</returns>
    double GetAverageMass(int atomicNumber);

    /// <summary>
    /// Returns the mass of the most abundant isotope for an element.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The monoisotopic mass, or <c>0</c> when the element is unknown.</returns>
    double GetMonoisotopicMass(int atomicNumber);

    /// <summary>
    /// Returns the exact mass recorded for a specific isotope.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <param name="massNumber">The isotope mass number.</param>
    /// <returns>The exact isotope mass, or <c>0</c> when the isotope is unknown.</returns>
    double GetIsotopeMass(int atomicNumber, int massNumber);
}
