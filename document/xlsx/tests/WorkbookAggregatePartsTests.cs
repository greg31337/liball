using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookAggregatePartsTests
{
    [Test]
    public void OpenBytes_LoadsPlannedPartsIntoWorkbookAggregate()
    {
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(CreateAggregatePackage());

        Assert.That(open.IsValid, Is.True, Diagnostics(open.Validation));
        Assert.That(open.Workbook, Is.Not.Null);
        XlsxWorkbook workbook = open.Workbook!;
        Assert.That(workbook.HasSharedStrings, Is.True);
        Assert.That(workbook.SharedStrings.GetText(0), Is.EqualTo("Original shared"));
        Assert.That(workbook.HasCalculationChain, Is.True);
        Assert.That(workbook.CalculationChain.Cells[0].Reference, Is.EqualTo("A1"));
        Assert.That(workbook.Chartsheets.Count, Is.EqualTo(1));
        Assert.That(workbook.Macrosheets.Count, Is.EqualTo(1));
        Assert.That(workbook.Macrosheets[0].Name, Is.EqualTo("Macro"));
        Assert.That(workbook.Dialogsheets.Count, Is.EqualTo(1));
        Assert.That(workbook.Drawings.Count, Is.EqualTo(1));
        Assert.That(workbook.Images.Count, Is.EqualTo(1));
        Assert.That(workbook.Comments.Count, Is.EqualTo(1));
        Assert.That(workbook.Comments[0].Comments[0].Text, Is.EqualTo("Original note"));
        Assert.That(workbook.ThreadedComments.Count, Is.EqualTo(1));
        Assert.That(workbook.ThreadedComments[0].Comments[0].Text, Is.EqualTo("Original thread"));
        Assert.That(workbook.HasConnections, Is.True);
        Assert.That(workbook.Connections.Connections[0].Name, Is.EqualTo("Original connection"));
        Assert.That(workbook.CoreProperties.Title, Is.EqualTo("Original title"));
        Assert.That(workbook.ExtendedProperties.Company, Is.EqualTo("Original company"));
        Assert.That(workbook.CustomProperties.Properties[0].TextValue, Is.EqualTo("Original custom"));
        Assert.That(workbook.PackageParts.Count, Is.GreaterThan(15));
        Assert.That(new XlsxWorkbookOperations().GetPackagePart(workbook, "/xl/future/future.bin")!.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void AggregateReplacements_SaveAndReopenThroughWorkbook()
    {
        XlsxWorkbook workbook = XlsxWorkbook.OpenBytes(CreateAggregatePackage()).Workbook!;
        workbook.SharedStrings = SharedStrings("Replacement shared");
        workbook.CalculationChain = CalculationChain("B2");
        workbook.Chartsheets[0] = Chartsheet("replacement-chart");
        workbook.Drawings[0] = Drawing("replacement-drawing");
        workbook.Images[0] = new XlsxImagePartReader().ReadBytesWithContentType("/xl/media/image1.png", "image/png", PngBytes(0x44));
        workbook.Comments[0] = Comments("Replacement note");
        workbook.ThreadedComments[0] = ThreadedComments("Replacement thread");
        workbook.Connections = Connections("Replacement connection");
        workbook.CoreProperties = CoreProperties("Replacement title");
        workbook.ExtendedProperties = ExtendedProperties("Replacement company");
        workbook.CustomProperties = CustomProperties("Replacement custom");
        workbook.Macrosheets[0].Name = "Macro replaced";
        workbook.Dialogsheets[0].Name = "Dialog replaced";
        XlsxPackagePart replacementPart = new XlsxPackagePart();
        replacementPart.Path = "/xl/future/future.bin";
        replacementPart.ContentType = "application/vnd.example.future";
        replacementPart.Data = new byte[] { 9, 8, 7, 6 };
        XlsxValidationResult replace = new XlsxWorkbookOperations().ReplacePackagePart(workbook, replacementPart);

        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxWorkbookOpenResult reopened = XlsxWorkbook.OpenBytes(save.Bytes);

        Assert.That(replace.IsValid, Is.True, Diagnostics(replace));
        Assert.That(save.IsValid, Is.True, Diagnostics(save.Validation));
        Assert.That(reopened.IsValid, Is.True, Diagnostics(reopened.Validation));
        XlsxWorkbook roundTrip = reopened.Workbook!;
        Assert.That(roundTrip.SharedStrings.GetText(0), Is.EqualTo("Replacement shared"));
        Assert.That(roundTrip.CalculationChain.Cells[0].Reference, Is.EqualTo("B2"));
        Assert.That(Encoding.UTF8.GetString(roundTrip.GetPackagePart("/xl/chartsheets/sheet1.xml")!.Data), Does.Contain("replacement-chart"));
        Assert.That(Encoding.UTF8.GetString(roundTrip.GetPackagePart("/xl/drawings/drawing1.xml")!.Data), Does.Contain("replacement-drawing"));
        Assert.That(roundTrip.Images[0].Data, Is.EqualTo(PngBytes(0x44)));
        Assert.That(roundTrip.Comments[0].Comments[0].Text, Is.EqualTo("Replacement note"));
        Assert.That(roundTrip.ThreadedComments[0].Comments[0].Text, Is.EqualTo("Replacement thread"));
        Assert.That(roundTrip.Connections.Connections[0].Name, Is.EqualTo("Replacement connection"));
        Assert.That(roundTrip.CoreProperties.Title, Is.EqualTo("Replacement title"));
        Assert.That(roundTrip.ExtendedProperties.Company, Is.EqualTo("Replacement company"));
        Assert.That(roundTrip.CustomProperties.Properties[0].TextValue, Is.EqualTo("Replacement custom"));
        Assert.That(roundTrip.Macrosheets[0].Name, Is.EqualTo("Macro replaced"));
        Assert.That(roundTrip.Dialogsheets[0].Name, Is.EqualTo("Dialog replaced"));
        Assert.That(new XlsxWorkbookOperations().GetPackagePart(roundTrip, "/xl/future/future.bin")!.Data, Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
    }

    static byte[] CreateAggregatePackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(Entry(XlsxPackagePaths.ContentTypes, ContentTypesXml()));
        entries.Add(Entry(XlsxPackagePaths.PackageRelationships, PackageRelationshipsXml()));
        entries.Add(Entry(XlsxPackagePaths.Workbook, WorkbookXml()));
        entries.Add(Entry(XlsxPackagePaths.WorkbookRelationships, WorkbookRelationshipsXml()));
        entries.Add(Entry(XlsxPackagePaths.Worksheet1, "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><sheetData /></worksheet>"));
        entries.Add(Entry("/xl/chartsheets/sheet1.xml", Chartsheet("original-chart").RawXml));
        entries.Add(Entry("/xl/macrosheets/sheet1.xml", "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><sheetData /></worksheet>"));
        entries.Add(Entry("/xl/dialogSheets/sheet1.xml", "<dialogsheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" />"));
        entries.Add(Entry(XlsxPackagePaths.SharedStrings, new XlsxSharedStringTableWriter().WriteBytes(SharedStrings("Original shared")).Bytes));
        entries.Add(Entry(XlsxPackagePaths.CalculationChain, new XlsxCalculationChainPartWriter().WriteBytes(CalculationChain("A1")).Bytes));
        entries.Add(Entry("/xl/drawings/drawing1.xml", Drawing("original-drawing").RawXml));
        entries.Add(Entry("/xl/media/image1.png", PngBytes(0x11)));
        entries.Add(Entry("/xl/comments/comment1.xml", new XlsxCommentsPartWriter().WriteBytes(Comments("Original note")).Bytes));
        entries.Add(Entry("/xl/threadedComments/threadedComment1.xml", new XlsxThreadedCommentsPartWriter().WriteBytes(ThreadedComments("Original thread")).Bytes));
        entries.Add(Entry(XlsxPackagePaths.Connections, new XlsxWorkbookConnectionsPartWriter().WriteBytes(Connections("Original connection")).Bytes));
        entries.Add(Entry(XlsxPackagePaths.CoreProperties, new XlsxCorePropertiesWriter().WriteBytes(CoreProperties("Original title")).Bytes));
        entries.Add(Entry(XlsxPackagePaths.ExtendedProperties, new XlsxExtendedPropertiesWriter().WriteBytes(ExtendedProperties("Original company")).Bytes));
        entries.Add(Entry(XlsxPackagePaths.CustomProperties, new XlsxCustomPropertiesWriter().WriteBytes(CustomProperties("Original custom")).Bytes));
        entries.Add(Entry("/xl/future/future.bin", new byte[] { 1, 2, 3 }));
        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static XlsxSharedStringTablePart SharedStrings(string text)
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();
        XlsxSharedStringItem item = new XlsxSharedStringItem();
        item.Text = text;
        item.ReferenceCount = 1;
        part.Items.Add(item);
        return part;
    }

    static XlsxCalculationChainPart CalculationChain(string reference)
    {
        XlsxCalculationChainPart part = new XlsxCalculationChainPart();
        XlsxCalculationChainCell cell = new XlsxCalculationChainCell();
        cell.Reference = reference;
        part.Cells.Add(cell);
        return part;
    }

    static XlsxChartsheetPart Chartsheet(string marker)
    {
        XlsxChartsheetPart part = new XlsxChartsheetPart();
        part.PartPath = "/xl/chartsheets/sheet1.xml";
        part.RawXml = "<chartsheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><extLst><ext uri=\"" + marker + "\" /></extLst></chartsheet>";
        return part;
    }

    static XlsxDrawingPart Drawing(string marker)
    {
        XlsxDrawingPart part = new XlsxDrawingPart();
        part.PartPath = "/xl/drawings/drawing1.xml";
        part.RawXml = "<xdr:wsDr xmlns:xdr=\"" + XlsxNamespaces.SpreadsheetDrawing + "\"><xdr:twoCellAnchor><xdr:clientData name=\"" + marker + "\" /></xdr:twoCellAnchor></xdr:wsDr>";
        return part;
    }

    static XlsxCommentsPart Comments(string text)
    {
        XlsxCommentsPart part = new XlsxCommentsPart();
        part.PartPath = "/xl/comments/comment1.xml";
        part.Authors.Add("Ada");
        XlsxComment comment = new XlsxComment();
        comment.Reference = "A1";
        comment.AuthorId = 0;
        comment.HasAuthorId = true;
        comment.Text = text;
        part.Comments.Add(comment);
        return part;
    }

    static XlsxThreadedCommentsPart ThreadedComments(string text)
    {
        XlsxThreadedCommentsPart part = new XlsxThreadedCommentsPart();
        part.PartPath = "/xl/threadedComments/threadedComment1.xml";
        XlsxThreadedComment comment = new XlsxThreadedComment();
        comment.Reference = "B2";
        comment.Id = "{11111111-1111-1111-1111-111111111111}";
        comment.PersonId = "{22222222-2222-2222-2222-222222222222}";
        comment.DateTimeText = "2026-08-27T00:00:00Z";
        comment.Text = text;
        part.Comments.Add(comment);
        return part;
    }

    static XlsxWorkbookConnectionsPart Connections(string name)
    {
        XlsxWorkbookConnectionsPart part = new XlsxWorkbookConnectionsPart();
        XlsxWorkbookConnectionInfo connection = new XlsxWorkbookConnectionInfo();
        connection.Id = 1;
        connection.HasId = true;
        connection.Name = name;
        part.Connections.Add(connection);
        return part;
    }

    static XlsxCorePropertiesPart CoreProperties(string title)
    {
        XlsxCorePropertiesPart part = new XlsxCorePropertiesPart();
        part.Title = title;
        return part;
    }

    static XlsxExtendedPropertiesPart ExtendedProperties(string company)
    {
        XlsxExtendedPropertiesPart part = new XlsxExtendedPropertiesPart();
        part.Company = company;
        return part;
    }

    static XlsxCustomPropertiesPart CustomProperties(string value)
    {
        XlsxCustomPropertiesPart part = new XlsxCustomPropertiesPart();
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.Name = "AggregateProperty";
        property.PropertyId = 2;
        property.ValueType = "lpwstr";
        property.TextValue = value;
        property.HasTextValue = true;
        part.Properties.Add(property);
        return part;
    }

    static byte[] PngBytes(byte tail)
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, tail };
    }

    static string ContentTypesXml()
    {
        return "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            Override(XlsxPackagePaths.Workbook, XlsxContentTypes.Workbook) +
            Override(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet) +
            Override("/xl/chartsheets/sheet1.xml", XlsxContentTypes.Chartsheet) +
            Override("/xl/macrosheets/sheet1.xml", XlsxContentTypes.Macrosheet) +
            Override("/xl/dialogSheets/sheet1.xml", XlsxContentTypes.Dialogsheet) +
            Override(XlsxPackagePaths.SharedStrings, XlsxContentTypes.SharedStrings) +
            Override(XlsxPackagePaths.CalculationChain, XlsxContentTypes.CalculationChain) +
            Override("/xl/drawings/drawing1.xml", XlsxContentTypes.Drawing) +
            Override("/xl/media/image1.png", "image/png") +
            Override("/xl/comments/comment1.xml", XlsxContentTypes.Comments) +
            Override("/xl/threadedComments/threadedComment1.xml", XlsxContentTypes.ThreadedComments) +
            Override(XlsxPackagePaths.Connections, XlsxContentTypes.Connections) +
            Override(XlsxPackagePaths.CoreProperties, XlsxContentTypes.CoreProperties) +
            Override(XlsxPackagePaths.ExtendedProperties, XlsxContentTypes.ExtendedProperties) +
            Override(XlsxPackagePaths.CustomProperties, XlsxContentTypes.CustomProperties) +
            Override("/xl/future/future.bin", "application/vnd.example.future") + "</Types>";
    }

    static string PackageRelationshipsXml()
    {
        return "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdOffice", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            Relationship("rIdCore", XlsxRelationshipTypes.CoreProperties, "docProps/core.xml") +
            Relationship("rIdApp", XlsxRelationshipTypes.ExtendedProperties, "docProps/app.xml") +
            Relationship("rIdCustom", XlsxRelationshipTypes.CustomProperties, "docProps/custom.xml") + "</Relationships>";
    }

    static string WorkbookXml()
    {
        return "<workbook xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\"><sheets>" +
            "<sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" />" +
            "<sheet name=\"Chart\" sheetId=\"2\" r:id=\"rIdChart1\" />" +
            "<sheet name=\"Macro\" sheetId=\"3\" r:id=\"rIdMacro1\" />" +
            "<sheet name=\"Dialog\" sheetId=\"4\" r:id=\"rIdDialog1\" />" +
            "</sheets></workbook>";
    }

    static string WorkbookRelationshipsXml()
    {
        return "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rIdChart1", XlsxRelationshipTypes.Chartsheet, "chartsheets/sheet1.xml") +
            Relationship("rIdMacro1", XlsxRelationshipTypes.Macrosheet, "macrosheets/sheet1.xml") +
            Relationship("rIdDialog1", XlsxRelationshipTypes.Dialogsheet, "dialogSheets/sheet1.xml") +
            Relationship("rIdShared", XlsxRelationshipTypes.SharedStrings, "sharedStrings.xml") +
            Relationship("rIdCalc", XlsxRelationshipTypes.CalculationChain, "calcChain.xml") +
            Relationship("rIdConnections", XlsxRelationshipTypes.Connections, "connections.xml") + "</Relationships>";
    }

    static string Override(string path, string contentType)
    {
        return "<Override PartName=\"" + path + "\" ContentType=\"" + contentType + "\" />";
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static XlsxPackageEntry Entry(string path, string text)
    {
        return Entry(path, Encoding.UTF8.GetBytes(text));
    }

    static XlsxPackageEntry Entry(string path, byte[] data)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = data;
        entry.UncompressedBytes = data.Length;
        return entry;
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        string text = "";
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            text = text + diagnostic.Code + ": " + diagnostic.Message + "\n";
        }
        return text;
    }
}
