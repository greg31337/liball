using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides element and isotope reference-data lookups.
/// </summary>
public class ElementTable : IElementTable
{
    internal List<Element> Elements;
    internal List<Isotope> Isotopes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementTable"/> class.
    /// </summary>
    public ElementTable()
    {
        Elements = new List<Element>();
        Isotopes = new List<Isotope>();
        AddBuiltInElements();
        AddBuiltInIsotopes();
    }

    /// <summary>
    /// Returns the element.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns>The resulting element.</returns>
    public Element GetElement(int atomicNumber)
    {
        int index = 0;
        while (index < Elements.Count)
        {
            if (Elements[index].AtomicNumber == atomicNumber)
            {
                return Elements[index];
            }

            index = index + 1;
        }

        return Element.CreateEmpty();
    }

    /// <summary>
    /// Returns the element by symbol.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The resulting element.</returns>
    public Element GetElementBySymbol(string symbol)
    {
        string normalizedSymbol = Element.NormalizeString(symbol);
        int index = 0;
        while (index < Elements.Count)
        {
            if (Elements[index].Symbol == normalizedSymbol)
            {
                return Elements[index];
            }

            index = index + 1;
        }

        return Element.CreateEmpty();
    }

    /// <summary>
    /// Determines whether the table contains the supplied atomic number.
    /// </summary>
    /// <param name="atomicNumber">The atomic number to test.</param>
    /// <returns><c>true</c> when the element is present; otherwise, <c>false</c>.</returns>
    public bool IsKnownElement(int atomicNumber)
    {
        Element element = GetElement(atomicNumber);
        return element.AtomicNumber != 0;
    }

    /// <summary>
    /// Determines whether the value is known symbol.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsKnownSymbol(string symbol)
    {
        Element element = GetElementBySymbol(symbol);
        return element.AtomicNumber != 0;
    }

    /// <summary>
    /// Returns the isotope records registered for an element.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>A new list of matching isotope records.</returns>
    public List<Isotope> GetIsotopes(int atomicNumber)
    {
        List<Isotope> result = new List<Isotope>();
        int index = 0;
        while (index < Isotopes.Count)
        {
            if (Isotopes[index].AtomicNumber == atomicNumber)
            {
                result.Add(Isotopes[index]);
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Returns a specific isotope record by element and mass number.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <param name="massNumber">The isotope mass number.</param>
    /// <returns>The matching record, or an empty record when no match exists.</returns>
    public Isotope GetIsotope(int atomicNumber, int massNumber)
    {
        int index = 0;
        while (index < Isotopes.Count)
        {
            Isotope isotope = Isotopes[index];
            if (isotope.AtomicNumber == atomicNumber && isotope.MassNumber == massNumber)
            {
                return isotope;
            }

            index = index + 1;
        }

        return Isotope.CreateEmpty();
    }

    /// <summary>
    /// Returns the isotope marked as the major naturally occurring isotope for an element.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The major isotope, or an empty record when the element is unknown.</returns>
    public Isotope GetMajorIsotope(int atomicNumber)
    {
        int index = 0;
        while (index < Isotopes.Count)
        {
            Isotope isotope = Isotopes[index];
            if (isotope.AtomicNumber == atomicNumber && isotope.IsMajor)
            {
                return isotope;
            }

            index = index + 1;
        }

        return Isotope.CreateEmpty();
    }

    /// <summary>
    /// Returns the element's standard atomic weight or representative isotope mass number.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The standard atomic weight, the representative isotope mass number when no standard atomic weight exists, or <c>0</c> when the element is unknown.</returns>
    public double GetAverageMass(int atomicNumber)
    {
        return GetElement(atomicNumber).AverageMass;
    }

    /// <summary>
    /// Returns the exact mass of the element's most abundant or designated representative isotope.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The monoisotopic mass, or <c>0</c> when no isotope is registered.</returns>
    public double GetMonoisotopicMass(int atomicNumber)
    {
        return GetElement(atomicNumber).MonoisotopicMass;
    }

    /// <summary>
    /// Returns the element's covalent radius in angstroms.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The radius, or <c>0</c> when the element is unknown.</returns>
    public double GetCovalentRadius(int atomicNumber)
    {
        return GetElement(atomicNumber).CovalentRadius;
    }

    internal void AddBuiltInElements()
    {
        PeriodicTableData data = new PeriodicTableData();
        data.AddElements(Elements);
    }

    internal void AddBuiltInIsotopes()
    {
        PeriodicTableData data = new PeriodicTableData();
        data.AddIsotopes(Isotopes);
    }
}
