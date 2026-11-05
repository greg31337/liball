using System.Text;

namespace odf.Tests;

public class SpreadsheetSettingsModelTests
{
    [Test]
    public void MaterializesCalculationAndViewSettings()
    {
        OdfSpreadsheetSettings settings = ReadSpreadsheet(
            FeatureXml()).SpreadsheetSettings;

        Assert.Multiple(() =>
        {
            Assert.That(settings.HasCalculationSettings, Is.True);
            Assert.That(settings.CaseSensitive, Is.False);
            Assert.That(settings.PrecisionAsShown, Is.True);
            Assert.That(settings.MatchWholeCell, Is.False);
            Assert.That(settings.AutomaticFindLabels, Is.False);
            Assert.That(settings.UseRegularExpressions, Is.False);
            Assert.That(settings.UseWildcards, Is.True);
            Assert.That(settings.NullYear, Is.EqualTo(1950));
            Assert.That(settings.HasNullDate, Is.True);
            Assert.That(settings.NullDate, Is.EqualTo("1904-01-01"));
            Assert.That(settings.HasIteration, Is.True);
            Assert.That(settings.IterationEnabled, Is.True);
            Assert.That(settings.IterationSteps, Is.EqualTo(250));
            Assert.That(settings.MaximumDifference, Is.EqualTo(0.0001));
            Assert.That(settings.ActiveTable, Is.EqualTo("Sheet2"));
            Assert.That(settings.SelectedTables,
                Is.EqualTo("Sheet1 Sheet2"));
            Assert.That(settings.SelectedRanges,
                Is.EqualTo("Sheet2.A2:Sheet2.C5"));
            Assert.That(settings.CalculationNode, Is.Not.Null);
            Assert.That(settings.NullDateNode, Is.Not.Null);
            Assert.That(settings.IterationNode, Is.Not.Null);
        });
    }

    [Test]
    public void SynchronizesEditsAndOptionalElementRemoval()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());
        OdfSpreadsheetSettings settings = document.SpreadsheetSettings;
        settings.CaseSensitive = true;
        settings.PrecisionAsShown = false;
        settings.MatchWholeCell = true;
        settings.AutomaticFindLabels = true;
        settings.UseRegularExpressions = true;
        settings.UseWildcards = false;
        settings.NullYear = 1940;
        settings.HasNullDate = false;
        settings.HasIteration = false;
        settings.ActiveTable = "Sheet1";
        settings.SelectedTables = "Sheet1";
        settings.SelectedRanges = "Sheet1.B3:Sheet1.D8";

        OdfSpreadsheetSettings result = WriteAndRead(
            document,
            "spreadsheet-settings.fods").SpreadsheetSettings;

        Assert.Multiple(() =>
        {
            Assert.That(result.CaseSensitive, Is.True);
            Assert.That(result.PrecisionAsShown, Is.False);
            Assert.That(result.MatchWholeCell, Is.True);
            Assert.That(result.AutomaticFindLabels, Is.True);
            Assert.That(result.UseRegularExpressions, Is.True);
            Assert.That(result.UseWildcards, Is.False);
            Assert.That(result.NullYear, Is.EqualTo(1940));
            Assert.That(result.HasNullDate, Is.False);
            Assert.That(result.NullDate, Is.EqualTo("1899-12-30"));
            Assert.That(result.HasIteration, Is.False);
            Assert.That(result.IterationEnabled, Is.False);
            Assert.That(result.IterationSteps, Is.EqualTo(100));
            Assert.That(result.MaximumDifference, Is.EqualTo(0.001));
            Assert.That(result.ActiveTable, Is.EqualTo("Sheet1"));
            Assert.That(result.SelectedTables, Is.EqualTo("Sheet1"));
            Assert.That(result.SelectedRanges,
                Is.EqualTo("Sheet1.B3:Sheet1.D8"));
        });
    }

    [Test]
    public void CreatesCalculationAndSettingsPartsForPackageDocument()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Spreadsheet);
        OdfSpreadsheetSettings settings = document.SpreadsheetSettings;
        settings.PrecisionAsShown = true;
        settings.NullYear = 1970;
        settings.NullDate = "1900-01-01";
        settings.IterationEnabled = true;
        settings.IterationSteps = 75;
        settings.MaximumDifference = 0.00001;
        settings.ActiveTable = "Budget";
        settings.SelectedTables = "Budget";
        settings.SelectedRanges = "Budget.A1:Budget.F20";

        OdfDocument reread = WriteAndRead(document, "settings-created.ods");
        OdfSpreadsheetSettings result = reread.SpreadsheetSettings;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasCalculationSettings, Is.True);
            Assert.That(result.PrecisionAsShown, Is.True);
            Assert.That(result.NullYear, Is.EqualTo(1970));
            Assert.That(result.HasNullDate, Is.True);
            Assert.That(result.NullDate, Is.EqualTo("1900-01-01"));
            Assert.That(result.HasIteration, Is.True);
            Assert.That(result.IterationEnabled, Is.True);
            Assert.That(result.IterationSteps, Is.EqualTo(75));
            Assert.That(result.MaximumDifference, Is.EqualTo(0.00001));
            Assert.That(result.ActiveTable, Is.EqualTo("Budget"));
            Assert.That(result.SelectedTables, Is.EqualTo("Budget"));
            Assert.That(result.SelectedRanges,
                Is.EqualTo("Budget.A1:Budget.F20"));
            Assert.That(reread.Parts.Any(part =>
                part.Path == OdfPartNames.Settings), Is.True);
        });
    }

    [Test]
    public void StructuralEditsAdjustSelectedRanges()
    {
        OdfDocument document = ReadSpreadsheet(
            FeatureXml().Replace(
                "Sheet2.A2:Sheet2.C5",
                "Sheet1.A2:Sheet1.C5"));

        TableStructureEditor.InsertRows(
            document,
            document.Tables[0],
            0,
            2);

        Assert.That(document.SpreadsheetSettings.SelectedRanges,
            Is.EqualTo("Sheet1.A4:Sheet1.C7"));
    }

    [Test]
    public void RejectsInvalidCalculationSettings()
    {
        OdfDocument invalidSteps = ReadSpreadsheet("");
        invalidSteps.SpreadsheetSettings.HasCalculationSettings = true;
        invalidSteps.SpreadsheetSettings.HasIteration = true;
        invalidSteps.SpreadsheetSettings.IterationSteps = 0;
        OdfDocument invalidMatching = ReadSpreadsheet("");
        invalidMatching.SpreadsheetSettings.HasCalculationSettings = true;
        invalidMatching.SpreadsheetSettings.UseRegularExpressions = true;
        invalidMatching.SpreadsheetSettings.UseWildcards = true;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => WriteDocument(invalidSteps),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(
                () => WriteDocument(invalidMatching),
                Throws.TypeOf<OdfDocumentException>());
        });
    }

    private static string FeatureXml()
    {
        return "<office:settings>"
            + "<config:config-item-set config:name='ooo:view-settings'>"
            + "<config:config-item-map-indexed config:name='Views'>"
            + "<config:config-item-map-entry>"
            + "<config:config-item config:name='ActiveTable' config:type='string'>Sheet2</config:config-item>"
            + "<config:config-item config:name='SelectedTables' config:type='string'>Sheet1 Sheet2</config:config-item>"
            + "<config:config-item config:name='SelectedRanges' config:type='string'>Sheet2.A2:Sheet2.C5</config:config-item>"
            + "</config:config-item-map-entry>"
            + "</config:config-item-map-indexed>"
            + "</config:config-item-set>"
            + "</office:settings>"
            + "<office:body><office:spreadsheet>"
            + "<table:calculation-settings table:case-sensitive='false' "
            + "table:precision-as-shown='true' "
            + "table:search-criteria-must-apply-to-whole-cell='false' "
            + "table:automatic-find-labels='false' "
            + "table:use-regular-expressions='false' "
            + "table:use-wildcards='true' table:null-year='1950'>"
            + "<table:null-date table:value-type='date' table:date-value='1904-01-01'/>"
            + "<table:iteration table:status='enable' table:steps='250' "
            + "table:maximum-difference='0.0001'/>"
            + "</table:calculation-settings>"
            + "<table:table table:name='Sheet1'/>"
            + "<table:table table:name='Sheet2'/>"
            + "</office:spreadsheet></office:body>";
    }

    private static OdfDocument ReadSpreadsheet(string content)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:config='" + OdfNamespaces.Config + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + (content.Length == 0
                ? "<office:body><office:spreadsheet/></office:body>"
                : content)
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "spreadsheet-settings.fods",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(
        OdfDocument document,
        string path)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            WriteDocument(document),
            path,
            new OdfLoadOptions());
    }

    private static byte[] WriteDocument(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }
}
