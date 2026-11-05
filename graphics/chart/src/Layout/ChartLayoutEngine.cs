namespace chart;
internal static class ChartLayoutEngineOperations
{
    /// <summary>
    /// Computes the layout for the configured value within the supplied bounds.
    /// </summary>
    /// <param name = "document">The chart document whose model, layout, or output is processed.</param>
    /// <returns>The resulting layout result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartLayoutResult Layout(ChartDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        ChartRect figure = document.FigureRect();
        ChartRect content = document.ContentRect();
        ChartRect remaining = ChartPlot.CopyRect(content);
        ChartLayoutResult result = new ChartLayoutResult(figure, content, content);
        result.AddItem(ChartLayoutItemKind.Figure, "figure", figure, ChartSide.Top);
        result.AddItem(ChartLayoutItemKind.Content, "content", content, ChartSide.Top);
        remaining = ReserveTextBlock(result, remaining, document.Title, ChartLayoutItemKind.Title, "title", ChartSide.Top);
        remaining = ReserveTextBlock(result, remaining, document.Subtitle, ChartLayoutItemKind.Subtitle, "subtitle", ChartSide.Top);
        remaining = ReserveTextBlock(result, remaining, document.Caption, ChartLayoutItemKind.Caption, "caption", ChartSide.Bottom);
        int index = 0;
        while (index < document.LayoutRequestCount)
        {
            remaining = ReserveRequest(result, remaining, document.LayoutRequests[index]);
            index = index + 1;
        }

        ValidateRemaining(remaining);
        result.PlotArea = ChartPlot.CopyRect(remaining);
        AddPlotItems(result, document, remaining);
        return result;
    }

    internal static ChartRect ReserveTextBlock(ChartLayoutResult result, ChartRect remaining, ChartTextBlock text, ChartLayoutItemKind kind, string id, ChartSide side)
    {
        if (!text.Visible)
        {
            return remaining;
        }

        double height = TextBlockHeight(text);
        ChartRect bounds = new ChartRect(remaining.X, remaining.Y, remaining.Width, text.FontSize);
        ChartRect next = new ChartRect(remaining.X, remaining.Y + height, remaining.Width, remaining.Height - height);
        if (side == ChartSide.Bottom)
        {
            bounds = new ChartRect(remaining.X, remaining.Bottom() - text.FontSize, remaining.Width, text.FontSize);
            next = new ChartRect(remaining.X, remaining.Y, remaining.Width, remaining.Height - height);
        }

        ValidateRemaining(next);
        result.AddItem(kind, id, bounds, side);
        return next;
    }

    internal static ChartRect ReserveRequest(ChartLayoutResult result, ChartRect remaining, ChartLayoutRequest request)
    {
        ChartRect bounds = RequestBounds(remaining, request);
        result.AddItem(request.Kind, request.Id, bounds, request.Side);
        if (request.Overlay)
        {
            return remaining;
        }

        ChartRect next = RemainingAfterRequest(remaining, request);
        ValidateRemaining(next);
        return next;
    }

    internal static ChartRect RequestBounds(ChartRect remaining, ChartLayoutRequest request)
    {
        double width = RequestWidth(remaining, request);
        double height = RequestHeight(remaining, request);
        if (request.Side == ChartSide.Left)
        {
            return new ChartRect(remaining.Left(), remaining.Top(), width, height);
        }

        if (request.Side == ChartSide.Right)
        {
            return new ChartRect(remaining.Right() - width, remaining.Top(), width, height);
        }

        if (request.Side == ChartSide.Top)
        {
            return new ChartRect(remaining.Left(), remaining.Top(), width, height);
        }

        return new ChartRect(remaining.Left(), remaining.Bottom() - height, width, height);
    }

    internal static ChartRect RemainingAfterRequest(ChartRect remaining, ChartLayoutRequest request)
    {
        if (request.Side == ChartSide.Left)
        {
            double amount = RequestWidth(remaining, request) + request.Spacing;
            return new ChartRect(remaining.X + amount, remaining.Y, remaining.Width - amount, remaining.Height);
        }

        if (request.Side == ChartSide.Right)
        {
            double amount = RequestWidth(remaining, request) + request.Spacing;
            return new ChartRect(remaining.X, remaining.Y, remaining.Width - amount, remaining.Height);
        }

        if (request.Side == ChartSide.Top)
        {
            double amount = RequestHeight(remaining, request) + request.Spacing;
            return new ChartRect(remaining.X, remaining.Y + amount, remaining.Width, remaining.Height - amount);
        }

        double bottomAmount = RequestHeight(remaining, request) + request.Spacing;
        return new ChartRect(remaining.X, remaining.Y, remaining.Width, remaining.Height - bottomAmount);
    }

    internal static double RequestWidth(ChartRect remaining, ChartLayoutRequest request)
    {
        if (request.Width > 0.0)
        {
            return request.Width;
        }

        return remaining.Width;
    }

    internal static double RequestHeight(ChartRect remaining, ChartLayoutRequest request)
    {
        if (request.Height > 0.0)
        {
            return request.Height;
        }

        return remaining.Height;
    }

    internal static double TextBlockHeight(ChartTextBlock text)
    {
        return text.FontSize + text.Spacing;
    }

    internal static void AddPlotItems(ChartLayoutResult result, ChartDocument document, ChartRect plotArea)
    {
        if (document.PlotCount == 0)
        {
            result.AddItem(ChartLayoutItemKind.Plot, "plot", plotArea, ChartSide.Top);
            return;
        }

        int index = 0;
        while (index < document.PlotCount)
        {
            ChartPlot plot = document.PlotAt(index);
            ChartRect cell = PlotCell(result, document, plotArea, plot, index);
            ChartRect bounds = plot.Margins.DeflateRect(cell);
            ValidateRemaining(bounds);
            result.AddItem(ChartLayoutItemKind.Plot, plot.Id, bounds, ChartSide.Top);
            AddAxisItems(result, document, plot, bounds);
            index = index + 1;
        }
    }

    internal static ChartRect PlotCell(ChartLayoutResult result, ChartDocument document, ChartRect plotArea, ChartPlot plot, int index)
    {
        if (plot.IsInset)
        {
            return InsetPlotBounds(result, plot);
        }

        if (plot.HasGridPosition)
        {
            return SpanCell(document.Grid, plotArea, plot.GridRow, plot.GridColumn, plot.RowSpan, plot.ColumnSpan);
        }

        return SequentialPlotCell(document, plotArea, index);
    }

    internal static ChartRect SequentialPlotCell(ChartDocument document, ChartRect plotArea, int index)
    {
        int row = index / document.Grid.Columns;
        int column = index - row * document.Grid.Columns;
        if (row >= document.Grid.Rows)
        {
            row = document.Grid.Rows - 1;
            column = document.Grid.Columns - 1;
        }

        return document.Grid.CellRect(plotArea, row, column);
    }

    internal static ChartRect SpanCell(ChartGrid grid, ChartRect plotArea, int row, int column, int rowSpan, int columnSpan)
    {
        ChartPlot.ValidateGridPosition(row, column, rowSpan, columnSpan);
        if (row + rowSpan > grid.Rows)
        {
            throw new System.ArgumentOutOfRangeException("rowSpan");
        }

        if (column + columnSpan > grid.Columns)
        {
            throw new System.ArgumentOutOfRangeException("columnSpan");
        }

        ChartRect start = grid.CellRect(plotArea, row, column);
        ChartRect end = grid.CellRect(plotArea, row + rowSpan - 1, column + columnSpan - 1);
        double right = end.Right();
        double bottom = end.Bottom();
        return new ChartRect(start.X, start.Y, right - start.X, bottom - start.Y);
    }

    internal static ChartRect InsetPlotBounds(ChartLayoutResult result, ChartPlot plot)
    {
        ChartLayoutItem parent = result.ItemById(plot.InsetParentId);
        double x = parent.Bounds.X + parent.Bounds.Width * plot.InsetBounds.X;
        double y = parent.Bounds.Y + parent.Bounds.Height * plot.InsetBounds.Y;
        double width = parent.Bounds.Width * plot.InsetBounds.Width;
        double height = parent.Bounds.Height * plot.InsetBounds.Height;
        return new ChartRect(x, y, width, height);
    }

    internal static void AddAxisItems(ChartLayoutResult result, ChartDocument document, ChartPlot plot, ChartRect plotBounds)
    {
        int index = 0;
        while (index < plot.AxisCount)
        {
            ChartAxis axis = plot.AxisAt(index);
            ChartRect bounds = AxisBounds(axis, plotBounds);
            string id = AxisLayoutId(document, plot, axis);
            if (result.ItemIndex(id) < 0)
            {
                result.AddItem(ChartLayoutItemKind.Axis, id, bounds, AxisSide(axis));
            }

            index = index + 1;
        }
    }

    internal static string AxisLayoutId(ChartDocument document, ChartPlot plot, ChartAxis axis)
    {
        if (axis.Kind == ChartAxisKind.X && plot.ShareXAxisGroup.Length > 0)
        {
            return "shared-x-" + plot.ShareXAxisGroup;
        }

        if (axis.Kind == ChartAxisKind.Y && plot.ShareYAxisGroup.Length > 0)
        {
            return "shared-y-" + plot.ShareYAxisGroup;
        }

        if (document.PlotCount == 1)
        {
            return axis.Id;
        }

        return plot.Id + "-" + axis.Id;
    }

    internal static ChartRect AxisBounds(ChartAxis axis, ChartRect plotBounds)
    {
        double thickness = AxisThickness(axis);
        if (axis.Placement == ChartAxisPlacement.Bottom)
        {
            return new ChartRect(plotBounds.X, plotBounds.Bottom(), plotBounds.Width, thickness);
        }

        if (axis.Placement == ChartAxisPlacement.Top)
        {
            return new ChartRect(plotBounds.X, plotBounds.Y - thickness, plotBounds.Width, thickness);
        }

        if (axis.Placement == ChartAxisPlacement.Left)
        {
            return new ChartRect(plotBounds.X - thickness, plotBounds.Y, thickness, plotBounds.Height);
        }

        if (axis.Placement == ChartAxisPlacement.Right)
        {
            return new ChartRect(plotBounds.Right(), plotBounds.Y, thickness, plotBounds.Height);
        }

        return plotBounds;
    }

    internal static ChartSide AxisSide(ChartAxis axis)
    {
        if (axis.Placement == ChartAxisPlacement.Left)
        {
            return ChartSide.Left;
        }

        if (axis.Placement == ChartAxisPlacement.Right)
        {
            return ChartSide.Right;
        }

        if (axis.Placement == ChartAxisPlacement.Bottom)
        {
            return ChartSide.Bottom;
        }

        return ChartSide.Top;
    }

    internal static double AxisThickness(ChartAxis axis)
    {
        double thickness = axis.TickLength;
        if (axis.ShowTickLabels)
        {
            thickness = thickness + axis.LabelOffset + axis.FontSize;
        }

        if (axis.Title.Length > 0)
        {
            thickness = System.Math.Max(thickness, axis.TitleOffset + axis.FontSize);
        }

        if (thickness <= 0.0)
        {
            thickness = 1.0;
        }

        return thickness;
    }

    internal static void ValidateRemaining(ChartRect rect)
    {
        ChartAxis.ValidatePlotArea(rect);
    }
}
