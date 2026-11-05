using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides hybridization data and operations for molecular graphs.
/// </summary>
public class HybridizationModel : IHybridizationModel
{
    internal Conjugation Conjugation;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridizationModel"/> class.
    /// </summary>
    public HybridizationModel()
    {
        Conjugation = new Conjugation();
    }

    /// <summary>
    /// Assign.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting hybridization assignment.</returns>
    public HybridizationAssignment Assign(Molecule? molecule)
    {
        HybridizationAssignment assignment = new HybridizationAssignment();

        if (molecule == null)
        {
            return assignment;
        }

        ConjugationResult conjugation = Conjugation.Assign(molecule);
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            assignment.Atoms.Add(AssignAtom(molecule, atomIndex, conjugation));
            atomIndex = atomIndex + 1;
        }

        return assignment;
    }

    internal HybridizationResult AssignAtom(Molecule? molecule, int atomId, ConjugationResult? conjugation)
    {
        HybridizationResult result = new HybridizationResult();

        if (molecule == null || !molecule.IsValidAtomId(atomId))
        {
            return result;
        }

        Atom atom = molecule.Atoms[atomId];
        result.AtomId = atomId;
        result.AtomicNumber = atom.AtomicNumber;
        result.SigmaBondCount = CountSigmaBonds(molecule, atomId) + atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
        result.PiBondCount = CountPiBonds(molecule, atomId);
        result.StericNumber = result.SigmaBondCount;
        result.IsConjugated = conjugation != null && conjugation.AtomIsConjugated(atomId);

        if (atom.AtomicNumber == 0)
        {
            result.Hybridization = Hybridization.Unknown;
            result.Reason = "unknown atom";
            return result;
        }

        if (atom.AtomicNumber == 1)
        {
            result.Hybridization = Hybridization.S;
            result.Reason = "hydrogen";
            return result;
        }

        if (HasTripleBond(molecule, atomId))
        {
            result.Hybridization = Hybridization.Sp;
            result.Reason = "triple bond";
            return result;
        }

        if (CountDoubleBonds(molecule, atomId) >= 2)
        {
            result.Hybridization = Hybridization.Sp;
            result.Reason = "cumulated double bonds";
            return result;
        }

        if (atom.IsAromatic || HasAromaticBond(molecule, atomId))
        {
            result.Hybridization = Hybridization.Sp2;
            result.Reason = "aromatic";
            return result;
        }

        if (CountDoubleBonds(molecule, atomId) > 0)
        {
            result.Hybridization = Hybridization.Sp2;
            result.Reason = "double bond";
            return result;
        }

        if (result.IsConjugated && CanUseConjugatedPOrbital(atom.AtomicNumber))
        {
            result.Hybridization = Hybridization.Sp2;
            result.Reason = "conjugated";
            return result;
        }

        if (result.SigmaBondCount >= 6)
        {
            result.Hybridization = Hybridization.Sp3d2;
            result.Reason = "six sigma domains";
            return result;
        }

        if (result.SigmaBondCount == 5)
        {
            result.Hybridization = Hybridization.Sp3d;
            result.Reason = "five sigma domains";
            return result;
        }

        result.Hybridization = Hybridization.Sp3;
        result.Reason = "single-bonded atom";
        return result;
    }

    internal bool CanUseConjugatedPOrbital(int atomicNumber)
    {
        if (atomicNumber == 5 || atomicNumber == 6 || atomicNumber == 7 || atomicNumber == 8 || atomicNumber == 15 || atomicNumber == 16)
        {
            return true;
        }

        return false;
    }

    internal int CountSigmaBonds(Molecule molecule, int atomId)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                if (bond.Order != BondOrder.None && bond.Order != BondOrder.Unknown)
                {
                    count = count + 1;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal int CountPiBonds(Molecule molecule, int atomId)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                if (bond.IsAromatic || bond.Order == BondOrder.Aromatic || bond.Order == BondOrder.Double || bond.Order == BondOrder.Triple)
                {
                    count = count + 1;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal int CountDoubleBonds(Molecule molecule, int atomId)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if ((bond.BeginAtomId == atomId || bond.EndAtomId == atomId) && bond.Order == BondOrder.Double)
            {
                count = count + 1;
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal bool HasTripleBond(Molecule molecule, int atomId)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if ((bond.BeginAtomId == atomId || bond.EndAtomId == atomId) && bond.Order == BondOrder.Triple)
            {
                return true;
            }

            bondIndex = bondIndex + 1;
        }

        return false;
    }

    internal bool HasAromaticBond(Molecule molecule, int atomId)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
                {
                    return true;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return false;
    }
}

