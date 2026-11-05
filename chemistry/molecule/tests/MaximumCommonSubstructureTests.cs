using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MaximumCommonSubstructureTests
{
    [Test]
    public void Find_ReturnsMaximumConnectedGraphAndDeterministicMappings()
    {
        MaximumCommonSubstructure service = new MaximumCommonSubstructure();
        Molecule left = BuildAcetamide();
        Molecule right = BuildAcetone();

        MaximumCommonSubstructureResult result = service.Find(left, right, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Found, Is.True);
        Assert.That(result.SearchCompleted, Is.True);
        Assert.That(result.AtomCount, Is.EqualTo(3));
        Assert.That(result.BondCount, Is.EqualTo(2));
        Assert.That(GetLeftAtomIds(result), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(GetRightAtomIds(result), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(GetLeftBondIds(result), Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(GetRightBondIds(result), Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(3));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(2));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(left.GetAtomCount(), Is.EqualTo(4));
        Assert.That(right.GetAtomCount(), Is.EqualTo(4));
    }

    [Test]
    public void Find_IsDeterministicAcrossRenumberedEquivalentMolecules()
    {
        MaximumCommonSubstructureResult result = new MaximumCommonSubstructure().Find(BuildEthanolForward(), BuildEthanolReversed(), null);

        Assert.That(result.AtomCount, Is.EqualTo(3));
        Assert.That(result.BondCount, Is.EqualTo(2));
        Assert.That(GetLeftAtomIds(result), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(GetRightAtomIds(result), Is.EqualTo(new int[] { 2, 1, 0 }));
    }

    [Test]
    public void Find_AppliesBondOrderAndFormalChargeQueryOptions()
    {
        MaximumCommonSubstructure service = new MaximumCommonSubstructure();
        Molecule doubleBond = BuildCarbonOxygen(BondOrder.Double, 0);
        Molecule chargedSingleBond = BuildCarbonOxygen(BondOrder.Single, -1);
        MaximumCommonSubstructureOptions relaxed = new MaximumCommonSubstructureOptions();
        relaxed.MatchBondOrders = false;
        MaximumCommonSubstructureOptions chargeSensitive = new MaximumCommonSubstructureOptions();
        chargeSensitive.MatchBondOrders = false;
        chargeSensitive.MatchFormalCharges = true;

        MaximumCommonSubstructureResult exactBond = service.Find(doubleBond, chargedSingleBond, null);
        MaximumCommonSubstructureResult relaxedBond = service.Find(doubleBond, chargedSingleBond, relaxed);
        MaximumCommonSubstructureResult exactCharge = service.Find(doubleBond, chargedSingleBond, chargeSensitive);

        Assert.That(exactBond.AtomCount, Is.EqualTo(1));
        Assert.That(relaxedBond.AtomCount, Is.EqualTo(2));
        Assert.That(relaxedBond.BondCount, Is.EqualTo(1));
        Assert.That(exactCharge.AtomCount, Is.EqualTo(1));
    }

    [Test]
    public void Find_ConnectedOptionRejectsDisconnectedAtomOnlyMapping()
    {
        MaximumCommonSubstructureOptions disconnected = new MaximumCommonSubstructureOptions();
        disconnected.RequireConnected = false;
        Molecule separateCarbons = BuildSeparateCarbons();
        Molecule ethane = BuildEthane();
        MaximumCommonSubstructure service = new MaximumCommonSubstructure();

        MaximumCommonSubstructureResult connectedResult = service.Find(separateCarbons, ethane, null);
        MaximumCommonSubstructureResult disconnectedResult = service.Find(separateCarbons, ethane, disconnected);
        MaximumCommonSubstructureResult reverseConnected = service.Find(ethane, separateCarbons, null);
        MaximumCommonSubstructureResult reverseDisconnected = service.Find(ethane, separateCarbons, disconnected);

        Assert.That(connectedResult.AtomCount, Is.EqualTo(1));
        Assert.That(disconnectedResult.AtomCount, Is.EqualTo(2));
        Assert.That(disconnectedResult.BondCount, Is.EqualTo(0));
        Assert.That(reverseConnected.AtomCount, Is.EqualTo(connectedResult.AtomCount));
        Assert.That(reverseDisconnected.AtomCount, Is.EqualTo(disconnectedResult.AtomCount));
    }

    [Test]
    public void Find_ReturnsBestSoFarWithWorkLimitDiagnostic()
    {
        MaximumCommonSubstructureOptions options = new MaximumCommonSubstructureOptions();
        options.MaximumSearchStepCount = 2;

        MaximumCommonSubstructureResult result = new MaximumCommonSubstructure().Find(BuildEthane(), BuildEthane(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Found, Is.True);
        Assert.That(result.AtomCount, Is.EqualTo(1));
        Assert.That(result.SearchStepCount, Is.EqualTo(2));
        Assert.That(result.SearchCompleted, Is.False);
        Assert.That(result.LimitReached, Is.True);
        Assert.That(result.WorkLimitReached, Is.True);
        Assert.That(result.TimedOut, Is.False);
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MCS_WORK_LIMIT_REACHED"));
    }

    [Test]
    public void Find_ReportsImmediateTimeoutDeterministically()
    {
        MaximumCommonSubstructureOptions options = new MaximumCommonSubstructureOptions();
        options.MaximumElapsedMilliseconds = 0;

        MaximumCommonSubstructureResult result = new MaximumCommonSubstructure().Find(BuildEthane(), BuildEthane(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Found, Is.False);
        Assert.That(result.SearchStepCount, Is.EqualTo(0));
        Assert.That(result.SearchCompleted, Is.False);
        Assert.That(result.TimedOut, Is.True);
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("MCS_TIMEOUT"));
    }

    [Test]
    public void Find_ReportsInvalidInputsAndOptions()
    {
        MaximumCommonSubstructure service = new MaximumCommonSubstructure();
        MaximumCommonSubstructureOptions options = new MaximumCommonSubstructureOptions();
        options.MaximumSearchStepCount = 0;

        MaximumCommonSubstructureResult nullResult = service.Find(null, BuildEthane(), null);
        MaximumCommonSubstructureResult invalidResult = service.Find(BuildEthane(), BuildEthane(), options);

        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.Report.Get(0).Code, Is.EqualTo("MCS_NULL_MOLECULE"));
        Assert.That(invalidResult.Success, Is.False);
        Assert.That(invalidResult.Report.Get(0).Code, Is.EqualTo("MCS_OPTIONS_INVALID"));
    }

    [Test]
    public void Find_AppliesEveryRemainingAtomAndStereoMatchOption()
    {
        MaximumCommonSubstructure service = new MaximumCommonSubstructure();
        Molecule decorated = BuildDecoratedCarbon(13, 1, 3, 1, 7, true);
        Molecule plain = BuildDecoratedCarbon(12, 0, 4, 0, 0, false);
        MaximumCommonSubstructureOptions relaxed = CreateRelaxedOptions();

        Assert.That(service.Find(decorated, plain, relaxed).AtomCount, Is.EqualTo(1));

        MaximumCommonSubstructureOptions isotope = CreateRelaxedOptions();
        isotope.MatchIsotopes = true;
        MaximumCommonSubstructureOptions hydrogens = CreateRelaxedOptions();
        hydrogens.MatchHydrogenCounts = true;
        MaximumCommonSubstructureOptions radicals = CreateRelaxedOptions();
        radicals.MatchRadicals = true;
        MaximumCommonSubstructureOptions maps = CreateRelaxedOptions();
        maps.MatchAtomMaps = true;
        MaximumCommonSubstructureOptions aromaticity = CreateRelaxedOptions();
        aromaticity.MatchAromaticity = true;
        MaximumCommonSubstructureOptions stereo = CreateRelaxedOptions();
        stereo.MatchStereo = true;

        Assert.That(service.Find(decorated, plain, isotope).Found, Is.False);
        Assert.That(service.Find(decorated, plain, hydrogens).Found, Is.False);
        Assert.That(service.Find(decorated, plain, radicals).Found, Is.False);
        Assert.That(service.Find(decorated, plain, maps).Found, Is.False);
        Assert.That(service.Find(decorated, plain, aromaticity).Found, Is.False);
        Assert.That(service.Find(BuildStereoBond(BondDirection.Up), BuildStereoBond(BondDirection.Down), stereo).AtomCount, Is.EqualTo(1));
    }

    [Test]
    public void Find_MinimumLargerThanEitherInputCompletesWithoutSearching()
    {
        MaximumCommonSubstructureOptions options = new MaximumCommonSubstructureOptions();
        options.MinimumAtomCount = 3;

        MaximumCommonSubstructureResult result = new MaximumCommonSubstructure().Find(BuildEthane(), BuildEthane(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Found, Is.False);
        Assert.That(result.SearchCompleted, Is.True);
        Assert.That(result.SearchStepCount, Is.Zero);
        Assert.That(result.Report.GetCount(), Is.Zero);
    }

    static MaximumCommonSubstructureOptions CreateRelaxedOptions()
    {
        MaximumCommonSubstructureOptions options = new MaximumCommonSubstructureOptions();
        options.MatchAromaticity = false;
        options.MatchBondOrders = false;
        return options;
    }

    static Molecule BuildDecoratedCarbon(int isotope, int charge, int hydrogens, int radicals, int atomMap, bool aromatic)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, isotope, charge, 0, hydrogens, radicals, atomMap, aromatic, "", "C"));
        return builder.Build();
    }

    static Molecule BuildStereoBond(BondDirection direction)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        builder.AddBond(carbon, fluorine, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, direction));
        return builder.Build();
    }

    static int[] GetLeftAtomIds(MaximumCommonSubstructureResult result)
    {
        int[] ids = new int[result.AtomMappings.Count];
        int index = 0;
        while (index < ids.Length)
        {
            ids[index] = result.AtomMappings[index].LeftAtomId;
            index = index + 1;
        }

        return ids;
    }

    static int[] GetRightAtomIds(MaximumCommonSubstructureResult result)
    {
        int[] ids = new int[result.AtomMappings.Count];
        int index = 0;
        while (index < ids.Length)
        {
            ids[index] = result.AtomMappings[index].RightAtomId;
            index = index + 1;
        }

        return ids;
    }

    static int[] GetLeftBondIds(MaximumCommonSubstructureResult result)
    {
        int[] ids = new int[result.BondMappings.Count];
        int index = 0;
        while (index < ids.Length)
        {
            ids[index] = result.BondMappings[index].LeftBondId;
            index = index + 1;
        }

        return ids;
    }

    static int[] GetRightBondIds(MaximumCommonSubstructureResult result)
    {
        int[] ids = new int[result.BondMappings.Count];
        int index = 0;
        while (index < ids.Length)
        {
            ids[index] = result.BondMappings[index].RightBondId;
            index = index + 1;
        }

        return ids;
    }

    static Molecule BuildAcetamide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(methyl, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, nitrogen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildAcetone()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methylA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int methylB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(methylA, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, methylB, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthanolForward()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthanolReversed()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCarbonOxygen(BondOrder order, int oxygenCharge)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, oxygenCharge, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildSeparateCarbons()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        return builder.Build();
    }

    static Molecule BuildEthane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(0, 1, new BondSpec());
        return builder.Build();
    }
}
