using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxSecurityScannerTests
{
    [Test]
    public void Scan_ReturnsNoDiagnosticsForSimplePackage()
    {
        PptxPackage package = CreateSimplePackage();

        PptxResult result = package.ScanSecurity();

        Assert.That(result.Succeeded(), Is.True);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Scan_ReportsExternalRelationships()
    {
        PptxPackage package = CreateSimplePackage();
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Hyperlink, "https://example.com", PptxRelationshipTargetMode.External);

        PptxResult result = PptxSecurityScannerCore.Scan(package);

        Assert.That(result.Succeeded(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("SEC001"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("https://example.com"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("/ppt/presentation.xml"));
    }

    [Test]
    public void Scan_ReportsMacroEnabledPresentationsAndVbaProjectParts()
    {
        PptxPackage package = CreateSimplePackage();
        package.GetPart("/ppt/presentation.xml").SetContentType(PptxContentTypes.MacroEnabledPresentation);
        package.AddPart("/ppt/vbaProject.bin", "application/vnd.ms-office.vbaProject", new byte[] { 1, 2, 3 });
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.VbaProject, "vbaProject.bin", PptxRelationshipTargetMode.Internal);

        PptxResult result = PptxSecurityScannerCore.Scan(package);

        Assert.That(result.Succeeded(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("SEC010"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("/ppt/vbaProject.bin"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("rId"));
    }

    [Test]
    public void Scan_ReportsActiveXOleEmbeddedPackageAndMediaParts()
    {
        PptxPackage package = CreateSimplePackage();
        package.AddPart("/ppt/activeX/activeX1.bin", "application/vnd.ms-office.activeX", new byte[] { 1 });
        package.AddPart("/ppt/embeddings/oleObject1.bin", "application/vnd.openxmlformats-officedocument.oleObject", new byte[] { 2 });
        package.AddPart("/ppt/embeddings/package1.bin", PptxContentTypes.Binary, new byte[] { 3 });
        package.AddPart("/ppt/media/video1.mp4", "video/mp4", new byte[] { 4 });

        PptxResult result = PptxSecurityScannerCore.Scan(package);

        Assert.That(result.Succeeded(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("SEC020"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("SEC030"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("SEC040"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("SEC050"));
    }

    [Test]
    public void Scan_ReportsSensitiveRelationshipTypes()
    {
        PptxPackage package = CreateSimplePackage();
        package.AddRelationship("/ppt/presentation.xml", "http://schemas.microsoft.com/office/2006/relationships/activeXControl", "activeX/activeX1.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.OleObject, "embeddings/oleObject1.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Package, "embeddings/package1.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Audio, "media/audio1.mp3", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Video, "media/video1.mp4", PptxRelationshipTargetMode.Internal);

        PptxResult result = PptxSecurityScannerCore.Scan(package);
        string diagnostics = result.FormatDiagnostics();

        Assert.That(result.Succeeded(), Is.True);
        Assert.That(diagnostics, Does.Contain("SEC020"));
        Assert.That(diagnostics, Does.Contain("SEC030"));
        Assert.That(diagnostics, Does.Contain("SEC040"));
        Assert.That(diagnostics, Does.Contain("SEC050"));
    }

    [Test]
    public void Scan_ReturnsErrorForNullPackage()
    {
        PptxResult result = PptxSecurityScannerCore.Scan(null!);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("SEC000"));
    }

    public static PptxPackage CreateSimplePackage()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("<p:presentation/>"));
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml", PptxRelationshipTargetMode.Internal);
        return package;
    }
}
