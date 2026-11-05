using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPackageContentTypeApiTests
{
    [Test]
    public void DefaultOperations_CreateFindUpdateEnumerateAndRemove()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.IDocxPackageContentTypes contentTypes = package;

        global::docx.DocxContentTypeDefault created = contentTypes.CreateDefaultContentType(".XML", global::docx.DocxConstants.ContentTypeXml);
        List<global::docx.DocxContentTypeDefault> declarations = contentTypes.GetContentTypeDefaults();

        declarations.Clear();

        Assert.That(created.Extension, Is.EqualTo("XML"));
        Assert.That(contentTypes.FindDefaultContentType("xml"), Is.SameAs(created));
        Assert.That(contentTypes.FindDefaultContentType(".xMl"), Is.SameAs(created));
        Assert.That(contentTypes.FindDefaultContentType("png"), Is.Null);
        Assert.That(contentTypes.GetContentTypeDefaults(), Has.Count.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => contentTypes.CreateDefaultContentType("xml", "application/duplicate"));

        Assert.That(contentTypes.UpdateDefaultContentType(".xml", "application/updated+xml"), Is.True);
        Assert.That(created.ContentType, Is.EqualTo("application/updated+xml"));
        Assert.That(contentTypes.UpdateDefaultContentType("png", global::docx.DocxConstants.ContentTypePng), Is.False);
        Assert.That(contentTypes.RemoveDefaultContentType("xMl"), Is.True);
        Assert.That(contentTypes.RemoveDefaultContentType("xml"), Is.False);
        Assert.That(contentTypes.FindDefaultContentType("xml"), Is.Null);
    }

    [Test]
    public void OverrideOperations_CreateFindUpdateEnumerateAndRemoveWithNormalizedNames()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.IDocxPackageContentTypes contentTypes = package;

        global::docx.DocxContentTypeOverride created = contentTypes.CreateOverrideContentType(
            "word\\document.xml",
            global::docx.DocxConstants.ContentTypeMainDocument);
        List<global::docx.DocxContentTypeOverride> declarations = contentTypes.GetContentTypeOverrides();

        declarations.Clear();

        Assert.That(created.PartName, Is.EqualTo("/word/document.xml"));
        Assert.That(contentTypes.FindOverrideContentType("word/document.xml"), Is.SameAs(created));
        Assert.That(contentTypes.FindOverrideContentType("/word/missing.xml"), Is.Null);
        Assert.That(contentTypes.GetContentTypeOverrides(), Has.Count.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => contentTypes.CreateOverrideContentType(
            "/word/document.xml",
            global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument));

        Assert.That(contentTypes.UpdateOverrideContentType(
            "word\\document.xml",
            global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument), Is.True);
        Assert.That(created.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument));
        Assert.That(contentTypes.UpdateOverrideContentType(
            "/word/missing.xml",
            global::docx.DocxConstants.ContentTypeXml), Is.False);
        Assert.That(contentTypes.RemoveOverrideContentType("word/document.xml"), Is.True);
        Assert.That(contentTypes.RemoveOverrideContentType("/word/document.xml"), Is.False);
        Assert.That(contentTypes.FindOverrideContentType("/word/document.xml"), Is.Null);
    }

    [Test]
    public void ResolveContentType_PrefersOverrideThenFallsBackToCaseInsensitiveDefault()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.IDocxPackageContentTypes contentTypes = package;

        contentTypes.CreateDefaultContentType("xml", global::docx.DocxConstants.ContentTypeXml);

        Assert.That(contentTypes.ResolveContentType("WORD/DOCUMENT.XML"), Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
        Assert.That(contentTypes.ResolveContentType("/custom/no-extension"), Is.EqualTo(""));

        contentTypes.CreateOverrideContentType("word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument);

        Assert.That(contentTypes.ResolveContentType("word\\document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));

        contentTypes.UpdateOverrideContentType("/word/document.xml", global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument);

        Assert.That(contentTypes.ResolveContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument));

        contentTypes.RemoveOverrideContentType("/word/document.xml");

        Assert.That(contentTypes.ResolveContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));

        contentTypes.RemoveDefaultContentType("xml");

        Assert.That(contentTypes.ResolveContentType("/word/document.xml"), Is.EqualTo(""));
    }

    [Test]
    public void ContentTypeMutations_RoundTripThroughPublicPackagePersistence()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageContentTypes contentTypes = package;
        global::docx.IDocxPackageParts parts = package;
        MemoryStream stream = new MemoryStream();

        contentTypes.CreateDefaultContentType("dat", "application/x-data");
        global::docx.DocxPart dataPart = parts.AddBinaryPart("custom/special.dat", "application/x-data", new byte[] { 1, 2, 3 });
        contentTypes.CreateOverrideContentType("custom/special.dat", "application/x-special-data");

        Assert.That(dataPart.ContentType, Is.EqualTo("application/x-special-data"));

        persistence.SavePackage(package, stream, new global::docx.DocxSaveOptions());

        stream.Position = 0;

        global::docx.DocxPackage reopenedPackage = persistence.OpenPackage(stream, new global::docx.DocxLoadOptions());
        global::docx.IDocxPackageContentTypes reopened = reopenedPackage;

        Assert.That(reopened.FindDefaultContentType("dat")!.ContentType, Is.EqualTo("application/x-data"));
        Assert.That(reopened.FindOverrideContentType("/custom/special.dat")!.ContentType, Is.EqualTo("application/x-special-data"));
        Assert.That(reopened.ResolveContentType("/custom/ordinary.dat"), Is.EqualTo("application/x-data"));
        Assert.That(reopened.ResolveContentType("/custom/special.dat"), Is.EqualTo("application/x-special-data"));
        Assert.That(reopenedPackage.FindPart("/custom/special.dat")!.ContentType, Is.EqualTo("application/x-special-data"));
    }

    [Test]
    public void ContentTypeApi_RejectsNullEmptyAndMalformedArguments()
    {
        global::docx.IDocxPackageContentTypes contentTypes = new global::docx.DocxPackage();

        Assert.Throws<ArgumentNullException>(() => contentTypes.FindDefaultContentType(null!));
        Assert.Throws<ArgumentException>(() => contentTypes.FindDefaultContentType(""));
        Assert.Throws<ArgumentException>(() => contentTypes.FindDefaultContentType("word/xml"));
        Assert.Throws<ArgumentNullException>(() => contentTypes.FindOverrideContentType(null!));
        Assert.Throws<ArgumentException>(() => contentTypes.FindOverrideContentType("/word/../document.xml"));
        Assert.Throws<ArgumentNullException>(() => contentTypes.CreateDefaultContentType(null!, "application/xml"));
        Assert.Throws<ArgumentNullException>(() => contentTypes.CreateDefaultContentType("xml", null!));
        Assert.Throws<ArgumentException>(() => contentTypes.CreateDefaultContentType("xml", ""));
        Assert.Throws<ArgumentNullException>(() => contentTypes.UpdateDefaultContentType("xml", null!));
        Assert.Throws<ArgumentException>(() => contentTypes.UpdateDefaultContentType("xml", ""));
        Assert.Throws<ArgumentNullException>(() => contentTypes.RemoveDefaultContentType(null!));
        Assert.Throws<ArgumentNullException>(() => contentTypes.CreateOverrideContentType(null!, "application/xml"));
        Assert.Throws<ArgumentNullException>(() => contentTypes.CreateOverrideContentType("/word/document.xml", null!));
        Assert.Throws<ArgumentException>(() => contentTypes.CreateOverrideContentType("/word/document.xml", ""));
        Assert.Throws<ArgumentNullException>(() => contentTypes.UpdateOverrideContentType("/word/document.xml", null!));
        Assert.Throws<ArgumentException>(() => contentTypes.UpdateOverrideContentType("/word/document.xml", ""));
        Assert.Throws<ArgumentNullException>(() => contentTypes.RemoveOverrideContentType(null!));
        Assert.Throws<ArgumentNullException>(() => contentTypes.ResolveContentType(null!));
        Assert.Throws<ArgumentException>(() => contentTypes.ResolveContentType("/word/../document.xml"));
    }
}
