using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class FragmentChooserTests
{
    [Test]
    public void Choose_SelectsFragmentWithMostHeavyAtomsBeforeMass()
    {
        Molecule molecule = BuildEthaneAndIodide();
        FragmentChooser chooser = new FragmentChooser();

        FragmentChoice choice = chooser.ChooseWithDetails(molecule);

        Assert.That(choice.Found, Is.True);
        Assert.That(choice.HeavyAtomCount, Is.EqualTo(2));
        Assert.That(choice.Fragment.GetAtomCount(), Is.EqualTo(2));
        Assert.That(choice.CanonicalKey, Is.EqualTo("CC"));
        Assert.That(choice.IsOrganic, Is.True);
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(3));
    }

    [Test]
    public void Choose_UsesMolecularWeightWhenHeavyAtomCountsTie()
    {
        Molecule molecule = BuildCarbonAndOxygen();
        FragmentChooser chooser = new FragmentChooser();

        FragmentChoice choice = chooser.ChooseWithDetails(molecule);

        Assert.That(choice.Found, Is.True);
        Assert.That(choice.HeavyAtomCount, Is.EqualTo(1));
        Assert.That(choice.Fragment.GetAtomCount(), Is.EqualTo(1));
        Assert.That(choice.Fragment.Atoms[0].AtomicNumber, Is.EqualTo(8));
        Assert.That(choice.MolecularWeight, Is.GreaterThan(15.0));
    }

    [Test]
    public void RankFragments_ReturnsSortedChoicesWithComponentDetails()
    {
        Molecule molecule = BuildEthaneAndIodide();
        FragmentChooser chooser = new FragmentChooser();

        List<FragmentChoice> choices = chooser.RankFragments(molecule);

        Assert.That(choices.Count, Is.EqualTo(2));
        Assert.That(choices[0].Component?.ComponentId, Is.EqualTo(0));
        Assert.That(choices[1].Component?.ComponentId, Is.EqualTo(1));
        Assert.That(choices[0].CanonicalKey, Is.EqualTo("CC"));
        Assert.That(choices[1].CanonicalKey, Is.EqualTo("[I-]"));
    }

    [Test]
    public void CompareChoices_PrefersOrganicWhenSizeAndMassTie()
    {
        FragmentChooser chooser = new FragmentChooser();
        FragmentChoice organic = CreateChoice(2, 100.0, true, "Z");
        FragmentChoice inorganic = CreateChoice(2, 100.0, false, "A");

        Assert.That(chooser.CompareChoices(organic, inorganic), Is.LessThan(0));
        Assert.That(chooser.CompareChoices(inorganic, organic), Is.GreaterThan(0));
    }

    [Test]
    public void CompareChoices_UsesCanonicalKeyAsFinalTieBreaker()
    {
        FragmentChooser chooser = new FragmentChooser();
        FragmentChoice left = CreateChoice(2, 100.0, true, "C");
        FragmentChoice right = CreateChoice(2, 100.0, true, "N");

        Assert.That(chooser.CompareChoices(left, right), Is.LessThan(0));
        Assert.That(chooser.CompareChoices(right, left), Is.GreaterThan(0));
    }

    [Test]
    public void Choose_ReturnsEmptyChoiceForNullOrEmptyMolecule()
    {
        FragmentChooser chooser = new FragmentChooser();

        FragmentChoice nullChoice = chooser.ChooseWithDetails(null);
        FragmentChoice emptyChoice = chooser.ChooseWithDetails(new Molecule());

        Assert.That(nullChoice.Found, Is.False);
        Assert.That(nullChoice.Fragment.GetAtomCount(), Is.EqualTo(0));
        Assert.That(emptyChoice.Found, Is.False);
        Assert.That(emptyChoice.Fragment.GetAtomCount(), Is.EqualTo(0));
    }

    [Test]
    public void Choose_ReturnsSelectedFragmentShortcutWithoutMutatingMixture()
    {
        Molecule mixture = BuildEthaneAndIodide();

        Molecule fragment = new FragmentChooser().Choose(mixture);

        Assert.That(fragment.GetAtomCount(), Is.EqualTo(2));
        Assert.That(fragment.GetBondCount(), Is.EqualTo(1));
        Assert.That(mixture.GetAtomCount(), Is.EqualTo(3));
    }

    static FragmentChoice CreateChoice(int heavyAtomCount, double molecularWeight, bool isOrganic, string canonicalKey)
    {
        FragmentChoice choice = new FragmentChoice();
        choice.HeavyAtomCount = heavyAtomCount;
        choice.MolecularWeight = molecularWeight;
        choice.IsOrganic = isOrganic;
        choice.CanonicalKey = canonicalKey;
        choice.Found = true;
        return choice;
    }

    static Molecule BuildEthaneAndIodide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(carbonA, carbonB, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddAtom(new AtomSpec(53, 0, -1, 0, 0, 0, 0, false, "", "I"));
        return builder.Build();
    }

    static Molecule BuildCarbonAndOxygen()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        return builder.Build();
    }
}
