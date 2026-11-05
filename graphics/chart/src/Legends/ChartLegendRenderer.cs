namespace chart;

internal static class ChartLegendLayoutEngine
{
    internal static ChartSize PreferredSize(ChartLegend legend)
    {
        if (legend == null)
        {
            throw new System.ArgumentNullException("legend");
        }

        double width = 0.0;
        int index = 0;
        while (index < legend.EntryCount)
        {
            ChartLegendEntry entry = legend.EntryAt(index);
            double rowWidth = EntrySymbolWidth(legend, entry) + legend.EntrySpacing + TextWidth(entry.Label, legend.FontSize);
            if (rowWidth > width)
            {
                width = rowWidth;
            }

            index = index + 1;
        }

        if (legend.Title.Length > 0)
        {
            double titleWidth = TextWidth(legend.Title, legend.FontSize);
            if (titleWidth > width)
            {
                width = titleWidth;
            }
        }

        double height = legend.Padding * 2.0;
        if (legend.Title.Length > 0)
        {
            height = height + legend.FontSize + legend.EntrySpacing;
        }

        if (legend.EntryCount > 0)
        {
            height = height + legend.EntryCount * EntryHeight(legend) + (legend.EntryCount - 1) * legend.EntrySpacing;
        }

        return new ChartSize(width + legend.Padding * 2.0, height);
    }

    internal static ChartLegendLayout LayoutAt(ChartLegend legend, double x, double y)
    {
        ChartSize size = PreferredSize(legend);
        return LayoutInBounds(legend, new ChartRect(x, y, size.Width, size.Height));
    }

    internal static ChartLegendLayout LayoutInBounds(ChartLegend legend, ChartRect bounds)
    {
        if (legend == null)
        {
            throw new System.ArgumentNullException("legend");
        }

        ChartAxis.ValidatePlotArea(bounds);
        ChartRect titleBounds = new ChartRect(bounds.X + legend.Padding, bounds.Y + legend.Padding, 1.0, 1.0);
        ChartLegendLayout layout = new ChartLegendLayout(bounds, titleBounds);
        double cursorY = bounds.Y + legend.Padding;
        if (legend.Title.Length > 0)
        {
            titleBounds = new ChartRect(bounds.X + legend.Padding, cursorY, bounds.Width - legend.Padding * 2.0, legend.FontSize);
            layout.TitleBounds = titleBounds;
            cursorY = cursorY + legend.FontSize + legend.EntrySpacing;
        }

        double rowHeight = EntryHeight(legend);
        int index = 0;
        while (index < legend.EntryCount)
        {
            ChartLegendEntry entry = legend.EntryAt(index);
            double symbolWidth = EntrySymbolWidth(legend, entry);
            double symbolHeight = EntrySymbolHeight(legend, entry);
            double symbolX = bounds.X + legend.Padding;
            double symbolY = cursorY + (rowHeight - symbolHeight) * 0.5;
            double labelX = symbolX + symbolWidth + legend.EntrySpacing;
            ChartRect symbolBounds = new ChartRect(symbolX, symbolY, symbolWidth, symbolHeight);
            double labelWidth = bounds.Right() - labelX - legend.Padding;
            if (labelWidth <= 0.0)
            {
                labelWidth = 1.0;
            }

            ChartRect labelBounds = new ChartRect(labelX, cursorY, labelWidth, rowHeight);
            layout.AddEntry(entry.Id, symbolBounds, labelBounds);
            cursorY = cursorY + rowHeight + legend.EntrySpacing;
            index = index + 1;
        }

        return layout;
    }

    internal static double EntryHeight(ChartLegend legend)
    {
        double height = legend.SymbolSize;
        if (legend.GradientHeight > height)
        {
            height = legend.GradientHeight;
        }

        if (legend.FontSize > height)
        {
            height = legend.FontSize;
        }

        return height;
    }

    internal static double EntrySymbolWidth(ChartLegend legend, ChartLegendEntry entry)
    {
        if (entry.Kind == ChartLegendEntryKind.Gradient)
        {
            return legend.GradientWidth;
        }

        return legend.SymbolSize;
    }

    internal static double EntrySymbolHeight(ChartLegend legend, ChartLegendEntry entry)
    {
        if (entry.Kind == ChartLegendEntryKind.Gradient)
        {
            return legend.GradientHeight;
        }

        return legend.SymbolSize;
    }

    internal static double TextWidth(string text, int fontSize)
    {
        ChartTextBlock.ValidateText(text, "text");
        if (fontSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException("fontSize");
        }

        return text.Length * fontSize * 0.6;
    }
}

internal static class ChartLegendRenderer
{
    internal static void RenderSvg(SvgWriter writer, ChartLegend legend, ChartLegendLayout layout)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (legend == null)
        {
            throw new System.ArgumentNullException("legend");
        }

        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        writer.BeginGroup(legend.Id, "", null);
        RenderFrameSvg(writer, legend, layout);
        RenderTitleSvg(writer, legend, layout);
        int index = 0;
        while (index < legend.EntryCount)
        {
            RenderEntrySvg(writer, legend, legend.EntryAt(index), layout.EntryAt(index));
            index = index + 1;
        }

        writer.EndGroup();
    }

    internal static void RenderFrameSvg(SvgWriter writer, ChartLegend legend, ChartLegendLayout layout)
    {
        if (!legend.HasBackground && !legend.ShowBorder)
        {
            return;
        }

        SvgStyle style = new SvgStyle();
        if (legend.HasBackground)
        {
            style.SetFill(legend.BackgroundColor);
        }
        else
        {
            style.SetNoFill();
        }

        if (legend.ShowBorder)
        {
            style.SetStroke(legend.BorderColor, legend.BorderWidth);
        }
        else
        {
            style.SetNoStroke();
        }

        writer.WriteRect(layout.Bounds.X, layout.Bounds.Y, layout.Bounds.Width, layout.Bounds.Height, 0.0, 0.0, style, legend.Id + "-frame", "");
    }

    internal static void RenderTitleSvg(SvgWriter writer, ChartLegend legend, ChartLegendLayout layout)
    {
        if (legend.Title.Length == 0)
        {
            return;
        }

        writer.WriteText(legend.Title, layout.TitleBounds.X, layout.TitleBounds.Y + legend.FontSize, CreateTextStyle(legend), legend.Id + "-title", "");
    }

    internal static void RenderEntrySvg(SvgWriter writer, ChartLegend legend, ChartLegendEntry entry, ChartLegendEntryLayout layout)
    {
        if (entry.Kind == ChartLegendEntryKind.Gradient)
        {
            RenderGradientSvg(writer, legend, entry, layout);
        }
        else if (entry.Kind == ChartLegendEntryKind.Custom)
        {
            RenderCustomSvg(writer, entry);
        }
        else
        {
            RenderSymbolSvg(writer, entry, layout.SymbolBounds);
        }

        writer.WriteText(entry.Label, layout.LabelBounds.X, layout.LabelBounds.Y + legend.FontSize, CreateTextStyle(legend), legend.Id + "-" + entry.Id + "-label", "");
    }

    internal static void RenderSymbolSvg(SvgWriter writer, ChartLegendEntry entry, ChartRect bounds)
    {
        SvgStyle style = CreateEntryStyle(entry);
        double centerX = bounds.X + bounds.Width * 0.5;
        double centerY = bounds.Y + bounds.Height * 0.5;
        if (entry.Shape == ChartMarkerShape.Circle)
        {
            writer.WriteCircle(centerX, centerY, System.Math.Min(bounds.Width, bounds.Height) * 0.5, style, entry.Id + "-symbol", "");
        }
        else if (entry.Shape == ChartMarkerShape.Diamond)
        {
            writer.WritePolygon(new ChartPoint[] { new ChartPoint(centerX, bounds.Y), new ChartPoint(bounds.Right(), centerY), new ChartPoint(centerX, bounds.Bottom()), new ChartPoint(bounds.X, centerY) }, style, entry.Id + "-symbol", "");
        }
        else if (entry.Shape == ChartMarkerShape.Triangle)
        {
            writer.WritePolygon(new ChartPoint[] { new ChartPoint(centerX, bounds.Y), new ChartPoint(bounds.Right(), bounds.Bottom()), new ChartPoint(bounds.X, bounds.Bottom()) }, style, entry.Id + "-symbol", "");
        }
        else if (entry.Shape == ChartMarkerShape.Line)
        {
            writer.WriteLine(bounds.X, centerY, bounds.Right(), centerY, style, entry.Id + "-symbol", "");
        }
        else
        {
            writer.WriteRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0.0, 0.0, style, entry.Id + "-symbol", "");
        }
    }

    internal static void RenderGradientSvg(SvgWriter writer, ChartLegend legend, ChartLegendEntry entry, ChartLegendEntryLayout layout)
    {
        string gradientId = legend.Id + "-" + entry.Id + "-gradient";
        writer.WriteLinearGradient(gradientId, layout.SymbolBounds.X, layout.SymbolBounds.Y, layout.SymbolBounds.Right(), layout.SymbolBounds.Y, entry.GradientColors, entry.GradientOffsets);
        AppendGradientRect(writer, legend.Id + "-" + entry.Id + "-symbol", layout.SymbolBounds, gradientId);
    }

    internal static void RenderCustomSvg(SvgWriter writer, ChartLegendEntry entry)
    {
        writer.AppendRawElement(entry.CustomMarkup);
    }

    internal static void AppendGradientRect(SvgWriter writer, string id, ChartRect bounds, string gradientId)
    {
        writer.AppendRawElement("<rect id=\"" + SvgWriter.EscapeAttribute(id) + "\" x=\"" + SvgWriter.FormatDouble(bounds.X) + "\" y=\"" + SvgWriter.FormatDouble(bounds.Y) + "\" width=\"" + SvgWriter.FormatDouble(bounds.Width) + "\" height=\"" + SvgWriter.FormatDouble(bounds.Height) + "\" fill=\"url(#" + SvgWriter.EscapeAttribute(gradientId) + ")\"/>");
    }

    internal static SvgStyle CreateEntryStyle(ChartLegendEntry entry)
    {
        if (entry == null)
        {
            throw new System.ArgumentNullException("entry");
        }

        ChartResolvedStyle resolved = new ChartResolvedStyle();
        ChartStyleCascade.ApplyStyle(resolved, ChartTheme.LibraryDefaultStyle());
        ChartStyleCascade.ApplyStyle(resolved, entry.Style);
        return resolved.ToSvgStyle();
    }

    internal static SvgStyle CreateTextStyle(ChartLegend legend)
    {
        if (legend == null)
        {
            throw new System.ArgumentNullException("legend");
        }

        SvgStyle style = new SvgStyle();
        style.SetFill(legend.TextColor);
        style.SetNoStroke();
        style.SetFont(legend.FontFamily, legend.FontSize);
        return style;
    }
}
