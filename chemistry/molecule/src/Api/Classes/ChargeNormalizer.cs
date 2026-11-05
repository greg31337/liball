using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides charge normalizer data and operations for molecular graphs.
/// </summary>
public class ChargeNormalizer : IChargeNormalizer
{
    internal NormalizationRules Rules;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChargeNormalizer"/> class.
    /// </summary>
    public ChargeNormalizer()
    {
        Rules = new NormalizationRules();
    }

    /// <summary>
    /// Returns a normalized copy and leaves the input molecule unchanged.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Normalize(Molecule? molecule)
    {
        ChargeNormalizationResult result = NormalizeWithResult(molecule);
        return result.Molecule;
    }

    /// <summary>
    /// Normalizes the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public ChargeNormalizationResult NormalizeWithResult(Molecule? molecule)
    {
        ChargeNormalizationResult result = new ChargeNormalizationResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("CHARGE_NORMALIZER_NULL_MOLECULE", "Cannot normalize charges for a null molecule.");
            return result;
        }

        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);
        List<ChargeNormalizationRule> rules = Rules.GetOrderedChargeRules();
        int index = 0;
        while (index < rules.Count)
        {
            ApplyRule(result.Molecule, rules[index], result);
            index = index + 1;
        }

        if (result.AppliedRuleCount > 0)
        {
            result.Changed = true;
            result.Molecule.ClearCaches();
        }

        return result;
    }

    internal void ApplyRule(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        if (rule.Name == "nitro")
        {
            ApplyNitro(molecule, rule, result);
        }
        else if (rule.Name == "n-oxide")
        {
            ApplyNOxide(molecule, rule, result);
        }
        else if (rule.Name == "sulfoxide")
        {
            ApplySulfoxide(molecule, rule, result);
        }
        else if (rule.Name == "phosphate")
        {
            ApplyPhosphate(molecule, rule, result);
        }
        else if (rule.Name == "carboxylate")
        {
            ApplyCarboxylate(molecule, rule, result);
        }
        else if (rule.Name == "azide")
        {
            ApplyAzide(molecule, rule, result);
        }
        else if (rule.Name == "diazo")
        {
            ApplyDiazo(molecule, rule, result);
        }
        else if (rule.Name == "amino-acid-zwitterion")
        {
            ApplyAminoAcidZwitterion(molecule, rule, result);
        }
    }

    internal void ApplyNitro(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom nitrogen = molecule.Atoms[atomIndex];
            if (nitrogen.AtomicNumber == 7 && nitrogen.FormalCharge == 0)
            {
                Bond? firstOxygenBond = FindNitroOxygenBond(molecule, nitrogen.Id, -1);
                if (firstOxygenBond != null)
                {
                    Bond? secondOxygenBond = FindNitroOxygenBond(molecule, nitrogen.Id, GetOtherAtomId(firstOxygenBond, nitrogen.Id));
                    if (secondOxygenBond != null)
                    {
                        Bond negativeBond = SelectNitroNegativeBond(molecule, nitrogen.Id, firstOxygenBond, secondOxygenBond);
                        int negativeOxygenId = GetOtherAtomId(negativeBond, nitrogen.Id);
                        nitrogen.FormalCharge = 1;
                        molecule.Atoms[negativeOxygenId].FormalCharge = -1;
                        negativeBond.Order = BondOrder.Single;
                        RegisterApplied(result, rule.Name);
                    }
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal Bond? FindNitroOxygenBond(Molecule molecule, int nitrogenId, int excludedOxygenId)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int oxygenId = GetOtherAtomId(bond, nitrogenId);
            if (oxygenId >= 0 && oxygenId != excludedOxygenId && molecule.Atoms[oxygenId].AtomicNumber == 8 && molecule.Atoms[oxygenId].FormalCharge == 0)
            {
                if (bond.Order == BondOrder.Double || bond.Order == BondOrder.Single)
                {
                    return bond;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal Bond SelectNitroNegativeBond(Molecule molecule, int nitrogenId, Bond first, Bond second)
    {
        if (first.Order == BondOrder.Single && second.Order != BondOrder.Single)
        {
            return first;
        }

        if (second.Order == BondOrder.Single && first.Order != BondOrder.Single)
        {
            return second;
        }

        int firstOxygenId = GetOtherAtomId(first, nitrogenId);
        int secondOxygenId = GetOtherAtomId(second, nitrogenId);
        if (firstOxygenId > secondOxygenId)
        {
            return first;
        }

        return second;
    }

    internal void ApplyNOxide(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int nitrogenId = GetAtomIdForAtomicNumber(molecule, bond, 7);
            int oxygenId = GetAtomIdForAtomicNumber(molecule, bond, 8);
            if (nitrogenId >= 0 && oxygenId >= 0 && bond.Order == BondOrder.Single)
            {
                Atom nitrogen = molecule.Atoms[nitrogenId];
                Atom oxygen = molecule.Atoms[oxygenId];
                if (nitrogen.FormalCharge == 0 && oxygen.FormalCharge == 0 && !HasDoubleBondToOxygen(molecule, nitrogenId))
                {
                    nitrogen.FormalCharge = 1;
                    oxygen.FormalCharge = -1;
                    RegisterApplied(result, rule.Name);
                }
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void ApplySulfoxide(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom sulfur = molecule.Atoms[atomIndex];
            if (sulfur.AtomicNumber == 16 && sulfur.FormalCharge == 0)
            {
                Bond? oxygenBond = FindOnlyDoubleOxygenBond(molecule, sulfur.Id);
                if (oxygenBond != null)
                {
                    int oxygenId = GetOtherAtomId(oxygenBond, sulfur.Id);
                    if (molecule.Atoms[oxygenId].FormalCharge == 0)
                    {
                        sulfur.FormalCharge = 1;
                        molecule.Atoms[oxygenId].FormalCharge = -1;
                        oxygenBond.Order = BondOrder.Single;
                        RegisterApplied(result, rule.Name);
                    }
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void ApplyPhosphate(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom phosphorus = molecule.Atoms[atomIndex];
            if (phosphorus.AtomicNumber == 15)
            {
                int oxygenCount = CountElementNeighbors(molecule, phosphorus.Id, 8);
                int doubleOxygenCount = CountElementNeighborsWithOrder(molecule, phosphorus.Id, 8, BondOrder.Double);
                bool changed = false;
                if (oxygenCount >= 3 && doubleOxygenCount == 1)
                {
                    int bondIndex = 0;
                    while (bondIndex < molecule.Bonds.Count)
                    {
                        Bond bond = molecule.Bonds[bondIndex];
                        int oxygenId = GetOtherAtomId(bond, phosphorus.Id);
                        if (oxygenId >= 0 && molecule.Atoms[oxygenId].AtomicNumber == 8 && bond.Order == BondOrder.Single)
                        {
                            Atom oxygen = molecule.Atoms[oxygenId];
                            if (oxygen.FormalCharge == 0 && oxygen.ExplicitHydrogenCount == 0 && oxygen.ImplicitHydrogenCount == 0 && CountBondsForAtom(molecule, oxygenId) == 1)
                            {
                                oxygen.FormalCharge = -1;
                                changed = true;
                            }
                        }

                        bondIndex = bondIndex + 1;
                    }
                }

                if (changed)
                {
                    RegisterApplied(result, rule.Name);
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void ApplyCarboxylate(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom carbon = molecule.Atoms[atomIndex];
            if (carbon.AtomicNumber == 6)
            {
                Bond? negativeBond = FindChargedOxygenBond(molecule, carbon.Id, -1);
                Bond? neutralBond = FindChargedOxygenBond(molecule, carbon.Id, 0);
                if (negativeBond != null && neutralBond != null && negativeBond.Order == BondOrder.Double && neutralBond.Order == BondOrder.Single)
                {
                    negativeBond.Order = BondOrder.Single;
                    neutralBond.Order = BondOrder.Double;
                    RegisterApplied(result, rule.Name);
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void ApplyAzide(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom center = molecule.Atoms[atomIndex];
            if (center.AtomicNumber == 7 && center.FormalCharge == 0)
            {
                Bond? first = FindElementBondWithOrder(molecule, center.Id, 7, BondOrder.Double, -1);
                if (first != null)
                {
                    int firstTerminalId = GetOtherAtomId(first, center.Id);
                    Bond? second = FindElementBondWithOrder(molecule, center.Id, 7, BondOrder.Double, firstTerminalId);
                    if (second != null)
                    {
                        int secondTerminalId = GetOtherAtomId(second, center.Id);
                        Atom firstTerminal = molecule.Atoms[firstTerminalId];
                        Atom secondTerminal = molecule.Atoms[secondTerminalId];
                        if (firstTerminal.FormalCharge == 0 && secondTerminal.FormalCharge == 0 && CountBondsForAtom(molecule, center.Id) == 2)
                        {
                            int negativeId = firstTerminalId;
                            if (secondTerminalId < negativeId)
                            {
                                negativeId = secondTerminalId;
                            }

                            center.FormalCharge = 1;
                            molecule.Atoms[negativeId].FormalCharge = -1;
                            RegisterApplied(result, rule.Name);
                        }
                    }
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void ApplyDiazo(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom center = molecule.Atoms[atomIndex];
            if (center.AtomicNumber == 7 && center.FormalCharge == 0 && CountBondsForAtom(molecule, center.Id) == 2)
            {
                Bond? carbonBond = FindElementBondWithOrder(molecule, center.Id, 6, BondOrder.Double, -1);
                Bond? nitrogenBond = FindElementBondWithOrder(molecule, center.Id, 7, BondOrder.Double, -1);
                if (carbonBond != null && nitrogenBond != null)
                {
                    int carbonId = GetOtherAtomId(carbonBond, center.Id);
                    int nitrogenId = GetOtherAtomId(nitrogenBond, center.Id);
                    if (molecule.Atoms[carbonId].FormalCharge == 0 && molecule.Atoms[nitrogenId].FormalCharge == 0)
                    {
                        center.FormalCharge = 1;
                        molecule.Atoms[carbonId].FormalCharge = -1;
                        RegisterApplied(result, rule.Name);
                    }
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void ApplyAminoAcidZwitterion(Molecule molecule, ChargeNormalizationRule rule, ChargeNormalizationResult result)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom carbonylCarbon = molecule.Atoms[atomIndex];
            if (carbonylCarbon.AtomicNumber == 6)
            {
                Bond? carbonylOxygenBond = FindElementBondWithOrder(molecule, carbonylCarbon.Id, 8, BondOrder.Double, -1);
                Bond? acidOxygenBond = FindProtonatedOxygenBond(molecule, carbonylCarbon.Id);
                int nitrogenId = FindAminoAcidNitrogen(molecule, carbonylCarbon.Id);
                if (carbonylOxygenBond != null && acidOxygenBond != null && nitrogenId >= 0)
                {
                    int oxygenId = GetOtherAtomId(acidOxygenBond, carbonylCarbon.Id);
                    Atom oxygen = molecule.Atoms[oxygenId];
                    Atom nitrogen = molecule.Atoms[nitrogenId];
                    if (oxygen.FormalCharge == 0 && nitrogen.FormalCharge == 0)
                    {
                        RemoveOneHydrogenCount(oxygen);
                        nitrogen.ImplicitHydrogenCount = nitrogen.ImplicitHydrogenCount + 1;
                        oxygen.FormalCharge = -1;
                        nitrogen.FormalCharge = 1;
                        RegisterApplied(result, rule.Name);
                    }
                }
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal Bond? FindChargedOxygenBond(Molecule molecule, int atomId, int charge)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int oxygenId = GetOtherAtomId(bond, atomId);
            if (oxygenId >= 0 && molecule.Atoms[oxygenId].AtomicNumber == 8 && molecule.Atoms[oxygenId].FormalCharge == charge)
            {
                return bond;
            }

            index = index + 1;
        }

        return null;
    }

    internal Bond? FindElementBondWithOrder(Molecule molecule, int atomId, int atomicNumber, BondOrder order, int excludedAtomId)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int otherId = GetOtherAtomId(bond, atomId);
            if (otherId >= 0 && otherId != excludedAtomId && molecule.Atoms[otherId].AtomicNumber == atomicNumber && bond.Order == order)
            {
                return bond;
            }

            index = index + 1;
        }

        return null;
    }

    internal Bond? FindProtonatedOxygenBond(Molecule molecule, int atomId)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int oxygenId = GetOtherAtomId(bond, atomId);
            if (oxygenId >= 0 && molecule.Atoms[oxygenId].AtomicNumber == 8 && bond.Order == BondOrder.Single)
            {
                Atom oxygen = molecule.Atoms[oxygenId];
                if (oxygen.FormalCharge == 0 && oxygen.ExplicitHydrogenCount + oxygen.ImplicitHydrogenCount > 0)
                {
                    return bond;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal int FindAminoAcidNitrogen(Molecule molecule, int carbonylCarbonId)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond alphaBond = molecule.Bonds[bondIndex];
            int alphaId = GetOtherAtomId(alphaBond, carbonylCarbonId);
            if (alphaId >= 0 && alphaBond.Order == BondOrder.Single && molecule.Atoms[alphaId].AtomicNumber == 6)
            {
                int candidateIndex = 0;
                while (candidateIndex < molecule.Bonds.Count)
                {
                    Bond nitrogenBond = molecule.Bonds[candidateIndex];
                    int nitrogenId = GetOtherAtomId(nitrogenBond, alphaId);
                    if (nitrogenId >= 0 && nitrogenId != carbonylCarbonId && nitrogenBond.Order == BondOrder.Single && molecule.Atoms[nitrogenId].AtomicNumber == 7)
                    {
                        return nitrogenId;
                    }

                    candidateIndex = candidateIndex + 1;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return -1;
    }

    internal void RemoveOneHydrogenCount(Atom atom)
    {
        if (atom.ExplicitHydrogenCount > 0)
        {
            atom.ExplicitHydrogenCount = atom.ExplicitHydrogenCount - 1;
        }
        else if (atom.ImplicitHydrogenCount > 0)
        {
            atom.ImplicitHydrogenCount = atom.ImplicitHydrogenCount - 1;
        }
    }

    internal int CountElementNeighbors(Molecule molecule, int atomId, int atomicNumber)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            int otherId = GetOtherAtomId(molecule.Bonds[index], atomId);
            if (otherId >= 0 && molecule.Atoms[otherId].AtomicNumber == atomicNumber)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountElementNeighborsWithOrder(Molecule molecule, int atomId, int atomicNumber, BondOrder order)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int otherId = GetOtherAtomId(bond, atomId);
            if (otherId >= 0 && molecule.Atoms[otherId].AtomicNumber == atomicNumber && bond.Order == order)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountBondsForAtom(Molecule molecule, int atomId)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal Bond? FindOnlyDoubleOxygenBond(Molecule molecule, int atomId)
    {
        Bond? found = null;
        int count = 0;
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int oxygenId = GetOtherAtomId(bond, atomId);
            if (oxygenId >= 0 && molecule.Atoms[oxygenId].AtomicNumber == 8 && bond.Order == BondOrder.Double)
            {
                found = bond;
                count = count + 1;
            }

            index = index + 1;
        }

        if (count == 1)
        {
            return found;
        }

        return null;
    }

    internal bool HasDoubleBondToOxygen(Molecule molecule, int atomId)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int oxygenId = GetOtherAtomId(bond, atomId);
            if (oxygenId >= 0 && molecule.Atoms[oxygenId].AtomicNumber == 8 && bond.Order == BondOrder.Double)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int GetAtomIdForAtomicNumber(Molecule molecule, Bond bond, int atomicNumber)
    {
        if (molecule.Atoms[bond.BeginAtomId].AtomicNumber == atomicNumber)
        {
            return bond.BeginAtomId;
        }

        if (molecule.Atoms[bond.EndAtomId].AtomicNumber == atomicNumber)
        {
            return bond.EndAtomId;
        }

        return -1;
    }

    internal int GetOtherAtomId(Bond? bond, int atomId)
    {
        if (bond == null)
        {
            return -1;
        }

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

    internal void RegisterApplied(ChargeNormalizationResult result, string ruleName)
    {
        result.AppliedRuleNames.Add(ruleName);
        result.AppliedRuleCount = result.AppliedRuleCount + 1;
        result.Report.AddInfo("CHARGE_NORMALIZATION_APPLIED", ruleName);
    }
}
