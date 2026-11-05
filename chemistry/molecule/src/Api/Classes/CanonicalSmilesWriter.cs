using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular objects as canonical smiles data.
/// </summary>
public class CanonicalSmilesWriter : ICanonicalSmilesWriter
{
    /// <summary>
    /// Initializes a canonical SMILES writer.
    /// </summary>
    public CanonicalSmilesWriter()
    {
    }

    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Write(Molecule? molecule)
    {
        return WriteWithOptions(molecule, false);
    }

    /// <summary>
    /// Writes canonical SMILES including supported atom and bond stereochemistry.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteIsomeric(Molecule? molecule)
    {
        return WriteWithOptions(molecule, true);
    }

    /// <summary>
    /// Writes the canonical isomeric.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteCanonicalIsomeric(Molecule? molecule)
    {
        return WriteIsomeric(molecule);
    }

    /// <summary>
    /// Writes SMILES with explicit control over whether stereochemistry is included.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="includeStereo">The include stereo.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteWithOptions(Molecule? molecule, bool includeStereo)
    {
        return WriteConfigured(molecule, includeStereo, true, false);
    }

    internal string WriteConfigured(Molecule? molecule, bool includeStereo, bool includeAtomMaps, bool explicitHydrogens)
    {
        if (molecule == null || molecule.Atoms.Count == 0)
        {
            return "";
        }

        CanonicalOrder order = CanonicalOrder.FromMolecule(molecule);
        bool[] treeBonds = BuildTreeBondSet(molecule, order);
        bool[] visitedAtoms = new bool[molecule.Atoms.Count];
        int[] ringNumbersByBondId = new int[molecule.Bonds.Count];
        SmilesRingCounter counter = new SmilesRingCounter();
        StringBuilder builder = new StringBuilder();

        int canonicalAtomIndex = 0;
        bool wroteComponent = false;
        while (canonicalAtomIndex < order.AtomIdsByCanonicalIndex.Count)
        {
            int atomId = order.GetOriginalAtomId(canonicalAtomIndex);
            if (!visitedAtoms[atomId])
            {
                if (wroteComponent)
                {
                    builder.Append(".");
                }

                AppendAtomTreeWithStereo(builder, molecule, order, atomId, -1, visitedAtoms, treeBonds, ringNumbersByBondId, counter, includeStereo, includeAtomMaps, explicitHydrogens);
                wroteComponent = true;
            }

            canonicalAtomIndex = canonicalAtomIndex + 1;
        }

        return builder.ToString();
    }

    internal bool[] BuildTreeBondSet(Molecule molecule, CanonicalOrder order)
    {
        bool[] treeBonds = new bool[molecule.Bonds.Count];
        bool[] visitedAtoms = new bool[molecule.Atoms.Count];

        int canonicalAtomIndex = 0;
        while (canonicalAtomIndex < order.AtomIdsByCanonicalIndex.Count)
        {
            int atomId = order.GetOriginalAtomId(canonicalAtomIndex);
            if (!visitedAtoms[atomId])
            {
                MarkTreeBonds(molecule, order, atomId, visitedAtoms, treeBonds);
            }

            canonicalAtomIndex = canonicalAtomIndex + 1;
        }

        return treeBonds;
    }

    internal void MarkTreeBonds(Molecule molecule, CanonicalOrder order, int atomId, bool[] visitedAtoms, bool[] treeBonds)
    {
        visitedAtoms[atomId] = true;
        List<int> neighbors = GetSortedNeighborAtomIds(molecule, order, atomId);
        int index = 0;
        while (index < neighbors.Count)
        {
            int neighborAtomId = neighbors[index];
            if (!visitedAtoms[neighborAtomId])
            {
                int bondId = molecule.GetBondIdBetween(atomId, neighborAtomId);
                if (bondId >= 0 && bondId < treeBonds.Length)
                {
                    treeBonds[bondId] = true;
                }

                MarkTreeBonds(molecule, order, neighborAtomId, visitedAtoms, treeBonds);
            }

            index = index + 1;
        }
    }

    internal void AppendAtomTree(StringBuilder builder, Molecule molecule, CanonicalOrder order, int atomId, int parentAtomId, bool[] visitedAtoms, bool[] treeBonds, int[] ringNumbersByBondId, SmilesRingCounter counter)
    {
        AppendAtomTreeWithStereo(builder, molecule, order, atomId, parentAtomId, visitedAtoms, treeBonds, ringNumbersByBondId, counter, false, true, false);
    }

    internal void AppendAtomTreeWithStereo(StringBuilder builder, Molecule molecule, CanonicalOrder order, int atomId, int parentAtomId, bool[] visitedAtoms, bool[] treeBonds, int[] ringNumbersByBondId, SmilesRingCounter counter, bool includeStereo, bool includeAtomMaps, bool explicitHydrogens)
    {
        visitedAtoms[atomId] = true;
        builder.Append(GetConfiguredAtomTokenForMolecule(molecule, atomId, includeStereo, includeAtomMaps, explicitHydrogens));
        AppendRingClosuresWithStereo(builder, molecule, order, atomId, treeBonds, ringNumbersByBondId, counter, includeStereo);

        List<int> children = GetSortedTreeChildren(molecule, order, atomId, parentAtomId, visitedAtoms, treeBonds);
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            int childAtomId = children[childIndex];
            int bondId = molecule.GetBondIdBetween(atomId, childAtomId);
            Bond bond = molecule.Bonds[bondId];
            if (childIndex < children.Count - 1)
            {
                builder.Append("(");
                builder.Append(GetBondSymbolWithStereo(bond, includeStereo));
                AppendAtomTreeWithStereo(builder, molecule, order, childAtomId, atomId, visitedAtoms, treeBonds, ringNumbersByBondId, counter, includeStereo, includeAtomMaps, explicitHydrogens);
                builder.Append(")");
            }
            else
            {
                builder.Append(GetBondSymbolWithStereo(bond, includeStereo));
                AppendAtomTreeWithStereo(builder, molecule, order, childAtomId, atomId, visitedAtoms, treeBonds, ringNumbersByBondId, counter, includeStereo, includeAtomMaps, explicitHydrogens);
            }

            childIndex = childIndex + 1;
        }
    }

    internal void AppendRingClosures(StringBuilder builder, Molecule molecule, CanonicalOrder order, int atomId, bool[] treeBonds, int[] ringNumbersByBondId, SmilesRingCounter counter)
    {
        AppendRingClosuresWithStereo(builder, molecule, order, atomId, treeBonds, ringNumbersByBondId, counter, false);
    }

    internal void AppendRingClosuresWithStereo(StringBuilder builder, Molecule molecule, CanonicalOrder order, int atomId, bool[] treeBonds, int[] ringNumbersByBondId, SmilesRingCounter counter, bool includeStereo)
    {
        List<int> neighbors = GetSortedNeighborAtomIds(molecule, order, atomId);
        int index = 0;
        while (index < neighbors.Count)
        {
            int neighborAtomId = neighbors[index];
            int bondId = molecule.GetBondIdBetween(atomId, neighborAtomId);
            if (bondId >= 0 && bondId < treeBonds.Length && !treeBonds[bondId])
            {
                if (ringNumbersByBondId[bondId] == 0)
                {
                    ringNumbersByBondId[bondId] = counter.NextRingNumber;
                    counter.NextRingNumber = counter.NextRingNumber + 1;
                    builder.Append(GetBondSymbolWithStereo(molecule.Bonds[bondId], includeStereo));
                    AppendRingNumber(builder, ringNumbersByBondId[bondId]);
                }
                else
                {
                    AppendRingNumber(builder, ringNumbersByBondId[bondId]);
                }
            }

            index = index + 1;
        }
    }

    internal List<int> GetSortedTreeChildren(Molecule molecule, CanonicalOrder order, int atomId, int parentAtomId, bool[] visitedAtoms, bool[] treeBonds)
    {
        List<int> children = new List<int>();
        List<int> neighbors = GetSortedNeighborAtomIds(molecule, order, atomId);
        int index = 0;
        while (index < neighbors.Count)
        {
            int neighborAtomId = neighbors[index];
            int bondId = molecule.GetBondIdBetween(atomId, neighborAtomId);
            if (neighborAtomId != parentAtomId && !visitedAtoms[neighborAtomId] && bondId >= 0 && treeBonds[bondId])
            {
                children.Add(neighborAtomId);
            }

            index = index + 1;
        }

        return children;
    }

    internal List<int> GetSortedNeighborAtomIds(Molecule molecule, CanonicalOrder order, int atomId)
    {
        List<int> neighbors = new List<int>();
        List<int> source = molecule.GetNeighborAtomIds(atomId);
        int index = 0;
        while (index < source.Count)
        {
            neighbors.Add(source[index]);
            index = index + 1;
        }

        SortAtomIdsByCanonicalOrder(neighbors, order);
        return neighbors;
    }

    internal void SortAtomIdsByCanonicalOrder(List<int> atomIds, CanonicalOrder order)
    {
        int index = 1;
        while (index < atomIds.Count)
        {
            int value = atomIds[index];
            int scan = index - 1;
            while (scan >= 0 && CompareAtomIdsByCanonicalOrder(atomIds[scan], value, order) > 0)
            {
                atomIds[scan + 1] = atomIds[scan];
                scan = scan - 1;
            }

            atomIds[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareAtomIdsByCanonicalOrder(int leftAtomId, int rightAtomId, CanonicalOrder order)
    {
        int leftCanonical = order.GetCanonicalAtomIndex(leftAtomId);
        int rightCanonical = order.GetCanonicalAtomIndex(rightAtomId);
        if (leftCanonical != rightCanonical)
        {
            return leftCanonical - rightCanonical;
        }

        return leftAtomId - rightAtomId;
    }

    internal void AppendRingNumber(StringBuilder builder, int ringNumber)
    {
        if (ringNumber < 10)
        {
            builder.Append(ringNumber);
        }
        else
        {
            builder.Append("%");
            builder.Append(ringNumber);
        }
    }

    internal string GetBondSymbol(Bond? bond)
    {
        return GetBondSymbolWithStereo(bond, false);
    }

    internal string GetBondSymbolWithStereo(Bond? bond, bool includeStereo)
    {
        if (bond == null)
        {
            return "";
        }

        if (includeStereo && bond.Direction == BondDirection.Up && bond.Order == BondOrder.Single)
        {
            return "/";
        }

        if (includeStereo && bond.Direction == BondDirection.Down && bond.Order == BondOrder.Single)
        {
            return "\\";
        }

        if (includeStereo && bond.Direction == BondDirection.Either && bond.Order == BondOrder.Single)
        {
            return "/";
        }

        if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
        {
            return ":";
        }

        if (bond.Order == BondOrder.Double)
        {
            return "=";
        }

        if (bond.Order == BondOrder.Triple)
        {
            return "#";
        }

        if (bond.Order == BondOrder.Quadruple)
        {
            return "$";
        }

        if (bond.Order == BondOrder.Unknown || bond.Order == BondOrder.None)
        {
            return "~";
        }

        return "";
    }

    internal string GetAtomTokenForMolecule(Molecule molecule, int atomId, bool includeStereo)
    {
        return GetConfiguredAtomTokenForMolecule(molecule, atomId, includeStereo, true, false);
    }

    internal string GetConfiguredAtomTokenForMolecule(Molecule molecule, int atomId, bool includeStereo, bool includeAtomMaps, bool explicitHydrogens)
    {
        Chirality chirality = Chirality.None;
        if (includeStereo)
        {
            chirality = GetTetrahedralChirality(molecule, atomId);
        }

        return GetConfiguredAtomTokenWithChirality(molecule.Atoms[atomId], chirality, includeAtomMaps, explicitHydrogens);
    }

    internal string GetAtomToken(Atom atom)
    {
        return GetAtomTokenWithChirality(atom, Chirality.None);
    }

    internal string GetAtomTokenWithChirality(Atom atom, Chirality chirality)
    {
        return GetConfiguredAtomTokenWithChirality(atom, chirality, true, false);
    }

    internal string GetConfiguredAtomTokenWithChirality(Atom atom, Chirality chirality, bool includeAtomMaps, bool explicitHydrogens)
    {
        if (CanUseConfiguredBareAtom(atom, chirality, includeAtomMaps, explicitHydrogens))
        {
            return GetBareAtomSymbol(atom);
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("[");
        if (atom.IsotopeMassNumber > 0)
        {
            builder.Append(atom.IsotopeMassNumber);
        }

        builder.Append(GetBracketAtomSymbol(atom));
        AppendBracketChirality(builder, chirality);
        AppendBracketHydrogen(builder, atom);
        AppendBracketCharge(builder, atom);
        if (includeAtomMaps && atom.AtomMapNumber > 0)
        {
            builder.Append(":");
            builder.Append(atom.AtomMapNumber);
        }

        builder.Append("]");
        return builder.ToString();
    }

    internal bool CanUseBareAtom(Atom? atom)
    {
        return CanUseBareAtomWithChirality(atom, Chirality.None);
    }

    internal bool CanUseBareAtomWithChirality(Atom? atom, Chirality chirality)
    {
        return CanUseConfiguredBareAtom(atom, chirality, true, false);
    }

    internal bool CanUseConfiguredBareAtom(Atom? atom, Chirality chirality, bool includeAtomMaps, bool explicitHydrogens)
    {
        if (atom == null)
        {
            return false;
        }

        if (chirality == Chirality.Clockwise || chirality == Chirality.CounterClockwise)
        {
            return false;
        }

        if (atom.IsotopeMassNumber != 0 || atom.FormalCharge != 0 || atom.ExplicitHydrogenCount != 0 || atom.RadicalElectronCount != 0)
        {
            return false;
        }

        if (includeAtomMaps && atom.AtomMapNumber != 0)
        {
            return false;
        }

        if (explicitHydrogens && atom.ImplicitHydrogenCount > 0)
        {
            return false;
        }

        if (atom.QueryOperation != QueryOperation.None || atom.QueryValue != "")
        {
            return false;
        }

        return IsOrganicSubsetAtom(atom);
    }

    internal Chirality GetTetrahedralChirality(Molecule? molecule, int atomId)
    {
        if (molecule == null)
        {
            return Chirality.None;
        }

        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record.Kind == "tetrahedral" && record.CenterAtomId == atomId)
            {
                return record.Chirality;
            }

            index = index + 1;
        }

        return Chirality.None;
    }

    internal bool IsOrganicSubsetAtom(Atom atom)
    {
        if (atom.AtomicNumber == 5 || atom.AtomicNumber == 6 || atom.AtomicNumber == 7 || atom.AtomicNumber == 8 || atom.AtomicNumber == 9)
        {
            return true;
        }

        if (atom.AtomicNumber == 15 || atom.AtomicNumber == 16 || atom.AtomicNumber == 17 || atom.AtomicNumber == 35 || atom.AtomicNumber == 53)
        {
            return true;
        }

        return false;
    }

    internal string GetBareAtomSymbol(Atom atom)
    {
        if (atom.IsAromatic)
        {
            if (atom.AtomicNumber == 5)
            {
                return "b";
            }

            if (atom.AtomicNumber == 6)
            {
                return "c";
            }

            if (atom.AtomicNumber == 7)
            {
                return "n";
            }

            if (atom.AtomicNumber == 8)
            {
                return "o";
            }

            if (atom.AtomicNumber == 15)
            {
                return "p";
            }

            if (atom.AtomicNumber == 16)
            {
                return "s";
            }
        }

        return GetElementSymbol(atom.AtomicNumber);
    }

    internal string GetBracketAtomSymbol(Atom atom)
    {
        if (atom.IsAromatic)
        {
            return GetBareAtomSymbol(atom);
        }

        return GetElementSymbol(atom.AtomicNumber);
    }

    internal string GetElementSymbol(int atomicNumber)
    {
        if (atomicNumber == 5)
        {
            return "B";
        }

        if (atomicNumber == 6)
        {
            return "C";
        }

        if (atomicNumber == 7)
        {
            return "N";
        }

        if (atomicNumber == 8)
        {
            return "O";
        }

        if (atomicNumber == 9)
        {
            return "F";
        }

        if (atomicNumber == 15)
        {
            return "P";
        }

        if (atomicNumber == 16)
        {
            return "S";
        }

        if (atomicNumber == 17)
        {
            return "Cl";
        }

        if (atomicNumber == 35)
        {
            return "Br";
        }

        if (atomicNumber == 53)
        {
            return "I";
        }

        return "*";
    }

    internal void AppendBracketHydrogen(StringBuilder builder, Atom atom)
    {
        int hydrogenCount = atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
        if (hydrogenCount <= 0)
        {
            return;
        }

        builder.Append("H");
        if (hydrogenCount > 1)
        {
            builder.Append(hydrogenCount);
        }
    }

    internal void AppendBracketChirality(StringBuilder builder, Chirality chirality)
    {
        if (chirality == Chirality.Clockwise)
        {
            builder.Append("@");
        }

        if (chirality == Chirality.CounterClockwise)
        {
            builder.Append("@@");
        }
    }

    internal void AppendBracketCharge(StringBuilder builder, Atom atom)
    {
        if (atom.FormalCharge == 0)
        {
            return;
        }

        if (atom.FormalCharge > 0)
        {
            builder.Append("+");
            if (atom.FormalCharge > 1)
            {
                builder.Append(atom.FormalCharge);
            }
        }
        else
        {
            builder.Append("-");
            if (atom.FormalCharge < -1)
            {
                builder.Append(0 - atom.FormalCharge);
            }
        }
    }
}
