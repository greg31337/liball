using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookDefinedNameTests
{
    [Test]
    public void ReadString_ParsesScopedNamesAndMetadataAttributes()
    {
        string xml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /><sheet name=\"Sheet2\" sheetId=\"2\" r:id=\"rId2\" /></sheets>" +
            "<definedNames>" +
            "<definedName name=\"ScopedName\">Sheet1!$A$1</definedName>" +
            "<definedName name=\"ScopedName\" localSheetId=\"1\" comment=\"local\" customMenu=\"Menu\" description=\"Description\" help=\"Help\" statusBar=\"Status\" shortcutKey=\"K\" hidden=\"1\" function=\"1\" vbProcedure=\"0\" xlm=\"1\" functionGroupId=\"7\" publishToServer=\"1\" workbookParameter=\"0\" x:name=\"n\">Sheet2!$B$2</definedName>" +
            "</definedNames>" +
            "</workbook>";

        XlsxWorkbookPart part = new XlsxWorkbookPartReader().ReadString(xml);
        XlsxWorkbookDefinedName? global = part.GetGlobalDefinedName("ScopedName");
        XlsxWorkbookDefinedName? local = part.GetLocalDefinedName("ScopedName", 1);
        XlsxWorkbookDefinedName? resolved = part.GetDefinedNameForSheet("ScopedName", 1);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.DefinedNames.Count, Is.EqualTo(2));
        Assert.That(global!.Text, Is.EqualTo("Sheet1!$A$1"));
        Assert.That(local!.Text, Is.EqualTo("Sheet2!$B$2"));
        Assert.That(resolved!.Text, Is.EqualTo("Sheet2!$B$2"));
        Assert.That(local.HasLocalSheetId, Is.True);
        Assert.That(local.LocalSheetId, Is.EqualTo(1));
        Assert.That(local.Comment, Is.EqualTo("local"));
        Assert.That(local.CustomMenu, Is.EqualTo("Menu"));
        Assert.That(local.Description, Is.EqualTo("Description"));
        Assert.That(local.Help, Is.EqualTo("Help"));
        Assert.That(local.StatusBar, Is.EqualTo("Status"));
        Assert.That(local.ShortcutKey, Is.EqualTo("K"));
        Assert.That(local.Hidden, Is.True);
        Assert.That(local.Function, Is.True);
        Assert.That(local.VbProcedure, Is.False);
        Assert.That(local.Xlm, Is.True);
        Assert.That(local.HasFunctionGroupId, Is.True);
        Assert.That(local.FunctionGroupId, Is.EqualTo(7));
        Assert.That(local.PublishToServer, Is.True);
        Assert.That(local.WorkbookParameter, Is.False);
        Assert.That(local.UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesDefinedNameMetadataAndRoundTrips()
    {
        XlsxWorkbookPart part = CreateMinimumWorkbook();
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = "LocalMacro";
        definedName.Text = "Sheet1!$C$3";
        definedName.LocalSheetId = 0;
        definedName.HasLocalSheetId = true;
        ConfigureMetadata(definedName);
        part.DefinedNames.Add(definedName);

        XlsxWorkbookPartWriteResult result = new XlsxWorkbookPartWriter().WriteString(part);
        XlsxWorkbookPart roundTrip = new XlsxWorkbookPartReader().ReadString(result.Xml);
        XlsxWorkbookDefinedName? local = roundTrip.GetLocalDefinedName("LocalMacro", 0);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<definedNames>"));
        Assert.That(result.Xml, Does.Contain("name=\"LocalMacro\""));
        Assert.That(result.Xml, Does.Contain("localSheetId=\"0\""));
        Assert.That(result.Xml, Does.Contain("customMenu=\"Menu\""));
        Assert.That(result.Xml, Does.Contain("description=\"Description\""));
        Assert.That(result.Xml, Does.Contain("help=\"Help\""));
        Assert.That(result.Xml, Does.Contain("statusBar=\"Status\""));
        Assert.That(result.Xml, Does.Contain("shortcutKey=\"K\""));
        Assert.That(result.Xml, Does.Contain("hidden=\"1\""));
        Assert.That(result.Xml, Does.Contain("function=\"1\""));
        Assert.That(result.Xml, Does.Contain("vbProcedure=\"0\""));
        Assert.That(result.Xml, Does.Contain("xlm=\"1\""));
        Assert.That(result.Xml, Does.Contain("functionGroupId=\"7\""));
        Assert.That(result.Xml, Does.Contain("publishToServer=\"1\""));
        Assert.That(result.Xml, Does.Contain("workbookParameter=\"0\""));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(local!.Text, Is.EqualTo("Sheet1!$C$3"));
        Assert.That(local.Description, Is.EqualTo("Description"));
        Assert.That(local.FunctionGroupId, Is.EqualTo(7));
        Assert.That(local.PublishToServer, Is.True);
        Assert.That(local.WorkbookParameter, Is.False);
    }

    [Test]
    public void DefinedNameLookup_ResolvesLocalNamesBeforeGlobalNames()
    {
        XlsxWorkbookPart part = CreateMinimumWorkbook();
        XlsxWorkbookDefinedName global = new XlsxWorkbookDefinedName();
        XlsxWorkbookDefinedName local = new XlsxWorkbookDefinedName();
        global.Name = "Rate";
        global.Text = "0.05";
        local.Name = "Rate";
        local.Text = "Sheet1!$A$1";
        local.LocalSheetId = 0;
        local.HasLocalSheetId = true;
        part.DefinedNames.Add(global);
        part.DefinedNames.Add(local);

        Assert.That(part.GetDefinedName("Rate")!.Text, Is.EqualTo("0.05"));
        Assert.That(part.GetGlobalDefinedName("Rate")!.Text, Is.EqualTo("0.05"));
        Assert.That(part.GetLocalDefinedName("Rate", 0)!.Text, Is.EqualTo("Sheet1!$A$1"));
        Assert.That(part.GetDefinedNameForSheet("Rate", 0)!.Text, Is.EqualTo("Sheet1!$A$1"));
        Assert.That(part.GetDefinedNameForSheet("Rate", 1)!.Text, Is.EqualTo("0.05"));
    }

    [Test]
    public void WriteString_PreservesLoadedDefinedNameUnknownAttributes()
    {
        string xml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "<definedNames><definedName name=\"Future\" description=\"Known\" x:futureName=\"n\">Sheet1!$A$1</definedName></definedNames>" +
            "</workbook>";
        XlsxWorkbookPart loaded = new XlsxWorkbookPartReader().ReadString(xml);

        XlsxWorkbookPartWriteResult result = new XlsxWorkbookPartWriter().WriteString(loaded);
        XlsxWorkbookPart roundTrip = new XlsxWorkbookPartReader().ReadString(result.Xml);

        Assert.That(loaded.IsValid, Is.True);
        Assert.That(loaded.DefinedNames[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("description=\"Known\""));
        Assert.That(result.Xml, Does.Contain("futureName"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.DefinedNames[0].Description, Is.EqualTo("Known"));
        Assert.That(roundTrip.DefinedNames[0].UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteBytes_RejectsInvalidDefinedNameDefinitions()
    {
        XlsxWorkbookPart nullList = CreateMinimumWorkbook();
        XlsxWorkbookPart nullEntry = CreateMinimumWorkbook();
        XlsxWorkbookPart missingName = CreateMinimumWorkbook();
        XlsxWorkbookPart negativeScope = CreateMinimumWorkbook();
        XlsxWorkbookPart duplicateScope = CreateMinimumWorkbook();
        XlsxWorkbookPart nullUnknownAttributes = CreateMinimumWorkbook();

        nullList.DefinedNames = null!;
        nullEntry.DefinedNames.Add(null!);
        missingName.DefinedNames.Add(new XlsxWorkbookDefinedName());
        XlsxWorkbookDefinedName negative = new XlsxWorkbookDefinedName();
        negative.Name = "Bad";
        negative.LocalSheetId = -1;
        negative.HasLocalSheetId = true;
        negativeScope.DefinedNames.Add(negative);
        duplicateScope.DefinedNames.Add(CreateDefinedName("Dup", "1", 0, true));
        duplicateScope.DefinedNames.Add(CreateDefinedName("Dup", "2", 0, true));
        XlsxWorkbookDefinedName nullUnknowns = CreateDefinedName("BadUnknowns", "1", 0, false);
        nullUnknowns.UnknownAttributes = null!;
        nullUnknownAttributes.DefinedNames.Add(nullUnknowns);

        XlsxWorkbookPartWriter writer = new XlsxWorkbookPartWriter();
        XlsxWorkbookPartWriteResult nullListResult = writer.WriteBytes(nullList);
        XlsxWorkbookPartWriteResult nullEntryResult = writer.WriteBytes(nullEntry);
        XlsxWorkbookPartWriteResult missingNameResult = writer.WriteBytes(missingName);
        XlsxWorkbookPartWriteResult negativeScopeResult = writer.WriteBytes(negativeScope);
        XlsxWorkbookPartWriteResult duplicateScopeResult = writer.WriteBytes(duplicateScope);
        XlsxWorkbookPartWriteResult nullUnknownAttributesResult = writer.WriteBytes(nullUnknownAttributes);

        Assert.That(HasDiagnostic(nullListResult, "workbook.definedNamesNull"), Is.True);
        Assert.That(HasDiagnostic(nullEntryResult, "workbook.definedNameNull"), Is.True);
        Assert.That(HasDiagnostic(missingNameResult, "workbook.definedName.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(negativeScopeResult, "workbook.definedName.localSheetIdRange"), Is.True);
        Assert.That(HasDiagnostic(duplicateScopeResult, "workbook.definedName.duplicate"), Is.True);
        Assert.That(HasDiagnostic(nullUnknownAttributesResult, "workbook.definedNameUnknownAttributesNull"), Is.True);
    }

    static void ConfigureMetadata(XlsxWorkbookDefinedName definedName)
    {
        definedName.Comment = "Comment";
        definedName.CustomMenu = "Menu";
        definedName.Description = "Description";
        definedName.Help = "Help";
        definedName.StatusBar = "Status";
        definedName.ShortcutKey = "K";
        definedName.Hidden = true;
        definedName.HasHidden = true;
        definedName.Function = true;
        definedName.HasFunction = true;
        definedName.VbProcedure = false;
        definedName.HasVbProcedure = true;
        definedName.Xlm = true;
        definedName.HasXlm = true;
        definedName.FunctionGroupId = 7;
        definedName.HasFunctionGroupId = true;
        definedName.PublishToServer = true;
        definedName.HasPublishToServer = true;
        definedName.WorkbookParameter = false;
        definedName.HasWorkbookParameter = true;
    }

    static XlsxWorkbookDefinedName CreateDefinedName(string name, string text, int localSheetId, bool hasLocalSheetId)
    {
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = name;
        definedName.Text = text;
        definedName.LocalSheetId = localSheetId;
        definedName.HasLocalSheetId = hasLocalSheetId;
        return definedName;
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
