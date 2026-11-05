using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides common-valence lookups for elements.
/// </summary>
public class ValenceTable : IValenceTable
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValenceTable"/> class.
    /// </summary>
    public ValenceTable()
    {
        Elements = new ElementTable();
    }

    /// <summary>
    /// Returns a copy of the common valences registered for an element.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The common valences in table order, or an empty list for an unknown element.</returns>
    public List<int> GetCommonValences(int atomicNumber)
    {
        Element element = Elements.GetElement(atomicNumber);
        List<int> values = new List<int>();
        int index = 0;
        while (index < element.CommonValences.Count)
        {
            values.Add(element.CommonValences[index]);
            index = index + 1;
        }

        return values;
    }

    /// <summary>
    /// Determines whether the value is allowed valence.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <param name="valence">The valence.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsAllowedValence(int atomicNumber, int valence)
    {
        Element element = Elements.GetElement(atomicNumber);
        return element.HasCommonValence(valence);
    }
}
