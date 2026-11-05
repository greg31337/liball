namespace chart;

internal static class ChartAxisRenderer
{
    internal static void RenderSvg(SvgWriter writer, ChartAxis axis)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (axis == null)
        {
            throw new System.ArgumentNullException("axis");
        }

        SvgStyle style = CreateAxisStyle(axis);
        writer.BeginGroup(axis.Id, "", null);
        if (axis.Kind == ChartAxisKind.Radial)
        {
            RenderRadialSvg(writer, axis, style);
        }
        else if (axis.Kind == ChartAxisKind.Angular)
        {
            RenderAngularSvg(writer, axis, style);
        }
        else if (axis.Kind == ChartAxisKind.Ternary)
        {
            RenderTernarySvg(writer, axis, style);
        }
        else
        {
            RenderCartesianSvg(writer, axis, style);
        }

        writer.EndGroup();
    }

    internal static void RenderCartesianSvg(SvgWriter writer, ChartAxis axis, SvgStyle style)
    {
        if (axis.Placement == ChartAxisPlacement.Bottom)
        {
            RenderHorizontalSvg(writer, axis, style, axis.PlotArea.Bottom() + axis.AxisOffset, 1.0);
            return;
        }

        if (axis.Placement == ChartAxisPlacement.Top)
        {
            RenderHorizontalSvg(writer, axis, style, axis.PlotArea.Top() - axis.AxisOffset, -1.0);
            return;
        }

        if (axis.Placement == ChartAxisPlacement.Left)
        {
            RenderVerticalSvg(writer, axis, style, axis.PlotArea.Left() - axis.AxisOffset, -1.0);
            return;
        }

        if (axis.Placement == ChartAxisPlacement.Right)
        {
            RenderVerticalSvg(writer, axis, style, axis.PlotArea.Right() + axis.AxisOffset, 1.0);
        }
    }

    internal static void RenderHorizontalSvg(SvgWriter writer, ChartAxis axis, SvgStyle style, double y, double direction)
    {
        RenderHorizontalBandsSvg(writer, axis);
        RenderHorizontalGridSvg(writer, axis);
        RenderHorizontalZeroLineSvg(writer, axis);
        RenderThresholdLinesSvg(writer, axis);
        writer.WriteLine(axis.PlotArea.Left(), y, axis.PlotArea.Right(), y, style, axis.Id + "-rule", "");
        SvgStyle tickStyle = CreateTickStyle(axis);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            if (IsTickVisible(axis, axis.Ticks[index].Value))
            {
                double x = MapX(axis, axis.Ticks[index].Value);
                writer.WriteLine(x, y, x, y + axis.TickLength * direction, tickStyle, TickId(axis, index), "");
            }

            index = index + 1;
        }

        RenderHorizontalMinorTicksSvg(writer, axis, CreateMinorTickStyle(axis), y, direction);
        RenderHorizontalBreakMarksSvg(writer, axis, style, y);
        RenderHorizontalLabelsSvg(writer, axis, y, direction);
        RenderHorizontalTitleSvg(writer, axis, y, direction);
    }

    internal static void RenderVerticalSvg(SvgWriter writer, ChartAxis axis, SvgStyle style, double x, double direction)
    {
        RenderVerticalBandsSvg(writer, axis);
        RenderVerticalGridSvg(writer, axis);
        RenderVerticalZeroLineSvg(writer, axis);
        RenderThresholdLinesSvg(writer, axis);
        writer.WriteLine(x, axis.PlotArea.Top(), x, axis.PlotArea.Bottom(), style, axis.Id + "-rule", "");
        SvgStyle tickStyle = CreateTickStyle(axis);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            if (IsTickVisible(axis, axis.Ticks[index].Value))
            {
                double y = MapY(axis, axis.Ticks[index].Value);
                writer.WriteLine(x, y, x + axis.TickLength * direction, y, tickStyle, TickId(axis, index), "");
            }

            index = index + 1;
        }

        RenderVerticalMinorTicksSvg(writer, axis, CreateMinorTickStyle(axis), x, direction);
        RenderVerticalBreakMarksSvg(writer, axis, style, x);
        RenderVerticalLabelsSvg(writer, axis, x, direction);
        RenderVerticalTitleSvg(writer, axis, x, direction);
    }

    internal static void RenderRadialSvg(SvgWriter writer, ChartAxis axis, SvgStyle style)
    {
        RenderThresholdLinesSvg(writer, axis);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            double radius = MapRadius(axis, axis.Ticks[index].Value);
            if (radius > 0.0)
            {
                writer.WriteCircle(axis.Center.X, axis.Center.Y, radius, style, RingId(axis, index), "");
            }

            index = index + 1;
        }

        RenderRadialLabelsSvg(writer, axis);
        RenderPolarTitleSvg(writer, axis);
    }

    internal static void RenderAngularSvg(SvgWriter writer, ChartAxis axis, SvgStyle style)
    {
        RenderThresholdLinesSvg(writer, axis);
        writer.WriteCircle(axis.Center.X, axis.Center.Y, axis.Radius, style, axis.Id + "-rule", "");
        SvgStyle tickStyle = CreateTickStyle(axis);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            ChartPoint point = PointAtAngle(axis, axis.Ticks[index].Value, axis.Radius);
            writer.WriteLine(axis.Center.X, axis.Center.Y, point.X, point.Y, tickStyle, TickId(axis, index), "");
            index = index + 1;
        }

        RenderAngularLabelsSvg(writer, axis);
        RenderPolarTitleSvg(writer, axis);
    }

    internal static void RenderTernarySvg(SvgWriter writer, ChartAxis axis, SvgStyle style)
    {
        ChartPoint normal = TernaryOutwardNormal(axis);
        ChartPoint start = new ChartPoint(axis.TernaryStart.X + normal.X * axis.AxisOffset, axis.TernaryStart.Y + normal.Y * axis.AxisOffset);
        ChartPoint end = new ChartPoint(axis.TernaryEnd.X + normal.X * axis.AxisOffset, axis.TernaryEnd.Y + normal.Y * axis.AxisOffset);
        writer.WriteLine(start.X, start.Y, end.X, end.Y, style, axis.Id + "-rule", "");
        SvgStyle tickStyle = CreateTickStyle(axis);
        SvgStyle textStyle = CreateTextStyle(axis);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            if (IsTickVisible(axis, axis.Ticks[index].Value))
            {
                ChartPoint point = TernaryPoint(axis, axis.Ticks[index].Value);
                point.X = point.X + normal.X * axis.AxisOffset;
                point.Y = point.Y + normal.Y * axis.AxisOffset;
                writer.WriteLine(point.X, point.Y, point.X + normal.X * axis.TickLength, point.Y + normal.Y * axis.TickLength, tickStyle, TickId(axis, index), "");
                if (axis.ShowTickLabels)
                {
                    string label = PrepareTickLabel(axis, axis.Ticks[index]);
                    double labelDistance = axis.TickLength + axis.LabelOffset;
                    WriteTickLabel(writer, axis, label, point.X + normal.X * labelDistance, point.Y + normal.Y * labelDistance, textStyle, LabelId(axis, index));
                }
            }

            index = index + 1;
        }

        RenderThresholdLinesSvg(writer, axis);
        if (axis.Title.Length > 0 || axis.Subtitle.Length > 0)
        {
            double middleX = (start.X + end.X) * 0.5;
            double middleY = (start.Y + end.Y) * 0.5;
            if (axis.Title.Length > 0)
            {
                writer.WriteText(axis.Title, middleX + normal.X * axis.TitleOffset, middleY + normal.Y * axis.TitleOffset, textStyle, axis.Id + "-title", "");
            }

            if (axis.Subtitle.Length > 0)
            {
                double distance = axis.TitleOffset + axis.FontSize * 1.4;
                writer.WriteText(axis.Subtitle, middleX + normal.X * distance, middleY + normal.Y * distance, textStyle, axis.Id + "-subtitle", "");
            }
        }
    }

    internal static void RenderThresholdLinesSvg(SvgWriter writer, ChartAxis axis)
    {
        int index = 0;
        while (index < axis.ThresholdCount)
        {
            double value = axis.ThresholdValues[index];
            if (value >= axis.Domain.Minimum && value <= axis.Domain.Maximum && IsTickVisible(axis, value))
            {
                SvgStyle style = new SvgStyle();
                style.SetNoFill();
                style.SetStroke(axis.ThresholdColors[index], axis.ThresholdWidths[index]);
                double labelX = axis.PlotArea.Left();
                double labelY = axis.PlotArea.Top();
                if (axis.Kind == ChartAxisKind.X)
                {
                    double x = MapX(axis, value);
                    writer.WriteLine(x, axis.PlotArea.Top(), x, axis.PlotArea.Bottom(), style, ThresholdId(axis, index), "");
                    labelX = x;
                }
                else if (axis.Kind == ChartAxisKind.Y)
                {
                    double y = MapY(axis, value);
                    writer.WriteLine(axis.PlotArea.Left(), y, axis.PlotArea.Right(), y, style, ThresholdId(axis, index), "");
                    labelY = y;
                }
                else if (axis.Kind == ChartAxisKind.Radial)
                {
                    double radius = MapRadius(axis, value);
                    if (radius > 0.0)
                    {
                        writer.WriteCircle(axis.Center.X, axis.Center.Y, radius, style, ThresholdId(axis, index), "");
                    }

                    labelX = axis.Center.X + radius;
                    labelY = axis.Center.Y;
                }
                else if (axis.Kind == ChartAxisKind.Angular)
                {
                    ChartPoint angularPoint = PointAtAngle(axis, value, axis.Radius);
                    writer.WriteLine(axis.Center.X, axis.Center.Y, angularPoint.X, angularPoint.Y, style, ThresholdId(axis, index), "");
                    labelX = angularPoint.X;
                    labelY = angularPoint.Y;
                }
                else
                {
                    ChartPoint ternaryPoint = TernaryPoint(axis, value);
                    writer.WriteLine(ternaryPoint.X, ternaryPoint.Y, axis.TernaryOpposite.X, axis.TernaryOpposite.Y, style, ThresholdId(axis, index), "");
                    labelX = ternaryPoint.X;
                    labelY = ternaryPoint.Y;
                }

                if (axis.ThresholdLabels[index].Length > 0)
                {
                    writer.WriteText(axis.ThresholdLabels[index], labelX, labelY, CreateTextStyle(axis), ThresholdId(axis, index) + "-label", "");
                }
            }

            index = index + 1;
        }
    }

    internal static void RenderHorizontalBreakMarksSvg(SvgWriter writer, ChartAxis axis, SvgStyle style, double y)
    {
        int index = 0;
        while (index < axis.BreakCount)
        {
            double x = MapX(axis, axis.BreakMinimums[index]);
            writer.WriteLine(x - 4.0, y - 3.0, x, y + 3.0, style, BreakId(axis, index) + "-a", "");
            writer.WriteLine(x, y - 3.0, x + 4.0, y + 3.0, style, BreakId(axis, index) + "-b", "");
            index = index + 1;
        }
    }

    internal static void RenderVerticalBreakMarksSvg(SvgWriter writer, ChartAxis axis, SvgStyle style, double x)
    {
        int index = 0;
        while (index < axis.BreakCount)
        {
            double y = MapY(axis, axis.BreakMinimums[index]);
            writer.WriteLine(x - 3.0, y - 4.0, x + 3.0, y, style, BreakId(axis, index) + "-a", "");
            writer.WriteLine(x - 3.0, y, x + 3.0, y + 4.0, style, BreakId(axis, index) + "-b", "");
            index = index + 1;
        }
    }

    internal static ChartPoint TernaryOutwardNormal(ChartAxis axis)
    {
        double dx = axis.TernaryEnd.X - axis.TernaryStart.X;
        double dy = axis.TernaryEnd.Y - axis.TernaryStart.Y;
        double length = System.Math.Sqrt(dx * dx + dy * dy);
        double nx = -dy / length;
        double ny = dx / length;
        double middleX = (axis.TernaryStart.X + axis.TernaryEnd.X) * 0.5;
        double middleY = (axis.TernaryStart.Y + axis.TernaryEnd.Y) * 0.5;
        double towardOpposite = nx * (axis.TernaryOpposite.X - middleX) + ny * (axis.TernaryOpposite.Y - middleY);
        if (towardOpposite > 0.0)
        {
            nx = -nx;
            ny = -ny;
        }

        return new ChartPoint(nx, ny);
    }

    internal static ChartPoint TernaryPoint(ChartAxis axis, double value)
    {
        double amount = ChartAxisBreakOperations.Normalize(value, axis.Domain, axis.BreakMinimums, axis.BreakMaximums, axis.BreakCount);
        return new ChartPoint(axis.TernaryStart.X + (axis.TernaryEnd.X - axis.TernaryStart.X) * amount, axis.TernaryStart.Y + (axis.TernaryEnd.Y - axis.TernaryStart.Y) * amount);
    }

    internal static void RenderHorizontalGridSvg(SvgWriter writer, ChartAxis axis)
    {
        if (!axis.ShowGridLines)
        {
            return;
        }

        SvgStyle style = CreateGridStyle(axis);
        double endY = axis.PlotArea.Top();
        if (axis.GridLength > 0.0)
        {
            endY = axis.PlotArea.Bottom() - axis.GridLength;
        }

        int index = 0;
        while (index < axis.Ticks.Length)
        {
            double x = MapX(axis, axis.Ticks[index].Value);
            writer.WriteLine(x, axis.PlotArea.Bottom(), x, endY, style, GridId(axis, index), "");
            index = index + 1;
        }
    }

    internal static void RenderVerticalGridSvg(SvgWriter writer, ChartAxis axis)
    {
        if (!axis.ShowGridLines)
        {
            return;
        }

        SvgStyle style = CreateGridStyle(axis);
        double endX = axis.PlotArea.Right();
        if (axis.GridLength > 0.0)
        {
            endX = axis.PlotArea.Left() + axis.GridLength;
        }

        int index = 0;
        while (index < axis.Ticks.Length)
        {
            double y = MapY(axis, axis.Ticks[index].Value);
            writer.WriteLine(axis.PlotArea.Left(), y, endX, y, style, GridId(axis, index), "");
            index = index + 1;
        }
    }

    internal static void RenderHorizontalZeroLineSvg(SvgWriter writer, ChartAxis axis)
    {
        if (!axis.ShowZeroLine || !axis.Domain.Contains(0.0))
        {
            return;
        }

        SvgStyle style = CreateZeroLineStyle(axis);
        double x = MapX(axis, 0.0);
        writer.WriteLine(x, axis.PlotArea.Top(), x, axis.PlotArea.Bottom(), style, axis.Id + "-zero", "");
    }

    internal static void RenderVerticalZeroLineSvg(SvgWriter writer, ChartAxis axis)
    {
        if (!axis.ShowZeroLine || !axis.Domain.Contains(0.0))
        {
            return;
        }

        SvgStyle style = CreateZeroLineStyle(axis);
        double y = MapY(axis, 0.0);
        writer.WriteLine(axis.PlotArea.Left(), y, axis.PlotArea.Right(), y, style, axis.Id + "-zero", "");
    }

    internal static void RenderHorizontalBandsSvg(SvgWriter writer, ChartAxis axis)
    {
        if (!axis.ShowGridBands)
        {
            return;
        }

        SvgStyle style = CreateBandStyle(axis);
        int index = 0;
        while (index < axis.Ticks.Length - 1)
        {
            if ((index % 2) == 0)
            {
                double x0 = MapX(axis, axis.Ticks[index].Value);
                double x1 = MapX(axis, axis.Ticks[index + 1].Value);
                double left = System.Math.Min(x0, x1);
                double width = System.Math.Abs(x1 - x0);
                if (width > 0.0)
                {
                    writer.WriteRect(left, axis.PlotArea.Top(), width, axis.PlotArea.Height, 0.0, 0.0, style, BandId(axis, index), "");
                }
            }

            index = index + 1;
        }
    }

    internal static void RenderVerticalBandsSvg(SvgWriter writer, ChartAxis axis)
    {
        if (!axis.ShowGridBands)
        {
            return;
        }

        SvgStyle style = CreateBandStyle(axis);
        int index = 0;
        while (index < axis.Ticks.Length - 1)
        {
            if ((index % 2) == 0)
            {
                double y0 = MapY(axis, axis.Ticks[index].Value);
                double y1 = MapY(axis, axis.Ticks[index + 1].Value);
                double top = System.Math.Min(y0, y1);
                double height = System.Math.Abs(y1 - y0);
                if (height > 0.0)
                {
                    writer.WriteRect(axis.PlotArea.Left(), top, axis.PlotArea.Width, height, 0.0, 0.0, style, BandId(axis, index), "");
                }
            }

            index = index + 1;
        }
    }

    internal static void RenderHorizontalMinorTicksSvg(SvgWriter writer, ChartAxis axis, SvgStyle style, double y, double direction)
    {
        if (!axis.ShowMinorTicks || axis.MinorTickCount <= 0)
        {
            return;
        }

        int index = 0;
        while (index < axis.Ticks.Length - 1)
        {
            int minorIndex = 1;
            while (minorIndex <= axis.MinorTickCount)
            {
                double value = MinorTickValue(axis, index, minorIndex);
                if (IsTickVisible(axis, value))
                {
                    double x = MapX(axis, value);
                    writer.WriteLine(x, y, x, y + axis.MinorTickLength * direction, style, MinorTickId(axis, index, minorIndex), "");
                }

                minorIndex = minorIndex + 1;
            }

            index = index + 1;
        }
    }

    internal static void RenderVerticalMinorTicksSvg(SvgWriter writer, ChartAxis axis, SvgStyle style, double x, double direction)
    {
        if (!axis.ShowMinorTicks || axis.MinorTickCount <= 0)
        {
            return;
        }

        int index = 0;
        while (index < axis.Ticks.Length - 1)
        {
            int minorIndex = 1;
            while (minorIndex <= axis.MinorTickCount)
            {
                double value = MinorTickValue(axis, index, minorIndex);
                if (IsTickVisible(axis, value))
                {
                    double y = MapY(axis, value);
                    writer.WriteLine(x, y, x + axis.MinorTickLength * direction, y, style, MinorTickId(axis, index, minorIndex), "");
                }

                minorIndex = minorIndex + 1;
            }

            index = index + 1;
        }
    }

    internal static void RenderHorizontalLabelsSvg(SvgWriter writer, ChartAxis axis, double y, double direction)
    {
        if (!axis.ShowTickLabels)
        {
            return;
        }

        SvgStyle style = CreateTextStyle(axis);
        double lastPosition = -1000000000000.0;
        double lastExtent = 0.0;
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            if (IsTickVisible(axis, axis.Ticks[index].Value))
            {
                double x = MapX(axis, axis.Ticks[index].Value);
                string label = PrepareTickLabel(axis, axis.Ticks[index]);
                double extent = ProjectedLabelExtent(axis, label, true);
                if (ShouldRenderLabel(axis, x, lastPosition, extent, lastExtent))
                {
                    WriteTickLabel(writer, axis, label, x, y + direction * (axis.TickLength + axis.LabelOffset), style, LabelId(axis, index));
                    lastPosition = x;
                    lastExtent = extent;
                }
            }

            index = index + 1;
        }
    }

    internal static void RenderVerticalLabelsSvg(SvgWriter writer, ChartAxis axis, double x, double direction)
    {
        if (!axis.ShowTickLabels)
        {
            return;
        }

        SvgStyle style = CreateTextStyle(axis);
        double lastPosition = -1000000000000.0;
        double lastExtent = 0.0;
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            if (IsTickVisible(axis, axis.Ticks[index].Value))
            {
                double y = MapY(axis, axis.Ticks[index].Value);
                string label = PrepareTickLabel(axis, axis.Ticks[index]);
                double extent = ProjectedLabelExtent(axis, label, false);
                if (ShouldRenderLabel(axis, y, lastPosition, extent, lastExtent))
                {
                    WriteTickLabel(writer, axis, label, x + direction * (axis.TickLength + axis.LabelOffset), y, style, LabelId(axis, index));
                    lastPosition = y;
                    lastExtent = extent;
                }
            }

            index = index + 1;
        }
    }

    internal static void RenderHorizontalTitleSvg(SvgWriter writer, ChartAxis axis, double y, double direction)
    {
        if (axis.Title.Length == 0 && axis.Subtitle.Length == 0)
        {
            return;
        }

        SvgStyle style = CreateTextStyle(axis);
        double x = axis.PlotArea.Left() + axis.PlotArea.Width * 0.5;
        if (axis.Title.Length > 0)
        {
            writer.WriteText(axis.Title, x, y + direction * axis.TitleOffset, style, axis.Id + "-title", "");
        }

        if (axis.Subtitle.Length > 0)
        {
            double subtitleY = y + direction * (axis.TitleOffset + axis.FontSize * 1.4);
            writer.WriteText(axis.Subtitle, x, subtitleY, style, axis.Id + "-subtitle", "");
        }
    }

    internal static void RenderVerticalTitleSvg(SvgWriter writer, ChartAxis axis, double x, double direction)
    {
        if (axis.Title.Length == 0 && axis.Subtitle.Length == 0)
        {
            return;
        }

        SvgStyle style = CreateTextStyle(axis);
        double y = axis.PlotArea.Top() + axis.PlotArea.Height * 0.5;
        double titleX = x + direction * axis.TitleOffset;
        string transform = SvgWriter.Rotate(90.0 * direction, titleX, y);
        if (axis.Title.Length > 0)
        {
            writer.WriteText(axis.Title, titleX, y, style, axis.Id + "-title", transform);
        }

        if (axis.Subtitle.Length > 0)
        {
            double subtitleX = x + direction * (axis.TitleOffset + axis.FontSize * 1.4);
            string subtitleTransform = SvgWriter.Rotate(90.0 * direction, subtitleX, y);
            writer.WriteText(axis.Subtitle, subtitleX, y, style, axis.Id + "-subtitle", subtitleTransform);
        }
    }

    internal static void RenderRadialLabelsSvg(SvgWriter writer, ChartAxis axis)
    {
        if (!axis.ShowTickLabels)
        {
            return;
        }

        SvgStyle style = CreateTextStyle(axis);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            double radius = MapRadius(axis, axis.Ticks[index].Value);
            string label = PrepareTickLabel(axis, axis.Ticks[index]);
            WriteTickLabel(writer, axis, label, axis.Center.X + radius + axis.LabelOffset, axis.Center.Y, style, LabelId(axis, index));
            index = index + 1;
        }
    }

    internal static void RenderAngularLabelsSvg(SvgWriter writer, ChartAxis axis)
    {
        if (!axis.ShowTickLabels)
        {
            return;
        }

        SvgStyle style = CreateTextStyle(axis);
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            ChartPoint point = PointAtAngle(axis, axis.Ticks[index].Value, axis.Radius + axis.LabelOffset);
            string label = PrepareTickLabel(axis, axis.Ticks[index]);
            WriteTickLabel(writer, axis, label, point.X, point.Y, style, LabelId(axis, index));
            index = index + 1;
        }
    }

    internal static void RenderPolarTitleSvg(SvgWriter writer, ChartAxis axis)
    {
        if (axis.Title.Length == 0 && axis.Subtitle.Length == 0)
        {
            return;
        }

        SvgStyle style = CreateTextStyle(axis);
        double y = axis.Center.Y - axis.Radius - axis.TitleOffset;
        if (axis.Title.Length > 0)
        {
            writer.WriteText(axis.Title, axis.Center.X, y, style, axis.Id + "-title", "");
        }

        if (axis.Subtitle.Length > 0)
        {
            writer.WriteText(axis.Subtitle, axis.Center.X, y - axis.FontSize * 1.4, style, axis.Id + "-subtitle", "");
        }
    }

    internal static string FormatTick(ChartAxis axis, ChartTick tick)
    {
        if (axis.TickFormatter == null)
        {
            return ChartTickFormatterOperations.FormatTick(tick);
        }

        string label = axis.TickFormatter.FormatTick(tick);
        if (label == null)
        {
            throw new System.InvalidOperationException("Tick formatter returned null.");
        }

        return label;
    }

    internal static string PrepareTickLabel(ChartAxis axis, ChartTick tick)
    {
        string label = FormatTick(axis, tick);
        if (axis.Unit.Length > 0)
        {
            label = label + " " + axis.Unit;
        }

        return ChartTickLabelOperations.Prepare(label, axis.MultilineTickLabels, axis.TrimTickLabels, axis.TickLabelMaximumCharacters, axis.EllipsizeTickLabels);
    }

    internal static void WriteTickLabel(SvgWriter writer, ChartAxis axis, string label, double x, double y, SvgStyle style, string id)
    {
        string transform = "";
        if (axis.TickLabelRotation != 0.0)
        {
            transform = SvgWriter.Rotate(axis.TickLabelRotation, x, y);
        }

        if (axis.MultilineTickLabels && label.IndexOf('\n') >= 0)
        {
            writer.WriteMultilineText(label, x, y, axis.FontSize * 1.2, style, id, transform);
            return;
        }

        writer.WriteText(label, x, y, style, id, transform);
    }

    internal static double MapX(ChartAxis axis, double value)
    {
        double amount = ChartAxisBreakOperations.Normalize(value, axis.Domain, axis.BreakMinimums, axis.BreakMaximums, axis.BreakCount);
        return axis.PlotArea.Left() + axis.PlotArea.Width * amount;
    }

    internal static double MapY(ChartAxis axis, double value)
    {
        double amount = ChartAxisBreakOperations.Normalize(value, axis.Domain, axis.BreakMinimums, axis.BreakMaximums, axis.BreakCount);
        return axis.PlotArea.Bottom() - axis.PlotArea.Height * amount;
    }

    internal static double MapRadius(ChartAxis axis, double value)
    {
        double amount = ChartAxisBreakOperations.Normalize(value, axis.Domain, axis.BreakMinimums, axis.BreakMaximums, axis.BreakCount);
        return axis.Radius * amount;
    }

    internal static ChartPoint PointAtAngle(ChartAxis axis, double angleDegrees, double radius)
    {
        ChartLinearScale.ValidateFinite(angleDegrees, "angleDegrees");
        ChartAxis.ValidateRadius(radius);
        double radians = (angleDegrees - 90.0) * System.Math.PI / 180.0;
        double x = axis.Center.X + System.Math.Cos(radians) * radius;
        double y = axis.Center.Y + System.Math.Sin(radians) * radius;
        return new ChartPoint(x, y);
    }

    internal static SvgStyle CreateAxisStyle(ChartAxis axis)
    {
        if (axis == null)
        {
            throw new System.ArgumentNullException("axis");
        }

        SvgStyle style = new SvgStyle();
        style.SetNoFill();
        style.SetStroke(axis.StrokeColor, axis.StrokeWidth);
        return style;
    }

    internal static SvgStyle CreateTickStyle(ChartAxis axis)
    {
        SvgStyle style = new SvgStyle();
        style.SetNoFill();
        style.SetStroke(axis.TickColor, axis.TickStrokeWidth);
        return style;
    }

    internal static SvgStyle CreateMinorTickStyle(ChartAxis axis)
    {
        SvgStyle style = new SvgStyle();
        style.SetNoFill();
        style.SetStroke(axis.MinorTickColor, axis.MinorTickStrokeWidth);
        return style;
    }

    internal static SvgStyle CreateGridStyle(ChartAxis axis)
    {
        if (axis == null)
        {
            throw new System.ArgumentNullException("axis");
        }

        SvgStyle style = new SvgStyle();
        style.SetNoFill();
        style.SetStroke(axis.GridColor, axis.GridStrokeWidth);
        return style;
    }

    internal static SvgStyle CreateZeroLineStyle(ChartAxis axis)
    {
        if (axis == null)
        {
            throw new System.ArgumentNullException("axis");
        }

        SvgStyle style = new SvgStyle();
        style.SetNoFill();
        style.SetStroke(axis.ZeroLineColor, axis.ZeroLineStrokeWidth);
        return style;
    }

    internal static SvgStyle CreateBandStyle(ChartAxis axis)
    {
        if (axis == null)
        {
            throw new System.ArgumentNullException("axis");
        }

        SvgStyle style = new SvgStyle();
        style.SetFill(axis.BandColor);
        style.SetNoStroke();
        return style;
    }

    internal static SvgStyle CreateTextStyle(ChartAxis axis)
    {
        if (axis == null)
        {
            throw new System.ArgumentNullException("axis");
        }

        SvgStyle style = new SvgStyle();
        style.SetFill(axis.TextColor);
        style.SetNoStroke();
        style.SetFont(axis.FontFamily, axis.FontSize);
        return style;
    }

    internal static bool ShouldRenderLabel(ChartAxis axis, double position, double lastPosition, double extent, double lastExtent)
    {
        if (!axis.AutoSkipTickLabels)
        {
            return true;
        }

        if (lastPosition < -100000000000.0)
        {
            return true;
        }

        double required = axis.MinimumLabelSpacing + (extent + lastExtent) * 0.5;
        return System.Math.Abs(position - lastPosition) >= required;
    }

    internal static bool IsTickVisible(ChartAxis axis, double value)
    {
        return ChartAxisBreakOperations.IsVisible(value, axis.BreakMinimums, axis.BreakMaximums, axis.BreakCount);
    }

    internal static double ProjectedLabelExtent(ChartAxis axis, string label, bool horizontal)
    {
        double width = ChartTickLabelOperations.LongestLineLength(label) * axis.FontSize * 0.6;
        double height = ChartTickLabelOperations.LineCount(label) * axis.FontSize * 1.2;
        double radians = axis.TickLabelRotation * System.Math.PI / 180.0;
        double cosine = System.Math.Abs(System.Math.Cos(radians));
        double sine = System.Math.Abs(System.Math.Sin(radians));
        if (horizontal)
        {
            return cosine * width + sine * height;
        }

        return sine * width + cosine * height;
    }

    internal static double MinorTickValue(ChartAxis axis, int tickIndex, int minorIndex)
    {
        if (tickIndex < 0 || tickIndex >= axis.Ticks.Length - 1)
        {
            throw new System.ArgumentOutOfRangeException("tickIndex");
        }

        if (minorIndex <= 0 || minorIndex > axis.MinorTickCount)
        {
            throw new System.ArgumentOutOfRangeException("minorIndex");
        }

        double start = axis.Ticks[tickIndex].Value;
        double end = axis.Ticks[tickIndex + 1].Value;
        double amount = minorIndex / (double)(axis.MinorTickCount + 1);
        return start + (end - start) * amount;
    }

    internal static string TickId(ChartAxis axis, int index)
    {
        return axis.Id + "-tick-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string MinorTickId(ChartAxis axis, int index, int minorIndex)
    {
        return axis.Id + "-minor-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + minorIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string GridId(ChartAxis axis, int index)
    {
        return axis.Id + "-grid-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string BandId(ChartAxis axis, int index)
    {
        return axis.Id + "-band-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string LabelId(ChartAxis axis, int index)
    {
        return axis.Id + "-label-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string RingId(ChartAxis axis, int index)
    {
        return axis.Id + "-ring-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string ThresholdId(ChartAxis axis, int index)
    {
        return axis.Id + "-threshold-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string BreakId(ChartAxis axis, int index)
    {
        return axis.Id + "-break-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
