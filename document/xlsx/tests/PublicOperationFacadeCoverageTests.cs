using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PublicOperationFacadeCoverageTests
{
    [Test]
    public void ReferenceConverterFacades_ExerciseStringSpanLeadingFormatAndBoundaryPaths()
    {
        IXlsxA1ColumnNameConverterOperations columns = new XlsxA1ColumnNameConverterOperations();
        XlsxA1ColumnNameResult columnString = columns.ParseString("XFD");
        XlsxA1ColumnNameResult columnSpan = columns.ParseSpan("AA".AsSpan());
        XlsxA1ColumnNameResult columnLeading = columns.ParseLeading("B12".AsSpan());
        XlsxA1ColumnNameResult columnFormat = columns.Format(16384);
        char[] columnBuffer = new char[3];
        XlsxA1ColumnNameResult columnSpanFormat = columns.FormatToSpan(703, columnBuffer.AsSpan());

        Assert.That(columnString.Column, Is.EqualTo(16384));
        Assert.That(columnSpan.Column, Is.EqualTo(27));
        Assert.That(columnLeading.CharactersConsumed, Is.EqualTo(1));
        Assert.That(columnFormat.Text, Is.EqualTo("XFD"));
        Assert.That(columnSpanFormat.IsValid, Is.True);
        Assert.That(columns.FormatString(28), Is.EqualTo("AB"));

        IXlsxA1CellReferenceConverterOperations cells = new XlsxA1CellReferenceConverterOperations();
        XlsxA1CellReferenceResult cellString = cells.ParseString("$C$12");
        XlsxA1CellReferenceResult cellSpan = cells.ParseSpan("D9".AsSpan());
        XlsxA1CellReferenceResult cellLeading = cells.ParseLeading("E5+1".AsSpan());
        XlsxA1CellReferenceResult cellValue = cells.FormatValue(cellString.Reference);
        XlsxA1CellReferenceResult cellCoordinates = cells.FormatCoordinates(7, 2, true, false);
        char[] cellBuffer = new char[8];
        XlsxA1CellReferenceResult cellSpanFormat = cells.FormatToSpan(1048576, 16384, false, false, cellBuffer.AsSpan());
        XlsxA1CellReferenceResult nullCell = cells.ParseString(null);

        Assert.That(cellString.Row, Is.EqualTo(12));
        Assert.That(cellSpan.Column, Is.EqualTo(4));
        Assert.That(cellLeading.CharactersConsumed, Is.EqualTo(2));
        Assert.That(cellValue.Text, Is.EqualTo("$C$12"));
        Assert.That(cellCoordinates.Text, Is.EqualTo("B$7"));
        Assert.That(cellSpanFormat.IsValid, Is.False);
        Assert.That(cellSpanFormat.BufferTooSmall, Is.True);
        Assert.That(cells.FormatString(3, 1, false, true), Is.EqualTo("$A3"));
        Assert.That(nullCell.IsValid, Is.False);

        IXlsxA1RangeReferenceConverterOperations ranges = new XlsxA1RangeReferenceConverterOperations();
        XlsxA1RangeReferenceResult rangeString = ranges.ParseString("A1:$B$2");
        XlsxA1RangeReferenceResult rangeSpan = ranges.ParseSpan("C3:D4".AsSpan());
        XlsxA1RangeReferenceResult rangeLeading = ranges.ParseLeading("E5:F6,other".AsSpan());
        XlsxA1RangeReferenceResult rangeFormat = ranges.Format(rangeString.Range);
        char[] rangeBuffer = new char[16];
        XlsxA1RangeReferenceResult rangeSpanFormat = ranges.FormatToSpan(rangeSpan.Range, rangeBuffer.AsSpan());

        Assert.That(rangeString.HasLast, Is.True);
        Assert.That(rangeSpan.Last.Row, Is.EqualTo(4));
        Assert.That(rangeLeading.CharactersConsumed, Is.EqualTo(5));
        Assert.That(rangeFormat.Text, Is.EqualTo("A1:$B$2"));
        Assert.That(rangeSpanFormat.IsValid, Is.True);
        Assert.That(ranges.FormatString(rangeSpan.Range), Is.EqualTo("C3:D4"));

        IXlsxR1C1ReferenceConverterOperations r1c1 = new XlsxR1C1ReferenceConverterOperations();
        XlsxR1C1ReferenceResult r1c1String = r1c1.ParseString("R1C1");
        XlsxR1C1ReferenceResult r1c1Span = r1c1.ParseSpan("R[-2]C[3]".AsSpan());
        XlsxR1C1ReferenceResult r1c1Leading = r1c1.ParseLeading("RC+1".AsSpan());
        XlsxR1C1ReferenceResult r1c1Format = r1c1.Format(r1c1Span.Reference);
        char[] r1c1Buffer = new char[12];
        XlsxR1C1ReferenceResult r1c1SpanFormat = r1c1.FormatToSpan(r1c1String.Reference, r1c1Buffer.AsSpan());
        char[] integerBuffer = new char[5];
        int integerEnd = r1c1.WriteUnsignedInteger(42, integerBuffer.AsSpan(), 1);

        Assert.That(r1c1String.IsValid, Is.True);
        Assert.That(r1c1Span.IsRelativeRow, Is.True);
        Assert.That(r1c1Leading.CharactersConsumed, Is.EqualTo(2));
        Assert.That(r1c1Format.Text, Is.EqualTo("R[-2]C[3]"));
        Assert.That(r1c1SpanFormat.Text, Is.EqualTo("R1C1"));
        Assert.That(r1c1.FormatString(r1c1String.Reference), Is.EqualTo("R1C1"));
        Assert.That(integerEnd, Is.EqualTo(3));
        Assert.That(new string(integerBuffer, 1, 2), Is.EqualTo("42"));
    }

    [Test]
    public void ValueFormattingFacades_ExerciseDatesFormatsInlineTextAndCalculationValues()
    {
        IXlsxDateSerialConverterOperations dates = new XlsxDateSerialConverterOperations();
        XlsxDateSerialConversionResult from1900 = dates.FromSerial(2, false);
        XlsxDateSerialConversionResult from1904 = dates.FromSerial(0, true);
        XlsxDateSerialConversionResult to1900 = dates.ToSerial(new DateTime(1900, 1, 2), false);
        XlsxDateSerialConversionResult invalid = dates.FromSerial(double.NaN, false);

        Assert.That(from1900.IsValid, Is.True);
        Assert.That(from1904.DateTimeValue, Is.EqualTo(new DateTime(1904, 1, 1)));
        Assert.That(to1900.SerialValue, Is.EqualTo(2));
        Assert.That(invalid.IsValid, Is.False);

        IXlsxNumberFormatParserOperations formats = new XlsxNumberFormatParserOperations();
        XlsxNumberFormatParseResult builtIn = formats.ParseBuiltIn(14);
        XlsxNumberFormatParseResult custom = formats.ParseCustom("#,##0.00");
        XlsxNumberFormatParseResult malformed = formats.ParseCustom("[");
        Assert.That(builtIn.IsValid, Is.True);
        Assert.That(custom.IsValid, Is.True);
        Assert.That(malformed.IsValid, Is.False);

        IXlsxFormattedTextRendererOperations renderer = new XlsxFormattedTextRendererOperations();
        XlsxFormattedTextRenderResult rendered = renderer.RenderCellValue(XlsxCellValue.CreateNumber(1234.5), "#,##0.00", false);
        Assert.That(rendered.IsValid, Is.True);
        Assert.That(rendered.Text, Is.EqualTo("1,234.50"));

        IXlsxInlineStringOperations inline = new XlsxInlineStringOperations();
        XlsxInlineString inlineText = inline.CreateText("hello");
        Assert.That(inlineText.Text, Is.EqualTo("hello"));

        IXlsxFormulaCalculationValueOperations calculationValues = new XlsxFormulaCalculationValueOperations();
        XlsxFormulaCalculationValue blank = calculationValues.CreateBlank();
        XlsxFormulaCalculationValue number = calculationValues.CreateNumber(12.5);
        XlsxFormulaCalculationValue text = calculationValues.CreateText("value");
        XlsxFormulaCalculationValue boolean = calculationValues.CreateBoolean(true);
        XlsxFormulaCalculationValue error = calculationValues.CreateError("#N/A");
        Assert.That(blank.Kind, Is.EqualTo(XlsxFormulaCalculationValueKind.Blank));
        Assert.That(number.Number, Is.EqualTo(12.5));
        Assert.That(text.Text, Is.EqualTo("value"));
        Assert.That(boolean.Boolean, Is.True);
        Assert.That(error.Error, Is.EqualTo("#N/A"));
    }

    [Test]
    public void DiagnosticAndResultFacades_MutateStateAndReportBoundaries()
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "audit", "message");
        IXlsxDiagnosticOperations diagnostics = new XlsxDiagnosticOperations();
        diagnostics.SetLocation(diagnostic, "/xl/workbook.xml", "sheet", "name");
        diagnostics.SetCellLocation(diagnostic, 3, 4);
        diagnostics.SetRelationshipId(diagnostic, "rId1");
        Assert.That(diagnostic.PartPath, Is.EqualTo("/xl/workbook.xml"));
        Assert.That(diagnostic.Row, Is.EqualTo(3));
        Assert.That(diagnostic.RelationshipId, Is.EqualTo("rId1"));

        XlsxValidationResult validation = new XlsxValidationResult();
        IXlsxValidationResultOperations validationOperations = new XlsxValidationResultOperations();
        validationOperations.AddInfo(validation, "info", "information");
        validationOperations.AddWarning(validation, "warning", "warning");
        Assert.That(validationOperations.CountSeverity(validation, XlsxDiagnosticSeverity.Warning), Is.EqualTo(1));
        Assert.That(validationOperations.HasErrors(validation), Is.False);

        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.MaxRows = 0;
        XlsxWriterOptions writerOptions = new XlsxWriterOptions();
        writerOptions.MaxZipEntries = 0;
        Assert.That(new XlsxReaderOptionsOperations().Validate(readerOptions).IsValid, Is.False);
        Assert.That(new XlsxWriterOptionsOperations().Validate(writerOptions).IsValid, Is.False);

        XlsxPerformanceCounters counters = new XlsxPerformanceCounters();
        IXlsxPerformanceCountersOperations counterOperations = new XlsxPerformanceCountersOperations();
        counterOperations.Reset(counters, "audit");
        counterOperations.Finish(counters, Environment.TickCount64 - 5);
        Assert.That(counters.Operation, Is.EqualTo("audit"));
        Assert.That(counters.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));

        XlsxSpreadsheetColorResolutionResult color = new XlsxSpreadsheetColorResolutionResult();
        new XlsxSpreadsheetColorResolutionResultOperations().AddError(color, "color.invalid", "invalid");
        Assert.That(color.IsValid, Is.False);

        XlsxWorkbookCompareResult comparison = new XlsxWorkbookCompareResult();
        new XlsxWorkbookCompareResultOperations().AddDifference(comparison, "difference", "/", "changed", "left", "right");
        Assert.That(comparison.DifferenceCount, Is.EqualTo(1));
        Assert.That(comparison.AreEqual, Is.False);

        XlsxWorkbookRepairResult repair = new XlsxWorkbookRepairResult();
        new XlsxWorkbookRepairResultOperations().AddRepair(repair, "repair", "repaired");
        Assert.That(repair.RepairCount, Is.EqualTo(1));

        XlsxWorkbookSanitizeResult sanitize = new XlsxWorkbookSanitizeResult();
        new XlsxWorkbookSanitizeResultOperations().AddWarning(sanitize, "sanitize", "warning");
        Assert.That(sanitize.Validation.CountSeverity(XlsxDiagnosticSeverity.Warning), Is.EqualTo(1));

        XlsxWorksheetMergeCellsResult merge = new XlsxWorksheetMergeCellsResult();
        new XlsxWorksheetMergeCellsResultOperations().SetRange(merge, 2, 3, 4, 5);
        Assert.That(merge.Reference, Is.EqualTo("C2:E4"));

        XlsxWorksheetUsedRangeResult used = new XlsxWorksheetUsedRangeResult();
        IXlsxWorksheetUsedRangeResultOperations usedOperations = new XlsxWorksheetUsedRangeResultOperations();
        usedOperations.SetBounds(used, 4, 5);
        usedOperations.SetBounds(used, 2, 3);
        usedOperations.UpdateRangeReference(used);
        Assert.That(used.Reference, Is.EqualTo("C2:E4"));
    }

    [Test]
    public void CellSharedStringAndRangeFacades_ExerciseRemainingTypedAndBoundaryOperations()
    {
        IXlsxCellValueOperations values = new XlsxCellValueOperations();
        XlsxCellValue duration = values.CreateDuration(TimeSpan.FromMinutes(90));
        XlsxCellValue formula = values.CreateFormulaValue("1+1", values.CreateNumber(2));
        XlsxInlineString inline = new XlsxInlineString();
        inline.Text = "rich";
        XlsxCellValue rich = values.CreateRichTextValue(inline);
        Assert.That(duration.Kind, Is.EqualTo(XlsxCellValueKind.Duration));
        Assert.That(formula.Kind, Is.EqualTo(XlsxCellValueKind.FormulaCachedValue));
        Assert.That(rich.Text, Is.EqualTo("rich"));

        XlsxSharedStringTablePart table = new XlsxSharedStringTablePart();
        IXlsxSharedStringTablePartOperations strings = new XlsxSharedStringTablePartOperations();
        int index = strings.AddTextReference(table, "value");
        Assert.That(strings.GetItem(table, index), Is.Not.Null);
        strings.RecalculateCountsFromReferences(table);
        Assert.That(table.Count, Is.EqualTo(1));

        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        IXlsxWorksheetRangeOperations ranges = new XlsxWorksheetRangeOperations();
        Assert.That(ranges.FillBoolean(sheet.GetRange("A1:B1"), true).IsValid, Is.True);
        Assert.That(ranges.FillLinearSeries(sheet.GetRange("A2:B2"), 2, 3).IsValid, Is.True);
        Assert.That(sheet.GetCell("B2")!.Value, Is.EqualTo("5"));
    }

    [Test]
    public void StyleAndDrawingFacades_ExerciseDerivedStateAndLowLevelAuthoring()
    {
        XlsxStyleBuilder builder = new XlsxStyleBuilder();
        IXlsxStyleBuilderOperations styles = new XlsxStyleBuilderOperations();
        styles.SetFontName(builder, "Aptos");
        styles.SetBold(builder, true);
        styles.SetHorizontalAlignment(builder, "center");
        styles.SetBorder(builder, "bottom", "thin", "FF000000");
        XlsxCellFormatDefinition format = styles.BuildCellFormat(builder, 0, false, 0, true, 0, false, 0, true);
        Assert.That(styles.HasFontValues(builder), Is.True);
        Assert.That(styles.HasAlignmentValues(builder), Is.True);
        Assert.That(styles.FindBorder(builder, "bottom"), Is.Not.Null);
        Assert.That(styles.BuildFontRawXml(builder), Does.Contain("Aptos"));
        Assert.That(styles.BuildFillRawXml(builder), Does.Contain("fill"));
        Assert.That(styles.BuildBorderRawXml(builder), Does.Contain("border"));
        Assert.That(styles.BuildCellFormatRawXml(builder, format), Does.Contain("xf"));

        XlsxDrawingPart drawing = new XlsxDrawingPart();
        IXlsxDrawingPartOperations drawings = new XlsxDrawingPartOperations();
        XlsxDrawingImagePlacementResult placement = drawings.AddImageAnchorCore(drawing, "twoCell", "rId1", "A1", "B2", "Audit image", "coverage");
        Assert.That(placement.IsValid, Is.True);
        Assert.That(drawings.GetNextPictureId(drawing), Is.EqualTo(2));
    }

    [Test]
    public void ReaderFacades_ExerciseByteStreamRelationshipAndSourceOverloads()
    {
        string chartXml = "<c:chartSpace xmlns:c=\"" + XlsxNamespaces.Chart + "\"><c:chart /></c:chartSpace>";
        IXlsxChartPartReader charts = new XlsxChartPartReader();
        XlsxChartPart chartBytes = charts.ReadBytesFromPart("/xl/charts/chart1.xml", System.Text.Encoding.UTF8.GetBytes(chartXml));
        XlsxChartPart chartStream = charts.ReadStreamFromPart("/xl/charts/chart2.xml", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(chartXml)));
        Assert.That(chartBytes.PartPath, Is.EqualTo("/xl/charts/chart1.xml"));
        Assert.That(chartStream.PartPath, Is.EqualTo("/xl/charts/chart2.xml"));

        string sheetXml = "<chartsheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" />";
        IXlsxChartsheetPartReader chartsheets = new XlsxChartsheetPartReader();
        XlsxChartsheetPart chartsheet = chartsheets.ReadBytesWithRelationships(System.Text.Encoding.UTF8.GetBytes(sheetXml), new XlsxRelationshipsPart());
        Assert.That(chartsheet.IsValid, Is.True);

        IXlsxSharedStringTableReader sharedStrings = new XlsxSharedStringTableReader();
        string sharedXml = "<sst xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><si><t>A</t></si></sst>";
        XlsxSharedStringTablePart shared = sharedStrings.ReadStreamWithOptions(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sharedXml)), new XlsxReaderOptions());
        Assert.That(shared.Items.Count, Is.EqualTo(1));

        List<string> fieldNames = new List<string>();
        fieldNames.Add("Region");
        IXlsxPivotTableDefinitionWriter pivotWriter = new XlsxPivotTableDefinitionWriter();
        XlsxPivotTableCreateResult created = pivotWriter.CreateFromSource(XlsxPivotTableSourceKind.Range, "Pivot1", 1, "Data", "A1:A2", "", "C1:D3", fieldNames);
        IXlsxPivotTableDefinitionReader pivotReader = new XlsxPivotTableDefinitionReader();
        XlsxPivotTableDefinitionPart pivot = pivotReader.ReadBytesWithRelationships("/xl/pivotTables/pivotTable1.xml", pivotWriter.WriteBytes(created.PivotTableDefinition).Bytes, new XlsxRelationshipsPart());
        Assert.That(created.IsValid, Is.True);
        Assert.That(pivot.Fields.Count, Is.EqualTo(1));
    }

    [Test]
    public void WorkbookFacades_ExerciseSanitizationComparisonRepairValidationAndInventory()
    {
        IXlsxWorkbookOperations workbooks = new XlsxWorkbookOperations();
        XlsxWorkbook left = workbooks.Create();
        XlsxWorkbook right = workbooks.Create();
        Assert.That(workbooks.RemoveMacros(left).IsValid, Is.True);
        Assert.That(workbooks.CompareToWithOptions(left, right, new XlsxWorkbookCompareOptions()).AreEqual, Is.True);
        Assert.That(workbooks.Repair(left).IsValid, Is.True);
        Assert.That(workbooks.RepairWithOptions(left, new XlsxWorkbookRepairOptions()).IsValid, Is.True);
        Assert.That(workbooks.CountMacros(left), Is.EqualTo(0));
        Assert.That(workbooks.CountExternalLinks(left), Is.EqualTo(0));
        Assert.That(workbooks.CountCustomXmlParts(left), Is.EqualTo(0));

        IXlsxWorkbookRepairer repairer = new XlsxWorkbookRepairer();
        Assert.That(repairer.RepairWorkbookWithOptions(left, new XlsxWorkbookRepairOptions()).IsValid, Is.True);
        IXlsxWorkbookValidator validator = new XlsxWorkbookValidator();
        Assert.That(validator.ValidateWorkbookWithOptions(left, new XlsxWorkbookValidationOptions()).IsValid, Is.True);
    }

    [Test]
    public void WorksheetFacade_ExercisesStorageTablesFiltersCellsAndConditionalFormatting()
    {
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        XlsxWorksheetStorageOptions storageOptions = new XlsxWorksheetStorageOptions();
        storageOptions.ForceSparse = true;
        Assert.That(operations.SelectStorageKind(sheet, storageOptions), Is.EqualTo(XlsxWorksheetStorageKind.Sparse));

        Assert.That(operations.AddSparkline(sheet, "D1", "A1:C1", "line").IsValid, Is.True);
        Assert.That(operations.GetSparklineGroupByType(sheet, "line"), Is.Not.Null);
        Assert.That(operations.SetCellValue(sheet, "A1", XlsxCellValue.CreateText("header")).IsValid, Is.True);
        Assert.That(operations.SetCellValueAt(sheet, 2, 1, XlsxCellValue.CreateNumber(4)).IsValid, Is.True);
        Assert.That(operations.SetBlankValue(sheet, "B2").IsValid, Is.True);
        Assert.That(operations.SetTypedErrorValue(sheet, "C2", XlsxCellErrorKind.NotAvailable).IsValid, Is.True);
        Assert.That(operations.GetRowByUnsignedIndex(sheet, 2), Is.Not.Null);
        Assert.That(operations.ContainsCellAt(sheet, 2, 1), Is.True);

        DataTable dataTable = new DataTable("Audit");
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Value", typeof(int));
        dataTable.Rows.Add("one", 1);
        XlsxDataTableImportResult imported = operations.ImportDataTableAsTableWithHeaders(sheet, "F1", dataTable, "AuditTable", true);
        Assert.That(imported.IsValid, Is.True);
        Assert.That(operations.SetTableHeaderRow(sheet, "AuditTable", false).IsValid, Is.True);

        Assert.That(operations.AddAutoFilter(sheet, "A1:C3").IsValid, Is.True);
        Assert.That(operations.SetAutoFilterDynamic(sheet, 0, "aboveAverage").IsValid, Is.True);
        Assert.That(operations.SetAutoFilterColor(sheet, 1, 2, true).IsValid, Is.True);
        Assert.That(operations.SetAutoFilterIcon(sheet, 2, "3TrafficLights1", 1).IsValid, Is.True);
        Assert.That(operations.ClearAutoFilterColumn(sheet, 2).IsValid, Is.True);
        Assert.That(operations.AddConditionalFormattingRule(sheet, "A1:A3", "expression", "A1>0").IsValid, Is.True);
    }

    [Test]
    public void WorksheetFacade_ExercisesFormulaDimensionMergePaneHyperlinkAndRelationshipHelpers()
    {
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        XlsxWorksheetPart sheet = new XlsxWorksheetPart();
        operations.SetNumberValue(sheet, "A1", 1);
        operations.SetRangeFormula(sheet, "B1", "B1:B2", XlsxCellFormula.CreateArray("A1+1", "B1:B2"), "2", "n");
        Assert.That(operations.UpdateFormulaReferencesForRowInsert(sheet, 1, 1).IsValid, Is.True);
        Assert.That(operations.UpdateFormulaReferencesForRowDelete(sheet, 2, 1).IsValid, Is.True);
        Assert.That(operations.UpdateFormulaReferencesForColumnInsert(sheet, 1, 1).IsValid, Is.True);
        Assert.That(operations.MoveFormulaTextReferences("A1+Z9", 1, 1, 1, 1, 1, 1).IsValid, Is.True);
        Assert.That(operations.CopyFormulaTextReferences("A1", 1, 1).IsValid, Is.True);
        Assert.That(operations.RenameFormulaTextSheetReferences("Old!A1", "Old", "New").IsValid, Is.True);
        Assert.That(operations.CalculateDimensionReference(sheet), Is.Not.Empty);
        Assert.That(operations.ValidateMergedCells(sheet).IsValid, Is.True);
        Assert.That(operations.MergeCellsByCoordinates(sheet, 4, 1, 4, 2).IsValid, Is.True);
        Assert.That(operations.UnmergeCellsByCoordinates(sheet, 4, 1, 4, 2).IsValid, Is.True);
        Assert.That(operations.SplitPanes(sheet, 100, 200).IsValid, Is.True);

        Assert.That(operations.AddExternalHyperlink(sheet, "A1", "https://example.test", "Example", "tip").IsValid, Is.True);
        XlsxWorksheetHyperlinkResult hyperlinkRelationships = operations.CreateHyperlinkRelationships(sheet);
        Assert.That(hyperlinkRelationships.IsValid, Is.True);
        Assert.That(hyperlinkRelationships.Relationships.Relationships.Count, Is.EqualTo(1));

        XlsxSheetDrawingReference drawing = new XlsxSheetDrawingReference();
        drawing.RelationshipId = "rIdDrawing";
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = "rIdDrawing";
        relationship.Type = XlsxRelationshipTypes.Drawing;
        relationship.Target = "../drawings/drawing1.xml";
        relationship.TargetPath = "/xl/drawings/drawing1.xml";
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.Relationships.Add(relationship);
        XlsxSheetDrawingRelationshipResult drawingResult = new XlsxSheetDrawingRelationshipResult();
        operations.ResolveDrawingRelationshipResult(drawingResult, drawing, relationships, "worksheet");
        Assert.That(drawingResult.IsValid, Is.True);
        Assert.That(drawing.TargetPath, Is.EqualTo("/xl/drawings/drawing1.xml"));

        string xml = "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><sheetData /></worksheet>";
        IXlsxWorksheetPartReader reader = new XlsxWorksheetPartReader();
        Assert.That(reader.ReadBytesWithRelationships(System.Text.Encoding.UTF8.GetBytes(xml), new XlsxRelationshipsPart()).IsValid, Is.True);
    }
}
