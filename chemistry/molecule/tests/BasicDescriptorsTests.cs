using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class BasicDescriptorsTests
{
    [Test]
    public void Calculate_CountsRotatableBondsAndFractionSp3ForButane()
    {
        BasicDescriptors descriptors = new BasicDescriptors();

        DescriptorSet set = descriptors.Calculate(BuildButane());

        Assert.That(set.RotatableBondCount, Is.EqualTo(1));
        Assert.That(set.HydrogenBondDonorCount, Is.EqualTo(0));
        Assert.That(set.HydrogenBondAcceptorCount, Is.EqualTo(0));
        Assert.That(set.TopologicalPolarSurfaceArea, Is.EqualTo(0.0));
        Assert.That(set.FractionCsp3, Is.EqualTo(1.0));
    }

    [Test]
    public void Calculate_CountsHydrogenBondDescriptorsAndTpsaForEthanol()
    {
        BasicDescriptors descriptors = new BasicDescriptors();

        DescriptorSet set = descriptors.Calculate(BuildEthanol());

        Assert.That(set.HydrogenBondDonorCount, Is.EqualTo(1));
        Assert.That(set.HydrogenBondAcceptorCount, Is.EqualTo(1));
        Assert.That(set.TopologicalPolarSurfaceArea, Is.EqualTo(17.07).Within(0.0001));
        Assert.That(set.FractionCsp3, Is.EqualTo(1.0));
        Assert.That(set.RotatableBondCount, Is.EqualTo(0));
    }

    [Test]
    public void Calculate_TreatsPositiveNitrogenAsDonorButNotAcceptor()
    {
        BasicDescriptors descriptors = new BasicDescriptors();

        DescriptorSet set = descriptors.Calculate(BuildAmmonium());

        Assert.That(set.FormalCharge, Is.EqualTo(1));
        Assert.That(set.HydrogenBondDonorCount, Is.EqualTo(1));
        Assert.That(set.HydrogenBondAcceptorCount, Is.EqualTo(0));
        Assert.That(set.TopologicalPolarSurfaceArea, Is.EqualTo(12.03).Within(0.0001));
    }

    [Test]
    public void Calculate_ReportsCarbonylCarbonAsNonSp3()
    {
        BasicDescriptors descriptors = new BasicDescriptors();

        DescriptorSet set = descriptors.Calculate(BuildAcetone());

        Assert.That(set.HydrogenBondAcceptorCount, Is.EqualTo(1));
        Assert.That(set.TopologicalPolarSurfaceArea, Is.EqualTo(17.07).Within(0.0001));
        Assert.That(set.FractionCsp3, Is.EqualTo(2.0 / 3.0).Within(0.0001));
    }

    [Test]
    public void Calculate_CountsRingAndAromaticRingDescriptors()
    {
        BasicDescriptors descriptors = new BasicDescriptors();

        DescriptorSet saturated = descriptors.Calculate(BuildCyclohexane());
        DescriptorSet aromatic = descriptors.Calculate(BuildAromaticBenzene());

        Assert.That(saturated.RingCount, Is.EqualTo(1));
        Assert.That(saturated.AromaticRingCount, Is.EqualTo(0));
        Assert.That(saturated.FractionCsp3, Is.EqualTo(1.0));
        Assert.That(aromatic.RingCount, Is.EqualTo(1));
        Assert.That(aromatic.AromaticRingCount, Is.EqualTo(1));
        Assert.That(aromatic.FractionCsp3, Is.EqualTo(0.0));
    }

    [Test]
    public void Calculate_ProvidesExplicitUnavailableDiagnosticsForLogPAndMolarRefractivity()
    {
        BasicDescriptors descriptors = new BasicDescriptors();

        DescriptorSet set = descriptors.Calculate(BuildEthanol());

        Assert.That(set.LogP.Available, Is.False);
        Assert.That(set.LogP.Value, Is.EqualTo(0.0));
        Assert.That(set.LogP.DiagnosticCode, Is.EqualTo("DESCRIPTOR_LOGP_UNAVAILABLE"));
        Assert.That(set.LogP.DiagnosticMessage, Does.Contain("validated atom-fragment coefficient model"));
        Assert.That(set.MolarRefractivity.Available, Is.False);
        Assert.That(set.MolarRefractivity.Value, Is.EqualTo(0.0));
        Assert.That(set.MolarRefractivity.DiagnosticCode, Is.EqualTo("DESCRIPTOR_MOLAR_REFRACTIVITY_UNAVAILABLE"));
        Assert.That(set.MolarRefractivity.DiagnosticMessage, Does.Contain("validated atom-fragment coefficient model"));
        Assert.That(set.Formula, Is.EqualTo("C2H6O"));
        Assert.That(set.MolecularWeight, Is.EqualTo(46.069).Within(0.001));
        Assert.That(set.HeavyAtomCount, Is.EqualTo(3));
        Assert.That(set.HydrogenBondDonorCount, Is.EqualTo(1));
        Assert.That(set.TopologicalPolarSurfaceArea, Is.EqualTo(17.07).Within(0.0001));
        Assert.That(set.FractionCsp3, Is.EqualTo(1.0));
    }

    static Molecule BuildButane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonC = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonD = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, carbonC, new BondSpec());
        builder.AddBond(carbonC, carbonD, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthanol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildAmmonium()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(7, 0, 1, 0, 4, 0, 0, false, "", "N"));
        return builder.Build();
    }

    static Molecule BuildAcetone()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonA, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, carbonB, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCyclohexane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < 6)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
            index = index + 1;
        }

        AddRingBonds(builder, BondOrder.Single, false);
        return builder.Build();
    }

    static Molecule BuildAromaticBenzene()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < 6)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, true, "", "C"));
            index = index + 1;
        }

        AddRingBonds(builder, BondOrder.Aromatic, true);
        return builder.Build();
    }

    static void AddRingBonds(MoleculeBuilder builder, BondOrder order, bool aromatic)
    {
        int index = 0;
        while (index < 6)
        {
            int next = index + 1;
            if (next == 6)
            {
                next = 0;
            }

            builder.AddBond(index, next, BondSpec.Create(order, aromatic, BondStereo.None, BondDirection.None));
            index = index + 1;
        }
    }
}
