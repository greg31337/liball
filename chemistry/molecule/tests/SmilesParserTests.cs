using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SmilesParserTests
{
    [Test]
    public void ReadAndParseWithResult_NormalizeNullAndEmptyInputs()
    {
        SmilesParser parser = new SmilesParser();

        Molecule read = parser.Read(null);
        SmilesParseResult parsed = parser.ParseWithResult("");

        Assert.That(read.GetAtomCount(), Is.Zero);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Molecule.GetAtomCount(), Is.Zero);
        Assert.That(parsed.Report.GetErrorCount(), Is.Zero);
    }

    [Test]
    public void Parse_ReadsAtomsBondsAndBranches()
    {
        SmilesParser parser = new SmilesParser();

        SmilesParseResult result = parser.ParseWithResult("CC(=O)N");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(4));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(result.Molecule.Atoms[2].AtomicNumber, Is.EqualTo(8));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void Parse_ReadsRingClosures()
    {
        SmilesParser parser = new SmilesParser();

        Molecule molecule = parser.Parse("C1CCCCC1");

        Assert.That(molecule.GetAtomCount(), Is.EqualTo(6));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(6));
        Assert.That(molecule.GetBondIdBetween(0, 5), Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Parse_ReadsDisconnectedComponents()
    {
        SmilesParser parser = new SmilesParser();

        Molecule molecule = parser.Parse("O.C");

        Assert.That(molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(0));
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(8));
        Assert.That(molecule.Atoms[1].AtomicNumber, Is.EqualTo(6));
    }

    [Test]
    public void Parse_ReadsBracketAtomIsotopeHydrogenChargeAndMap()
    {
        SmilesParser parser = new SmilesParser();

        Molecule molecule = parser.Parse("[13CH3+:7]");
        Atom atom = molecule.Atoms[0];

        Assert.That(atom.AtomicNumber, Is.EqualTo(6));
        Assert.That(atom.IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(atom.ExplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(atom.FormalCharge, Is.EqualTo(1));
        Assert.That(atom.AtomMapNumber, Is.EqualTo(7));
    }

    [Test]
    public void Parse_ReadsRepeatedChargeAndAromaticBracketHydrogen()
    {
        SmilesParser parser = new SmilesParser();

        Molecule cation = parser.Parse("[NH4++]");
        Molecule aromatic = parser.Parse("[nH]");

        Assert.That(cation.Atoms[0].AtomicNumber, Is.EqualTo(7));
        Assert.That(cation.Atoms[0].ExplicitHydrogenCount, Is.EqualTo(4));
        Assert.That(cation.Atoms[0].FormalCharge, Is.EqualTo(2));
        Assert.That(aromatic.Atoms[0].IsAromatic, Is.True);
        Assert.That(aromatic.Atoms[0].ExplicitHydrogenCount, Is.EqualTo(1));
    }

    [Test]
    public void Parse_ReadsDirectionalAndAromaticBonds()
    {
        SmilesParser parser = new SmilesParser();

        Molecule directional = parser.Parse("C/N");
        Molecule aromatic = parser.Parse("c:c");

        Assert.That(directional.Bonds[0].Direction, Is.EqualTo(BondDirection.Up));
        Assert.That(directional.Bonds[0].Stereo, Is.EqualTo(BondStereo.Wedge));
        Assert.That(aromatic.Atoms[0].IsAromatic, Is.True);
        Assert.That(aromatic.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(aromatic.Bonds[0].IsAromatic, Is.True);
    }

    [Test]
    public void Parse_PreservesInputOrderForWriterRoundTrip()
    {
        SmilesParser parser = new SmilesParser();
        SmilesWriter writer = new SmilesWriter();

        Molecule molecule = parser.Parse("C(O)(N)F");

        Assert.That(writer.Write(molecule), Is.EqualTo("C(O)(N)F"));
    }

    [Test]
    public void ParseWithResult_ReturnsFailureForUnclosedRingAndBranch()
    {
        SmilesParser parser = new SmilesParser();

        SmilesParseResult ring = parser.ParseWithResult("C1CC");
        SmilesParseResult branch = parser.ParseWithResult("C(O");

        Assert.That(ring.Success, Is.False);
        Assert.That(ring.ErrorMessage, Does.Contain("ring"));
        Assert.That(branch.Success, Is.False);
        Assert.That(branch.ErrorMessage, Does.Contain("branch"));
    }

    [Test]
    public void ParseWithResult_ReturnsFailureForUnsupportedAtom()
    {
        SmilesParser parser = new SmilesParser();

        SmilesParseResult result = parser.ParseWithResult("X");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("atom"));
    }

    [Test]
    public void ParseWithResult_AddsStructuredDiagnosticForStrictFailure()
    {
        SmilesParser parser = new SmilesParser();

        SmilesParseResult result = parser.ParseWithResult("C1CC");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("SMILES_PARSE_ERROR"));
        Assert.That(result.Report.Get(0).Location.CharacterIndex, Is.EqualTo(4));
        Assert.That(result.Report.Get(0).Location.Context, Is.EqualTo("C1CC"));
    }

    [Test]
    public void ParseWithRecovery_RemovesUnclosedRingClosure()
    {
        SmilesParser parser = new SmilesParser();

        SmilesParseResult result = parser.ParseWithRecovery("C1CC");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Recovered, Is.True);
        Assert.That(HasCode(result.Report, "SMILES_UNCLOSED_RING"), Is.True);
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(3));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(2));
    }

    [Test]
    public void ParseWithRecovery_RepairsBranchesAndTrailingBonds()
    {
        SmilesParser parser = new SmilesParser();

        SmilesParseResult unmatchedClose = parser.ParseWithRecovery("C)O");
        SmilesParseResult unclosedOpen = parser.ParseWithRecovery("C(O");
        SmilesParseResult trailingBond = parser.ParseWithRecovery("C=");

        Assert.That(unmatchedClose.Success, Is.True);
        Assert.That(HasCode(unmatchedClose.Report, "SMILES_UNMATCHED_BRANCH_CLOSE"), Is.True);
        Assert.That(unmatchedClose.Molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(unclosedOpen.Success, Is.True);
        Assert.That(HasCode(unclosedOpen.Report, "SMILES_UNCLOSED_BRANCH"), Is.True);
        Assert.That(unclosedOpen.Molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(trailingBond.Success, Is.True);
        Assert.That(HasCode(trailingBond.Report, "SMILES_TRAILING_BOND"), Is.True);
        Assert.That(trailingBond.Molecule.GetAtomCount(), Is.EqualTo(1));
    }

    [Test]
    public void ParseWithRecovery_SkipsUnsupportedCharactersAndWhitespace()
    {
        SmilesParser parser = new SmilesParser();

        SmilesParseResult result = parser.ParseWithRecovery("C X O");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(HasCode(result.Report, "SMILES_UNSUPPORTED_CHARACTER"), Is.True);
        Assert.That(result.RecoveredIssueCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void ParseWithRecovery_InsertsMissingBracketClose()
    {
        SmilesParser parser = new SmilesParser();

        SmilesParseResult result = parser.ParseWithRecovery("[NH4+");

        Assert.That(result.Success, Is.True);
        Assert.That(HasCode(result.Report, "SMILES_UNCLOSED_BRACKET"), Is.True);
        Assert.That(result.Molecule.Atoms[0].AtomicNumber, Is.EqualTo(7));
        Assert.That(result.Molecule.Atoms[0].ExplicitHydrogenCount, Is.EqualTo(4));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
    }

    static bool HasCode(MoleculeReport report, string code)
    {
        int index = 0;
        while (index < report.GetCount())
        {
            if (report.Get(index).Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
