using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class AdvancedSmartsTests
{
    [Test]
    public void Parser_PreservesLogicalPrecedenceInPublicExpressionTree()
    {
        QueryAtom atom = new SmartsParser().Parse("[#6,#7;H1]").Atoms[0];

        Assert.That(atom.Expression.Operation, Is.EqualTo(QueryOperation.And));
        Assert.That(atom.Expression.Children[0].Operation, Is.EqualTo(QueryOperation.Or));
        Assert.That(atom.Expression.Children[1].Primitive, Is.EqualTo(QueryPrimitive.HydrogenCount));
        Assert.That(atom.MatchesAtomInMolecule(BuildSingleAtom(6, 1), 0), Is.True);
        Assert.That(atom.MatchesAtomInMolecule(BuildSingleAtom(7, 0), 0), Is.False);
        Assert.That(atom.MatchesAtomInMolecule(BuildSingleAtom(8, 1), 0), Is.False);
    }

    [Test]
    public void Parser_AppliesNegationBeforeAndAndOr()
    {
        SmartsParser parser = new SmartsParser();
        QueryAtom negatedCarbonOrNitrogen = parser.Parse("[!#6,#7]").Atoms[0];
        QueryAtom negatedGroup = parser.Parse("[!(#6,#7)]").Atoms[0];

        Assert.That(negatedCarbonOrNitrogen.MatchesAtomInMolecule(BuildSingleAtom(8, 0), 0), Is.True);
        Assert.That(negatedCarbonOrNitrogen.MatchesAtomInMolecule(BuildSingleAtom(7, 0), 0), Is.True);
        Assert.That(negatedCarbonOrNitrogen.MatchesAtomInMolecule(BuildSingleAtom(6, 0), 0), Is.False);
        Assert.That(negatedGroup.MatchesAtomInMolecule(BuildSingleAtom(8, 0), 0), Is.True);
        Assert.That(negatedGroup.MatchesAtomInMolecule(BuildSingleAtom(7, 0), 0), Is.False);
    }

    [Test]
    public void Matcher_AnchorsRecursiveAndNestedRecursiveSmarts()
    {
        Molecule target = BuildAcetamide();
        SmartsMatcher matcher = new SmartsMatcher();

        SubgraphMatch recursive = matcher.FindFirst(target, "[$([#6]=[#8])]");
        SubgraphMatch nested = matcher.FindFirst(target, "[$([#6;$([#6]=[#8])])]");

        Assert.That(recursive.Success, Is.True);
        Assert.That(recursive.AtomMapping, Is.EqualTo(new int[] { 1 }));
        Assert.That(nested.Success, Is.True);
        Assert.That(nested.AtomMapping, Is.EqualTo(new int[] { 1 }));
    }

    [Test]
    public void ParserAndMatcher_SupportExpandedAtomPrimitives()
    {
        SmartsParser parser = new SmartsParser();
        QueryAtom connectivity = parser.Parse("[#6;X4;h3]").Atoms[0];
        QueryAtom hydrogenElement = parser.Parse("[H]").Atoms[0];
        QueryAtom sodiumCation = parser.Parse("[Na+]").Atoms[0];
        Molecule ethane = BuildEthane();

        Assert.That(connectivity.MatchesAtomInMolecule(ethane, 0), Is.True);
        Assert.That(hydrogenElement.MatchesAtomInMolecule(BuildSingleAtom(1, 0), 0), Is.True);
        Assert.That(hydrogenElement.MatchesAtomInMolecule(BuildSingleAtom(6, 1), 0), Is.False);
        Assert.That(sodiumCation.MatchesAtomInMolecule(BuildChargedAtom(11, 1), 0), Is.True);
        Assert.That(sodiumCation.MatchesAtomInMolecule(BuildChargedAtom(11, 0), 0), Is.False);
    }

    [Test]
    public void Matcher_SupportsRingSizeConnectivityAndRingBondPrimitives()
    {
        SmartsMatcher matcher = new SmartsMatcher();
        Molecule cyclopropane = BuildCyclopropane();
        Molecule ethane = BuildEthane();

        Assert.That(matcher.FindFirst(cyclopropane, "[r3;x2]").Success, Is.True);
        Assert.That(matcher.FindFirst(cyclopropane, "[r6]").Success, Is.False);
        Assert.That(matcher.FindFirst(cyclopropane, "C@C").Success, Is.True);
        Assert.That(matcher.FindFirst(ethane, "C@C").Success, Is.False);
        Assert.That(matcher.FindFirst(ethane, "C!@C").Success, Is.True);
        Assert.That(matcher.FindFirst(cyclopropane, "C!@C").Success, Is.False);
    }

    [Test]
    public void Matcher_EvaluatesLogicalBondExpressions()
    {
        SmartsMatcher matcher = new SmartsMatcher();

        Assert.That(matcher.FindFirst(BuildCarbonOxygenBond(BondOrder.Single), "C-,=O").Success, Is.True);
        Assert.That(matcher.FindFirst(BuildCarbonOxygenBond(BondOrder.Double), "C-,=O").Success, Is.True);
        Assert.That(matcher.FindFirst(BuildCarbonOxygenBond(BondOrder.Triple), "C-,=O").Success, Is.False);
    }

    [Test]
    public void Matcher_EnforcesParenthesizedComponentGroups()
    {
        SmartsParser parser = new SmartsParser();
        SmartsMatcher matcher = new SmartsMatcher();
        QueryMolecule grouped = parser.Parse("([#6].[#8])");
        Molecule connected = BuildCarbonOxygenBond(BondOrder.Single);
        Molecule disconnected = BuildDisconnectedCarbonOxygen();

        Assert.That(grouped.ComponentGroupIdsByAtomId, Is.EqualTo(new int[] { 0, 0 }));
        Assert.That(matcher.FindFirstQuery(connected, grouped).Success, Is.True);
        Assert.That(matcher.FindFirstQuery(disconnected, grouped).Success, Is.False);
        Assert.That(matcher.FindFirst(disconnected, "[#6].[#8]").Success, Is.True);
    }

    [Test]
    public void FindAll_DeduplicatesGroupedAutomorphismsDeterministically()
    {
        SmartsMatcher matcher = new SmartsMatcher();

        List<SubgraphMatch> first = matcher.FindAll(BuildEthane(), "([#6].[#6])");
        List<SubgraphMatch> second = matcher.FindAll(BuildEthane(), "([#6].[#6])");

        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(first[0].AtomMapping, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(second, Has.Count.EqualTo(1));
        Assert.That(second[0].AtomMapping, Is.EqualTo(first[0].AtomMapping));
    }

    static Molecule BuildSingleAtom(int atomicNumber, int implicitHydrogenCount)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(atomicNumber, 0, 0, 0, implicitHydrogenCount, 0, 0, false, "", ""));
        return builder.Build();
    }

    static Molecule BuildChargedAtom(int atomicNumber, int charge)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(atomicNumber, 0, charge, 0, 0, 0, 0, false, "", ""));
        return builder.Build();
    }

    static Molecule BuildEthane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", ""));
        int second = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", ""));
        builder.AddBond(first, second, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildAcetamide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", ""));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", ""));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", ""));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", ""));
        builder.AddBond(methyl, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, nitrogen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCyclopropane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", ""));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", ""));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", ""));
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        builder.AddBond(2, 0, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCarbonOxygenBond(BondOrder order)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", ""));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", ""));
        builder.AddBond(carbon, oxygen, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildDisconnectedCarbonOxygen()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", ""));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", ""));
        return builder.Build();
    }
}
