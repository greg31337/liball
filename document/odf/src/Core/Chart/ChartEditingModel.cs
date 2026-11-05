using System.Collections.Generic;
using System.Numerics;

namespace odf;

internal static class ChartEditingModel
{
    internal static OdfChart[] GetCharts(OdfDocument document)
    {
        RequireDocument(document);
        OdfChart[] result = new OdfChart[document.Charts.Length];
        global::System.Array.Copy(document.Charts, result, result.Length);
        return result;
    }

    internal static OdfChart CreateChart(
        OdfDocument document,
        OdfChartKind kind)
    {
        RequireDocument(document);
        string className = ChartClass(kind);
        if (className.Length == 0)
        {
            throw new OdfEditException(
                "A recognized chart kind is required for chart creation.");
        }
        if (document.Kind != OdfDocumentKind.Chart
            && document.Kind != OdfDocumentKind.ChartTemplate)
        {
            throw new OdfEditException(
                "Root chart creation requires a chart document.");
        }
        if (document.Charts.Length == 1)
        {
            OdfNode existingNode = document.Charts[0].Node
                ?? new OdfNode();
            document.Charts[0].Kind = kind;
            document.Charts[0].ClassName = className;
            ApplyChartChanges(document);
            return RequireChartByNode(document, existingNode);
        }
        if (document.Charts.Length > 1)
        {
            throw new OdfEditException(
                "The chart document contains multiple root charts.");
        }
        OdfNode container = RequireChartContainer(document);
        OdfNode chartNode = CreateChartElement("chart");
        StyleDocumentModel.WriteAttribute(
            chartNode,
            OdfNamespaces.Chart,
            "class",
            OdfNamespaces.ChartPrefix,
            className);
        StyleDocumentModel.AppendNode(
            chartNode,
            CreateChartElement("plot-area"));
        StyleDocumentModel.AppendNode(container, chartNode);
        EmbeddedContentManager.ReloadChartsAfterReferenceUpdate(document);
        return RequireChartByNode(document, chartNode);
    }

    internal static OdfChart BindChartData(
        OdfDocument document,
        OdfChart chart,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        bool seriesInColumns,
        bool firstRowLabels,
        bool firstColumnLabels)
    {
        RequireOwnedChart(document, chart);
        RequireOwnedTable(document, table);
        if (ChartClass(chart.Kind).Length == 0)
        {
            throw new OdfEditException(
                "A recognized chart kind is required for data binding.");
        }
        RequireBindingRange(
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            seriesInColumns,
            firstRowLabels,
            firstColumnLabels);
        OdfNode chartNode = chart.Node ?? new OdfNode();
        OdfNode plotNode = RequirePlotNode(chart);
        RemoveSeriesNodes(plotNode);
        StyleDocumentModel.WriteAttribute(
            plotNode,
            OdfNamespaces.Table,
            "cell-range-address",
            OdfNamespaces.TablePrefix,
            FormatRange(
                table.Name,
                rowIndex,
                columnIndex,
                rowIndex + rowCount - 1,
                columnIndex + columnCount - 1));
        StyleDocumentModel.WriteAttribute(
            plotNode,
            OdfNamespaces.Chart,
            "data-source-has-labels",
            OdfNamespaces.ChartPrefix,
            LabelMode(firstRowLabels, firstColumnLabels));
        BindCategories(
            plotNode,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            seriesInColumns,
            firstRowLabels,
            firstColumnLabels);
        AppendBoundSeries(
            plotNode,
            table,
            chart,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            seriesInColumns,
            firstRowLabels,
            firstColumnLabels);
        EmbeddedContentManager.ReloadChartsAfterReferenceUpdate(document);
        return RequireChartByNode(document, chartNode);
    }

    internal static void ApplyChartChanges(OdfDocument document)
    {
        RequireDocument(document);
        int index = 0;
        while (index < document.Charts.Length)
        {
            PrepareChartKind(document.Charts[index]);
            index = index + 1;
        }
        ChartSeriesModel.SynchronizeAllChartStyles(document);
        StyleDocumentModel.SynchronizeStyles(document);
        EmbeddedContentManager.SynchronizeEmbeddedContent(document);
        TableDocumentModel.LoadTables(document);
    }

    internal static OdfDiagnostic[] ValidateChart(
        OdfDocument document,
        OdfChart chart)
    {
        RequireOwnedChart(document, chart);
        List<OdfDiagnostic> diagnostics = new List<OdfDiagnostic>();
        if (ChartClass(chart.Kind).Length == 0)
        {
            AddDiagnostic(
                diagnostics,
                OdfValidationSeverity.Warning,
                OdfDiagnosticCodes.InvalidDatatype,
                "The chart class is not a recognized standard chart kind.",
                chart.Node);
        }
        CheckStyle(
            document,
            diagnostics,
            chart.StyleName,
            chart.Node);
        CheckTextRange(document, chart, diagnostics, chart.Title);
        CheckTextRange(document, chart, diagnostics, chart.Subtitle);
        CheckTextRange(document, chart, diagnostics, chart.Footer);
        OdfChartPlotArea? plot = chart.PlotArea;
        if (plot == null || plot.Node == null)
        {
            AddDiagnostic(
                diagnostics,
                OdfValidationSeverity.Error,
                OdfDiagnosticCodes.MissingRequiredContent,
                "The chart does not contain a plot area.",
                chart.Node);
            return diagnostics.ToArray();
        }
        CheckRange(
            document,
            chart,
            diagnostics,
            plot.CellRangeAddress,
            "The plot-area source range is invalid.",
            plot.Node);
        CheckStyle(
            document,
            diagnostics,
            plot.StyleName,
            plot.Node);
        CheckAxes(document, chart, diagnostics, plot);
        CheckSeries(document, chart, diagnostics, plot);
        return diagnostics.ToArray();
    }

    static void BindCategories(
        OdfNode plotNode,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        bool seriesInColumns,
        bool firstRowLabels,
        bool firstColumnLabels)
    {
        OdfNode axis = RequireCategoryAxis(plotNode);
        RemoveCategoryNodes(axis);
        bool hasCategories = seriesInColumns
            ? firstColumnLabels
            : firstRowLabels;
        if (!hasCategories)
        {
            return;
        }
        long startRow = seriesInColumns
            ? rowIndex + (firstRowLabels ? 1 : 0)
            : rowIndex;
        long endRow = seriesInColumns
            ? rowIndex + rowCount - 1
            : rowIndex;
        long startColumn = seriesInColumns
            ? columnIndex
            : columnIndex + (firstColumnLabels ? 1 : 0);
        long endColumn = seriesInColumns
            ? columnIndex
            : columnIndex + columnCount - 1;
        OdfNode categories = CreateChartElement("categories");
        StyleDocumentModel.WriteAttribute(
            categories,
            OdfNamespaces.Table,
            "cell-range-address",
            OdfNamespaces.TablePrefix,
            FormatRange(
                table.Name,
                startRow,
                startColumn,
                endRow,
                endColumn));
        StyleDocumentModel.AppendNode(axis, categories);
    }

    static void AppendBoundSeries(
        OdfNode plotNode,
        OdfTable table,
        OdfChart chart,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        bool seriesInColumns,
        bool firstRowLabels,
        bool firstColumnLabels)
    {
        long seriesCount = seriesInColumns
            ? columnCount - (firstColumnLabels ? 1 : 0)
            : rowCount - (firstRowLabels ? 1 : 0);
        long index = 0;
        while (index < seriesCount)
        {
            OdfNode series = CreateChartElement("series");
            StyleDocumentModel.WriteAttribute(
                series,
                OdfNamespaces.Chart,
                "class",
                OdfNamespaces.ChartPrefix,
                ChartClass(chart.Kind));
            WriteSeriesRanges(
                series,
                table,
                rowIndex,
                columnIndex,
                rowCount,
                columnCount,
                index,
                seriesInColumns,
                firstRowLabels,
                firstColumnLabels);
            StyleDocumentModel.AppendNode(plotNode, series);
            index = index + 1;
        }
    }

    static void WriteSeriesRanges(
        OdfNode series,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        long seriesIndex,
        bool seriesInColumns,
        bool firstRowLabels,
        bool firstColumnLabels)
    {
        long startRow = seriesInColumns
            ? rowIndex + (firstRowLabels ? 1 : 0)
            : rowIndex + (firstRowLabels ? 1 : 0) + seriesIndex;
        long endRow = seriesInColumns
            ? rowIndex + rowCount - 1
            : startRow;
        long startColumn = seriesInColumns
            ? columnIndex + (firstColumnLabels ? 1 : 0) + seriesIndex
            : columnIndex + (firstColumnLabels ? 1 : 0);
        long endColumn = seriesInColumns
            ? startColumn
            : columnIndex + columnCount - 1;
        StyleDocumentModel.WriteAttribute(
            series,
            OdfNamespaces.Chart,
            "values-cell-range-address",
            OdfNamespaces.ChartPrefix,
            FormatRange(
                table.Name,
                startRow,
                startColumn,
                endRow,
                endColumn));
        bool hasLabel = seriesInColumns
            ? firstRowLabels
            : firstColumnLabels;
        if (hasLabel)
        {
            long labelRow = seriesInColumns ? rowIndex : startRow;
            long labelColumn = seriesInColumns ? startColumn : columnIndex;
            StyleDocumentModel.WriteAttribute(
                series,
                OdfNamespaces.Chart,
                "label-cell-address",
                OdfNamespaces.ChartPrefix,
                FormatCell(table.Name, labelRow, labelColumn));
        }
    }

    static OdfNode RequireCategoryAxis(OdfNode plotNode)
    {
        int index = 0;
        while (index < plotNode.Children.Length)
        {
            OdfNode child = plotNode.Children[index];
            if (IsChartElement(child, "axis")
                && StyleDocumentModel.ReadAttribute(
                    child,
                    OdfNamespaces.Chart,
                    "dimension") == "x")
            {
                return child;
            }
            index = index + 1;
        }
        OdfNode axis = CreateChartElement("axis");
        StyleDocumentModel.WriteAttribute(
            axis,
            OdfNamespaces.Chart,
            "dimension",
            OdfNamespaces.ChartPrefix,
            "x");
        StyleDocumentModel.AppendNode(plotNode, axis);
        return axis;
    }

    static void RemoveSeriesNodes(OdfNode plotNode)
    {
        List<OdfNode> retained = new List<OdfNode>();
        int index = 0;
        while (index < plotNode.Children.Length)
        {
            if (!IsChartElement(plotNode.Children[index], "series"))
            {
                retained.Add(plotNode.Children[index]);
            }
            else
            {
                plotNode.Children[index].Parent = null;
            }
            index = index + 1;
        }
        plotNode.Children = retained.ToArray();
    }

    static void RemoveCategoryNodes(OdfNode axis)
    {
        List<OdfNode> retained = new List<OdfNode>();
        int index = 0;
        while (index < axis.Children.Length)
        {
            if (!IsChartElement(axis.Children[index], "categories"))
            {
                retained.Add(axis.Children[index]);
            }
            else
            {
                axis.Children[index].Parent = null;
            }
            index = index + 1;
        }
        axis.Children = retained.ToArray();
    }

    static string FormatRange(
        string tableName,
        long startRow,
        long startColumn,
        long endRow,
        long endColumn)
    {
        OdfCellRangeValue range = new OdfCellRangeValue();
        range.Start = CreateAddress(tableName, startRow, startColumn);
        range.End = CreateAddress(tableName, endRow, endColumn);
        range.HasEnd = true;
        range.IsValid = true;
        return OdfCellAddressFormatter.FormatCellRange(range);
    }

    static string FormatCell(
        string tableName,
        long rowIndex,
        long columnIndex)
    {
        return OdfCellAddressFormatter.FormatCellAddress(
            CreateAddress(tableName, rowIndex, columnIndex));
    }

    static OdfCellAddressValue CreateAddress(
        string tableName,
        long rowIndex,
        long columnIndex)
    {
        OdfCellAddressValue result = new OdfCellAddressValue();
        result.HasSheet = true;
        result.SheetName = tableName;
        result.HasColumn = true;
        result.ColumnIndex = new BigInteger(columnIndex) + 1;
        result.HasRow = true;
        result.Row = new BigInteger(rowIndex) + 1;
        result.IsValid = true;
        return result;
    }

    static string LabelMode(bool firstRowLabels, bool firstColumnLabels)
    {
        if (firstRowLabels && firstColumnLabels)
        {
            return "both";
        }
        if (firstRowLabels)
        {
            return "row";
        }
        if (firstColumnLabels)
        {
            return "column";
        }
        return "none";
    }

    static void PrepareChartKind(OdfChart chart)
    {
        string className = ChartClass(chart.Kind);
        if (className.Length > 0)
        {
            chart.ClassName = className;
        }
        OdfChartPlotArea? plot = chart.PlotArea;
        if (plot == null)
        {
            return;
        }
        int index = 0;
        while (index < plot.Series.Length)
        {
            string seriesClass = ChartClass(plot.Series[index].Kind);
            if (seriesClass.Length > 0)
            {
                plot.Series[index].ClassName = seriesClass;
            }
            index = index + 1;
        }
    }

    static string ChartClass(OdfChartKind kind)
    {
        if (kind == OdfChartKind.Bar) return "chart:bar";
        if (kind == OdfChartKind.Line) return "chart:line";
        if (kind == OdfChartKind.Area) return "chart:area";
        if (kind == OdfChartKind.Pie) return "chart:circle";
        if (kind == OdfChartKind.Ring) return "chart:ring";
        if (kind == OdfChartKind.Scatter) return "chart:scatter";
        if (kind == OdfChartKind.Radar) return "chart:radar";
        if (kind == OdfChartKind.FilledRadar) return "chart:filled-radar";
        if (kind == OdfChartKind.Stock) return "chart:stock";
        if (kind == OdfChartKind.Bubble) return "chart:bubble";
        if (kind == OdfChartKind.Surface) return "chart:surface";
        if (kind == OdfChartKind.Gantt) return "chart:gantt";
        return "";
    }

    static void CheckTextRange(
        OdfDocument document,
        OdfChart chart,
        List<OdfDiagnostic> diagnostics,
        OdfChartText? text)
    {
        if (text != null)
        {
            CheckRange(
                document,
                chart,
                diagnostics,
                text.CellRange,
                "The chart text source range is invalid.",
                text.Node);
        }
    }

    static void CheckAxes(
        OdfDocument document,
        OdfChart chart,
        List<OdfDiagnostic> diagnostics,
        OdfChartPlotArea plot)
    {
        int index = 0;
        while (index < plot.Axes.Length)
        {
            OdfChartAxis axis = plot.Axes[index];
            CheckStyle(
                document,
                diagnostics,
                axis.StyleName,
                axis.Node);
            OdfChartCategories? categories = axis.Categories;
            if (categories != null)
            {
                CheckRange(
                    document,
                    chart,
                    diagnostics,
                    categories.CellRangeAddress,
                    "The chart category range is invalid.",
                    categories.Node);
            }
            index = index + 1;
        }
    }

    static void CheckSeries(
        OdfDocument document,
        OdfChart chart,
        List<OdfDiagnostic> diagnostics,
        OdfChartPlotArea plot)
    {
        if (plot.Series.Length == 0)
        {
            AddDiagnostic(
                diagnostics,
                OdfValidationSeverity.Warning,
                OdfDiagnosticCodes.MissingRequiredContent,
                "The chart plot area does not contain a data series.",
                plot.Node);
        }
        int index = 0;
        while (index < plot.Series.Length)
        {
            OdfChartSeries series = plot.Series[index];
            CheckRange(
                document,
                chart,
                diagnostics,
                series.ValuesCellRangeAddress,
                "The chart series value range is invalid.",
                series.Node);
            CheckRange(
                document,
                chart,
                diagnostics,
                series.LabelCellAddress,
                "The chart series label range is invalid.",
                series.Node);
            CheckStyle(
                document,
                diagnostics,
                series.StyleName,
                series.Node);
            CheckDomains(document, chart, diagnostics, series);
            CheckErrorRanges(document, chart, diagnostics, series);
            index = index + 1;
        }
    }

    static void CheckDomains(
        OdfDocument document,
        OdfChart chart,
        List<OdfDiagnostic> diagnostics,
        OdfChartSeries series)
    {
        int index = 0;
        while (index < series.Domains.Length)
        {
            CheckRange(
                document,
                chart,
                diagnostics,
                series.Domains[index].CellRangeAddress,
                "The chart series domain range is invalid.",
                series.Domains[index].Node);
            index = index + 1;
        }
    }

    static void CheckErrorRanges(
        OdfDocument document,
        OdfChart chart,
        List<OdfDiagnostic> diagnostics,
        OdfChartSeries series)
    {
        int index = 0;
        while (index < series.ErrorIndicators.Length)
        {
            OdfChartStyleMetadata metadata =
                series.ErrorIndicators[index].StyleMetadata;
            CheckRange(
                document,
                chart,
                diagnostics,
                metadata.ErrorLowerRange,
                "The chart lower-error range is invalid.",
                series.ErrorIndicators[index].Node);
            CheckRange(
                document,
                chart,
                diagnostics,
                metadata.ErrorUpperRange,
                "The chart upper-error range is invalid.",
                series.ErrorIndicators[index].Node);
            index = index + 1;
        }
    }

    static void CheckRange(
        OdfDocument document,
        OdfChart chart,
        List<OdfDiagnostic> diagnostics,
        string lexical,
        string message,
        OdfNode? node)
    {
        if (lexical.Length == 0)
        {
            return;
        }
        OdfCellRangeListValue parsed =
            OdfCellAddressParser.ParseRangeList(lexical);
        if (!parsed.IsValid || !RangeListResolves(document, chart, parsed))
        {
            AddDiagnostic(
                diagnostics,
                OdfValidationSeverity.Error,
                OdfDiagnosticCodes.InvalidRangeReference,
                message,
                node);
        }
    }

    static bool RangeListResolves(
        OdfDocument document,
        OdfChart chart,
        OdfCellRangeListValue ranges)
    {
        int index = 0;
        while (index < ranges.Ranges.Length)
        {
            OdfCellRangeValue range = ranges.Ranges[index];
            string startName = range.Start.HasSheet
                ? range.Start.SheetName
                : "";
            string endName = range.HasEnd && range.End.HasSheet
                ? range.End.SheetName
                : startName;
            if (startName.Length > 0
                && endName.Length > 0
                && startName != endName)
            {
                return false;
            }
            if (!RangeTableExists(document, chart, startName))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool RangeTableExists(
        OdfDocument document,
        OdfChart chart,
        string tableName)
    {
        if (tableName.Length == 0)
        {
            return chart.DataTable != null;
        }
        if (chart.DataTable != null
            && chart.DataTable.Name == tableName)
        {
            return true;
        }
        int count = 0;
        int index = 0;
        while (index < document.Tables.Length)
        {
            OdfTable table = document.Tables[index];
            if (table.Name == tableName)
            {
                if (table.PartPath == chart.PartPath)
                {
                    return true;
                }
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1;
    }

    static void CheckStyle(
        OdfDocument document,
        List<OdfDiagnostic> diagnostics,
        string styleName,
        OdfNode? node)
    {
        if (styleName.Length == 0)
        {
            return;
        }
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == styleName
                && style.Family == OdfStyleFamily.Chart)
            {
                return;
            }
            index = index + 1;
        }
        AddDiagnostic(
            diagnostics,
            OdfValidationSeverity.Error,
            OdfDiagnosticCodes.InvalidStyleReference,
            "The chart style reference cannot be resolved.",
            node);
    }

    static void AddDiagnostic(
        List<OdfDiagnostic> diagnostics,
        OdfValidationSeverity severity,
        string code,
        string message,
        OdfNode? node)
    {
        OdfDiagnostic diagnostic = new OdfDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.IsRecoverable = severity != OdfValidationSeverity.Fatal;
        if (node != null)
        {
            diagnostic.Location = node.Location;
        }
        diagnostics.Add(diagnostic);
    }

    static void RequireBindingRange(
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        bool seriesInColumns,
        bool firstRowLabels,
        bool firstColumnLabels)
    {
        if (rowIndex < 0 || columnIndex < 0
            || rowCount <= 0 || columnCount <= 0
            || rowCount > table.RowCount
            || columnCount > table.ColumnCount
            || rowIndex > table.RowCount - rowCount
            || columnIndex > table.ColumnCount - columnCount)
        {
            throw new OdfEditException(
                "The chart data range is outside the source table.");
        }
        long seriesCount = seriesInColumns
            ? columnCount - (firstColumnLabels ? 1 : 0)
            : rowCount - (firstRowLabels ? 1 : 0);
        long valueCount = seriesInColumns
            ? rowCount - (firstRowLabels ? 1 : 0)
            : columnCount - (firstColumnLabels ? 1 : 0);
        if (seriesCount <= 0 || valueCount <= 0
            || seriesCount > int.MaxValue)
        {
            throw new OdfEditException(
                "The chart data range must contain series and values after labels are excluded.");
        }
    }

    static void RequireOwnedChart(OdfDocument document, OdfChart chart)
    {
        RequireDocument(document);
        if (chart == null || chart.Node == null)
        {
            throw new OdfEditException(
                "An attached chart is required for chart editing.");
        }
        int index = 0;
        while (index < document.Charts.Length)
        {
            if (document.Charts[index] == chart)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The chart does not belong to the document.");
    }

    static void RequireOwnedTable(OdfDocument document, OdfTable table)
    {
        if (table == null || table.Node == null)
        {
            throw new OdfEditException(
                "An attached source table is required for chart binding.");
        }
        int index = 0;
        while (index < document.Tables.Length)
        {
            if (document.Tables[index] == table)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The chart source table does not belong to the document.");
    }

    static OdfNode RequireChartContainer(OdfDocument document)
    {
        OdfNode? body = document.Body;
        if (body == null)
        {
            throw new OdfEditException(
                "The chart document does not contain an office body.");
        }
        OdfNode? result = null;
        int index = 0;
        while (index < body.Children.Length)
        {
            OdfNode child = body.Children[index];
            if (child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Office
                && child.Name.LocalName == "chart")
            {
                if (result != null)
                {
                    throw new OdfEditException(
                        "The chart document body is ambiguous.");
                }
                result = child;
            }
            index = index + 1;
        }
        if (result == null)
        {
            throw new OdfEditException(
                "The chart document does not contain a chart body.");
        }
        return result;
    }

    static OdfNode RequirePlotNode(OdfChart chart)
    {
        OdfChartPlotArea? plot = chart.PlotArea;
        if (plot == null || plot.Node == null)
        {
            throw new OdfEditException(
                "The chart requires a plot area before data can be bound.");
        }
        return plot.Node;
    }

    static OdfChart RequireChartByNode(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.Charts.Length)
        {
            if (document.Charts[index].Node == node)
            {
                return document.Charts[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The edited chart could not be rematerialized.");
    }

    static OdfNode CreateChartElement(string localName)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Chart,
            localName,
            OdfNamespaces.ChartPrefix);
        result.WasEmptyElement = true;
        return result;
    }

    static bool IsChartElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Chart
            && node.Name.LocalName == localName;
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for chart editing.");
        }
    }
}
