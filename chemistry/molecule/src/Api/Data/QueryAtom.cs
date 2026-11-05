using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides query atom data and operations for molecular graphs.
/// </summary>
public class QueryAtom
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The parenthesized component-group identifier, or <c>-1</c> when this atom is ungrouped.
    /// </summary>
    public int ComponentGroupId;
    /// <summary>
    /// The raw text.
    /// </summary>
    public string RawText;
    /// <summary>
    /// The logical SMARTS expression evaluated for this atom.
    /// </summary>
    public QueryExpression Expression;
    /// <summary>
    /// Whether this query primitive accepts any value.
    /// </summary>
    public bool MatchesAny;
    /// <summary>
    /// The element's atomic number; <c>0</c> represents an unspecified or query atom.
    /// </summary>
    public int AtomicNumber;
    /// <summary>
    /// Whether the value has atomic number.
    /// </summary>
    public bool HasAtomicNumber;
    /// <summary>
    /// The atomic numbers.
    /// </summary>
    public List<int> AtomicNumbers;
    /// <summary>
    /// Whether uses atomic number list.
    /// </summary>
    public bool UsesAtomicNumberList;
    /// <summary>
    /// Whether negate atomic number.
    /// </summary>
    public bool NegateAtomicNumber;
    /// <summary>
    /// Whether the value has aromaticity.
    /// </summary>
    public bool HasAromaticity;
    /// <summary>
    /// Whether the value is aromatic.
    /// </summary>
    public bool IsAromatic;
    /// <summary>
    /// Whether the value has formal charge.
    /// </summary>
    public bool HasFormalCharge;
    /// <summary>
    /// The formal charge.
    /// </summary>
    public int FormalCharge;
    /// <summary>
    /// Whether the value has isotope mass number.
    /// </summary>
    public bool HasIsotopeMassNumber;
    /// <summary>
    /// The isotope mass number.
    /// </summary>
    public int IsotopeMassNumber;
    /// <summary>
    /// Whether the value has hydrogen count.
    /// </summary>
    public bool HasHydrogenCount;
    /// <summary>
    /// The hydrogen count.
    /// </summary>
    public int HydrogenCount;
    /// <summary>
    /// Whether the value has degree.
    /// </summary>
    public bool HasDegree;
    /// <summary>
    /// The degree.
    /// </summary>
    public int Degree;
    /// <summary>
    /// Whether the value has ring count.
    /// </summary>
    public bool HasRingCount;
    /// <summary>
    /// The ring count.
    /// </summary>
    public int RingCount;
    /// <summary>
    /// Whether the value has valence.
    /// </summary>
    public bool HasValence;
    /// <summary>
    /// The valence.
    /// </summary>
    public int Valence;
    internal RingFinder Rings;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAtom"/> class.
    /// </summary>
    public QueryAtom()
    {
        Id = -1;
        ComponentGroupId = -1;
        RawText = "";
        Expression = new QueryExpression();
        MatchesAny = true;
        AtomicNumber = 0;
        HasAtomicNumber = false;
        AtomicNumbers = new List<int>();
        UsesAtomicNumberList = false;
        NegateAtomicNumber = false;
        HasAromaticity = false;
        IsAromatic = false;
        HasFormalCharge = false;
        FormalCharge = 0;
        HasIsotopeMassNumber = false;
        IsotopeMassNumber = 0;
        HasHydrogenCount = false;
        HydrogenCount = 0;
        HasDegree = false;
        Degree = 0;
        HasRingCount = false;
        RingCount = 0;
        HasValence = false;
        Valence = 0;
        Rings = new RingFinder();
    }

    /// <summary>
    /// Sets the any.
    /// </summary>
    internal void SetAny()
    {
        MatchesAny = true;
        HasAtomicNumber = false;
        UsesAtomicNumberList = false;
        HasAromaticity = false;
        HasFormalCharge = false;
        HasIsotopeMassNumber = false;
        HasHydrogenCount = false;
        HasDegree = false;
        HasRingCount = false;
        HasValence = false;
        AtomicNumbers.Clear();
    }

    /// <summary>
    /// Sets the atomic number.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    internal void SetAtomicNumber(int atomicNumber)
    {
        MatchesAny = false;
        HasAtomicNumber = true;
        UsesAtomicNumberList = false;
        AtomicNumber = atomicNumber;
    }

    /// <summary>
    /// Adds the atomic number.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    internal void AddAtomicNumber(int atomicNumber)
    {
        MatchesAny = false;
        UsesAtomicNumberList = true;
        AddUniqueInt(AtomicNumbers, atomicNumber);
    }

    /// <summary>
    /// Sets the aromaticity.
    /// </summary>
    /// <param name="isAromatic">The is aromatic.</param>
    internal void SetAromaticity(bool isAromatic)
    {
        MatchesAny = false;
        HasAromaticity = true;
        IsAromatic = isAromatic;
    }

    /// <summary>
    /// Sets the formal charge.
    /// </summary>
    /// <param name="formalCharge">The formal charge.</param>
    internal void SetFormalCharge(int formalCharge)
    {
        MatchesAny = false;
        HasFormalCharge = true;
        FormalCharge = formalCharge;
    }

    /// <summary>
    /// Sets the isotope mass number.
    /// </summary>
    /// <param name="isotopeMassNumber">The isotope mass number.</param>
    internal void SetIsotopeMassNumber(int isotopeMassNumber)
    {
        MatchesAny = false;
        HasIsotopeMassNumber = true;
        IsotopeMassNumber = isotopeMassNumber;
    }

    /// <summary>
    /// Sets the hydrogen count.
    /// </summary>
    /// <param name="hydrogenCount">The hydrogen count.</param>
    internal void SetHydrogenCount(int hydrogenCount)
    {
        MatchesAny = false;
        HasHydrogenCount = true;
        HydrogenCount = hydrogenCount;
    }

    /// <summary>
    /// Sets the degree.
    /// </summary>
    /// <param name="degree">The degree.</param>
    internal void SetDegree(int degree)
    {
        MatchesAny = false;
        HasDegree = true;
        Degree = degree;
    }

    /// <summary>
    /// Sets the ring count.
    /// </summary>
    /// <param name="ringCount">The ring count.</param>
    internal void SetRingCount(int ringCount)
    {
        MatchesAny = false;
        HasRingCount = true;
        RingCount = ringCount;
    }

    /// <summary>
    /// Sets the valence.
    /// </summary>
    /// <param name="valence">The valence.</param>
    internal void SetValence(int valence)
    {
        MatchesAny = false;
        HasValence = true;
        Valence = valence;
    }

    /// <summary>
    /// Determines whether the supplied atom or bond satisfies this query primitive.
    /// </summary>
    /// <param name="atom">The atom.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool Matches(Atom? atom)
    {
        if (atom == null)
        {
            return false;
        }

        if (!MatchesAtomicNumber(atom.AtomicNumber))
        {
            return false;
        }

        if (HasAromaticity && atom.IsAromatic != IsAromatic)
        {
            return false;
        }

        if (HasFormalCharge && atom.FormalCharge != FormalCharge)
        {
            return false;
        }

        if (HasIsotopeMassNumber && atom.IsotopeMassNumber != IsotopeMassNumber)
        {
            return false;
        }

        if (HasHydrogenCount && atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount != HydrogenCount)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the supplied atom or bond satisfies this query primitive.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool MatchesAtomInMolecule(Molecule? molecule, int atomId)
    {
        if (molecule == null || !molecule.IsValidAtomId(atomId))
        {
            return false;
        }

        if (Expression.Operation != QueryOperation.None)
        {
            return new SmartsExpressionMatcher().MatchesAtomExpression(Expression, molecule, atomId);
        }

        if (!Matches(molecule.Atoms[atomId]))
        {
            return false;
        }

        if (HasDegree && molecule.GetDegree(atomId) != Degree)
        {
            return false;
        }

        if (HasRingCount)
        {
            RingSet ringSet = Rings.EnsureRingCache(molecule);
            if (ringSet.GetAtomRingCount(atomId) != RingCount)
            {
                return false;
            }
        }

        if (HasValence && GetValence(molecule, atomId) != Valence)
        {
            return false;
        }

        return true;
    }

    internal int GetValence(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        int value = atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                value = value + GetBondOrderValue(bond);
            }

            bondIndex = bondIndex + 1;
        }

        return value;
    }

    internal int GetBondOrderValue(Bond bond)
    {
        if (bond.Order == BondOrder.Double)
        {
            return 2;
        }

        if (bond.Order == BondOrder.Triple)
        {
            return 3;
        }

        if (bond.Order == BondOrder.Quadruple)
        {
            return 4;
        }

        if (bond.Order == BondOrder.Aromatic || bond.IsAromatic)
        {
            return 1;
        }

        if (bond.Order == BondOrder.None || bond.Order == BondOrder.Unknown)
        {
            return 0;
        }

        return 1;
    }

    internal bool MatchesAtomicNumber(int atomicNumber)
    {
        bool matches = true;

        if (HasAtomicNumber)
        {
            matches = atomicNumber == AtomicNumber;
        }

        if (UsesAtomicNumberList)
        {
            matches = ContainsInt(AtomicNumbers, atomicNumber);
        }

        if (NegateAtomicNumber)
        {
            return !matches;
        }

        return matches;
    }

    internal void AddUniqueInt(List<int> values, int value)
    {
        if (!ContainsInt(values, value))
        {
            values.Add(value);
        }
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
