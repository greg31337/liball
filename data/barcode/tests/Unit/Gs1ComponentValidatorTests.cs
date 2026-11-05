namespace barcode.Tests;

public class Gs1ComponentValidatorTests
{
    [Test]
    public void CheckDigitFailureUsesTheCheckCharacterSourceOffset()
    {
        Gs1ElementStringParseResult result =
            Gs1ElementStringParser.ParseElementString(
                "0109506000134353");

        Assert.Multiple(() =>
        {
            Assert.That(result.Elements, Has.Length.EqualTo(1));
            AssertIssue(
                result,
                "GS1_LINTER_INCORRECT_CHECK_DIGIT",
                15,
                "3",
                "01");
            Assert.That(result.Issues, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void DateAndTimeLintersAccountForComponentOffsets()
    {
        Gs1ElementStringParseResult date =
            Gs1ElementStringParser.ParseElementString("4326250230");
        Gs1ElementStringParseResult time =
            Gs1ElementStringParser.ParseElementString("70032501012360");
        Gs1ElementStringParseResult dayZero =
            Gs1ElementStringParser.ParseElementString("17250100");

        Assert.Multiple(() =>
        {
            AssertIssue(
                date,
                "GS1_LINTER_ILLEGAL_DAY",
                8,
                "30",
                "4326");
            AssertIssue(
                time,
                "GS1_LINTER_ILLEGAL_MINUTE",
                12,
                "60",
                "7003");
            Assert.That(dayZero.Success, Is.True);
        });
    }

    [Test]
    public void CountryCurrencyAndCoordinateTablesAreApplied()
    {
        Gs1ElementStringParseResult country =
            Gs1ElementStringParser.ParseElementString("422000");
        Gs1ElementStringParseResult currency =
            Gs1ElementStringParser.ParseElementString("39120001");
        Gs1ElementStringParseResult coordinate =
            Gs1ElementStringParser.ParseElementString(
                "430918000000003600000001");

        Assert.Multiple(() =>
        {
            AssertIssue(
                country,
                "GS1_LINTER_NOT_ISO3166",
                3,
                "000",
                "422");
            AssertIssue(
                currency,
                "GS1_LINTER_NOT_ISO4217",
                4,
                "000",
                "3912");
            AssertIssue(
                coordinate,
                "GS1_LINTER_INVALID_LONGITUDE",
                14,
                "3600000001",
                "4309");
        });
    }

    [Test]
    public void IbanMediaAndPackageFailuresRetainTheirExactSpans()
    {
        Gs1ElementStringParseResult iban =
            Gs1ElementStringParser.ParseElementString(
                "8007BE71096123456760");
        Gs1ElementStringParseResult media =
            Gs1ElementStringParser.ParseElementString("724111");
        Gs1ElementStringParseResult package =
            Gs1ElementStringParser.ParseElementString("70410");

        Assert.Multiple(() =>
        {
            AssertIssue(
                iban,
                "GS1_LINTER_INCORRECT_IBAN_CHECKSUM",
                6,
                "71",
                "8007");
            AssertIssue(
                media,
                "GS1_LINTER_INVALID_MEDIA_TYPE",
                4,
                "11",
                "7241");
            AssertIssue(
                package,
                "GS1_LINTER_INVALID_PACKAGE_TYPE",
                4,
                "0",
                "7041");
        });
    }

    [Test]
    public void CouponAndSpecializedLintersAreAppliedByDictionaryName()
    {
        Gs1ElementStringParseResult coupon =
            Gs1ElementStringParser.ParseElementString("81107");
        Gs1ElementStringParseResult positiveOffer =
            Gs1ElementStringParser.ParseElementString("81122");
        Gs1ElementStringParseResult pieceOfTotal =
            Gs1ElementStringParser.ParseElementString(
                "8006095060001343520201");
        Gs1ElementStringParseResult position =
            Gs1ElementStringParser.ParseElementString("72583/2");
        Gs1ElementStringParseResult alphaCheck =
            Gs1ElementStringParser.ParseElementString("801333");

        Assert.Multiple(() =>
        {
            AssertIssue(
                coupon,
                "GS1_LINTER_COUPON_INVALID_GCP_LENGTH",
                4,
                "7",
                "8110");
            AssertIssue(
                positiveOffer,
                "GS1_LINTER_COUPON_INVALID_FORMAT_CODE",
                4,
                "2",
                "8112");
            AssertIssue(
                pieceOfTotal,
                "GS1_LINTER_PIECE_NUMBER_EXCEEDS_TOTAL",
                18,
                "0201",
                "8006");
            AssertIssue(
                position,
                "GS1_LINTER_POSITION_EXCEEDS_END",
                4,
                "3/2",
                "7258");
            AssertIssue(
                alphaCheck,
                "GS1_LINTER_INCORRECT_CHECK_PAIR",
                4,
                "33",
                "8013");
        });
    }

    [Test]
    public void CharacterSetFailureSuppressesDependentSemanticLinters()
    {
        Gs1ElementStringParseResult result =
            Gs1ElementStringParser.ParseElementString("17A60101");

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            AssertIssue(
                result,
                "GS1_INVALID_NUMERIC_CHARACTER",
                2,
                "character 'A'",
                "17");
        });
    }

    [Test]
    public void ValidSemanticComponentsDoNotAddIssues()
    {
        string source =
            "0109506000134352"
            + "17250100"
            + "422840\u001d"
            + "724199\u001d"
            + "7041X20";

        Gs1ElementStringParseResult result =
            Gs1ElementStringParser.ParseElementString(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Elements, Has.Length.EqualTo(5));
        });
    }

    static void AssertIssue(
        Gs1ElementStringParseResult result,
        string code,
        int offset,
        string observed,
        string field)
    {
        BarcodeValidationIssue issue = result.Issues.Single(
            candidate => candidate.Code == code);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(issue.Severity,
                Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(issue.Offset, Is.EqualTo(offset));
            Assert.That(issue.Observed, Is.EqualTo(observed));
            Assert.That(issue.Field, Is.EqualTo(field));
            Assert.That(issue.Standard,
                Is.EqualTo(Gs1ElementStringParser.StandardName));
            Assert.That(issue.Message, Is.Not.Empty);
            Assert.That(issue.Expected, Is.Not.Empty);
        });
    }
}
