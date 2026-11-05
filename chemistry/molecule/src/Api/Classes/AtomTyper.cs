using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides atom typer data and operations for molecular graphs.
/// </summary>
public class AtomTyper : IAtomTyper
{
    internal ElementTable Elements;
    internal HydrogenCalculator Hydrogens;
    internal ValenceCalculator Valences;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomTyper"/> class.
    /// </summary>
    public AtomTyper()
    {
        Elements = new ElementTable();
        Hydrogens = new HydrogenCalculator();
        Valences = new ValenceCalculator();
    }

    /// <summary>
    /// Calculates hydrogen counts for one atom.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The calculated hydrogen counts.</returns>
    public HydrogenCalculationResult CalculateHydrogens(Molecule? molecule, int atomId)
    {
        return Hydrogens.CalculateAtomHydrogens(molecule, atomId);
    }

    /// <summary>
    /// Calculates valence information for one atom.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The calculated valence information.</returns>
    public ValenceResult CalculateValence(Molecule? molecule, int atomId)
    {
        return Valences.CalculateAtomValence(molecule, atomId);
    }

    /// <summary>
    /// Returns the radical state represented by an atom's radical electron count.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The radical state, or <see cref="RadicalState.None"/> when the atom is unavailable.</returns>
    public RadicalState GetRadicalState(Molecule? molecule, int atomId)
    {
        if (molecule == null || !molecule.IsValidAtomId(atomId))
        {
            return RadicalState.None;
        }

        int radicalElectronCount = molecule.Atoms[atomId].RadicalElectronCount;
        if (radicalElectronCount == 1)
        {
            return RadicalState.Doublet;
        }

        if (radicalElectronCount == 2)
        {
            return RadicalState.Triplet;
        }

        if (radicalElectronCount >= 3)
        {
            return RadicalState.Quartet;
        }

        return RadicalState.None;
    }

    /// <summary>
    /// Classifies one atom and reports its elemental and supported functional-group features.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public AtomTypeResult TypeAtom(Molecule? molecule, int atomId)
    {
        AtomTypeResult result = new AtomTypeResult();

        if (molecule == null)
        {
            return result;
        }

        if (!molecule.IsValidAtomId(atomId))
        {
            return result;
        }

        Atom atom = molecule.Atoms[atomId];
        Element element = Elements.GetElement(atom.AtomicNumber);
        result.AtomId = atomId;
        result.AtomicNumber = atom.AtomicNumber;
        result.Symbol = element.Symbol;
        result.IsOrganicSubset = element.IsOrganicSubset;
        result.IsMetal = IsMetal(atom.AtomicNumber);
        result.IsHalogen = IsHalogen(atom.AtomicNumber);
        result.IsHeteroAtom = atom.AtomicNumber > 1 && atom.AtomicNumber != 6;
        result.IsHydrogenBondDonor = IsHydrogenBondDonor(molecule, atomId);
        result.IsHydrogenBondAcceptor = IsHydrogenBondAcceptor(molecule, atomId);
        result.IsHydroxylOxygen = IsHydroxylOxygen(molecule, atomId);
        result.IsCarbonylCarbon = IsCarbonylCarbon(molecule, atomId);
        result.IsCarboxylCarbon = IsCarboxylCarbon(molecule, atomId);
        result.IsAmineNitrogen = IsAmineNitrogen(molecule, atomId);
        result.IsNitroNitrogen = IsNitroNitrogen(molecule, atomId);
        result.IsThiolSulfur = IsThiolSulfur(molecule, atomId);
        result.FunctionalGroup = GetPrimaryFunctionalGroup(result);
        return result;
    }

    /// <summary>
    /// Classifies every atom in identifier order.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<AtomTypeResult> TypeAtoms(Molecule? molecule)
    {
        List<AtomTypeResult> results = new List<AtomTypeResult>();

        if (molecule == null)
        {
            return results;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            results.Add(TypeAtom(molecule, atomIndex));
            atomIndex = atomIndex + 1;
        }

        return results;
    }

    /// <summary>
    /// Determines whether the value is metal.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsMetal(int atomicNumber)
    {
        return atomicNumber == 3
            || atomicNumber == 4
            || atomicNumber == 11
            || atomicNumber == 12
            || atomicNumber == 13
            || atomicNumber == 19
            || atomicNumber == 20
            || atomicNumber == 26
            || atomicNumber == 29
            || atomicNumber == 30;
    }

    /// <summary>
    /// Determines whether the value is halogen.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsHalogen(int atomicNumber)
    {
        return atomicNumber == 9 || atomicNumber == 17 || atomicNumber == 35 || atomicNumber == 53;
    }

    internal bool IsHydrogenBondDonor(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (!(atom.AtomicNumber == 7 || atom.AtomicNumber == 8 || atom.AtomicNumber == 16))
        {
            return false;
        }

        HydrogenCalculationResult hydrogens = Hydrogens.CalculateAtomHydrogens(molecule, atomId);
        return hydrogens.TotalHydrogenCount > 0;
    }

    internal bool IsHydrogenBondAcceptor(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        ValenceResult valence = Valences.CalculateAtomValence(molecule, atomId);

        if (atom.FormalCharge > 0)
        {
            return false;
        }

        if (atom.AtomicNumber == 8 && valence.TotalValence <= 2)
        {
            return true;
        }

        if (atom.AtomicNumber == 7 && valence.TotalValence <= 3)
        {
            return true;
        }

        if (atom.AtomicNumber == 16 && valence.TotalValence <= 2)
        {
            return true;
        }

        return false;
    }

    internal bool IsHydroxylOxygen(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber != 8)
        {
            return false;
        }

        HydrogenCalculationResult hydrogens = Hydrogens.CalculateAtomHydrogens(molecule, atomId);
        if (hydrogens.TotalHydrogenCount == 0)
        {
            return false;
        }

        return CountHeavyNeighbors(molecule, atomId) >= 1;
    }

    internal bool IsCarbonylCarbon(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber != 6)
        {
            return false;
        }

        return CountDoubleBondedAtoms(molecule, atomId, 8) + CountDoubleBondedAtoms(molecule, atomId, 16) > 0;
    }

    internal bool IsCarboxylCarbon(Molecule molecule, int atomId)
    {
        if (!IsCarbonylCarbon(molecule, atomId))
        {
            return false;
        }

        return CountSingleBondedAtoms(molecule, atomId, 8) > 0;
    }

    internal bool IsAmineNitrogen(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber != 7)
        {
            return false;
        }

        if (atom.FormalCharge > 1)
        {
            return false;
        }

        return CountHeavyNeighbors(molecule, atomId) > 0 || Hydrogens.CalculateAtomHydrogens(molecule, atomId).TotalHydrogenCount > 0;
    }

    internal bool IsNitroNitrogen(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber != 7)
        {
            return false;
        }

        int oxygenNeighborCount = CountBondedAtoms(molecule, atomId, 8);
        int doubleOxygenCount = CountDoubleBondedAtoms(molecule, atomId, 8);
        if (oxygenNeighborCount >= 2 && (atom.FormalCharge > 0 || doubleOxygenCount > 0))
        {
            return true;
        }

        return false;
    }

    internal bool IsThiolSulfur(Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber != 16)
        {
            return false;
        }

        HydrogenCalculationResult hydrogens = Hydrogens.CalculateAtomHydrogens(molecule, atomId);
        return hydrogens.TotalHydrogenCount > 0 && CountHeavyNeighbors(molecule, atomId) > 0;
    }

    internal int CountBondedAtoms(Molecule molecule, int atomId, int atomicNumber)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            int neighborAtomId = GetNeighborAtomId(molecule.Bonds[bondIndex], atomId);
            if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && molecule.Atoms[neighborAtomId].AtomicNumber == atomicNumber)
            {
                count = count + 1;
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal int CountSingleBondedAtoms(Molecule molecule, int atomId, int atomicNumber)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, atomId);
            if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && molecule.Atoms[neighborAtomId].AtomicNumber == atomicNumber && bond.Order == BondOrder.Single)
            {
                count = count + 1;
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal int CountDoubleBondedAtoms(Molecule molecule, int atomId, int atomicNumber)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetNeighborAtomId(bond, atomId);
            if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && molecule.Atoms[neighborAtomId].AtomicNumber == atomicNumber && bond.Order == BondOrder.Double)
            {
                count = count + 1;
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal int CountHeavyNeighbors(Molecule molecule, int atomId)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            int neighborAtomId = GetNeighborAtomId(molecule.Bonds[bondIndex], atomId);
            if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && molecule.Atoms[neighborAtomId].AtomicNumber > 1)
            {
                count = count + 1;
            }

            bondIndex = bondIndex + 1;
        }

        return count;
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

    internal string GetPrimaryFunctionalGroup(AtomTypeResult result)
    {
        if (result.IsNitroNitrogen)
        {
            return "nitro";
        }

        if (result.IsCarboxylCarbon)
        {
            return "carboxyl";
        }

        if (result.IsCarbonylCarbon)
        {
            return "carbonyl";
        }

        if (result.IsHydroxylOxygen)
        {
            return "hydroxyl";
        }

        if (result.IsAmineNitrogen)
        {
            return "amine";
        }

        if (result.IsThiolSulfur)
        {
            return "thiol";
        }

        return "";
    }
}
