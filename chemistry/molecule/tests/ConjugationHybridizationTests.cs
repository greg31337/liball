using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ConjugationHybridizationTests
{
    [Test]
    public void Assign_IdentifiesButadieneConjugatedSystem()
    {
        Molecule molecule = BuildButadiene();
        Conjugation conjugation = new Conjugation();

        ConjugationResult result = conjugation.Assign(molecule);

        Assert.That(result.Systems.Count, Is.EqualTo(1));
        Assert.That(result.Systems[0].AtomIds, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(result.Systems[0].BondIds, Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(result.AtomIsConjugated(0), Is.True);
        Assert.That(result.AtomIsConjugated(3), Is.True);
        Assert.That(result.BondIsConjugated(1), Is.True);
    }

    [Test]
    public void Assign_DoesNotTreatIsolatedAlkeneAsConjugatedSystem()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddBond(0, 1, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        Conjugation conjugation = new Conjugation();

        ConjugationResult result = conjugation.Assign(builder.Build());

        Assert.That(result.Systems, Is.Empty);
        Assert.That(result.ConjugatedAtomIds, Is.Empty);
        Assert.That(result.ConjugatedBondIds, Is.Empty);
    }

    [Test]
    public void Assign_IncludesAmideNitrogenLonePairInConjugatedSystem()
    {
        Molecule molecule = BuildAmide();
        Conjugation conjugation = new Conjugation();

        ConjugationResult result = conjugation.Assign(molecule);

        Assert.That(result.Systems.Count, Is.EqualTo(1));
        Assert.That(result.AtomIsConjugated(0), Is.True);
        Assert.That(result.AtomIsConjugated(1), Is.True);
        Assert.That(result.AtomIsConjugated(2), Is.True);
        Assert.That(result.BondIsConjugated(0), Is.True);
        Assert.That(result.BondIsConjugated(1), Is.True);
    }

    [Test]
    public void Assign_ReportsAromaticRingAsConjugatedSystem()
    {
        Molecule molecule = BuildAlternatingCarbonRing(6);
        AromaticityModel aromaticity = new AromaticityModel();
        aromaticity.PerceiveInPlace(molecule);
        Conjugation conjugation = new Conjugation();

        ConjugationResult result = conjugation.Assign(molecule);

        Assert.That(result.Systems.Count, Is.EqualTo(1));
        Assert.That(result.ConjugatedAtomIds.Count, Is.EqualTo(6));
        Assert.That(result.ConjugatedBondIds.Count, Is.EqualTo(6));
    }

    [Test]
    public void Assign_ReturnsEmptyResultsForNullMolecule()
    {
        Conjugation conjugation = new Conjugation();
        HybridizationModel hybridization = new HybridizationModel();
        Molecule? molecule = null;

        ConjugationResult conjugationResult = conjugation.Assign(molecule);
        HybridizationAssignment hybridizationResult = hybridization.Assign(molecule);

        Assert.That(conjugationResult.Systems, Is.Empty);
        Assert.That(hybridizationResult.Atoms, Is.Empty);
    }

    [Test]
    public void Assign_ClassifiesCommonHybridizationStates()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methaneCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        int etheneCarbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int etheneCarbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int nitrileCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int nitrileNitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int hydrogen = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        builder.AddBond(etheneCarbonA, etheneCarbonB, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrileCarbon, nitrileNitrogen, BondSpec.Create(BondOrder.Triple, false, BondStereo.None, BondDirection.None));
        HybridizationModel model = new HybridizationModel();

        HybridizationAssignment result = model.Assign(builder.Build());

        Assert.That(result.GetAtom(methaneCarbon).Hybridization, Is.EqualTo(Hybridization.Sp3));
        Assert.That(result.GetAtom(etheneCarbonA).Hybridization, Is.EqualTo(Hybridization.Sp2));
        Assert.That(result.GetAtom(nitrileCarbon).Hybridization, Is.EqualTo(Hybridization.Sp));
        Assert.That(result.GetAtom(nitrileNitrogen).Hybridization, Is.EqualTo(Hybridization.Sp));
        Assert.That(result.GetAtom(hydrogen).Hybridization, Is.EqualTo(Hybridization.S));
    }

    [Test]
    public void Assign_UsesConjugationAndAromaticityForSp2Hybridization()
    {
        Molecule amide = BuildAmide();
        Molecule aromatic = BuildAlternatingCarbonRing(6);
        AromaticityModel aromaticity = new AromaticityModel();
        aromaticity.PerceiveInPlace(aromatic);
        HybridizationModel model = new HybridizationModel();

        HybridizationAssignment amideHybridization = model.Assign(amide);
        HybridizationAssignment aromaticHybridization = model.Assign(aromatic);

        Assert.That(amideHybridization.GetAtom(2).Hybridization, Is.EqualTo(Hybridization.Sp2));
        Assert.That(amideHybridization.GetAtom(2).IsConjugated, Is.True);
        Assert.That(amideHybridization.GetAtom(2).Reason, Is.EqualTo("conjugated"));
        Assert.That(aromaticHybridization.GetAtom(0).Hybridization, Is.EqualTo(Hybridization.Sp2));
        Assert.That(aromaticHybridization.GetAtom(0).Reason, Is.EqualTo("aromatic"));
    }

    [Test]
    public void Assign_ClassifiesCumulatedHypervalentAndUnknownAtomStates()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int alleneCenter = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int alleneLeft = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int alleneRight = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int phosphorus = builder.AddAtom(new AtomSpec(15, 0, 0, 0, 0, 0, 0, false, "", "P"));
        int sulfur = builder.AddAtom(new AtomSpec(16, 0, 0, 0, 0, 0, 0, false, "", "S"));
        int unknown = builder.AddAtom(new AtomSpec(0, 0, 0, 0, 0, 0, 0, false, "", "*"));
        builder.AddBond(alleneCenter, alleneLeft, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(alleneCenter, alleneRight, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        AddTerminalAtoms(builder, phosphorus, 5);
        AddTerminalAtoms(builder, sulfur, 6);
        HybridizationAssignment result = new HybridizationModel().Assign(builder.Build());

        Assert.That(result.GetAtom(alleneCenter).Hybridization, Is.EqualTo(Hybridization.Sp));
        Assert.That(result.GetAtom(phosphorus).Hybridization, Is.EqualTo(Hybridization.Sp3d));
        Assert.That(result.GetAtom(sulfur).Hybridization, Is.EqualTo(Hybridization.Sp3d2));
        Assert.That(result.GetAtom(unknown).Hybridization, Is.EqualTo(Hybridization.Unknown));
    }

    static void AddTerminalAtoms(MoleculeBuilder builder, int centerAtomId, int count)
    {
        int index = 0;
        while (index < count)
        {
            int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
            builder.AddBond(centerAtomId, fluorine, new BondSpec());
            index = index + 1;
        }
    }

    static Molecule BuildButadiene()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < 4)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
            index = index + 1;
        }

        builder.AddBond(0, 1, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(1, 2, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(2, 3, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildAmide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(0, 1, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(1, 2, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildAlternatingCarbonRing(int size)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < size)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
            index = index + 1;
        }

        index = 0;
        while (index < size)
        {
            int next = index + 1;
            if (next == size)
            {
                next = 0;
            }

            BondOrder order = BondOrder.Single;
            if (index % 2 == 0)
            {
                order = BondOrder.Double;
            }

            builder.AddBond(index, next, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
            index = index + 1;
        }

        return builder.Build();
    }
}
