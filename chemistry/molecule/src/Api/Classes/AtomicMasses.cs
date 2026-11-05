namespace molecule;


/// <summary>
/// Provides atomic mass lookups for elements and isotopes.
/// </summary>
public class AtomicMasses : IAtomicMasses
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomicMasses"/> class.
    /// </summary>
    public AtomicMasses()
    {
        Elements = new ElementTable();
    }

    /// <summary>
    /// Returns the standard atomic weight or representative isotope mass number.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns>The standard atomic weight, the representative isotope mass number when no standard atomic weight exists, or <c>0</c> when the element is unknown.</returns>
    public double GetAverageMass(int atomicNumber)
    {
        return Elements.GetAverageMass(atomicNumber);
    }

    /// <summary>
    /// Returns the exact mass of the most abundant or designated representative isotope for an element.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The monoisotopic mass, or <c>0</c> when the element is unknown.</returns>
    public double GetMonoisotopicMass(int atomicNumber)
    {
        return Elements.GetMonoisotopicMass(atomicNumber);
    }

    /// <summary>
    /// Returns the exact mass recorded for a specific isotope.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <param name="massNumber">The isotope mass number.</param>
    /// <returns>The exact isotope mass, or <c>0</c> when the isotope is unknown.</returns>
    public double GetIsotopeMass(int atomicNumber, int massNumber)
    {
        return Elements.GetIsotope(atomicNumber, massNumber).ExactMass;
    }
}
