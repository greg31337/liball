using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides fragment chooser data and operations for molecular graphs.
/// </summary>
public class FragmentChooser : IFragmentChooser
{
    internal Components Components;
    internal MassCalculator MassCalculator;
    internal CanonicalSmilesWriter SmilesWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="FragmentChooser"/> class.
    /// </summary>
    public FragmentChooser()
    {
        Components = new Components();
        MassCalculator = new MassCalculator();
        SmilesWriter = new CanonicalSmilesWriter();
    }

    /// <summary>
    /// Selects the preferred parent fragment using size, composition, and deterministic tie breakers.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Choose(Molecule? molecule)
    {
        FragmentChoice choice = ChooseWithDetails(molecule);
        return choice.Fragment;
    }

    /// <summary>
    /// Selects the preferred parent fragment and returns its ranking details.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The selected fragment and its ranking details.</returns>
    public FragmentChoice ChooseWithDetails(Molecule? molecule)
    {
        List<FragmentChoice> choices = RankFragments(molecule);
        if (choices.Count == 0)
        {
            return new FragmentChoice();
        }

        return choices[0];
    }

    internal List<FragmentChoice> RankFragments(Molecule? molecule)
    {
        List<FragmentChoice> choices = new List<FragmentChoice>();
        if (molecule == null)
        {
            return choices;
        }

        List<ComponentInfo> components = Components.FindComponents(molecule);
        int index = 0;
        while (index < components.Count)
        {
            Molecule fragment = Components.CreateMoleculeForComponent(molecule, components[index]);
            choices.Add(CreateChoice(fragment, components[index]));
            index = index + 1;
        }

        SortChoices(choices);
        return choices;
    }

    internal FragmentChoice CreateChoice(Molecule fragment, ComponentInfo? component)
    {
        FragmentChoice choice = new FragmentChoice();
        choice.Fragment = fragment;
        choice.Component = component;
        choice.Found = true;

        if (component != null && component.ComponentId >= 0)
        {
            choice.HeavyAtomCount = component.HeavyAtomCount;
        }
        else
        {
            choice.HeavyAtomCount = CountHeavyAtoms(fragment);
        }

        choice.MolecularWeight = MassCalculator.MolecularWeight(fragment);
        choice.IsOrganic = ContainsCarbon(fragment);
        choice.CanonicalKey = SmilesWriter.Write(fragment);
        return choice;
    }

    internal void SortChoices(List<FragmentChoice> choices)
    {
        int index = 1;
        while (index < choices.Count)
        {
            FragmentChoice value = choices[index];
            int scan = index - 1;

            while (scan >= 0 && CompareChoices(choices[scan], value) > 0)
            {
                choices[scan + 1] = choices[scan];
                scan = scan - 1;
            }

            choices[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareChoices(FragmentChoice left, FragmentChoice right)
    {
        if (left.HeavyAtomCount != right.HeavyAtomCount)
        {
            return right.HeavyAtomCount - left.HeavyAtomCount;
        }

        if (left.MolecularWeight < right.MolecularWeight)
        {
            return 1;
        }

        if (left.MolecularWeight > right.MolecularWeight)
        {
            return -1;
        }

        if (left.IsOrganic != right.IsOrganic)
        {
            if (left.IsOrganic)
            {
                return -1;
            }

            return 1;
        }

        int canonicalComparison = string.CompareOrdinal(left.CanonicalKey, right.CanonicalKey);
        if (canonicalComparison != 0)
        {
            return canonicalComparison;
        }

        if (left.Component != null && right.Component != null)
        {
            return left.Component.FirstAtomId - right.Component.FirstAtomId;
        }

        return 0;
    }

    internal int CountHeavyAtoms(Molecule? molecule)
    {
        int count = 0;
        if (molecule == null)
        {
            return count;
        }

        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].AtomicNumber > 1)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool ContainsCarbon(Molecule? molecule)
    {
        if (molecule == null)
        {
            return false;
        }

        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].AtomicNumber == 6)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
