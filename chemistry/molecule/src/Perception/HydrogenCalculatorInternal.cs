namespace molecule;


/// <summary>
/// Calculates hydrogen information from molecular graphs.
/// </summary>
internal class HydrogenCalculator
{
    internal ValenceCalculator ValenceCalculator;
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="HydrogenCalculator"/> class.
    /// </summary>
    public HydrogenCalculator()
    {
        ValenceCalculator = new ValenceCalculator();
        Elements = new ElementTable();
    }

    internal HydrogenCalculationResult CalculateAtomHydrogens(Molecule? molecule, int atomId)
    {
        HydrogenCalculationResult result = new HydrogenCalculationResult();

        if (molecule == null)
        {
            return result;
        }

        if (!molecule.IsValidAtomId(atomId))
        {
            return result;
        }

        Atom atom = molecule.Atoms[atomId];
        result.IsValidAtom = true;
        result.AtomId = atomId;
        result.AtomicNumber = atom.AtomicNumber;
        result.ExistingImplicitHydrogenCount = atom.ImplicitHydrogenCount;
        result.SpecExplicitHydrogenCount = atom.ExplicitHydrogenCount;
        result.GraphExplicitHydrogenCount = CountExplicitHydrogenNeighbors(molecule, atomId);
        result.IsKnownElement = Elements.IsKnownElement(atom.AtomicNumber);

        if (!result.IsKnownElement)
        {
            return result;
        }

        if (atom.AtomicNumber == 1)
        {
            result.BondValence = CalculateBondValence(molecule, atomId);
            result.TargetValence = 1;
            result.TotalHydrogenCount = result.GraphExplicitHydrogenCount;
            result.IsValidValence = result.BondValence + atom.ExplicitHydrogenCount <= result.TargetValence && atom.ExplicitHydrogenCount >= 0;
            return result;
        }

        result.BondValence = CalculateBondValence(molecule, atomId);
        result.TargetValence = GetTargetValence(atom, result.BondValence);
        result.CalculatedImplicitHydrogenCount = result.TargetValence - result.BondValence - atom.ExplicitHydrogenCount;

        if (result.CalculatedImplicitHydrogenCount < 0)
        {
            result.CalculatedImplicitHydrogenCount = 0;
        }

        result.TotalHydrogenCount = result.SpecExplicitHydrogenCount + result.GraphExplicitHydrogenCount + result.CalculatedImplicitHydrogenCount;
        result.IsValidValence = result.BondValence + result.SpecExplicitHydrogenCount <= result.TargetValence && result.SpecExplicitHydrogenCount >= 0;
        return result;
    }

    internal int CalculateImplicitHydrogenCount(Molecule molecule, int atomId)
    {
        return CalculateAtomHydrogens(molecule, atomId).CalculatedImplicitHydrogenCount;
    }

    internal int CountExplicitHydrogenNeighbors(Molecule? molecule, int atomId)
    {
        if (molecule == null)
        {
            return 0;
        }

        if (!molecule.IsValidAtomId(atomId))
        {
            return 0;
        }

        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = -1;

            if (bond.BeginAtomId == atomId)
            {
                neighborAtomId = bond.EndAtomId;
            }

            if (bond.EndAtomId == atomId)
            {
                neighborAtomId = bond.BeginAtomId;
            }

            if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && molecule.Atoms[neighborAtomId].AtomicNumber == 1)
            {
                count = count + 1;
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal int CalculateBondValence(Molecule molecule, int atomId)
    {
        int valence = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                valence = valence + ValenceCalculator.GetBondValence(bond);
            }

            bondIndex = bondIndex + 1;
        }

        return valence;
    }

    internal int GetTargetValence(Atom atom, int bondValence)
    {
        if (atom.AtomicNumber == 7 && atom.FormalCharge == 1)
        {
            return 4;
        }

        if (atom.AtomicNumber == 8 && atom.FormalCharge == -1)
        {
            return 1;
        }

        if (atom.AtomicNumber == 5 && atom.FormalCharge == -1)
        {
            return 4;
        }

        if (atom.AtomicNumber == 6 && atom.FormalCharge != 0)
        {
            return 3;
        }

        if ((atom.AtomicNumber == 9 || atom.AtomicNumber == 17 || atom.AtomicNumber == 35 || atom.AtomicNumber == 53) && atom.FormalCharge == -1)
        {
            return 0;
        }

        if (atom.IsAromatic)
        {
            return GetAromaticTargetValence(atom);
        }

        int currentValence = bondValence + atom.ExplicitHydrogenCount;
        int radicalAdjustedValence = currentValence + atom.RadicalElectronCount;
        int bestValence = 0;
        int maximumValence = 0;
        int index = 0;
        Element element = Elements.GetElement(atom.AtomicNumber);

        while (index < element.CommonValences.Count)
        {
            int valence = element.CommonValences[index];
            if (valence > maximumValence)
            {
                maximumValence = valence;
            }

            if (valence >= radicalAdjustedValence && (bestValence == 0 || valence < bestValence))
            {
                bestValence = valence;
            }

            index = index + 1;
        }

        if (bestValence == 0)
        {
            bestValence = maximumValence;
        }

        if (atom.RadicalElectronCount > 0)
        {
            bestValence = bestValence - atom.RadicalElectronCount;
        }

        if (bestValence < 0)
        {
            bestValence = 0;
        }

        return bestValence;
    }

    internal int GetAromaticTargetValence(Atom atom)
    {
        if (atom.AtomicNumber == 6)
        {
            return 3;
        }

        if (atom.AtomicNumber == 7)
        {
            return 3;
        }

        if (atom.AtomicNumber == 8)
        {
            return 2;
        }

        if (atom.AtomicNumber == 15)
        {
            return 3;
        }

        if (atom.AtomicNumber == 16)
        {
            return 2;
        }

        return GetTargetValenceWithoutAromatic(atom);
    }

    internal int GetTargetValenceWithoutAromatic(Atom atom)
    {
        bool original = atom.IsAromatic;
        atom.IsAromatic = false;
        int target = GetTargetValence(atom, 0);
        atom.IsAromatic = original;
        return target;
    }

    internal void AssignImplicitHydrogens(Molecule? molecule)
    {
        if (molecule == null)
        {
            return;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            molecule.Atoms[atomIndex].ImplicitHydrogenCount = CalculateImplicitHydrogenCount(molecule, atomIndex);
            atomIndex = atomIndex + 1;
        }
    }

    internal int AddHydrogens(Molecule? molecule)
    {
        if (molecule == null)
        {
            return 0;
        }

        int addedCount = 0;
        int originalAtomCount = molecule.Atoms.Count;
        int atomIndex = 0;

        while (atomIndex < originalAtomCount)
        {
            Atom atom = molecule.Atoms[atomIndex];
            int hydrogenCount = atom.ImplicitHydrogenCount;

            if (hydrogenCount > 0 && atom.AtomicNumber != 1)
            {
                int hydrogenIndex = 0;
                while (hydrogenIndex < hydrogenCount)
                {
                    int hydrogenAtomId = molecule.Atoms.Count;
                    Atom hydrogen = Atom.CreateWithAtomicNumber(hydrogenAtomId, 1);
                    hydrogen.ResidueId = atom.ResidueId;
                    hydrogen.ResidueName = atom.ResidueName;
                    hydrogen.ChainId = atom.ChainId;
                    hydrogen.ModelId = atom.ModelId;
                    hydrogen.InsertionCode = atom.InsertionCode;
                    molecule.Atoms.Add(hydrogen);

                    int bondId = molecule.Bonds.Count;
                    Bond bond = Bond.CreateWithOrder(bondId, atomIndex, hydrogenAtomId, BondOrder.Single);
                    molecule.Bonds.Add(bond);
                    AttachHydrogenToAssociatedData(molecule, atomIndex, hydrogenAtomId);

                    addedCount = addedCount + 1;
                    hydrogenIndex = hydrogenIndex + 1;
                }

                atom.ImplicitHydrogenCount = 0;
            }

            atomIndex = atomIndex + 1;
        }

        if (addedCount > 0)
        {
            molecule.EnsureConformerCoordinateCounts();
            molecule.ClearCaches();
        }

        return addedCount;
    }

    internal void AttachHydrogenToAssociatedData(Molecule molecule, int parentAtomId, int hydrogenAtomId)
    {
        int residueIndex = 0;
        while (residueIndex < molecule.Residues.Count)
        {
            if (ContainsAtomId(molecule.Residues[residueIndex].AtomIds, parentAtomId))
            {
                molecule.Residues[residueIndex].AtomIds.Add(hydrogenAtomId);
            }

            residueIndex = residueIndex + 1;
        }

        int modelIndex = 0;
        while (modelIndex < molecule.Models.Count)
        {
            if (ContainsAtomId(molecule.Models[modelIndex].AtomIds, parentAtomId))
            {
                molecule.Models[modelIndex].AtomIds.Add(hydrogenAtomId);
            }

            modelIndex = modelIndex + 1;
        }
    }

    internal bool ContainsAtomId(System.Collections.Generic.List<int> atomIds, int atomId)
    {
        int index = 0;
        while (index < atomIds.Count)
        {
            if (atomIds[index] == atomId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int AddCalculatedHydrogens(Molecule molecule)
    {
        AssignImplicitHydrogens(molecule);
        return AddHydrogens(molecule);
    }

    internal int RemoveHydrogens(Molecule? molecule, bool preserveIsotopicHydrogens)
    {
        if (molecule == null)
        {
            return 0;
        }

        int removedCount = 0;
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.Molecule = molecule;

        int atomIndex = molecule.Atoms.Count - 1;
        while (atomIndex >= 0)
        {
            if (CanRemoveHydrogen(molecule, atomIndex, preserveIsotopicHydrogens))
            {
                int neighborAtomId = GetSingleHeavyAtomNeighbor(molecule, atomIndex);
                if (neighborAtomId >= 0)
                {
                    molecule.Atoms[neighborAtomId].ImplicitHydrogenCount = molecule.Atoms[neighborAtomId].ImplicitHydrogenCount + 1;
                }

                builder.RemoveAtom(atomIndex);
                removedCount = removedCount + 1;
            }

            atomIndex = atomIndex - 1;
        }

        if (removedCount > 0)
        {
            molecule.ClearCaches();
        }

        return removedCount;
    }

    internal bool CanRemoveHydrogen(Molecule? molecule, int atomId, bool preserveIsotopicHydrogens)
    {
        if (molecule == null)
        {
            return false;
        }

        if (!molecule.IsValidAtomId(atomId))
        {
            return false;
        }

        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber != 1)
        {
            return false;
        }

        if (preserveIsotopicHydrogens && atom.IsotopeMassNumber > 0)
        {
            return false;
        }

        if (atom.FormalCharge != 0 || atom.RadicalElectronCount != 0)
        {
            return false;
        }

        return GetSingleHeavyAtomNeighbor(molecule, atomId) >= 0;
    }

    internal int GetSingleHeavyAtomNeighbor(Molecule molecule, int atomId)
    {
        int neighborAtomId = -1;
        int neighborCount = 0;
        int bondIndex = 0;

        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int candidateAtomId = -1;

            if (bond.BeginAtomId == atomId)
            {
                candidateAtomId = bond.EndAtomId;
            }

            if (bond.EndAtomId == atomId)
            {
                candidateAtomId = bond.BeginAtomId;
            }

            if (candidateAtomId >= 0 && molecule.IsValidAtomId(candidateAtomId) && molecule.Atoms[candidateAtomId].AtomicNumber > 1)
            {
                neighborAtomId = candidateAtomId;
                neighborCount = neighborCount + 1;
            }

            bondIndex = bondIndex + 1;
        }

        if (neighborCount == 1)
        {
            return neighborAtomId;
        }

        return -1;
    }
}
