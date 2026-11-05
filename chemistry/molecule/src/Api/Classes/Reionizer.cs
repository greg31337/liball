using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides reionizer data and operations for molecular graphs.
/// </summary>
public class Reionizer : IReionizer
{
    internal NormalizationRules Rules;

    /// <summary>
    /// Initializes a new instance of the <see cref="Reionizer"/> class.
    /// </summary>
    public Reionizer()
    {
        Rules = new NormalizationRules();
    }

    /// <summary>
    /// Returns a copy after deterministic proton transfers between recognized acid and base sites.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Reionize(Molecule? molecule)
    {
        ReionizationResult result = ReionizeWithResult(molecule);
        return result.Molecule;
    }

    /// <summary>
    /// Reionizes the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public ReionizationResult ReionizeWithResult(Molecule? molecule)
    {
        ReionizationResult result = new ReionizationResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("REIONIZER_NULL_MOLECULE", "Cannot reionize a null molecule.");
            return result;
        }

        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);
        int limit = result.Molecule.Atoms.Count + 1;
        int step = 0;
        bool moved = true;
        while (moved && step < limit)
        {
            moved = ApplyBestTransfer(result.Molecule, result);
            step = step + 1;
        }

        if (result.TransferCount > 0)
        {
            result.Changed = true;
            result.Molecule.ClearCaches();
        }

        return result;
    }

    internal bool ApplyBestTransfer(Molecule molecule, ReionizationResult result)
    {
        AcidBaseSite donor = FindBestDonor(molecule);
        if (!donor.Found)
        {
            return false;
        }

        AcidBaseSite acceptor = FindBestAcceptor(molecule, donor);
        if (!acceptor.Found)
        {
            return false;
        }

        TransferProton(molecule, donor, acceptor);
        RegisterTransfer(result, donor, acceptor);
        return true;
    }

    internal AcidBaseSite FindBestDonor(Molecule molecule)
    {
        AcidBaseSite best = new AcidBaseSite();
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            if (CanDonateProton(atom))
            {
                AcidBaseSite site = FindSite(molecule, atom.Id);
                if (site.Found && IsBetterDonor(site, best))
                {
                    best = site;
                }
            }

            atomIndex = atomIndex + 1;
        }

        return best;
    }

    internal AcidBaseSite FindBestAcceptor(Molecule molecule, AcidBaseSite donor)
    {
        AcidBaseSite best = new AcidBaseSite();
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            if (atom.FormalCharge == -1)
            {
                AcidBaseSite site = FindSite(molecule, atom.Id);
                if (site.Found && site.Rule.Priority > donor.Rule.Priority && IsBetterAcceptor(site, best))
                {
                    best = site;
                }
            }

            atomIndex = atomIndex + 1;
        }

        return best;
    }

    internal AcidBaseSite FindSite(Molecule molecule, int atomId)
    {
        AcidBaseSite site = new AcidBaseSite();
        List<AcidBaseRule> rules = Rules.GetOrderedAcidBaseRules();
        int index = 0;
        while (index < rules.Count)
        {
            if (RuleMatchesSite(molecule, atomId, rules[index]))
            {
                site.Found = true;
                site.AtomId = atomId;
                site.Rule = rules[index];
                return site;
            }

            index = index + 1;
        }

        return site;
    }

    internal bool RuleMatchesSite(Molecule molecule, int atomId, AcidBaseRule rule)
    {
        Atom atom = molecule.Atoms[atomId];
        if (atom.AtomicNumber != rule.AcidAtomicNumber)
        {
            return false;
        }

        int neighborId = FindNeighborWithAtomicNumber(molecule, atomId, rule.NeighborAtomicNumber);
        if (neighborId < 0)
        {
            return false;
        }

        if (rule.Name == "phenol" && !molecule.Atoms[neighborId].IsAromatic)
        {
            return false;
        }

        if (rule.RequiresDoubleOxygenNeighbor && !HasDoubleOxygenNeighbor(molecule, neighborId, atomId))
        {
            return false;
        }

        return true;
    }

    internal bool CanDonateProton(Atom atom)
    {
        if (atom.FormalCharge != 0 && atom.FormalCharge != 1)
        {
            return false;
        }

        return atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount > 0;
    }

    internal bool IsBetterDonor(AcidBaseSite candidate, AcidBaseSite current)
    {
        if (!current.Found)
        {
            return true;
        }

        if (candidate.Rule.Priority != current.Rule.Priority)
        {
            return candidate.Rule.Priority < current.Rule.Priority;
        }

        return candidate.AtomId < current.AtomId;
    }

    internal bool IsBetterAcceptor(AcidBaseSite candidate, AcidBaseSite current)
    {
        if (!current.Found)
        {
            return true;
        }

        if (candidate.Rule.Priority != current.Rule.Priority)
        {
            return candidate.Rule.Priority > current.Rule.Priority;
        }

        return candidate.AtomId < current.AtomId;
    }

    internal void TransferProton(Molecule molecule, AcidBaseSite donor, AcidBaseSite acceptor)
    {
        Atom donorAtom = molecule.Atoms[donor.AtomId];
        Atom acceptorAtom = molecule.Atoms[acceptor.AtomId];
        RemoveOneHydrogen(donorAtom);
        AddOneHydrogen(acceptorAtom);
        donorAtom.FormalCharge = donorAtom.FormalCharge - 1;
        acceptorAtom.FormalCharge = acceptorAtom.FormalCharge + 1;
    }

    internal void RemoveOneHydrogen(Atom atom)
    {
        if (atom.ExplicitHydrogenCount > 0)
        {
            atom.ExplicitHydrogenCount = atom.ExplicitHydrogenCount - 1;
            return;
        }

        if (atom.ImplicitHydrogenCount > 0)
        {
            atom.ImplicitHydrogenCount = atom.ImplicitHydrogenCount - 1;
        }
    }

    internal void AddOneHydrogen(Atom atom)
    {
        atom.ImplicitHydrogenCount = atom.ImplicitHydrogenCount + 1;
    }

    internal int FindNeighborWithAtomicNumber(Molecule molecule, int atomId, int atomicNumber)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int neighborId = GetOtherAtomId(bond, atomId);
            if (neighborId >= 0 && molecule.Atoms[neighborId].AtomicNumber == atomicNumber)
            {
                return neighborId;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool HasDoubleOxygenNeighbor(Molecule molecule, int atomId, int excludedAtomId)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            int neighborId = GetOtherAtomId(bond, atomId);
            if (neighborId >= 0 && neighborId != excludedAtomId && molecule.Atoms[neighborId].AtomicNumber == 8 && bond.Order == BondOrder.Double)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int GetOtherAtomId(Bond bond, int atomId)
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

    internal void RegisterTransfer(ReionizationResult result, AcidBaseSite donor, AcidBaseSite acceptor)
    {
        string name = donor.Rule.Name + "->" + acceptor.Rule.Name;
        result.TransferNames.Add(name);
        result.TransferCount = result.TransferCount + 1;
        result.Report.AddInfo("REIONIZATION_TRANSFER", name);
    }
}

