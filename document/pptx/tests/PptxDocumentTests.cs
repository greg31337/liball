using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxDocumentTests
{
    [Test]
    public void Create_ReturnsDocumentWithPackageAccess()
    {
        PptxDocument document = PptxDocument.Create();
        PptxPackage package = document.GetPackage();

        Assert.That(package, Is.SameAs(document.Package));
        Assert.That(package.ContentTypes.GetDefaultContentType("rels"), Is.EqualTo(PptxContentTypes.Relationships));
        Assert.That(package.ContentTypes.GetDefaultContentType("xml"), Is.EqualTo(PptxContentTypes.Xml));
        Assert.That(package.IsDirty, Is.True);
    }

    [Test]
    public void OpenBytesAndSaveBytes_RoundTripPackage()
    {
        PptxDocument document = CreateSimpleDocument();

        byte[] bytes = document.SaveBytes();
        PptxDocument opened = PptxDocument.OpenBytes(bytes);

        Assert.That(opened.GetPackage().HasPart("/ppt/presentation.xml"), Is.True);
        Assert.That(opened.GetPackage().RootRelationships.Count(), Is.EqualTo(1));
        Assert.That(opened.GetPackage().Succeeded(), Is.True, opened.GetPackage().Diagnostics.FormatDiagnostics());
    }

    [Test]
    public void OpenStreamAndSaveStream_RoundTripPackage()
    {
        PptxDocument document = CreateSimpleDocument();
        using MemoryStream output = new MemoryStream();

        document.SaveStream(output);
        output.Position = 0;
        PptxDocument opened = PptxDocument.OpenStream(output);

        Assert.That(opened.GetPackage().HasPart("/ppt/presentation.xml"), Is.True);
    }

    [Test]
    public void OpenFileAndSaveFile_RoundTripPackage()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "document-roundtrip.pptx");
        PptxDocument document = CreateSimpleDocument();

        document.SaveFile(path);
        PptxDocument opened = PptxDocument.OpenFile(path);

        Assert.That(opened.GetPackage().HasPart("/ppt/presentation.xml"), Is.True);
    }

    [Test]
    public void ValidateAndRepair_DelegateToPackage()
    {
        PptxDocument document = CreateSimpleDocument();
        document.GetPackage().RemoveContentTypeOverride("/ppt/presentation.xml");

        PptxResult validation = document.Validate();
        PptxResult repair = document.Repair();

        Assert.That(validation.Failed(), Is.True);
        Assert.That(validation.FormatDiagnostics(), Does.Contain("PKGV034"));
        Assert.That(repair.Succeeded(), Is.True, repair.FormatDiagnostics());
        Assert.That(repair.FormatDiagnostics(), Does.Contain("PKGR010"));
        Assert.That(document.GetPackage().ContentTypes.GetOverrideContentType("/ppt/presentation.xml"), Is.EqualTo(PptxContentTypes.Presentation));
    }

    [Test]
    public void CloneDocument_CreatesIndependentPackageWithoutSavingOriginal()
    {
        PptxDocument document = CreateSimpleDocument();
        document.GetPackage().MarkClean();

        PptxDocument clone = document.CloneDocument();
        clone.GetPackage().AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("<p:sld/>"));

        Assert.That(document.GetPackage().HasPart("/ppt/slides/slide1.xml"), Is.False);
        Assert.That(clone.GetPackage().HasPart("/ppt/slides/slide1.xml"), Is.True);
        Assert.That(document.GetPackage().IsDirty, Is.False);
    }

    [Test]
    public void Clone_CopiesLoadedXmlDocumentIndependently()
    {
        PptxDocument document = CreateSimpleDocument();
        PptxPart part = document.GetPackage().GetPart("/ppt/presentation.xml");
        part.LoadXmlDocument();

        PptxDocument clone = document.Clone();
        PptxPart clonePart = clone.GetPackage().GetPart("/ppt/presentation.xml");
        PptxXml.SetAttributeValue(clonePart.XmlDocument.DocumentElement!, "", "", "changed", "1");

        Assert.That(part.XmlDocument.DocumentElement!.GetAttribute("changed"), Is.EqualTo(""));
        Assert.That(clonePart.XmlDocument.DocumentElement!.GetAttribute("changed"), Is.EqualTo("1"));
    }

    public static PptxDocument CreateSimpleDocument()
    {
        PptxDocument document = PptxDocument.Create();
        PptxPackage package = document.GetPackage();
        package.AddPart(
            "/ppt/presentation.xml",
            PptxContentTypes.Presentation,
            Encoding.UTF8.GetBytes("<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"/>"));
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml", PptxRelationshipTargetMode.Internal);
        return document;
    }
}
