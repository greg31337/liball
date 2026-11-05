using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPackageTests
{
    [Test]
    public void Open_LoadsContentTypesPartsAndRelationshipSets()
    {
        MemoryStream stream = CreatePackageStream(true);

        global::docx.DocxPackage package = global::docx.DocxPackage.Open(stream);

        Assert.That(package.ContentTypes.DefaultCount(), Is.EqualTo(3));
        Assert.That(package.ContentTypes.OverrideCount(), Is.EqualTo(1));
        Assert.That(package.PartCount(), Is.EqualTo(5));
        Assert.That(package.RelationshipSetCount(), Is.EqualTo(2));

        global::docx.DocxPart documentPart = package.Parts.Single(part => part.PartName == "/word/document.xml");
        global::docx.DocxPart imagePart = package.Parts.Single(part => part.PartName == "/word/media/image1.png");
        global::docx.DocxPart customPart = package.Parts.Single(part => part.PartName == "/custom/data.bin");
        global::docx.DocxPart relationshipPart = package.Parts.Single(part => part.PartName == "/word/_rels/document.xml.rels");

        Assert.That(documentPart.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(documentPart.GetText(), Is.EqualTo("<w:document />"));
        Assert.That(imagePart.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(imagePart.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(customPart.ContentType, Is.EqualTo(""));
        Assert.That(relationshipPart.IsRelationshipPart, Is.True);
    }

    [Test]
    public void Open_LoadsRootAndPartRelationshipsWithoutHardCodedMainDocumentPath()
    {
        MemoryStream stream = CreatePackageStream(true);

        global::docx.DocxPackage package = global::docx.DocxPackage.Open(stream);

        global::docx.DocxRelationshipSet rootRelationships = package.RelationshipSets.Single(set => set.SourcePartName == "/");
        global::docx.DocxRelationshipSet documentRelationships = package.RelationshipSets.Single(set => set.SourcePartName == "/word/document.xml");

        Assert.That(rootRelationships.ResolveTargetPartNameById("rIdMain"), Is.EqualTo("/word/document.xml"));
        Assert.That(documentRelationships.ResolveTargetPartNameById("rIdImage"), Is.EqualTo("/word/media/image1.png"));
    }

    [Test]
    public void Open_LeavesInputStreamOpen()
    {
        MemoryStream stream = CreatePackageStream(true);

        global::docx.DocxPackage.Open(stream);

        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void Open_RejectsNonZipPackageStream()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        Assert.Throws<InvalidDataException>(() => global::docx.DocxPackage.Open(stream));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void OpenFile_LoadsPackageCatalog()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "catalog-load.docx");
        MemoryStream stream = CreatePackageStream(true);

        File.WriteAllBytes(path, stream.ToArray());

        global::docx.DocxPackage package = global::docx.DocxPackage.OpenFile(path);

        Assert.That(package.PartCount(), Is.EqualTo(5));
        Assert.That(package.RelationshipSetCount(), Is.EqualTo(2));
    }

    [Test]
    public void Open_LoadsPartsEvenWhenContentTypesAreMissing()
    {
        MemoryStream stream = CreatePackageStream(false);

        global::docx.DocxPackage package = global::docx.DocxPackage.Open(stream);

        Assert.That(package.ContentTypes.DefaultCount(), Is.EqualTo(0));
        Assert.That(package.PartCount(), Is.EqualTo(5));
        Assert.That(package.Parts.All(part => part.ContentType == ""), Is.True);
    }

    [Test]
    public void Open_IgnoresDirectoryEntries()
    {
        MemoryStream stream = CreatePackageStreamWithDirectoryEntries();

        global::docx.DocxPackage package = global::docx.DocxPackage.Open(stream);

        Assert.That(package.PartCount(), Is.EqualTo(5));
        Assert.That(package.Parts.Any(part => part.PartName == "/word/"), Is.False);
        Assert.That(package.Parts.Any(part => part.PartName == "/word/media/"), Is.False);
        Assert.That(package.FindPart("/word/document.xml"), Is.Not.Null);
        Assert.That(package.FindPart("/word/media/image1.png"), Is.Not.Null);
    }

    [Test]
    public void Open_WithLazyXmlPartsDefersXmlBytesUntilRead()
    {
        MemoryStream stream = CreatePackageStream(true);
        global::docx.DocxLoadOptions options = new global::docx.DocxLoadOptions();

        options.LazyLoadXmlParts = true;
        options.LazyLoadBinaryParts = false;

        global::docx.DocxPackage package = global::docx.DocxPackage.Open(stream, options);

        global::docx.DocxPart documentPart = package.Parts.Single(part => part.PartName == "/word/document.xml");
        global::docx.DocxPart imagePart = package.Parts.Single(part => part.PartName == "/word/media/image1.png");
        global::docx.DocxPart relationshipPart = package.Parts.Single(part => part.PartName == "/_rels/.rels");

        Assert.That(documentPart.IsLazy, Is.True);
        Assert.That(documentPart.Bytes, Is.Empty);
        Assert.That(documentPart.ZipEntryName, Is.EqualTo("word/document.xml"));
        Assert.That(documentPart.LoadCount, Is.EqualTo(0));
        Assert.That(imagePart.IsLazy, Is.False);
        Assert.That(relationshipPart.IsLazy, Is.False);
        Assert.That(relationshipPart.LoadCount, Is.EqualTo(1));

        Assert.That(documentPart.GetText(), Is.EqualTo("<w:document />"));
        Assert.That(documentPart.IsLazy, Is.False);
        Assert.That(documentPart.LoadCount, Is.EqualTo(1));

        Assert.That(documentPart.GetText(), Is.EqualTo("<w:document />"));
        Assert.That(documentPart.LoadCount, Is.EqualTo(1));
    }

    [Test]
    public void Open_WithLazyXmlPartsDisabledLoadsXmlBytesImmediately()
    {
        MemoryStream stream = CreatePackageStream(true);
        global::docx.DocxLoadOptions options = new global::docx.DocxLoadOptions();

        options.LazyLoadXmlParts = false;
        options.LazyLoadBinaryParts = false;

        global::docx.DocxPackage package = global::docx.DocxPackage.Open(stream, options);

        global::docx.DocxPart documentPart = package.Parts.Single(part => part.PartName == "/word/document.xml");
        global::docx.DocxPart relationshipPart = package.Parts.Single(part => part.PartName == "/_rels/.rels");

        Assert.That(documentPart.IsLazy, Is.False);
        Assert.That(documentPart.LoadCount, Is.EqualTo(0));
        Assert.That(documentPart.GetText(), Is.EqualTo("<w:document />"));
        Assert.That(relationshipPart.IsLazy, Is.False);
        Assert.That(relationshipPart.LoadCount, Is.EqualTo(0));
    }

    [Test]
    public void OpenFile_WithLoadOptionsHonorsLazyXmlParts()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "lazy-open-file.docx");
        MemoryStream stream = CreatePackageStream(true);
        global::docx.DocxLoadOptions options = new global::docx.DocxLoadOptions();

        options.LazyLoadXmlParts = true;
        options.LazyLoadBinaryParts = false;
        File.WriteAllBytes(path, stream.ToArray());

        global::docx.DocxPackage package = global::docx.DocxPackage.OpenFile(path, options);

        global::docx.DocxPart documentPart = package.Parts.Single(part => part.PartName == "/word/document.xml");

        Assert.That(documentPart.IsLazy, Is.True);
        Assert.That(documentPart.GetText(), Is.EqualTo("<w:document />"));
    }

    [Test]
    public void Open_WithLazyBinaryPartsDefersMediaBytesUntilRead()
    {
        MemoryStream stream = CreatePackageStream(true);
        global::docx.DocxLoadOptions options = new global::docx.DocxLoadOptions();

        options.LazyLoadXmlParts = false;
        options.LazyLoadBinaryParts = true;

        global::docx.DocxPackage package = global::docx.DocxPackage.Open(stream, options);

        global::docx.DocxPart documentPart = package.Parts.Single(part => part.PartName == "/word/document.xml");
        global::docx.DocxPart imagePart = package.Parts.Single(part => part.PartName == "/word/media/image1.png");
        global::docx.DocxPart customPart = package.Parts.Single(part => part.PartName == "/custom/data.bin");
        global::docx.DocxPart relationshipPart = package.Parts.Single(part => part.PartName == "/_rels/.rels");

        Assert.That(documentPart.IsLazy, Is.False);
        Assert.That(relationshipPart.IsLazy, Is.False);
        Assert.That(imagePart.IsLazy, Is.True);
        Assert.That(imagePart.Bytes, Is.Empty);
        Assert.That(imagePart.ZipEntryName, Is.EqualTo("word/media/image1.png"));
        Assert.That(imagePart.LoadCount, Is.EqualTo(0));
        Assert.That(customPart.IsLazy, Is.True);

        Assert.That(imagePart.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(imagePart.IsLazy, Is.False);
        Assert.That(imagePart.LoadCount, Is.EqualTo(1));

        Assert.That(imagePart.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(imagePart.LoadCount, Is.EqualTo(1));
    }

    [Test]
    public void Open_WithLazyBinaryPartsDisabledLoadsMediaBytesImmediately()
    {
        MemoryStream stream = CreatePackageStream(true);
        global::docx.DocxLoadOptions options = new global::docx.DocxLoadOptions();

        options.LazyLoadXmlParts = false;
        options.LazyLoadBinaryParts = false;

        global::docx.DocxPackage package = global::docx.DocxPackage.Open(stream, options);

        global::docx.DocxPart imagePart = package.Parts.Single(part => part.PartName == "/word/media/image1.png");

        Assert.That(imagePart.IsLazy, Is.False);
        Assert.That(imagePart.LoadCount, Is.EqualTo(0));
        Assert.That(imagePart.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Create_BuildsMinimalPackageWithContentTypesRootRelationshipAndDocumentPart()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();

        Assert.That(package.PartCount(), Is.EqualTo(2));
        Assert.That(package.RelationshipSetCount(), Is.EqualTo(1));
        Assert.That(package.ContentTypes.GetDefaultContentType("rels"), Is.EqualTo(global::docx.DocxConstants.ContentTypeRelationships));
        Assert.That(package.ContentTypes.GetDefaultContentType("xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeXml));
        Assert.That(package.ContentTypes.GetOverrideContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));

        global::docx.DocxPart? documentPart = package.FindPart("/word/document.xml");
        global::docx.DocxPart? relationshipPart = package.FindPart("/_rels/.rels");
        global::docx.DocxRelationshipSet rootRelationships = package.RelationshipSets.Single(set => set.SourcePartName == "/");

        Assert.That(documentPart, Is.Not.Null);
        Assert.That(documentPart!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(documentPart.GetText(), Is.EqualTo(global::docx.DocxPackage.CreateMinimalDocumentXml()));
        Assert.That(relationshipPart, Is.Not.Null);
        Assert.That(relationshipPart!.IsRelationshipPart, Is.True);
        Assert.That(rootRelationships.ResolveTargetPartNameById("rId1"), Is.EqualTo("/word/document.xml"));
    }

    [Test]
    public void Create_MinimalPackageCanBeSavedAndReopened()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        MemoryStream saved = new MemoryStream();

        package.Save(saved);

        saved.Position = 0;

        global::docx.DocxPackage roundTripped = global::docx.DocxPackage.Open(saved);

        Assert.That(roundTripped.ContentTypes.GetContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(roundTripped.FindPart("/word/document.xml")!.GetText(), Is.EqualTo(global::docx.DocxPackage.CreateMinimalDocumentXml()));
        Assert.That(roundTripped.RelationshipSets.Single(set => set.SourcePartName == "/").ResolveTargetPartNameById("rId1"), Is.EqualTo("/word/document.xml"));
    }

    [Test]
    public void Create_MinimalPackageZipEntriesContainOnlyCatalogRelationshipsAndDocument()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();

        options.Mode = global::docx.DocxSaveMode.DeterministicRoundTrip;

        List<global::docx.DocxZipWriteEntry> entries = package.CreateZipEntriesWithOptions(options);

        Assert.That(entries.Select(entry => entry.FullName).ToArray(), Is.EqualTo(new string[]
        {
            "[Content_Types].xml",
            "word/document.xml",
            "_rels/.rels"
        }));
        Assert.That(Encoding.UTF8.GetString(entries[0].Bytes), Does.Contain("PartName=\"/word/document.xml\""));
        Assert.That(Encoding.UTF8.GetString(entries[1].Bytes), Is.EqualTo(global::docx.DocxPackage.CreateMinimalDocumentXml()));
        Assert.That(Encoding.UTF8.GetString(entries[2].Bytes), Does.Contain(global::docx.DocxConstants.RelationshipTypeOfficeDocument));
        Assert.That(entries.All(entry => entry.IsDirectory == false), Is.True);
    }

    [Test]
    public void CreateRootRelationshipSet_UsesProvidedMainDocumentTarget()
    {
        global::docx.DocxRelationshipSet relationships = global::docx.DocxPackage.CreateRootRelationshipSet("/custom/main.xml");
        global::docx.DocxRelationship? relationship = relationships.FindById("rId1");

        Assert.That(relationships.SourcePartName, Is.EqualTo("/"));
        Assert.That(relationship, Is.Not.Null);
        Assert.That(relationship!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeOfficeDocument));
        Assert.That(relationship.Target, Is.EqualTo("custom/main.xml"));
        Assert.That(relationships.ResolveTargetPartNameById("rId1"), Is.EqualTo("/custom/main.xml"));
    }

    [Test]
    public void MainDocumentDiscovery_FindsCreatedMainDocumentPart()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxPart? part = package.GetMainDocumentPart();

        Assert.That(package.GetPackageRelationshipSet(), Is.Not.Null);
        Assert.That(package.FindMainDocumentRelationship(), Is.Not.Null);
        Assert.That(package.GetMainDocumentPartName(), Is.EqualTo("/word/document.xml"));
        Assert.That(part, Is.Not.Null);
        Assert.That(part!.PartName, Is.EqualTo("/word/document.xml"));
        Assert.That(package.HasMainDocumentPart(), Is.True);
    }

    [Test]
    public void MainDocumentDiscovery_UsesRootOfficeDocumentRelationshipTarget()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/custom/main-document.xml");

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/custom/main-document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<w:document />");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        Assert.That(package.GetMainDocumentPartName(), Is.EqualTo("/custom/main-document.xml"));
        Assert.That(package.GetMainDocumentPart()!.GetText(), Is.EqualTo("<w:document />"));
    }

    [Test]
    public void MainDocumentDiscovery_ReturnsMissingNameButNoPartWhenTargetIsAbsent()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/missing.xml");

        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        Assert.That(package.GetMainDocumentPartName(), Is.EqualTo("/word/missing.xml"));
        Assert.That(package.GetMainDocumentPart(), Is.Null);
        Assert.That(package.HasMainDocumentPart(), Is.False);
    }

    [Test]
    public void MainDocumentDiscovery_IgnoresExternalOfficeDocumentRelationships()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = new global::docx.DocxRelationshipSet("/");

        rootRelationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeOfficeDocument, "https://example.com/document.xml", true);
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        Assert.That(package.FindMainDocumentRelationship(), Is.Null);
        Assert.That(package.GetMainDocumentPartName(), Is.EqualTo(""));
        Assert.That(package.GetMainDocumentPart(), Is.Null);
        Assert.That(package.HasMainDocumentPart(), Is.False);
    }

    [Test]
    public void MacroDetection_UsesMainDocumentContentTypeVbaPartAndRelationship()
    {
        global::docx.DocxPackage package = CreateMacroEnabledPackage();

        Assert.That(package.IsMacroEnabled(), Is.True);
        Assert.That(package.HasMacroEnabledMainDocument(), Is.True);
        Assert.That(package.HasVbaProjectPart(), Is.True);
        Assert.That(package.GetVbaProjectPart(), Is.Not.Null);
        Assert.That(package.GetVbaProjectPart()!.GetBytes(), Is.EqualTo(new byte[] { 222, 173, 190, 239 }));
        Assert.That(package.FindVbaProjectRelationship(), Is.Not.Null);
        Assert.That(package.FindVbaProjectRelationship()!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeVbaProject));
    }

    [Test]
    public void MacroDetection_ReturnsFalseForPlainDocument()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();

        Assert.That(package.IsMacroEnabled(), Is.False);
        Assert.That(package.HasMacroEnabledMainDocument(), Is.False);
        Assert.That(package.HasVbaProjectPart(), Is.False);
        Assert.That(package.GetVbaProjectPart(), Is.Null);
        Assert.That(package.FindVbaProjectRelationship(), Is.Null);
    }

    [Test]
    public void SaveAndReopen_PreservesMacroPartBytesContentTypeAndRelationship()
    {
        global::docx.DocxPackage package = CreateMacroEnabledPackage();
        MemoryStream saved = new MemoryStream();

        package.Save(saved);

        saved.Position = 0;

        global::docx.DocxPackage roundTripped = global::docx.DocxPackage.Open(saved);
        global::docx.DocxPart? vbaPart = roundTripped.GetVbaProjectPart();

        Assert.That(roundTripped.IsMacroEnabled(), Is.True);
        Assert.That(roundTripped.HasMacroEnabledMainDocument(), Is.True);
        Assert.That(vbaPart, Is.Not.Null);
        Assert.That(vbaPart!.PartName, Is.EqualTo("/word/vbaProject.bin"));
        Assert.That(vbaPart.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeVbaProject));
        Assert.That(vbaPart.GetBytes(), Is.EqualTo(new byte[] { 222, 173, 190, 239 }));
        Assert.That(roundTripped.FindVbaProjectRelationship(), Is.Not.Null);
        Assert.That(roundTripped.FindRelationshipSet("/word/document.xml")!.ResolveTargetPartName(roundTripped.FindVbaProjectRelationship()!), Is.EqualTo("/word/vbaProject.bin"));
    }

    [Test]
    public void GetPartAndContainsPart_UseNormalizedPartNames()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));

        Assert.That(package.ContainsPart("word/document.xml"), Is.True);
        Assert.That(package.FindPart("\\word\\document.xml"), Is.Not.Null);
        Assert.That(package.FindPart("word/missing.xml"), Is.Null);
    }

    [Test]
    public void AddXmlPart_AddsPartAndUpdatesContentTypes()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));

        global::docx.DocxPart part = package.AddXmlPartWithContent("custom/item1.xml", "application/custom+xml", "<root />");

        Assert.That(part.PartName, Is.EqualTo("/custom/item1.xml"));
        Assert.That(part.GetText(), Is.EqualTo("<root />"));
        Assert.That(package.FindPart("/custom/item1.xml"), Is.SameAs(part));
        Assert.That(package.ContentTypes.GetContentType("/custom/item1.xml"), Is.EqualTo("application/custom+xml"));
    }

    [Test]
    public void AddBinaryPart_CopiesInputBytesAndRejectsDuplicates()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        byte[] bytes = new byte[] { 5, 6, 7 };

        global::docx.DocxPart part = package.AddBinaryPart("/media/image1.png", global::docx.DocxConstants.ContentTypePng, bytes);

        bytes[0] = 0;

        Assert.That(part.GetBytes(), Is.EqualTo(new byte[] { 5, 6, 7 }));
        Assert.Throws<InvalidOperationException>(() => package.AddBinaryPart("/media/image1.png", global::docx.DocxConstants.ContentTypePng, new byte[] { 1 }));
    }

    [Test]
    public void ReplacePart_UpdatesBytesContentTypeAndContentTypeCatalog()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));

        Assert.That(package.ReplaceXmlPart("word/document.xml", global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument, "<w:document macro=\"1\" />"), Is.True);

        global::docx.DocxPart? part = package.FindPart("/word/document.xml");

        Assert.That(part, Is.Not.Null);
        Assert.That(part!.GetText(), Is.EqualTo("<w:document macro=\"1\" />"));
        Assert.That(part.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument));
        Assert.That(package.ContentTypes.GetContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument));
        Assert.That(package.ReplaceBinaryPart("word/missing.xml", global::docx.DocxConstants.ContentTypeXml, new byte[] { 1 }), Is.False);
    }

    [Test]
    public void RemovePart_RemovesPartAndPartSpecificContentType()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));

        Assert.That(package.RemovePart("word/document.xml"), Is.True);
        Assert.That(package.FindPart("word/document.xml"), Is.Null);
        Assert.That(package.ContentTypes.GetOverrideContentType("/word/document.xml"), Is.EqualTo(""));
        Assert.That(package.RemovePart("word/document.xml"), Is.False);
    }

    [Test]
    public void RelationshipPartMutations_SynchronizeRelationshipSets()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();

        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, CreatePackageRelationshipsXml());

        Assert.That(package.RelationshipSetCount(), Is.EqualTo(1));
        Assert.That(package.RelationshipSets[0].SourcePartName, Is.EqualTo("/"));

        package.ReplaceXmlPart("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, CreateAlternatePackageRelationshipsXml());

        Assert.That(package.RelationshipSetCount(), Is.EqualTo(1));
        Assert.That(package.RelationshipSets[0].ResolveTargetPartNameById("rIdOther"), Is.EqualTo("/custom/other.xml"));

        Assert.That(package.RemovePart("_rels/.rels"), Is.True);
        Assert.That(package.RelationshipSetCount(), Is.EqualTo(0));
    }

    [Test]
    public void Save_PreservesUnknownPartBytesDuringRoundTrip()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));
        MemoryStream saved = new MemoryStream();

        package.Save(saved);

        saved.Position = 0;

        global::docx.DocxPackage roundTripped = global::docx.DocxPackage.Open(saved);
        global::docx.DocxPart? unknownPart = roundTripped.FindPart("/custom/data.bin");

        Assert.That(unknownPart, Is.Not.Null);
        Assert.That(unknownPart!.GetBytes(), Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(unknownPart.ContentType, Is.EqualTo(""));
    }

    [Test]
    public void Save_WritesContentTypesAndAllCatalogedParts()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));
        MemoryStream saved = new MemoryStream();

        package.Save(saved);

        saved.Position = 0;

        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();
        List<global::docx.DocxZipEntry> entries = reader.ReadEntries(saved);

        Assert.That(entries.Select(entry => entry.FullName), Does.Contain("[Content_Types].xml"));
        Assert.That(entries.Select(entry => entry.FullName), Does.Contain("custom/data.bin"));
        Assert.That(entries.Select(entry => entry.FullName), Does.Contain("word/document.xml"));
    }

    [Test]
    public void Save_RoundTripsAddedReplacedAndRemovedCatalogChanges()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));
        MemoryStream saved = new MemoryStream();

        package.AddXmlPartWithContent("/custom/item1.xml", "application/custom+xml", "<item>1</item>");
        package.ReplaceBinaryPart("/word/media/image1.png", global::docx.DocxConstants.ContentTypePng, new byte[] { 4, 5, 6 });
        package.RemovePart("/custom/data.bin");

        package.Save(saved);

        saved.Position = 0;

        global::docx.DocxPackage roundTripped = global::docx.DocxPackage.Open(saved);

        Assert.That(roundTripped.FindPart("/custom/item1.xml")!.GetText(), Is.EqualTo("<item>1</item>"));
        Assert.That(roundTripped.FindPart("/word/media/image1.png")!.GetBytes(), Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(roundTripped.FindPart("/custom/data.bin"), Is.Null);
        Assert.That(roundTripped.ContentTypes.GetContentType("/custom/item1.xml"), Is.EqualTo("application/custom+xml"));
        Assert.That(roundTripped.ContentTypes.GetContentType("/custom/data.bin"), Is.EqualTo(""));
    }

    [Test]
    public void Save_WithOptionsWritesDeterministicPackageEntries()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();
        MemoryStream saved = new MemoryStream();

        options.Deterministic = true;

        package.SaveWithOptions(saved, options);

        saved.Position = 0;

        using (ZipArchive archive = new ZipArchive(saved, ZipArchiveMode.Read, true))
        {
            Assert.That(archive.Entries.Select(entry => entry.FullName).ToArray(), Is.EqualTo(new string[]
            {
                "[Content_Types].xml",
                "_rels/.rels",
                "word/document.xml"
            }));

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Assert.That(entry.LastWriteTime.DateTime, Is.EqualTo(new DateTime(1980, 1, 1, 0, 0, 0)));
            }
        }
    }

    [Test]
    public void Save_WithDeterministicRoundTripModeProducesStableBytesAfterReopen()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();

        options.Mode = global::docx.DocxSaveMode.DeterministicRoundTrip;

        package.SaveWithOptions(first, options);

        first.Position = 0;

        global::docx.DocxPackage reopened = global::docx.DocxPackage.Open(first);

        reopened.SaveWithOptions(second, options);

        Assert.That(second.ToArray(), Is.EqualTo(first.ToArray()));
    }

    [Test]
    public void Save_WithDeterministicRoundTripModeSortsEntriesTimestampsAndContentTypes()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Open(CreatePackageStream(true));
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();
        MemoryStream saved = new MemoryStream();

        options.Mode = global::docx.DocxSaveMode.DeterministicRoundTrip;

        package.SaveWithOptions(saved, options);

        saved.Position = 0;

        using (ZipArchive archive = new ZipArchive(saved, ZipArchiveMode.Read, true))
        {
            Assert.That(archive.Entries.Select(entry => entry.FullName).ToArray(), Is.EqualTo(new string[]
            {
                "[Content_Types].xml",
                "_rels/.rels",
                "custom/data.bin",
                "word/_rels/document.xml.rels",
                "word/document.xml",
                "word/media/image1.png"
            }));

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Assert.That(entry.LastWriteTime.DateTime, Is.EqualTo(new DateTime(1980, 1, 1, 0, 0, 0)));
            }

            ZipArchiveEntry? contentTypesEntry = archive.GetEntry("[Content_Types].xml");

            Assert.That(contentTypesEntry, Is.Not.Null);

            using (Stream stream = contentTypesEntry!.Open())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string xml = reader.ReadToEnd();

                    Assert.That(xml.IndexOf("Extension=\"png\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("Extension=\"rels\"", StringComparison.Ordinal)));
                    Assert.That(xml.IndexOf("Extension=\"rels\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("Extension=\"xml\"", StringComparison.Ordinal)));
                    Assert.That(xml.IndexOf("PartName=\"/word/document.xml\"", StringComparison.Ordinal), Is.GreaterThan(0));
                }
            }
        }
    }

    [Test]
    public void SaveFile_WritesPackageThatCanBeOpened()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "created-package.docx");
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();

        package.SaveFile(path);

        global::docx.DocxPackage opened = global::docx.DocxPackage.OpenFile(path);

        Assert.That(File.Exists(path), Is.True);
        Assert.That(opened.ContentTypes.GetContentType("/word/document.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeMainDocument));
        Assert.That(opened.FindPart("/word/document.xml")!.GetText(), Is.EqualTo(global::docx.DocxPackage.CreateMinimalDocumentXml()));
        Assert.That(opened.RelationshipSets.Single(set => set.SourcePartName == "/").ResolveTargetPartNameById("rId1"), Is.EqualTo("/word/document.xml"));
    }

    [Test]
    public void SaveFile_WithOptionsOverwritesExistingFile()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "overwrite-package.docx");
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();

        options.Deterministic = true;
        File.WriteAllText(path, "old");

        package.SaveFileWithOptions(path, options);

        global::docx.DocxPackage opened = global::docx.DocxPackage.OpenFile(path);

        Assert.That(opened.PartCount(), Is.EqualTo(2));
        Assert.That(opened.ContainsPart("/_rels/.rels"), Is.True);
        Assert.That(opened.ContainsPart("/word/document.xml"), Is.True);
    }

    [Test]
    public void PackageAndPart_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.Open(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.Open(new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.OpenFile(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.OpenFile(null!, new global::docx.DocxLoadOptions()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.OpenFile(Path.Combine(TestContext.CurrentContext.WorkDirectory, "null-load-options.docx"), null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().Save(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().SaveWithOptions(new MemoryStream(), null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().SaveFile(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().SaveFileWithOptions(null!, new global::docx.DocxSaveOptions()));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().SaveFileWithOptions(Path.Combine(TestContext.CurrentContext.WorkDirectory, "null-options.docx"), null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().CreateZipEntriesWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().LoadPartsWithOptions(null!, new global::docx.DocxLoadOptions(), new byte[0]));
        using (MemoryStream emptyPackageStream = new MemoryStream())
        {
            using (ZipArchive emptyArchive = new ZipArchive(emptyPackageStream, ZipArchiveMode.Create, true))
            {
                Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().LoadPartsWithOptions(emptyArchive, null!, new byte[0]));
                Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().LoadPartsWithOptions(emptyArchive, new global::docx.DocxLoadOptions(), null!));
            }
        }
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.IsDirectoryEntryName(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.NormalizeZipEntryName(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadXmlPart(null!, "", false, new global::docx.DocxLoadOptions()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadXmlPart("/word/document.xml", null!, false, new global::docx.DocxLoadOptions()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadXmlPart("/word/document.xml", "", false, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadBinaryPart(null!, "", false, new global::docx.DocxLoadOptions()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadBinaryPart("/word/media/image1.png", null!, false, new global::docx.DocxLoadOptions()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadBinaryPart("/word/media/image1.png", "", false, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadPart(null!, "", false, new global::docx.DocxLoadOptions()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadPart("/word/document.xml", null!, false, new global::docx.DocxLoadOptions()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ShouldLazyLoadPart("/word/document.xml", "", false, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.IsXmlPart(null!, ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.IsXmlPart("/word/document.xml", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ReadStreamBytes(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.ReadZipEntryBytes(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPackage.CreateRootRelationshipSet(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart(null!, "", new byte[0], false));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", null!, new byte[0], false));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", "", null!, false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CreateLazy(null!, "", new byte[0], "word/document.xml", false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CreateLazy("/word/document.xml", null!, new byte[0], "word/document.xml", false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CreateLazy("/word/document.xml", "", null!, "word/document.xml", false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CreateLazy("/word/document.xml", "", new byte[0], null!, false));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", "", new byte[0], false).ReplaceText(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", "", new byte[0], false).ReplaceBytes(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", "", new byte[0], false).SetContentType(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().AddXmlPartWithContent("word/document.xml", global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().AddBinaryPart("word/document.xml", null!, new byte[0]));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().AddBinaryPart("word/document.xml", global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxPackage().AddBinaryPart("word/document.xml", "", new byte[0]));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().ReplaceXmlPart("word/document.xml", global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().ReplaceBinaryPart("word/document.xml", null!, new byte[0]));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().ReplaceBinaryPart("word/document.xml", global::docx.DocxConstants.ContentTypeXml, null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxPackage().ReplaceBinaryPart("word/document.xml", "", new byte[0]));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().SynchronizeRelationshipPart(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().IsMacroPart(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPackage().IsMacroRelationship(null!));
    }

    static global::docx.DocxPackage CreateMacroEnabledPackage()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument, global::docx.DocxPackage.CreateMinimalDocumentXml());
        package.AddBinaryPart("/word/vbaProject.bin", global::docx.DocxConstants.ContentTypeVbaProject, new byte[] { 222, 173, 190, 239 });
        relationships.CreateRelationshipWithId("rIdVba", global::docx.DocxConstants.RelationshipTypeVbaProject, "vbaProject.bin", false);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, relationships.WriteXml());

        return package;
    }

    static MemoryStream CreatePackageStream(bool includeContentTypes)
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            if (includeContentTypes)
            {
                AddTextEntry(archive, "[Content_Types].xml", CreateContentTypesXml());
            }

            AddTextEntry(archive, "_rels/.rels", CreatePackageRelationshipsXml());
            AddTextEntry(archive, "word/document.xml", "<w:document />");
            AddTextEntry(archive, "word/_rels/document.xml.rels", CreateDocumentRelationshipsXml());
            AddBinaryEntry(archive, "word/media/image1.png", new byte[] { 1, 2, 3 });
            AddBinaryEntry(archive, "custom/data.bin", new byte[] { 9, 8, 7 });
        }

        stream.Position = 0;
        return stream;
    }

    static MemoryStream CreatePackageStreamWithDirectoryEntries()
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddDirectoryEntry(archive, "word/");
            AddDirectoryEntry(archive, "word/media/");
            AddTextEntry(archive, "[Content_Types].xml", CreateContentTypesXml());
            AddTextEntry(archive, "_rels/.rels", CreatePackageRelationshipsXml());
            AddTextEntry(archive, "word/document.xml", "<w:document />");
            AddTextEntry(archive, "word/_rels/document.xml.rels", CreateDocumentRelationshipsXml());
            AddBinaryEntry(archive, "word/media/image1.png", new byte[] { 1, 2, 3 });
            AddBinaryEntry(archive, "custom/data.bin", new byte[] { 9, 8, 7 });
        }

        stream.Position = 0;
        return stream;
    }

    static string CreateContentTypesXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">",
            "  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />",
            "  <Default Extension=\"xml\" ContentType=\"application/xml\" />",
            "  <Default Extension=\"png\" ContentType=\"image/png\" />",
            "  <Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\" />",
            "</Types>"
        });
    }

    static string CreatePackageRelationshipsXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">",
            "  <Relationship Id=\"rIdMain\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\" />",
            "</Relationships>"
        });
    }

    static string CreateAlternatePackageRelationshipsXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">",
            "  <Relationship Id=\"rIdOther\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"custom/other.xml\" />",
            "</Relationships>"
        });
    }

    static string CreateDocumentRelationshipsXml()
    {
        return string.Join("\n", new string[]
        {
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">",
            "  <Relationship Id=\"rIdImage\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"media/image1.png\" />",
            "</Relationships>"
        });
    }

    static void AddTextEntry(ZipArchive archive, string entryName, string text)
    {
        AddBinaryEntry(archive, entryName, Encoding.UTF8.GetBytes(text));
    }

    static void AddBinaryEntry(ZipArchive archive, string entryName, byte[] bytes)
    {
        ZipArchiveEntry entry = archive.CreateEntry(entryName);

        using (Stream stream = entry.Open())
        {
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    static void AddDirectoryEntry(ZipArchive archive, string entryName)
    {
        archive.CreateEntry(entryName);
    }
}
