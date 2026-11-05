using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class DeterministicSerializerTests
{
    [Test]
    public void SerializeAndParse_RoundTripsExactStructuralHash()
    {
        Molecule molecule = BuildDecoratedMolecule();
        DeterministicSerializer serializer = new DeterministicSerializer();
        StructuralHash structuralHash = new StructuralHash();

        string text = serializer.Serialize(molecule);
        DeterministicParseResult result = serializer.Parse(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(molecule.GetAtomCount()));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(molecule.GetBondCount()));
        Assert.That(structuralHash.ComputeExact(result.Molecule), Is.EqualTo(structuralHash.ComputeExact(molecule)));
    }

    [Test]
    public void Serialize_IsStableUnderAtomAndBondRenumbering()
    {
        Molecule forward = BuildAsymmetricAmide();
        Molecule reversed = BuildAsymmetricAmideReversed();
        DeterministicSerializer serializer = new DeterministicSerializer();

        string forwardText = serializer.Serialize(forward);
        string reversedText = serializer.Serialize(reversed);

        Assert.That(forwardText, Is.EqualTo(reversedText));
    }

    [Test]
    public void Parse_RebuildsCanonicalAtomAndBondIds()
    {
        Molecule molecule = BuildAsymmetricAmideReversed();
        DeterministicSerializer serializer = new DeterministicSerializer();

        Molecule parsed = serializer.Deserialize(serializer.Serialize(molecule));

        int atomIndex = 0;
        while (atomIndex < parsed.Atoms.Count)
        {
            Assert.That(parsed.Atoms[atomIndex].Id, Is.EqualTo(atomIndex));
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < parsed.Bonds.Count)
        {
            Assert.That(parsed.Bonds[bondIndex].Id, Is.EqualTo(bondIndex));
            Assert.That(parsed.Bonds[bondIndex].BeginAtomId, Is.LessThanOrEqualTo(parsed.Bonds[bondIndex].EndAtomId));
            bondIndex = bondIndex + 1;
        }
    }

    [Test]
    public void SerializeAndParse_PreservesEscapedStringFields()
    {
        Molecule molecule = BuildDecoratedMolecule();
        DeterministicSerializer serializer = new DeterministicSerializer();

        DeterministicParseResult result = serializer.Parse(serializer.Serialize(molecule));

        Assert.That(result.Success, Is.True);
        Atom atom = FindAtomByAlias(result.Molecule, "C\talpha");
        Bond bond = FindBondByQueryValue(result.Molecule, "bond\tquery");

        Assert.That(atom.Id, Is.GreaterThanOrEqualTo(0));
        Assert.That(atom.Label, Is.EqualTo("line\none"));
        Assert.That(atom.QueryValue, Is.EqualTo("x\\y"));
        Assert.That(bond.Id, Is.GreaterThanOrEqualTo(0));
        Assert.That(bond.SourceFlags, Is.EqualTo("src\\flag"));
    }

    [Test]
    public void Serialize_ReturnsEmptyRecordForNullMolecule()
    {
        DeterministicSerializer serializer = new DeterministicSerializer();

        DeterministicParseResult result = serializer.Parse(serializer.Serialize(null));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReturnsFailureForInvalidHeader()
    {
        DeterministicSerializer serializer = new DeterministicSerializer();

        DeterministicParseResult result = serializer.Parse("BAD\nATOMS\t0\nBONDS\t0\nEND\n");

        Assert.That(result.Success, Is.False);
        Assert.That(result.LineNumber, Is.EqualTo(1));
        Assert.That(result.ErrorMessage, Does.Contain("version header"));
    }

    [Test]
    public void Parse_ReturnsFailureForBadIntegerAndInvalidBondReference()
    {
        DeterministicSerializer serializer = new DeterministicSerializer();

        DeterministicParseResult badInteger = serializer.Parse("MOLDET1\nATOMS\tx\nBONDS\t0\nEND\n");
        DeterministicParseResult badBond = serializer.Parse("MOLDET1\nATOMS\t1\nBONDS\t1\nATOM\t6\t0\t0\t0\t4\t0\t0\t0\t0\t\t\t\nBOND\t0\t3\t-1\t-1\t2\t0\t0\t0\t0\t0\tq\tflag\nEND\n");

        Assert.That(badInteger.Success, Is.False);
        Assert.That(badInteger.LineNumber, Is.EqualTo(2));
        Assert.That(badBond.Success, Is.False);
        Assert.That(badBond.ErrorMessage, Does.Contain("atom id"));
    }

    [Test]
    public void Parse_ReturnsFailureForIntegerOutsideIntRange()
    {
        DeterministicSerializer serializer = new DeterministicSerializer();

        DeterministicParseResult positive = serializer.Parse("MOLDET1\nATOMS\t999999999999999999999\nBONDS\t0\nEND\n");
        DeterministicParseResult negative = serializer.Parse("MOLDET1\nATOMS\t-999999999999999999999\nBONDS\t0\nEND\n");

        Assert.That(positive.Success, Is.False);
        Assert.That(positive.ErrorMessage, Does.Contain("out of range"));
        Assert.That(negative.Success, Is.False);
        Assert.That(negative.ErrorMessage, Does.Contain("out of range"));
    }

    [Test]
    public void ReaderWriterAliasesRoundTripAndNullTextReturnsEmptyMolecule()
    {
        DeterministicSerializer serializer = new DeterministicSerializer();
        Molecule source = BuildDecoratedMolecule();

        string text = serializer.Write(source);
        Molecule read = serializer.Read(text);
        Molecule deserializedNull = serializer.Deserialize(null);

        Assert.That(new StructuralHash().ComputeExact(read), Is.EqualTo(new StructuralHash().ComputeExact(source)));
        Assert.That(deserializedNull.Atoms, Is.Empty);
        Assert.That(deserializedNull.Bonds, Is.Empty);
        Assert.That(serializer.Parse(null).Success, Is.False);
    }

    [Test]
    public void Parse_RejectsIncompleteAtomBondAndEndRecords()
    {
        DeterministicSerializer serializer = new DeterministicSerializer();

        DeterministicParseResult atom = serializer.Parse("MOLDET1\nATOMS\t1\nBONDS\t0\nEND");
        DeterministicParseResult bond = serializer.Parse("MOLDET1\nATOMS\t0\nBONDS\t1\nEND");
        DeterministicParseResult end = serializer.Parse("MOLDET1\nATOMS\t0\nBONDS\t0\nMISSING");

        Assert.That(atom.Success, Is.False);
        Assert.That(atom.ErrorMessage, Does.Contain("atom record"));
        Assert.That(bond.Success, Is.False);
        Assert.That(bond.ErrorMessage, Does.Contain("bond record"));
        Assert.That(end.Success, Is.False);
        Assert.That(end.ErrorMessage, Does.Contain("END record"));
    }

    static Molecule BuildDecoratedMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        AtomSpec carbonSpec = new AtomSpec(6, 13, 1, 0, 3, 1, 7, true, "C\talpha", "line\none");
        carbonSpec.QueryOperation = QueryOperation.Equals;
        carbonSpec.QueryValue = "x\\y";
        AtomSpec oxygenSpec = new AtomSpec(8, 0, -1, 0, 1, 0, 0, false, "", "O");
        int carbon = builder.AddAtom(carbonSpec);
        int oxygen = builder.AddAtom(oxygenSpec);
        BondSpec bondSpec = BondSpec.Create(BondOrder.Double, true, BondStereo.Wedge, BondDirection.Up);
        bondSpec.IsDative = true;
        bondSpec.QueryOperation = QueryOperation.And;
        bondSpec.QueryValue = "bond\tquery";
        bondSpec.SourceFlags = "src\\flag";
        builder.AddBond(carbon, oxygen, bondSpec);
        return builder.Build();
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

    static Atom FindAtomByAlias(Molecule molecule, string alias)
    {
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].Alias == alias)
            {
                return molecule.Atoms[index];
            }

            index = index + 1;
        }

        return Atom.CreateEmpty();
    }

    static Bond FindBondByQueryValue(Molecule molecule, string queryValue)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            if (molecule.Bonds[index].QueryValue == queryValue)
            {
                return molecule.Bonds[index];
            }

            index = index + 1;
        }

        return Bond.CreateEmpty();
    }
}
