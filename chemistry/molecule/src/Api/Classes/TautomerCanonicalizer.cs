using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides tautomer canonicalizer data and operations for molecular graphs.
/// </summary>
public class TautomerCanonicalizer : ITautomerCanonicalizer
{
    internal TautomerEnumerator Enumerator;
    internal DeterministicSerializer Serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TautomerCanonicalizer"/> class.
    /// </summary>
    public TautomerCanonicalizer()
    {
        Enumerator = new TautomerEnumerator();
        Serializer = new DeterministicSerializer();
    }

    /// <summary>
    /// Selects the highest-scoring tautomer from the bounded enumeration.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Canonicalize(Molecule? molecule, TautomerEnumerationOptions? options)
    {
        TautomerCanonicalizationResult result = CanonicalizeWithResult(molecule, options);
        return result.Molecule;
    }

    /// <summary>
    /// Canonicalizes the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public TautomerCanonicalizationResult CanonicalizeWithResult(Molecule? molecule, TautomerEnumerationOptions? options)
    {
        TautomerCanonicalizationResult result = new TautomerCanonicalizationResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("TAUTOMER_CANONICALIZER_NULL_MOLECULE", "Cannot canonicalize tautomers for a null molecule.");
            return result;
        }

        TautomerEnumerationResult enumeration = Enumerator.EnumerateWithResult(molecule, options);
        if (!enumeration.Success)
        {
            result.Success = false;
            CopyReport(enumeration.Report, result.Report);
            return result;
        }

        result.TautomerCount = enumeration.Tautomers.Count;
        ScoreTautomers(enumeration.Tautomers, result);
        if (result.Scores.Count == 0)
        {
            result.Success = false;
            result.Report.AddError("TAUTOMER_CANONICALIZER_NO_TAUTOMERS", "Tautomer enumeration produced no candidate molecules.");
            return result;
        }

        int bestIndex = SelectBestScoreIndex(result.Scores);
        result.SelectedIndex = result.Scores[bestIndex].TautomerIndex;
        result.BestScore = result.Scores[bestIndex];
        result.Molecule = MoleculeBuilder.CloneMolecule(enumeration.Tautomers[result.SelectedIndex]);
        result.Report.AddInfo("TAUTOMER_CANONICAL_SELECTED", result.SelectedIndex.ToString());
        return result;
    }

    internal Molecule SelectCanonicalTautomer(List<Molecule> tautomers)
    {
        TautomerCanonicalizationResult result = SelectCanonicalTautomerWithResult(tautomers);
        return result.Molecule;
    }

    internal TautomerCanonicalizationResult SelectCanonicalTautomerWithResult(List<Molecule>? tautomers)
    {
        TautomerCanonicalizationResult result = new TautomerCanonicalizationResult();
        if (tautomers == null || tautomers.Count == 0)
        {
            result.Success = false;
            result.Report.AddError("TAUTOMER_CANONICALIZER_NO_TAUTOMERS", "Cannot select a canonical tautomer from an empty list.");
            return result;
        }

        result.TautomerCount = tautomers.Count;
        ScoreTautomers(tautomers, result);
        int bestIndex = SelectBestScoreIndex(result.Scores);
        result.SelectedIndex = result.Scores[bestIndex].TautomerIndex;
        result.BestScore = result.Scores[bestIndex];
        result.Molecule = MoleculeBuilder.CloneMolecule(tautomers[result.SelectedIndex]);
        result.Report.AddInfo("TAUTOMER_CANONICAL_SELECTED", result.SelectedIndex.ToString());
        return result;
    }

    internal void ScoreTautomers(List<Molecule> tautomers, TautomerCanonicalizationResult result)
    {
        int index = 0;
        while (index < tautomers.Count)
        {
            TautomerScore score = ScoreMolecule(tautomers[index]);
            score.TautomerIndex = index;
            result.Scores.Add(score);
            index = index + 1;
        }
    }

    internal TautomerScore ScoreMolecule(Molecule molecule)
    {
        TautomerScore score = new TautomerScore();
        score.CarbonylBondCount = CountCarbonylBonds(molecule);
        score.HeteroDoubleBondCount = CountHeteroDoubleBonds(molecule);
        score.AromaticBondCount = CountAromaticBonds(molecule);
        score.FormalChargePenalty = CountFormalChargeMagnitude(molecule);
        score.HeteroHydrogenPenalty = CountHeteroHydrogens(molecule);
        score.RadicalPenalty = CountRadicalElectrons(molecule);
        score.TotalScore = CalculateTotalScore(score);
        score.TieBreakKey = Serializer.Serialize(molecule);
        return score;
    }

    internal int CalculateTotalScore(TautomerScore score)
    {
        int total = 0;
        total = total + score.CarbonylBondCount * 100;
        total = total + score.HeteroDoubleBondCount * 60;
        total = total + score.AromaticBondCount * 20;
        total = total - score.FormalChargePenalty * 25;
        total = total - score.HeteroHydrogenPenalty * 10;
        total = total - score.RadicalPenalty * 30;
        return total;
    }

    internal int SelectBestScoreIndex(List<TautomerScore> scores)
    {
        int bestIndex = 0;
        int index = 1;
        while (index < scores.Count)
        {
            if (CompareScores(scores[index], scores[bestIndex]) < 0)
            {
                bestIndex = index;
            }

            index = index + 1;
        }

        return bestIndex;
    }

    internal int CompareScores(TautomerScore left, TautomerScore right)
    {
        if (left.TotalScore != right.TotalScore)
        {
            if (left.TotalScore > right.TotalScore)
            {
                return -1;
            }

            return 1;
        }

        int keyComparison = string.CompareOrdinal(left.TieBreakKey, right.TieBreakKey);
        if (keyComparison != 0)
        {
            return keyComparison;
        }

        if (left.TautomerIndex < right.TautomerIndex)
        {
            return -1;
        }

        if (left.TautomerIndex > right.TautomerIndex)
        {
            return 1;
        }

        return 0;
    }

    internal int CountCarbonylBonds(Molecule molecule)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            if (bond.Order == BondOrder.Double && IsCarbonHeteroBond(molecule, bond, 8))
            {
                count = count + 1;
            }

            if (bond.Order == BondOrder.Double && IsCarbonHeteroBond(molecule, bond, 16))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountHeteroDoubleBonds(Molecule molecule)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            if (bond.Order == BondOrder.Double && IsAnyCarbonHeteroBond(molecule, bond))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountAromaticBonds(Molecule molecule)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[index];
            if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountFormalChargeMagnitude(Molecule molecule)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            count = count + Abs(molecule.Atoms[index].FormalCharge);
            index = index + 1;
        }

        return count;
    }

    internal int CountHeteroHydrogens(Molecule molecule)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            if (IsHeteroAtom(atom.AtomicNumber))
            {
                count = count + atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountRadicalElectrons(Molecule molecule)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            count = count + Abs(molecule.Atoms[index].RadicalElectronCount);
            index = index + 1;
        }

        return count;
    }

    internal bool IsAnyCarbonHeteroBond(Molecule molecule, Bond bond)
    {
        if (IsCarbonHeteroBond(molecule, bond, 7))
        {
            return true;
        }

        if (IsCarbonHeteroBond(molecule, bond, 8))
        {
            return true;
        }

        if (IsCarbonHeteroBond(molecule, bond, 15))
        {
            return true;
        }

        if (IsCarbonHeteroBond(molecule, bond, 16))
        {
            return true;
        }

        return false;
    }

    internal bool IsCarbonHeteroBond(Molecule molecule, Bond bond, int heteroAtomicNumber)
    {
        Atom begin = molecule.Atoms[bond.BeginAtomId];
        Atom end = molecule.Atoms[bond.EndAtomId];
        if (begin.AtomicNumber == 6 && end.AtomicNumber == heteroAtomicNumber)
        {
            return true;
        }

        if (end.AtomicNumber == 6 && begin.AtomicNumber == heteroAtomicNumber)
        {
            return true;
        }

        return false;
    }

    internal bool IsHeteroAtom(int atomicNumber)
    {
        if (atomicNumber == 7 || atomicNumber == 8 || atomicNumber == 15 || atomicNumber == 16)
        {
            return true;
        }

        return false;
    }

    internal int Abs(int value)
    {
        if (value < 0)
        {
            return -value;
        }

        return value;
    }

    internal void CopyReport(MoleculeReport source, MoleculeReport target)
    {
        int index = 0;
        while (index < source.Errors.Count)
        {
            target.Add(source.Errors[index]);
            index = index + 1;
        }
    }
}

