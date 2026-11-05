using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SmartsParserTests
{
    [Test]
    public void ParseAndParseWithResult_NormalizeNullAndEmptyInputs()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule query = parser.Parse(null);
        SmartsParseResult result = parser.ParseWithResult("");

        Assert.That(query.GetAtomCount(), Is.Zero);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Query.GetAtomCount(), Is.Zero);
        Assert.That(result.Report.GetErrorCount(), Is.Zero);
    }

    [Test]
    public void ParseWithResult_ReportsExactLocationForUnmatchedBranch()
    {
        SmartsParseResult result = new SmartsParser().ParseWithResult(")C");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorIndex, Is.Zero);
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("SMARTS_PARSE_ERROR"));
        Assert.That(result.Report.Get(0).Location.LineNumber, Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Location.ColumnNumber, Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Location.Context, Is.EqualTo(")C"));
    }

    [Test]
    public void Parse_ReadsBareAtomsAndPrimitiveBonds()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule query = parser.Parse("C=O");

        Assert.That(query.GetAtomCount(), Is.EqualTo(2));
        Assert.That(query.GetBondCount(), Is.EqualTo(1));
        Assert.That(query.GetAtom(0).AtomicNumber, Is.EqualTo(6));
        Assert.That(query.GetAtom(1).AtomicNumber, Is.EqualTo(8));
        Assert.That(query.GetBond(0).Order, Is.EqualTo(BondOrder.Double));
        Assert.That(query.GetBond(0).HasOrder, Is.True);
    }

    [Test]
    public void Parse_ReadsAnyAtomAndAnyBond()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule query = parser.Parse("*~N");

        Assert.That(query.GetAtom(0).MatchesAny, Is.True);
        Assert.That(query.GetAtom(1).AtomicNumber, Is.EqualTo(7));
        Assert.That(query.GetBond(0).MatchesAny, Is.True);
        Assert.That(query.GetBond(0).HasOrder, Is.False);
    }

    [Test]
    public void Parse_ReadsBracketAtomicNumberIsotopeHydrogenAndCharge()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule query = parser.Parse("[13#6][NH2][O-]");

        Assert.That(query.GetAtomCount(), Is.EqualTo(3));
        Assert.That(query.GetAtom(0).AtomicNumber, Is.EqualTo(6));
        Assert.That(query.GetAtom(0).IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(query.GetAtom(1).AtomicNumber, Is.EqualTo(7));
        Assert.That(query.GetAtom(1).HydrogenCount, Is.EqualTo(2));
        Assert.That(query.GetAtom(2).AtomicNumber, Is.EqualTo(8));
        Assert.That(query.GetAtom(2).FormalCharge, Is.EqualTo(-1));
    }

    [Test]
    public void Parse_ReadsAromaticAtomsAndAromaticBonds()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule query = parser.Parse("c:c");

        Assert.That(query.GetAtom(0).IsAromatic, Is.True);
        Assert.That(query.GetAtom(0).HasAromaticity, Is.True);
        Assert.That(query.GetBond(0).Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(query.GetBond(0).IsAromatic, Is.True);
    }

    [Test]
    public void Parse_ReadsBranchesRingsAndComponents()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule branch = parser.Parse("C(O)N");
        QueryMolecule ring = parser.Parse("C1CC1");
        QueryMolecule components = parser.Parse("C.O");

        Assert.That(branch.GetAtomCount(), Is.EqualTo(3));
        Assert.That(branch.GetBondIdBetween(0, 1), Is.EqualTo(0));
        Assert.That(branch.GetBondIdBetween(0, 2), Is.EqualTo(1));
        Assert.That(ring.GetAtomCount(), Is.EqualTo(3));
        Assert.That(ring.GetBondCount(), Is.EqualTo(3));
        Assert.That(components.GetAtomCount(), Is.EqualTo(2));
        Assert.That(components.GetBondCount(), Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReadsDirectionalBondPrimitives()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule query = parser.Parse("C/N");

        Assert.That(query.GetBond(0).HasDirection, Is.True);
        Assert.That(query.GetBond(0).Direction, Is.EqualTo(BondDirection.Up));
    }

    [Test]
    public void ParseWithResult_ReturnsDiagnosticForInvalidPrimitive()
    {
        SmartsParser parser = new SmartsParser();

        SmartsParseResult result = parser.ParseWithResult("X");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("SMARTS_PARSE_ERROR"));
        Assert.That(result.ErrorMessage, Does.Contain("atom"));
    }

    [Test]
    public void ParseWithResult_ReturnsDiagnosticForUnclosedRing()
    {
        SmartsParser parser = new SmartsParser();

        SmartsParseResult result = parser.ParseWithResult("C1CC");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("ring"));
        Assert.That(result.Report.Get(0).Location.Context, Is.EqualTo("C1CC"));
    }

    [Test]
    public void Parse_ReadsAtomListsAndNegatedAtomicNumbers()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule list = parser.Parse("[#6,#7]");
        QueryMolecule negated = parser.Parse("[!#8]");

        Assert.That(list.GetAtom(0).UsesAtomicNumberList, Is.True);
        Assert.That(list.GetAtom(0).AtomicNumbers, Is.EqualTo(new int[] { 6, 7 }));
        Assert.That(list.GetAtom(0).Matches(new Atom(0, new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"))), Is.True);
        Assert.That(list.GetAtom(0).Matches(new Atom(1, new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"))), Is.False);
        Assert.That(negated.GetAtom(0).NegateAtomicNumber, Is.True);
        Assert.That(negated.GetAtom(0).Matches(new Atom(1, new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"))), Is.False);
    }

    [Test]
    public void Parse_ReadsAromaticAndAliphaticLogicalPrimitives()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule aromatic = parser.Parse("[a]");
        QueryMolecule aliphatic = parser.Parse("[A]");
        Atom aromaticCarbon = new Atom(0, new AtomSpec(6, 0, 0, 0, 1, 0, 0, true, "", "C"));
        Atom aliphaticCarbon = new Atom(1, new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));

        Assert.That(aromatic.GetAtom(0).Matches(aromaticCarbon), Is.True);
        Assert.That(aromatic.GetAtom(0).Matches(aliphaticCarbon), Is.False);
        Assert.That(aliphatic.GetAtom(0).Matches(aliphaticCarbon), Is.True);
        Assert.That(aliphatic.GetAtom(0).Matches(aromaticCarbon), Is.False);
    }

    [Test]
    public void Parse_ReadsDegreeRingAndValenceConstraints()
    {
        SmartsParser parser = new SmartsParser();
        Molecule cyclopropane = BuildCyclopropane();

        QueryMolecule query = parser.Parse("[#6;D2;R1;v4]");

        Assert.That(query.GetAtom(0).HasDegree, Is.True);
        Assert.That(query.GetAtom(0).Degree, Is.EqualTo(2));
        Assert.That(query.GetAtom(0).HasRingCount, Is.True);
        Assert.That(query.GetAtom(0).RingCount, Is.EqualTo(1));
        Assert.That(query.GetAtom(0).HasValence, Is.True);
        Assert.That(query.GetAtom(0).Valence, Is.EqualTo(4));
        Assert.That(query.GetAtom(0).MatchesAtomInMolecule(cyclopropane, 0), Is.True);
    }

    [Test]
    public void Parse_AssignsComponentGroupsFromDots()
    {
        SmartsParser parser = new SmartsParser();

        QueryMolecule query = parser.Parse("C.O.N");

        Assert.That(query.GetComponentCount(), Is.EqualTo(3));
        Assert.That(query.GetComponentId(0), Is.EqualTo(0));
        Assert.That(query.GetComponentId(1), Is.EqualTo(1));
        Assert.That(query.GetComponentId(2), Is.EqualTo(2));
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
}
