namespace chart;

internal static class ChartDocumentCompositionOperations
{
    internal static ChartRenderScene Render(ChartDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        ChartLayoutResult layout = ChartLayoutEngineOperations.Layout(document);
        ChartRenderScene scene = document.CreateScene();
        bool scaled = document.DevicePixelRatio != 1.0;
        if (scaled)
        {
            scene.PushTransform("document-device-scale", ChartMatrix.Scale(document.DevicePixelRatio, document.DevicePixelRatio));
        }

        RenderPlots(scene, document, layout);
        RenderCompositionGuides(scene, document, layout);
        RenderDocumentText(scene, document, layout);
        if (scaled)
        {
            scene.PopTransform();
        }

        return scene;
    }

    internal static void RenderPlots(ChartRenderScene scene, ChartDocument document, ChartLayoutResult layout)
    {
        string[] renderedAxes = new string[AxisTotal(document)];
        int renderedAxisCount = 0;
        int plotIndex = 0;
        while (plotIndex < document.PlotCount)
        {
            ChartPlot plot = document.PlotAt(plotIndex);
            ChartRect plotBounds = layout.ItemById(plot.Id).Bounds;
            scene.BeginGroup(plot.Id);
            if (plot.HasBackground)
            {
                scene.AddRect(plot.Id + "-background", plotBounds.X, plotBounds.Y, plotBounds.Width, plotBounds.Height, 0.0, 0.0, FillRenderStyle(plot.BackgroundColor));
            }

            int axisIndex = 0;
            while (axisIndex < plot.AxisCount)
            {
                ChartAxis gridAxis = PrepareAxis(document, layout, plot, plot.AxisAt(axisIndex), plotBounds);
                RenderAxisGrid(scene, gridAxis, plot.Id + "-" + gridAxis.Id);
                axisIndex = axisIndex + 1;
            }

            RenderPlotSeries(scene, plot, plotBounds, false);
            axisIndex = 0;
            while (axisIndex < plot.AxisCount)
            {
                ChartAxis axis = PrepareAxis(document, layout, plot, plot.AxisAt(axisIndex), plotBounds);
                if (!ContainsId(renderedAxes, renderedAxisCount, axis.Id))
                {
                    RenderAxisForeground(scene, axis);
                    renderedAxes[renderedAxisCount] = axis.Id;
                    renderedAxisCount = renderedAxisCount + 1;
                }

                axisIndex = axisIndex + 1;
            }

            RenderPlotSeries(scene, plot, plotBounds, true);
            scene.EndGroup();
            plotIndex = plotIndex + 1;
        }
    }

    internal static void RenderPlotSeries(ChartRenderScene scene, ChartPlot plot, ChartRect plotBounds, bool annotations)
    {
        ChartRange xDomain = ResolvePlotDomain(plot, ChartAxisKind.X);
        ChartRange yDomain = ResolvePlotDomain(plot, ChartAxisKind.Y);
        ChartCartesianTransform transform = new ChartCartesianTransform(plotBounds, xDomain, yDomain);
        if (plot.HasFixedAspectRatio)
        {
            transform.SetFixedAspectRatio(plot.FixedAspectRatio);
        }

        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        renderer.ClipToPlotArea = plot.ClipToPlotArea;
        int count = plot.SeriesCount;
        if (annotations)
        {
            count = plot.AnnotationSeriesCount;
        }

        int index = 0;
        while (index < count)
        {
            if (annotations)
            {
                renderer.AddSeries(plot.AnnotationSeries[index]);
            }
            else
            {
                renderer.AddSeries(plot.Series[index]);
            }

            index = index + 1;
        }

        renderer.RenderTo(scene);
    }

    internal static ChartRange ResolvePlotDomain(ChartPlot plot, ChartAxisKind kind)
    {
        if (plot.HasCartesianDomains)
        {
            if (kind == ChartAxisKind.X)
            {
                return new ChartRange(plot.XDomain.Minimum, plot.XDomain.Maximum);
            }

            return new ChartRange(plot.YDomain.Minimum, plot.YDomain.Maximum);
        }

        int index = 0;
        while (index < plot.AxisCount)
        {
            if (plot.Axes[index].Kind == kind)
            {
                return new ChartRange(plot.Axes[index].Domain.Minimum, plot.Axes[index].Domain.Maximum);
            }

            index = index + 1;
        }

        return new ChartRange(0.0, 1.0);
    }

    internal static ChartAxis PrepareAxis(ChartDocument document, ChartLayoutResult layout, ChartPlot plot, ChartAxis source, ChartRect plotBounds)
    {
        ChartAxis axis = source.Copy();
        axis.Id = ChartLayoutEngineOperations.AxisLayoutId(document, plot, source);
        axis.PlotArea = plotBounds;
        if (source.Kind == ChartAxisKind.X && plot.ShareXAxisGroup.Length > 0)
        {
            axis.PlotArea = SharedPlotBounds(document, layout, plot.ShareXAxisGroup, true);
        }
        else if (source.Kind == ChartAxisKind.Y && plot.ShareYAxisGroup.Length > 0)
        {
            axis.PlotArea = SharedPlotBounds(document, layout, plot.ShareYAxisGroup, false);
        }

        axis.Center = axis.PlotArea.Center();
        axis.Radius = System.Math.Min(axis.PlotArea.Width, axis.PlotArea.Height) * 0.5;
        return axis;
    }

    internal static ChartRect SharedPlotBounds(ChartDocument document, ChartLayoutResult layout, string group, bool horizontal)
    {
        ChartRect bounds = new ChartRect(0.0, 0.0, 1.0, 1.0);
        bool found = false;
        int index = 0;
        while (index < document.PlotCount)
        {
            ChartPlot plot = document.PlotAt(index);
            string candidate = plot.ShareYAxisGroup;
            if (horizontal)
            {
                candidate = plot.ShareXAxisGroup;
            }

            if (candidate == group)
            {
                ChartRect current = layout.ItemById(plot.Id).Bounds;
                if (!found)
                {
                    bounds = ChartPlot.CopyRect(current);
                    found = true;
                }
                else
                {
                    bounds = UnionRect(bounds, current);
                }
            }

            index = index + 1;
        }

        return bounds;
    }

    internal static ChartRect UnionRect(ChartRect first, ChartRect second)
    {
        double left = System.Math.Min(first.Left(), second.Left());
        double top = System.Math.Min(first.Top(), second.Top());
        double right = System.Math.Max(first.Right(), second.Right());
        double bottom = System.Math.Max(first.Bottom(), second.Bottom());
        return new ChartRect(left, top, right - left, bottom - top);
    }

    internal static void RenderAxisGrid(ChartRenderScene scene, ChartAxis axis, string idPrefix)
    {
        if (axis.Kind != ChartAxisKind.X && axis.Kind != ChartAxisKind.Y)
        {
            return;
        }

        if (axis.ShowGridBands)
        {
            RenderAxisBands(scene, axis, idPrefix);
        }

        if (axis.ShowGridLines)
        {
            ChartRenderStyle gridStyle = StrokeRenderStyle(axis.GridColor, axis.GridStrokeWidth);
            int index = 0;
            while (index < axis.Ticks.Length)
            {
                if (ChartAxisRenderer.IsTickVisible(axis, axis.Ticks[index].Value))
                {
                    if (axis.Kind == ChartAxisKind.X)
                    {
                        double x = ChartAxisRenderer.MapX(axis, axis.Ticks[index].Value);
                        scene.AddLine(idPrefix + "-grid-" + IndexText(index), x, axis.PlotArea.Top(), x, axis.PlotArea.Bottom(), gridStyle);
                    }
                    else
                    {
                        double y = ChartAxisRenderer.MapY(axis, axis.Ticks[index].Value);
                        scene.AddLine(idPrefix + "-grid-" + IndexText(index), axis.PlotArea.Left(), y, axis.PlotArea.Right(), y, gridStyle);
                    }
                }

                index = index + 1;
            }
        }

        if (axis.ShowZeroLine && axis.Domain.Contains(0.0))
        {
            ChartRenderStyle zeroStyle = StrokeRenderStyle(axis.ZeroLineColor, axis.ZeroLineStrokeWidth);
            if (axis.Kind == ChartAxisKind.X)
            {
                double zeroX = ChartAxisRenderer.MapX(axis, 0.0);
                scene.AddLine(idPrefix + "-zero", zeroX, axis.PlotArea.Top(), zeroX, axis.PlotArea.Bottom(), zeroStyle);
            }
            else
            {
                double zeroY = ChartAxisRenderer.MapY(axis, 0.0);
                scene.AddLine(idPrefix + "-zero", axis.PlotArea.Left(), zeroY, axis.PlotArea.Right(), zeroY, zeroStyle);
            }
        }

        RenderAxisThresholds(scene, axis, idPrefix);
    }

    internal static void RenderAxisBands(ChartRenderScene scene, ChartAxis axis, string idPrefix)
    {
        ChartRenderStyle style = FillRenderStyle(axis.BandColor);
        int index = 0;
        while (index < axis.Ticks.Length - 1)
        {
            if (index % 2 == 0)
            {
                if (axis.Kind == ChartAxisKind.X)
                {
                    double firstX = ChartAxisRenderer.MapX(axis, axis.Ticks[index].Value);
                    double secondX = ChartAxisRenderer.MapX(axis, axis.Ticks[index + 1].Value);
                    scene.AddRect(idPrefix + "-band-" + IndexText(index), System.Math.Min(firstX, secondX), axis.PlotArea.Y, System.Math.Abs(secondX - firstX), axis.PlotArea.Height, 0.0, 0.0, style);
                }
                else
                {
                    double firstY = ChartAxisRenderer.MapY(axis, axis.Ticks[index].Value);
                    double secondY = ChartAxisRenderer.MapY(axis, axis.Ticks[index + 1].Value);
                    scene.AddRect(idPrefix + "-band-" + IndexText(index), axis.PlotArea.X, System.Math.Min(firstY, secondY), axis.PlotArea.Width, System.Math.Abs(secondY - firstY), 0.0, 0.0, style);
                }
            }

            index = index + 1;
        }
    }

    internal static void RenderAxisThresholds(ChartRenderScene scene, ChartAxis axis, string idPrefix)
    {
        int index = 0;
        while (index < axis.ThresholdCount)
        {
            double value = axis.ThresholdValues[index];
            if (axis.Domain.Contains(value) && ChartAxisRenderer.IsTickVisible(axis, value))
            {
                ChartRenderStyle style = StrokeRenderStyle(axis.ThresholdColors[index], axis.ThresholdWidths[index]);
                double labelX = axis.PlotArea.Left();
                double labelY = axis.PlotArea.Top();
                if (axis.Kind == ChartAxisKind.X)
                {
                    labelX = ChartAxisRenderer.MapX(axis, value);
                    scene.AddLine(idPrefix + "-threshold-" + IndexText(index), labelX, axis.PlotArea.Top(), labelX, axis.PlotArea.Bottom(), style);
                }
                else if (axis.Kind == ChartAxisKind.Y)
                {
                    labelY = ChartAxisRenderer.MapY(axis, value);
                    scene.AddLine(idPrefix + "-threshold-" + IndexText(index), axis.PlotArea.Left(), labelY, axis.PlotArea.Right(), labelY, style);
                }

                if (axis.ThresholdLabels[index].Length > 0)
                {
                    scene.AddText(idPrefix + "-threshold-label-" + IndexText(index), axis.ThresholdLabels[index], labelX, labelY, AxisTextRenderStyle(axis));
                }
            }

            index = index + 1;
        }
    }

    internal static void RenderAxisForeground(ChartRenderScene scene, ChartAxis axis)
    {
        if (axis.Kind == ChartAxisKind.X || axis.Kind == ChartAxisKind.Y)
        {
            RenderCartesianAxisForeground(scene, axis);
        }
        else if (axis.Kind == ChartAxisKind.Radial)
        {
            RenderRadialAxisForeground(scene, axis);
        }
        else if (axis.Kind == ChartAxisKind.Angular)
        {
            RenderAngularAxisForeground(scene, axis);
        }
        else if (axis.Kind == ChartAxisKind.Ternary)
        {
            scene.AddLine(axis.Id + "-rule", axis.TernaryStart.X, axis.TernaryStart.Y, axis.TernaryEnd.X, axis.TernaryEnd.Y, StrokeRenderStyle(axis.StrokeColor, axis.StrokeWidth));
        }
    }

    internal static void RenderCartesianAxisForeground(ChartRenderScene scene, ChartAxis axis)
    {
        bool horizontal = axis.Kind == ChartAxisKind.X;
        double direction = 1.0;
        double rule = axis.PlotArea.Bottom() + axis.AxisOffset;
        if (axis.Placement == ChartAxisPlacement.Top || axis.Placement == ChartAxisPlacement.Left)
        {
            direction = -1.0;
        }

        if (axis.Placement == ChartAxisPlacement.Top)
        {
            rule = axis.PlotArea.Top() - axis.AxisOffset;
        }
        else if (axis.Placement == ChartAxisPlacement.Left)
        {
            rule = axis.PlotArea.Left() - axis.AxisOffset;
        }
        else if (axis.Placement == ChartAxisPlacement.Right)
        {
            rule = axis.PlotArea.Right() + axis.AxisOffset;
        }

        ChartRenderStyle axisStyle = StrokeRenderStyle(axis.StrokeColor, axis.StrokeWidth);
        ChartRenderStyle tickStyle = StrokeRenderStyle(axis.TickColor, axis.TickStrokeWidth);
        if (horizontal)
        {
            scene.AddLine(axis.Id + "-rule", axis.PlotArea.Left(), rule, axis.PlotArea.Right(), rule, axisStyle);
        }
        else
        {
            scene.AddLine(axis.Id + "-rule", rule, axis.PlotArea.Top(), rule, axis.PlotArea.Bottom(), axisStyle);
        }

        double lastPosition = -1000000000000.0;
        double lastExtent = 0.0;
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            if (ChartAxisRenderer.IsTickVisible(axis, axis.Ticks[index].Value))
            {
                double position = ChartAxisRenderer.MapY(axis, axis.Ticks[index].Value);
                if (horizontal)
                {
                    position = ChartAxisRenderer.MapX(axis, axis.Ticks[index].Value);
                    scene.AddLine(axis.Id + "-tick-" + IndexText(index), position, rule, position, rule + axis.TickLength * direction, tickStyle);
                }
                else
                {
                    scene.AddLine(axis.Id + "-tick-" + IndexText(index), rule, position, rule + axis.TickLength * direction, position, tickStyle);
                }

                if (axis.ShowTickLabels)
                {
                    string label = ChartAxisRenderer.PrepareTickLabel(axis, axis.Ticks[index]);
                    double extent = ChartAxisRenderer.ProjectedLabelExtent(axis, label, horizontal);
                    if (ChartAxisRenderer.ShouldRenderLabel(axis, position, lastPosition, extent, lastExtent))
                    {
                        double labelX = rule + (axis.TickLength + axis.LabelOffset) * direction;
                        double labelY = position;
                        if (horizontal)
                        {
                            labelX = position;
                            labelY = rule + (axis.TickLength + axis.LabelOffset) * direction;
                        }

                        scene.AddText(axis.Id + "-label-" + IndexText(index), label, labelX, labelY, AxisTextRenderStyle(axis));
                        lastPosition = position;
                        lastExtent = extent;
                    }
                }
            }

            index = index + 1;
        }

        RenderMinorTicks(scene, axis, horizontal, rule, direction);
        if (axis.Title.Length > 0)
        {
            double titleX = rule + axis.TitleOffset * direction;
            double titleY = axis.PlotArea.Y + axis.PlotArea.Height * 0.5;
            if (horizontal)
            {
                titleX = axis.PlotArea.X + axis.PlotArea.Width * 0.5;
                titleY = rule + axis.TitleOffset * direction;
            }

            scene.AddText(axis.Id + "-title", axis.Title, titleX, titleY, AxisTextRenderStyle(axis));
        }

        if (axis.Subtitle.Length > 0)
        {
            double subtitleX = rule + (axis.TitleOffset + axis.FontSize * 1.4) * direction;
            double subtitleY = axis.PlotArea.Y + axis.PlotArea.Height * 0.5;
            if (horizontal)
            {
                subtitleX = axis.PlotArea.X + axis.PlotArea.Width * 0.5;
                subtitleY = rule + (axis.TitleOffset + axis.FontSize * 1.4) * direction;
            }

            scene.AddText(axis.Id + "-subtitle", axis.Subtitle, subtitleX, subtitleY, AxisTextRenderStyle(axis));
        }
    }

    internal static void RenderMinorTicks(ChartRenderScene scene, ChartAxis axis, bool horizontal, double rule, double direction)
    {
        if (!axis.ShowMinorTicks || axis.MinorTickCount <= 0)
        {
            return;
        }

        ChartRenderStyle style = StrokeRenderStyle(axis.MinorTickColor, axis.MinorTickStrokeWidth);
        int tickIndex = 0;
        while (tickIndex < axis.Ticks.Length - 1)
        {
            int minorIndex = 1;
            while (minorIndex <= axis.MinorTickCount)
            {
                double value = ChartAxisRenderer.MinorTickValue(axis, tickIndex, minorIndex);
                if (horizontal)
                {
                    double x = ChartAxisRenderer.MapX(axis, value);
                    scene.AddLine(axis.Id + "-minor-" + IndexText(tickIndex) + "-" + IndexText(minorIndex), x, rule, x, rule + axis.MinorTickLength * direction, style);
                }
                else
                {
                    double y = ChartAxisRenderer.MapY(axis, value);
                    scene.AddLine(axis.Id + "-minor-" + IndexText(tickIndex) + "-" + IndexText(minorIndex), rule, y, rule + axis.MinorTickLength * direction, y, style);
                }

                minorIndex = minorIndex + 1;
            }

            tickIndex = tickIndex + 1;
        }
    }

    internal static void RenderRadialAxisForeground(ChartRenderScene scene, ChartAxis axis)
    {
        ChartRenderStyle style = StrokeRenderStyle(axis.StrokeColor, axis.StrokeWidth);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            double radius = ChartAxisRenderer.MapRadius(axis, axis.Ticks[index].Value);
            if (radius > 0.0)
            {
                scene.AddCircle(axis.Id + "-ring-" + IndexText(index), axis.Center.X, axis.Center.Y, radius, style);
            }

            index = index + 1;
        }
    }

    internal static void RenderAngularAxisForeground(ChartRenderScene scene, ChartAxis axis)
    {
        ChartRenderStyle style = StrokeRenderStyle(axis.StrokeColor, axis.StrokeWidth);
        scene.AddCircle(axis.Id + "-rule", axis.Center.X, axis.Center.Y, axis.Radius, style);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            ChartPoint point = ChartAxisRenderer.PointAtAngle(axis, axis.Ticks[index].Value, axis.Radius);
            scene.AddLine(axis.Id + "-tick-" + IndexText(index), axis.Center.X, axis.Center.Y, point.X, point.Y, style);
            index = index + 1;
        }
    }

    internal static void RenderCompositionGuides(ChartRenderScene scene, ChartDocument document, ChartLayoutResult layout)
    {
        int index = 0;
        while (index < document.LegendCount)
        {
            ChartLegend legend = document.Legends[index];
            RenderLegend(scene, legend, layout.ItemById(legend.Id).Bounds);
            index = index + 1;
        }

        index = 0;
        while (index < document.ColorbarCount)
        {
            ChartLegend colorbar = document.Colorbars[index];
            RenderLegend(scene, colorbar, layout.ItemById(colorbar.Id).Bounds);
            index = index + 1;
        }

        index = 0;
        while (index < document.CompositionAnnotationCount)
        {
            ChartRect bounds = layout.ItemById(document.CompositionAnnotationIds[index]).Bounds;
            scene.AddText(document.CompositionAnnotationIds[index], document.CompositionAnnotationTexts[index], bounds.X + 4.0, bounds.Y + bounds.Height * 0.5, document.CompositionAnnotationStyles[index]);
            index = index + 1;
        }
    }

    internal static void RenderLegend(ChartRenderScene scene, ChartLegend legend, ChartRect bounds)
    {
        ChartLegendLayout layout = ChartLegendLayoutEngine.LayoutInBounds(legend, bounds);
        scene.BeginGroup(legend.Id);
        if (legend.HasBackground || legend.ShowBorder)
        {
            ChartRenderStyle frame = new ChartRenderStyle();
            if (legend.HasBackground)
            {
                frame.SetFill(legend.BackgroundColor);
            }
            else
            {
                frame.SetNoFill();
            }

            if (legend.ShowBorder)
            {
                frame.SetStroke(legend.BorderColor, legend.BorderWidth);
            }
            else
            {
                frame.SetNoStroke();
            }

            scene.AddRect(legend.Id + "-frame", bounds.X, bounds.Y, bounds.Width, bounds.Height, 0.0, 0.0, frame);
        }

        ChartRenderStyle textStyle = LegendTextRenderStyle(legend);
        if (legend.Title.Length > 0)
        {
            scene.AddText(legend.Id + "-title", legend.Title, layout.TitleBounds.X, layout.TitleBounds.Y + legend.FontSize, textStyle);
        }

        int index = 0;
        while (index < legend.EntryCount)
        {
            ChartLegendEntry entry = legend.EntryAt(index);
            ChartLegendEntryLayout entryLayout = layout.EntryAt(index);
            RenderLegendSymbol(scene, legend, entry, entryLayout.SymbolBounds);
            scene.AddText(legend.Id + "-" + entry.Id + "-label", entry.Label, entryLayout.LabelBounds.X, entryLayout.LabelBounds.Y + legend.FontSize, textStyle);
            index = index + 1;
        }

        scene.EndGroup();
    }

    internal static void RenderLegendSymbol(ChartRenderScene scene, ChartLegend legend, ChartLegendEntry entry, ChartRect bounds)
    {
        if (entry.Kind == ChartLegendEntryKind.Custom)
        {
            return;
        }

        if (entry.Kind == ChartLegendEntryKind.Gradient)
        {
            ChartRenderStyle gradient = new ChartRenderStyle();
            gradient.SetFillGradient(new ChartPoint(bounds.Left(), bounds.Top()), new ChartPoint(bounds.Right(), bounds.Top()), entry.GradientColors, entry.GradientOffsets);
            gradient.SetNoStroke();
            scene.AddRect(legend.Id + "-" + entry.Id + "-symbol", bounds.X, bounds.Y, bounds.Width, bounds.Height, 0.0, 0.0, gradient);
            return;
        }

        ChartRenderStyle style = LegendEntryRenderStyle(entry);
        double centerX = bounds.X + bounds.Width * 0.5;
        double centerY = bounds.Y + bounds.Height * 0.5;
        if (entry.Shape == ChartMarkerShape.Line)
        {
            scene.AddLine(legend.Id + "-" + entry.Id + "-symbol", bounds.Left(), centerY, bounds.Right(), centerY, style);
        }
        else
        {
            scene.AddMarker(legend.Id + "-" + entry.Id + "-symbol", entry.Shape, centerX, centerY, System.Math.Min(bounds.Width, bounds.Height), style);
        }
    }

    internal static void RenderDocumentText(ChartRenderScene scene, ChartDocument document, ChartLayoutResult layout)
    {
        RenderTextBlock(scene, layout, document.Title, "title");
        RenderTextBlock(scene, layout, document.Subtitle, "subtitle");
        RenderTextBlock(scene, layout, document.Caption, "caption");
    }

    internal static void RenderTextBlock(ChartRenderScene scene, ChartLayoutResult layout, ChartTextBlock text, string id)
    {
        if (!text.Visible)
        {
            return;
        }

        ChartRect bounds = layout.ItemById(id).Bounds;
        scene.AddTextBlock(id, text, bounds.X, bounds.Y);
    }

    internal static ChartRenderStyle LegendEntryRenderStyle(ChartLegendEntry entry)
    {
        ChartResolvedStyle resolved = new ChartResolvedStyle();
        ChartStyleCascade.ApplyStyle(resolved, ChartTheme.LibraryDefaultStyle());
        ChartStyleCascade.ApplyStyle(resolved, entry.Style);
        ChartRenderStyle style = new ChartRenderStyle();
        if (resolved.FillNone)
        {
            style.SetNoFill();
        }
        else
        {
            style.SetFill(resolved.FillColor);
        }

        if (resolved.StrokeNone)
        {
            style.SetNoStroke();
        }
        else
        {
            style.SetStroke(resolved.StrokeColor, resolved.StrokeWidth);
        }

        style.SetOpacity(resolved.Opacity);
        return style;
    }

    internal static ChartRenderStyle LegendTextRenderStyle(ChartLegend legend)
    {
        ChartRenderStyle style = FillRenderStyle(legend.TextColor);
        style.SetFont(legend.FontFamily, legend.FontSize);
        return style;
    }

    internal static ChartRenderStyle AxisTextRenderStyle(ChartAxis axis)
    {
        ChartRenderStyle style = FillRenderStyle(axis.TextColor);
        style.SetFont(axis.FontFamily, axis.FontSize);
        return style;
    }

    internal static ChartRenderStyle FillRenderStyle(ChartColor color)
    {
        ChartRenderStyle style = new ChartRenderStyle();
        style.SetFill(color);
        style.SetNoStroke();
        return style;
    }

    internal static ChartRenderStyle StrokeRenderStyle(ChartColor color, double width)
    {
        ChartRenderStyle style = new ChartRenderStyle();
        style.SetNoFill();
        style.SetStroke(color, width);
        return style;
    }

    internal static int AxisTotal(ChartDocument document)
    {
        int total = 0;
        int index = 0;
        while (index < document.PlotCount)
        {
            total = total + document.Plots[index].AxisCount;
            index = index + 1;
        }

        return total;
    }

    internal static bool ContainsId(string[] values, int count, string value)
    {
        int index = 0;
        while (index < count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string IndexText(int index)
    {
        return index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
