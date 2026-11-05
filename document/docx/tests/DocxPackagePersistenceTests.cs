using System.IO.Compression;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPackagePersistenceTests
{
    [Test]
    public void CreateEntryPoints_ReturnMinimalPackageAndDocument()
    {
        global::docx.IDocxPackagePersistence packages = new global::docx.DocxPackagePersistence();
        global::docx.IDocxDocumentPersistence documents = (global::docx.IDocxDocumentPersistence)packages;

        global::docx.DocxPackage package = packages.CreatePackage();
        global::docx.DocxDocument document = documents.CreateDocument();

        Assert.That(package.ContainsPart("/word/document.xml"), Is.True);
        Assert.That(package.GetPackageRelationshipSet(), Is.Not.Null);
        Assert.That(document.GetText(), Is.EqualTo(""));
        Assert.That(document.GetSections(), Is.Empty);
    }

    [Test]
    public void PackageStreamEntryPoints_HonorLoadAndSaveOptionsAndLeaveStreamsOpen()
    {
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxSaveOptions saveOptions = new global::docx.DocxSaveOptions();
        global::docx.DocxLoadOptions loadOptions = new global::docx.DocxLoadOptions();
        MemoryStream stream = new MemoryStream();

        saveOptions.Deterministic = true;
        loadOptions.LazyLoadXmlParts = true;
        loadOptions.LazyLoadBinaryParts = false;

        persistence.SavePackage(persistence.CreatePackage(), stream, saveOptions);

        Assert.That(stream.CanWrite, Is.True);

        stream.Position = 0;

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            Assert.That(archive.Entries.Select(entry => entry.FullName), Is.EqualTo(new string[]
            {
                "[Content_Types].xml",
                "_rels/.rels",
                "word/document.xml"
            }));
            Assert.That(archive.GetEntry("word/document.xml")!.LastWriteTime.DateTime, Is.EqualTo(new DateTime(1980, 1, 1, 0, 0, 0)));
        }

        stream.Position = 0;

        global::docx.DocxPackage opened = persistence.OpenPackage(stream, loadOptions);
        global::docx.DocxPart? mainPart = opened.FindPart("/word/document.xml");

        Assert.That(stream.CanRead, Is.True);
        Assert.That(mainPart, Is.Not.Null);
        Assert.That(mainPart!.IsLazy, Is.True);
    }

    [Test]
    public void PackageFileEntryPoints_HonorOptionsAndRoundTrip()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "public-package-persistence.docx");
        global::docx.IDocxPackagePersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxSaveOptions saveOptions = new global::docx.DocxSaveOptions();
        global::docx.DocxLoadOptions loadOptions = new global::docx.DocxLoadOptions();

        saveOptions.Mode = global::docx.DocxSaveMode.DeterministicRoundTrip;
        loadOptions.LazyLoadXmlParts = true;
        loadOptions.LazyLoadBinaryParts = false;

        persistence.SavePackageFile(persistence.CreatePackage(), path, saveOptions);

        global::docx.DocxPackage opened = persistence.OpenPackageFile(path, loadOptions);
        global::docx.DocxPart? mainPart = opened.FindPart("/word/document.xml");

        Assert.That(File.Exists(path), Is.True);
        Assert.That(mainPart, Is.Not.Null);
        Assert.That(mainPart!.IsLazy, Is.True);

        using (FileStream stream = File.OpenRead(path))
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                Assert.That(archive.GetEntry("word/document.xml")!.LastWriteTime.DateTime, Is.EqualTo(new DateTime(1980, 1, 1, 0, 0, 0)));
            }
        }
    }

    [Test]
    public void DocumentStreamEntryPoints_SerializeCurrentBodyAndLeaveStreamsOpen()
    {
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxSaveOptions saveOptions = new global::docx.DocxSaveOptions();
        global::docx.DocxLoadOptions loadOptions = new global::docx.DocxLoadOptions();
        MemoryStream stream = new MemoryStream();

        document.Body = new global::docx.DocxBody(CreateBodyXml("stream document"));
        saveOptions.Deterministic = true;
        loadOptions.LazyLoadXmlParts = false;
        loadOptions.LazyLoadBinaryParts = false;

        persistence.SaveDocument(document, stream, saveOptions);

        Assert.That(stream.CanWrite, Is.True);

        stream.Position = 0;

        global::docx.DocxDocument opened = persistence.OpenDocument(stream, loadOptions);

        Assert.That(stream.CanRead, Is.True);
        Assert.That(opened.GetText(), Is.EqualTo("stream document"));
    }

    [Test]
    public void DocumentFileEntryPoints_HonorOptionsAndRoundTrip()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "public-document-persistence.docx");
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxSaveOptions saveOptions = new global::docx.DocxSaveOptions();
        global::docx.DocxLoadOptions loadOptions = new global::docx.DocxLoadOptions();

        document.Body = new global::docx.DocxBody(CreateBodyXml("file document"));
        saveOptions.Mode = global::docx.DocxSaveMode.DeterministicRoundTrip;
        loadOptions.LazyLoadXmlParts = false;
        loadOptions.LazyLoadBinaryParts = false;

        persistence.SaveDocumentFile(document, path, saveOptions);

        global::docx.DocxDocument opened = persistence.OpenDocumentFile(path, loadOptions);

        Assert.That(File.Exists(path), Is.True);
        Assert.That(opened.GetText(), Is.EqualTo("file document"));

        using (FileStream stream = File.OpenRead(path))
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                Assert.That(archive.GetEntry("word/document.xml")!.LastWriteTime.DateTime, Is.EqualTo(new DateTime(1980, 1, 1, 0, 0, 0)));
            }
        }
    }

    [Test]
    public void FileSaveEntryPoints_InferOutputKindAndPreserveMacrosWithoutMutatingOptions()
    {
        string documentPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "public-output.docx");
        string templatePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "public-output.dotx");
        string macroTemplatePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "public-output.dotm");
        global::docx.IDocxPackagePersistence packages = new global::docx.DocxPackagePersistence();
        global::docx.IDocxDocumentPersistence documents = (global::docx.IDocxDocumentPersistence)packages;
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();
        global::docx.DocxLoadOptions loadOptions = new global::docx.DocxLoadOptions();

        packages.SavePackageFile(packages.CreatePackage(), documentPath, options);
        documents.SaveDocumentFile(documents.CreateDocument(), templatePath, options);

        global::docx.DocxPackage document = packages.OpenPackageFile(documentPath, loadOptions);
        global::docx.DocxPackage template = packages.OpenPackageFile(templatePath, loadOptions);

        Assert.That(document.ResolveContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(template.ResolveContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeTemplateMainDocument));
        Assert.That(options.OutputType, Is.EqualTo(global::docx.DocxOutputType.Preserve));

        byte[] macroBytes = new byte[] { 1, 3, 3, 7 };
        global::docx.DocxPackage macroPackage = packages.CreatePackage();
        macroPackage.AddBinaryPart("/word/vbaProject.bin", global::docx.DocxConstants.ContentTypeVbaProject, macroBytes);
        macroPackage.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeVbaProject,
            "vbaProject.bin",
            false);
        packages.SavePackageFile(macroPackage, macroTemplatePath, options);

        global::docx.DocxPackage macroTemplate = packages.OpenPackageFile(macroTemplatePath, loadOptions);

        Assert.That(
            macroTemplate.ResolveContentType("/word/document.xml"),
            Is.EqualTo(global::docx.DocxConstants.ContentTypeMacroEnabledTemplateMainDocument));
        Assert.That(macroTemplate.GetPartBytes("/word/vbaProject.bin"), Is.EqualTo(macroBytes));
        Assert.That(macroTemplate.FindVbaProjectRelationship(), Is.Not.Null);
        Assert.That(options.OutputType, Is.EqualTo(global::docx.DocxOutputType.Preserve));
    }

    [Test]
    public void PublicEditAndDefaultSave_PreserveUnmodeledPackageAndBodyContent()
    {
        global::docx.IDocxPackagePersistence packages = new global::docx.DocxPackagePersistence();
        global::docx.IDocxDocumentPersistence documents = (global::docx.IDocxDocumentPersistence)packages;
        global::docx.DocxPackage package = packages.CreatePackage();
        byte[] payload = new byte[] { 7, 0, 9, 255 };
        byte[] macro = new byte[] { 222, 173, 190, 239 };
        string customRelationshipType = "urn:vendor:relationship";
        global::docx.DocxRelationship headerRelationship = package.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeHeader,
            "header1.xml",
            false);

        package.ReplaceXmlPart(
            "/word/document.xml",
            global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument,
            CreatePreservationDocumentXml(headerRelationship.Id));
        package.AddXmlPartWithContent(
            "/word/header1.xml",
            global::docx.DocxConstants.ContentTypeHeader,
            "<hp:hdr xmlns:hp=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
                + "\" xmlns:hx=\"urn:vendor:header\" hx:flag=\"preserve\">"
                + "<hp:p><hp:r><hp:t>Header Before</hp:t></hp:r></hp:p><hx:unknown /></hp:hdr>");
        package.AddBinaryPart("/custom/payload.bin", "application/x-vendor-payload", payload);
        package.AddBinaryPart("/word/vbaProject.bin", global::docx.DocxConstants.ContentTypeVbaProject, macro);
        global::docx.DocxRelationship customRelationship = package.CreateRelationship(
            "/word/document.xml",
            customRelationshipType,
            "../custom/payload.bin",
            false);
        package.CreateRelationship(
            "/word/document.xml",
            global::docx.DocxConstants.RelationshipTypeVbaProject,
            "vbaProject.bin",
            false);
        string relationshipPartName = "/word/_rels/document.xml.rels";
        System.Xml.XmlDocument relationshipDocument = new System.Xml.XmlDocument();
        relationshipDocument.LoadXml(package.GetPartXml(relationshipPartName));
        System.Xml.XmlElement relationshipRoot = relationshipDocument.DocumentElement!;
        relationshipRoot.Prefix = "pr";
        relationshipRoot.SetAttribute("xmlns:pr", global::docx.DocxConstants.NamespacePackageRelationships);
        relationshipRoot.SetAttribute("xmlns:rx", "urn:vendor:relationships");
        System.Xml.XmlAttribute relationshipFlag = relationshipDocument.CreateAttribute("rx", "flag", "urn:vendor:relationships");
        relationshipFlag.Value = "preserve";
        relationshipRoot.Attributes.Append(relationshipFlag);

        foreach (System.Xml.XmlNode relationshipNode in relationshipRoot.ChildNodes)
        {
            if (relationshipNode.NodeType == System.Xml.XmlNodeType.Element)
            {
                relationshipNode.Prefix = "pr";
            }
        }

        package.ReplaceXmlPart(relationshipPartName, global::docx.DocxConstants.ContentTypeRelationships, relationshipDocument.OuterXml);
        package.CreateRelationship(
            "/word/document.xml",
            "urn:vendor:additional-relationship",
            "../custom/payload.bin",
            false);

        global::docx.DocxDocument document = new global::docx.DocxDocument(package);
        global::docx.DocxGenerationManagement generation = new global::docx.DocxGenerationManagement();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxSectionManagement sections = new global::docx.DocxSectionManagement();
        generation.AppendParagraph(document, "After");
        sections.AppendHeaderBlock(document.GetHeaders()[0], factory.CreateParagraphBlock(factory.CreateTextParagraph("Header After")));

        MemoryStream stream = new MemoryStream();
        documents.SaveDocument(document, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;

        global::docx.DocxPackage reopened = packages.OpenPackage(stream, new global::docx.DocxLoadOptions());
        string xml = reopened.GetPartXml("/word/document.xml");
        string headerXml = reopened.GetPartXml("/word/header1.xml");
        string relationshipXml = reopened.GetPartXml(relationshipPartName);
        global::docx.DocxRelationship? reopenedRelationship = reopened.FindRelationship(
            "/word/document.xml",
            customRelationship.Id);

        Assert.That(xml, Does.StartWith("<wp:document"));
        Assert.That(xml, Does.Contain("<wp:body"));
        Assert.That(xml, Does.Contain("<wp:p>"));
        Assert.That(xml, Does.Contain("mc:Ignorable=\"w14\""));
        Assert.That(xml, Does.Contain("b:flag=\"preserve\""));
        Assert.That(xml, Does.Contain("<mc:AlternateContent"));
        Assert.That(
            xml.IndexOf("<mc:AlternateContent", StringComparison.Ordinal),
            Is.EqualTo(xml.LastIndexOf("<mc:AlternateContent", StringComparison.Ordinal)));
        Assert.That(xml, Does.Contain("<w14:future w14:value=\"choice\""));
        Assert.That(xml, Does.Contain("<vendor:unknown vendor:value=\"keep\""));
        Assert.That(
            xml.IndexOf("<vendor:unknown", StringComparison.Ordinal),
            Is.EqualTo(xml.LastIndexOf("<vendor:unknown", StringComparison.Ordinal)));
        Assert.That(xml, Does.Contain("<vendor:nested />"));
        Assert.That(xml.IndexOf("AlternateContent", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("After", StringComparison.Ordinal)));
        Assert.That(reopened.GetPartBytes("/custom/payload.bin"), Is.EqualTo(payload));
        Assert.That(reopened.GetPartBytes("/word/vbaProject.bin"), Is.EqualTo(macro));
        Assert.That(reopened.HasMacroEnabledMainDocument(), Is.True);
        Assert.That(reopened.FindVbaProjectRelationship(), Is.Not.Null);
        Assert.That(reopenedRelationship, Is.Not.Null);
        Assert.That(reopenedRelationship!.Type, Is.EqualTo(customRelationshipType));
        Assert.That(reopened.ResolveRelationshipTarget("/word/document.xml", customRelationship.Id), Is.EqualTo("/custom/payload.bin"));
        Assert.That(headerXml, Does.StartWith("<hp:hdr"));
        Assert.That(headerXml, Does.Contain("hx:flag=\"preserve\""));
        Assert.That(headerXml, Does.Contain("<hx:unknown"));
        Assert.That(headerXml, Does.Contain("Header After"));
        Assert.That(relationshipXml, Does.Contain("<pr:Relationships"));
        Assert.That(relationshipXml, Does.Contain("<pr:Relationship"));
        Assert.That(relationshipXml, Does.Contain("rx:flag=\"preserve\""));
    }

    [Test]
    public void UneditedDocumentRoundTrip_PreservesDeterministicBytesAcrossCompressionModes()
    {
        global::docx.IDocxPackagePersistence packages = new global::docx.DocxPackagePersistence();
        global::docx.IDocxDocumentPersistence documents = (global::docx.IDocxDocumentPersistence)packages;
        global::docx.DocxPackage package = packages.CreatePackage();
        string documentXml = CreateExactRoundTripDocumentXml();
        byte[] payload = new byte[4096];

        package.ReplaceXmlPart(
            "/word/document.xml",
            global::docx.DocxConstants.ContentTypeMainDocument,
            documentXml);
        package.AddBinaryPart("/custom/unknown.bin", "application/x-round-trip", payload);
        global::docx.DocxRelationship relationship = package.CreateRelationship(
            "/word/document.xml",
            "urn:vendor:unknown-relationship",
            "../custom/unknown.bin",
            false);

        List<global::docx.DocxCompressionMode> compressionModes = new List<global::docx.DocxCompressionMode>();
        compressionModes.Add(global::docx.DocxCompressionMode.Optimal);
        compressionModes.Add(global::docx.DocxCompressionMode.Fastest);
        compressionModes.Add(global::docx.DocxCompressionMode.NoCompression);

        foreach (global::docx.DocxCompressionMode compressionMode in compressionModes)
        {
            global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();
            options.Mode = global::docx.DocxSaveMode.DeterministicRoundTrip;
            options.CompressionMode = compressionMode;
            MemoryStream original = new MemoryStream();
            packages.SavePackage(package, original, options);
            original.Position = 0;

            global::docx.DocxDocument opened = documents.OpenDocument(original, new global::docx.DocxLoadOptions());
            MemoryStream roundTripped = new MemoryStream();
            documents.SaveDocument(opened, roundTripped, options);

            Assert.That(roundTripped.ToArray(), Is.EqualTo(original.ToArray()));

            roundTripped.Position = 0;
            global::docx.DocxPackage reopened = packages.OpenPackage(roundTripped, new global::docx.DocxLoadOptions());

            Assert.That(reopened.GetPartXml("/word/document.xml"), Is.EqualTo(documentXml));
            Assert.That(reopened.GetPartBytes("/custom/unknown.bin"), Is.EqualTo(payload));
            Assert.That(reopened.FindRelationship("/word/document.xml", relationship.Id)!.Type, Is.EqualTo("urn:vendor:unknown-relationship"));

            roundTripped.Position = 0;

            using (ZipArchive archive = new ZipArchive(roundTripped, ZipArchiveMode.Read, true))
            {
                string[] entryNames = archive.Entries.Select(entry => entry.FullName).ToArray();
                string[] sortedEntryNames = entryNames.OrderBy(entryName => entryName, StringComparer.Ordinal).ToArray();
                ZipArchiveEntry payloadEntry = archive.GetEntry("custom/unknown.bin")!;

                Assert.That(entryNames, Is.EqualTo(sortedEntryNames));

                if (compressionMode == global::docx.DocxCompressionMode.NoCompression)
                {
                    Assert.That(payloadEntry.CompressedLength, Is.EqualTo(payloadEntry.Length));
                }
                else
                {
                    Assert.That(payloadEntry.CompressedLength, Is.LessThan(payloadEntry.Length));
                }
            }
        }
    }

    [Test]
    public void EntryPoints_RejectNullRequiredArguments()
    {
        global::docx.IDocxPackagePersistence packages = new global::docx.DocxPackagePersistence();
        global::docx.IDocxDocumentPersistence documents = (global::docx.IDocxDocumentPersistence)packages;
        global::docx.DocxPackage package = packages.CreatePackage();
        global::docx.DocxDocument document = documents.CreateDocument();
        global::docx.DocxLoadOptions loadOptions = new global::docx.DocxLoadOptions();
        global::docx.DocxSaveOptions saveOptions = new global::docx.DocxSaveOptions();
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "persistence-null-options.docx");

        Assert.Throws<ArgumentNullException>(() => packages.OpenPackage(null!, loadOptions));
        Assert.Throws<ArgumentNullException>(() => packages.OpenPackage(new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => packages.OpenPackageFile(null!, loadOptions));
        Assert.Throws<ArgumentNullException>(() => packages.OpenPackageFile(path, null!));
        Assert.Throws<ArgumentNullException>(() => packages.SavePackage(null!, new MemoryStream(), saveOptions));
        Assert.Throws<ArgumentNullException>(() => packages.SavePackage(package, null!, saveOptions));
        Assert.Throws<ArgumentNullException>(() => packages.SavePackage(package, new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => packages.SavePackageFile(null!, path, saveOptions));
        Assert.Throws<ArgumentNullException>(() => packages.SavePackageFile(package, null!, saveOptions));
        Assert.Throws<ArgumentNullException>(() => packages.SavePackageFile(package, path, null!));
        Assert.Throws<ArgumentNullException>(() => documents.OpenDocument(null!, loadOptions));
        Assert.Throws<ArgumentNullException>(() => documents.OpenDocument(new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => documents.OpenDocumentFile(null!, loadOptions));
        Assert.Throws<ArgumentNullException>(() => documents.OpenDocumentFile(path, null!));
        Assert.Throws<ArgumentNullException>(() => documents.SaveDocument(null!, new MemoryStream(), saveOptions));
        Assert.Throws<ArgumentNullException>(() => documents.SaveDocument(document, null!, saveOptions));
        Assert.Throws<ArgumentNullException>(() => documents.SaveDocument(document, new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => documents.SaveDocumentFile(null!, path, saveOptions));
        Assert.Throws<ArgumentNullException>(() => documents.SaveDocumentFile(document, null!, saveOptions));
        Assert.Throws<ArgumentNullException>(() => documents.SaveDocumentFile(document, path, null!));
    }

    static string CreateBodyXml(string text)
    {
        return "<w:body xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\"><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:body>";
    }

    static string CreatePreservationDocumentXml(string headerRelationshipId)
    {
        return "<wp:document xmlns:wp=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\" xmlns:r=\"" + global::docx.DocxConstants.NamespaceRelationships
            + "\" xmlns:mc=\"" + global::docx.DocxConstants.NamespaceMarkupCompatibility
            + "\" xmlns:w14=\"http://schemas.microsoft.com/office/word/2010/wordml\""
            + " xmlns:vendor=\"urn:vendor:markup\" mc:Ignorable=\"w14\" vendor:root=\"preserve\">"
            + "<wp:body xmlns:b=\"urn:vendor:body\" b:flag=\"preserve\">"
            + "<wp:p><wp:r><wp:t>Before</wp:t></wp:r></wp:p>"
            + "<mc:AlternateContent><mc:Choice Requires=\"w14\"><w14:future w14:value=\"choice\" /></mc:Choice>"
            + "<mc:Fallback><wp:p><wp:r><wp:t>Fallback</wp:t></wp:r></wp:p></mc:Fallback></mc:AlternateContent>"
            + "<vendor:unknown vendor:value=\"keep\"><vendor:nested /></vendor:unknown>"
            + "<wp:sectPr><wp:headerReference wp:type=\"default\" r:id=\"" + headerRelationshipId
            + "\" /></wp:sectPr></wp:body></wp:document>";
    }

    static string CreateExactRoundTripDocumentXml()
    {
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + "<wp:document xmlns:wp=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\" xmlns:mc=\"" + global::docx.DocxConstants.NamespaceMarkupCompatibility
            + "\" xmlns:w14=\"http://schemas.microsoft.com/office/word/2010/wordml\""
            + " xmlns:vendor=\"urn:vendor:round-trip\" mc:Ignorable=\"w14\" vendor:flag=\"keep\">\n"
            + "  <wp:body>\n"
            + "    <wp:p vendor:paragraph=\"keep\"><wp:r><wp:t xml:space=\"preserve\"> spaced text </wp:t></wp:r></wp:p>\n"
            + "    <mc:AlternateContent>\n"
            + "      <mc:Choice Requires=\"w14\"><w14:future vendor:value=\"choice\" /></mc:Choice>\n"
            + "      <mc:Fallback><vendor:fallback /></mc:Fallback>\n"
            + "    </mc:AlternateContent>\n"
            + "    <vendor:unknown><vendor:nested /></vendor:unknown>\n"
            + "  </wp:body>\n"
            + "</wp:document>";
    }
}
