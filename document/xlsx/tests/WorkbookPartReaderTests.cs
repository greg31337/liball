using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookPartReaderTests
{
    [Test]
    public void ReadString_ParsesSheetsViewsNamesCalculationProtectionAndExtensions()
    {
        string xml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:x=\"urn:x\" xmlns:x15=\"urn:x15\" mc:Ignorable=\"x15\" x:root=\"r\">" +
            "<bookViews><workbookView visibility=\"visible\" activeTab=\"1\" firstSheet=\"0\" tabRatio=\"600\" xWindow=\"10\" yWindow=\"20\" windowWidth=\"12000\" windowHeight=\"8000\" x:view=\"v\" /></bookViews>" +
            "<workbookProtection workbookPassword=\"ABCD\" revisionsPassword=\"1234\" lockStructure=\"1\" lockWindows=\"false\" lockRevision=\"0\" x:protect=\"p\" />" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" state=\"visible\" x:sheet=\"s\" /><sheet name=\"Hidden\" sheetId=\"2\" r:id=\"rId2\" state=\"hidden\" /></sheets>" +
            "<definedNames><definedName name=\"_xlnm.Print_Area\" localSheetId=\"0\" hidden=\"1\" function=\"0\" comment=\"print area\" x:name=\"n\">Sheet1!$A$1:$B$2</definedName></definedNames>" +
            "<calcPr calcId=\"191029\" calcMode=\"auto\" refMode=\"A1\" fullCalcOnLoad=\"1\" forceFullCalc=\"0\" iterate=\"1\" iterateCount=\"100\" iterateDelta=\"0.001\" x:calc=\"c\" />" +
            "<extLst><ext uri=\"{22222222-2222-2222-2222-222222222222}\"><x15:workbookPr /></ext></extLst>" +
            "<x:future><x:child /></x:future>" +
            "</workbook>";
        XlsxWorkbookPartReader reader = new XlsxWorkbookPartReader();

        XlsxWorkbookPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.NamespaceUri, Is.EqualTo(XlsxNamespaces.Spreadsheet));
        Assert.That(part.MarkupCompatibility.Ignorable, Is.EqualTo("x15"));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Sheets.Count, Is.EqualTo(2));
        Assert.That(part.Sheets[0].Name, Is.EqualTo("Sheet1"));
        Assert.That(part.Sheets[0].SheetId, Is.EqualTo(1));
        Assert.That(part.Sheets[0].RelationshipId, Is.EqualTo("rId1"));
        Assert.That(part.Sheets[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.GetSheetByName("Hidden")!.State, Is.EqualTo("hidden"));
        Assert.That(part.WorkbookViews.Count, Is.EqualTo(1));
        Assert.That(part.WorkbookViews[0].ActiveTab, Is.EqualTo(1));
        Assert.That(part.WorkbookViews[0].WindowWidth, Is.EqualTo(12000));
        Assert.That(part.WorkbookViews[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.HasWorkbookProtection, Is.True);
        Assert.That(part.WorkbookProtection!.WorkbookPassword, Is.EqualTo("ABCD"));
        Assert.That(part.WorkbookProtection.LockStructure, Is.True);
        Assert.That(part.WorkbookProtection.LockWindows, Is.False);
        Assert.That(part.WorkbookProtection.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.DefinedNames.Count, Is.EqualTo(1));
        Assert.That(part.DefinedNames[0].Name, Is.EqualTo("_xlnm.Print_Area"));
        Assert.That(part.DefinedNames[0].Text, Is.EqualTo("Sheet1!$A$1:$B$2"));
        Assert.That(part.DefinedNames[0].HasLocalSheetId, Is.True);
        Assert.That(part.DefinedNames[0].LocalSheetId, Is.EqualTo(0));
        Assert.That(part.DefinedNames[0].Hidden, Is.True);
        Assert.That(part.HasCalculationProperties, Is.True);
        Assert.That(part.CalculationProperties.CalcId, Is.EqualTo(191029));
        Assert.That(part.CalculationProperties.CalcMode, Is.EqualTo("auto"));
        Assert.That(part.CalculationProperties.FullCalcOnLoad, Is.True);
        Assert.That(part.CalculationProperties.ForceFullCalc, Is.False);
        Assert.That(part.CalculationProperties.IterateCount, Is.EqualTo(100));
        Assert.That(part.CalculationProperties.IterateDelta, Is.EqualTo(0.001d).Within(0.0000001d));
        Assert.That(part.CalculationProperties.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.HasExtensionList, Is.True);
        Assert.That(part.ExtensionList.Extensions.Count, Is.EqualTo(1));
        Assert.That(part.ExtensionList.Extensions[0].RawXml, Does.Contain("x15:workbookPr"));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml[0], Does.Contain("future"));
    }

    [Test]
    public void ReadString_AcceptsStrictSpreadsheetNamespace()
    {
        string xml = "<workbook xmlns=\"http://purl.oclc.org/ooxml/spreadsheetml/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "</workbook>";
        XlsxWorkbookPartReader reader = new XlsxWorkbookPartReader();

        XlsxWorkbookPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.NamespaceUri, Is.EqualTo(XlsxNamespaces.StrictSpreadsheet));
        Assert.That(part.Sheets.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ReportsMissingOrInvalidRoot()
    {
        XlsxWorkbookPartReader reader = new XlsxWorkbookPartReader();

        XlsxWorkbookPart missingRoot = reader.ReadString("");
        XlsxWorkbookPart invalidRoot = reader.ReadString("<bad xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" />");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "workbook.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "workbook.invalidRoot"), Is.True);
    }

    [Test]
    public void ReadString_ReportsMissingSheetAttributesAndInvalidValueAttributes()
    {
        string xml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<bookViews><workbookView activeTab=\"bad\" /></bookViews>" +
            "<sheets><sheet sheetId=\"bad\" /></sheets>" +
            "<definedNames><definedName hidden=\"maybe\">A1</definedName></definedNames>" +
            "<calcPr fullCalcOnLoad=\"maybe\" iterateDelta=\"bad\" />" +
            "<workbookProtection lockStructure=\"maybe\" />" +
            "</workbook>";
        XlsxWorkbookPartReader reader = new XlsxWorkbookPartReader();

        XlsxWorkbookPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.False);
        Assert.That(HasDiagnostic(part, "workbook.sheet.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "workbook.sheet.relationshipIdMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(part, "workbook.definedName.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(part, "xml.double.invalid"), Is.True);
        Assert.That(part.Sheets.Count, Is.EqualTo(1));
        Assert.That(part.WorkbookViews.Count, Is.EqualTo(1));
        Assert.That(part.DefinedNames.Count, Is.EqualTo(1));
    }

    static bool HasDiagnostic(XlsxWorkbookPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
