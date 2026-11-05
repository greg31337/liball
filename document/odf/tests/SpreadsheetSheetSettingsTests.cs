using System.Text;

namespace odf.Tests;

public class SpreadsheetSheetSettingsTests
{
    [Test]
    public void MaterializesSourceScenarioProtectionPrintAndDisplaySettings()
    {
        OdfDocument document = ReadFeatureSpreadsheet();
        OdfTable table = document.Tables[0];
        OdfTableSource source = table.Source;
        OdfScenario scenario = document.Scenarios[0];
        OdfSpreadsheetSettings settings = document.SpreadsheetSettings;

        Assert.Multiple(() =>
        {
            Assert.That(source.HasSource, Is.True);
            Assert.That(source.Href, Is.EqualTo("../source.ods"));
            Assert.That(source.XLinkType, Is.EqualTo("simple"));
            Assert.That(source.XLinkActuate, Is.EqualTo("onRequest"));
            Assert.That(source.TableName, Is.EqualTo("Source Data"));
            Assert.That(source.Mode, Is.EqualTo("copy-results-only"));
            Assert.That(source.FilterName, Is.EqualTo("calc8"));
            Assert.That(source.FilterOptions, Is.EqualTo("44,34,0,1"));
            Assert.That(source.RefreshDelay, Is.EqualTo("PT15M"));
            Assert.That(source.Node, Is.Not.Null);
            Assert.That(scenario.DisplayBorder, Is.False);
            Assert.That(scenario.BorderColor, Is.EqualTo("#336699"));
            Assert.That(scenario.IsActive, Is.True);
            Assert.That(table.IsPrinted, Is.False);
            Assert.That(table.PrintRangeText, Is.EqualTo(".A1:.D20"));
            Assert.That(table.IsProtected, Is.True);
            Assert.That(table.ProtectionKey, Is.EqualTo("sheet-key"));
            Assert.That(table.ProtectionKeyDigestAlgorithm,
                Is.EqualTo("urn:example:sha256"));
            Assert.That(settings.StructureProtected, Is.True);
            Assert.That(settings.ProtectionKey, Is.EqualTo("book-key"));
            Assert.That(settings.ProtectionKeyDigestAlgorithm,
                Is.EqualTo("urn:example:sha512"));
        });
    }

    [Test]
    public void SynchronizesLinkedSourceScenarioAndProtectionEdits()
    {
        OdfDocument document = ReadFeatureSpreadsheet();
        OdfTable table = document.Tables[0];
        table.Source.Href = "../updated.ods";
        table.Source.TableName = "Updated";
        table.Source.Mode = "copy-all";
        table.Source.FilterName = "";
        table.Source.FilterOptions = "";
        table.Source.RefreshDelay = "PT1H";
        table.IsPrinted = true;
        table.ProtectionKey = "new-sheet-key";
        table.ProtectionKeyDigestAlgorithm = "urn:example:new-sheet-digest";
        OdfScenario scenario = document.Scenarios[0];
        scenario.DisplayBorder = true;
        scenario.BorderColor = "#AA5500";
        scenario.CopyBack = true;
        OdfSpreadsheetSettings settings = document.SpreadsheetSettings;
        settings.StructureProtected = false;
        settings.ProtectionKey = "new-book-key";
        settings.ProtectionKeyDigestAlgorithm =
            "urn:example:new-book-digest";

        OdfDocument reread = WriteAndRead(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfTable result = reread.Tables[0];
        OdfScenario resultScenario = reread.Scenarios[0];
        OdfSpreadsheetSettings resultSettings = reread.SpreadsheetSettings;

        Assert.Multiple(() =>
        {
            Assert.That(result.Source.Href, Is.EqualTo("../updated.ods"));
            Assert.That(result.Source.TableName, Is.EqualTo("Updated"));
            Assert.That(result.Source.Mode, Is.EqualTo("copy-all"));
            Assert.That(result.Source.FilterName, Is.Empty);
            Assert.That(result.Source.FilterOptions, Is.Empty);
            Assert.That(result.Source.RefreshDelay, Is.EqualTo("PT1H"));
            Assert.That(result.IsPrinted, Is.True);
            Assert.That(result.ProtectionKey, Is.EqualTo("new-sheet-key"));
            Assert.That(result.ProtectionKeyDigestAlgorithm,
                Is.EqualTo("urn:example:new-sheet-digest"));
            Assert.That(resultScenario.DisplayBorder, Is.True);
            Assert.That(resultScenario.BorderColor, Is.EqualTo("#AA5500"));
            Assert.That(resultScenario.CopyBack, Is.True);
            Assert.That(resultSettings.StructureProtected, Is.False);
            Assert.That(resultSettings.ProtectionKey,
                Is.EqualTo("new-book-key"));
            Assert.That(resultSettings.ProtectionKeyDigestAlgorithm,
                Is.EqualTo("urn:example:new-book-digest"));
        });
    }

    [Test]
    public void CreatesAndRemovesLinkedTableSource()
    {
        OdfDocument document = ReadSpreadsheet(
            "",
            "<table:table table:name='Local'><table:table-row>"
                + "<table:table-cell/></table:table-row></table:table>");
        OdfTableSource source = document.Tables[0].Source;
        source.Href = "linked.ods";
        source.TableName = "Remote";
        source.XLinkActuate = "onRequest";
        source.RefreshDelay = "P1D";

        OdfDocument created = WriteAndRead(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(created.Tables[0].Source.HasSource, Is.True);
            Assert.That(created.Tables[0].Source.Href,
                Is.EqualTo("linked.ods"));
            Assert.That(created.Tables[0].Source.TableName,
                Is.EqualTo("Remote"));
            Assert.That(created.Tables[0].Source.RefreshDelay,
                Is.EqualTo("P1D"));
        });

        created.Tables[0].Source.HasSource = false;
        OdfDocument removed = WriteAndRead(
            created,
            new OdfSaveOptions
            {
                Deterministic = true
            });

        Assert.That(removed.Tables[0].Source.HasSource, Is.False);
        Assert.That(removed.Tables[0].Source.Node, Is.Null);
    }

    [Test]
    public void OmitsDigestAlgorithmsBeforeOdf12AndRestoresThemForOdf12()
    {
        OdfDocument document = ReadFeatureSpreadsheet();

        OdfDocument legacy = WriteAndRead(
            document,
            new OdfSaveOptions
            {
                Deterministic = true,
                TargetVersion = OdfVersion.Version11
            });
        OdfDocument modern = WriteAndRead(
            document,
            new OdfSaveOptions
            {
                Deterministic = true,
                TargetVersion = OdfVersion.Version12
            });

        Assert.Multiple(() =>
        {
            Assert.That(legacy.VersionInfo!.Version,
                Is.EqualTo(OdfVersion.Version11));
            Assert.That(legacy.Tables[0].ProtectionKeyDigestAlgorithm,
                Is.Empty);
            Assert.That(
                legacy.SpreadsheetSettings.ProtectionKeyDigestAlgorithm,
                Is.Empty);
            Assert.That(modern.VersionInfo!.Version,
                Is.EqualTo(OdfVersion.Version12));
            Assert.That(modern.Tables[0].ProtectionKeyDigestAlgorithm,
                Is.EqualTo("urn:example:sha256"));
            Assert.That(
                modern.SpreadsheetSettings.ProtectionKeyDigestAlgorithm,
                Is.EqualTo("urn:example:sha512"));
        });
    }

    [Test]
    public void RejectsInvalidNewLinkedTableSource()
    {
        OdfDocument document = ReadSpreadsheet(
            "",
            "<table:table table:name='Local'/>");
        document.Tables[0].Source.HasSource = true;
        document.Tables[0].Source.Href = "linked.ods";
        document.Tables[0].Source.Mode = "vendor-copy";

        Assert.That(
            () => WriteDocument(
                document,
                new OdfSaveOptions
                {
                    Deterministic = true
                }),
            Throws.TypeOf<OdfDocumentException>());
    }

    private static OdfDocument ReadFeatureSpreadsheet()
    {
        return ReadSpreadsheet(
            " table:structure-protected='true'"
                + " table:protection-key='book-key'"
                + " table:protection-key-digest-algorithm='urn:example:sha512'",
            "<table:table table:name='Linked' table:protected='true' "
                + "table:protection-key='sheet-key' "
                + "table:protection-key-digest-algorithm='urn:example:sha256' "
                + "table:print='false' table:print-ranges='.A1:.D20'>"
                + "<table:title>Linked data</table:title>"
                + "<table:desc>Imported values</table:desc>"
                + "<table:table-source xlink:type='simple' "
                + "xlink:href='../source.ods' xlink:actuate='onRequest' "
                + "table:table-name='Source Data' "
                + "table:mode='copy-results-only' table:filter-name='calc8' "
                + "table:filter-options='44,34,0,1' "
                + "table:refresh-delay='PT15M'/>"
                + "<table:scenario table:scenario-ranges='.A1:.D20' "
                + "table:is-active='true' table:display-border='false' "
                + "table:border-color='#336699'/>"
                + "<table:table-row><table:table-cell/>"
                + "</table:table-row></table:table>");
    }

    private static OdfDocument ReadSpreadsheet(
        string spreadsheetAttributes,
        string tables)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:body><office:spreadsheet"
            + spreadsheetAttributes
            + ">"
            + tables
            + "</office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "sheet-settings.fods",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(
        OdfDocument document,
        OdfSaveOptions options)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            WriteDocument(document, options),
            "sheet-settings.fods",
            new OdfLoadOptions());
    }

    private static byte[] WriteDocument(
        OdfDocument document,
        OdfSaveOptions options)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            options);
    }
}
