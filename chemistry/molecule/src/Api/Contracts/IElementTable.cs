using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines element and isotope reference-data lookup operations.
/// </summary>
public interface IElementTable
{
    /// <summary>
    /// Returns the element.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns>The resulting element.</returns>
    Element GetElement(int atomicNumber);

    /// <summary>
    /// Returns the element by symbol.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The resulting element.</returns>
    Element GetElementBySymbol(string symbol);

    /// <summary>
    /// Determines whether the table contains the supplied atomic number.
    /// </summary>
    /// <param name="atomicNumber">The atomic number to test.</param>
    /// <returns><c>true</c> when the element is present; otherwise, <c>false</c>.</returns>
    bool IsKnownElement(int atomicNumber);

    /// <summary>
    /// Determines whether the table contains the supplied element symbol.
    /// </summary>
    /// <param name="symbol">The symbol to test.</param>
    /// <returns><c>true</c> when the element is present; otherwise, <c>false</c>.</returns>
    bool IsKnownSymbol(string symbol);

    /// <summary>
    /// Returns the isotope records registered for an element.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>A new list of matching isotope records.</returns>
    List<Isotope> GetIsotopes(int atomicNumber);

    /// <summary>
    /// Returns a specific isotope record by element and mass number.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <param name="massNumber">The isotope mass number.</param>
    /// <returns>The matching record, or an empty record when no match exists.</returns>
    Isotope GetIsotope(int atomicNumber, int massNumber);

    /// <summary>
    /// Returns the isotope marked as the major naturally occurring isotope for an element.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The major isotope, or an empty record when the element is unknown.</returns>
    Isotope GetMajorIsotope(int atomicNumber);

    /// <summary>
    /// Returns the element's abundance-weighted average atomic mass.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The average mass, or <c>0</c> when the element is unknown.</returns>
    double GetAverageMass(int atomicNumber);

    /// <summary>
    /// Returns the exact mass of the element's major isotope.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The monoisotopic mass, or <c>0</c> when no isotope is registered.</returns>
    double GetMonoisotopicMass(int atomicNumber);

    /// <summary>
    /// Returns the element's covalent radius in angstroms.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The radius, or <c>0</c> when the element is unknown.</returns>
    double GetCovalentRadius(int atomicNumber);
}
