using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookProtectionTests
{
    [Test]
    public void ReadString_ParsesPasswordsHashesLocksAndUnknownAttributes()
    {
        string xml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\">" +
            "<workbookProtection workbookPassword=\"ABCD\" workbookAlgorithmName=\"SHA-512\" workbookHashValue=\"workbookHash==\" workbookSaltValue=\"workbookSalt==\" workbookSpinCount=\"100000\" revisionsPassword=\"1234\" revisionsAlgorithmName=\"SHA-384\" revisionsHashValue=\"revisionsHash==\" revisionsSaltValue=\"revisionsSalt==\" revisionsSpinCount=\"200000\" lockStructure=\"1\" lockWindows=\"0\" lockRevision=\"1\" x:protect=\"p\" />" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "</workbook>";

        XlsxWorkbookPart part = new XlsxWorkbookPartReader().ReadString(xml);
        XlsxWorkbookProtection protection = part.WorkbookProtection!;

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasWorkbookProtection, Is.True);
        Assert.That(protection!.WorkbookPassword, Is.EqualTo("ABCD"));
        Assert.That(protection.WorkbookAlgorithmName, Is.EqualTo("SHA-512"));
        Assert.That(protection.WorkbookHashValue, Is.EqualTo("workbookHash=="));
        Assert.That(protection.WorkbookSaltValue, Is.EqualTo("workbookSalt=="));
        Assert.That(protection.HasWorkbookSpinCount, Is.True);
        Assert.That(protection.WorkbookSpinCount, Is.EqualTo(100000));
        Assert.That(protection.RevisionsPassword, Is.EqualTo("1234"));
        Assert.That(protection.RevisionsAlgorithmName, Is.EqualTo("SHA-384"));
        Assert.That(protection.RevisionsHashValue, Is.EqualTo("revisionsHash=="));
        Assert.That(protection.RevisionsSaltValue, Is.EqualTo("revisionsSalt=="));
        Assert.That(protection.HasRevisionsSpinCount, Is.True);
        Assert.That(protection.RevisionsSpinCount, Is.EqualTo(200000));
        Assert.That(protection.HasLockStructure, Is.True);
        Assert.That(protection.LockStructure, Is.True);
        Assert.That(protection.HasLockWindows, Is.True);
        Assert.That(protection.LockWindows, Is.False);
        Assert.That(protection.HasLockRevision, Is.True);
        Assert.That(protection.LockRevision, Is.True);
        Assert.That(protection.UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesProtectionHashesAndRoundTrips()
    {
        XlsxWorkbookPart part = CreateMinimumWorkbook();
        part.HasWorkbookProtection = true;
        ConfigureProtection(part.WorkbookProtection!);

        XlsxWorkbookPartWriteResult result = new XlsxWorkbookPartWriter().WriteString(part);
        XlsxWorkbookPart roundTrip = new XlsxWorkbookPartReader().ReadString(result.Xml);
        XlsxWorkbookProtection protection = roundTrip.WorkbookProtection!;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("workbookPassword=\"ABCD\""));
        Assert.That(result.Xml, Does.Contain("workbookAlgorithmName=\"SHA-512\""));
        Assert.That(result.Xml, Does.Contain("workbookHashValue=\"workbookHash==\""));
        Assert.That(result.Xml, Does.Contain("workbookSaltValue=\"workbookSalt==\""));
        Assert.That(result.Xml, Does.Contain("workbookSpinCount=\"100000\""));
        Assert.That(result.Xml, Does.Contain("revisionsPassword=\"1234\""));
        Assert.That(result.Xml, Does.Contain("revisionsAlgorithmName=\"SHA-384\""));
        Assert.That(result.Xml, Does.Contain("revisionsHashValue=\"revisionsHash==\""));
        Assert.That(result.Xml, Does.Contain("revisionsSaltValue=\"revisionsSalt==\""));
        Assert.That(result.Xml, Does.Contain("revisionsSpinCount=\"200000\""));
        Assert.That(result.Xml, Does.Contain("lockStructure=\"1\""));
        Assert.That(result.Xml, Does.Contain("lockWindows=\"0\""));
        Assert.That(result.Xml, Does.Contain("lockRevision=\"1\""));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(protection!.WorkbookAlgorithmName, Is.EqualTo("SHA-512"));
        Assert.That(protection.WorkbookHashValue, Is.EqualTo("workbookHash=="));
        Assert.That(protection.WorkbookSaltValue, Is.EqualTo("workbookSalt=="));
        Assert.That(protection.WorkbookSpinCount, Is.EqualTo(100000));
        Assert.That(protection.RevisionsAlgorithmName, Is.EqualTo("SHA-384"));
        Assert.That(protection.RevisionsHashValue, Is.EqualTo("revisionsHash=="));
        Assert.That(protection.RevisionsSaltValue, Is.EqualTo("revisionsSalt=="));
        Assert.That(protection.RevisionsSpinCount, Is.EqualTo(200000));
        Assert.That(protection.LockStructure, Is.True);
        Assert.That(protection.LockWindows, Is.False);
        Assert.That(protection.LockRevision, Is.True);
    }

    [Test]
    public void WriteString_PreservesLoadedProtectionUnknownAttributes()
    {
        string xml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\">" +
            "<workbookProtection lockStructure=\"1\" workbookAlgorithmName=\"SHA-512\" workbookHashValue=\"hash==\" workbookSaltValue=\"salt==\" workbookSpinCount=\"100000\" x:futureProtection=\"f\" />" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "</workbook>";
        XlsxWorkbookPart loaded = new XlsxWorkbookPartReader().ReadString(xml);

        XlsxWorkbookPartWriteResult result = new XlsxWorkbookPartWriter().WriteString(loaded);
        XlsxWorkbookPart roundTrip = new XlsxWorkbookPartReader().ReadString(result.Xml);

        Assert.That(loaded.IsValid, Is.True);
        Assert.That(loaded.WorkbookProtection!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("workbookAlgorithmName=\"SHA-512\""));
        Assert.That(result.Xml, Does.Contain("futureProtection"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.WorkbookProtection!.LockStructure, Is.True);
        Assert.That(roundTrip.WorkbookProtection.UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteBytes_RejectsInvalidProtectionDefinitions()
    {
        XlsxWorkbookPart nullProtection = CreateMinimumWorkbook();
        XlsxWorkbookPart zeroWorkbookSpinCount = CreateMinimumWorkbook();
        XlsxWorkbookPart zeroRevisionsSpinCount = CreateMinimumWorkbook();
        XlsxWorkbookPart incompleteWorkbookHash = CreateMinimumWorkbook();
        XlsxWorkbookPart incompleteRevisionsHash = CreateMinimumWorkbook();
        XlsxWorkbookPart nullUnknownAttributes = CreateMinimumWorkbook();

        nullProtection.HasWorkbookProtection = true;
        nullProtection.WorkbookProtection = null;
        zeroWorkbookSpinCount.HasWorkbookProtection = true;
        zeroWorkbookSpinCount.WorkbookProtection!.HasWorkbookSpinCount = true;
        zeroWorkbookSpinCount.WorkbookProtection.WorkbookSpinCount = 0;
        zeroRevisionsSpinCount.HasWorkbookProtection = true;
        zeroRevisionsSpinCount.WorkbookProtection!.HasRevisionsSpinCount = true;
        zeroRevisionsSpinCount.WorkbookProtection.RevisionsSpinCount = 0;
        incompleteWorkbookHash.HasWorkbookProtection = true;
        incompleteWorkbookHash.WorkbookProtection!.WorkbookAlgorithmName = "SHA-512";
        incompleteWorkbookHash.WorkbookProtection.WorkbookHashValue = "hash==";
        incompleteRevisionsHash.HasWorkbookProtection = true;
        incompleteRevisionsHash.WorkbookProtection!.RevisionsSaltValue = "salt==";
        incompleteRevisionsHash.WorkbookProtection.RevisionsSpinCount = 100000;
        incompleteRevisionsHash.WorkbookProtection.HasRevisionsSpinCount = true;
        nullUnknownAttributes.HasWorkbookProtection = true;
        nullUnknownAttributes.WorkbookProtection!.UnknownAttributes = null!;

        XlsxWorkbookPartWriter writer = new XlsxWorkbookPartWriter();
        XlsxWorkbookPartWriteResult nullProtectionResult = writer.WriteBytes(nullProtection);
        XlsxWorkbookPartWriteResult zeroWorkbookSpinCountResult = writer.WriteBytes(zeroWorkbookSpinCount);
        XlsxWorkbookPartWriteResult zeroRevisionsSpinCountResult = writer.WriteBytes(zeroRevisionsSpinCount);
        XlsxWorkbookPartWriteResult incompleteWorkbookHashResult = writer.WriteBytes(incompleteWorkbookHash);
        XlsxWorkbookPartWriteResult incompleteRevisionsHashResult = writer.WriteBytes(incompleteRevisionsHash);
        XlsxWorkbookPartWriteResult nullUnknownAttributesResult = writer.WriteBytes(nullUnknownAttributes);

        Assert.That(nullProtectionResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullProtectionResult, "workbook.protectionNull"), Is.True);
        Assert.That(zeroWorkbookSpinCountResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(zeroWorkbookSpinCountResult, "workbook.protectionWorkbookSpinCountRange"), Is.True);
        Assert.That(zeroRevisionsSpinCountResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(zeroRevisionsSpinCountResult, "workbook.protectionRevisionsSpinCountRange"), Is.True);
        Assert.That(incompleteWorkbookHashResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(incompleteWorkbookHashResult, "workbook.protectionWorkbookHashIncomplete"), Is.True);
        Assert.That(incompleteRevisionsHashResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(incompleteRevisionsHashResult, "workbook.protectionRevisionsHashIncomplete"), Is.True);
        Assert.That(nullUnknownAttributesResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullUnknownAttributesResult, "workbook.protectionUnknownAttributesNull"), Is.True);
    }

    static void ConfigureProtection(XlsxWorkbookProtection protection)
    {
        protection.WorkbookPassword = "ABCD";
        protection.WorkbookAlgorithmName = "SHA-512";
        protection.WorkbookHashValue = "workbookHash==";
        protection.WorkbookSaltValue = "workbookSalt==";
        protection.WorkbookSpinCount = 100000;
        protection.HasWorkbookSpinCount = true;
        protection.RevisionsPassword = "1234";
        protection.RevisionsAlgorithmName = "SHA-384";
        protection.RevisionsHashValue = "revisionsHash==";
        protection.RevisionsSaltValue = "revisionsSalt==";
        protection.RevisionsSpinCount = 200000;
        protection.HasRevisionsSpinCount = true;
        protection.LockStructure = true;
        protection.HasLockStructure = true;
        protection.LockWindows = false;
        protection.HasLockWindows = true;
        protection.LockRevision = true;
        protection.HasLockRevision = true;
    }

    static XlsxWorkbookPart CreateMinimumWorkbook()
    {
        XlsxWorkbookPart part = new XlsxWorkbookPart();
        XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
        sheet.Name = "Sheet1";
        sheet.SheetId = 1;
        sheet.RelationshipId = "rId1";
        part.Sheets.Add(sheet);
        return part;
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
