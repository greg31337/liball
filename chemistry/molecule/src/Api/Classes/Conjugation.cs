using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides conjugation data and operations for molecular graphs.
/// </summary>
public class Conjugation : IConjugation
{
    /// <summary>
    /// Initializes a conjugated-system assignment service.
    /// </summary>
    public Conjugation()
    {
    }

    /// <summary>
    /// Assign.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public ConjugationResult Assign(Molecule? molecule)
    {
        ConjugationResult result = new ConjugationResult();

        if (molecule == null)
        {
            return result;
        }

        bool[] piCapableAtoms = new bool[molecule.Atoms.Count];
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            piCapableAtoms[atomIndex] = IsPiCapableAtom(molecule, atomIndex);
            atomIndex = atomIndex + 1;
        }

        bool[] visited = new bool[molecule.Atoms.Count];
        atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (piCapableAtoms[atomIndex] && !visited[atomIndex])
            {
                ConjugatedSystem system = BuildSystem(molecule, atomIndex, piCapableAtoms, visited);
                if (IsConjugatedSystem(molecule, system))
                {
                    system.Id = result.Systems.Count;
                    result.Systems.Add(system);
                    AddSystemToResult(result, system);
                }
            }

            atomIndex = atomIndex + 1;
        }

        return result;
    }

    internal ConjugatedSystem BuildSystem(Molecule molecule, int startAtomId, bool[] piCapableAtoms, bool[] visited)
    {
        ConjugatedSystem system = new ConjugatedSystem();
        List<int> queue = new List<int>();
        visited[startAtomId] = true;
        queue.Add(startAtomId);

        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            int atomId = queue[queueIndex];
            AddUniqueInt(system.AtomIds, atomId);

            int bondIndex = 0;
            while (bondIndex < molecule.Bonds.Count)
            {
                Bond bond = molecule.Bonds[bondIndex];
                int neighborAtomId = GetNeighborAtomId(bond, atomId);
                if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && piCapableAtoms[neighborAtomId] && IsConjugatingBond(bond))
                {
                    AddUniqueInt(system.BondIds, bond.Id);
                    if (!visited[neighborAtomId])
                    {
                        visited[neighborAtomId] = true;
                        queue.Add(neighborAtomId);
                    }
                }

                bondIndex = bondIndex + 1;
            }

            queueIndex = queueIndex + 1;
        }

        return system;
    }

    internal bool IsConjugatedSystem(Molecule molecule, ConjugatedSystem system)
    {
        if (system.AtomIds.Count >= 3)
        {
            return true;
        }

        int bondIndex = 0;
        while (bondIndex < system.BondIds.Count)
        {
            Bond bond = molecule.Bonds[system.BondIds[bondIndex]];
            if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
            {
                return true;
            }

            bondIndex = bondIndex + 1;
        }

        return false;
    }

    internal void AddSystemToResult(ConjugationResult result, ConjugatedSystem system)
    {
        int atomIndex = 0;
        while (atomIndex < system.AtomIds.Count)
        {
            AddUniqueInt(result.ConjugatedAtomIds, system.AtomIds[atomIndex]);
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < system.BondIds.Count)
        {
            AddUniqueInt(result.ConjugatedBondIds, system.BondIds[bondIndex]);
            bondIndex = bondIndex + 1;
        }
    }

    internal bool IsPiCapableAtom(Molecule molecule, int atomId)
    {
        if (!molecule.IsValidAtomId(atomId))
        {
            return false;
        }

        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber <= 1)
        {
            return false;
        }

        if (atom.IsAromatic)
        {
            return true;
        }

        if (CountPiBonds(molecule, atomId) > 0)
        {
            return true;
        }

        if (HasConjugatingLonePair(molecule, atomId))
        {
            return true;
        }

        return false;
    }

    internal bool HasConjugatingLonePair(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (!(atom.AtomicNumber == 7 || atom.AtomicNumber == 8 || atom.AtomicNumber == 15 || atom.AtomicNumber == 16))
        {
            return false;
        }

        if (atom.FormalCharge > 0)
        {
            return false;
        }

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, atomId);
            if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && bond.Order == BondOrder.Single)
            {
                if (AtomHasPiBondOrAromaticFlag(molecule, neighborAtomId))
                {
                    return true;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return false;
    }

    internal bool AtomHasPiBondOrAromaticFlag(Molecule molecule, int atomId)
    {
        if (molecule.Atoms[atomId].IsAromatic)
        {
            return true;
        }

        return CountPiBonds(molecule, atomId) > 0;
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

    internal bool IsConjugatingBond(Bond? bond)
    {
        if (bond == null)
        {
            return false;
        }

        if (bond.Order == BondOrder.Single || bond.Order == BondOrder.Double || bond.Order == BondOrder.Triple)
        {
            return true;
        }

        if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
        {
            return true;
        }

        return false;
    }

    internal int GetNeighborAtomId(Bond bond, int atomId)
    {
        if (bond.BeginAtomId == atomId)
        {
            return bond.EndAtomId;
        }

        if (bond.EndAtomId == atomId)
        {
            return bond.BeginAtomId;
        }

        return -1;
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

    internal void AddUniqueInt(List<int> values, int value)
    {
        if (!ContainsInt(values, value))
        {
            values.Add(value);
        }
    }
}

