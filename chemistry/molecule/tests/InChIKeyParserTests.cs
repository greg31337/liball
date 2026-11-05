using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class InChIKeyParserTests
{
    [Test]
    public void Parse_ValidKeyExtractsBlocksVersionAndProtonation()
    {
        InChIKeyParser parser = new InChIKeyParser();

        InChIKeyRecord record = parser.Parse("BSYNRYMUTXBXSQ-UHFFFAOYSA-N");

        Assert.That(record.Success, Is.True);
        Assert.That(record.Report.GetErrorCount(), Is.EqualTo(0));
        Assert.That(record.NormalizedText, Is.EqualTo("BSYNRYMUTXBXSQ-UHFFFAOYSA-N"));
        Assert.That(record.FirstBlock, Is.EqualTo("BSYNRYMUTXBXSQ"));
        Assert.That(record.SecondBlock, Is.EqualTo("UHFFFAOYSA"));
        Assert.That(record.RemainingHashBlock, Is.EqualTo("UHFFFAOY"));
        Assert.That(record.VersionBlock, Is.EqualTo("SA"));
        Assert.That(record.VersionCharacter, Is.EqualTo("S"));
        Assert.That(record.StandardCharacter, Is.EqualTo("A"));
        Assert.That(record.ProtonationIndicator, Is.EqualTo("N"));
        Assert.That(record.HasFirstBlock(), Is.True);
        Assert.That(record.HasSecondBlock(), Is.True);
        Assert.That(record.HasVersion(), Is.True);
        Assert.That(record.HasProtonationIndicator(), Is.True);
    }

    [Test]
    public void Parse_TrimsWhitespaceBeforeValidation()
    {
        InChIKeyParser parser = new InChIKeyParser();

        InChIKeyRecord record = parser.Parse("  XLYOFNOQVPJJNP-UHFFFAOYSA-N\n");

        Assert.That(record.Success, Is.True);
        Assert.That(record.NormalizedText, Is.EqualTo("XLYOFNOQVPJJNP-UHFFFAOYSA-N"));
        Assert.That(record.FirstBlock, Is.EqualTo("XLYOFNOQVPJJNP"));
    }

    [Test]
    public void Parse_ReportsInvalidLength()
    {
        InChIKeyParser parser = new InChIKeyParser();

        InChIKeyRecord record = parser.Parse("XLYOFNOQVPJJNP");

        Assert.That(record.Success, Is.False);
        Assert.That(record.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(record.Report.Get(0).Code, Is.EqualTo("INCHIKEY_LENGTH"));
    }

    [Test]
    public void ParseAndValidate_ReportNullAndWhitespaceAsEmpty()
    {
        InChIKeyParser parser = new InChIKeyParser();

        InChIKeyRecord nullRecord = parser.Parse(null);
        InChIKeyRecord whitespaceRecord = parser.Parse("  \n");

        Assert.That(nullRecord.Success, Is.False);
        Assert.That(nullRecord.Report.Get(0).Code, Is.EqualTo("INCHIKEY_EMPTY"));
        Assert.That(whitespaceRecord.Success, Is.False);
        Assert.That(whitespaceRecord.Report.Get(0).Code, Is.EqualTo("INCHIKEY_EMPTY"));
        Assert.That(parser.Validate(null), Is.False);
    }

    [Test]
    public void Parse_ReportsInvalidSeparators()
    {
        InChIKeyParser parser = new InChIKeyParser();

        InChIKeyRecord record = parser.Parse("XLYOFNOQVPJJNPAUHFFFAOYSAAN");

        Assert.That(record.Success, Is.False);
        Assert.That(record.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(record.Report.Get(0).Code, Is.EqualTo("INCHIKEY_SEPARATOR"));
    }

    [Test]
    public void Parse_ReportsInvalidCharacters()
    {
        InChIKeyParser parser = new InChIKeyParser();

        InChIKeyRecord record = parser.Parse("XLYOFNOQVPJJNP-UHFFFAOYS1-N");

        Assert.That(record.Success, Is.False);
        Assert.That(record.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(record.Report.Get(0).Code, Is.EqualTo("INCHIKEY_CHARACTER"));
        Assert.That(record.Report.Get(0).Location.ColumnNumber, Is.EqualTo(25));
    }

    [Test]
    public void Validate_ReturnsBooleanForShape()
    {
        InChIKeyParser parser = new InChIKeyParser();

        Assert.That(parser.Validate("BSYNRYMUTXBXSQ-UHFFFAOYSA-N"), Is.True);
        Assert.That(parser.Validate("bsynrymuxbxsq-uhfffaoysa-n"), Is.False);
    }

    [Test]
    public void Record_ComparesBlocksAndReturnsNamedBlocks()
    {
        InChIKeyParser parser = new InChIKeyParser();
        InChIKeyRecord first = parser.Parse("BSYNRYMUTXBXSQ-UHFFFAOYSA-N");
        InChIKeyRecord second = parser.Parse("BSYNRYMUTXBXSQ-UHFFFAOYSA-M");

        Assert.That(first.HasSameFirstBlock(second), Is.True);
        Assert.That(first.HasSameSecondBlock(second), Is.True);
        Assert.That(first.GetBlock("connectivity"), Is.EqualTo("BSYNRYMUTXBXSQ"));
        Assert.That(first.GetBlock("second"), Is.EqualTo("UHFFFAOYSA"));
        Assert.That(first.GetBlock("remaining"), Is.EqualTo("UHFFFAOY"));
        Assert.That(first.GetBlock("version"), Is.EqualTo("SA"));
        Assert.That(first.GetBlock("protonation"), Is.EqualTo("N"));
    }

    [Test]
    public void FormatDetection_InChIKeyRequiresUppercaseLetterBlocks()
    {
        FormatDetection detector = new FormatDetection();

        Assert.That(detector.IsLikelyInChIKey("BSYNRYMUTXBXSQ-UHFFFAOYSA-N"), Is.True);
        Assert.That(detector.IsLikelyInChIKey("BSYNRYMUTXBXSQ-UHFFFAOYS1-N"), Is.False);
        Assert.That(detector.Detect("BSYNRYMUTXBXSQ-UHFFFAOYSA-N", "").Kind, Is.EqualTo(FormatKind.InChIKey));
        Assert.That(detector.Detect("BSYNRYMUTXBXSQ-UHFFFAOYS1-N", "").Kind, Is.Not.EqualTo(FormatKind.InChIKey));
    }
}
