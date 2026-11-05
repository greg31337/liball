namespace chart;

/// <summary>
/// Provides operations for diagnostics.
/// </summary>
internal static class ChartDiagnosticsOperations
{
    /// <summary>
    /// Adds the supplied layout overlay to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="layout">The layout.</param>
    public static void AddLayoutOverlay(IChartRenderTarget scene, ChartLayoutResult layout)
    {
        AddLayoutOverlay(scene, layout, "layout-debug");
    }

    /// <summary>
    /// Adds the supplied layout overlay to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="layout">The layout.</param>
    /// <param name="idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddLayoutOverlay(IChartRenderTarget scene, ChartLayoutResult layout, string idPrefix)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartPlot.ValidateId(idPrefix);
        int index = 0;
        while (index < layout.ItemCount)
        {
            ChartLayoutItem item = layout.ItemAt(index);
            string id = idPrefix + "-" + item.Id;
            ChartRenderStyle boundsStyle = CreateLayoutBoundsStyle(item.Kind);
            ChartRenderStyle textStyle = CreateLayoutTextStyle(item.Kind);

            scene.AddRect(id + "-bounds", item.Bounds.X, item.Bounds.Y, item.Bounds.Width, item.Bounds.Height, 0.0, 0.0, boundsStyle);
            scene.AddText(id + "-label", LayoutItemLabel(item), item.Bounds.X + 2.0, item.Bounds.Y + 12.0, textStyle);
            index = index + 1;
        }
    }

    /// <summary>
    /// Computes the layout for trace within the supplied bounds.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string LayoutTrace(ChartLayoutResult layout)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("ChartLayoutTrace figure=");
        builder.Append(FormatRect(layout.FigureRect));
        builder.Append(" content=");
        builder.Append(FormatRect(layout.ContentRect));
        builder.Append(" plotArea=");
        builder.Append(FormatRect(layout.PlotArea));
        builder.Append(" items=");
        builder.Append(FormatInt(layout.ItemCount));
        builder.AppendLine();

        int index = 0;
        while (index < layout.ItemCount)
        {
            ChartLayoutItem item = layout.ItemAt(index);
            builder.Append("[");
            builder.Append(FormatInt(index));
            builder.Append("] ");
            builder.Append(item.Kind.ToString());
            builder.Append(" id=");
            builder.Append(item.Id);
            builder.Append(" side=");
            builder.Append(item.Side.ToString());
            builder.Append(" bounds=");
            builder.Append(FormatRect(item.Bounds));
            builder.AppendLine();
            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Renders trace using the supplied data and configuration.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string RenderTrace(ChartRenderScene scene)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("ChartRenderTrace width=");
        builder.Append(FormatInt(scene.Width));
        builder.Append(" height=");
        builder.Append(FormatInt(scene.Height));
        builder.Append(" commands=");
        builder.Append(FormatInt(scene.CommandCount));
        builder.AppendLine();

        int index = 0;
        while (index < scene.CommandCount)
        {
            ChartRenderCommand command = scene.CommandAt(index);
            builder.Append("[");
            builder.Append(FormatInt(index));
            builder.Append("] ");
            builder.Append(command.Kind.ToString());
            builder.Append(" id=");
            builder.Append(command.Id);
            AppendCommandGeometry(builder, command);
            builder.Append(" style=");
            builder.Append(StyleTrace(command.Style));
            builder.AppendLine();
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static ChartRenderStyle CreateLayoutBoundsStyle(ChartLayoutItemKind kind)
    {
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(LayoutColor(kind), 1.0);
        style.SetOpacity(0.9);
        return style;
    }

    internal static ChartRenderStyle CreateLayoutTextStyle(ChartLayoutItemKind kind)
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFill(LayoutColor(kind));
        style.SetFont("Arial", 10);
        return style;
    }

    internal static ChartColor LayoutColor(ChartLayoutItemKind kind)
    {
        if (kind == ChartLayoutItemKind.Figure)
        {
            return ChartColor.FromRgba(220, 60, 60, 220);
        }

        if (kind == ChartLayoutItemKind.Content)
        {
            return ChartColor.FromRgba(40, 120, 220, 220);
        }

        if (kind == ChartLayoutItemKind.Plot)
        {
            return ChartColor.FromRgba(40, 150, 90, 220);
        }

        if (kind == ChartLayoutItemKind.Axis)
        {
            return ChartColor.FromRgba(210, 120, 40, 220);
        }

        if (kind == ChartLayoutItemKind.Legend || kind == ChartLayoutItemKind.Colorbar)
        {
            return ChartColor.FromRgba(140, 80, 190, 220);
        }

        return ChartColor.FromRgba(80, 80, 80, 220);
    }

    internal static string LayoutItemLabel(ChartLayoutItem item)
    {
        if (item == null)
        {
            throw new System.ArgumentNullException("item");
        }

        return item.Kind.ToString() + ":" + item.Id + " " + FormatRect(item.Bounds);
    }

    internal static void AppendCommandGeometry(System.Text.StringBuilder builder, ChartRenderCommand command)
    {
        if (builder == null)
        {
            throw new System.ArgumentNullException("builder");
        }

        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        if (command.Kind == ChartRenderCommandKind.Clear)
        {
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Line)
        {
            AppendLineGeometry(builder, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Polyline || command.Kind == ChartRenderCommandKind.Polygon)
        {
            AppendPointsGeometry(builder, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Rect)
        {
            AppendRectGeometry(builder, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Circle)
        {
            AppendCircleGeometry(builder, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Ellipse)
        {
            AppendEllipseGeometry(builder, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Path)
        {
            builder.Append(" pathCommands=");
            builder.Append(FormatInt(command.Path.Commands.Count));
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Text)
        {
            AppendTextGeometry(builder, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Arc || command.Kind == ChartRenderCommandKind.Wedge)
        {
            builder.Append(" center=");
            builder.Append(FormatDouble(command.X1));
            builder.Append(",");
            builder.Append(FormatDouble(command.Y1));
            builder.Append(" innerRadius=");
            builder.Append(FormatDouble(command.InnerRadius));
            builder.Append(" outerRadius=");
            builder.Append(FormatDouble(command.RadiusX));
            builder.Append(" angles=");
            builder.Append(FormatDouble(command.StartAngle));
            builder.Append(",");
            builder.Append(FormatDouble(command.EndAngle));
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Image)
        {
            AppendRectGeometry(builder, command);
            builder.Append(" bitmap=");
            builder.Append(FormatInt(command.Image.Width));
            builder.Append("x");
            builder.Append(FormatInt(command.Image.Height));
            return;
        }

        if (command.Kind == ChartRenderCommandKind.PushTransform)
        {
            builder.Append(" matrix=");
            builder.Append(SvgWriter.Matrix(command.Transform));
            return;
        }

        if (command.Kind == ChartRenderCommandKind.PushClip)
        {
            builder.Append(" clip=");
            builder.Append(FormatRect(command.ClipBounds));
            return;
        }

        if (command.Kind == ChartRenderCommandKind.BeginGroup || command.Kind == ChartRenderCommandKind.EndGroup || command.Kind == ChartRenderCommandKind.PopTransform || command.Kind == ChartRenderCommandKind.PopClip)
        {
            return;
        }

        AppendMarkerGeometry(builder, command);
    }

    internal static string StyleTrace(ChartRenderStyle style)
    {
        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        return "fill=" + FillTrace(style) + " stroke=" + StrokeTrace(style) + " strokeWidth=" + FormatDouble(style.StrokeWidth) + " opacity=" + FormatDouble(style.Opacity) + " blend=" + style.BlendMode.ToString();
    }

    internal static string FillTrace(ChartRenderStyle style)
    {
        if (!style.HasFill)
        {
            return "unset";
        }

        if (style.FillNone)
        {
            return "none";
        }

        return style.FillColor.ToHexRgba();
    }

    internal static string StrokeTrace(ChartRenderStyle style)
    {
        if (!style.HasStroke)
        {
            return "unset";
        }

        if (style.StrokeNone)
        {
            return "none";
        }

        return style.StrokeColor.ToHexRgba();
    }

    internal static string FormatRect(ChartRect rect)
    {
        if (rect == null)
        {
            throw new System.ArgumentNullException("rect");
        }

        return "x=" + FormatDouble(rect.X) + " y=" + FormatDouble(rect.Y) + " width=" + FormatDouble(rect.Width) + " height=" + FormatDouble(rect.Height);
    }

    internal static string FormatPoint(ChartPoint point)
    {
        if (point == null)
        {
            throw new System.ArgumentNullException("point");
        }

        return "(" + FormatDouble(point.X) + "," + FormatDouble(point.Y) + ")";
    }

    internal static string FormatDouble(double value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FormatInt(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string SafeText(string text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        return text.Replace("\r", " ").Replace("\n", " ");
    }

    internal static void AppendLineGeometry(System.Text.StringBuilder builder, ChartRenderCommand command)
    {
        builder.Append(" x1=");
        builder.Append(FormatDouble(command.X1));
        builder.Append(" y1=");
        builder.Append(FormatDouble(command.Y1));
        builder.Append(" x2=");
        builder.Append(FormatDouble(command.X2));
        builder.Append(" y2=");
        builder.Append(FormatDouble(command.Y2));
    }

    internal static void AppendPointsGeometry(System.Text.StringBuilder builder, ChartRenderCommand command)
    {
        builder.Append(" points=");
        builder.Append(FormatInt(command.Points.Length));
        if (command.Points.Length > 0)
        {
            builder.Append(" first=");
            builder.Append(FormatPoint(command.Points[0]));
            builder.Append(" last=");
            builder.Append(FormatPoint(command.Points[command.Points.Length - 1]));
        }
    }

    internal static void AppendRectGeometry(System.Text.StringBuilder builder, ChartRenderCommand command)
    {
        builder.Append(" x=");
        builder.Append(FormatDouble(command.X1));
        builder.Append(" y=");
        builder.Append(FormatDouble(command.Y1));
        builder.Append(" width=");
        builder.Append(FormatDouble(command.Width));
        builder.Append(" height=");
        builder.Append(FormatDouble(command.Height));
        builder.Append(" radiusX=");
        builder.Append(FormatDouble(command.RadiusX));
        builder.Append(" radiusY=");
        builder.Append(FormatDouble(command.RadiusY));
    }

    internal static void AppendCircleGeometry(System.Text.StringBuilder builder, ChartRenderCommand command)
    {
        builder.Append(" cx=");
        builder.Append(FormatDouble(command.X1));
        builder.Append(" cy=");
        builder.Append(FormatDouble(command.Y1));
        builder.Append(" r=");
        builder.Append(FormatDouble(command.RadiusX));
    }

    internal static void AppendEllipseGeometry(System.Text.StringBuilder builder, ChartRenderCommand command)
    {
        builder.Append(" cx=");
        builder.Append(FormatDouble(command.X1));
        builder.Append(" cy=");
        builder.Append(FormatDouble(command.Y1));
        builder.Append(" rx=");
        builder.Append(FormatDouble(command.RadiusX));
        builder.Append(" ry=");
        builder.Append(FormatDouble(command.RadiusY));
    }

    internal static void AppendTextGeometry(System.Text.StringBuilder builder, ChartRenderCommand command)
    {
        builder.Append(" text=\"");
        builder.Append(SafeText(command.Text));
        builder.Append("\" x=");
        builder.Append(FormatDouble(command.X1));
        builder.Append(" y=");
        builder.Append(FormatDouble(command.Y1));
    }

    internal static void AppendMarkerGeometry(System.Text.StringBuilder builder, ChartRenderCommand command)
    {
        builder.Append(" shape=");
        builder.Append(command.MarkerShape.ToString());
        builder.Append(" cx=");
        builder.Append(FormatDouble(command.X1));
        builder.Append(" cy=");
        builder.Append(FormatDouble(command.Y1));
        builder.Append(" size=");
        builder.Append(FormatDouble(command.MarkerSize));
        if (command.MarkerShape == ChartMarkerShape.CustomPath)
        {
            builder.Append(" pathCommands=");
            builder.Append(FormatInt(command.MarkerPath.Commands.Count));
        }
    }
}
