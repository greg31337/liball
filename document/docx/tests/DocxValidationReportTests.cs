using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxValidationReportTests
{
    [Test]
    public void DocxError_StoresSeverityCodeMessageAndLocation()
    {
        global::docx.DocxError error = new global::docx.DocxError(
            global::docx.DocxErrorSeverity.Error,
            "opc.missing-content-types",
            "Missing [Content_Types].xml.",
            "/[Content_Types].xml");

        Assert.That(error.Severity, Is.EqualTo(global::docx.DocxErrorSeverity.Error));
        Assert.That(error.Code, Is.EqualTo("opc.missing-content-types"));
        Assert.That(error.Message, Is.EqualTo("Missing [Content_Types].xml."));
        Assert.That(error.Location, Is.EqualTo("/[Content_Types].xml"));
        Assert.That(error.IsError(), Is.True);
        Assert.That(error.IsWarning(), Is.False);
    }

    [Test]
    public void ValidationReport_AddsDiagnosticsAndReportsSeverityState()
    {
        global::docx.DocxValidationReport report = new global::docx.DocxValidationReport();

        report.Add(global::docx.DocxErrorSeverity.Information, "opc.opened", "Package opened.", "/");
        report.Add(global::docx.DocxErrorSeverity.Warning, "opc.unknown-part", "Unknown part preserved.", "/custom/item1.xml");

        Assert.That(report.Count(), Is.EqualTo(2));
        Assert.That(report.HasWarnings(), Is.True);
        Assert.That(report.HasErrors(), Is.False);
        Assert.That(report.ContainsCode("opc.unknown-part"), Is.True);
        Assert.That(report.ContainsCode("opc.missing-content-types"), Is.False);

        report.AddError(new global::docx.DocxError(
            global::docx.DocxErrorSeverity.Error,
            "opc.missing-content-types",
            "Missing [Content_Types].xml.",
            "/[Content_Types].xml"));

        Assert.That(report.Count(), Is.EqualTo(3));
        Assert.That(report.HasErrors(), Is.True);
        Assert.That(report.ContainsCode("opc.missing-content-types"), Is.True);
    }

    [Test]
    public void ValidationReport_RejectsNullDiagnostics()
    {
        global::docx.DocxValidationReport report = new global::docx.DocxValidationReport();

        Assert.Throws<ArgumentNullException>(() => report.AddError(null!));
        Assert.Throws<ArgumentNullException>(() => report.ContainsCode(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxError(global::docx.DocxErrorSeverity.Error, null!, "message", "/"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxError(global::docx.DocxErrorSeverity.Error, "code", null!, "/"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxError(global::docx.DocxErrorSeverity.Error, "code", "message", null!));
    }
}
