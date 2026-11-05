using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PackageContentAuthoringTests
{
    [Test]
    public void WorkbookOperations_AuthorAndRemoveOleObjectsAndEmbeddedPackagesFromRawBytes()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxRelationshipsPart relationships = WorksheetRelationships(worksheet);
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();

        XlsxEmbeddedObjectPart ole = operations.AddOleObject(workbook, worksheet, relationships, OleBytes(), "Excel.Sheet.12", 1025);
        XlsxEmbeddedObjectPart packagePart = operations.AddEmbeddedPackage(workbook, worksheet, relationships, PackageBytes(), XlsxContentTypes.EmbeddedSpreadsheetPackage, "xlsx", "Package", 1026);
        XlsxWorkbookSaveResult authoredSave = workbook.SaveBytes();
        XlsxZipPackageReadResult authoredPackage = ReadPackage(authoredSave.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(ole.IsValid, Is.True);
            Assert.That(ole.IsOleObject, Is.True);
            Assert.That(packagePart.IsValid, Is.True);
            Assert.That(packagePart.IsEmbeddedPackage, Is.True);
            Assert.That(workbook.EmbeddedObjects.Count, Is.EqualTo(2));
            Assert.That(worksheet.EmbeddedObjectReferences.Count, Is.EqualTo(2));
            Assert.That(relationships.Relationships.Count, Is.EqualTo(2));
            Assert.That(authoredSave.IsValid, Is.True);
            Assert.That(authoredPackage.GetEntry(ole.PartPath)!.Data, Is.EqualTo(OleBytes()));
            Assert.That(authoredPackage.GetEntry(packagePart.PartPath)!.Data, Is.EqualTo(PackageBytes()));
            AssertEntryText(authoredPackage, worksheet.PartPath, "<oleObjects>");
            AssertEntryText(authoredPackage, worksheet.PartPath, "progId=\"Excel.Sheet.12\"");
            AssertEntryText(authoredPackage, worksheet.PartPath, "shapeId=\"1026\"");
            AssertEntryText(authoredPackage, relationships.PartPath, XlsxRelationshipTypes.OleObject);
            AssertEntryText(authoredPackage, relationships.PartPath, XlsxRelationshipTypes.EmbeddedPackage);
        });

        XlsxValidationResult removal = operations.RemoveEmbeddedObject(workbook, worksheet, relationships, ole.PartPath);
        XlsxWorkbookSaveResult removedSave = workbook.SaveBytes();
        XlsxZipPackageReadResult removedPackage = ReadPackage(removedSave.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(removal.IsValid, Is.True);
            Assert.That(workbook.EmbeddedObjects.Count, Is.EqualTo(1));
            Assert.That(worksheet.EmbeddedObjectReferences.Count, Is.EqualTo(1));
            Assert.That(relationships.Relationships.Count, Is.EqualTo(1));
            Assert.That(removedSave.IsValid, Is.True);
            Assert.That(removedPackage.GetEntry(ole.PartPath), Is.Null);
            Assert.That(removedPackage.GetEntry(packagePart.PartPath), Is.Not.Null);
            AssertEntryTextDoesNotContain(removedPackage, relationships.PartPath, XlsxRelationshipTypes.OleObject);
        });
    }

    [Test]
    public void WorkbookOperations_AddReplaceAndRemoveVbaProjectRelationship()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();

        XlsxVbaProjectPart first = operations.SetVbaProject(workbook, VbaBytes(1));
        XlsxVbaProjectPart replacement = operations.SetVbaProject(workbook, VbaBytes(2));
        XlsxWorkbookSaveResult macroSave = workbook.SaveBytes();
        XlsxZipPackageReadResult macroPackage = ReadPackage(macroSave.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsValid, Is.True);
            Assert.That(replacement.IsValid, Is.True);
            Assert.That(workbook.HasVbaProject, Is.True);
            Assert.That(workbook.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xlsm));
            Assert.That(CountRelationships(workbook.WorkbookRelationships, XlsxRelationshipTypes.VbaProject), Is.EqualTo(1));
            Assert.That(macroSave.IsValid, Is.True);
            Assert.That(macroPackage.GetEntry(XlsxPackagePaths.VbaProject)!.Data, Is.EqualTo(VbaBytes(2)));
            AssertEntryText(macroPackage, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.VbaProject);
            AssertEntryText(macroPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.MacroEnabledWorkbook);
        });

        XlsxValidationResult removal = operations.RemoveVbaProject(workbook);
        XlsxWorkbookSaveResult cleanSave = workbook.SaveBytes();
        XlsxZipPackageReadResult cleanPackage = ReadPackage(cleanSave.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(removal.IsValid, Is.True);
            Assert.That(workbook.HasVbaProject, Is.False);
            Assert.That(workbook.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xlsx));
            Assert.That(cleanPackage.GetEntry(XlsxPackagePaths.VbaProject), Is.Null);
            AssertEntryTextDoesNotContain(cleanPackage, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.VbaProject);
        });
    }

    [Test]
    public void WorkbookOperations_AuthorAndRemoveRawInCellPictureMetadata()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();
        byte[] sheetMetadata = Encoding.UTF8.GetBytes("<metadata xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><metadataTypes count=\"0\"/></metadata>");
        byte[] richValues = Encoding.UTF8.GetBytes("<rvData xmlns=\"http://schemas.microsoft.com/office/spreadsheetml/2017/richdata\" count=\"0\"/>");

        XlsxInCellPictureMetadataPart sheet = operations.AddInCellPictureMetadata(workbook, XlsxPackagePaths.SheetMetadata, XlsxContentTypes.SheetMetadata, sheetMetadata);
        XlsxInCellPictureMetadataPart rich = operations.AddInCellPictureMetadata(workbook, "/xl/richData/rdrichvalue.xml", XlsxContentTypes.RichValue, richValues);
        XlsxWorkbookSaveResult authoredSave = workbook.SaveBytes();
        XlsxZipPackageReadResult authoredPackage = ReadPackage(authoredSave.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.IsValid, Is.True);
            Assert.That(rich.IsValid, Is.True);
            Assert.That(workbook.InCellPictureMetadataParts.Count, Is.EqualTo(2));
            Assert.That(authoredPackage.GetEntry(XlsxPackagePaths.SheetMetadata)!.Data, Is.EqualTo(sheetMetadata));
            Assert.That(authoredPackage.GetEntry(rich.PartPath)!.Data, Is.EqualTo(richValues));
            AssertEntryText(authoredPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.SheetMetadata);
            AssertEntryText(authoredPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.RichValue);
        });

        XlsxValidationResult removal = operations.RemoveInCellPictureMetadata(workbook, rich.PartPath);
        XlsxWorkbookSaveResult removedSave = workbook.SaveBytes();
        XlsxZipPackageReadResult removedPackage = ReadPackage(removedSave.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(removal.IsValid, Is.True);
            Assert.That(workbook.InCellPictureMetadataParts.Count, Is.EqualTo(1));
            Assert.That(removedPackage.GetEntry(rich.PartPath), Is.Null);
            Assert.That(removedPackage.GetEntry(XlsxPackagePaths.SheetMetadata), Is.Not.Null);
        });
    }

    [Test]
    public void DrawingOperations_AuthorAndRemoveNamedFormControls()
    {
        XlsxDrawingPart drawing = new XlsxDrawingPart();
        IXlsxDrawingPartOperations operations = new XlsxDrawingPartOperations();
        XlsxDrawingObjectOperationResult add = operations.AddOneCellFormControl(drawing, "rIdControl1", "B3", "500000", "200000", "Check Box 1");
        XlsxDrawingPartWriteResult authored = new XlsxDrawingPartWriter().WriteString(drawing);
        XlsxDrawingObjectOperationResult removal = operations.RemoveFormControl(drawing, 0);
        XlsxDrawingPartWriteResult removed = new XlsxDrawingPartWriter().WriteString(drawing);

        Assert.Multiple(() =>
        {
            Assert.That(add.IsValid, Is.True);
            Assert.That(authored.IsValid, Is.True);
            Assert.That(authored.Xml, Does.Contain("<xdr:control r:id=\"rIdControl1\""));
            Assert.That(authored.Xml, Does.Contain("name=\"Check Box 1\""));
            Assert.That(removal.IsValid, Is.True);
            Assert.That(removal.Anchor!.RelationshipId, Is.EqualTo("rIdControl1"));
            Assert.That(drawing.FormControlAnchors.Count, Is.EqualTo(0));
            Assert.That(removed.IsValid, Is.True);
            Assert.That(removed.Xml, Does.Not.Contain("<xdr:control"));
        });
    }

    [Test]
    public void WorksheetReaderWriter_PreservesUnsupportedOleObjectMarkup()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheetData/><oleObjects><oleObject progId=\"Excel.Sheet.12\" shapeId=\"2048\" r:id=\"rId7\"><objectPr defaultSize=\"0\"><anchor moveWithCells=\"1\"/></objectPr></oleObject></oleObjects></worksheet>";

        XlsxWorksheetPart worksheet = new XlsxWorksheetPartReader().ReadString(xml);
        XlsxWorksheetPartWriteResult written = new XlsxWorksheetPartWriter().WriteString(worksheet);

        Assert.Multiple(() =>
        {
            Assert.That(worksheet.EmbeddedObjectReferences.Count, Is.EqualTo(1));
            Assert.That(worksheet.EmbeddedObjectReferences[0].RelationshipId, Is.EqualTo("rId7"));
            Assert.That(written.IsValid, Is.True);
            Assert.That(written.Xml, Does.Contain("<objectPr defaultSize=\"0\">"));
            Assert.That(written.Xml, Does.Contain("<anchor moveWithCells=\"1\""));
        });
    }

    [Test]
    public void WorkbookOperations_AuthorAndRemovePackageAwareFormControlProperties()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxDrawingPart drawing = new XlsxDrawingPart();
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = drawing.PartPath;
        relationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(drawing.PartPath);
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();
        byte[] properties = Encoding.UTF8.GetBytes("<formControlPr xmlns=\"http://schemas.microsoft.com/office/spreadsheetml/2009/9/main\" objectType=\"CheckBox\"/>");

        XlsxDrawingObjectOperationResult add = operations.AddFormControl(workbook, drawing, relationships, properties, "", "C4", "400000", "200000", "Approval");
        XlsxWorkbookSaveResult authoredSave = workbook.SaveBytes();
        XlsxZipPackageReadResult authoredPackage = ReadPackage(authoredSave.Bytes);
        string propertyPath = relationships.Relationships[0].TargetPath;

        Assert.Multiple(() =>
        {
            Assert.That(add.IsValid, Is.True);
            Assert.That(relationships.Relationships[0].Type, Is.EqualTo(XlsxRelationshipTypes.ControlProperties));
            Assert.That(authoredPackage.GetEntry(propertyPath)!.Data, Is.EqualTo(properties));
            AssertEntryText(authoredPackage, drawing.PartPath, "<xdr:control");
            AssertEntryText(authoredPackage, relationships.PartPath, XlsxRelationshipTypes.ControlProperties);
            AssertEntryText(authoredPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.ControlProperties);
        });

        XlsxDrawingObjectOperationResult removal = operations.RemoveFormControl(workbook, drawing, relationships, 0);
        XlsxWorkbookSaveResult removedSave = workbook.SaveBytes();
        XlsxZipPackageReadResult removedPackage = ReadPackage(removedSave.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(removal.IsValid, Is.True);
            Assert.That(relationships.Relationships.Count, Is.EqualTo(0));
            Assert.That(removedPackage.GetEntry(propertyPath), Is.Null);
            AssertEntryTextDoesNotContain(removedPackage, drawing.PartPath, "<xdr:control");
        });
    }

    [Test]
    public void PackageContentOperations_ReportInvalidPathsAndMissingParts()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxRelationshipsPart relationships = WorksheetRelationships(worksheet);
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();

        XlsxEmbeddedObjectPart badObject = operations.AddRawEmbeddedObject(workbook, worksheet, relationships, "/xl/media/object.bin", XlsxContentTypes.OleObject, OleBytes(), true, "Package", 1);
        XlsxInCellPictureMetadataPart badMetadata = operations.AddInCellPictureMetadata(workbook, "/xl/custom/metadata.xml", "application/xml", Encoding.UTF8.GetBytes("<metadata/>"));
        XlsxValidationResult missingObject = operations.RemoveEmbeddedObject(workbook, worksheet, relationships, "/xl/embeddings/missing.bin");
        XlsxValidationResult missingMetadata = operations.RemoveInCellPictureMetadata(workbook, "/xl/richData/missing.xml");

        Assert.Multiple(() =>
        {
            Assert.That(badObject.IsValid, Is.False);
            Assert.That(badMetadata.IsValid, Is.False);
            Assert.That(missingObject.IsValid, Is.False);
            Assert.That(missingMetadata.IsValid, Is.False);
            Assert.That(HasDiagnostic(badObject.Validation, "embeddedObject.pathInvalid"), Is.True);
            Assert.That(HasDiagnostic(badMetadata.Validation, "inCellPictureMetadata.pathInvalid"), Is.True);
            Assert.That(HasDiagnostic(missingObject, "embeddedObject.partNotFound"), Is.True);
            Assert.That(HasDiagnostic(missingMetadata, "inCellPictureMetadata.partNotFound"), Is.True);
        });
    }

    static XlsxRelationshipsPart WorksheetRelationships(XlsxWorksheetPart worksheet)
    {
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = worksheet.PartPath;
        relationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(worksheet.PartPath);
        return relationships;
    }

    static byte[] OleBytes() => new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 1, 2, 3, 4 };
    static byte[] PackageBytes() => new byte[] { 0x50, 0x4B, 0x03, 0x04, 5, 6, 7, 8 };
    static byte[] VbaBytes(byte marker) => new byte[] { 0xD0, 0xCF, 0x11, 0xE0, marker };

    static XlsxZipPackageReadResult ReadPackage(byte[] bytes) => new XlsxZipPackageReader().ReadBytes(bytes, new XlsxReaderOptions());

    static int CountRelationships(XlsxRelationshipsPart relationships, string type)
    {
        int count = 0;
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            if (relationship.Type == type) { count = count + 1; }
        }
        return count;
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code) { return true; }
        }
        return false;
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string text)
    {
        Assert.That(package.GetEntry(path), Is.Not.Null);
        Assert.That(package.GetEntry(path)!.GetText(), Does.Contain(text));
    }

    static void AssertEntryTextDoesNotContain(XlsxZipPackageReadResult package, string path, string text)
    {
        Assert.That(package.GetEntry(path), Is.Not.Null);
        Assert.That(package.GetEntry(path)!.GetText(), Does.Not.Contain(text));
    }
}
