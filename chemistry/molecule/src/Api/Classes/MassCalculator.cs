namespace molecule;


/// <summary>
/// Calculates mass information from molecular graphs.
/// </summary>
public class MassCalculator : IMassCalculator
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassCalculator"/> class.
    /// </summary>
    public MassCalculator()
    {
        Elements = new ElementTable();
    }

    /// <summary>
    /// Calculates average molecular weight including attached hydrogens.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The calculated numeric value.</returns>
    public double MolecularWeight(Molecule? molecule)
    {
        double mass = 0.0;

        if (molecule == null)
        {
            return mass;
        }

        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            mass = mass + GetAverageMassForAtom(atom);
            mass = mass + GetAttachedHydrogenCount(atom) * Elements.GetAverageMass(1);
            index = index + 1;
        }

        return mass;
    }

    /// <summary>
    /// Calculates monoisotopic mass, using explicit isotope masses where present.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The calculated numeric value.</returns>
    public double ExactMass(Molecule? molecule)
    {
        double mass = 0.0;

        if (molecule == null)
        {
            return mass;
        }

        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            mass = mass + GetExactMassForAtom(atom);
            mass = mass + GetAttachedHydrogenCount(atom) * Elements.GetMonoisotopicMass(1);
            index = index + 1;
        }

        return mass;
    }

    /// <summary>
    /// Calculates integer nominal mass including attached hydrogens.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting count or integer value.</returns>
    public int NominalMass(Molecule? molecule)
    {
        int mass = 0;

        if (molecule == null)
        {
            return mass;
        }

        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            mass = mass + GetNominalMassForAtom(atom);
            mass = mass + GetAttachedHydrogenCount(atom);
            index = index + 1;
        }

        return mass;
    }

    internal double GetAverageMassForAtom(Atom? atom)
    {
        if (atom == null)
        {
            return 0.0;
        }

        if (atom.AtomicNumber <= 0)
        {
            return 0.0;
        }

        if (atom.IsotopeMassNumber > 0)
        {
            Isotope isotope = Elements.GetIsotope(atom.AtomicNumber, atom.IsotopeMassNumber);
            if (isotope.AtomicNumber != 0)
            {
                return isotope.ExactMass;
            }

            return atom.IsotopeMassNumber;
        }

        return Elements.GetAverageMass(atom.AtomicNumber);
    }

    internal double GetExactMassForAtom(Atom? atom)
    {
        if (atom == null)
        {
            return 0.0;
        }

        if (atom.AtomicNumber <= 0)
        {
            return 0.0;
        }

        if (atom.IsotopeMassNumber > 0)
        {
            Isotope isotope = Elements.GetIsotope(atom.AtomicNumber, atom.IsotopeMassNumber);
            if (isotope.AtomicNumber != 0)
            {
                return isotope.ExactMass;
            }

            return atom.IsotopeMassNumber;
        }

        return Elements.GetMonoisotopicMass(atom.AtomicNumber);
    }

    internal int GetNominalMassForAtom(Atom? atom)
    {
        if (atom == null)
        {
            return 0;
        }

        if (atom.AtomicNumber <= 0)
        {
            return 0;
        }

        if (atom.IsotopeMassNumber > 0)
        {
            return atom.IsotopeMassNumber;
        }

        Isotope isotope = Elements.GetMajorIsotope(atom.AtomicNumber);
        if (isotope.AtomicNumber != 0)
        {
            return isotope.MassNumber;
        }

        return (int)(Elements.GetMonoisotopicMass(atom.AtomicNumber) + 0.5);
    }

    internal int GetAttachedHydrogenCount(Atom? atom)
    {
        if (atom == null)
        {
            return 0;
        }

        return atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
    }
}

