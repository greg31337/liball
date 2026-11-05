using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetProtectionTests
{
    [Test]
    public void ReadString_ParsesHashAndAllowedActionFlags()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\">" +
            "<sheetData />" +
            "<sheetProtection sheet=\"1\" objects=\"0\" scenarios=\"1\" formatCells=\"0\" formatColumns=\"1\" formatRows=\"0\" insertColumns=\"1\" insertRows=\"0\" insertHyperlinks=\"1\" deleteColumns=\"0\" deleteRows=\"1\" selectLockedCells=\"1\" sort=\"0\" autoFilter=\"1\" pivotTables=\"0\" selectUnlockedCells=\"1\" password=\"ABCD\" algorithmName=\"SHA-512\" hashValue=\"hash==\" saltValue=\"salt==\" spinCount=\"100000\" x:protection=\"p\" />" +
            "</worksheet>";

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);
        XlsxWorksheetProtection protection = part.SheetProtection!;

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasSheetProtection, Is.True);
        Assert.That(protection!.HasSheet, Is.True);
        Assert.That(protection.Sheet, Is.True);
        Assert.That(protection.HasObjects, Is.True);
        Assert.That(protection.Objects, Is.False);
        Assert.That(protection.HasScenarios, Is.True);
        Assert.That(protection.Scenarios, Is.True);
        Assert.That(protection.HasFormatCells, Is.True);
        Assert.That(protection.FormatCells, Is.False);
        Assert.That(protection.HasFormatColumns, Is.True);
        Assert.That(protection.FormatColumns, Is.True);
        Assert.That(protection.HasFormatRows, Is.True);
        Assert.That(protection.FormatRows, Is.False);
        Assert.That(protection.HasInsertColumns, Is.True);
        Assert.That(protection.InsertColumns, Is.True);
        Assert.That(protection.HasInsertRows, Is.True);
        Assert.That(protection.InsertRows, Is.False);
        Assert.That(protection.HasInsertHyperlinks, Is.True);
        Assert.That(protection.InsertHyperlinks, Is.True);
        Assert.That(protection.HasDeleteColumns, Is.True);
        Assert.That(protection.DeleteColumns, Is.False);
        Assert.That(protection.HasDeleteRows, Is.True);
        Assert.That(protection.DeleteRows, Is.True);
        Assert.That(protection.HasSelectLockedCells, Is.True);
        Assert.That(protection.SelectLockedCells, Is.True);
        Assert.That(protection.HasSort, Is.True);
        Assert.That(protection.Sort, Is.False);
        Assert.That(protection.HasAutoFilter, Is.True);
        Assert.That(protection.AutoFilter, Is.True);
        Assert.That(protection.HasPivotTables, Is.True);
        Assert.That(protection.PivotTables, Is.False);
        Assert.That(protection.HasSelectUnlockedCells, Is.True);
        Assert.That(protection.SelectUnlockedCells, Is.True);
        Assert.That(protection.Password, Is.EqualTo("ABCD"));
        Assert.That(protection.AlgorithmName, Is.EqualTo("SHA-512"));
        Assert.That(protection.HashValue, Is.EqualTo("hash=="));
        Assert.That(protection.SaltValue, Is.EqualTo("salt=="));
        Assert.That(protection.HasSpinCount, Is.True);
        Assert.That(protection.SpinCount, Is.EqualTo(100000));
        Assert.That(protection.UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesHashAndAllowedActionFlagsAndRoundTrips()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.HasSheetProtection = true;
        ConfigureProtection(part.SheetProtection!);

        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(result.Xml);
        XlsxWorksheetProtection protection = roundTrip.SheetProtection!;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("sheet=\"1\""));
        Assert.That(result.Xml, Does.Contain("formatCells=\"0\""));
        Assert.That(result.Xml, Does.Contain("formatColumns=\"1\""));
        Assert.That(result.Xml, Does.Contain("insertHyperlinks=\"1\""));
        Assert.That(result.Xml, Does.Contain("deleteRows=\"1\""));
        Assert.That(result.Xml, Does.Contain("selectLockedCells=\"1\""));
        Assert.That(result.Xml, Does.Contain("sort=\"0\""));
        Assert.That(result.Xml, Does.Contain("autoFilter=\"1\""));
        Assert.That(result.Xml, Does.Contain("pivotTables=\"0\""));
        Assert.That(result.Xml, Does.Contain("selectUnlockedCells=\"1\""));
        Assert.That(result.Xml, Does.Contain("algorithmName=\"SHA-512\""));
        Assert.That(result.Xml, Does.Contain("hashValue=\"hash==\""));
        Assert.That(result.Xml, Does.Contain("saltValue=\"salt==\""));
        Assert.That(result.Xml, Does.Contain("spinCount=\"100000\""));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(protection!.FormatCells, Is.False);
        Assert.That(protection.FormatColumns, Is.True);
        Assert.That(protection.FormatRows, Is.False);
        Assert.That(protection.InsertColumns, Is.True);
        Assert.That(protection.InsertRows, Is.False);
        Assert.That(protection.InsertHyperlinks, Is.True);
        Assert.That(protection.DeleteColumns, Is.False);
        Assert.That(protection.DeleteRows, Is.True);
        Assert.That(protection.SelectLockedCells, Is.True);
        Assert.That(protection.Sort, Is.False);
        Assert.That(protection.AutoFilter, Is.True);
        Assert.That(protection.PivotTables, Is.False);
        Assert.That(protection.SelectUnlockedCells, Is.True);
        Assert.That(protection.AlgorithmName, Is.EqualTo("SHA-512"));
        Assert.That(protection.HashValue, Is.EqualTo("hash=="));
        Assert.That(protection.SaltValue, Is.EqualTo("salt=="));
        Assert.That(protection.SpinCount, Is.EqualTo(100000));
    }

    [Test]
    public void WriteString_PreservesLoadedSheetProtectionUnknownAttributes()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\">" +
            "<sheetData />" +
            "<sheetProtection sheet=\"1\" autoFilter=\"1\" x:futureFlag=\"f\" />" +
            "</worksheet>";
        XlsxWorksheetPart loaded = new XlsxWorksheetPartReader().ReadString(xml);

        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriter().WriteString(loaded);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(result.Xml);

        Assert.That(loaded.IsValid, Is.True);
        Assert.That(loaded.SheetProtection!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("autoFilter=\"1\""));
        Assert.That(result.Xml, Does.Contain("futureFlag"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.SheetProtection!.AutoFilter, Is.True);
        Assert.That(roundTrip.SheetProtection.UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteBytes_RejectsInvalidSheetProtectionDefinitions()
    {
        XlsxWorksheetPart nullProtection = new XlsxWorksheetPart();
        XlsxWorksheetPart zeroSpinCount = new XlsxWorksheetPart();
        XlsxWorksheetPart incompleteHash = new XlsxWorksheetPart();
        XlsxWorksheetPart nullUnknownAttributes = new XlsxWorksheetPart();

        nullProtection.HasSheetProtection = true;
        nullProtection.SheetProtection = null;
        zeroSpinCount.HasSheetProtection = true;
        zeroSpinCount.SheetProtection!.HasSpinCount = true;
        zeroSpinCount.SheetProtection.SpinCount = 0;
        incompleteHash.HasSheetProtection = true;
        incompleteHash.SheetProtection!.AlgorithmName = "SHA-512";
        incompleteHash.SheetProtection.HashValue = "hash==";
        nullUnknownAttributes.HasSheetProtection = true;
        nullUnknownAttributes.SheetProtection!.UnknownAttributes = null!;

        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();
        XlsxWorksheetPartWriteResult nullProtectionResult = writer.WriteBytes(nullProtection);
        XlsxWorksheetPartWriteResult zeroSpinCountResult = writer.WriteBytes(zeroSpinCount);
        XlsxWorksheetPartWriteResult incompleteHashResult = writer.WriteBytes(incompleteHash);
        XlsxWorksheetPartWriteResult nullUnknownAttributesResult = writer.WriteBytes(nullUnknownAttributes);

        Assert.That(nullProtectionResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullProtectionResult, "worksheet.sheetProtectionNull"), Is.True);
        Assert.That(zeroSpinCountResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(zeroSpinCountResult, "worksheet.sheetProtectionSpinCountRange"), Is.True);
        Assert.That(incompleteHashResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(incompleteHashResult, "worksheet.sheetProtectionHashIncomplete"), Is.True);
        Assert.That(nullUnknownAttributesResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullUnknownAttributesResult, "worksheet.sheetProtectionUnknownAttributesNull"), Is.True);
    }

    static void ConfigureProtection(XlsxWorksheetProtection protection)
    {
        protection.Sheet = true;
        protection.HasSheet = true;
        protection.Objects = false;
        protection.HasObjects = true;
        protection.Scenarios = true;
        protection.HasScenarios = true;
        protection.FormatCells = false;
        protection.HasFormatCells = true;
        protection.FormatColumns = true;
        protection.HasFormatColumns = true;
        protection.FormatRows = false;
        protection.HasFormatRows = true;
        protection.InsertColumns = true;
        protection.HasInsertColumns = true;
        protection.InsertRows = false;
        protection.HasInsertRows = true;
        protection.InsertHyperlinks = true;
        protection.HasInsertHyperlinks = true;
        protection.DeleteColumns = false;
        protection.HasDeleteColumns = true;
        protection.DeleteRows = true;
        protection.HasDeleteRows = true;
        protection.SelectLockedCells = true;
        protection.HasSelectLockedCells = true;
        protection.Sort = false;
        protection.HasSort = true;
        protection.AutoFilter = true;
        protection.HasAutoFilter = true;
        protection.PivotTables = false;
        protection.HasPivotTables = true;
        protection.SelectUnlockedCells = true;
        protection.HasSelectUnlockedCells = true;
        protection.Password = "ABCD";
        protection.AlgorithmName = "SHA-512";
        protection.HashValue = "hash==";
        protection.SaltValue = "salt==";
        protection.SpinCount = 100000;
        protection.HasSpinCount = true;
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
}
