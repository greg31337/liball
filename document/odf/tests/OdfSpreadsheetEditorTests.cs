using System.Text;

namespace odf.Tests;

public class OdfSpreadsheetEditorTests
{
    [Test]
    public void CreatesRenamesMovesAndDeletesSheetsThroughPublicApi()
    {
        OdfDocument document = ReadSpreadsheet(
            SheetXml("First", 2, 2) + SheetXml("Second", 1, 1));
        IOdfSpreadsheetEditor editor = new OdfSpreadsheetEditor();
        OdfTable[] snapshot = editor.GetSheets(document);
        snapshot[0] = new OdfTable();
        OdfNamedExpression pending = new OdfNamedExpression();
        pending.Name = "Pending";
        pending.IsRange = true;
        pending.RangeAddress = "First.A1:First.B2";
        editor.SetNamedExpressions(document, [pending]);

        OdfTable created = editor.CreateSheet(document, "Third", 2, 3);
        editor.GetSpreadsheetSettings(document).ActiveTable = "Third";
        editor.RenameSheet(document, created, "Renamed");
        OdfTable moved = editor.MoveSheet(document, created, 0);
        editor.DeleteSheet(document, editor.FindSheet(document, "Second")!);
        OdfDocument reread = WriteAndRead(document);
        OdfTable[] sheets = editor.GetSheets(reread);

        Assert.Multiple(() =>
        {
            Assert.That(document.Tables[0].Name, Is.EqualTo("Renamed"));
            Assert.That(moved.Name, Is.EqualTo("Renamed"));
            Assert.That(sheets.Select(item => item.Name),
                Is.EqualTo(new[] { "Renamed", "First" }));
            Assert.That(sheets[0].RowCount, Is.EqualTo(2));
            Assert.That(sheets[0].ColumnCount, Is.EqualTo(3));
            Assert.That(reread.SpreadsheetSettings.ActiveTable,
                Is.EqualTo("Renamed"));
            Assert.That(reread.NamedExpressions, Has.Length.EqualTo(1));
            Assert.That(reread.NamedExpressions[0].Name, Is.EqualTo("Pending"));
        });
    }

    [Test]
    public void RejectsInvalidSheetLifecycleOperations()
    {
        OdfDocument document = ReadSpreadsheet(SheetXml("Only", 1, 1));
        IOdfSpreadsheetEditor editor = new OdfSpreadsheetEditor();
        OdfTable sheet = document.Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(
                () => editor.CreateSheet(document, "Only", 1, 1),
                Throws.TypeOf<OdfEditException>());
            Assert.That(
                () => editor.CreateSheet(document, "New", 0, 1),
                Throws.TypeOf<OdfEditException>());
            Assert.That(
                () => editor.RenameSheet(document, sheet, ""),
                Throws.TypeOf<OdfEditException>());
            Assert.That(
                () => editor.MoveSheet(document, sheet, 1),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void ReplacesSpreadsheetSpecificCollectionsWithSnapshots()
    {
        OdfDocument document = ReadSpreadsheet(SheetXml("Sheet1", 2, 2));
        IOdfSpreadsheetEditor editor = new OdfSpreadsheetEditor();
        OdfNamedExpression named = new OdfNamedExpression();
        OdfLabelRange label = new OdfLabelRange();
        OdfDatabaseRange database = new OdfDatabaseRange();
        OdfConsolidation consolidation = new OdfConsolidation();
        OdfScenario scenario = new OdfScenario();
        OdfContentValidation validation = new OdfContentValidation();
        OdfConditionalStyle conditional = new OdfConditionalStyle();
        OdfPivotTable pivot = new OdfPivotTable();
        OdfDetectiveRange detectiveRange = new OdfDetectiveRange();
        OdfDetectiveOperation detectiveOperation = new OdfDetectiveOperation();
        OdfSpreadsheetChange change = new OdfSpreadsheetChange();

        editor.SetNamedExpressions(document, [named]);
        editor.SetLabelRanges(document, [label]);
        editor.SetDatabaseRanges(document, [database]);
        editor.SetConsolidations(document, [consolidation]);
        editor.SetScenarios(document, [scenario]);
        editor.SetContentValidations(document, [validation]);
        editor.SetConditionalStyles(document, [conditional]);
        editor.SetPivotTables(document, [pivot]);
        editor.SetDetectiveRanges(document, [detectiveRange]);
        editor.SetDetectiveOperations(document, [detectiveOperation]);
        editor.SetSpreadsheetChanges(document, [change]);
        editor.SetSpreadsheetChangeTracking(document, true);

        OdfNamedExpression[] namedSnapshot = editor.GetNamedExpressions(document);
        OdfLabelRange[] labelSnapshot = editor.GetLabelRanges(document);
        OdfDatabaseRange[] databaseSnapshot = editor.GetDatabaseRanges(document);
        OdfConsolidation[] consolidationSnapshot =
            editor.GetConsolidations(document);
        OdfScenario[] scenarioSnapshot = editor.GetScenarios(document);
        OdfContentValidation[] validationSnapshot =
            editor.GetContentValidations(document);
        OdfConditionalStyle[] conditionalSnapshot =
            editor.GetConditionalStyles(document);
        OdfPivotTable[] pivotSnapshot = editor.GetPivotTables(document);
        OdfDetectiveRange[] rangeSnapshot = editor.GetDetectiveRanges(document);
        OdfDetectiveOperation[] operationSnapshot =
            editor.GetDetectiveOperations(document);
        OdfSpreadsheetChange[] changeSnapshot =
            editor.GetSpreadsheetChanges(document);
        namedSnapshot[0] = new OdfNamedExpression();
        labelSnapshot[0] = new OdfLabelRange();
        databaseSnapshot[0] = new OdfDatabaseRange();
        consolidationSnapshot[0] = new OdfConsolidation();
        scenarioSnapshot[0] = new OdfScenario();
        validationSnapshot[0] = new OdfContentValidation();
        conditionalSnapshot[0] = new OdfConditionalStyle();
        pivotSnapshot[0] = new OdfPivotTable();
        rangeSnapshot[0] = new OdfDetectiveRange();
        operationSnapshot[0] = new OdfDetectiveOperation();
        changeSnapshot[0] = new OdfSpreadsheetChange();

        Assert.Multiple(() =>
        {
            Assert.That(document.NamedExpressions[0], Is.SameAs(named));
            Assert.That(document.LabelRanges[0], Is.SameAs(label));
            Assert.That(document.DatabaseRanges[0], Is.SameAs(database));
            Assert.That(document.Consolidations[0], Is.SameAs(consolidation));
            Assert.That(document.Scenarios[0], Is.SameAs(scenario));
            Assert.That(document.ContentValidations[0], Is.SameAs(validation));
            Assert.That(document.ConditionalStyles[0], Is.SameAs(conditional));
            Assert.That(document.PivotTables[0], Is.SameAs(pivot));
            Assert.That(document.DetectiveRanges[0], Is.SameAs(detectiveRange));
            Assert.That(document.DetectiveOperations[0],
                Is.SameAs(detectiveOperation));
            Assert.That(document.SpreadsheetChanges[0], Is.SameAs(change));
            Assert.That(document.TrackSpreadsheetChanges, Is.True);
            Assert.That(editor.GetSpreadsheetSettings(document),
                Is.SameAs(document.SpreadsheetSettings));
        });
    }

    [Test]
    public void PersistsSpreadsheetDeclarationsConfiguredThroughPublicApi()
    {
        OdfDocument document = ReadSpreadsheet(SheetXml("Sheet1", 3, 3));
        IOdfSpreadsheetEditor editor = new OdfSpreadsheetEditor();
        OdfNamedExpression named = new OdfNamedExpression();
        named.Name = "Data";
        named.IsRange = true;
        named.RangeAddress = "Sheet1.A1:Sheet1.C3";
        OdfContentValidation validation = new OdfContentValidation();
        validation.Name = "Positive";
        validation.Condition = "of:cell-content()>0";
        OdfPivotField field = new OdfPivotField();
        field.SourceFieldName = "Value";
        field.Orientation = "row";
        OdfPivotTable pivot = new OdfPivotTable();
        pivot.Name = "Summary";
        pivot.TargetRangeAddress = "Sheet1.E1:Sheet1.G4";
        pivot.SourceKind = "cell-range";
        pivot.SourceRangeAddress = "Sheet1.A1:Sheet1.C3";
        pivot.Fields = [field];
        OdfSpreadsheetSettings settings = editor.GetSpreadsheetSettings(document);
        settings.HasCalculationSettings = true;
        settings.CaseSensitive = false;
        settings.ActiveTable = "Sheet1";

        editor.SetNamedExpressions(document, [named]);
        editor.SetContentValidations(document, [validation]);
        editor.SetPivotTables(document, [pivot]);
        editor.SetSpreadsheetChangeTracking(document, true);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.NamedExpressions[0].Name, Is.EqualTo("Data"));
            Assert.That(reread.ContentValidations[0].Name,
                Is.EqualTo("Positive"));
            Assert.That(reread.PivotTables[0].Name, Is.EqualTo("Summary"));
            Assert.That(reread.PivotTables[0].Fields[0].SourceFieldName,
                Is.EqualTo("Value"));
            Assert.That(reread.SpreadsheetSettings.CaseSensitive, Is.False);
            Assert.That(reread.SpreadsheetSettings.ActiveTable,
                Is.EqualTo("Sheet1"));
            Assert.That(reread.TrackSpreadsheetChanges, Is.True);
        });
    }

    [Test]
    public void KeepsFormulaReferencesValidAcrossSheetLifecycleChanges()
    {
        OdfDocument document = ReadSpreadsheet(
            SheetXml("Old Sheet", 1, 1)
            + "<table:table table:name='Calc' "
            + "table:print-ranges=\"'Old Sheet'.A1:'Old Sheet'.B2\">"
            + "<table:table-row><table:table-cell "
            + "table:formula=\"of:=['Old Sheet'.A1]"
            + "+'Old Sheet'.$$Local"
            + "+['book.ods'#'Old Sheet'.A1]\"/>"
            + "</table:table-row></table:table>");
        IOdfSpreadsheetEditor editor = new OdfSpreadsheetEditor();
        OdfTable oldSheet = editor.FindSheet(document, "Old Sheet")!;

        editor.RenameSheet(document, oldSheet, "New Sheet");
        OdfTable calc = editor.FindSheet(document, "Calc")!;
        string renamedFormula =
            TableDocumentModel.GetCell(calc, 0, 0)!.Formula;
        string renamedRange = calc.PrintRangeText;
        OdfTable inserted = editor.CreateSheet(document, "Inserted", 1, 1);
        editor.MoveSheet(document, inserted, 0);
        string reorderedFormula = TableDocumentModel.GetCell(
            editor.FindSheet(document, "Calc")!,
            0,
            0)!.Formula;
        editor.DeleteSheet(
            document,
            editor.FindSheet(document, "New Sheet")!);
        OdfDocument reread = WriteAndRead(document);
        OdfTable rereadCalc = editor.FindSheet(reread, "Calc")!;

        Assert.Multiple(() =>
        {
            Assert.That(renamedFormula, Is.EqualTo(
                "of:=['New Sheet'.A1]"
                + "+'New Sheet'.$$Local"
                + "+['book.ods'#'Old Sheet'.A1]"));
            Assert.That(renamedRange,
                Is.EqualTo("'New Sheet'.A1:'New Sheet'.B2"));
            Assert.That(reorderedFormula, Is.EqualTo(renamedFormula));
            Assert.That(
                TableDocumentModel.GetCell(rereadCalc, 0, 0)!.Formula,
                Is.EqualTo(
                    "of:=#REF!+#REF!+['book.ods'#'Old Sheet'.A1]"));
            Assert.That(rereadCalc.PrintRangeText, Is.Empty);
        });
    }

    [Test]
    public void RoundTripsNamedDatabaseFilterSortSubtotalAndScenarioData()
    {
        OdfDocument document = ReadSpreadsheet(SpreadsheetDataXml());
        IOdfSpreadsheetEditor editor = new OdfSpreadsheetEditor();
        OdfNamedExpression[] named = editor.GetNamedExpressions(document);
        OdfLabelRange[] labels = editor.GetLabelRanges(document);
        OdfDatabaseRange[] databases = editor.GetDatabaseRanges(document);
        OdfConsolidation[] consolidations = editor.GetConsolidations(document);
        OdfScenario[] scenarios = editor.GetScenarios(document);
        OdfDatabaseRange database = databases[0];

        Assert.Multiple(() =>
        {
            Assert.That(named, Has.Length.EqualTo(2));
            Assert.That(named[0].IsRange, Is.True);
            Assert.That(named[0].RangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C4"));
            Assert.That(named[1].Expression,
                Is.EqualTo("of:=[Sheet1.C4]*0.2"));
            Assert.That(labels[0].Orientation, Is.EqualTo("row"));
            Assert.That(database.Name, Is.EqualTo("SalesDb"));
            Assert.That(database.SortCaseSensitive, Is.True);
            Assert.That(database.SortRules, Has.Length.EqualTo(2));
            Assert.That(database.SortRules[1].Order, Is.EqualTo("descending"));
            Assert.That(database.FilterRules, Has.Length.EqualTo(2));
            Assert.That(database.FilterRules[0].Connector, Is.EqualTo("and"));
            Assert.That(database.FilterRules[1].Operator, Is.EqualTo(">"));
            Assert.That(database.SubtotalRules[0].Fields[0].Function,
                Is.EqualTo("sum"));
            Assert.That(consolidations[0].Function, Is.EqualTo("average"));
            Assert.That(scenarios[0].IsActive, Is.True);
            Assert.That(scenarios[0].CopyFormulas, Is.True);
        });

        named[0].Name = "RenamedData";
        labels[0].Orientation = "column";
        database.DisplayFilterButtons = false;
        database.SortRules[0].Order = "descending";
        database.FilterRules[0].Value = "West";
        database.SubtotalRules[0].Fields[0].Function = "average";
        consolidations[0].UseLabel = "both";
        scenarios[0].Comment = "Updated scenario";
        editor.SetNamedExpressions(document, named);
        editor.SetLabelRanges(document, labels);
        editor.SetDatabaseRanges(document, databases);
        editor.SetConsolidations(document, consolidations);
        editor.SetScenarios(document, scenarios);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.NamedExpressions[0].Name,
                Is.EqualTo("RenamedData"));
            Assert.That(reread.LabelRanges[0].Orientation,
                Is.EqualTo("column"));
            Assert.That(reread.DatabaseRanges[0].DisplayFilterButtons,
                Is.False);
            Assert.That(reread.DatabaseRanges[0].SortRules[0].Order,
                Is.EqualTo("descending"));
            Assert.That(reread.DatabaseRanges[0].FilterRules[0].Value,
                Is.EqualTo("West"));
            Assert.That(reread.DatabaseRanges[0].SubtotalRules[0]
                .Fields[0].Function, Is.EqualTo("average"));
            Assert.That(reread.Consolidations[0].UseLabel, Is.EqualTo("both"));
            Assert.That(reread.Scenarios[0].Comment,
                Is.EqualTo("Updated scenario"));
        });
    }

    [Test]
    public void RoundTripsValidationConditionalFormattingAndPivotData()
    {
        OdfDocument document = ReadXml(FormattingAndPivotXml());
        IOdfSpreadsheetEditor editor = new OdfSpreadsheetEditor();
        OdfContentValidation[] validations =
            editor.GetContentValidations(document);
        OdfConditionalStyle[] conditionals =
            editor.GetConditionalStyles(document);
        OdfPivotTable[] pivots = editor.GetPivotTables(document);
        OdfContentValidation validation = validations[0];
        OdfConditionalStyle conditional = conditionals[0];
        OdfPivotTable pivot = pivots[0];
        OdfPivotLevel level = pivot.Fields[0].Level!;

        Assert.Multiple(() =>
        {
            Assert.That(validation.Name, Is.EqualTo("Positive"));
            Assert.That(validation.DisplayHelp, Is.True);
            Assert.That(validation.HelpText,
                Is.EqualTo("Enter a positive value."));
            Assert.That(validation.DisplayError, Is.True);
            Assert.That(validation.ErrorMessageType, Is.EqualTo("warning"));
            Assert.That(document.Tables[0].Rows[0].Cells[0].Validation,
                Is.SameAs(validation));
            Assert.That(conditional.OwnerStyleName, Is.EqualTo("InputCell"));
            Assert.That(conditional.Condition, Is.EqualTo("cell-content()<0"));
            Assert.That(conditional.ApplyStyleName, Is.EqualTo("Negative"));
            Assert.That(pivot.SourceRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C5"));
            Assert.That(pivot.FilterRules[1].Operator, Is.EqualTo(">"));
            Assert.That(pivot.Fields, Has.Length.EqualTo(2));
            Assert.That(level.Subtotals, Has.Length.EqualTo(2));
            Assert.That(level.Members[0].Name, Is.EqualTo("North"));
            Assert.That(level.SortOrder, Is.EqualTo("descending"));
        });

        validation.HelpTitle = "Updated input";
        validation.ErrorMessageType = "information";
        conditional.Condition = "cell-content()>100";
        conditional.ApplyStyleName = "High";
        pivot.FilterRules[0].Value = "West";
        level.Subtotals[0].Function = "average";
        level.SortMode = "name";
        level.SortDataField = "";
        editor.SetContentValidations(document, validations);
        editor.SetConditionalStyles(document, conditionals);
        editor.SetPivotTables(document, pivots);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.ContentValidations[0].HelpTitle,
                Is.EqualTo("Updated input"));
            Assert.That(reread.ContentValidations[0].ErrorMessageType,
                Is.EqualTo("information"));
            Assert.That(reread.ConditionalStyles[0].Condition,
                Is.EqualTo("cell-content()>100"));
            Assert.That(reread.ConditionalStyles[0].ApplyStyleName,
                Is.EqualTo("High"));
            Assert.That(reread.PivotTables[0].FilterRules[0].Value,
                Is.EqualTo("West"));
            Assert.That(reread.PivotTables[0].Fields[0].Level!
                .Subtotals[0].Function, Is.EqualTo("average"));
            Assert.That(reread.PivotTables[0].Fields[0].Level!.SortMode,
                Is.EqualTo("name"));
        });
    }

    [Test]
    public void RoundTripsDetectiveChangesAndSpreadsheetSettings()
    {
        OdfDocument document = ReadXml(DetectiveSettingsAndChangesXml());
        IOdfSpreadsheetEditor editor = new OdfSpreadsheetEditor();
        OdfDetectiveRange[] ranges = editor.GetDetectiveRanges(document);
        OdfDetectiveOperation[] operations =
            editor.GetDetectiveOperations(document);
        OdfSpreadsheetChange[] changes =
            editor.GetSpreadsheetChanges(document);
        OdfSpreadsheetSettings settings =
            editor.GetSpreadsheetSettings(document);

        Assert.Multiple(() =>
        {
            Assert.That(ranges[0].RangeAddress,
                Is.EqualTo("Sheet1.A2:Sheet1.B4"));
            Assert.That(ranges[0].ContainsError, Is.True);
            Assert.That(operations[0].Name, Is.EqualTo("trace-precedents"));
            Assert.That(operations[0].Index, Is.EqualTo(2));
            Assert.That(changes, Has.Length.EqualTo(2));
            Assert.That(changes[0].Kind, Is.EqualTo("cell-content"));
            Assert.That(changes[1].Kind, Is.EqualTo("movement"));
            Assert.That(document.TrackSpreadsheetChanges, Is.True);
            Assert.That(settings.CaseSensitive, Is.False);
            Assert.That(settings.PrecisionAsShown, Is.True);
            Assert.That(settings.UseWildcards, Is.True);
            Assert.That(settings.NullYear, Is.EqualTo(1950));
            Assert.That(settings.NullDate, Is.EqualTo("1904-01-01"));
            Assert.That(settings.IterationEnabled, Is.True);
            Assert.That(settings.IterationSteps, Is.EqualTo(250));
            Assert.That(settings.MaximumDifference, Is.EqualTo(0.0001));
            Assert.That(settings.ActiveTable, Is.EqualTo("Sheet1"));
            Assert.That(settings.SelectedRanges,
                Is.EqualTo("Sheet1.A2:Sheet1.C5"));
        });

        ranges[0].Direction = "to-another-table";
        ranges[0].ContainsError = false;
        operations[0].Name = "trace-dependents";
        operations[0].Index = 5;
        changes[0].AcceptanceState = "accepted";
        changes[1].TargetEndRow = 4;
        settings.CaseSensitive = true;
        settings.PrecisionAsShown = false;
        settings.UseWildcards = false;
        settings.UseRegularExpressions = true;
        settings.NullYear = 1940;
        settings.IterationSteps = 75;
        settings.ActiveTable = "Sheet1";
        settings.SelectedRanges = "Sheet1.B3:Sheet1.D8";
        editor.SetDetectiveRanges(document, ranges);
        editor.SetDetectiveOperations(document, operations);
        editor.SetSpreadsheetChanges(document, changes);
        editor.SetSpreadsheetChangeTracking(document, false);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.DetectiveRanges[0].Direction,
                Is.EqualTo("to-another-table"));
            Assert.That(reread.DetectiveRanges[0].ContainsError, Is.False);
            Assert.That(reread.DetectiveOperations[0].Name,
                Is.EqualTo("trace-dependents"));
            Assert.That(reread.DetectiveOperations[0].Index, Is.EqualTo(5));
            Assert.That(reread.SpreadsheetChanges[0].AcceptanceState,
                Is.EqualTo("accepted"));
            Assert.That(reread.SpreadsheetChanges[1].TargetEndRow,
                Is.EqualTo(4));
            Assert.That(reread.TrackSpreadsheetChanges, Is.False);
            Assert.That(reread.SpreadsheetSettings.CaseSensitive, Is.True);
            Assert.That(reread.SpreadsheetSettings.PrecisionAsShown, Is.False);
            Assert.That(reread.SpreadsheetSettings.UseRegularExpressions,
                Is.True);
            Assert.That(reread.SpreadsheetSettings.IterationSteps,
                Is.EqualTo(75));
            Assert.That(reread.SpreadsheetSettings.SelectedRanges,
                Is.EqualTo("Sheet1.B3:Sheet1.D8"));
        });
    }

    private static string SpreadsheetDataXml()
    {
        return "<table:table table:name='Sheet1'>"
            + "<table:scenario table:scenario-ranges='Sheet1.A2:Sheet1.C3' "
            + "table:comment='Baseline' table:is-active='true' "
            + "table:copy-formulas='true'/><table:table-row "
            + "table:number-rows-repeated='4'><table:table-cell "
            + "table:number-columns-repeated='7'/></table:table-row>"
            + "</table:table><table:named-expressions>"
            + "<table:named-range table:name='Data' "
            + "table:base-cell-address='Sheet1.A1' "
            + "table:cell-range-address='Sheet1.A1:Sheet1.C4'/>"
            + "<table:named-expression table:name='Tax' "
            + "table:base-cell-address='Sheet1.C4' "
            + "table:expression='of:=[Sheet1.C4]*0.2'/>"
            + "</table:named-expressions><table:label-ranges>"
            + "<table:label-range table:label-cell-range-address='Sheet1.A1:Sheet1.C1' "
            + "table:data-cell-range-address='Sheet1.A2:Sheet1.C4' "
            + "table:orientation='row'/></table:label-ranges>"
            + "<table:database-ranges><table:database-range table:name='SalesDb' "
            + "table:target-range-address='Sheet1.A1:Sheet1.C4' "
            + "table:display-filter-buttons='true'>"
            + "<table:sort table:case-sensitive='true'>"
            + "<table:sort-by table:field-number='1' table:data-type='text'/>"
            + "<table:sort-by table:field-number='2' table:data-type='number' "
            + "table:order='descending'/></table:sort>"
            + "<table:filter><table:filter-and>"
            + "<table:filter-condition table:field-number='0' table:operator='=' "
            + "table:value='North'/><table:filter-condition "
            + "table:field-number='2' table:operator='&gt;' table:value='100' "
            + "table:data-type='number'/></table:filter-and></table:filter>"
            + "<table:subtotal-rules><table:subtotal-rule "
            + "table:group-by-field-number='0'><table:subtotal-field "
            + "table:field-number='2' table:function='sum'/></table:subtotal-rule>"
            + "</table:subtotal-rules></table:database-range></table:database-ranges>"
            + "<table:consolidation table:function='average' "
            + "table:source-cell-range-addresses='Sheet1.A1:Sheet1.C4' "
            + "table:target-cell-address='Sheet1.A6' table:use-label='row'/>";
    }

    private static string FormattingAndPivotXml()
    {
        return DocumentStart()
            + "<office:automatic-styles>"
            + "<style:style style:name='InputCell' style:family='table-cell'>"
            + "<style:map style:condition='cell-content()&lt;0' "
            + "style:apply-style-name='Negative' "
            + "style:base-cell-address='Sheet1.A2'/></style:style>"
            + "<style:style style:name='Negative' style:family='table-cell'/>"
            + "<style:style style:name='High' style:family='table-cell'/>"
            + "</office:automatic-styles><office:body><office:spreadsheet>"
            + "<table:content-validations><table:content-validation "
            + "table:name='Positive' table:condition='of:cell-content()&gt;0' "
            + "table:base-cell-address='Sheet1.A2' table:allow-empty-cell='false'>"
            + "<table:help-message table:title='Input' table:display='true'>"
            + "<text:p>Enter a positive value.</text:p></table:help-message>"
            + "<table:error-message table:title='Invalid' table:display='true' "
            + "table:message-type='warning'><text:p>Value is invalid.</text:p>"
            + "</table:error-message></table:content-validation>"
            + "</table:content-validations><table:table table:name='Sheet1'>"
            + "<table:table-row table:number-rows-repeated='12'>"
            + "<table:table-cell table:number-columns-repeated='12' "
            + "table:style-name='InputCell' "
            + "table:content-validation-name='Positive'/></table:table-row>"
            + "</table:table><table:data-pilot-tables>"
            + "<table:data-pilot-table table:name='SalesPivot' "
            + "table:target-range-address='Sheet1.D7:Sheet1.G12'>"
            + "<table:source-cell-range table:cell-range-address='Sheet1.A1:Sheet1.C5'>"
            + "<table:filter><table:filter-and>"
            + "<table:filter-condition table:field-number='0' table:operator='=' "
            + "table:value='North'/><table:filter-condition "
            + "table:field-number='2' table:operator='&gt;' table:value='100'/>"
            + "</table:filter-and></table:filter></table:source-cell-range>"
            + "<table:data-pilot-field table:source-field-name='Region' "
            + "table:orientation='row'><table:data-pilot-level>"
            + "<table:data-pilot-subtotals><table:data-pilot-subtotal "
            + "table:function='sum'/><table:data-pilot-subtotal "
            + "table:function='count'/></table:data-pilot-subtotals>"
            + "<table:data-pilot-members><table:data-pilot-member "
            + "table:name='North'/></table:data-pilot-members>"
            + "<table:data-pilot-sort-info table:sort-mode='data' "
            + "table:data-field='Sales' table:order='descending'/>"
            + "</table:data-pilot-level></table:data-pilot-field>"
            + "<table:data-pilot-field table:source-field-name='Sales' "
            + "table:orientation='data' table:function='sum'/>"
            + "</table:data-pilot-table></table:data-pilot-tables>"
            + "</office:spreadsheet></office:body></office:document>";
    }

    private static string DetectiveSettingsAndChangesXml()
    {
        string changeInfo = "<office:change-info><dc:creator>Alice</dc:creator>"
            + "<dc:date>2026-09-01T12:00:00</dc:date></office:change-info>";
        return DocumentStart()
            + "<office:settings><config:config-item-set "
            + "config:name='ooo:view-settings'><config:config-item-map-indexed "
            + "config:name='Views'><config:config-item-map-entry>"
            + "<config:config-item config:name='ActiveTable' "
            + "config:type='string'>Sheet1</config:config-item>"
            + "<config:config-item config:name='SelectedTables' "
            + "config:type='string'>Sheet1</config:config-item>"
            + "<config:config-item config:name='SelectedRanges' "
            + "config:type='string'>Sheet1.A2:Sheet1.C5</config:config-item>"
            + "</config:config-item-map-entry></config:config-item-map-indexed>"
            + "</config:config-item-set></office:settings>"
            + "<office:body><office:spreadsheet><table:calculation-settings "
            + "table:case-sensitive='false' table:precision-as-shown='true' "
            + "table:use-regular-expressions='false' table:use-wildcards='true' "
            + "table:null-year='1950'><table:null-date table:value-type='date' "
            + "table:date-value='1904-01-01'/><table:iteration "
            + "table:status='enable' table:steps='250' "
            + "table:maximum-difference='0.0001'/></table:calculation-settings>"
            + "<table:tracked-changes table:track-changes='true'>"
            + "<table:cell-content-change table:id='c1'>"
            + "<table:cell-address table:column='0' table:row='1' table:table='0'/>"
            + changeInfo + "<table:previous><table:table-cell/></table:previous>"
            + "</table:cell-content-change><table:movement table:id='c2'>"
            + "<table:source-range-address table:start-column='0' "
            + "table:start-row='0' table:start-table='0' table:end-column='1' "
            + "table:end-row='3' table:end-table='0'/>"
            + "<table:target-range-address table:start-column='2' "
            + "table:start-row='1' table:start-table='0' table:end-column='3' "
            + "table:end-row='2' table:end-table='0'/>" + changeInfo
            + "</table:movement></table:tracked-changes>"
            + "<table:table table:name='Sheet1'><table:table-row "
            + "table:number-rows-repeated='5'><table:table-cell "
            + "table:number-columns-repeated='5'><table:detective>"
            + "<table:highlighted-range "
            + "table:cell-range-address='Sheet1.A2:Sheet1.B4' "
            + "table:direction='from-same-table' table:contains-error='true'/>"
            + "<table:operation table:name='trace-precedents' table:index='2'/>"
            + "</table:detective></table:table-cell></table:table-row>"
            + "</table:table></office:spreadsheet></office:body>"
            + "</office:document>";
    }

    private static string DocumentStart()
    {
        return "<?xml version='1.0' encoding='UTF-8'?><office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:dc='" + OdfNamespaces.DublinCore + "'"
            + " xmlns:config='" + OdfNamespaces.Config + "'"
            + " xmlns:of='" + OdfNamespaces.OpenFormula + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>";
    }

    private static string SheetXml(
        string name,
        long rowCount,
        long columnCount)
    {
        return "<table:table table:name='" + name + "'>"
            + "<table:table-column table:number-columns-repeated='"
            + columnCount + "'/>"
            + "<table:table-row table:number-rows-repeated='" + rowCount + "'>"
            + "<table:table-cell table:number-columns-repeated='"
            + columnCount + "'/></table:table-row></table:table>";
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
            "spreadsheet-editor.fods",
            new OdfLoadOptions());
    }

    private static OdfDocument ReadXml(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "spreadsheet-editor.fods",
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
            "spreadsheet-editor.fods",
            new OdfLoadOptions());
    }
}
