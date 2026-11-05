using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetSparklineTests
{
    [Test]
    public void WorksheetReaderWriter_ReadsAndPreservesSparklineExtensionGroups()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(WorksheetWithSparklinesXml());
        part.ExtensionList.RawXml = "";
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasExtensionList, Is.True);
        Assert.That(part.HasSparklineGroups, Is.True);
        Assert.That(part.SparklineGroups.Count, Is.EqualTo(1));
        Assert.That(part.SparklineGroups[0].Type, Is.EqualTo("line"));
        Assert.That(part.SparklineGroups[0].Sparklines.Count, Is.EqualTo(1));
        Assert.That(part.SparklineGroups[0].Sparklines[0].Formula, Is.EqualTo("Sheet1!A1:C1"));
        Assert.That(part.SparklineGroups[0].Sparklines[0].CellReference, Is.EqualTo("D1"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("sparklineGroups"));
        Assert.That(write.Xml, Does.Contain("Sheet1!A1:C1"));
        Assert.That(write.Xml, Does.Contain("D1</xm:sqref>"));
        Assert.That(readBack.HasSparklineGroups, Is.True);
        Assert.That(readBack.SparklineGroups[0].Sparklines[0].CellReference, Is.EqualTo("D1"));
    }

    [Test]
    public void WorksheetSparklineApi_AddsSparklineAndWritesExtensionList()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxSparklineOperationResult add = part.AddSparkline("E2", "Sheet1!B2:D2", "column");
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(add.IsValid, Is.True);
        Assert.That(add.HasSparkline, Is.True);
        Assert.That(part.HasSparklineGroups, Is.True);
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<extLst"));
        Assert.That(write.Xml, Does.Contain("type=\"column\""));
        Assert.That(write.Xml, Does.Contain("<xm:f>Sheet1!B2:D2</xm:f>"));
        Assert.That(write.Xml, Does.Contain("<xm:sqref>E2</xm:sqref>"));
        Assert.That(readBack.HasSparklineGroups, Is.True);
        Assert.That(readBack.SparklineGroups.Count, Is.EqualTo(1));
        Assert.That(readBack.SparklineGroups[0].Type, Is.EqualTo("column"));
        Assert.That(readBack.SparklineGroups[0].Sparklines[0].Formula, Is.EqualTo("Sheet1!B2:D2"));
    }

    [Test]
    public void WorksheetSparklineWriterAndApi_ReportInvalidStates()
    {
        XlsxWorksheetPart nullGroups = new XlsxWorksheetPart();
        nullGroups.HasSparklineGroups = true;
        nullGroups.SparklineGroups = null!;
        XlsxWorksheetPart badGroup = new XlsxWorksheetPart();
        badGroup.HasSparklineGroups = true;
        XlsxSparklineGroup group = new XlsxSparklineGroup();
        group.Type = "bad";
        group.Sparklines.Add(new XlsxSparkline());
        badGroup.SparklineGroups.Add(group);
        XlsxSparklineOperationResult invalidCell = new XlsxWorksheetPart().AddSparkline("XFE1", "Sheet1!A1:C1", "line");
        XlsxSparklineOperationResult invalidType = new XlsxWorksheetPart().AddSparkline("A1", "Sheet1!A1:C1", "bad");

        XlsxWorksheetPartWriteResult nullGroupsWrite = new XlsxWorksheetPartWriter().WriteString(nullGroups);
        XlsxWorksheetPartWriteResult badGroupWrite = new XlsxWorksheetPartWriter().WriteString(badGroup);

        Assert.That(nullGroupsWrite.IsValid, Is.False);
        Assert.That(badGroupWrite.IsValid, Is.False);
        Assert.That(invalidCell.IsValid, Is.False);
        Assert.That(invalidType.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullGroupsWrite, "worksheet.sparklineGroupsNull"), Is.True);
        Assert.That(HasDiagnostic(badGroupWrite, "worksheet.sparklineTypeUnsupported"), Is.True);
        Assert.That(HasDiagnostic(badGroupWrite, "worksheet.sparklineFormulaMissing"), Is.True);
        Assert.That(HasDiagnostic(badGroupWrite, "worksheet.sparklineCellMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidCell, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(invalidType, "sparkline.typeUnsupported"), Is.True);
    }

    static string WorksheetWithSparklinesXml()
    {
        return "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x14=\"http://schemas.microsoft.com/office/spreadsheetml/2009/9/main\" xmlns:xm=\"http://schemas.microsoft.com/office/excel/2006/main\">" +
            "<sheetData />" +
            "<extLst><ext uri=\"{05C60535-1F16-4fd2-B633-F4F36F0B64E0}\"><x14:sparklineGroups xmlns:xm=\"http://schemas.microsoft.com/office/excel/2006/main\"><x14:sparklineGroup type=\"line\" displayEmptyCellsAs=\"gap\"><x14:sparklines><x14:sparkline><xm:f>Sheet1!A1:C1</xm:f><xm:sqref>D1</xm:sqref></x14:sparkline></x14:sparklines></x14:sparklineGroup></x14:sparklineGroups></ext></extLst>" +
            "</worksheet>";
    }

    static bool HasDiagnostic(XlsxWorksheetPartWriteResult result, string code)
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

    static bool HasDiagnostic(XlsxSparklineOperationResult result, string code)
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
