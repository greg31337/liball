using System.Text;

namespace odf.Tests;

public class PivotTableModelTests
{
    [Test]
    public void MaterializesPivotSourceFiltersFieldsLevelsMembersAndSubtotals()
    {
        OdfDocument document = ReadSpreadsheet(PivotXml());
        OdfPivotTable pivot = document.PivotTables[0];
        OdfPivotField region = pivot.Fields[0];
        OdfPivotLevel level = region.Level!;

        Assert.Multiple(() =>
        {
            Assert.That(document.PivotTables, Has.Length.EqualTo(1));
            Assert.That(pivot.Name, Is.EqualTo("SalesPivot"));
            Assert.That(pivot.ApplicationData, Is.EqualTo("preserved"));
            Assert.That(pivot.GrandTotal, Is.EqualTo("both"));
            Assert.That(pivot.IgnoreEmptyRows, Is.True);
            Assert.That(pivot.IdentifyCategories, Is.True);
            Assert.That(pivot.TargetRangeAddress,
                Is.EqualTo("Sheet1.D7:Sheet1.G12"));
            Assert.That(pivot.ButtonRangeAddresses,
                Is.EqualTo("Sheet1.D7:Sheet1.E7"));
            Assert.That(pivot.SourceKind, Is.EqualTo("cell-range"));
            Assert.That(pivot.SourceRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C5"));
            Assert.That(pivot.FilterRules, Has.Length.EqualTo(2));
            Assert.That(pivot.FilterRules[0].Connector, Is.EqualTo("and"));
            Assert.That(pivot.FilterRules[1].Operator, Is.EqualTo(">"));
            Assert.That(pivot.Fields, Has.Length.EqualTo(2));
            Assert.That(region.SourceFieldName, Is.EqualTo("Region"));
            Assert.That(region.Orientation, Is.EqualTo("row"));
            Assert.That(level.ShowEmpty, Is.True);
            Assert.That(level.Subtotals, Has.Length.EqualTo(2));
            Assert.That(level.Subtotals[0].Function, Is.EqualTo("sum"));
            Assert.That(level.Members, Has.Length.EqualTo(2));
            Assert.That(level.Members[0].Name, Is.EqualTo("North"));
            Assert.That(level.Members[0].Display, Is.False);
            Assert.That(level.Members[1].ShowDetails, Is.False);
            Assert.That(level.HasDisplayInfo, Is.True);
            Assert.That(level.DisplayMemberCount, Is.EqualTo(3));
            Assert.That(level.DisplayMemberMode, Is.EqualTo("from-top"));
            Assert.That(level.HasSortInfo, Is.True);
            Assert.That(level.SortMode, Is.EqualTo("data"));
            Assert.That(level.SortOrder, Is.EqualTo("descending"));
            Assert.That(level.HasLayoutInfo, Is.True);
            Assert.That(level.LayoutMode,
                Is.EqualTo("outline-subtotals-bottom"));
            Assert.That(level.AddEmptyLines, Is.True);
            Assert.That(pivot.Fields[1].Function, Is.EqualTo("sum"));
        });
    }

    [Test]
    public void SynchronizesPivotEditsAdditionsAndRemovals()
    {
        OdfDocument document = ReadSpreadsheet(PivotXml());
        OdfPivotTable pivot = document.PivotTables[0];
        pivot.Name = "ChangedPivot";
        pivot.TargetRangeAddress = "Sheet1.E8:Sheet1.H14";
        pivot.ButtonRangeAddresses = "Sheet1.E8:Sheet1.F8";
        pivot.SourceRangeAddress = "Sheet1.A1:Sheet1.C6";
        pivot.ShowFilterButton = false;
        pivot.FilterRules[0].Value = "West";
        OdfFilterRule filter = new OdfFilterRule();
        filter.FieldNumber = 2;
        filter.Operator = "<=";
        filter.Value = "1000";
        filter.DataType = "number";
        pivot.FilterRules = [pivot.FilterRules[0], filter];
        pivot.Fields = [pivot.Fields[0]];
        OdfPivotLevel level = pivot.Fields[0].Level!;
        level.Subtotals = [level.Subtotals[1]];
        level.Subtotals[0].Function = "average";
        level.Members = [level.Members[0]];
        level.Members[0].Name = "West";
        level.Members[0].Display = true;
        level.DisplayMemberMode = "from-bottom";
        level.SortMode = "name";
        level.SortDataField = "";
        OdfPivotTable added = new OdfPivotTable();
        added.Name = "AddedPivot";
        added.TargetRangeAddress = "Sheet1.J1:Sheet1.L5";
        added.SourceKind = "cell-range";
        added.SourceRangeAddress = "Sheet1.A1:Sheet1.B6";
        OdfPivotField addedField = new OdfPivotField();
        addedField.SourceFieldName = "Region";
        addedField.Orientation = "row";
        added.Fields = [addedField];
        document.PivotTables = [pivot, added];

        OdfDocument reread = WriteAndRead(document);
        OdfPivotTable changed = reread.PivotTables.Single(
            item => item.Name == "ChangedPivot");
        OdfPivotTable addedResult = reread.PivotTables.Single(
            item => item.Name == "AddedPivot");
        OdfPivotLevel changedLevel = changed.Fields[0].Level!;

        Assert.Multiple(() =>
        {
            Assert.That(reread.PivotTables, Has.Length.EqualTo(2));
            Assert.That(changed.TargetRangeAddress,
                Is.EqualTo("Sheet1.E8:Sheet1.H14"));
            Assert.That(changed.SourceRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C6"));
            Assert.That(changed.ShowFilterButton, Is.False);
            Assert.That(changed.FilterRules, Has.Length.EqualTo(2));
            Assert.That(changed.FilterRules[0].Value, Is.EqualTo("West"));
            Assert.That(changed.FilterRules[1].Operator, Is.EqualTo("<="));
            Assert.That(changed.Fields, Has.Length.EqualTo(1));
            Assert.That(changedLevel.Subtotals, Has.Length.EqualTo(1));
            Assert.That(changedLevel.Subtotals[0].Function,
                Is.EqualTo("average"));
            Assert.That(changedLevel.Members.Single().Name,
                Is.EqualTo("West"));
            Assert.That(changedLevel.DisplayMemberMode,
                Is.EqualTo("from-bottom"));
            Assert.That(changedLevel.SortMode, Is.EqualTo("name"));
            Assert.That(addedResult.SourceRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.B6"));
            Assert.That(addedResult.Fields.Single().SourceFieldName,
                Is.EqualTo("Region"));
        });
    }

    [Test]
    public void StructuralEditsUpdatePivotSourceOutputAndButtonRanges()
    {
        OdfDocument document = ReadSpreadsheet(PivotXml());

        TableStructureEditor.InsertRows(
            document,
            document.Tables[0],
            1,
            2);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.PivotTables[0].SourceRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C7"));
            Assert.That(document.PivotTables[0].TargetRangeAddress,
                Is.EqualTo("Sheet1.D9:Sheet1.G14"));
            Assert.That(document.PivotTables[0].ButtonRangeAddresses,
                Is.EqualTo("Sheet1.D9:Sheet1.E9"));
            Assert.That(reread.PivotTables[0].SourceRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C7"));
        });
    }

    [Test]
    public void RejectsPivotWithoutFields()
    {
        OdfDocument document = ReadSpreadsheet(PivotXml());
        document.PivotTables[0].Fields = [];

        Assert.That(
            () => new OdfDocumentWriter().WriteDocumentBytes(
                document,
                new OdfSaveOptions()),
            Throws.TypeOf<OdfEditException>());
    }

    private static string PivotXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:body><office:spreadsheet>"
            + "<table:table table:name='Sheet1'><table:table-row "
            + "table:number-rows-repeated='12'><table:table-cell "
            + "table:number-columns-repeated='12'/></table:table-row></table:table>"
            + "<table:data-pilot-tables><table:data-pilot-table "
            + "table:name='SalesPivot' table:application-data='preserved' "
            + "table:grand-total='both' table:ignore-empty-rows='true' "
            + "table:identify-categories='true' "
            + "table:target-range-address='Sheet1.D7:Sheet1.G12' "
            + "table:buttons='Sheet1.D7:Sheet1.E7'>"
            + "<table:source-cell-range table:cell-range-address='Sheet1.A1:Sheet1.C5'>"
            + "<table:filter><table:filter-and>"
            + "<table:filter-condition table:field-number='0' table:operator='=' "
            + "table:value='North'/><table:filter-condition table:field-number='2' "
            + "table:operator='&gt;' table:value='100' table:data-type='number'/>"
            + "</table:filter-and></table:filter></table:source-cell-range>"
            + "<table:data-pilot-field table:source-field-name='Region' "
            + "table:orientation='row'><table:data-pilot-level table:show-empty='true'>"
            + "<table:data-pilot-subtotals>"
            + "<table:data-pilot-subtotal table:function='sum'/>"
            + "<table:data-pilot-subtotal table:function='count'/>"
            + "</table:data-pilot-subtotals><table:data-pilot-members>"
            + "<table:data-pilot-member table:name='North' table:display='false'/>"
            + "<table:data-pilot-member table:name='South' table:show-details='false'/>"
            + "</table:data-pilot-members><table:data-pilot-display-info "
            + "table:enabled='true' table:data-field='Sales' table:member-count='3' "
            + "table:display-member-mode='from-top'/><table:data-pilot-sort-info "
            + "table:sort-mode='data' table:data-field='Sales' table:order='descending'/>"
            + "<table:data-pilot-layout-info table:layout-mode='outline-subtotals-bottom' "
            + "table:add-empty-lines='true'/></table:data-pilot-level>"
            + "</table:data-pilot-field><table:data-pilot-field "
            + "table:source-field-name='Sales' table:orientation='data' "
            + "table:function='sum'/></table:data-pilot-table>"
            + "</table:data-pilot-tables></office:spreadsheet></office:body>"
            + "</office:document>";
    }

    private static OdfDocument ReadSpreadsheet(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "pivot.fods",
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
        return ReadSpreadsheet(Encoding.UTF8.GetString(bytes));
    }
}
