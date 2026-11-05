using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides element data and operations for molecular graphs.
/// </summary>
public class Element
{
    /// <summary>
    /// The element's atomic number; <c>0</c> represents an unspecified or query atom.
    /// </summary>
    public int AtomicNumber;
    /// <summary>
    /// The symbol.
    /// </summary>
    public string Symbol;
    /// <summary>
    /// The name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The standard atomic weight, or the mass number of the designated representative isotope when no standard atomic weight exists.
    /// </summary>
    public double AverageMass;
    /// <summary>
    /// The exact mass of the most abundant naturally occurring isotope, or of the designated representative isotope when none occurs naturally.
    /// </summary>
    public double MonoisotopicMass;
    /// <summary>
    /// The covalent radius.
    /// </summary>
    public double CovalentRadius;
    /// <summary>
    /// Whether the value is organic subset.
    /// </summary>
    public bool IsOrganicSubset;
    /// <summary>
    /// The common valences.
    /// </summary>
    public List<int> CommonValences;

    /// <summary>
    /// Creates an empty element sentinel.
    /// </summary>
    /// <returns>An element with atomic number zero and no descriptive values.</returns>
    internal static Element CreateEmpty()
    {
        return new Element(0, "", "", 0.0, 0.0, 0.0, false, new int[] { });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Element"/> class.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <param name="symbol">The symbol.</param>
    /// <param name="name">The name.</param>
    /// <param name="averageMass">The average mass.</param>
    /// <param name="monoisotopicMass">The monoisotopic mass.</param>
    /// <param name="covalentRadius">The covalent radius.</param>
    /// <param name="isOrganicSubset">The is organic subset.</param>
    /// <param name="commonValences">The common valences.</param>
    public Element(int atomicNumber, string symbol, string name, double averageMass, double monoisotopicMass, double covalentRadius, bool isOrganicSubset, int[] commonValences)
    {
        AtomicNumber = atomicNumber;
        Symbol = NormalizeString(symbol);
        Name = NormalizeString(name);
        AverageMass = averageMass;
        MonoisotopicMass = monoisotopicMass;
        CovalentRadius = covalentRadius;
        IsOrganicSubset = isOrganicSubset;
        CommonValences = new List<int>();

        int index = 0;
        while (index < commonValences.Length)
        {
            CommonValences.Add(commonValences[index]);
            index = index + 1;
        }
    }

    internal bool HasCommonValence(int valence)
    {
        int index = 0;
        while (index < CommonValences.Count)
        {
            if (CommonValences[index] == valence)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
