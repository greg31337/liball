using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SubgraphMatcherTests
{
    [Test]
    public void ContainsSubgraph_MatchesExactMoleculeFragment()
    {
        Molecule target = BuildAcetamide();
        Molecule query = BuildCarbonylQuery();
        SubgraphMatcher matcher = new SubgraphMatcher();

        SubgraphMatch match = matcher.FindFirst(target, query);

        Assert.That(match.Success, Is.True);
        Assert.That(match.AtomMapping, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void ContainsSubgraph_RejectsWrongExactBondOrder()
    {
        Molecule target = BuildAcetamide();
        Molecule query = BuildTripleCarbonOxygenQuery();
        SubgraphMatcher matcher = new SubgraphMatcher();

        Assert.That(matcher.ContainsSubgraph(target, query), Is.False);
    }

    [Test]
    public void ContainsQuery_MatchesParsedSmartsAtomsAndBonds()
    {
        Molecule target = BuildAcetamide();
        SmartsParser parser = new SmartsParser();
        QueryMolecule query = parser.Parse("[#6]=O");
        SubgraphMatcher matcher = new SubgraphMatcher();

        SubgraphMatch match = matcher.FindFirstQuery(target, query);

        Assert.That(match.Success, Is.True);
        Assert.That(match.AtomMapping, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void FingerprintScreen_PassesExactQueryWhenFeaturesArePresent()
    {
        SubgraphMatcher matcher = new SubgraphMatcher();

        bool passes = matcher.PassesFingerprintScreen(BuildAcetamide(), BuildCarbonylQuery());

        Assert.That(passes, Is.True);
        Assert.That(matcher.ContainsSubgraph(BuildAcetamide(), BuildCarbonylQuery()), Is.True);
    }

    [Test]
    public void FingerprintScreen_RejectsExactQueryWhenRequiredFeatureIsMissing()
    {
        SubgraphMatcher matcher = new SubgraphMatcher();

        bool passes = matcher.PassesFingerprintScreen(BuildEthane(), BuildCarbonylQuery());

        Assert.That(passes, Is.False);
        Assert.That(matcher.ContainsSubgraph(BuildEthane(), BuildCarbonylQuery()), Is.False);
    }

    [Test]
    public void QueryFingerprintScreen_RejectsSimpleAtomicSmartsWhenFeatureIsMissing()
    {
        SubgraphMatcher matcher = new SubgraphMatcher();
        SmartsParser parser = new SmartsParser();
        QueryMolecule query = parser.Parse("[#6]-[#8]");

        bool passes = matcher.PassesQueryFingerprintScreen(BuildEthane(), query);

        Assert.That(passes, Is.False);
        Assert.That(matcher.ContainsQuery(BuildEthane(), query), Is.False);
    }

    [Test]
    public void QueryFingerprintScreen_SkipsWildcardQueries()
    {
        SubgraphMatcher matcher = new SubgraphMatcher();
        QueryMolecule query = new QueryMolecule();
        int first = query.AddAnyAtom();
        int second = query.AddAnyAtom();
        QueryBond bond = new QueryBond();
        bond.SetAny();
        query.AddBond(first, second, bond);

        bool passes = matcher.PassesQueryFingerprintScreen(BuildEthane(), query);

        Assert.That(passes, Is.True);
        Assert.That(matcher.ContainsQuery(BuildEthane(), query), Is.True);
    }

    [Test]
    public void SmartsMatcher_AppliesDegreeValenceAndRingConstraints()
    {
        SmartsMatcher matcher = new SmartsMatcher();
        Molecule acetamide = BuildAcetamide();
        Molecule cyclopropane = BuildCyclopropane();

        Assert.That(matcher.IsMatch(acetamide, "[#6;D3;v4]=O"), Is.True);
        Assert.That(matcher.IsMatch(acetamide, "[R1]"), Is.False);
        Assert.That(matcher.IsMatch(cyclopropane, "[#6;D2;R1;v4]"), Is.True);
    }

    [Test]
    public void SmartsMatcher_RejectsInvalidPatternInsteadOfMatchingEmptyQuery()
    {
        SmartsMatcher matcher = new SmartsMatcher();

        SubgraphMatch match = matcher.FindFirst(BuildAcetamide(), "X");

        Assert.That(match.Success, Is.False);
        Assert.That(match.AtomMapping, Is.Empty);
    }

    [Test]
    public void SmartsMatcher_RejectsAbsentQuery()
    {
        SmartsMatcher matcher = new SmartsMatcher();

        Assert.That(matcher.IsMatch(BuildAcetamide(), "N#N"), Is.False);
    }

    [Test]
    public void GraphIsomorphism_MatchesRenumberedMolecules()
    {
        GraphIsomorphism graph = new GraphIsomorphism();

        Assert.That(graph.AreIsomorphic(BuildEthanolForward(), BuildEthanolReversed()), Is.True);
    }

    [Test]
    public void GraphIsomorphism_RejectsDifferentBondOrders()
    {
        GraphIsomorphism graph = new GraphIsomorphism();

        Assert.That(graph.AreIsomorphic(BuildEthanolForward(), BuildCarbonCarbonDoubleOxygen()), Is.False);
    }

    [Test]
    public void GraphIsomorphism_RejectsNullAndDifferentSizedGraphs()
    {
        GraphIsomorphism graph = new GraphIsomorphism();

        Assert.That(graph.AreIsomorphic(null, BuildEthane()), Is.False);
        Assert.That(graph.AreIsomorphic(BuildEthane(), null), Is.False);
        Assert.That(graph.AreIsomorphic(BuildEthane(), BuildPropane()), Is.False);
        Assert.That(graph.AreIsomorphic(new Molecule(), new Molecule()), Is.True);
    }

    [Test]
    public void SubgraphEntryPoints_RejectNullAndOversizedQueries()
    {
        SubgraphMatcher matcher = new SubgraphMatcher();
        QueryMolecule query = new SmartsParser().Parse("[#6]-[#6]-[#6]");

        Assert.That(matcher.ContainsSubgraph(null, BuildEthane()), Is.False);
        Assert.That(matcher.ContainsSubgraph(BuildEthane(), null), Is.False);
        Assert.That(matcher.FindAll(null, BuildEthane()), Is.Empty);
        Assert.That(matcher.FindAll(BuildEthane(), BuildPropane()), Is.Empty);
        Assert.That(matcher.ContainsQuery(BuildEthane(), null), Is.False);
        Assert.That(matcher.FindFirstQuery(BuildEthane(), query).Success, Is.False);
        Assert.That(matcher.FindAllQuery(BuildEthane(), query), Is.Empty);
    }

    [Test]
    public void SmartsMatcher_QueryOverloadsMatchStringOverloads()
    {
        SmartsMatcher matcher = new SmartsMatcher();
        QueryMolecule query = new SmartsParser().Parse("[#6]=O");
        Molecule target = BuildAcetamide();

        SubgraphMatch first = matcher.FindFirstQuery(target, query);
        List<SubgraphMatch> all = matcher.FindAllQuery(target, query);

        Assert.That(first.Success, Is.True);
        Assert.That(first.AtomMapping, Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].AtomMapping, Is.EqualTo(first.AtomMapping));
    }

    [Test]
    public void FindAll_DeduplicatesAutomorphicExactMatches()
    {
        SubgraphMatcher matcher = new SubgraphMatcher();

        List<SubgraphMatch> matches = matcher.FindAll(BuildEthane(), BuildEthane());

        Assert.That(matches.Count, Is.EqualTo(1));
        Assert.That(matches[0].Success, Is.True);
        Assert.That(matches[0].AtomMapping.Length, Is.EqualTo(2));
        Assert.That(matches[0].GetTargetAtomId(0), Is.EqualTo(0));
        Assert.That(matches[0].GetTargetAtomId(1), Is.EqualTo(1));
    }

    [Test]
    public void FindAll_ReturnsDistinctExactTargetAtomSets()
    {
        SubgraphMatcher matcher = new SubgraphMatcher();

        List<SubgraphMatch> matches = matcher.FindAll(BuildPropane(), BuildTerminalCarbonPairQuery());

        Assert.That(matches.Count, Is.EqualTo(2));
        Assert.That(matches[0].AtomMapping, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(matches[1].AtomMapping, Is.EqualTo(new int[] { 2, 1 }));
    }

    [Test]
    public void FindAll_DeduplicatesAutomorphicSmartsMatches()
    {
        SmartsMatcher matcher = new SmartsMatcher();

        List<SubgraphMatch> matches = matcher.FindAll(BuildEthane(), "[#6]-[#6]");

        Assert.That(matches.Count, Is.EqualTo(1));
        Assert.That(matches[0].AtomMapping, Is.EqualTo(new int[] { 0, 1 }));
    }

    [Test]
    public void FindAll_ReturnsAtomMappingOutputForEachSmartsHit()
    {
        SmartsMatcher matcher = new SmartsMatcher();

        List<SubgraphMatch> matches = matcher.FindAll(BuildAcetamide(), "[#6]");

        Assert.That(matches.Count, Is.EqualTo(2));
        Assert.That(matches[0].GetTargetAtomId(0), Is.EqualTo(0));
        Assert.That(matches[1].GetTargetAtomId(0), Is.EqualTo(1));
    }

    [Test]
    public void FindAll_ReturnsEmptyListForInvalidSmarts()
    {
        SmartsMatcher matcher = new SmartsMatcher();

        List<SubgraphMatch> matches = matcher.FindAll(BuildAcetamide(), "X");

        Assert.That(matches, Is.Empty);
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

    static Molecule BuildCarbonylQuery()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildTripleCarbonOxygenQuery()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Triple, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildCyclopropane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        builder.AddBond(2, 0, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthanolForward()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildPropane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonC = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, carbonC, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildTerminalCarbonPairQuery()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int terminal = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int middle = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddBond(terminal, middle, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthanolReversed()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(oxygen, carbonB, new BondSpec());
        builder.AddBond(carbonB, carbonA, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCarbonCarbonDoubleOxygen()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
