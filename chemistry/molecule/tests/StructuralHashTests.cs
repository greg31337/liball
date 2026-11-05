using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class StructuralHashTests
{
    [Test]
    public void ComputeExact_ReturnsStableHashUnderAtomAndBondRenumbering()
    {
        Molecule forward = BuildAsymmetricAmide();
        Molecule reversed = BuildAsymmetricAmideReversed();
        StructuralHash structuralHash = new StructuralHash();

        string forwardHash = structuralHash.ComputeExact(forward);
        string reversedHash = structuralHash.ComputeExact(reversed);

        Assert.That(forwardHash, Is.EqualTo(reversedHash));
        Assert.That(forwardHash.Length, Is.EqualTo(64));
    }

    [Test]
    public void BuildCanonicalText_ReturnsStableTextUnderAtomAndBondRenumbering()
    {
        Molecule forward = BuildAsymmetricAmide();
        Molecule reversed = BuildAsymmetricAmideReversed();
        StructuralHash structuralHash = new StructuralHash();

        string forwardText = structuralHash.BuildCanonicalText(forward);
        string reversedText = structuralHash.BuildCanonicalText(reversed);

        Assert.That(forwardText, Is.EqualTo(reversedText));
    }

    [Test]
    public void ComputeExact_ChangesWhenBondOrderChanges()
    {
        Molecule single = BuildCarbonOxygen(BondOrder.Single);
        Molecule doubleBond = BuildCarbonOxygen(BondOrder.Double);
        StructuralHash structuralHash = new StructuralHash();

        string singleHash = structuralHash.ComputeExact(single);
        string doubleHash = structuralHash.ComputeExact(doubleBond);

        Assert.That(singleHash, Is.Not.EqualTo(doubleHash));
    }

    [Test]
    public void ComputeExact_ChangesWhenExactAtomFieldsChange()
    {
        Molecule neutralCarbon = BuildSingleAtom(new AtomSpec(6, 12, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule chargedCarbon = BuildSingleAtom(new AtomSpec(6, 12, 1, 0, 3, 0, 0, false, "", "C"));
        Molecule isotopeCarbon = BuildSingleAtom(new AtomSpec(6, 13, 0, 0, 4, 0, 0, false, "", "C"));
        StructuralHash structuralHash = new StructuralHash();

        string neutralHash = structuralHash.ComputeExact(neutralCarbon);
        string chargedHash = structuralHash.ComputeExact(chargedCarbon);
        string isotopeHash = structuralHash.ComputeExact(isotopeCarbon);

        Assert.That(neutralHash, Is.Not.EqualTo(chargedHash));
        Assert.That(neutralHash, Is.Not.EqualTo(isotopeHash));
    }

    [Test]
    public void ComputeExact_ChangesWhenDirectionalStereoChanges()
    {
        Molecule up = BuildDirectedBond(BondDirection.Up);
        Molecule down = BuildDirectedBond(BondDirection.Down);
        StructuralHash structuralHash = new StructuralHash();

        string upHash = structuralHash.ComputeExact(up);
        string downHash = structuralHash.ComputeExact(down);

        Assert.That(upHash, Is.Not.EqualTo(downHash));
    }

    [Test]
    public void BuildCanonicalText_EscapesStringFields()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        AtomSpec spec = new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "C|alpha", "line\none");
        spec.QueryValue = "x\\y";
        builder.AddAtom(spec);
        StructuralHash structuralHash = new StructuralHash();

        string text = structuralHash.BuildCanonicalText(builder.Build());

        Assert.That(text, Does.Contain("C\\palpha"));
        Assert.That(text, Does.Contain("line\\none"));
        Assert.That(text, Does.Contain("x\\\\y"));
    }

    [Test]
    public void ComputeExact_ReturnsStableSha256HexForNullMolecule()
    {
        StructuralHash structuralHash = new StructuralHash();

        string firstHash = structuralHash.ComputeExact(null);
        string secondHash = structuralHash.Compute(null);

        Assert.That(firstHash, Is.EqualTo(secondHash));
        Assert.That(firstHash.Length, Is.EqualTo(64));
    }

    static Molecule BuildAsymmetricAmide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(methylCarbon, carbonylCarbon, new BondSpec());
        builder.AddBond(carbonylCarbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonylCarbon, nitrogen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildAsymmetricAmideReversed()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int methylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonylCarbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrogen, carbonylCarbon, new BondSpec());
        builder.AddBond(methylCarbon, carbonylCarbon, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCarbonOxygen(BondOrder order)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildSingleAtom(AtomSpec spec)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(spec);
        return builder.Build();
    }

    static Molecule BuildDirectedBond(BondDirection direction)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(carbon, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, direction));
        return builder.Build();
    }
}
