using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class UnknownPartStoreTests
{
    [Test]
    public void LoadFromPackage_StoresUnsupportedPartsWithContentTypes()
    {
        byte[] packageBytes = CreatePackageWithUnknownParts();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(packageBytes, new XlsxReaderOptions());
        XlsxContentTypesPart contentTypes = new XlsxContentTypesReader().ReadBytes(package.GetEntry(XlsxPackagePaths.ContentTypes)!.Data);
        XlsxUnknownPartStore store = new XlsxUnknownPartStore();

        store.LoadFromPackage(package, contentTypes);

        Assert.That(store.IsValid, Is.True);
        Assert.That(store.Parts.Count, Is.EqualTo(2));
        Assert.That(store.ContainsPart("/custom/item.bin"), Is.True);
        Assert.That(store.ContainsPart(XlsxPackagePaths.Worksheet1), Is.True);
        Assert.That(store.GetPart("/custom/item.bin")!.ContentType, Is.EqualTo("application/x-custom"));
        Assert.That(store.GetPart(XlsxPackagePaths.Worksheet1)!.ContentType, Is.EqualTo(XlsxContentTypes.Xml));
        Assert.That(System.Text.Encoding.UTF8.GetString(store.GetPart("/custom/item.bin")!.Data), Is.EqualTo("custom-data"));
    }

    [Test]
    public void AddToEntries_RoundTripsUnknownPartsWithoutKnownWorkbookBytes()
    {
        byte[] packageBytes = CreatePackageWithUnknownParts();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(packageBytes, new XlsxReaderOptions());
        XlsxContentTypesPart contentTypes = new XlsxContentTypesReader().ReadBytes(package.GetEntry(XlsxPackagePaths.ContentTypes)!.Data);
        XlsxUnknownPartStore store = new XlsxUnknownPartStore();
        store.LoadFromPackage(package, contentTypes);
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes, package.GetEntry(XlsxPackagePaths.ContentTypes)!.Data));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships, package.GetEntry(XlsxPackagePaths.PackageRelationships)!.Data));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook, System.Text.Encoding.UTF8.GetBytes("<workbook updated=\"1\" />")));

        store.AddToEntries(entries);
        byte[] writtenBytes = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions()).Bytes;
        XlsxZipPackageReadResult writtenPackage = new XlsxZipPackageReader().ReadBytes(writtenBytes, new XlsxReaderOptions());

        Assert.That(writtenPackage.GetEntry(XlsxPackagePaths.Workbook)!.GetText(), Is.EqualTo("<workbook updated=\"1\" />"));
        Assert.That(writtenPackage.GetEntry("/custom/item.bin")!.GetText(), Is.EqualTo("custom-data"));
        Assert.That(writtenPackage.GetEntry(XlsxPackagePaths.Worksheet1)!.GetText(), Is.EqualTo("<worksheet />"));
    }

    [Test]
    public void AddContentTypesToPart_AddsMissingOverridesForManualUnknownParts()
    {
        XlsxUnknownPartStore store = new XlsxUnknownPartStore();
        IXlsxUnknownPartStoreOperations operations = new XlsxUnknownPartStoreOperations();
        operations.AddPart(store, "/custom/item.bin", "application/x-custom", System.Text.Encoding.UTF8.GetBytes("custom-data"));
        XlsxContentTypesPart contentTypes = CreateBaseContentTypes();

        operations.AddContentTypesToPart(store, contentTypes);

        Assert.That(store.IsValid, Is.True);
        Assert.That(operations.ContainsPart(store, "/custom/item.bin"), Is.True);
        Assert.That(operations.GetPart(store, "/custom/item.bin")!.Data, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("custom-data")));
        Assert.That(contentTypes.GetOverride("/custom/item.bin"), Is.Not.Null);
        Assert.That(contentTypes.GetOverride("/custom/item.bin")!.ContentType, Is.EqualTo("application/x-custom"));
        Assert.That(operations.RemovePart(store, "/custom/item.bin"), Is.True);
    }

    [Test]
    public void AddToEntries_DoesNotOverwriteExplicitEntries()
    {
        XlsxUnknownPartStore store = new XlsxUnknownPartStore();
        store.AddPart("/custom/item.bin", "application/x-custom", System.Text.Encoding.UTF8.GetBytes("stored"));
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry("/custom/item.bin", System.Text.Encoding.UTF8.GetBytes("explicit")));

        store.AddToEntries(entries);
        byte[] writtenBytes = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions()).Bytes;
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(writtenBytes, new XlsxReaderOptions());

        Assert.That(entries.Count, Is.EqualTo(1));
        Assert.That(package.GetEntry("/custom/item.bin")!.GetText(), Is.EqualTo("explicit"));
    }

    [Test]
    public void Store_ReportsDuplicateInvalidAndNullInputs()
    {
        XlsxUnknownPartStore store = new XlsxUnknownPartStore();

        store.AddPart("/custom/item.bin", "application/x-custom", new byte[] { 1 });
        store.AddPart("custom/../item.bin", "application/x-custom", new byte[] { 2 });
        store.AddPart("/custom/item.bin", "application/x-custom", new byte[] { 3 });
        store.AddToEntries(null);
        store.AddContentTypesToPart(null);

        Assert.That(store.IsValid, Is.False);
        Assert.That(HasDiagnostic(store, "path.parentSegment"), Is.True);
        Assert.That(HasDiagnostic(store, "unknownParts.duplicatePart"), Is.True);
        Assert.That(HasDiagnostic(store, "unknownParts.entriesNull"), Is.True);
        Assert.That(HasDiagnostic(store, "unknownParts.contentTypesNull"), Is.True);
        Assert.That(store.RemovePart("/custom/item.bin"), Is.True);
        Assert.That(store.RemovePart("/custom/item.bin"), Is.False);
    }

    static byte[] CreatePackageWithUnknownParts()
    {
        XlsxContentTypesPart contentTypes = CreateBaseContentTypes();
        XlsxContentTypeDefault xmlDefault = new XlsxContentTypeDefault();
        xmlDefault.Extension = "xml";
        xmlDefault.ContentType = XlsxContentTypes.Xml;
        contentTypes.Defaults.Add(xmlDefault);
        XlsxContentTypeOverride customOverride = new XlsxContentTypeOverride();
        customOverride.PartName = "/custom/item.bin";
        customOverride.ContentType = "application/x-custom";
        contentTypes.Overrides.Add(customOverride);
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = "/";
        relationships.PartPath = XlsxPackagePaths.PackageRelationships;
        XlsxRelationship officeDocument = new XlsxRelationship();
        officeDocument.Id = "rId1";
        officeDocument.Type = XlsxRelationshipTypes.OfficeDocument;
        officeDocument.Target = "xl/workbook.xml";
        relationships.Relationships.Add(officeDocument);
        XlsxContentTypesWriteResult contentTypesWrite = new XlsxContentTypesWriter().WriteBytes(contentTypes);
        XlsxRelationshipsWriteResult relationshipsWrite = new XlsxPartRelationshipsWriter().WriteBytes(relationships);
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes, contentTypesWrite.Bytes));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships, relationshipsWrite.Bytes));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook, System.Text.Encoding.UTF8.GetBytes("<workbook />")));
        entries.Add(CreateEntry("/custom/item.bin", System.Text.Encoding.UTF8.GetBytes("custom-data")));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1, System.Text.Encoding.UTF8.GetBytes("<worksheet />")));
        return new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions()).Bytes;
    }

    static XlsxContentTypesPart CreateBaseContentTypes()
    {
        XlsxContentTypesPart contentTypes = new XlsxContentTypesPart();
        XlsxContentTypeDefault relsDefault = new XlsxContentTypeDefault();
        relsDefault.Extension = "rels";
        relsDefault.ContentType = XlsxContentTypes.Relationships;
        contentTypes.Defaults.Add(relsDefault);
        XlsxContentTypeOverride workbookOverride = new XlsxContentTypeOverride();
        workbookOverride.PartName = XlsxPackagePaths.Workbook;
        workbookOverride.ContentType = XlsxContentTypes.Workbook;
        contentTypes.Overrides.Add(workbookOverride);
        return contentTypes;
    }

    static XlsxPackageEntry CreateEntry(string path, byte[] data)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = data;
        entry.UncompressedBytes = data.Length;
        return entry;
    }

    static bool HasDiagnostic(XlsxUnknownPartStore store, string code)
    {
        foreach (XlsxDiagnostic diagnostic in store.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
