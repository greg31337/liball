using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides metal disconnector data and operations for molecular graphs.
/// </summary>
public class MetalDisconnector : IMetalDisconnector
{
    internal AtomTyper AtomTyper;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetalDisconnector"/> class.
    /// </summary>
    public MetalDisconnector()
    {
        AtomTyper = new AtomTyper();
    }

    /// <summary>
    /// Returns a copy with supported bonds between metals and organic atoms removed.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Disconnect(Molecule? molecule)
    {
        MetalDisconnectionResult result = DisconnectWithResult(molecule);
        return result.Molecule;
    }

    /// <summary>
    /// Disconnects the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public MetalDisconnectionResult DisconnectWithResult(Molecule? molecule)
    {
        MetalDisconnectionResult result = new MetalDisconnectionResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("METAL_DISCONNECTOR_NULL_MOLECULE", "Cannot disconnect metals for a null molecule.");
            return result;
        }

        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);
        List<Bond> keptBonds = new List<Bond>();

        int bondIndex = 0;
        while (bondIndex < result.Molecule.Bonds.Count)
        {
            Bond bond = result.Molecule.Bonds[bondIndex];
            if (IsDisconnectableMetalBond(result.Molecule, bond))
            {
                DisconnectBond(result.Molecule, bond, result);
            }
            else
            {
                keptBonds.Add(bond);
            }

            bondIndex = bondIndex + 1;
        }

        if (result.DisconnectionCount > 0)
        {
            ReplaceBonds(result.Molecule, keptBonds);
            result.Changed = true;
            result.Molecule.ClearCaches();
        }

        return result;
    }

    internal bool IsDisconnectableMetalBond(Molecule molecule, Bond bond)
    {
        int beginAtomicNumber = molecule.Atoms[bond.BeginAtomId].AtomicNumber;
        int endAtomicNumber = molecule.Atoms[bond.EndAtomId].AtomicNumber;
        bool beginMetal = IsMetal(beginAtomicNumber);
        bool endMetal = IsMetal(endAtomicNumber);

        if (beginMetal == endMetal)
        {
            return false;
        }

        if (beginAtomicNumber == 1 || endAtomicNumber == 1)
        {
            return false;
        }

        return true;
    }

    internal void DisconnectBond(Molecule molecule, Bond bond, MetalDisconnectionResult result)
    {
        int metalAtomId = GetMetalAtomId(molecule, bond);
        int ligandAtomId = GetLigandAtomId(molecule, bond);
        if (!IsDativeBond(bond))
        {
            int charge = GetBondChargeIncrement(bond);
            molecule.Atoms[metalAtomId].FormalCharge = molecule.Atoms[metalAtomId].FormalCharge + charge;
            molecule.Atoms[ligandAtomId].FormalCharge = molecule.Atoms[ligandAtomId].FormalCharge - charge;
        }

        result.RemovedBondIds.Add(bond.Id);
        result.MetalAtomIds.Add(metalAtomId);
        result.LigandAtomIds.Add(ligandAtomId);
        result.DisconnectionCount = result.DisconnectionCount + 1;
        result.Report.AddInfo("METAL_BOND_DISCONNECTED", metalAtomId.ToString() + "-" + ligandAtomId.ToString());
    }

    internal void ReplaceBonds(Molecule molecule, List<Bond> keptBonds)
    {
        molecule.Bonds.Clear();
        int index = 0;
        while (index < keptBonds.Count)
        {
            Bond bond = keptBonds[index];
            bond.Id = index;
            molecule.Bonds.Add(bond);
            index = index + 1;
        }
    }

    internal int GetMetalAtomId(Molecule molecule, Bond bond)
    {
        if (IsMetal(molecule.Atoms[bond.BeginAtomId].AtomicNumber))
        {
            return bond.BeginAtomId;
        }

        return bond.EndAtomId;
    }

    internal int GetLigandAtomId(Molecule molecule, Bond bond)
    {
        if (IsMetal(molecule.Atoms[bond.BeginAtomId].AtomicNumber))
        {
            return bond.EndAtomId;
        }

        return bond.BeginAtomId;
    }

    internal int GetBondChargeIncrement(Bond bond)
    {
        if (bond.Order == BondOrder.Double)
        {
            return 2;
        }

        if (bond.Order == BondOrder.Triple)
        {
            return 3;
        }

        return 1;
    }

    internal bool IsDativeBond(Bond bond)
    {
        return bond.IsDative || bond.Order == BondOrder.Dative;
    }

    internal bool IsMetal(int atomicNumber)
    {
        return AtomTyper.IsMetal(atomicNumber);
    }
}

