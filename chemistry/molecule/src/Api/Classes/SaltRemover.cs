using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides salt remover data and operations for molecular graphs.
/// </summary>
public class SaltRemover : ISaltRemover
{
    internal CommonFragments CommonFragments;
    internal Components Components;
    internal FragmentChooser FragmentChooser;
    internal SmilesParser SmilesParser;
    internal CanonicalSmilesWriter SmilesWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaltRemover"/> class.
    /// </summary>
    public SaltRemover()
    {
        CommonFragments = new CommonFragments();
        Components = new Components();
        FragmentChooser = new FragmentChooser();
        SmilesParser = new SmilesParser();
        SmilesWriter = new CanonicalSmilesWriter();
    }

    /// <summary>
    /// Removes the salts.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule RemoveSalts(Molecule? molecule)
    {
        SaltRemovalResult result = RemoveSaltsWithResult(molecule);
        return result.Parent;
    }

    /// <summary>
    /// Remove saltss the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public SaltRemovalResult RemoveSaltsWithResult(Molecule? molecule)
    {
        SaltRemovalResult result = new SaltRemovalResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("SALT_REMOVAL_NULL_MOLECULE", "Cannot remove salts from a null molecule.");
            return result;
        }

        List<ComponentInfo> components = Components.FindComponents(molecule);
        result.InputFragmentCount = components.Count;
        if (components.Count == 0)
        {
            result.Parent = new Molecule();
            return result;
        }

        List<Molecule> retainedFragments = new List<Molecule>();
        List<Molecule> matchedSaltFragments = new List<Molecule>();
        List<string> matchedSaltNames = new List<string>();

        int index = 0;
        while (index < components.Count)
        {
            Molecule fragment = Components.CreateMoleculeForComponent(molecule, components[index]);
            string saltName = FindKnownSaltName(fragment);
            if (saltName.Length > 0)
            {
                matchedSaltFragments.Add(fragment);
                matchedSaltNames.Add(saltName);
            }
            else
            {
                retainedFragments.Add(fragment);
            }

            index = index + 1;
        }

        result.MatchedSaltFragmentCount = matchedSaltFragments.Count;
        if (retainedFragments.Count == 0)
        {
            result.Parent = MoleculeBuilder.CloneMolecule(molecule);
            result.RetainedFragmentCount = components.Count;
            result.AllFragmentsMatchedSalts = true;
            return result;
        }

        CopyRemovedFragments(result, matchedSaltFragments, matchedSaltNames);
        result.RemovedFragmentCount = matchedSaltFragments.Count;
        result.RetainedFragmentCount = retainedFragments.Count;
        result.Parent = ChooseFromFragments(retainedFragments);
        result.Changed = result.RemovedFragmentCount > 0 || retainedFragments.Count != components.Count || retainedFragments.Count > 1;
        return result;
    }

    internal Molecule ChooseFromFragments(List<Molecule>? fragments)
    {
        if (fragments == null || fragments.Count == 0)
        {
            return new Molecule();
        }

        List<FragmentChoice> choices = new List<FragmentChoice>();
        int index = 0;
        while (index < fragments.Count)
        {
            choices.Add(FragmentChooser.CreateChoice(fragments[index], new ComponentInfo()));
            index = index + 1;
        }

        FragmentChooser.SortChoices(choices);
        return choices[0].Fragment;
    }

    internal void CopyRemovedFragments(SaltRemovalResult result, List<Molecule> fragments, List<string> names)
    {
        int index = 0;
        while (index < fragments.Count)
        {
            result.RemovedFragments.Add(fragments[index]);
            result.RemovedFragmentNames.Add(names[index]);
            index = index + 1;
        }
    }

    internal bool IsKnownSaltFragment(Molecule fragment)
    {
        return FindKnownSaltName(fragment).Length > 0;
    }

    internal string FindKnownSaltName(Molecule fragment)
    {
        string key = SmilesWriter.Write(fragment);
        if (key.Length == 0)
        {
            return "";
        }

        List<CommonFragmentRecord> records = CommonFragments.GetByFlag(true, false, true);
        int index = 0;
        while (index < records.Count)
        {
            if (RecordMatchesFragment(records[index], fragment, key))
            {
                return records[index].Name;
            }

            index = index + 1;
        }

        return "";
    }

    internal bool RecordMatchesFragment(CommonFragmentRecord record, Molecule fragment, string key)
    {
        if (record.Smiles == key)
        {
            return true;
        }

        SmilesParseResult result = SmilesParser.ParseWithResult(record.Smiles);
        if (!result.Success)
        {
            return RecordMatchesSimpleIon(record, fragment);
        }

        if (SmilesWriter.Write(result.Molecule) == key)
        {
            return true;
        }

        return RecordMatchesSimpleIon(record, fragment);
    }

    internal bool RecordMatchesSimpleIon(CommonFragmentRecord record, Molecule? fragment)
    {
        if (fragment == null || fragment.Atoms.Count != 1)
        {
            return false;
        }

        Atom atom = fragment.Atoms[0];
        if (record.Name == "sodium" && atom.AtomicNumber == 11 && atom.FormalCharge == 1)
        {
            return true;
        }

        if (record.Name == "potassium" && atom.AtomicNumber == 19 && atom.FormalCharge == 1)
        {
            return true;
        }

        if (record.Name == "lithium" && atom.AtomicNumber == 3 && atom.FormalCharge == 1)
        {
            return true;
        }

        if (record.Name == "calcium" && atom.AtomicNumber == 20 && atom.FormalCharge == 2)
        {
            return true;
        }

        if (record.Name == "magnesium" && atom.AtomicNumber == 12 && atom.FormalCharge == 2)
        {
            return true;
        }

        return false;
    }
}

