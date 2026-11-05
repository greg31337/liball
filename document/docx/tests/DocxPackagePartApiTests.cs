using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPackagePartApiTests
{
    [Test]
    public void EnumerationAndContentAccess_UseNormalizedNamesAndReturnCopies()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageParts parts = package;

        List<global::docx.DocxPart> enumerated = parts.GetParts();
        string xml = parts.GetPartXml("\\word\\document.xml");
        byte[] firstBytes = parts.GetPartBytes("word/document.xml");

        enumerated.Clear();
        firstBytes[0] = 0;

        Assert.That(package.PartCount(), Is.EqualTo(2));
        Assert.That(xml, Does.Contain("<w:document"));
        Assert.That(parts.GetPartBytes("/word/document.xml"), Is.EqualTo(Encoding.UTF8.GetBytes(xml)));
        Assert.That(parts.GetParts().Select(part => part.PartName), Is.EqualTo(new string[]
        {
            "/word/document.xml",
            "/_rels/.rels"
        }));
    }

    [Test]
    public void MutationOperations_AddReplaceAndRemoveXmlAndBinaryParts()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.IDocxPackageParts parts = package;
        byte[] sourceBytes = new byte[] { 1, 2, 3 };

        global::docx.DocxPart xmlPart = parts.AddXmlPartWithContent("custom\\item.xml", "application/custom+xml", "<item>one</item>");
        global::docx.DocxPart binaryPart = parts.AddBinaryPart("custom/data.bin", "application/octet-stream", sourceBytes);

        sourceBytes[0] = 9;

        Assert.That(xmlPart.PartName, Is.EqualTo("/custom/item.xml"));
        Assert.That(binaryPart.PartName, Is.EqualTo("/custom/data.bin"));
        Assert.That(parts.GetPartXml("custom/item.xml"), Is.EqualTo("<item>one</item>"));
        Assert.That(parts.GetPartBytes("/custom/data.bin"), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(package.GetContentTypes().GetContentType("/custom/item.xml"), Is.EqualTo("application/custom+xml"));
        Assert.That(package.GetContentTypes().GetContentType("/custom/data.bin"), Is.EqualTo("application/octet-stream"));

        byte[] replacementBytes = new byte[] { 4, 5, 6 };

        Assert.That(parts.ReplaceXmlPart("/custom/item.xml", global::docx.DocxConstants.ContentTypeXml, "<item>two</item>"), Is.True);
        Assert.That(parts.ReplaceBinaryPart("custom\\data.bin", "application/custom-binary", replacementBytes), Is.True);
        Assert.That(parts.ReplaceXmlPart("/custom/missing.xml", global::docx.DocxConstants.ContentTypeXml, "<missing />"), Is.False);

        replacementBytes[0] = 9;

        Assert.That(parts.GetPartXml("/custom/item.xml"), Is.EqualTo("<item>two</item>"));
        Assert.That(parts.GetPartBytes("/custom/data.bin"), Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(package.FindPart("/custom/data.bin")!.ContentType, Is.EqualTo("application/custom-binary"));

        Assert.That(parts.RemovePart("custom\\item.xml"), Is.True);
        Assert.That(parts.RemovePart("/custom/item.xml"), Is.False);
        Assert.That(package.ContainsPart("/custom/item.xml"), Is.False);
        Assert.That(package.GetContentTypes().GetContentType("/custom/item.xml"), Is.EqualTo(""));
    }

    [Test]
    public void XmlContentAccess_RecognizesExtensionsAndMediaTypesCaseInsensitively()
    {
        global::docx.IDocxPackageParts parts = new global::docx.DocxPackage();

        parts.AddXmlPartWithContent(
            "/custom/by-extension.XML",
            "application/octet-stream",
            "<extension>one</extension>");
        parts.AddXmlPartWithContent(
            "/custom/by-content-type.data",
            "APPLICATION/VND.EXAMPLE+XML",
            "<contentType>one</contentType>");

        Assert.That(parts.GetPartXml("custom\\by-extension.XML"), Is.EqualTo("<extension>one</extension>"));
        Assert.That(parts.GetPartXml("/custom/by-content-type.data"), Is.EqualTo("<contentType>one</contentType>"));

        Assert.That(parts.ReplaceXmlPart(
            "/custom/by-content-type.data",
            "Application/Vnd.Example+Xml",
            "<contentType>two</contentType>"), Is.True);
        Assert.That(parts.GetPartXml("/custom/by-content-type.data"), Is.EqualTo("<contentType>two</contentType>"));
    }

    [Test]
    public void OrphanInspection_TraversesInternalRelationshipsAndIgnoresExternalTargets()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageParts parts = package;

        parts.AddXmlPartWithContent(
            "/word/_rels/document.xml.rels",
            global::docx.DocxConstants.ContentTypeRelationships,
            CreateRelationshipsXml("media/image1.bin", "media/external.bin"));
        parts.AddBinaryPart("/word/media/image1.bin", "application/octet-stream", new byte[] { 1 });
        parts.AddXmlPartWithContent(
            "/word/media/_rels/image1.bin.rels",
            global::docx.DocxConstants.ContentTypeRelationships,
            CreateRelationshipsXml("../../custom/child.bin", "https://example.com/ignored.bin"));
        parts.AddBinaryPart("/custom/child.bin", "application/octet-stream", new byte[] { 2 });
        parts.AddBinaryPart("/word/media/external.bin", "application/octet-stream", new byte[] { 3 });
        parts.AddBinaryPart("/custom/orphan.bin", "application/octet-stream", new byte[] { 4 });

        List<global::docx.DocxPart> orphans = parts.GetOrphanParts();

        Assert.That(orphans.Select(part => part.PartName), Is.EqualTo(new string[]
        {
            "/word/media/external.bin",
            "/custom/orphan.bin"
        }));
        Assert.That(parts.IsOrphanPart("word\\media\\external.bin"), Is.True);
        Assert.That(parts.IsOrphanPart("/custom/orphan.bin"), Is.True);
        Assert.That(parts.IsOrphanPart("/word/document.xml"), Is.False);
        Assert.That(parts.IsOrphanPart("/word/_rels/document.xml.rels"), Is.False);
        Assert.That(parts.IsOrphanPart("/word/media/image1.bin"), Is.False);
        Assert.That(parts.IsOrphanPart("/word/media/_rels/image1.bin.rels"), Is.False);
        Assert.That(parts.IsOrphanPart("/custom/child.bin"), Is.False);
        Assert.That(parts.IsOrphanPart("/custom/missing.bin"), Is.False);

        orphans.Clear();

        Assert.That(parts.GetOrphanParts(), Has.Count.EqualTo(2));
    }

    [Test]
    public void OrphanInspection_MarksDescendantsOrphanedWhenTheirRootedSourceIsRemoved()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxPackage package = persistence.CreatePackage();
        global::docx.IDocxPackageParts parts = package;

        parts.AddXmlPartWithContent(
            "/word/_rels/document.xml.rels",
            global::docx.DocxConstants.ContentTypeRelationships,
            CreateRelationshipsXml("media/image1.bin", "https://example.com/image.bin"));
        parts.AddBinaryPart("/word/media/image1.bin", "application/octet-stream", new byte[] { 1 });

        Assert.That(parts.GetOrphanParts(), Is.Empty);

        parts.RemovePart("/word/document.xml");

        Assert.That(parts.GetOrphanParts().Select(part => part.PartName), Is.EqualTo(new string[]
        {
            "/word/_rels/document.xml.rels",
            "/word/media/image1.bin"
        }));
    }

    [Test]
    public void PartApi_RejectsInvalidContentRequestsAndArguments()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.IDocxPackageParts parts = package;

        parts.AddBinaryPart("/custom/data.bin", "application/octet-stream", new byte[] { 1 });

        Assert.Throws<ArgumentNullException>(() => parts.GetPartXml(null!));
        Assert.Throws<ArgumentNullException>(() => parts.GetPartBytes(null!));
        Assert.Throws<KeyNotFoundException>(() => parts.GetPartXml("/custom/missing.xml"));
        Assert.Throws<KeyNotFoundException>(() => parts.GetPartBytes("/custom/missing.bin"));
        Assert.Throws<InvalidOperationException>(() => parts.GetPartXml("/custom/data.bin"));
        Assert.Throws<ArgumentNullException>(() => parts.AddXmlPartWithContent(null!, global::docx.DocxConstants.ContentTypeXml, "<item />"));
        Assert.Throws<ArgumentNullException>(() => parts.AddXmlPartWithContent("/custom/item.xml", null!, "<item />"));
        Assert.Throws<ArgumentNullException>(() => parts.AddXmlPartWithContent("/custom/item.xml", global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentException>(() => parts.AddBinaryPart("/custom/empty.bin", "", new byte[0]));
        Assert.Throws<InvalidOperationException>(() => parts.AddBinaryPart("/custom/data.bin", "application/octet-stream", new byte[0]));
        Assert.Throws<ArgumentNullException>(() => parts.ReplaceXmlPart("/custom/item.xml", global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentNullException>(() => parts.ReplaceBinaryPart("/custom/data.bin", "application/octet-stream", null!));
        Assert.Throws<ArgumentNullException>(() => parts.RemovePart(null!));
        Assert.Throws<ArgumentNullException>(() => parts.IsOrphanPart(null!));
        Assert.Throws<ArgumentException>(() => parts.IsOrphanPart("/../invalid.bin"));
    }

    static string CreateRelationshipsXml(string internalTarget, string externalTarget)
    {
        return "<Relationships xmlns=\"" + global::docx.DocxConstants.NamespacePackageRelationships + "\">"
            + "<Relationship Id=\"rId1\" Type=\"urn:test:internal\" Target=\"" + internalTarget + "\" />"
            + "<Relationship Id=\"rId2\" Type=\"urn:test:external\" Target=\"" + externalTarget + "\" TargetMode=\"External\" />"
            + "</Relationships>";
    }
}
