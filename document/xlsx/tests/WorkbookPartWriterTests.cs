using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookPartWriterTests
{
    [Test]
    public void WriteString_WritesParseableMinimumWorkbook()
    {
        XlsxWorkbookPart part = new XlsxWorkbookPart();
        part.Sheets.Add(CreateSheet("Sheet1", 1, "rId1", ""));
        part.Sheets.Add(CreateSheet("Hidden", 2, "rId2", "hidden"));
        XlsxWorkbookPartWriter writer = new XlsxWorkbookPartWriter();

        XlsxWorkbookPartWriteResult result = writer.WriteString(part);
        XlsxWorkbookPart readPart = new XlsxWorkbookPartReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<workbook"));
        Assert.That(result.Xml, Does.Contain("xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\""));
        Assert.That(result.Xml, Does.Contain("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\""));
        Assert.That(result.Xml, Does.Contain("<sheets>"));
        Assert.That(result.Xml, Does.Contain("name=\"Sheet1\""));
        Assert.That(result.Xml, Does.Contain("r:id=\"rId1\""));
        Assert.That(result.Xml, Does.Contain("state=\"hidden\""));
        Assert.That(readPart.IsValid, Is.True);
        Assert.That(readPart.Sheets.Count, Is.EqualTo(2));
        Assert.That(readPart.Sheets[0].Name, Is.EqualTo("Sheet1"));
        Assert.That(readPart.Sheets[1].State, Is.EqualTo("hidden"));
    }

    [Test]
    public void WriteBytes_ProducesIdenticalBytesForEquivalentMinimumWorkbooks()
    {
        XlsxWorkbookPart first = new XlsxWorkbookPart();
        first.Sheets.Add(CreateSheet("Sheet1", 1, "rId1", ""));
        XlsxWorkbookPart second = new XlsxWorkbookPart();
        second.Sheets.Add(CreateSheet("Sheet1", 1, "rId1", ""));
        XlsxWorkbookPartWriter writer = new XlsxWorkbookPartWriter();

        XlsxWorkbookPartWriteResult firstResult = writer.WriteBytes(first);
        XlsxWorkbookPartWriteResult secondResult = writer.WriteBytes(second);

        Assert.That(firstResult.IsValid, Is.True);
        Assert.That(secondResult.IsValid, Is.True);
        Assert.That(firstResult.Bytes, Is.EqualTo(secondResult.Bytes));
    }

    [Test]
    public void WriteString_RoundTripsLoadedWorkbookMetadataAndUnknownXml()
    {
        string xml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:x=\"urn:x\" xmlns:x15=\"urn:x15\" mc:Ignorable=\"x15\" x:root=\"r\">" +
            "<bookViews><workbookView visibility=\"visible\" activeTab=\"0\" firstSheet=\"0\" tabRatio=\"600\" xWindow=\"10\" yWindow=\"20\" windowWidth=\"12000\" windowHeight=\"8000\" x:view=\"v\" /></bookViews>" +
            "<workbookProtection workbookPassword=\"ABCD\" revisionsPassword=\"1234\" lockStructure=\"1\" lockWindows=\"0\" lockRevision=\"0\" x:protect=\"p\" />" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /><sheet name=\"VeryHidden\" sheetId=\"2\" state=\"veryHidden\" r:id=\"rId2\" x:sheet=\"s\" /></sheets>" +
            "<definedNames><definedName name=\"LocalName\" localSheetId=\"0\" comment=\"local\" hidden=\"1\" function=\"0\" x:name=\"n\">Sheet1!$A$1</definedName></definedNames>" +
            "<calcPr calcId=\"191029\" calcMode=\"auto\" refMode=\"A1\" fullCalcOnLoad=\"1\" forceFullCalc=\"0\" iterate=\"1\" iterateCount=\"100\" iterateDelta=\"0.001\" x:calc=\"c\" />" +
            "<extLst><ext uri=\"{33333333-3333-3333-3333-333333333333}\"><x15:future /></ext></extLst>" +
            "<x:future><x:child /></x:future>" +
            "</workbook>";
        XlsxWorkbookPart loaded = new XlsxWorkbookPartReader().ReadString(xml);

        XlsxWorkbookPartWriteResult result = new XlsxWorkbookPartWriter().WriteString(loaded);
        XlsxWorkbookPart roundTrip = new XlsxWorkbookPartReader().ReadString(result.Xml);

        Assert.That(loaded.IsValid, Is.True);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("mc:Ignorable=\"x15\""));
        Assert.That(result.Xml, Does.Contain("x:root=\"r\""));
        Assert.That(result.Xml, Does.Contain("activeTab=\"0\""));
        Assert.That(result.Xml, Does.Contain("workbookProtection"));
        Assert.That(result.Xml, Does.Contain("definedName"));
        Assert.That(result.Xml, Does.Contain("calcPr"));
        Assert.That(result.Xml, Does.Contain("x15:future"));
        Assert.That(result.Xml, Does.Contain("x:future"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.MarkupCompatibility.Ignorable, Is.EqualTo("x15"));
        Assert.That(roundTrip.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.WorkbookViews.Count, Is.EqualTo(1));
        Assert.That(roundTrip.WorkbookViews[0].HasActiveTab, Is.True);
        Assert.That(roundTrip.WorkbookViews[0].ActiveTab, Is.EqualTo(0));
        Assert.That(roundTrip.WorkbookViews[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.HasWorkbookProtection, Is.True);
        Assert.That(roundTrip.WorkbookProtection!.LockStructure, Is.True);
        Assert.That(roundTrip.WorkbookProtection.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Sheets.Count, Is.EqualTo(2));
        Assert.That(roundTrip.Sheets[1].State, Is.EqualTo("veryHidden"));
        Assert.That(roundTrip.Sheets[1].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.DefinedNames.Count, Is.EqualTo(1));
        Assert.That(roundTrip.DefinedNames[0].Text, Is.EqualTo("Sheet1!$A$1"));
        Assert.That(roundTrip.DefinedNames[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.HasCalculationProperties, Is.True);
        Assert.That(roundTrip.CalculationProperties.HasIterateDelta, Is.True);
        Assert.That(roundTrip.CalculationProperties.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.HasExtensionList, Is.True);
        Assert.That(roundTrip.ExtensionList.Extensions[0].RawXml, Does.Contain("x15:future"));
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteStream_WritesToProvidedStreamWithoutClosingIt()
    {
        XlsxWorkbookPart part = new XlsxWorkbookPart();
        part.Sheets.Add(CreateSheet("Sheet1", 1, "rId1", ""));
        MemoryStream stream = new MemoryStream();

        XlsxWorkbookPartWriteResult result = new XlsxWorkbookPartWriter().WriteStream(stream, part);

        long lengthAfterWrite = stream.Length;
        stream.WriteByte(32);

        Assert.That(result.IsValid, Is.True);
        Assert.That(lengthAfterWrite, Is.GreaterThan(0));
        Assert.That(stream.Length, Is.EqualTo(lengthAfterWrite + 1));
        stream.Dispose();
    }

    [Test]
    public void WriteBytes_RejectsInvalidMinimumWorkbookBeforeWriting()
    {
        XlsxWorkbookPart part = new XlsxWorkbookPart();
        part.Sheets.Add(CreateSheet("", 0, "", "bad"));
        part.Sheets.Add(CreateSheet("Bad/Name", 1, "1bad", ""));
        part.Sheets.Add(CreateSheet("Dup", 1, "rId1", ""));
        part.Sheets.Add(CreateSheet("dup", 2, "rId1", ""));

        XlsxWorkbookPartWriteResult result = new XlsxWorkbookPartWriter().WriteBytes(part);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(result, "workbook.sheet.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.sheetIdMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.relationshipIdMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.stateInvalid"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.nameInvalid"), Is.True);
        Assert.That(HasDiagnostic(result, "xml.relationshipId.invalid"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.sheetIdDuplicate"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.relationshipIdDuplicate"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.nameDuplicate"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsNullPartAndWorkbookWithoutSheets()
    {
        XlsxWorkbookPartWriteResult? nullResult = new XlsxWorkbookPartWriter().WriteBytes(null!);
        XlsxWorkbookPart emptyPart = new XlsxWorkbookPart();
        XlsxWorkbookPartWriteResult emptyResult = new XlsxWorkbookPartWriter().WriteBytes(emptyPart);

        Assert.That(nullResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullResult, "workbook.partNull"), Is.True);
        Assert.That(emptyResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(emptyResult, "workbook.sheetsMissing"), Is.True);
    }

    static XlsxWorkbookSheet CreateSheet(string name, uint sheetId, string relationshipId, string state)
    {
        XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
        sheet.Name = name;
        sheet.SheetId = sheetId;
        sheet.RelationshipId = relationshipId;
        sheet.State = state;
        return sheet;
    }

    static bool HasDiagnostic(XlsxWorkbookPartWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
