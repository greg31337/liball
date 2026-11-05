namespace chart;

internal class SvgStyle
{
    internal bool HasFill;
    internal bool FillNone;
    internal ChartColor FillColor;
    internal bool HasStroke;
    internal bool StrokeNone;
    internal ChartColor StrokeColor;
    internal bool HasStrokeWidth;
    internal double StrokeWidth;
    internal bool HasOpacity;
    internal double Opacity;
    internal bool HasFontFamily;
    internal string FontFamily;
    internal bool HasFontSize;
    internal int FontSize;
    internal double[] DashArray;
    internal double MiterLimit;
    internal bool HasMiterLimit;
    internal double FillOpacity;
    internal bool HasFillOpacity;
    internal double StrokeOpacity;
    internal bool HasStrokeOpacity;
    internal ChartFontWeight FontWeight;
    internal bool HasFontWeight;
    internal ChartFontStyle FontStyle;
    internal bool HasFontStyle;
    internal bool HasTextOutline;
    internal ChartColor TextOutlineColor;
    internal double TextOutlineWidth;
    internal bool HasTextShadow;
    internal ChartColor TextShadowColor;
    internal double TextShadowOffsetX;
    internal double TextShadowOffsetY;
    internal double TextShadowBlur;
    internal string ClipPathId;
    internal string FillReference;
    internal string StrokeReference;

    internal SvgStyle()
    {
        FillColor = ChartColor.Black();
        StrokeColor = ChartColor.Black();
        FontFamily = "";
        DashArray = new double[0];
        MiterLimit = 4.0;
        FillOpacity = 1.0;
        StrokeOpacity = 1.0;
        FontWeight = ChartFontWeight.Normal;
        FontStyle = ChartFontStyle.Normal;
        TextOutlineColor = ChartColor.Transparent();
        TextShadowColor = ChartColor.Transparent();
        ClipPathId = "";
        FillReference = "";
        StrokeReference = "";
    }

    internal void SetFill(ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        HasFill = true;
        FillNone = false;
        FillColor = color.Copy();
    }

    internal void SetNoFill()
    {
        HasFill = true;
        FillNone = true;
    }

    internal void SetFillReference(string reference)
    {
        ChartTextBlock.ValidateText(reference, "reference");
        HasFill = true;
        FillNone = false;
        FillReference = reference;
    }

    internal void SetStroke(ChartColor color, double strokeWidth)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        if (strokeWidth < 0.0 || double.IsNaN(strokeWidth) || double.IsInfinity(strokeWidth))
        {
            throw new System.ArgumentOutOfRangeException("strokeWidth");
        }

        HasStroke = true;
        StrokeNone = false;
        StrokeColor = color.Copy();
        HasStrokeWidth = true;
        StrokeWidth = strokeWidth;
    }

    internal void SetNoStroke()
    {
        HasStroke = true;
        StrokeNone = true;
    }

    internal void SetStrokeReference(string reference, double strokeWidth)
    {
        ChartTextBlock.ValidateText(reference, "reference");
        ChartRenderStyle.ValidatePositive(strokeWidth, "strokeWidth");
        HasStroke = true;
        StrokeNone = false;
        StrokeReference = reference;
        HasStrokeWidth = true;
        StrokeWidth = strokeWidth;
    }

    internal void SetOpacity(double opacity)
    {
        if (opacity < 0.0 || opacity > 1.0 || double.IsNaN(opacity) || double.IsInfinity(opacity))
        {
            throw new System.ArgumentOutOfRangeException("opacity");
        }

        HasOpacity = true;
        Opacity = opacity;
    }

    internal void SetDashArray(double[] dashArray)
    {
        if (dashArray == null)
        {
            throw new System.ArgumentNullException("dashArray");
        }

        DashArray = ChartStyle.CopyDashArrayAllowEmpty(dashArray);
    }

    internal void SetMiterLimit(double miterLimit)
    {
        ChartRenderStyle.ValidatePositive(miterLimit, "miterLimit");
        HasMiterLimit = true;
        MiterLimit = miterLimit;
    }

    internal void SetFillOpacity(double fillOpacity)
    {
        ChartStyle.ValidateOpacity(fillOpacity, "fillOpacity");
        HasFillOpacity = true;
        FillOpacity = fillOpacity;
    }

    internal void SetStrokeOpacity(double strokeOpacity)
    {
        ChartStyle.ValidateOpacity(strokeOpacity, "strokeOpacity");
        HasStrokeOpacity = true;
        StrokeOpacity = strokeOpacity;
    }

    internal void SetFont(string family, int size)
    {
        if (family == null)
        {
            throw new System.ArgumentNullException("family");
        }

        if (size <= 0)
        {
            throw new System.ArgumentOutOfRangeException("size");
        }

        HasFontFamily = true;
        FontFamily = family;
        HasFontSize = true;
        FontSize = size;
    }

    internal void SetFontWeight(ChartFontWeight fontWeight)
    {
        ChartStyle.ValidateFontWeight(fontWeight);
        HasFontWeight = true;
        FontWeight = fontWeight;
    }

    internal void SetFontStyle(ChartFontStyle fontStyle)
    {
        ChartStyle.ValidateFontStyle(fontStyle);
        HasFontStyle = true;
        FontStyle = fontStyle;
    }

    internal void SetTextOutline(ChartColor color, double width)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartRenderStyle.ValidatePositive(width, "width");
        HasTextOutline = true;
        TextOutlineColor = color.Copy();
        TextOutlineWidth = width;
    }

    internal void SetTextShadow(ChartColor color, double offsetX, double offsetY, double blur)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartLinearScale.ValidateFinite(offsetX, "offsetX");
        ChartLinearScale.ValidateFinite(offsetY, "offsetY");
        ChartStyle.ValidateNonNegative(blur, "blur");
        HasTextShadow = true;
        TextShadowColor = color.Copy();
        TextShadowOffsetX = offsetX;
        TextShadowOffsetY = offsetY;
        TextShadowBlur = blur;
    }

    internal void SetClipPath(string clipPathId)
    {
        ChartTextBlock.ValidateText(clipPathId, "clipPathId");
        ClipPathId = clipPathId;
    }

    internal string ToStyleString()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (HasFill)
        {
            string fillValue = FillColor.ToHexRgb();
            if (FillReference.Length > 0)
            {
                fillValue = "url(#" + FillReference + ")";
            }
            AppendStyle(builder, "fill", FillNone ? "none" : fillValue);
            if (!FillNone && (FillColor.A < 255 || HasFillOpacity))
            {
                AppendStyle(builder, "fill-opacity", SvgWriter.FormatDouble(FillColor.A / 255.0 * FillOpacity));
            }
        }

        if (HasStroke)
        {
            string strokeValue = StrokeColor.ToHexRgb();
            if (StrokeReference.Length > 0)
            {
                strokeValue = "url(#" + StrokeReference + ")";
            }
            AppendStyle(builder, "stroke", StrokeNone ? "none" : strokeValue);
            if (!StrokeNone && (StrokeColor.A < 255 || HasStrokeOpacity))
            {
                AppendStyle(builder, "stroke-opacity", SvgWriter.FormatDouble(StrokeColor.A / 255.0 * StrokeOpacity));
            }
        }

        if (HasStrokeWidth)
        {
            AppendStyle(builder, "stroke-width", SvgWriter.FormatDouble(StrokeWidth));
        }

        if (DashArray.Length > 0)
        {
            AppendStyle(builder, "stroke-dasharray", DashArrayString());
        }

        if (HasMiterLimit)
        {
            AppendStyle(builder, "stroke-miterlimit", SvgWriter.FormatDouble(MiterLimit));
        }

        if (HasOpacity)
        {
            AppendStyle(builder, "opacity", SvgWriter.FormatDouble(Opacity));
        }

        if (HasFontFamily)
        {
            AppendStyle(builder, "font-family", FontFamily);
        }

        if (HasFontSize)
        {
            AppendStyle(builder, "font-size", FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (HasFontWeight)
        {
            AppendStyle(builder, "font-weight", FontWeight == ChartFontWeight.Bold ? "bold" : "normal");
        }

        if (HasFontStyle)
        {
            string fontStyle = "normal";
            if (FontStyle == ChartFontStyle.Italic)
            {
                fontStyle = "italic";
            }
            else if (FontStyle == ChartFontStyle.Oblique)
            {
                fontStyle = "oblique";
            }

            AppendStyle(builder, "font-style", fontStyle);
        }

        if (HasTextOutline)
        {
            AppendStyle(builder, "paint-order", "stroke");
            AppendStyle(builder, "stroke", TextOutlineColor.ToHexRgb());
            AppendStyle(builder, "stroke-width", SvgWriter.FormatDouble(TextOutlineWidth));
        }

        if (HasTextShadow)
        {
            AppendStyle(builder, "filter", TextShadowString());
        }

        if (ClipPathId.Length > 0)
        {
            AppendStyle(builder, "clip-path", "url(#" + ClipPathId + ")");
        }

        return builder.ToString();
    }

    internal string DashArrayString()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < DashArray.Length)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(SvgWriter.FormatDouble(DashArray[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal string TextShadowString()
    {
        return "drop-shadow(" + SvgWriter.FormatDouble(TextShadowOffsetX) + "px " + SvgWriter.FormatDouble(TextShadowOffsetY) + "px " + SvgWriter.FormatDouble(TextShadowBlur) + "px " + TextShadowColor.ToHexRgb() + ")";
    }

    internal static void AppendStyle(System.Text.StringBuilder builder, string name, string value)
    {
        if (builder.Length > 0)
        {
            builder.Append(';');
        }

        builder.Append(name);
        builder.Append(':');
        builder.Append(value);
    }
}

internal class SvgWriter
{
    internal int Width;
    internal int Height;
    internal System.Text.StringBuilder Builder;
    internal int NextIdValue;
    internal int OpenGroups;
    internal bool Closed;
    internal int RootTagEndIndex;
    internal bool Configured;
    internal bool UseCssClasses;
    internal bool IncludeAccessibilityRoles;
    internal string IdPrefix;
    internal ChartAccessibilityMetadata? Accessibility;
    internal string[] UsedIds;
    internal int UsedIdCount;
    internal string[] StyleValues;
    internal string[] StyleClasses;
    internal int StyleCount;
    internal string NextSemanticClass;
    internal string NextRole;
    internal string NextLabel;
    internal string NextSeriesId;
    internal string NextPointId;
    internal int NextRowIndex;
    internal string NextCueShape;
    internal string NextCueDash;
    internal string NextCueLabel;
    internal string NextCuePattern;

    internal SvgWriter(int width, int height)
    {
        if (width <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        Width = width;
        Height = height;
        Builder = new System.Text.StringBuilder();
        NextIdValue = 1;
        OpenGroups = 0;
        Closed = false;
        Configured = false;
        UseCssClasses = false;
        IncludeAccessibilityRoles = false;
        IdPrefix = "";
        Accessibility = null;
        UsedIds = new string[16];
        UsedIdCount = 0;
        StyleValues = new string[8];
        StyleClasses = new string[8];
        StyleCount = 0;
        NextSemanticClass = "";
        NextRole = "";
        NextLabel = "";
        NextSeriesId = "";
        NextPointId = "";
        NextRowIndex = -1;
        NextCueShape = "";
        NextCueDash = "";
        NextCueLabel = "";
        NextCuePattern = "";
        Builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"");
        Builder.Append(width.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Builder.Append("\" height=\"");
        Builder.Append(height.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Builder.Append("\" viewBox=\"0 0 ");
        Builder.Append(width.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Builder.Append(' ');
        Builder.Append(height.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Builder.Append("\">");
        RootTagEndIndex = Builder.Length - 1;
    }

    internal void Configure(ChartSvgOptions options, ChartAccessibilityMetadata accessibility)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (accessibility == null)
        {
            throw new System.ArgumentNullException("accessibility");
        }
        if (options.IdPrefix == null)
        {
            throw new System.ArgumentNullException("IdPrefix");
        }
        if (Configured)
        {
            throw new System.InvalidOperationException("SVG serialization options are already configured.");
        }

        UseCssClasses = options.UseCssClasses;
        IncludeAccessibilityRoles = options.IncludeAccessibilityRoles;
        if (options.IdPrefix.Length > 0)
        {
            IdPrefix = SanitizeIdPrefix(options.IdPrefix);
        }
        Accessibility = accessibility;
        Configured = true;
        if (IncludeAccessibilityRoles)
        {
            string attributes = " role=\"img\"";
            if (accessibility.Title.Length > 0)
            {
                attributes = attributes + " aria-label=\"" + EscapeAttribute(accessibility.Title) + "\"";
            }

            Builder.Insert(RootTagEndIndex, attributes);
        }
    }

    internal string CreateId(string? prefix)
    {
        string safePrefix = SanitizeIdPrefix(prefix);
        if (IdPrefix.Length > 0)
        {
            safePrefix = IdPrefix + "-" + safePrefix;
        }

        string id = "";
        bool available = false;
        while (!available)
        {
            id = safePrefix + "-" + NextIdValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            NextIdValue = NextIdValue + 1;
            available = !ContainsId(id);
        }

        RegisterId(id);
        return id;
    }

    internal string ResolveId(string? requested, string prefix)
    {
        if (requested == null || requested.Length == 0)
        {
            return CreateId(prefix);
        }

        string baseId = SanitizeIdPrefix(requested);
        if (IdPrefix.Length > 0)
        {
            baseId = IdPrefix + "-" + baseId;
        }

        string id = baseId;
        int suffix = 2;
        while (ContainsId(id))
        {
            id = baseId + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }

        RegisterId(id);
        return id;
    }

    internal string ResolveDerivedId(string baseId, string suffix)
    {
        ChartTextBlock.ValidateText(baseId, "baseId");
        ChartTextBlock.ValidateText(suffix, "suffix");
        string id = baseId + "-" + SanitizeIdPrefix(suffix);
        if (!ContainsId(id))
        {
            RegisterId(id);
            return id;
        }

        int number = 2;
        string candidate = id + "-2";
        while (ContainsId(candidate))
        {
            number = number + 1;
            candidate = id + "-" + number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        RegisterId(candidate);
        return candidate;
    }

    internal void AddTitleDescription(string? title, string? description)
    {
        if (title != null && title.Length > 0)
        {
            Builder.Append("<title>");
            Builder.Append(EscapeText(title));
            Builder.Append("</title>");
        }

        if (description != null && description.Length > 0)
        {
            Builder.Append("<desc>");
            Builder.Append(EscapeText(description));
            Builder.Append("</desc>");
        }
    }

    internal void AddMetadata(string metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        Builder.Append("<metadata>");
        Builder.Append(EscapeText(metadata));
        Builder.Append("</metadata>");
    }

    internal void BeginGroup(string? id, string? transform, SvgStyle? style)
    {
        Builder.Append("<g");
        AppendAttribute("id", id);
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-group");
        Builder.Append('>');
        OpenGroups = OpenGroups + 1;
    }

    internal void BeginRetainedGroup(string originalId, string id)
    {
        SetNextSemantic(originalId);
        string cssClass = "chart-group";
        if (NextSemanticClass.Length > 0)
        {
            cssClass = NextSemanticClass;
        }

        Builder.Append("<g");
        AppendAttribute("id", id);
        AppendPresentation(null, cssClass);
        Builder.Append('>');
        OpenGroups = OpenGroups + 1;
    }

    internal void BeginStructuralGroup(string id, string transform, SvgStyle? style, string cssClass)
    {
        ClearNextSemantic();
        Builder.Append("<g");
        AppendAttribute("id", id);
        AppendAttribute("transform", transform);
        AppendPresentation(style, cssClass);
        Builder.Append('>');
        OpenGroups = OpenGroups + 1;
    }

    internal void EndGroup()
    {
        if (OpenGroups <= 0)
        {
            throw new System.InvalidOperationException("No open SVG group exists.");
        }

        Builder.Append("</g>");
        OpenGroups = OpenGroups - 1;
    }

    internal void AppendRawElement(string markup)
    {
        if (markup == null)
        {
            throw new System.ArgumentNullException("markup");
        }

        Builder.Append(markup);
    }

    internal void CloseDocument()
    {
        if (Closed)
        {
            return;
        }

        while (OpenGroups > 0)
        {
            EndGroup();
        }

        WriteCssRules();
        Builder.Append("</svg>");
        Closed = true;
    }

    internal string ToSvgString()
    {
        CloseDocument();
        return Builder.ToString();
    }

    internal void AppendAttribute(string name, string? value)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        if (value == null || value.Length == 0)
        {
            return;
        }

        Builder.Append(' ');
        Builder.Append(name);
        Builder.Append("=\"");
        Builder.Append(EscapeAttribute(value));
        Builder.Append('"');
    }

    internal void AppendStyleAttribute(SvgStyle? style)
    {
        if (style == null)
        {
            return;
        }

        string value = style.ToStyleString();
        if (value.Length > 0)
        {
            if (UseCssClasses)
            {
                AppendAttribute("class", RegisterStyle(value));
            }
            else
            {
                AppendAttribute("style", value);
            }
        }
    }

    internal void AppendPresentation(SvgStyle? style, string elementClass)
    {
        string styleValue = "";
        if (style != null)
        {
            styleValue = style.ToStyleString();
        }

        if (UseCssClasses)
        {
            string classValue = elementClass;
            if (NextSemanticClass.Length > 0 && NextSemanticClass != elementClass)
            {
                classValue = classValue + " " + NextSemanticClass;
            }
            if (styleValue.Length > 0)
            {
                string styleClass = RegisterStyle(styleValue);
                classValue = classValue + " " + styleClass;
            }

            AppendAttribute("class", classValue);
        }
        else if (styleValue.Length > 0)
        {
            AppendAttribute("style", styleValue);
        }

        AppendAttribute("role", NextRole);
        AppendAttribute("aria-label", NextLabel);
        AppendAttribute("data-series-id", NextSeriesId);
        AppendAttribute("data-point-id", NextPointId);
        AppendAttribute("data-marker-shape", NextCueShape);
        AppendAttribute("data-dash-pattern", NextCueDash);
        AppendAttribute("data-value-label", NextCueLabel);
        AppendAttribute("data-pattern-description", NextCuePattern);
        if (NextRowIndex >= 0)
        {
            AppendAttribute("data-row-index", NextRowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    internal string RegisterStyle(string value)
    {
        int index = 0;
        while (index < StyleCount)
        {
            if (StyleValues[index] == value)
            {
                return StyleClasses[index];
            }

            index = index + 1;
        }

        EnsureStyleCapacity(StyleCount + 1);
        string className = "chart-style-" + (StyleCount + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        StyleValues[StyleCount] = value;
        StyleClasses[StyleCount] = className;
        StyleCount = StyleCount + 1;
        return className;
    }

    internal void WriteCssRules()
    {
        if (!UseCssClasses || StyleCount == 0)
        {
            return;
        }

        Builder.Append("<style>");
        int index = 0;
        while (index < StyleCount)
        {
            Builder.Append('.');
            Builder.Append(StyleClasses[index]);
            Builder.Append('{');
            Builder.Append(EscapeText(StyleValues[index]));
            Builder.Append('}');
            index = index + 1;
        }

        Builder.Append("</style>");
    }

    internal void SetNextSemantic(string id)
    {
        ClearNextSemantic();
        if (Accessibility == null || id == null || id.Length == 0)
        {
            return;
        }

        int seriesIndex = 0;
        while (seriesIndex < Accessibility.SeriesCount)
        {
            ChartSeriesAccessibility series = Accessibility.Series[seriesIndex];
            if (series.SeriesId == id)
            {
                NextSemanticClass = "chart-series";
                NextSeriesId = series.SeriesId;
                NextLabel = ChartAccessibilityOutput.SeriesSemanticLabel(series);
                SetNextCues(series);
                if (IncludeAccessibilityRoles)
                {
                    NextRole = "group";
                }
                return;
            }

            int pointIndex = 0;
            while (pointIndex < series.PointCount)
            {
                ChartPointAccessibility point = series.Points[pointIndex];
                if (point.PointId == id)
                {
                    NextSemanticClass = "chart-point";
                    NextSeriesId = series.SeriesId;
                    NextPointId = point.PointId;
                    NextRowIndex = point.RowIndex;
                    NextLabel = ChartAccessibilityOutput.PointSemanticLabel(point, series);
                    SetNextCues(series);
                    if (IncludeAccessibilityRoles)
                    {
                        NextRole = "img";
                    }
                    return;
                }

                pointIndex = pointIndex + 1;
            }

            seriesIndex = seriesIndex + 1;
        }
    }

    internal void ClearNextSemantic()
    {
        NextSemanticClass = "";
        NextRole = "";
        NextLabel = "";
        NextSeriesId = "";
        NextPointId = "";
        NextRowIndex = -1;
        NextCueShape = "";
        NextCueDash = "";
        NextCueLabel = "";
        NextCuePattern = "";
    }

    internal void SetNextCues(ChartSeriesAccessibility series)
    {
        if (!series.HasNonColorCues)
        {
            return;
        }

        NextCueShape = series.CueShape.ToString();
        NextCueDash = ChartAccessibilityOutput.CueDashText(series);
        NextCueLabel = series.CueLabel;
        NextCuePattern = series.CuePatternDescription;
    }

    internal bool ContainsId(string id)
    {
        int index = 0;
        while (index < UsedIdCount)
        {
            if (UsedIds[index] == id)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void RegisterId(string id)
    {
        if (UsedIdCount >= UsedIds.Length)
        {
            int capacity = ChartArrayUtilities.GrowthCapacity(UsedIds.Length, UsedIdCount + 1);
            string[] values = new string[capacity];
            System.Array.Copy(UsedIds, values, UsedIdCount);
            UsedIds = values;
        }

        UsedIds[UsedIdCount] = id;
        UsedIdCount = UsedIdCount + 1;
    }

    internal void EnsureStyleCapacity(int capacity)
    {
        if (capacity <= StyleValues.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(StyleValues.Length, capacity);
        string[] values = new string[newCapacity];
        string[] classes = new string[newCapacity];
        System.Array.Copy(StyleValues, values, StyleCount);
        System.Array.Copy(StyleClasses, classes, StyleCount);
        StyleValues = values;
        StyleClasses = classes;
    }

    internal void WriteLine(double x1, double y1, double x2, double y2, SvgStyle? style, string? id, string? transform)
    {
        Builder.Append("<line");
        AppendAttribute("id", id);
        AppendAttribute("x1", FormatDouble(x1));
        AppendAttribute("y1", FormatDouble(y1));
        AppendAttribute("x2", FormatDouble(x2));
        AppendAttribute("y2", FormatDouble(y2));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-line");
        Builder.Append("/>");
    }

    internal void WritePolyline(ChartPoint[] points, SvgStyle? style, string? id, string? transform)
    {
        ValidatePoints(points, 2);
        Builder.Append("<polyline");
        AppendAttribute("id", id);
        AppendAttribute("points", PointsData(points));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-polyline");
        Builder.Append("/>");
    }

    internal void WritePolygon(ChartPoint[] points, SvgStyle? style, string? id, string? transform)
    {
        ValidatePoints(points, 3);
        Builder.Append("<polygon");
        AppendAttribute("id", id);
        AppendAttribute("points", PointsData(points));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-polygon");
        Builder.Append("/>");
    }

    internal void WriteRect(double x, double y, double width, double height, double radiusX, double radiusY, SvgStyle? style, string? id, string? transform)
    {
        ValidatePositive(width, "width");
        ValidatePositive(height, "height");
        Builder.Append("<rect");
        AppendAttribute("id", id);
        AppendAttribute("x", FormatDouble(x));
        AppendAttribute("y", FormatDouble(y));
        AppendAttribute("width", FormatDouble(width));
        AppendAttribute("height", FormatDouble(height));
        if (radiusX > 0.0)
        {
            AppendAttribute("rx", FormatDouble(radiusX));
        }

        if (radiusY > 0.0)
        {
            AppendAttribute("ry", FormatDouble(radiusY));
        }

        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-rect");
        Builder.Append("/>");
    }

    internal void WriteCircle(double centerX, double centerY, double radius, SvgStyle? style, string? id, string? transform)
    {
        ValidatePositive(radius, "radius");
        Builder.Append("<circle");
        AppendAttribute("id", id);
        AppendAttribute("cx", FormatDouble(centerX));
        AppendAttribute("cy", FormatDouble(centerY));
        AppendAttribute("r", FormatDouble(radius));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-circle");
        Builder.Append("/>");
    }

    internal void WriteEllipse(double centerX, double centerY, double radiusX, double radiusY, SvgStyle? style, string? id, string? transform)
    {
        ValidatePositive(radiusX, "radiusX");
        ValidatePositive(radiusY, "radiusY");
        Builder.Append("<ellipse");
        AppendAttribute("id", id);
        AppendAttribute("cx", FormatDouble(centerX));
        AppendAttribute("cy", FormatDouble(centerY));
        AppendAttribute("rx", FormatDouble(radiusX));
        AppendAttribute("ry", FormatDouble(radiusY));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-ellipse");
        Builder.Append("/>");
    }

    internal void WriteText(string text, double x, double y, SvgStyle? style, string? id, string? transform)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        Builder.Append("<text");
        AppendAttribute("id", id);
        AppendAttribute("x", FormatDouble(x));
        AppendAttribute("y", FormatDouble(y));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-text");
        Builder.Append('>');
        Builder.Append(EscapeText(text));
        Builder.Append("</text>");
    }

    internal void WriteMultilineText(string text, double x, double y, double lineHeight, SvgStyle? style, string? id, string? transform)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        ValidatePositive(lineHeight, "lineHeight");
        Builder.Append("<text");
        AppendAttribute("id", id);
        AppendAttribute("x", FormatDouble(x));
        AppendAttribute("y", FormatDouble(y));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-text");
        Builder.Append('>');
        int start = 0;
        int line = 0;
        int index = 0;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == '\n')
            {
                Builder.Append("<tspan");
                AppendAttribute("x", FormatDouble(x));
                if (line == 0)
                {
                    AppendAttribute("dy", "0");
                }
                else
                {
                    AppendAttribute("dy", FormatDouble(lineHeight));
                }

                Builder.Append('>');
                Builder.Append(EscapeText(text.Substring(start, index - start)));
                Builder.Append("</tspan>");
                start = index + 1;
                line = line + 1;
            }

            index = index + 1;
        }

        Builder.Append("</text>");
    }

    internal void WriteRichText(ChartTextSpan[] spans, int spanCount, double x, double y, SvgStyle style, string id, string transform, ChartTextDirection direction)
    {
        if (spans == null)
        {
            throw new System.ArgumentNullException("spans");
        }

        if (spanCount < 0 || spanCount > spans.Length)
        {
            throw new System.ArgumentOutOfRangeException("spanCount");
        }

        Builder.Append("<text");
        AppendAttribute("id", id);
        AppendAttribute("x", FormatDouble(x));
        AppendAttribute("y", FormatDouble(y));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-text");
        Builder.Append('>');
        if (direction == ChartTextDirection.RightToLeft)
        {
            int reverse = spanCount - 1;
            while (reverse >= 0)
            {
                WriteRichTextSpan(spans[reverse], direction);
                reverse = reverse - 1;
            }
        }
        else
        {
            int index = 0;
            while (index < spanCount)
            {
                WriteRichTextSpan(spans[index], direction);
                index = index + 1;
            }
        }

        Builder.Append("</text>");
    }

    internal void WriteRichTextSpan(ChartTextSpan span, ChartTextDirection direction)
    {
        if (span == null)
        {
            throw new System.ArgumentNullException("span");
        }

        Builder.Append("<tspan");
        AppendStyleAttribute(span.Style.ToSvgStyle());
        Builder.Append('>');
        Builder.Append(EscapeText(ChartTextLayoutOperations.PrepareBuiltInText(span.Text, direction)));
        Builder.Append("</tspan>");
    }

    internal void WritePath(ChartPath path, SvgStyle? style, string? id, string? transform)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        Builder.Append("<path");
        AppendAttribute("id", id);
        AppendAttribute("d", PathData(path));
        AppendAttribute("transform", transform);
        AppendPresentation(style, "chart-path");
        Builder.Append("/>");
    }

    internal void BeginClipPath(string id)
    {
        Builder.Append("<clipPath");
        AppendAttribute("id", id);
        Builder.Append('>');
    }

    internal void EndClipPath()
    {
        Builder.Append("</clipPath>");
    }

    internal void WriteClipPathRect(string id, double x, double y, double width, double height)
    {
        BeginClipPath(id);
        WriteRect(x, y, width, height, 0.0, 0.0, null, "", "");
        EndClipPath();
    }

    internal void WriteLinearGradient(string id, double x1, double y1, double x2, double y2, ChartColor[] colors, double[] offsets)
    {
        ValidateGradient(colors, offsets);
        Builder.Append("<linearGradient");
        AppendAttribute("id", id);
        AppendAttribute("x1", FormatDouble(x1));
        AppendAttribute("y1", FormatDouble(y1));
        AppendAttribute("x2", FormatDouble(x2));
        AppendAttribute("y2", FormatDouble(y2));
        Builder.Append('>');

        int index = 0;
        while (index < colors.Length)
        {
            Builder.Append("<stop");
            AppendAttribute("offset", FormatDouble(offsets[index] * 100.0) + "%");
            AppendAttribute("stop-color", colors[index].ToHexRgb());
            if (colors[index].A < 255)
            {
                AppendAttribute("stop-opacity", FormatDouble(colors[index].A / 255.0));
            }

            Builder.Append("/>");
            index = index + 1;
        }

        Builder.Append("</linearGradient>");
    }

    internal void BeginPattern(string id, double width, double height)
    {
        ValidatePositive(width, "width");
        ValidatePositive(height, "height");
        Builder.Append("<pattern");
        AppendAttribute("id", id);
        AppendAttribute("patternUnits", "userSpaceOnUse");
        AppendAttribute("width", FormatDouble(width));
        AppendAttribute("height", FormatDouble(height));
        Builder.Append('>');
    }

    internal void EndPattern()
    {
        Builder.Append("</pattern>");
    }

    internal void WriteImage(RawBitmap bitmap, double x, double y, double width, double height, double opacity, string id, string transform)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        ValidatePositive(width, "width");
        ValidatePositive(height, "height");
        ChartStyle.ValidateOpacity(opacity, "opacity");
        byte[] png = PngEncoder.Encode(bitmap);
        string source = "data:image/png;base64," + System.Convert.ToBase64String(png);
        Builder.Append("<image");
        AppendAttribute("id", id);
        AppendAttribute("x", FormatDouble(x));
        AppendAttribute("y", FormatDouble(y));
        AppendAttribute("width", FormatDouble(width));
        AppendAttribute("height", FormatDouble(height));
        AppendAttribute("href", source);
        AppendAttribute("transform", transform);
        if (opacity < 1.0)
        {
            AppendAttribute("opacity", FormatDouble(opacity));
        }

        AppendPresentation(null, "chart-image");
        Builder.Append("/>");
    }

    internal static string PointsData(ChartPoint[] points)
    {
        ValidatePoints(points, 1);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < points.Length)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(FormatDouble(points[index].X));
            builder.Append(',');
            builder.Append(FormatDouble(points[index].Y));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string PathData(ChartPath path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < path.Commands.Count)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            ChartPathCommand command = path.Commands[index];
            if (command.Kind == ChartPathCommandKind.MoveTo)
            {
                builder.Append("M ");
                builder.Append(FormatDouble(command.X1));
                builder.Append(' ');
                builder.Append(FormatDouble(command.Y1));
            }
            else if (command.Kind == ChartPathCommandKind.LineTo)
            {
                builder.Append("L ");
                builder.Append(FormatDouble(command.X1));
                builder.Append(' ');
                builder.Append(FormatDouble(command.Y1));
            }
            else if (command.Kind == ChartPathCommandKind.QuadraticBezierTo)
            {
                builder.Append("Q ");
                builder.Append(FormatDouble(command.X1));
                builder.Append(' ');
                builder.Append(FormatDouble(command.Y1));
                builder.Append(' ');
                builder.Append(FormatDouble(command.X2));
                builder.Append(' ');
                builder.Append(FormatDouble(command.Y2));
            }
            else if (command.Kind == ChartPathCommandKind.CubicBezierTo)
            {
                builder.Append("C ");
                builder.Append(FormatDouble(command.X1));
                builder.Append(' ');
                builder.Append(FormatDouble(command.Y1));
                builder.Append(' ');
                builder.Append(FormatDouble(command.X2));
                builder.Append(' ');
                builder.Append(FormatDouble(command.Y2));
                builder.Append(' ');
                builder.Append(FormatDouble(command.X3));
                builder.Append(' ');
                builder.Append(FormatDouble(command.Y3));
            }
            else if (command.Kind == ChartPathCommandKind.Close)
            {
                builder.Append('Z');
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void ValidatePoints(ChartPoint[] points, int minimumCount)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        if (points.Length < minimumCount)
        {
            throw new System.ArgumentException("Not enough SVG points.");
        }

        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null)
            {
                throw new System.ArgumentException("SVG points cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateGradient(ChartColor[] colors, double[] offsets)
    {
        if (colors == null)
        {
            throw new System.ArgumentNullException("colors");
        }

        if (offsets == null)
        {
            throw new System.ArgumentNullException("offsets");
        }

        if (colors.Length == 0 || colors.Length != offsets.Length)
        {
            throw new System.ArgumentException("Gradient colors and offsets must have the same nonzero length.");
        }

        int index = 0;
        while (index < colors.Length)
        {
            if (colors[index] == null)
            {
                throw new System.ArgumentException("Gradient colors cannot contain null values.");
            }

            if (offsets[index] < 0.0 || offsets[index] > 1.0 || double.IsNaN(offsets[index]) || double.IsInfinity(offsets[index]))
            {
                throw new System.ArgumentOutOfRangeException("offsets");
            }

            index = index + 1;
        }
    }

    internal static void ValidatePositive(double value, string name)
    {
        if (value <= 0.0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    internal static string Translate(double x, double y)
    {
        return "translate(" + FormatDouble(x) + " " + FormatDouble(y) + ")";
    }

    internal static string Scale(double x, double y)
    {
        return "scale(" + FormatDouble(x) + " " + FormatDouble(y) + ")";
    }

    internal static string Rotate(double angleDegrees, double centerX, double centerY)
    {
        return "rotate(" + FormatDouble(angleDegrees) + " " + FormatDouble(centerX) + " " + FormatDouble(centerY) + ")";
    }

    internal static string Matrix(ChartMatrix matrix)
    {
        if (matrix == null)
        {
            throw new System.ArgumentNullException("matrix");
        }

        return "matrix(" + FormatDouble(matrix.M11) + " " + FormatDouble(matrix.M12) + " " + FormatDouble(matrix.M21) + " " + FormatDouble(matrix.M22) + " " + FormatDouble(matrix.OffsetX) + " " + FormatDouble(matrix.OffsetY) + ")";
    }

    internal static string FormatDouble(double value)
    {
        if (System.Math.Abs(value) < 0.0000005)
        {
            return "0";
        }

        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string EscapeText(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char c = value[index];
            if (c == '&')
            {
                builder.Append("&amp;");
            }
            else if (c == '<')
            {
                builder.Append("&lt;");
            }
            else if (c == '>')
            {
                builder.Append("&gt;");
            }
            else
            {
                builder.Append(c);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string EscapeAttribute(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char c = value[index];
            if (c == '&')
            {
                builder.Append("&amp;");
            }
            else if (c == '<')
            {
                builder.Append("&lt;");
            }
            else if (c == '>')
            {
                builder.Append("&gt;");
            }
            else if (c == '"')
            {
                builder.Append("&quot;");
            }
            else if (c == '\'')
            {
                builder.Append("&apos;");
            }
            else
            {
                builder.Append(c);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string SanitizeIdPrefix(string? prefix)
    {
        if (prefix == null || prefix.Length == 0)
        {
            return "id";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < prefix.Length)
        {
            char c = prefix[index];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_')
            {
                builder.Append(c);
            }
            else
            {
                builder.Append('-');
            }

            index = index + 1;
        }

        if (builder.Length == 0)
        {
            return "id";
        }

        char first = builder[0];
        if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_'))
        {
            builder.Insert(0, "id-");
        }

        return builder.ToString();
    }
}
