using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class KekulizerTests
{
    [Test]
    public void Kekulize_ClonesMoleculeAndAssignsDeterministicBenzenePattern()
    {
        Molecule molecule = BuildAromaticCarbonRing(6);
        Kekulizer kekulizer = new Kekulizer();

        KekulizationResult result = kekulizer.Kekulize(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(result.Report.GetCount(), Is.EqualTo(0));
        Assert.That(result.Molecule, Is.Not.SameAs(molecule));
        Assert.That(result.KekulizedBondIds.Count, Is.EqualTo(6));
        AssertBondOrderPattern(result.Molecule, new BondOrder[] {
            BondOrder.Double,
            BondOrder.Single,
            BondOrder.Double,
            BondOrder.Single,
            BondOrder.Double,
            BondOrder.Single
        });
        Assert.That(result.Molecule.Bonds[0].IsAromatic, Is.False);
        Assert.That(result.Molecule.Atoms[0].IsAromatic, Is.False);
        Assert.That(result.Molecule.IsRingCacheValid, Is.False);
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(molecule.Bonds[0].IsAromatic, Is.True);
    }

    [Test]
    public void KekulizeInPlace_AssignsFuranPatternWithoutDoubleBondToOxygen()
    {
        Molecule molecule = BuildAromaticFuranLikeRing();
        Kekulizer kekulizer = new Kekulizer();

        KekulizationResult result = kekulizer.KekulizeInPlace(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(result.Report.GetCount(), Is.EqualTo(0));
        AssertBondOrderPattern(molecule, new BondOrder[] {
            BondOrder.Single,
            BondOrder.Double,
            BondOrder.Single,
            BondOrder.Double,
            BondOrder.Single
        });
        Assert.That(molecule.Bonds[0].IsAromatic, Is.False);
        Assert.That(molecule.Bonds[4].IsAromatic, Is.False);
        Assert.That(molecule.Atoms[0].IsAromatic, Is.False);
    }

    [Test]
    public void Kekulize_UsesFallbackAndDiagnosticForNonCandidateAromaticRing()
    {
        Molecule molecule = BuildAromaticCarbonRing(5);
        Kekulizer kekulizer = new Kekulizer();

        KekulizationResult result = kekulizer.Kekulize(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedFallback, Is.True);
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("KEKULIZATION_FALLBACK"));
        Assert.That(result.Report.Get(0).BondId, Is.EqualTo(0));
        AssertBondOrderPattern(result.Molecule, new BondOrder[] {
            BondOrder.Double,
            BondOrder.Single,
            BondOrder.Double,
            BondOrder.Single,
            BondOrder.Single
        });
    }

    [Test]
    public void Kekulize_ReturnsErrorForNullMolecule()
    {
        Kekulizer kekulizer = new Kekulizer();
        Molecule? molecule = null;

        KekulizationResult result = kekulizer.Kekulize(molecule);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("NULL_MOLECULE"));
    }

    static Molecule BuildAromaticCarbonRing(int size)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < size)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "C"));
            index = index + 1;
        }

        AddAromaticRingBonds(builder, size);
        return builder.Build();
    }

    static Molecule BuildAromaticFuranLikeRing()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, true, "", "O"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "C"));
        AddAromaticRingBonds(builder, 5);
        return builder.Build();
    }

    static void AddAromaticRingBonds(MoleculeBuilder builder, int size)
    {
        int index = 0;
        while (index < size)
        {
            int next = index + 1;
            if (next == size)
            {
                next = 0;
            }

            builder.AddBond(index, next, BondSpec.Create(BondOrder.Aromatic, true, BondStereo.None, BondDirection.None));
            index = index + 1;
        }
    }

    static void AssertBondOrderPattern(Molecule molecule, BondOrder[] expectedOrders)
    {
        int index = 0;
        while (index < expectedOrders.Length)
        {
            Assert.That(molecule.Bonds[index].Order, Is.EqualTo(expectedOrders[index]));
            index = index + 1;
        }
    }
}
