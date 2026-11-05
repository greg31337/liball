using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides basic descriptors data and operations for molecular graphs.
/// </summary>
public class BasicDescriptors : IBasicDescriptors
{
    internal FormulaCalculator FormulaCalculator;
    internal MassCalculator MassCalculator;
    internal RingFinder Rings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicDescriptors"/> class.
    /// </summary>
    public BasicDescriptors()
    {
        FormulaCalculator = new FormulaCalculator();
        MassCalculator = new MassCalculator();
        Rings = new RingFinder();
    }

    /// <summary>
    /// Calculates the values represented by this calculator for the supplied molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting descriptor set.</returns>
    public DescriptorSet Calculate(Molecule? molecule)
    {
        DescriptorSet set = new DescriptorSet();

        if (molecule == null)
        {
            return set;
        }

        set.Formula = FormulaCalculator.Formula(molecule);
        set.MolecularWeight = MassCalculator.MolecularWeight(molecule);
        set.ExactMass = MassCalculator.ExactMass(molecule);
        set.NominalMass = MassCalculator.NominalMass(molecule);
        FillAtomCounts(molecule, set);
        FillStructuralDescriptors(molecule, set);
        return set;
    }

    internal void FillAtomCounts(Molecule molecule, DescriptorSet set)
    {
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            int attachedHydrogenCount = atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;

            set.ExplicitAtomCount = set.ExplicitAtomCount + 1;
            set.AtomCount = set.AtomCount + 1 + attachedHydrogenCount;
            set.HydrogenAtomCount = set.HydrogenAtomCount + attachedHydrogenCount;
            set.FormalCharge = set.FormalCharge + atom.FormalCharge;

            if (atom.AtomicNumber == 1)
            {
                set.HydrogenAtomCount = set.HydrogenAtomCount + 1;
            }

            if (atom.AtomicNumber == 6)
            {
                set.CarbonAtomCount = set.CarbonAtomCount + 1;
            }

            if (atom.AtomicNumber > 1)
            {
                set.HeavyAtomCount = set.HeavyAtomCount + 1;
            }

            if (atom.AtomicNumber > 1 && atom.AtomicNumber != 6)
            {
                set.HeteroAtomCount = set.HeteroAtomCount + 1;
            }

            index = index + 1;
        }
    }

    internal void FillStructuralDescriptors(Molecule molecule, DescriptorSet set)
    {
        RingSet ringSet = Rings.EnsureRingCache(molecule);
        set.RingCount = ringSet.GetRingCount();
        set.AromaticRingCount = CountAromaticRings(ringSet);
        set.RotatableBondCount = CountRotatableBonds(molecule, ringSet);

        int sp3CarbonCount = 0;
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            if (IsHydrogenBondDonor(atom))
            {
                set.HydrogenBondDonorCount = set.HydrogenBondDonorCount + 1;
            }

            if (IsHydrogenBondAcceptor(molecule, atomIndex))
            {
                set.HydrogenBondAcceptorCount = set.HydrogenBondAcceptorCount + 1;
            }

            set.TopologicalPolarSurfaceArea = set.TopologicalPolarSurfaceArea + ApproximateAtomPolarSurfaceArea(molecule, atomIndex);

            if (IsSp3Carbon(molecule, atomIndex))
            {
                sp3CarbonCount = sp3CarbonCount + 1;
            }

            atomIndex = atomIndex + 1;
        }

        if (set.CarbonAtomCount > 0)
        {
            set.FractionCsp3 = (double)sp3CarbonCount / (double)set.CarbonAtomCount;
        }
    }

    internal int CountAromaticRings(RingSet ringSet)
    {
        int count = 0;
        int index = 0;
        while (index < ringSet.Rings.Count)
        {
            if (ringSet.Rings[index].IsAromatic)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountRotatableBonds(Molecule molecule, RingSet ringSet)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            if (IsRotatableBond(molecule, ringSet, bondIndex))
            {
                count = count + 1;
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal bool IsRotatableBond(Molecule molecule, RingSet ringSet, int bondId)
    {
        if (bondId < 0 || bondId >= molecule.Bonds.Count)
        {
            return false;
        }

        Bond bond = molecule.Bonds[bondId];
        if (bond.Order != BondOrder.Single || bond.IsAromatic || bond.Order == BondOrder.Aromatic || bond.IsDative)
        {
            return false;
        }

        if (ringSet.BondIsInRing(bondId))
        {
            return false;
        }

        if (!molecule.IsValidAtomId(bond.BeginAtomId) || !molecule.IsValidAtomId(bond.EndAtomId))
        {
            return false;
        }

        Atom begin = molecule.Atoms[bond.BeginAtomId];
        Atom end = molecule.Atoms[bond.EndAtomId];
        if (begin.AtomicNumber <= 1 || end.AtomicNumber <= 1)
        {
            return false;
        }

        if (CountHeavyNeighbors(molecule, bond.BeginAtomId) <= 1 || CountHeavyNeighbors(molecule, bond.EndAtomId) <= 1)
        {
            return false;
        }

        return true;
    }

    internal int CountHeavyNeighbors(Molecule molecule, int atomId)
    {
        int count = 0;
        List<int> neighbors = molecule.GetNeighborAtomIds(atomId);
        int index = 0;
        while (index < neighbors.Count)
        {
            if (molecule.Atoms[neighbors[index]].AtomicNumber > 1)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool IsHydrogenBondDonor(Atom atom)
    {
        int hydrogenCount = atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
        if (hydrogenCount <= 0)
        {
            return false;
        }

        if (atom.AtomicNumber == 7 || atom.AtomicNumber == 8 || atom.AtomicNumber == 16)
        {
            return true;
        }

        return false;
    }

    internal bool IsHydrogenBondAcceptor(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.FormalCharge > 0)
        {
            return false;
        }

        if (atom.AtomicNumber == 8 || atom.AtomicNumber == 16)
        {
            return true;
        }

        if (atom.AtomicNumber == 7 && GetBondOrderSum(molecule, atomId) <= 3)
        {
            return true;
        }

        return false;
    }

    internal double ApproximateAtomPolarSurfaceArea(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber == 7)
        {
            return 12.03;
        }

        if (atom.AtomicNumber == 8)
        {
            return 17.07;
        }

        if (atom.AtomicNumber == 15)
        {
            return 13.59;
        }

        if (atom.AtomicNumber == 16)
        {
            return 25.30;
        }

        return 0.0;
    }

    internal bool IsSp3Carbon(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber != 6)
        {
            return false;
        }

        if (atom.IsAromatic)
        {
            return false;
        }

        if (HasPiOrAromaticBond(molecule, atomId))
        {
            return false;
        }

        return true;
    }

    internal bool HasPiOrAromaticBond(Molecule molecule, int atomId)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                if (bond.IsAromatic || bond.Order == BondOrder.Aromatic || bond.Order == BondOrder.Double || bond.Order == BondOrder.Triple || bond.Order == BondOrder.Quadruple)
                {
                    return true;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return false;
    }

    internal int GetBondOrderSum(Molecule molecule, int atomId)
    {
        int sum = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                sum = sum + GetBondOrderValue(bond);
            }

            bondIndex = bondIndex + 1;
        }

        return sum;
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

        if (bond.Order == BondOrder.None || bond.Order == BondOrder.Unknown)
        {
            return 0;
        }

        return 1;
    }
}

