using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ConfigurableRingFinderTests
{
    [Test]
    public void FindRingsWithOptions_ReturnsDeterministicIndependentCycleBasis()
    {
        Molecule molecule = BuildSquareWithDiagonal();
        IRingFinder finder = new RingFinder();

        RingFindingResult first = finder.FindRingsWithOptions(molecule, null);
        RingFindingResult second = finder.FindRingsWithOptions(molecule, null);

        Assert.That(first.Success, Is.True);
        Assert.That(first.CycleBasis.Rings, Has.Count.EqualTo(2));
        Assert.That(first.AllSmallRings.Rings, Is.Empty);
        Assert.That(first.Rings.Rings, Has.Count.EqualTo(2));
        Assert.That(first.CycleBasis.Rings[0].Size, Is.EqualTo(3));
        Assert.That(first.CycleBasis.Rings[1].Size, Is.EqualTo(3));
        Assert.That(first.CycleBasis.Rings[0].Key, Is.EqualTo(second.CycleBasis.Rings[0].Key));
        Assert.That(first.CycleBasis.Rings[1].Key, Is.EqualTo(second.CycleBasis.Rings[1].Key));
        Assert.That(first.Rings.RingAtomIds, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(first.Rings.RingBondIds, Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void FindRingsWithOptions_EnumeratesAllSmallRingsWithinSizeLimit()
    {
        Molecule molecule = BuildSquareWithDiagonal();
        RingFindingOptions options = new RingFindingOptions();
        options.FindCycleBasis = false;
        options.FindAllSmallRings = true;
        options.MaximumRingSize = 4;
        IRingFinder finder = new RingFinder();

        RingFindingResult result = finder.FindRingsWithOptions(molecule, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LimitReached, Is.False);
        Assert.That(result.CycleBasis.Rings, Is.Empty);
        Assert.That(result.AllSmallRings.Rings, Has.Count.EqualTo(3));
        Assert.That(result.AllSmallRings.Rings[0].Size, Is.EqualTo(3));
        Assert.That(result.AllSmallRings.Rings[1].Size, Is.EqualTo(3));
        Assert.That(result.AllSmallRings.Rings[2].Size, Is.EqualTo(4));
        Assert.That(result.Rings.Rings, Has.Count.EqualTo(3));
        Assert.That(CountRingsContainingBond(result.Rings, 4), Is.EqualTo(2));
    }

    [Test]
    public void FindRingsWithOptions_ExcludesRingsAboveMaximumSize()
    {
        Molecule molecule = BuildSquareWithDiagonal();
        RingFindingOptions options = new RingFindingOptions();
        options.FindCycleBasis = false;
        options.FindAllSmallRings = true;
        options.MaximumRingSize = 3;

        RingFindingResult result = new RingFinder().FindRingsWithOptions(molecule, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AllSmallRings.Rings, Has.Count.EqualTo(2));
        Assert.That(result.AllSmallRings.Rings[0].Size, Is.EqualTo(3));
        Assert.That(result.AllSmallRings.Rings[1].Size, Is.EqualTo(3));
    }

    [Test]
    public void FindRingsWithOptions_ReportsCallerCountLimit()
    {
        RingFindingOptions options = new RingFindingOptions();
        options.FindCycleBasis = false;
        options.FindAllSmallRings = true;
        options.MaximumRingSize = 4;
        options.MaximumSmallRingCount = 1;

        RingFindingResult result = new RingFinder().FindRingsWithOptions(BuildSquareWithDiagonal(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LimitReached, Is.True);
        Assert.That(result.AllSmallRings.Rings, Has.Count.EqualTo(1));
        Assert.That(result.Report.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("RING_SEARCH_LIMIT_REACHED"));
        Assert.That(result.Report.Errors[0].Severity, Is.EqualTo(ErrorSeverity.Warning));
    }

    [Test]
    public void FindRingsWithOptions_ClassifiesFusedAndBridgedSystems()
    {
        RingFindingOptions options = new RingFindingOptions();
        options.FindAllSmallRings = true;
        options.MaximumRingSize = 6;
        RingFinder finder = new RingFinder();

        RingFindingResult fused = finder.FindRingsWithOptions(BuildFusedTriangles(), options);
        RingFindingResult bridged = finder.FindRingsWithOptions(BuildBridgedThetaGraph(), options);

        Assert.That(fused.Rings.RingSystems, Has.Count.EqualTo(1));
        Assert.That(fused.Rings.RingSystems[0].IsFused, Is.True);
        Assert.That(fused.Rings.RingSystems[0].IsBridged, Is.False);
        Assert.That(fused.Rings.RingSystems[0].BridgeheadAtomIds, Is.Empty);
        Assert.That(bridged.Rings.RingSystems, Has.Count.EqualTo(1));
        Assert.That(bridged.Rings.RingSystems[0].IsFused, Is.False);
        Assert.That(bridged.Rings.RingSystems[0].IsBridged, Is.True);
        Assert.That(bridged.Rings.RingSystems[0].BridgeheadAtomIds, Is.EqualTo(new int[] { 0, 1 }));
    }

    [Test]
    public void FindRingsWithOptions_ReportsNullMoleculeAndInvalidLimits()
    {
        RingFinder finder = new RingFinder();
        RingFindingOptions invalid = new RingFindingOptions();
        invalid.FindAllSmallRings = true;
        invalid.MaximumRingSize = 2;

        RingFindingResult nullResult = finder.FindRingsWithOptions(null, null);
        RingFindingResult invalidResult = finder.FindRingsWithOptions(BuildSquareWithDiagonal(), invalid);

        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("RING_NULL_MOLECULE"));
        Assert.That(invalidResult.Success, Is.False);
        Assert.That(invalidResult.Report.Errors[0].Code, Is.EqualTo("RING_INVALID_OPTIONS"));
        Assert.That(invalidResult.Rings.Rings, Is.Empty);
    }

    static int CountRingsContainingBond(RingSet ringSet, int bondId)
    {
        int count = 0;
        int ringIndex = 0;
        while (ringIndex < ringSet.Rings.Count)
        {
            int memberIndex = 0;
            while (memberIndex < ringSet.Rings[ringIndex].BondIds.Count)
            {
                if (ringSet.Rings[ringIndex].BondIds[memberIndex] == bondId)
                {
                    count = count + 1;
                    memberIndex = ringSet.Rings[ringIndex].BondIds.Count;
                }
                else
                {
                    memberIndex = memberIndex + 1;
                }
            }

            ringIndex = ringIndex + 1;
        }

        return count;
    }

    static Molecule BuildSquareWithDiagonal()
    {
        MoleculeBuilder builder = BuildAtoms(4);
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        builder.AddBond(2, 3, new BondSpec());
        builder.AddBond(3, 0, new BondSpec());
        builder.AddBond(0, 2, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildFusedTriangles()
    {
        MoleculeBuilder builder = BuildAtoms(4);
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        builder.AddBond(2, 0, new BondSpec());
        builder.AddBond(0, 3, new BondSpec());
        builder.AddBond(3, 2, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildBridgedThetaGraph()
    {
        MoleculeBuilder builder = BuildAtoms(6);
        builder.AddBond(0, 2, new BondSpec());
        builder.AddBond(2, 1, new BondSpec());
        builder.AddBond(0, 3, new BondSpec());
        builder.AddBond(3, 1, new BondSpec());
        builder.AddBond(0, 4, new BondSpec());
        builder.AddBond(4, 5, new BondSpec());
        builder.AddBond(5, 1, new BondSpec());
        return builder.Build();
    }

    static MoleculeBuilder BuildAtoms(int count)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < count)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
            index = index + 1;
        }

        return builder;
    }
}
