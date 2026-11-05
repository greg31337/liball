using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxRelationshipSetTests
{
    [Test]
    public void ParsePackageRelationships_ReadsRootRelationshipFile()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePackageRelationships(CreatePackageRelationshipsXml());

        Assert.That(relationships.SourcePartName, Is.EqualTo("/"));
        Assert.That(relationships.Count(), Is.EqualTo(3));
        Assert.That(relationships.Relationships[0].Id, Is.EqualTo("rId1"));
        Assert.That(relationships.Relationships[0].Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeOfficeDocument));
        Assert.That(relationships.Relationships[0].Target, Is.EqualTo("word/document.xml"));
        Assert.That(relationships.Relationships[0].TargetMode, Is.EqualTo(""));
        Assert.That(relationships.Relationships[0].IsExternal, Is.False);
    }

    [Test]
    public void ParsePackageRelationships_PreservesExternalTargetMode()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePackageRelationships(CreatePackageRelationshipsXml());
        global::docx.DocxRelationship? relationship = relationships.FindById("rIdExternal");

        Assert.That(relationship, Is.Not.Null);
        Assert.That(relationship!.Target, Is.EqualTo("https://example.com/"));
        Assert.That(relationship.TargetMode, Is.EqualTo("External"));
        Assert.That(relationship.IsExternal, Is.True);
    }

    [Test]
    public void ParsePackageRelationships_StreamReadsRelationshipsAndLeavesStreamOpen()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreatePackageRelationshipsXml());
        MemoryStream stream = new MemoryStream(bytes);

        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePackageRelationships(stream);

        Assert.That(relationships.Count(), Is.EqualTo(3));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void FindById_ReturnsNullWhenRelationshipIsMissing()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePackageRelationships(CreatePackageRelationshipsXml());

        Assert.That(relationships.FindById("missing"), Is.Null);
    }

    [Test]
    public void ParsePartRelationships_ReadsRelationshipsForSourcePart()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePartRelationships(
            "word/document.xml",
            CreatePartRelationshipsXml());

        Assert.That(relationships.SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(relationships.Count(), Is.EqualTo(2));

        global::docx.DocxRelationship? image = relationships.FindById("rIdImage");

        Assert.That(image, Is.Not.Null);
        Assert.That(image!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeImage));
        Assert.That(image.Target, Is.EqualTo("media/image1.png"));
        Assert.That(image.IsExternal, Is.False);

        global::docx.DocxRelationship? hyperlink = relationships.FindById("rIdLink");

        Assert.That(hyperlink, Is.Not.Null);
        Assert.That(hyperlink!.TargetMode, Is.EqualTo("External"));
        Assert.That(hyperlink.IsExternal, Is.True);
    }

    [Test]
    public void ParsePartRelationshipsFromZip_ReadsSiblingRelsPart()
    {
        MemoryStream stream = CreateZipWithPartRelationships();

        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePartRelationshipsFromZip(
            stream,
            "/word/document.xml");

        Assert.That(relationships.Count(), Is.EqualTo(2));
        Assert.That(relationships.FindById("rIdImage"), Is.Not.Null);
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void RelationshipPartNameMapping_ConvertsBetweenSourceAndRelationshipParts()
    {
        Assert.That(global::docx.DocxRelationshipSet.GetRelationshipPartName("/word/document.xml"), Is.EqualTo("/word/_rels/document.xml.rels"));
        Assert.That(global::docx.DocxRelationshipSet.GetRelationshipPartName("docProps/core.xml"), Is.EqualTo("/docProps/_rels/core.xml.rels"));
        Assert.That(global::docx.DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName("/word/_rels/document.xml.rels"), Is.EqualTo("/word/document.xml"));
        Assert.That(global::docx.DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName("docProps/_rels/core.xml.rels"), Is.EqualTo("/docProps/core.xml"));
        Assert.That(global::docx.DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName("/_rels/.rels"), Is.EqualTo("/"));
    }

    [Test]
    public void ResolveTargetPartName_ResolvesPackageRootTargetsWithoutHardCodedDocumentPath()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePackageRelationships(CreateCustomPackageRelationshipsXml());

        Assert.That(relationships.ResolveTargetPartNameById("rIdMain"), Is.EqualTo("/custom/main-document.xml"));
        Assert.That(relationships.ResolveTargetPartNameById("missing"), Is.EqualTo(""));
    }

    [Test]
    public void ResolveTargetPartName_ResolvesPartRelativeTargets()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePartRelationships(
            "/word/document.xml",
            CreatePartRelationshipsWithRelativeTargetsXml());

        Assert.That(relationships.ResolveTargetPartNameById("rIdImage"), Is.EqualTo("/word/media/image1.png"));
        Assert.That(relationships.ResolveTargetPartNameById("rIdTheme"), Is.EqualTo("/theme/theme1.xml"));
        Assert.That(relationships.ResolveTargetPartNameById("rIdAbsolute"), Is.EqualTo("/custom/item1.xml"));
    }

    [Test]
    public void PathHelpers_NormalizeSourceBaseAndPathSegments()
    {
        Assert.That(global::docx.DocxRelationshipSet.GetSourceBaseDirectory("/word/document.xml"), Is.EqualTo("/word/"));
        Assert.That(global::docx.DocxRelationshipSet.GetSourceBaseDirectory("/"), Is.EqualTo("/"));
        Assert.That(global::docx.DocxRelationshipSet.ResolvePathSegments("/word/./media/../theme/theme1.xml"), Is.EqualTo("/word/theme/theme1.xml"));
        Assert.That(global::docx.DocxRelationshipSet.ResolvePathSegments("word/media/image1.png"), Is.EqualTo("/word/media/image1.png"));
    }

    [Test]
    public void ResolveTargetPartName_RejectsExternalAndInvalidInternalTargets()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePartRelationships(
            "/word/document.xml",
            CreatePartRelationshipsXml());

        Assert.Throws<InvalidOperationException>(() => relationships.ResolveTargetPartNameById("rIdLink"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxRelationshipSet.ResolveTargetPartName("/word/document.xml", "../../outside.xml"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxRelationshipSet.ResolveTargetPartName("/word/document.xml", "http://example.com/item.xml"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxRelationshipSet.ResolveTargetPartName("/word/document.xml", "media/image 1.png"));
    }

    [Test]
    public void CreateRelationship_AllocatesIdsAndAddsRelationships()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        global::docx.DocxRelationship image = relationships.CreateRelationship(
            global::docx.DocxConstants.RelationshipTypeImage,
            "media/image1.png",
            false);

        global::docx.DocxRelationship link = relationships.CreateRelationship(
            global::docx.DocxConstants.RelationshipTypeHyperlink,
            "https://example.com/",
            true);

        Assert.That(image.Id, Is.EqualTo("rId1"));
        Assert.That(image.TargetMode, Is.EqualTo(""));
        Assert.That(link.Id, Is.EqualTo("rId2"));
        Assert.That(link.TargetMode, Is.EqualTo("External"));
        Assert.That(link.IsExternal, Is.True);
        Assert.That(relationships.Count(), Is.EqualTo(2));
    }

    [Test]
    public void CreateRelationship_WithExplicitIdRejectsDuplicates()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        relationships.CreateRelationshipWithId("rId5", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);

        Assert.Throws<InvalidOperationException>(() => relationships.CreateRelationshipWithId("rId5", global::docx.DocxConstants.RelationshipTypeImage, "media/image2.png", false));
        Assert.Throws<InvalidOperationException>(() => relationships.AddRelationship(new global::docx.DocxRelationship("rId5", global::docx.DocxConstants.RelationshipTypeImage, "media/image3.png", "")));
    }

    [Test]
    public void UpdateRelationship_ChangesTypeTargetAndExternalMode()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        relationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);

        Assert.That(relationships.UpdateRelationship("rId1", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/", true), Is.True);

        global::docx.DocxRelationship? relationship = relationships.FindById("rId1");

        Assert.That(relationship, Is.Not.Null);
        Assert.That(relationship!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeHyperlink));
        Assert.That(relationship.Target, Is.EqualTo("https://example.com/"));
        Assert.That(relationship.TargetMode, Is.EqualTo("External"));
        Assert.That(relationship.IsExternal, Is.True);
        Assert.That(relationships.UpdateRelationship("missing", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false), Is.False);
    }

    [Test]
    public void RemoveRelationship_RemovesRelationshipById()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        relationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);
        relationships.CreateRelationshipWithId("rId2", global::docx.DocxConstants.RelationshipTypeImage, "media/image2.png", false);

        Assert.That(relationships.RemoveRelationship("rId1"), Is.True);
        Assert.That(relationships.FindById("rId1"), Is.Null);
        Assert.That(relationships.Count(), Is.EqualTo(1));
        Assert.That(relationships.RemoveRelationship("rId1"), Is.False);
    }

    [Test]
    public void WriteXml_ReflectsUpdatedAndRemovedRelationships()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        relationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);
        relationships.CreateRelationshipWithId("rId2", global::docx.DocxConstants.RelationshipTypeImage, "media/image2.png", false);
        relationships.UpdateRelationship("rId1", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/", true);
        relationships.RemoveRelationship("rId2");

        string xml = relationships.WriteXml();
        global::docx.DocxRelationshipSet roundTripped = global::docx.DocxRelationshipSet.ParsePartRelationships("/word/document.xml", xml);
        global::docx.DocxRelationship? relationship = roundTripped.FindById("rId1");

        Assert.That(roundTripped.Count(), Is.EqualTo(1));
        Assert.That(relationship, Is.Not.Null);
        Assert.That(relationship!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeHyperlink));
        Assert.That(relationship.Target, Is.EqualTo("https://example.com/"));
        Assert.That(relationship.TargetMode, Is.EqualTo("External"));
        Assert.That(roundTripped.FindById("rId2"), Is.Null);
    }

    [Test]
    public void ParseAndWriteXml_PreservesUnknownRelationshipsAttributesAndExternalTargets()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxRelationshipSet.ParsePackageRelationships(CreateRelationshipsWithUnknownsXml());

        global::docx.DocxRelationship? unknown = relationships.FindById("rIdUnknown");
        global::docx.DocxRelationship? external = relationships.FindById("rIdExternal");

        Assert.That(unknown, Is.Not.Null);
        Assert.That(unknown!.Type, Is.EqualTo("http://example.com/custom/relationship"));
        Assert.That(unknown.Target, Is.EqualTo("custom/data.bin"));
        Assert.That(unknown.UnknownAttributeCount(), Is.EqualTo(1));
        Assert.That(unknown.UnknownAttributes[0].LocalName, Is.EqualTo("Data"));
        Assert.That(unknown.UnknownAttributes[0].Value, Is.EqualTo("preserve"));
        Assert.That(external, Is.Not.Null);
        Assert.That(external!.Target, Is.EqualTo("https://example.com/resource"));
        Assert.That(external.TargetMode, Is.EqualTo("External"));
        Assert.That(external.IsExternal, Is.True);

        string xml = relationships.WriteXml();
        global::docx.DocxRelationshipSet roundTripped = global::docx.DocxRelationshipSet.ParsePackageRelationships(xml);
        global::docx.DocxRelationship? roundTrippedUnknown = roundTripped.FindById("rIdUnknown");
        global::docx.DocxRelationship? roundTrippedExternal = roundTripped.FindById("rIdExternal");

        Assert.That(roundTrippedUnknown, Is.Not.Null);
        Assert.That(roundTrippedUnknown!.Type, Is.EqualTo("http://example.com/custom/relationship"));
        Assert.That(roundTrippedUnknown.UnknownAttributeCount(), Is.EqualTo(1));
        Assert.That(roundTrippedUnknown.UnknownAttributes[0].Value, Is.EqualTo("preserve"));
        Assert.That(roundTrippedExternal, Is.Not.Null);
        Assert.That(roundTrippedExternal!.Target, Is.EqualTo("https://example.com/resource"));
        Assert.That(roundTrippedExternal.TargetMode, Is.EqualTo("External"));
    }

    [Test]
    public void ParsePackageRelationships_RejectsMissingRequiredRelationshipAttributes()
    {
        Assert.Throws<InvalidDataException>(() => global::docx.DocxRelationshipSet.ParsePackageRelationships(
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Type=\"x\" Target=\"word/document.xml\" /></Relationships>"));

        Assert.Throws<InvalidDataException>(() => global::docx.DocxRelationshipSet.ParsePackageRelationships(
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Target=\"word/document.xml\" /></Relationships>"));

        Assert.Throws<InvalidDataException>(() => global::docx.DocxRelationshipSet.ParsePackageRelationships(
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"x\" /></Relationships>"));
    }

    [Test]
    public void RelationshipSet_RejectsNullAndEmptyInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationship(null!, "type", "target", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationship("rId1", null!, "target", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationship("rId1", "type", null!, ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationship("rId1", "type", "target", null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationshipAttribute(null!, "Data", "", "value"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationshipAttribute("", null!, "", "value"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationshipAttribute("", "Data", null!, "value"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationshipAttribute("", "Data", "", null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxRelationshipAttribute("", "", "", "value"));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxRelationship("", "type", "target", ""));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxRelationship("rId1", "", "target", ""));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxRelationship("rId1", "type", "", ""));
        Assert.Throws<ArgumentNullException>(() => relationshipsNullUpdateHelper().UpdateRelationship(null!, "type", "target", false));
        Assert.Throws<ArgumentNullException>(() => relationshipsNullUpdateHelper().UpdateRelationship("rId1", null!, "target", false));
        Assert.Throws<ArgumentNullException>(() => relationshipsNullUpdateHelper().UpdateRelationship("rId1", "type", null!, false));
        Assert.Throws<ArgumentNullException>(() => relationshipsNullUpdateHelper().RemoveRelationship(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.ParsePackageRelationships((string)null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.ParsePackageRelationships((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.Parse(null!, CreatePackageRelationshipsXml()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.ParsePartRelationships(null!, CreatePartRelationshipsXml()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.ParsePartRelationships("word/document.xml", (Stream)null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.ParsePartRelationshipsFromZip(null!, "word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.ResolveTargetPartName(null!, "word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.ResolveTargetPartName("/", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.GetSourceBaseDirectory(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRelationshipSet.ResolvePathSegments(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxRelationshipSet.ResolveTargetPartName("/", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRelationshipSet(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxRelationshipSet.ParsePartRelationships("/", CreatePartRelationshipsXml()));
        Assert.Throws<ArgumentException>(() => global::docx.DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName("/word/document.xml"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxRelationshipSet.GetSourcePartNameFromRelationshipPartName("/word/_rels/document.xml"));
    }

    static global::docx.DocxRelationshipSet relationshipsNullUpdateHelper()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");
        relationships.CreateRelationshipWithId("rId1", "type", "target", false);
        return relationships;
    }

    static string CreatePackageRelationshipsXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">",
            "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\" />",
            "  <Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\" />",
            "  <Relationship Id=\"rIdExternal\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com/\" TargetMode=\"External\" />",
            "</Relationships>"
        });
    }

    static string CreateRelationshipsWithUnknownsXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">",
            "  <Relationship Id=\"rIdUnknown\" Type=\"http://example.com/custom/relationship\" Target=\"custom/data.bin\" Data=\"preserve\" />",
            "  <Relationship Id=\"rIdExternal\" Type=\"http://example.com/custom/external\" Target=\"https://example.com/resource\" TargetMode=\"External\" />",
            "</Relationships>"
        });
    }

    static string CreateCustomPackageRelationshipsXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">",
            "  <Relationship Id=\"rIdMain\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"custom/main-document.xml\" />",
            "</Relationships>"
        });
    }

    static string CreatePartRelationshipsXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">",
            "  <Relationship Id=\"rIdImage\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"media/image1.png\" />",
            "  <Relationship Id=\"rIdLink\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com/\" TargetMode=\"External\" />",
            "</Relationships>"
        });
    }

    static string CreatePartRelationshipsWithRelativeTargetsXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">",
            "  <Relationship Id=\"rIdImage\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"media/image1.png\" />",
            "  <Relationship Id=\"rIdTheme\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme\" Target=\"../theme/theme1.xml\" />",
            "  <Relationship Id=\"rIdAbsolute\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXml\" Target=\"/custom/item1.xml\" />",
            "</Relationships>"
        });
    }

    static MemoryStream CreateZipWithPartRelationships()
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            ZipArchiveEntry entry = archive.CreateEntry("word/_rels/document.xml.rels");

            using (Stream entryStream = entry.Open())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(CreatePartRelationshipsXml());
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
