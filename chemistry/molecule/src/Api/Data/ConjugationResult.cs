using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by conjugation.
/// </summary>
public class ConjugationResult
{
    /// <summary>
    /// The conjugated atom identifiers.
    /// </summary>
    public List<int> ConjugatedAtomIds;
    /// <summary>
    /// The conjugated bond identifiers.
    /// </summary>
    public List<int> ConjugatedBondIds;
    /// <summary>
    /// The systems.
    /// </summary>
    public List<ConjugatedSystem> Systems;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConjugationResult"/> class.
    /// </summary>
    public ConjugationResult()
    {
        ConjugatedAtomIds = new List<int>();
        ConjugatedBondIds = new List<int>();
        Systems = new List<ConjugatedSystem>();
    }

    /// <summary>
    /// Atom is conjugated.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AtomIsConjugated(int atomId)
    {
        return ContainsInt(ConjugatedAtomIds, atomId);
    }

    /// <summary>
    /// Bond is conjugated.
    /// </summary>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool BondIsConjugated(int bondId)
    {
        return ContainsInt(ConjugatedBondIds, bondId);
    }

    internal bool ContainsInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

