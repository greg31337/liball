using System.Collections.Generic;
using System.Text;

namespace odf;

internal static class ChartDocumentModel
{
    internal static void LoadChartStructure(
        OdfDocument document,
        OdfChart chart)
    {
        OdfNode? chartNode = chart.Node;
        if (chartNode == null)
        {
            return;
        }
        chart.Title = LoadDirectChartText(chartNode, "title");
        chart.Subtitle = LoadDirectChartText(chartNode, "subtitle");
        chart.Footer = LoadDirectChartText(chartNode, "footer");
        OdfNode? legendNode = FindDirectChartChild(chartNode, "legend");
        chart.Legend = legendNode == null ? null : LoadLegend(legendNode);
        OdfNode? plotNode = FindDirectChartChild(chartNode, "plot-area");
        chart.PlotArea = plotNode == null ? null : LoadPlotArea(plotNode);
        ChartSeriesModel.LoadSeriesAndMetadata(document, chart);
    }

    internal static void AttachChartDataTables(OdfDocument document)
    {
        int chartIndex = 0;
        while (chartIndex < document.Charts.Length)
        {
            OdfChart chart = document.Charts[chartIndex];
            chart.DataTable = null;
            OdfNode? chartNode = chart.Node;
            OdfNode? tableNode = chartNode == null
                ? null
                : FindDirectTableChild(chartNode);
            int tableIndex = 0;
            while (tableNode != null && tableIndex < document.Tables.Length)
            {
                if (document.Tables[tableIndex].Node == tableNode)
                {
                    chart.DataTable = document.Tables[tableIndex];
                    break;
                }
                tableIndex = tableIndex + 1;
            }
            chartIndex = chartIndex + 1;
        }
        ChartSourceRangeModel.ResolveChartSourceRanges(document);
    }

    internal static void SynchronizeChartStructure(OdfChart chart)
    {
        WriteChartText(chart.Title);
        WriteChartText(chart.Subtitle);
        WriteChartText(chart.Footer);
        WriteLegend(chart.Legend);
        OdfChartPlotArea? plot = chart.PlotArea;
        if (plot == null || plot.Node == null)
        {
            return;
        }
        WriteChanged(plot.Node, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, plot.X, plot.OriginalX);
        WriteChanged(plot.Node, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, plot.Y, plot.OriginalY);
        WriteChanged(plot.Node, OdfNamespaces.Svg, "width", OdfNamespaces.SvgPrefix, plot.Width, plot.OriginalWidth);
        WriteChanged(plot.Node, OdfNamespaces.Svg, "height", OdfNamespaces.SvgPrefix, plot.Height, plot.OriginalHeight);
        WriteChanged(plot.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, plot.StyleName, plot.OriginalStyleName);
        WriteChanged(plot.Node, OdfNamespaces.Table, "cell-range-address", OdfNamespaces.TablePrefix, plot.CellRangeAddress, plot.OriginalCellRangeAddress);
        WriteChanged(plot.Node, OdfNamespaces.Chart, "data-source-has-labels", OdfNamespaces.ChartPrefix, plot.DataSourceHasLabels, plot.OriginalDataSourceHasLabels);
        int index = 0;
        while (index < plot.Axes.Length)
        {
            WriteAxis(plot.Axes[index]);
            index = index + 1;
        }
        WriteSurface(plot.Wall);
        WriteSurface(plot.Floor);
        ChartSeriesModel.SynchronizeSeriesStructure(chart);
        CapturePlotOriginalValues(plot);
    }

    static OdfChartText? LoadDirectChartText(OdfNode parent, string name)
    {
        OdfNode? node = FindDirectChartChild(parent, name);
        return node == null ? null : LoadChartText(node);
    }

    static OdfChartText LoadChartText(OdfNode node)
    {
        OdfChartText result = new OdfChartText();
        result.Node = node;
        result.Text = TextContent(node);
        result.CellRange = Read(node, OdfNamespaces.Table, "cell-range");
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        CaptureTextOriginalValues(result);
        return result;
    }

    static OdfChartLegend LoadLegend(OdfNode node)
    {
        OdfChartLegend result = new OdfChartLegend();
        result.Node = node;
        result.Position = Read(node, OdfNamespaces.Chart, "legend-position");
        result.Alignment = Read(node, OdfNamespaces.Chart, "legend-align");
        result.Expansion = Read(node, OdfNamespaces.Style, "legend-expansion");
        result.ExpansionAspectRatio = Read(node, OdfNamespaces.Style, "legend-expansion-aspect-ratio");
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.Text = TextContent(node);
        CaptureLegendOriginalValues(result);
        return result;
    }

    static OdfChartPlotArea LoadPlotArea(OdfNode node)
    {
        OdfChartPlotArea result = new OdfChartPlotArea();
        result.Node = node;
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.Width = Read(node, OdfNamespaces.Svg, "width");
        result.Height = Read(node, OdfNamespaces.Svg, "height");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.CellRangeAddress = Read(node, OdfNamespaces.Table, "cell-range-address");
        result.DataSourceHasLabels = Read(node, OdfNamespaces.Chart, "data-source-has-labels");
        List<OdfChartAxis> axes = new List<OdfChartAxis>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsChartElement(child, "axis"))
            {
                axes.Add(LoadAxis(child));
            }
            else if (IsChartElement(child, "wall"))
            {
                result.Wall = LoadSurface(child);
            }
            else if (IsChartElement(child, "floor"))
            {
                result.Floor = LoadSurface(child);
            }
            index = index + 1;
        }
        result.Axes = axes.ToArray();
        CapturePlotOriginalValues(result);
        return result;
    }

    static OdfChartAxis LoadAxis(OdfNode node)
    {
        OdfChartAxis result = new OdfChartAxis();
        result.Node = node;
        result.Dimension = Read(node, OdfNamespaces.Chart, "dimension");
        result.Name = Read(node, OdfNamespaces.Chart, "name");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.Title = LoadDirectChartText(node, "title");
        OdfNode? categoriesNode = FindDirectChartChild(node, "categories");
        if (categoriesNode != null)
        {
            OdfChartCategories categories = new OdfChartCategories();
            categories.Node = categoriesNode;
            categories.CellRangeAddress = Read(categoriesNode, OdfNamespaces.Table, "cell-range-address");
            categories.OriginalCellRangeAddress = categories.CellRangeAddress;
            result.Categories = categories;
        }
        List<OdfChartGrid> grids = new List<OdfChartGrid>();
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsChartElement(node.Children[index], "grid"))
            {
                grids.Add(LoadGrid(node.Children[index]));
            }
            index = index + 1;
        }
        result.Grids = grids.ToArray();
        CaptureAxisOriginalValues(result);
        return result;
    }

    static OdfChartGrid LoadGrid(OdfNode node)
    {
        OdfChartGrid result = new OdfChartGrid();
        result.Node = node;
        result.ClassName = Read(node, OdfNamespaces.Chart, "class");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.OriginalClassName = result.ClassName;
        result.OriginalStyleName = result.StyleName;
        return result;
    }

    static OdfChartSurface LoadSurface(OdfNode node)
    {
        OdfChartSurface result = new OdfChartSurface();
        result.Node = node;
        result.Width = Read(node, OdfNamespaces.Svg, "width");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.OriginalWidth = result.Width;
        result.OriginalStyleName = result.StyleName;
        return result;
    }

    static void WriteChartText(OdfChartText? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Table, "cell-range", OdfNamespaces.TablePrefix, item.CellRange, item.OriginalCellRange);
        WriteChanged(item.Node, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, item.X, item.OriginalX);
        WriteChanged(item.Node, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, item.Y, item.OriginalY);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        if (item.Text != item.OriginalText)
        {
            SetParagraphText(item.Node, item.Text);
        }
        CaptureTextOriginalValues(item);
    }

    static void WriteLegend(OdfChartLegend? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Chart, "legend-position", OdfNamespaces.ChartPrefix, item.Position, item.OriginalPosition);
        WriteChanged(item.Node, OdfNamespaces.Chart, "legend-align", OdfNamespaces.ChartPrefix, item.Alignment, item.OriginalAlignment);
        WriteChanged(item.Node, OdfNamespaces.Style, "legend-expansion", OdfNamespaces.StylePrefix, item.Expansion, item.OriginalExpansion);
        WriteChanged(item.Node, OdfNamespaces.Style, "legend-expansion-aspect-ratio", OdfNamespaces.StylePrefix, item.ExpansionAspectRatio, item.OriginalExpansionAspectRatio);
        WriteChanged(item.Node, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, item.X, item.OriginalX);
        WriteChanged(item.Node, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, item.Y, item.OriginalY);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        if (item.Text != item.OriginalText)
        {
            SetParagraphText(item.Node, item.Text);
        }
        CaptureLegendOriginalValues(item);
    }

    static void WriteAxis(OdfChartAxis? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Chart, "dimension", OdfNamespaces.ChartPrefix, item.Dimension, item.OriginalDimension);
        WriteChanged(item.Node, OdfNamespaces.Chart, "name", OdfNamespaces.ChartPrefix, item.Name, item.OriginalName);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        WriteChartText(item.Title);
        OdfChartCategories? categories = item.Categories;
        if (categories != null && categories.Node != null)
        {
            WriteChanged(categories.Node, OdfNamespaces.Table, "cell-range-address", OdfNamespaces.TablePrefix, categories.CellRangeAddress, categories.OriginalCellRangeAddress);
            categories.OriginalCellRangeAddress = categories.CellRangeAddress;
        }
        int index = 0;
        while (index < item.Grids.Length)
        {
            WriteGrid(item.Grids[index]);
            index = index + 1;
        }
        CaptureAxisOriginalValues(item);
    }

    static void WriteGrid(OdfChartGrid? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Chart, "class", OdfNamespaces.ChartPrefix, item.ClassName, item.OriginalClassName);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        item.OriginalClassName = item.ClassName;
        item.OriginalStyleName = item.StyleName;
    }

    static void WriteSurface(OdfChartSurface? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Svg, "width", OdfNamespaces.SvgPrefix, item.Width, item.OriginalWidth);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        item.OriginalWidth = item.Width;
        item.OriginalStyleName = item.StyleName;
    }

    static void SetParagraphText(OdfNode parent, string value)
    {
        OdfNode? paragraph = FindDirectTextChild(parent);
        if (paragraph == null)
        {
            paragraph = CreateTextParagraph(parent.Location.PartPath);
            AppendChartChild(parent, paragraph);
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = paragraph;
        text.Location.PartPath = paragraph.Location.PartPath;
        paragraph.Children = [text];
        paragraph.WasEmptyElement = false;
    }

    static OdfNode CreateTextParagraph(string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(OdfNamespaces.Text, "p", OdfNamespaces.TextPrefix);
        result.Location.PartPath = partPath;
        result.WasEmptyElement = true;
        return result;
    }

    static void AppendChartChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        int index = 0;
        while (index < parent.Children.Length)
        {
            children[index] = parent.Children[index];
            index = index + 1;
        }
        child.Parent = parent;
        children[children.Length - 1] = child;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static OdfNode? FindDirectChartChild(OdfNode parent, string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (IsChartElement(parent.Children[index], localName))
            {
                return parent.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectTableChild(OdfNode parent)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Table
                && child.Name.LocalName == "table")
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectTextChild(OdfNode parent)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Text
                && child.Name.LocalName == "p")
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsChartElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Chart
            && node.Name.LocalName == localName;
    }

    static string TextContent(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        AppendTextContent(node, result);
        return result.ToString();
    }

    static void AppendTextContent(OdfNode node, StringBuilder result)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendTextContent(node.Children[index], result);
            index = index + 1;
        }
    }

    static string Read(OdfNode node, string namespaceName, string localName)
    {
        return StyleDocumentModel.ReadAttribute(node, namespaceName, localName);
    }

    static void WriteChanged(OdfNode node, string namespaceName, string localName, string prefix, string value, string original)
    {
        if (value == original)
        {
            return;
        }
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(node, namespaceName, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(node, namespaceName, localName, prefix, value);
        }
    }

    static void CaptureTextOriginalValues(OdfChartText item)
    {
        item.OriginalText = item.Text;
        item.OriginalCellRange = item.CellRange;
        item.OriginalX = item.X;
        item.OriginalY = item.Y;
        item.OriginalStyleName = item.StyleName;
    }

    static void CaptureLegendOriginalValues(OdfChartLegend item)
    {
        item.OriginalPosition = item.Position;
        item.OriginalAlignment = item.Alignment;
        item.OriginalExpansion = item.Expansion;
        item.OriginalExpansionAspectRatio = item.ExpansionAspectRatio;
        item.OriginalX = item.X;
        item.OriginalY = item.Y;
        item.OriginalStyleName = item.StyleName;
        item.OriginalText = item.Text;
    }

    static void CapturePlotOriginalValues(OdfChartPlotArea item)
    {
        item.OriginalX = item.X;
        item.OriginalY = item.Y;
        item.OriginalWidth = item.Width;
        item.OriginalHeight = item.Height;
        item.OriginalStyleName = item.StyleName;
        item.OriginalCellRangeAddress = item.CellRangeAddress;
        item.OriginalDataSourceHasLabels = item.DataSourceHasLabels;
    }

    static void CaptureAxisOriginalValues(OdfChartAxis item)
    {
        item.OriginalDimension = item.Dimension;
        item.OriginalName = item.Name;
        item.OriginalStyleName = item.StyleName;
    }
}
