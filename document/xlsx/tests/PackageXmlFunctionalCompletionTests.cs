using System;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PackageXmlFunctionalCompletionTests
{
    [Test]
    public void WorkbookOperations_AllPublicOpenAndSavePathsRoundTripPackageContent()
    {
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.UnknownParts.AddPart("/xl/future/item.bin", "application/vnd.example.future", new byte[] { 7, 8, 9 });
        XlsxWriterOptions writerOptions = new XlsxWriterOptions();
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        string path = Path.GetTempFileName();
        try
        {
            XlsxWorkbookSaveResult bytes = operations.SaveBytes(workbook);
            XlsxWorkbookSaveResult bytesWithOptions = operations.SaveBytesWithOptions(workbook, writerOptions);
            using MemoryStream firstStream = new MemoryStream();
            XlsxWorkbookSaveResult stream = operations.SaveStream(workbook, firstStream);
            using MemoryStream secondStream = new MemoryStream();
            XlsxWorkbookSaveResult streamWithOptions = operations.SaveStreamWithOptions(workbook, secondStream, writerOptions);
            XlsxWorkbookSaveResult file = operations.SaveFile(workbook, path);
            XlsxWorkbookSaveResult fileWithOptions = operations.SaveFileWithOptions(workbook, path, writerOptions);

            XlsxWorkbookOpenResult fromBytes = operations.OpenBytes(bytes.Bytes);
            XlsxWorkbookOpenResult fromBytesWithOptions = operations.OpenBytesWithOptions(bytesWithOptions.Bytes, readerOptions);
            XlsxWorkbookOpenResult fromMemory = operations.OpenMemory(new ReadOnlyMemory<byte>(bytes.Bytes));
            XlsxWorkbookOpenResult fromMemoryWithOptions = operations.OpenMemoryWithOptions(new ReadOnlyMemory<byte>(bytes.Bytes), readerOptions);
            XlsxWorkbookOpenResult fromSpan = operations.OpenSpan(bytes.Bytes.AsSpan());
            XlsxWorkbookOpenResult fromSpanWithOptions = operations.OpenSpanWithOptions(bytes.Bytes.AsSpan(), readerOptions);
            firstStream.Position = 0;
            secondStream.Position = 0;
            XlsxWorkbookOpenResult fromStream = operations.OpenStream(firstStream);
            XlsxWorkbookOpenResult fromStreamWithOptions = operations.OpenStreamWithOptions(secondStream, readerOptions);
            XlsxWorkbookOpenResult fromFile = operations.OpenFile(path);
            XlsxWorkbookOpenResult fromFileWithOptions = operations.OpenFileWithOptions(path, readerOptions);

            Assert.That(bytes.IsValid && bytesWithOptions.IsValid && stream.IsValid && streamWithOptions.IsValid && file.IsValid && fileWithOptions.IsValid, Is.True);
            Assert.That(fromBytes.IsValid && fromBytesWithOptions.IsValid && fromMemory.IsValid && fromMemoryWithOptions.IsValid && fromSpan.IsValid && fromSpanWithOptions.IsValid && fromStream.IsValid && fromStreamWithOptions.IsValid && fromFile.IsValid && fromFileWithOptions.IsValid, Is.True);
            Assert.That(fromFileWithOptions.Workbook!.UnknownParts.ContainsPart("/xl/future/item.bin"), Is.True);
            Assert.That(firstStream.CanRead, Is.True);
            Assert.That(secondStream.CanWrite, Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void WorkbookOperations_UnreadableAndUnwritableStreamsReturnDiagnostics()
    {
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();
        string path = Path.GetTempFileName();
        try
        {
            using FileStream writeOnly = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            XlsxWorkbookOpenResult open = operations.OpenStream(writeOnly);
            writeOnly.Dispose();
            using FileStream readOnly = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            XlsxWorkbookSaveResult save = operations.SaveStream(XlsxWorkbook.Create(), readOnly);

            Assert.That(open.IsValid, Is.False);
            Assert.That(save.IsValid, Is.False);
            Assert.That(HasDiagnostic(open.Validation, "workbook.open.streamNotReadable"), Is.True);
            Assert.That(HasDiagnostic(save.Validation, "workbook.save.streamNotWritable"), Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCase(XlsxNamespaces.Spreadsheet)]
    [TestCase(XlsxNamespaces.StrictSpreadsheet)]
    public void WorkbookAndWorksheetWriters_PreserveSpreadsheetNamespaceOnEveryElement(string namespaceUri)
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.WorkbookPart.NamespaceUri = namespaceUri;
        workbook.WorkbookPart.CalculationProperties.HasFullCalcOnLoad = true;
        workbook.WorkbookPart.CalculationProperties.FullCalcOnLoad = true;
        XlsxWorkbookPartWriteResult workbookWrite = new XlsxWorkbookPartWriter().WriteString(workbook.WorkbookPart);
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.NamespaceUri = namespaceUri;
        worksheet.SetFormulaCachedValue("A1", "1+1", "2", "n");
        worksheet.SetInlineStringValue("A2", "strict text");
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteString(worksheet);

        Assert.That(workbookWrite.IsValid, Is.True);
        Assert.That(worksheetWrite.IsValid, Is.True);
        AssertAllElementsUseNamespace(workbookWrite.Xml, namespaceUri);
        AssertAllElementsUseNamespace(worksheetWrite.Xml, namespaceUri);
    }

    [Test]
    public void StrictPackage_SaveReopenPreservesMarkupCompatibilityUnknownPartsAndThumbnail()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.WorkbookPart.NamespaceUri = XlsxNamespaces.StrictSpreadsheet;
        workbook.WorkbookPart.MarkupCompatibility.Ignorable = "x14";
        XlsxXmlNamespace markupNamespace = new XlsxXmlNamespace();
        markupNamespace.Prefix = "mc";
        markupNamespace.NamespaceUri = XlsxNamespaces.MarkupCompatibility;
        workbook.WorkbookPart.MarkupCompatibility.NamespaceDeclarations.Add(markupNamespace);
        XlsxXmlNamespace futureNamespace = new XlsxXmlNamespace();
        futureNamespace.Prefix = "x14";
        futureNamespace.NamespaceUri = XlsxNamespaces.Office2010Spreadsheet;
        workbook.WorkbookPart.MarkupCompatibility.NamespaceDeclarations.Add(futureNamespace);
        workbook.WorkbookPart.UnknownXml.Add("<mc:AlternateContent xmlns:mc=\"" + XlsxNamespaces.MarkupCompatibility + "\" xmlns:x14=\"" + XlsxNamespaces.Office2010Spreadsheet + "\"><mc:Choice Requires=\"x14\"><x14:future /></mc:Choice><mc:Fallback><fallback xmlns=\"" + XlsxNamespaces.StrictSpreadsheet + "\" /></mc:Fallback></mc:AlternateContent>");
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.NamespaceUri = XlsxNamespaces.StrictSpreadsheet;
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        workbook.UnknownParts.AddPart(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);
        byte[] thumbnail = new byte[] { 137, 80, 78, 71, 5, 4, 3, 2 };
        workbook.UnknownParts.AddPart("/docProps/thumbnail.png", "image/png", thumbnail);
        workbook.UnknownParts.AddPart("/xl/future/opaque.bin", "application/vnd.example.opaque", new byte[] { 1, 3, 5, 7 });
        workbook.PackageRelationships.Relationships.Add(CreateRelationship("rIdThumbnail", XlsxRelationshipTypes.Thumbnail, "docProps/thumbnail.png"));

        XlsxWorkbookSaveResult firstSave = workbook.SaveBytes();
        XlsxWorkbookOpenResult firstOpen = XlsxWorkbook.OpenBytes(firstSave.Bytes);
        XlsxWorkbookSaveResult secondSave = firstOpen.Workbook!.SaveBytes();
        XlsxWorkbookOpenResult secondOpen = XlsxWorkbook.OpenBytes(secondSave.Bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(secondSave.Bytes, new XlsxReaderOptions());

        Assert.That(firstSave.IsValid && firstOpen.IsValid && secondSave.IsValid && secondOpen.IsValid, Is.True);
        Assert.That(secondOpen.Workbook!.Conformance, Is.EqualTo(XlsxPackageConformance.Strict));
        Assert.That(secondOpen.Workbook.WorkbookPart.MarkupCompatibility.Ignorable, Is.EqualTo("x14"));
        Assert.That(secondOpen.Workbook.WorkbookPart.UnknownXml[0], Does.Contain("AlternateContent"));
        Assert.That(secondOpen.Workbook.UnknownParts.GetPart("/xl/future/opaque.bin")!.Data, Is.EqualTo(new byte[] { 1, 3, 5, 7 }));
        Assert.That(secondOpen.Workbook.PreviewMetadata.HasThumbnail, Is.True);
        Assert.That(secondOpen.Workbook.PreviewMetadata.Data, Is.EqualTo(thumbnail));
        Assert.That(package.GetEntry(XlsxPackagePaths.Workbook)!.GetText(), Does.Contain(XlsxNamespaces.StrictSpreadsheet));
        Assert.That(package.GetEntry(XlsxPackagePaths.Workbook)!.GetText(), Does.Contain("mc:AlternateContent"));
        Assert.That(package.GetEntry("/docProps/thumbnail.png")!.Data, Is.EqualTo(thumbnail));
    }

    [Test]
    public void ContentTypesAndRelationships_AllPublicReadWriteAndLookupOperationsAgree()
    {
        string contentTypesXml = "<Types xmlns=\"" + XlsxNamespaces.ContentTypes + "\"><Default Extension=\"XML\" ContentType=\"" + XlsxContentTypes.Xml + "\" /><Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" /></Types>";
        byte[] contentTypesBytes = Encoding.UTF8.GetBytes(contentTypesXml);
        IXlsxContentTypesReader contentReader = new XlsxContentTypesReader();
        XlsxContentTypesPart fromString = contentReader.ReadString(contentTypesXml);
        XlsxContentTypesPart fromBytes = contentReader.ReadBytes(contentTypesBytes);
        XlsxContentTypesPart fromMemory = contentReader.ReadMemory(new ReadOnlyMemory<byte>(contentTypesBytes));
        XlsxContentTypesPart fromSpan = contentReader.ReadSpan(contentTypesBytes.AsSpan());
        using MemoryStream contentInput = new MemoryStream(contentTypesBytes);
        XlsxContentTypesPart fromStream = contentReader.ReadStream(contentInput);
        IXlsxContentTypesPartOperations contentOperations = new XlsxContentTypesPartOperations();
        IXlsxContentTypesWriter contentWriter = new XlsxContentTypesWriter();
        using MemoryStream contentOutput = new MemoryStream();
        XlsxContentTypesWriteResult contentString = contentWriter.WriteString(fromString);
        XlsxContentTypesWriteResult contentBytes = contentWriter.WriteBytes(fromString);
        XlsxContentTypesWriteResult contentStream = contentWriter.WriteStream(contentOutput, fromString);

        string relationshipsXml = "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\"><Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.Worksheet + "\" Target=\"worksheets/sheet1.xml\" /><Relationship Id=\"rId2\" Type=\"" + XlsxRelationshipTypes.Hyperlink + "\" Target=\"https://example.invalid\" TargetMode=\"External\" /></Relationships>";
        byte[] relationshipBytes = Encoding.UTF8.GetBytes(relationshipsXml);
        IXlsxPartRelationshipsReader relationshipReader = new XlsxPartRelationshipsReader();
        XlsxReaderOptions options = new XlsxReaderOptions();
        XlsxRelationshipsPart relationships = relationshipReader.ReadString(XlsxPackagePaths.Workbook, relationshipsXml);
        XlsxRelationshipsPart relationshipBytesResult = relationshipReader.ReadBytes(XlsxPackagePaths.Workbook, relationshipBytes, options);
        XlsxRelationshipsPart relationshipMemory = relationshipReader.ReadMemory(XlsxPackagePaths.Workbook, new ReadOnlyMemory<byte>(relationshipBytes), options);
        XlsxRelationshipsPart relationshipSpan = relationshipReader.ReadSpan(XlsxPackagePaths.Workbook, relationshipBytes.AsSpan(), options);
        using MemoryStream relationshipInput = new MemoryStream(relationshipBytes);
        XlsxRelationshipsPart relationshipStream = relationshipReader.ReadStream(XlsxPackagePaths.Workbook, relationshipInput, options);
        IXlsxRelationshipsPartOperations relationshipOperations = new XlsxRelationshipsPartOperations();
        IXlsxPartRelationshipsWriter relationshipWriter = new XlsxPartRelationshipsWriter();
        using MemoryStream relationshipOutput = new MemoryStream();
        XlsxRelationshipsWriteResult relationshipString = relationshipWriter.WriteString(relationships);
        XlsxRelationshipsWriteResult relationshipWriteBytes = relationshipWriter.WriteBytes(relationships);
        XlsxRelationshipsWriteResult relationshipWriteStream = relationshipWriter.WriteStream(relationshipOutput, relationships);

        Assert.That(fromString.IsValid && fromBytes.IsValid && fromMemory.IsValid && fromSpan.IsValid && fromStream.IsValid, Is.True);
        Assert.That(contentOperations.GetDefault(fromString, ".xml"), Is.Not.Null);
        Assert.That(contentOperations.GetOverride(fromString, "xl/workbook.xml"), Is.Not.Null);
        Assert.That(contentOperations.GetContentType(fromString, "/XL/ITEM.XML"), Is.EqualTo(XlsxContentTypes.Xml));
        Assert.That(contentString.IsValid && contentBytes.IsValid && contentStream.IsValid, Is.True);
        Assert.That(relationships.IsValid && relationshipBytesResult.IsValid && relationshipMemory.IsValid && relationshipSpan.IsValid && relationshipStream.IsValid, Is.True);
        Assert.That(relationshipOperations.GetRelationship(relationships, "rId1")!.TargetPath, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(relationshipOperations.GetFirstByType(relationships, XlsxRelationshipTypes.Hyperlink)!.IsExternal, Is.True);
        Assert.That(relationshipString.IsValid && relationshipWriteBytes.IsValid && relationshipWriteStream.IsValid, Is.True);
        Assert.That(contentInput.CanRead && contentOutput.CanWrite && relationshipInput.CanRead && relationshipOutput.CanWrite, Is.True);
    }

    [Test]
    public void PackagePaths_RejectMalformedAndAmbiguousPercentEscapes()
    {
        IXlsxPackagePathResultOperations operations = new XlsxPackagePathResultOperations();
        XlsxPackagePathResult valid = XlsxPackagePath.NormalizePartPath("/xl/my%20file.xml");
        XlsxPackagePathResult malformed = XlsxPackagePath.NormalizePartPath("/xl/item%2.xml");
        XlsxPackagePathResult slash = XlsxPackagePath.NormalizePartPath("/xl%2Fworksheets/sheet1.xml");
        XlsxPackagePathResult dot = XlsxPackagePath.NormalizePartPath("/xl/%2e%2e/evil.xml");
        XlsxPackagePathResult colon = XlsxPackagePath.NormalizePartPath("/xl/https%3Aexample.xml");
        XlsxPackagePathResult control = XlsxPackagePath.NormalizePartPath("/xl/item%00.xml");
        XlsxPackagePathResult relationship = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, "worksheets/%2e%2e/evil.xml", false);

        Assert.That(operations.IsValid(valid), Is.True);
        Assert.That(valid.Path, Is.EqualTo("/xl/my%20file.xml"));
        Assert.That(HasDiagnostic(malformed.Validation, "path.percentEncoding"), Is.True);
        Assert.That(HasDiagnostic(slash.Validation, "path.escapedSeparator"), Is.True);
        Assert.That(HasDiagnostic(dot.Validation, "path.escapedDelimiter"), Is.True);
        Assert.That(HasDiagnostic(colon.Validation, "path.escapedDelimiter"), Is.True);
        Assert.That(HasDiagnostic(control.Validation, "path.escapedControl"), Is.True);
        Assert.That(operations.IsValid(relationship), Is.False);
    }

    [Test]
    public void WorkbookPartReader_ConfiguredXmlLimitsApplyToDirectPublicReads()
    {
        string xml = "<workbook xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" first=\"1\" second=\"2\"><sheets /></workbook>";
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.SecurityOptions.MaxXmlAttributesPerElement = 2;
        XlsxWorkbookPart part = new XlsxWorkbookPartReader().ReadStringWithOptions(xml, options);

        Assert.That(part.IsValid, Is.False);
        Assert.That(HasDiagnostic(part.Validation, "xml.maxAttributes"), Is.True);
    }

    static XlsxRelationship CreateRelationship(string id, string type, string target)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = id;
        relationship.Type = type;
        relationship.Target = target;
        return relationship;
    }

    static void AssertAllElementsUseNamespace(string xml, string expectedNamespace)
    {
        using StringReader text = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(text, XlsxXmlReaderHelpers.CreateSafeReaderSettings());
        int elementCount = 0;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element) { continue; }
            elementCount = elementCount + 1;
            Assert.That(reader.NamespaceURI, Is.EqualTo(expectedNamespace), "Element " + reader.LocalName + " used an unexpected namespace.");
        }
        Assert.That(elementCount, Is.GreaterThan(1));
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics) { if (diagnostic.Code == code) { return true; } }
        return false;
    }
}
