using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SecurityOptionsTests
{
    [Test]
    public void PackageAndXmlLimits_RejectOversizedHighlyCompressedAndTextHeavyContent()
    {
        byte[] packageBytes = CreateZip("xl/payload.xml", new string('A', 4096));
        XlsxReaderOptions packageOptions = new XlsxReaderOptions();
        packageOptions.SecurityOptions.MaxPartBytes = 512;
        packageOptions.SecurityOptions.MaxCompressionRatio = 2;

        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(packageBytes, packageOptions);

        XlsxReaderOptions xmlOptions = new XlsxReaderOptions();
        xmlOptions.SecurityOptions.MaxXmlAttributesPerElement = 1;
        xmlOptions.SecurityOptions.MaxXmlTotalTextCharacters = 3;
        XlsxXmlReadLimitResult xml = XlsxXmlReaderHelpers.ValidateStringLimits("<root a=\"1\" b=\"2\"><value>text</value></root>", xmlOptions, "/test.xml");

        Assert.That(package.IsValid, Is.False);
        Assert.That(HasDiagnostic(package.Validation, "zip.maxPartBytes"), Is.True);
        Assert.That(HasDiagnostic(package.Validation, "zip.maxCompressionRatio"), Is.True);
        Assert.That(xml.IsValid, Is.False);
        Assert.That(xml.MaxObservedAttributes, Is.EqualTo(2));
        Assert.That(xml.TotalObservedTextCharacters, Is.EqualTo(4));
        Assert.That(HasDiagnostic(xml.Validation, "xml.maxAttributes"), Is.True);
        Assert.That(HasDiagnostic(xml.Validation, "xml.maxTotalTextCharacters"), Is.True);
    }

    [Test]
    public void Apply_ReportsEveryWorkbookResourceLimit()
    {
        XlsxWorkbook workbook = CreateSecurityContentWorkbook();
        workbook.Images.Add(new XlsxImagePart());
        XlsxCommentsPart comments = new XlsxCommentsPart();
        comments.Comments.Add(new XlsxComment());
        comments.Comments.Add(new XlsxComment());
        workbook.Comments.Add(comments);
        workbook.Worksheets[0].SetFormulaCachedValue("A2", "1+1", "2", "n");
        workbook.WorkbookRelationships.Relationships.Add(CreateExternalRelationship("rIdExternal2", XlsxRelationshipTypes.Hyperlink, "https://example.invalid/2"));
        XlsxSecurityOptions options = new XlsxSecurityOptions();
        options.MaxImages = 1;
        options.MaxComments = 1;
        options.MaxFormulas = 1;
        options.MaxExternalRelationships = 1;

        XlsxSecurityPolicyResult result = new XlsxWorkbookSecurityService().Apply(workbook, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ImageCount, Is.EqualTo(2));
        Assert.That(result.CommentCount, Is.EqualTo(2));
        Assert.That(result.FormulaCount, Is.EqualTo(2));
        Assert.That(result.ExternalRelationshipCount, Is.EqualTo(2));
        Assert.That(HasDiagnostic(result.Validation, "security.maxImagesExceeded"), Is.True);
        Assert.That(HasDiagnostic(result.Validation, "security.maxCommentsExceeded"), Is.True);
        Assert.That(HasDiagnostic(result.Validation, "security.maxFormulasExceeded"), Is.True);
        Assert.That(HasDiagnostic(result.Validation, "security.maxExternalRelationshipsExceeded"), Is.True);
    }

    [Test]
    public void Apply_PreserveAndRejectPoliciesLeaveContentUnchanged()
    {
        XlsxWorkbook workbook = CreateSecurityContentWorkbook();
        XlsxSecurityOptions preserve = new XlsxSecurityOptions();

        XlsxSecurityPolicyResult preserved = new XlsxWorkbookSecurityService().Apply(workbook, preserve);

        XlsxSecurityOptions reject = CreateUniformPolicy(XlsxSecurityContentPolicy.Reject);
        XlsxSecurityPolicyResult rejected = new XlsxWorkbookSecurityService().Apply(workbook, reject);

        Assert.That(preserved.IsValid, Is.True);
        Assert.That(rejected.IsValid, Is.False);
        Assert.That(rejected.Validation.CountSeverity(XlsxDiagnosticSeverity.Error), Is.EqualTo(8));
        Assert.That(workbook.HasVbaProject, Is.True);
        Assert.That(workbook.EmbeddedObjects.Count, Is.EqualTo(1));
        Assert.That(workbook.ExternalLinks.Count, Is.EqualTo(1));
        Assert.That(workbook.HasConnections, Is.True);
        Assert.That(workbook.QueryTables.Count, Is.EqualTo(1));
        Assert.That(workbook.CustomXmlParts.Count, Is.EqualTo(1));
        Assert.That(workbook.UnknownParts.ContainsPart("/xl/unknown/payload.exe"), Is.True);
        Assert.That(CountRelationship(workbook, XlsxRelationshipTypes.Image), Is.EqualTo(1));
    }

    [Test]
    public void Apply_RemovePoliciesStripEveryRequestedContentFamilyAndPreserveSafeUnknownParts()
    {
        XlsxWorkbook workbook = CreateSecurityContentWorkbook();
        XlsxSecurityOptions remove = CreateUniformPolicy(XlsxSecurityContentPolicy.Remove);

        XlsxSecurityPolicyResult result = new XlsxWorkbookSecurityService().Apply(workbook, remove);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.MacrosRemoved, Is.GreaterThan(0));
        Assert.That(result.OleObjectsRemoved, Is.GreaterThan(0));
        Assert.That(result.ExternalImagesRemoved, Is.EqualTo(1));
        Assert.That(result.ExternalLinksRemoved, Is.GreaterThan(0));
        Assert.That(result.ConnectionsRemoved, Is.EqualTo(1));
        Assert.That(result.QueryTablesRemoved, Is.EqualTo(1));
        Assert.That(result.CustomXmlPartsRemoved, Is.EqualTo(1));
        Assert.That(result.UnknownActivePartsRemoved, Is.EqualTo(1));
        Assert.That(workbook.HasVbaProject, Is.False);
        Assert.That(workbook.EmbeddedObjects, Is.Empty);
        Assert.That(workbook.ExternalLinks, Is.Empty);
        Assert.That(workbook.HasConnections, Is.False);
        Assert.That(workbook.QueryTables, Is.Empty);
        Assert.That(workbook.CustomXmlParts, Is.Empty);
        Assert.That(workbook.UnknownParts.ContainsPart("/xl/unknown/payload.exe"), Is.False);
        Assert.That(workbook.UnknownParts.ContainsPart("/xl/unknown/safe.xml"), Is.True);
        Assert.That(CountRelationship(workbook, XlsxRelationshipTypes.Image), Is.EqualTo(0));
        Assert.That(CountRelationship(workbook, XlsxRelationshipTypes.ExternalLink), Is.EqualTo(0));
        Assert.That(CountRelationship(workbook, XlsxRelationshipTypes.Connections), Is.EqualTo(0));
        Assert.That(CountRelationship(workbook, XlsxRelationshipTypes.QueryTable), Is.EqualTo(0));
    }

    [Test]
    public void WorkbookOpen_AppliesRejectPolicyToUnknownActiveContent()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.UnknownParts.AddPart("/xl/unknown/payload.exe", "application/x-msdownload", new byte[] { 1, 2, 3 });
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.SecurityOptions.UnknownActiveContentPolicy = XlsxSecurityContentPolicy.Reject;

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(save.Bytes, options);

        Assert.That(save.IsValid, Is.True);
        Assert.That(open.IsValid, Is.False);
        Assert.That(open.Workbook, Is.Not.Null);
        Assert.That(HasDiagnostic(open.Validation, "security.unknownActiveContentRejected"), Is.True);
    }

    [Test]
    public void WorkbookOpen_EnforcesFormulaLimitWithoutMaterializingWorksheets()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = XlsxPackagePaths.Worksheet1;
        worksheet.SetFormulaCachedValue("A1", "1+1", "2", "n");
        worksheet.SetFormulaCachedValue("A2", "2+2", "4", "n");
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        workbook.UnknownParts.AddPart(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.SecurityOptions.MaxFormulas = 1;

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(save.Bytes, options);

        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(open.IsValid, Is.False);
        Assert.That(open.Workbook, Is.Not.Null);
        Assert.That(open.Workbook!.Worksheets, Is.Empty);
        Assert.That(HasDiagnostic(open.Validation, "security.maxFormulasExceeded"), Is.True);
    }

    static XlsxWorkbook CreateSecurityContentWorkbook()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = XlsxPackagePaths.Worksheet1;
        worksheet.SetFormulaCachedValue("A1", "1+1", "2", "n");
        workbook.Worksheets.Add(worksheet);
        workbook.Images.Add(new XlsxImagePart());
        workbook.PackageVariant = XlsxPackageVariant.Xlsm;
        workbook.HasVbaProject = true;
        workbook.VbaProject.Data = new byte[] { 1 };
        workbook.EmbeddedObjects.Add(new XlsxEmbeddedObjectPart());
        workbook.ExternalLinks.Add(new XlsxExternalLinkPart());
        workbook.HasConnections = true;
        workbook.Connections.RawXml = "<connections />";
        workbook.QueryTables.Add(new XlsxQueryTablePart());
        workbook.CustomXmlParts.Add(new XlsxCustomXmlPart());
        workbook.UnknownParts.AddPart("/xl/unknown/payload.exe", "application/x-msdownload", new byte[] { 2 });
        workbook.UnknownParts.AddPart("/xl/unknown/safe.xml", XlsxContentTypes.Xml, Encoding.UTF8.GetBytes("<safe />"));
        workbook.WorkbookRelationships.Relationships.Add(CreateRelationship("rIdVba", XlsxRelationshipTypes.VbaProject, "vbaProject.bin"));
        workbook.WorkbookRelationships.Relationships.Add(CreateRelationship("rIdOle", XlsxRelationshipTypes.OleObject, "embeddings/oleObject1.bin"));
        workbook.WorkbookRelationships.Relationships.Add(CreateExternalRelationship("rIdImage", XlsxRelationshipTypes.Image, "https://example.invalid/image.png"));
        workbook.WorkbookRelationships.Relationships.Add(CreateRelationship("rIdLink", XlsxRelationshipTypes.ExternalLink, "externalLinks/externalLink1.xml"));
        workbook.WorkbookRelationships.Relationships.Add(CreateRelationship("rIdConnection", XlsxRelationshipTypes.Connections, "connections.xml"));
        workbook.WorkbookRelationships.Relationships.Add(CreateRelationship("rIdQuery", XlsxRelationshipTypes.QueryTable, "queryTables/queryTable1.xml"));
        workbook.PackageRelationships.Relationships.Add(CreateRelationship("rIdCustom", XlsxRelationshipTypes.CustomXml, "customXml/item1.xml"));
        return workbook;
    }

    static XlsxSecurityOptions CreateUniformPolicy(XlsxSecurityContentPolicy policy)
    {
        XlsxSecurityOptions options = new XlsxSecurityOptions();
        options.MacroPolicy = policy;
        options.OleObjectPolicy = policy;
        options.ExternalImagePolicy = policy;
        options.ExternalLinkPolicy = policy;
        options.ConnectionPolicy = policy;
        options.QueryTablePolicy = policy;
        options.CustomXmlPolicy = policy;
        options.UnknownActiveContentPolicy = policy;
        return options;
    }

    static XlsxRelationship CreateRelationship(string id, string type, string target)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = id;
        relationship.Type = type;
        relationship.Target = target;
        return relationship;
    }

    static XlsxRelationship CreateExternalRelationship(string id, string type, string target)
    {
        XlsxRelationship relationship = CreateRelationship(id, type, target);
        relationship.TargetMode = "External";
        relationship.IsExternal = true;
        return relationship;
    }

    static int CountRelationship(XlsxWorkbook workbook, string type)
    {
        int count = 0;
        foreach (XlsxRelationship relationship in workbook.WorkbookRelationships.Relationships) { if (relationship.Type == type) { count = count + 1; } }
        foreach (XlsxRelationship relationship in workbook.PackageRelationships.Relationships) { if (relationship.Type == type) { count = count + 1; } }
        return count;
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics) { if (diagnostic.Code == code) { return true; } }
        return false;
    }

    static byte[] CreateZip(string path, string text)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
                using (Stream entryStream = entry.Open())
                {
                    byte[] data = Encoding.UTF8.GetBytes(text);
                    entryStream.Write(data, 0, data.Length);
                }
            }
            return stream.ToArray();
        }
    }
}
