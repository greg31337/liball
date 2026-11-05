namespace barcode.Tests;

public class ValidationIssueFactoryTests
{
    [Test]
    public void CreatePreservesCompleteIssueContext()
    {
        BarcodeValidationIssue issue = ValidationIssueFactory.Create(
            BarcodeValidationSeverity.Warning,
            "GS1_TEST_VALUE_INVALID",
            "The test value is invalid.",
            BarcodeStandards.Gs1GeneralSpecifications,
            "01",
            12,
            "fourteen digits",
            "ABC");

        Assert.Multiple(() =>
        {
            Assert.That(issue.Severity, Is.EqualTo(BarcodeValidationSeverity.Warning));
            Assert.That(issue.Code, Is.EqualTo("GS1_TEST_VALUE_INVALID"));
            Assert.That(issue.Message, Is.EqualTo("The test value is invalid."));
            Assert.That(issue.Standard, Is.EqualTo(BarcodeStandards.Gs1GeneralSpecifications));
            Assert.That(issue.Field, Is.EqualTo("01"));
            Assert.That(issue.Offset, Is.EqualTo(12));
            Assert.That(issue.Expected, Is.EqualTo("fourteen digits"));
            Assert.That(issue.Observed, Is.EqualTo("ABC"));
        });
    }

    [Test]
    public void CreateSupportsNonSourceIssuesAndOptionalComparisonData()
    {
        BarcodeValidationIssue issue = ValidationIssueFactory.Create(
            BarcodeValidationSeverity.Information,
            "PDF417_ASSEMBLY_COMPLETE",
            "The file is complete.",
            BarcodeStandards.Pdf417,
            "SegmentCount",
            -1,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(issue.Offset, Is.EqualTo(-1));
            Assert.That(issue.Expected, Is.Null);
            Assert.That(issue.Observed, Is.Null);
        });
    }

    [Test]
    public void CopyWithOffsetReturnsAnIndependentIssueWithMappedLocation()
    {
        BarcodeValidationIssue source = ValidationIssueFactory.Create(
            BarcodeValidationSeverity.Error,
            "GS1_SOURCE_INVALID",
            "The source value is invalid.",
            BarcodeStandards.Gs1DigitalLinkUriSyntax,
            "21",
            2,
            "valid data",
            "bad data");

        BarcodeValidationIssue copy = ValidationIssueFactory.CopyWithOffset(
            source,
            19);
        source.Code = "CHANGED";
        source.Offset = 99;

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(copy.Code, Is.EqualTo("GS1_SOURCE_INVALID"));
            Assert.That(copy.Offset, Is.EqualTo(19));
            Assert.That(copy.Field, Is.EqualTo("21"));
            Assert.That(copy.Expected, Is.EqualTo("valid data"));
            Assert.That(copy.Observed, Is.EqualTo("bad data"));
        });
    }

    [TestCase("lower_case")]
    [TestCase("_LEADING")]
    [TestCase("TRAILING_")]
    [TestCase("HAS-DASH")]
    public void CreateRejectsUnstableIssueCodes(string code)
    {
        Assert.That(
            () => ValidationIssueFactory.Create(
                BarcodeValidationSeverity.Error,
                code,
                "Message",
                "Standard",
                "Field",
                -1,
                null,
                null),
            Throws.TypeOf<BarcodeValidationException>());
    }

    [Test]
    public void CreateRejectsInvalidSeverityContextAndOffset()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => CreateInvalid((BarcodeValidationSeverity)99, "CODE", "Message", "Standard", "Field", -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CreateInvalid(BarcodeValidationSeverity.Error, "", "Message", "Standard", "Field", -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CreateInvalid(BarcodeValidationSeverity.Error, "CODE", "", "Standard", "Field", -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CreateInvalid(BarcodeValidationSeverity.Error, "CODE", "Message", "", "Field", -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CreateInvalid(BarcodeValidationSeverity.Error, "CODE", "Message", "Standard", "", -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CreateInvalid(BarcodeValidationSeverity.Error, "CODE", "Message", "Standard", "Field", -2),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ValidationIssueFactory.CopyWithOffset(null!, 0),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BarcodeValidationIssue CreateInvalid(
        BarcodeValidationSeverity severity,
        string code,
        string message,
        string standard,
        string field,
        int offset)
    {
        return ValidationIssueFactory.Create(
            severity,
            code,
            message,
            standard,
            field,
            offset,
            null,
            null);
    }
}
