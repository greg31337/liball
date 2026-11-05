using System.Text;

namespace odf.Tests;

public class SpreadsheetDataModelTests
{
    [Test]
    public void MaterializesNamedDatabaseSubtotalConsolidationAndScenarioData()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());
        OdfNamedExpression namedRange = document.NamedExpressions[0];
        OdfNamedExpression expression = document.NamedExpressions[1];
        OdfDatabaseRange database = document.DatabaseRanges[0];

        Assert.Multiple(() =>
        {
            Assert.That(document.NamedExpressions, Has.Length.EqualTo(2));
            Assert.That(namedRange.Name, Is.EqualTo("Data"));
            Assert.That(namedRange.IsRange, Is.True);
            Assert.That(namedRange.RangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C4"));
            Assert.That(expression.Name, Is.EqualTo("Tax"));
            Assert.That(expression.Expression, Is.EqualTo("of:=[Sheet1.C4]*0.2"));
            Assert.That(document.LabelRanges, Has.Length.EqualTo(1));
            Assert.That(document.LabelRanges[0].Orientation, Is.EqualTo("row"));
            Assert.That(database.Name, Is.EqualTo("SalesDb"));
            Assert.That(database.ContainsHeader, Is.True);
            Assert.That(database.DisplayFilterButtons, Is.True);
            Assert.That(database.KeepStyles, Is.False);
            Assert.That(database.SortCaseSensitive, Is.True);
            Assert.That(database.SortRules, Has.Length.EqualTo(2));
            Assert.That(database.SortRules[0].FieldNumber, Is.EqualTo(1));
            Assert.That(database.SortRules[1].Order, Is.EqualTo("descending"));
            Assert.That(database.FilterRules, Has.Length.EqualTo(2));
            Assert.That(database.FilterRules[0].Connector, Is.EqualTo("and"));
            Assert.That(database.FilterRules[0].GroupDepth, Is.EqualTo(1));
            Assert.That(database.FilterRules[1].Operator, Is.EqualTo(">"));
            Assert.That(database.SubtotalRules, Has.Length.EqualTo(1));
            Assert.That(database.SubtotalRules[0].GroupByFieldNumber,
                Is.EqualTo(0));
            Assert.That(database.SubtotalRules[0].Fields[0].Function,
                Is.EqualTo("sum"));
            Assert.That(document.Consolidations, Has.Length.EqualTo(1));
            Assert.That(document.Consolidations[0].Function,
                Is.EqualTo("average"));
            Assert.That(document.Scenarios, Has.Length.EqualTo(1));
            Assert.That(document.Scenarios[0].Table, Is.SameAs(document.Tables[0]));
            Assert.That(document.Scenarios[0].IsActive, Is.True);
            Assert.That(document.Scenarios[0].CopyFormulas, Is.True);
        });
    }

    [Test]
    public void SynchronizesEditsAdditionsAndRemovalsThroughRoundTrip()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());
        document.NamedExpressions = [document.NamedExpressions[0]];
        document.NamedExpressions[0].Name = "RenamedData";
        document.NamedExpressions[0].RangeAddress = "Sheet1.A2:Sheet1.C5";
        OdfNamedExpression addedExpression = new OdfNamedExpression();
        addedExpression.Name = "Added";
        addedExpression.Expression = "of:=[Sheet1.A2]+1";
        addedExpression.BaseCellAddress = "Sheet1.A2";
        document.NamedExpressions =
            [document.NamedExpressions[0], addedExpression];
        document.LabelRanges[0].Orientation = "column";
        OdfDatabaseRange database = document.DatabaseRanges[0];
        database.DisplayFilterButtons = false;
        database.KeepSize = true;
        database.SortRules = [database.SortRules[1]];
        database.SortRules[0].FieldNumber = 2;
        database.FilterRules[0].Value = "West";
        OdfFilterRule addedFilter = new OdfFilterRule();
        addedFilter.FieldNumber = 2;
        addedFilter.Operator = "<=";
        addedFilter.Value = "500";
        addedFilter.DataType = "number";
        database.FilterRules =
            [database.FilterRules[0], database.FilterRules[1], addedFilter];
        database.SubtotalRules[0].Fields[0].Function = "average";
        document.Consolidations[0].UseLabel = "both";
        document.Scenarios[0].Comment = "Updated scenario";
        OdfScenario addedScenario = new OdfScenario();
        addedScenario.Table = document.Tables[0];
        addedScenario.RangeAddresses = "Sheet1.B2:Sheet1.C3";
        addedScenario.Comment = "Alternative";
        addedScenario.CopyBack = true;
        document.Scenarios = [document.Scenarios[0], addedScenario];

        OdfDocument reread = WriteAndRead(document);
        OdfDatabaseRange result = reread.DatabaseRanges[0];

        Assert.Multiple(() =>
        {
            Assert.That(reread.NamedExpressions, Has.Length.EqualTo(2));
            Assert.That(reread.NamedExpressions[0].Name,
                Is.EqualTo("RenamedData"));
            Assert.That(reread.NamedExpressions[1].Name, Is.EqualTo("Added"));
            Assert.That(reread.NamedExpressions.Any(item => item.Name == "Tax"),
                Is.False);
            Assert.That(reread.LabelRanges[0].Orientation,
                Is.EqualTo("column"));
            Assert.That(result.DisplayFilterButtons, Is.False);
            Assert.That(result.KeepSize, Is.True);
            Assert.That(result.SortRules, Has.Length.EqualTo(1));
            Assert.That(result.SortRules[0].FieldNumber, Is.EqualTo(2));
            Assert.That(result.FilterRules, Has.Length.EqualTo(3));
            Assert.That(result.FilterRules[0].Value, Is.EqualTo("West"));
            Assert.That(result.FilterRules[2].Operator, Is.EqualTo("<="));
            Assert.That(result.SubtotalRules[0].Fields[0].Function,
                Is.EqualTo("average"));
            Assert.That(reread.Consolidations[0].UseLabel, Is.EqualTo("both"));
            Assert.That(reread.Scenarios, Has.Length.EqualTo(2));
            Assert.That(reread.Scenarios[1].Comment, Is.EqualTo("Alternative"));
            Assert.That(reread.Scenarios[1].Table, Is.SameAs(reread.Tables[0]));
        });
    }

    [Test]
    public void StructuralEditsUpdateEverySpreadsheetFeatureReference()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());

        TableStructureEditor.InsertRows(
            document,
            document.Tables[0],
            1,
            2);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.NamedExpressions[0].RangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C6"));
            Assert.That(document.NamedExpressions[1].Expression,
                Is.EqualTo("of:=[Sheet1.C6]*0.2"));
            Assert.That(document.LabelRanges[0].DataRangeAddress,
                Is.EqualTo("Sheet1.A4:Sheet1.C6"));
            Assert.That(document.DatabaseRanges[0].TargetRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C6"));
            Assert.That(document.Consolidations[0].SourceRangeAddresses,
                Is.EqualTo("Sheet1.A1:Sheet1.C6 Sheet1.E1:Sheet1.G6"));
            Assert.That(document.Scenarios[0].RangeAddresses,
                Is.EqualTo("Sheet1.A4:Sheet1.C5"));
            Assert.That(reread.DatabaseRanges[0].TargetRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C6"));
        });
    }

    [Test]
    public void RejectsNegativeFieldNumbersDuringSynchronization()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());
        document.DatabaseRanges[0].SortRules[0].FieldNumber = -1;

        Assert.That(
            () => new OdfDocumentWriter().WriteDocumentBytes(
                document,
                new OdfSaveOptions()),
            Throws.TypeOf<OdfEditException>());
    }

    private static string FeatureXml()
    {
        return "<table:table table:name='Sheet1'>"
            + "<table:scenario table:scenario-ranges='Sheet1.A2:Sheet1.C3' "
            + "table:comment='Baseline' table:is-active='true' "
            + "table:copy-formulas='true'/><table:table-row table:number-rows-repeated='4'>"
            + "<table:table-cell table:number-columns-repeated='7'/></table:table-row>"
            + "</table:table>"
            + "<table:named-expressions>"
            + "<table:named-range table:name='Data' "
            + "table:base-cell-address='Sheet1.A1' "
            + "table:cell-range-address='Sheet1.A1:Sheet1.C4' "
            + "table:range-usable-as='repeat-row filter'/>"
            + "<table:named-expression table:name='Tax' "
            + "table:base-cell-address='Sheet1.C4' "
            + "table:expression='of:=[Sheet1.C4]*0.2'/></table:named-expressions>"
            + "<table:label-ranges><table:label-range "
            + "table:label-cell-range-address='Sheet1.A1:Sheet1.C1' "
            + "table:data-cell-range-address='Sheet1.A2:Sheet1.C4' "
            + "table:orientation='row'/></table:label-ranges>"
            + "<table:database-ranges><table:database-range table:name='SalesDb' "
            + "table:target-range-address='Sheet1.A1:Sheet1.C4' "
            + "table:display-filter-buttons='true' table:on-update-keep-styles='false'>"
            + "<table:sort table:case-sensitive='true' table:target-range-address='Sheet1.A2:Sheet1.C4'>"
            + "<table:sort-by table:field-number='1' table:data-type='text'/>"
            + "<table:sort-by table:field-number='2' table:data-type='number' table:order='descending'/>"
            + "</table:sort><table:filter><table:filter-and>"
            + "<table:filter-condition table:field-number='0' table:operator='=' table:value='North'/>"
            + "<table:filter-condition table:field-number='2' table:operator='&gt;' table:value='100' table:data-type='number'/>"
            + "</table:filter-and></table:filter>"
            + "<table:subtotal-rules><table:subtotal-rule table:group-by-field-number='0'>"
            + "<table:subtotal-field table:field-number='2' table:function='sum'/>"
            + "</table:subtotal-rule></table:subtotal-rules>"
            + "</table:database-range></table:database-ranges>"
            + "<table:consolidation table:function='average' "
            + "table:source-cell-range-addresses='Sheet1.A1:Sheet1.C4 Sheet1.E1:Sheet1.G4' "
            + "table:target-cell-address='Sheet1.A6' table:use-label='row'/>";
    }

    private static OdfDocument ReadSpreadsheet(string content)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:of='" + OdfNamespaces.OpenFormula + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:body><office:spreadsheet>"
            + content
            + "</office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "spreadsheet-data.fods",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "spreadsheet-data.fods",
            new OdfLoadOptions());
    }
}
