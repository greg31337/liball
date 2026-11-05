using System.Text;

namespace odf;

internal static class ChartAdvancedModel
{
    internal static OdfChartErrorIndicator LoadErrorIndicator(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfChartErrorIndicator result = new OdfChartErrorIndicator();
        result.Node = node;
        result.Dimension = Read(node, OdfNamespaces.Chart, "dimension");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.StyleMetadata = ChartSeriesModel.LoadChartStyleMetadata(
            document,
            result.StyleName,
            partPath);
        result.OriginalDimension = result.Dimension;
        result.OriginalStyleName = result.StyleName;
        return result;
    }

    internal static OdfChartRegressionCurve LoadRegressionCurve(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfChartRegressionCurve result = new OdfChartRegressionCurve();
        result.Node = node;
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.StyleMetadata = ChartSeriesModel.LoadChartStyleMetadata(
            document,
            result.StyleName,
            partPath);
        OdfNode? equation = FindDirectChartChild(node, "equation");
        result.Equation = equation == null ? null : LoadEquation(equation);
        result.OriginalStyleName = result.StyleName;
        return result;
    }

    internal static void LoadStockComponents(
        OdfDocument document,
        OdfChart chart,
        OdfChartPlotArea plot)
    {
        if (plot.Node == null)
        {
            return;
        }
        int index = 0;
        while (index < plot.Node.Children.Length)
        {
            OdfNode child = plot.Node.Children[index];
            if (IsChartElement(child, "stock-gain-marker"))
            {
                plot.StockGainMarker = LoadStockComponent(
                    document,
                    chart,
                    child,
                    "stock-gain-marker");
            }
            else if (IsChartElement(child, "stock-loss-marker"))
            {
                plot.StockLossMarker = LoadStockComponent(
                    document,
                    chart,
                    child,
                    "stock-loss-marker");
            }
            else if (IsChartElement(child, "stock-range-line"))
            {
                plot.StockRangeLine = LoadStockComponent(
                    document,
                    chart,
                    child,
                    "stock-range-line");
            }
            index = index + 1;
        }
    }

    internal static void SynchronizeSeriesElements(OdfChartSeries series)
    {
        int index = 0;
        while (index < series.ErrorIndicators.Length)
        {
            WriteErrorIndicator(series.ErrorIndicators[index]);
            index = index + 1;
        }
        index = 0;
        while (index < series.RegressionCurves.Length)
        {
            WriteRegressionCurve(series.RegressionCurves[index]);
            index = index + 1;
        }
    }

    internal static void SynchronizeStockComponents(
        OdfChartPlotArea plot)
    {
        WriteStockComponent(plot.StockGainMarker);
        WriteStockComponent(plot.StockLossMarker);
        WriteStockComponent(plot.StockRangeLine);
    }

    static OdfChartEquation LoadEquation(OdfNode node)
    {
        OdfChartEquation result = new OdfChartEquation();
        result.Node = node;
        result.Text = TextContent(node);
        result.AutomaticContentValue = Read(
            node,
            OdfNamespaces.Chart,
            "automatic-content");
        result.DisplayRSquareValue = Read(
            node,
            OdfNamespaces.Chart,
            "display-r-square");
        result.DisplayEquationValue = Read(
            node,
            OdfNamespaces.Chart,
            "display-equation");
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        RefreshEquationBooleans(result);
        CaptureEquationOriginalValues(result);
        return result;
    }

    static OdfChartStockComponent LoadStockComponent(
        OdfDocument document,
        OdfChart chart,
        OdfNode node,
        string elementName)
    {
        OdfChartStockComponent result = new OdfChartStockComponent();
        result.Node = node;
        result.ElementName = elementName;
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.StyleMetadata = ChartSeriesModel.LoadChartStyleMetadata(
            document,
            result.StyleName,
            chart.PartPath);
        result.OriginalStyleName = result.StyleName;
        return result;
    }

    static void WriteErrorIndicator(OdfChartErrorIndicator? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Chart, "dimension", OdfNamespaces.ChartPrefix, item.Dimension, item.OriginalDimension);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        item.OriginalDimension = item.Dimension;
        item.OriginalStyleName = item.StyleName;
    }

    static void WriteRegressionCurve(OdfChartRegressionCurve? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        WriteEquation(item.Equation);
        item.OriginalStyleName = item.StyleName;
    }

    static void WriteEquation(OdfChartEquation? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        PrepareEquationBooleanValues(item);
        WriteChanged(item.Node, OdfNamespaces.Chart, "automatic-content", OdfNamespaces.ChartPrefix, item.AutomaticContentValue, item.OriginalAutomaticContentValue);
        WriteChanged(item.Node, OdfNamespaces.Chart, "display-r-square", OdfNamespaces.ChartPrefix, item.DisplayRSquareValue, item.OriginalDisplayRSquareValue);
        WriteChanged(item.Node, OdfNamespaces.Chart, "display-equation", OdfNamespaces.ChartPrefix, item.DisplayEquationValue, item.OriginalDisplayEquationValue);
        WriteChanged(item.Node, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, item.X, item.OriginalX);
        WriteChanged(item.Node, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, item.Y, item.OriginalY);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        if (item.Text != item.OriginalText)
        {
            SetEquationText(item.Node, item.Text);
        }
        RefreshEquationBooleans(item);
        CaptureEquationOriginalValues(item);
    }

    static void WriteStockComponent(OdfChartStockComponent? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        item.OriginalStyleName = item.StyleName;
    }

    static void PrepareEquationBooleanValues(OdfChartEquation item)
    {
        if (item.IsAutomaticContent != item.OriginalIsAutomaticContent
            && item.AutomaticContentValue
                == item.OriginalAutomaticContentValue)
            item.AutomaticContentValue = item.IsAutomaticContent
                ? "true"
                : "false";
        if (item.DisplaysRSquare != item.OriginalDisplaysRSquare
            && item.DisplayRSquareValue
                == item.OriginalDisplayRSquareValue)
            item.DisplayRSquareValue = item.DisplaysRSquare
                ? "true"
                : "false";
        if (item.DisplaysEquation != item.OriginalDisplaysEquation
            && item.DisplayEquationValue
                == item.OriginalDisplayEquationValue)
            item.DisplayEquationValue = item.DisplaysEquation
                ? "true"
                : "false";
    }

    static void RefreshEquationBooleans(OdfChartEquation item)
    {
        OdfLexicalValue automatic = OdfLexicalParser.ParseBoolean(
            item.AutomaticContentValue);
        item.IsAutomaticContent =
            automatic.IsValid && automatic.BooleanValue;
        item.IsAutomaticContentValid =
            item.AutomaticContentValue.Length == 0 || automatic.IsValid;
        OdfLexicalValue square = OdfLexicalParser.ParseBoolean(
            item.DisplayRSquareValue);
        item.DisplaysRSquare = square.IsValid && square.BooleanValue;
        item.IsDisplayRSquareValid =
            item.DisplayRSquareValue.Length == 0 || square.IsValid;
        OdfLexicalValue equation = OdfLexicalParser.ParseBoolean(
            item.DisplayEquationValue);
        item.DisplaysEquation = equation.IsValid && equation.BooleanValue;
        item.IsDisplayEquationValid =
            item.DisplayEquationValue.Length == 0 || equation.IsValid;
    }

    static void SetEquationText(OdfNode parent, string value)
    {
        OdfNode? paragraph = FindDirectTextChild(parent);
        if (paragraph == null)
        {
            paragraph = new OdfNode();
            paragraph.Kind = OdfNodeKind.Element;
            paragraph.Name = StyleDocumentModel.QualifiedName(
                OdfNamespaces.Text,
                "p",
                OdfNamespaces.TextPrefix);
            paragraph.Location.PartPath = parent.Location.PartPath;
            AppendEquationChild(parent, paragraph);
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = paragraph;
        text.Location.PartPath = paragraph.Location.PartPath;
        paragraph.Children = [text];
        paragraph.WasEmptyElement = false;
    }

    static void AppendEquationChild(OdfNode parent, OdfNode child)
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
        AppendEquationText(node, result);
        return result.ToString();
    }

    static void AppendEquationText(OdfNode node, StringBuilder result)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendEquationText(node.Children[index], result);
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

    static void CaptureEquationOriginalValues(OdfChartEquation item)
    {
        item.OriginalText = item.Text;
        item.OriginalAutomaticContentValue = item.AutomaticContentValue;
        item.OriginalIsAutomaticContent = item.IsAutomaticContent;
        item.OriginalDisplayRSquareValue = item.DisplayRSquareValue;
        item.OriginalDisplaysRSquare = item.DisplaysRSquare;
        item.OriginalDisplayEquationValue = item.DisplayEquationValue;
        item.OriginalDisplaysEquation = item.DisplaysEquation;
        item.OriginalX = item.X;
        item.OriginalY = item.Y;
        item.OriginalStyleName = item.StyleName;
    }
}
