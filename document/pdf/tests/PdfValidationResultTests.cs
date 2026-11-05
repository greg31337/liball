using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfValidationResultTests
{
    [Test]
    public void ValidationResultReportsBothSeveritiesAndEveryQueryMethod()
    {
        PdfValidationResult result = new PdfValidationResult();

        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.GetIssues(), Is.Empty);
        Assert.That(result.HasIssue("missing"), Is.False);

        result.AddWarning("WARN", "Review this", 4);
        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.CountWarnings(), Is.EqualTo(1));
        Assert.That(result.CountErrors(), Is.EqualTo(0));

        result.AddError("ERROR", "Fix this", 9);
        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.HasIssue("WARN"), Is.True);
        Assert.That(result.HasIssue("ERROR"), Is.True);
        Assert.That(result.CountWarnings(), Is.EqualTo(1));
        Assert.That(result.CountErrors(), Is.EqualTo(1));
        Assert.That(result.GetIssues()[0].Severity, Is.EqualTo(PdfValidationIssueSeverity.Warning));
        Assert.That(result.GetIssues()[1].Severity, Is.EqualTo(PdfValidationIssueSeverity.Error));
        Assert.That(result.GetIssues()[1].ObjectNumber, Is.EqualTo(9));
        Assert.Throws<ArgumentNullException>(() => result.HasIssue(null!));
    }

    [Test]
    public void PdfAPreparationResultReportsBothSeveritiesAndEveryQueryMethod()
    {
        PdfAPreparationResult result = new PdfAPreparationResult();

        result.AddWarning("WARN", "Review this");
        Assert.That(result.IsReady(), Is.True);
        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.CountWarnings(), Is.EqualTo(1));
        Assert.That(result.CountErrors(), Is.EqualTo(0));

        result.AddError("ERROR", "Fix this");
        Assert.That(result.IsReady(), Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.HasIssue("WARN"), Is.True);
        Assert.That(result.HasIssue("ERROR"), Is.True);
        Assert.That(result.HasIssue("missing"), Is.False);
        Assert.That(result.CountWarnings(), Is.EqualTo(1));
        Assert.That(result.CountErrors(), Is.EqualTo(1));
        Assert.That(result.GetIssues()[0].Severity, Is.EqualTo(PdfAPreparationIssueSeverity.Warning));
        Assert.That(result.GetIssues()[1].Severity, Is.EqualTo(PdfAPreparationIssueSeverity.Error));
        Assert.Throws<ArgumentNullException>(() => result.HasIssue(null!));
    }

    [Test]
    public void AccessibilityPreparationResultReportsBothSeveritiesAndEveryQueryMethod()
    {
        PdfAccessibilityPreparationResult result = new PdfAccessibilityPreparationResult();

        result.AddWarning("WARN", "Review this");
        Assert.That(result.IsReady(), Is.True);
        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.CountWarnings(), Is.EqualTo(1));
        Assert.That(result.CountErrors(), Is.EqualTo(0));

        result.AddError("ERROR", "Fix this");
        Assert.That(result.IsReady(), Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.HasIssue("WARN"), Is.True);
        Assert.That(result.HasIssue("ERROR"), Is.True);
        Assert.That(result.HasIssue("missing"), Is.False);
        Assert.That(result.CountWarnings(), Is.EqualTo(1));
        Assert.That(result.CountErrors(), Is.EqualTo(1));
        Assert.That(result.GetIssues()[0].Severity, Is.EqualTo(PdfAccessibilityPreparationIssueSeverity.Warning));
        Assert.That(result.GetIssues()[1].Severity, Is.EqualTo(PdfAccessibilityPreparationIssueSeverity.Error));
        Assert.Throws<ArgumentNullException>(() => result.HasIssue(null!));
    }

    [Test]
    public void IssueValuesValidateSeverityCodeMessageAndObjectNumber()
    {
        PdfValidationIssue validationError = new PdfValidationIssue(PdfValidationIssueSeverity.Error, "E", "error", 3);
        PdfValidationIssue validationWarning = new PdfValidationIssue(PdfValidationIssueSeverity.Warning, "W", "warning", 0);
        PdfAPreparationIssue pdfAError = new PdfAPreparationIssue(PdfAPreparationIssueSeverity.Error, "AE", "error");
        PdfAPreparationIssue pdfAWarning = new PdfAPreparationIssue(PdfAPreparationIssueSeverity.Warning, "AW", "warning");
        PdfAccessibilityPreparationIssue accessibilityError = new PdfAccessibilityPreparationIssue(PdfAccessibilityPreparationIssueSeverity.Error, "UE", "error");
        PdfAccessibilityPreparationIssue accessibilityWarning = new PdfAccessibilityPreparationIssue(PdfAccessibilityPreparationIssueSeverity.Warning, "UW", "warning");

        Assert.That(validationError.Severity, Is.EqualTo(PdfValidationIssueSeverity.Error));
        Assert.That(validationWarning.Severity, Is.EqualTo(PdfValidationIssueSeverity.Warning));
        Assert.That(pdfAError.Severity, Is.EqualTo(PdfAPreparationIssueSeverity.Error));
        Assert.That(pdfAWarning.Severity, Is.EqualTo(PdfAPreparationIssueSeverity.Warning));
        Assert.That(accessibilityError.Severity, Is.EqualTo(PdfAccessibilityPreparationIssueSeverity.Error));
        Assert.That(accessibilityWarning.Severity, Is.EqualTo(PdfAccessibilityPreparationIssueSeverity.Warning));

        Assert.Throws<ArgumentException>(() => new PdfValidationIssue((PdfValidationIssueSeverity)99, "E", "message", 0));
        Assert.Throws<ArgumentException>(() => new PdfAPreparationIssue((PdfAPreparationIssueSeverity)99, "E", "message"));
        Assert.Throws<ArgumentException>(() => new PdfAccessibilityPreparationIssue((PdfAccessibilityPreparationIssueSeverity)99, "E", "message"));
        Assert.Throws<ArgumentException>(() => new PdfValidationIssue(PdfValidationIssueSeverity.Error, string.Empty, "message", 0));
        Assert.Throws<ArgumentNullException>(() => new PdfAPreparationIssue(PdfAPreparationIssueSeverity.Error, "E", null!));
        Assert.Throws<ArgumentNullException>(() => new PdfAccessibilityPreparationIssue(PdfAccessibilityPreparationIssueSeverity.Error, null!, "message"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfValidationIssue(PdfValidationIssueSeverity.Error, "E", "message", -1));
    }
}
