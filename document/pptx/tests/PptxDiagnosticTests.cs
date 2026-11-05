using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxDiagnosticTests
{
    [Test]
    public void Diagnostic_NormalizesOptionalTextAndFormatsLocation()
    {
        PptxDiagnostic diagnostic = new PptxDiagnostic(
            PptxDiagnosticSeverity.Warning,
            "PPTX001",
            "Relationship target is external.",
            "/ppt/slides/slide1.xml",
            "rId7",
            "/p:sld/p:cSld");

        Assert.That(diagnostic.Severity, Is.EqualTo(PptxDiagnosticSeverity.Warning));
        Assert.That(diagnostic.Code, Is.EqualTo("PPTX001"));
        Assert.That(diagnostic.Message, Is.EqualTo("Relationship target is external."));
        Assert.That(diagnostic.HasLocation(), Is.True);
        Assert.That(diagnostic.FormatLocation(), Is.EqualTo("/ppt/slides/slide1.xml rId7 /p:sld/p:cSld"));
        Assert.That(diagnostic.Format(), Is.EqualTo("Warning PPTX001: Relationship target is external. (/ppt/slides/slide1.xml rId7 /p:sld/p:cSld)"));
    }

    [Test]
    public void Diagnostic_AllowsEmptyLocationFields()
    {
        PptxDiagnostic diagnostic = PptxDiagnostic.CreateInfo("PPTX000", "No issues.", null!, null!, null!);

        Assert.That(diagnostic.PartName, Is.EqualTo(""));
        Assert.That(diagnostic.RelationshipId, Is.EqualTo(""));
        Assert.That(diagnostic.XmlPath, Is.EqualTo(""));
        Assert.That(diagnostic.HasLocation(), Is.False);
        Assert.That(diagnostic.Format(), Is.EqualTo("Info PPTX000: No issues."));
    }

    [Test]
    public void Result_TracksSuccessUntilAnErrorDiagnosticIsAdded()
    {
        PptxResult result = new PptxResult();

        Assert.That(result.Count(), Is.EqualTo(0));
        Assert.That(result.Succeeded(), Is.True);
        Assert.That(result.Failed(), Is.False);

        PptxDiagnostic warning = result.AddWarning("PPTX010", "Unknown extension preserved.", "/ppt/presentation.xml", "", "/p:presentation/p:extLst");

        Assert.That(warning.IsError(), Is.False);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.Succeeded(), Is.True);

        PptxDiagnostic error = result.AddError("PPTX011", "Missing presentation relationship.", "/_rels/.rels", "", "");

        Assert.That(error.IsError(), Is.True);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.Succeeded(), Is.False);
        Assert.That(result.Failed(), Is.True);
    }

    [Test]
    public void Result_FormatsDiagnosticsInInsertionOrder()
    {
        PptxResult result = new PptxResult();

        result.AddInfo("PPTX020", "Loaded package.", "", "", "");
        result.AddError("PPTX021", "Broken target.", "/ppt/presentation.xml", "rId2", "");

        Assert.That(
            result.FormatDiagnostics(),
            Is.EqualTo("Info PPTX020: Loaded package.\nError PPTX021: Broken target. (/ppt/presentation.xml rId2)"));
    }
}
