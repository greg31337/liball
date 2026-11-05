namespace chart;

internal static class ChartConfigurationValidatorOperations
{
    internal static ChartValidationResult ValidateSeriesData(ChartSeriesData? data)
    {
        if (data == null)
        {
            return MissingResult("data");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (data.RowCount <= 0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidDataCode, "Series row count must be positive.", "data.rowCount", -1);
        }

        int present = 0;
        present = present + CheckNumberChannel(data.XValues, data.RowCount, "data.x", result);
        present = present + CheckNumberChannel(data.YValues, data.RowCount, "data.y", result);
        present = present + CheckNumberChannel(data.ZValues, data.RowCount, "data.z", result);
        present = present + CheckNumberChannel(data.OpenValues, data.RowCount, "data.open", result);
        present = present + CheckNumberChannel(data.HighValues, data.RowCount, "data.high", result);
        present = present + CheckNumberChannel(data.LowValues, data.RowCount, "data.low", result);
        present = present + CheckNumberChannel(data.CloseValues, data.RowCount, "data.close", result);
        present = present + CheckNumberChannel(data.SizeValues, data.RowCount, "data.size", result);
        present = present + CheckNumberChannel(data.ColorValues, data.RowCount, "data.color", result);
        present = present + CheckTextChannel(data.LabelValues, data.RowCount, "data.label", result);
        present = present + CheckTextChannel(data.GroupValues, data.RowCount, "data.group", result);
        if (present == 0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidDataCode, "Series data has no configured channels.", "data", -1);
        }

        return result;
    }

    internal static ChartValidationResult ValidateDataTable(ChartDataTable? table)
    {
        if (table == null)
        {
            return MissingResult("dataTable");
        }
        return ChartDataValidatorOperations.ValidateTable(table);
    }

    internal static ChartValidationResult ValidateDataColumn(ChartDataColumn? column)
    {
        if (column == null)
        {
            return MissingResult("dataColumn");
        }
        return ChartDataValidatorOperations.ValidateColumn(column);
    }

    internal static ChartValidationResult ValidateDataMatrix(ChartDataMatrix? matrix)
    {
        if (matrix == null)
        {
            return MissingResult("dataMatrix");
        }
        return ChartDataValidatorOperations.ValidateMatrix(matrix);
    }

    internal static ChartValidationResult ValidateScalePolicy(ChartScalePolicy? policy)
    {
        if (policy == null)
        {
            return MissingResult("scalePolicy");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (!IsNonnegativeFinite(policy.DomainPadding))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Domain padding must be finite and nonnegative.", "scalePolicy.domainPadding", -1);
        }
        if (policy.NiceTickCount <= 0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Nice tick count must be positive.", "scalePolicy.niceTickCount", -1);
        }
        if (policy.HasUnknownValue && !IsFinite(policy.UnknownValue))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Unknown-value output must be finite.", "scalePolicy.unknownValue", -1);
        }

        int sharing = (int)policy.Sharing;
        if (sharing < 0 || sharing > 2)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Scale sharing mode is invalid.", "scalePolicy.sharing", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateLinearScale(ChartLinearScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("scale");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum == scale.DomainMaximum)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Scale domain must be finite and non-zero.", "scale.domain", -1);
        }
        if (!IsFinite(scale.RangeMinimum) || !IsFinite(scale.RangeMaximum))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Scale range must be finite.", "scale.range", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateLogScale(ChartLogScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("logScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum <= 0.0 || scale.DomainMaximum <= 0.0 || scale.DomainMinimum == scale.DomainMaximum || !IsFinite(scale.BaseValue) || scale.BaseValue <= 0.0 || scale.BaseValue == 1.0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Log scale domain and base are invalid.", "logScale.domain", -1);
        }
        if (!IsFinite(scale.RangeMinimum) || !IsFinite(scale.RangeMaximum))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Log scale range must be finite.", "logScale.range", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidatePowerScale(ChartPowerScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("powerScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum == scale.DomainMaximum || !IsFinite(scale.Exponent) || scale.Exponent <= 0.0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Power scale domain and exponent are invalid.", "powerScale.domain", -1);
        }
        if (!IsFinite(scale.RangeMinimum) || !IsFinite(scale.RangeMaximum) || scale.RangeMinimum == scale.RangeMaximum)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Power scale range must be finite.", "powerScale.range", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateSquareRootScale(ChartSquareRootScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("squareRootScale");
        }
        if (scale.Scale == null)
        {
            return MissingResult("squareRootScale.scale");
        }
        return ValidatePowerScale(scale.Scale);
    }

    internal static ChartValidationResult ValidateSymmetricLogScale(ChartSymmetricLogScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("symmetricLogScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum == scale.DomainMaximum || !IsPositiveFinite(scale.Constant))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Symmetric log scale domain and constant are invalid.", "symmetricLogScale.domain", -1);
        }
        if (!IsFinite(scale.RangeMinimum) || !IsFinite(scale.RangeMaximum) || scale.RangeMinimum == scale.RangeMaximum)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Symmetric log scale range must be finite.", "symmetricLogScale.range", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateTimeScale(ChartTimeScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("timeScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (scale.DomainStart == scale.DomainEnd)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Time scale domain must be non-zero.", "timeScale.domain", -1);
        }
        if (!IsFinite(scale.RangeMinimum) || !IsFinite(scale.RangeMaximum))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Time scale range must be finite.", "timeScale.range", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateUtcTimeScale(ChartUtcTimeScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("utcTimeScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (scale.DomainStart == scale.DomainEnd || scale.DomainStart.Kind != System.DateTimeKind.Utc || scale.DomainEnd.Kind != System.DateTimeKind.Utc)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "UTC time scale domain must be UTC and non-zero.", "utcTimeScale.domain", -1);
        }
        if (!IsFinite(scale.RangeMinimum) || !IsFinite(scale.RangeMaximum) || scale.RangeMinimum == scale.RangeMaximum)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "UTC time scale range must be finite.", "utcTimeScale.range", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateQuantileScale(ChartQuantileScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("quantileScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!AreOrderedValuesValid(scale.Thresholds) || !AreFiniteValues(scale.RangeValues) || scale.RangeValues.Length != scale.Thresholds.Length + 1)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Quantile thresholds and range values are invalid.", "quantileScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateQuantizeScale(ChartQuantizeScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("quantizeScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum == scale.DomainMaximum || !AreFiniteValues(scale.RangeValues) || scale.RangeValues.Length == 0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Quantize domain and range values are invalid.", "quantizeScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateThresholdScale(ChartThresholdScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("thresholdScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!AreThresholdValuesValid(scale.Thresholds) || !AreFiniteValues(scale.RangeValues) || scale.RangeValues.Length != scale.Thresholds.Length + 1)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Threshold values and range values are invalid.", "thresholdScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateDivergingScale(ChartDivergingScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("divergingScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsStrictTriple(scale.DomainMinimum, scale.DomainMidpoint, scale.DomainMaximum) || !IsMonotonicTriple(scale.RangeMinimum, scale.RangeMidpoint, scale.RangeMaximum))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Diverging scale domain or range is invalid.", "divergingScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateRadialScale(ChartRadialScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("radialScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum == scale.DomainMaximum || !IsNonnegativeFinite(scale.RadiusMinimum) || !IsNonnegativeFinite(scale.RadiusMaximum) || scale.RadiusMinimum == scale.RadiusMaximum)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Radial scale domain or radius is invalid.", "radialScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateAngleScale(ChartAngleScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("angleScale");
        }
        if (scale.Scale == null)
        {
            return MissingResult("angleScale.scale");
        }
        return ValidateLinearScale(scale.Scale);
    }

    internal static ChartValidationResult ValidateOpacityScale(ChartOpacityScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("opacityScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum == scale.DomainMaximum || !IsUnit(scale.OpacityMinimum) || !IsUnit(scale.OpacityMaximum) || scale.OpacityMinimum == scale.OpacityMaximum)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Opacity scale domain or range is invalid.", "opacityScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateShapeScale(ChartShapeScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("shapeScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!AreCategoriesValid(scale.Categories) || scale.Shapes == null || scale.Shapes.Length != scale.Categories.Length || !AreMarkerShapesValid(scale.Shapes))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Shape scale categories or marker shapes are invalid.", "shapeScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateCategoryBandScale(ChartCategoryBandScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("categoryBandScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!AreCategoriesValid(scale.Categories) || !IsFinite(scale.RangeMinimum) || !IsFinite(scale.RangeMaximum) || !IsPaddingValid(scale.InnerPadding) || !IsPaddingValid(scale.OuterPadding) || !IsFinite(scale.Step) || !IsFinite(scale.Bandwidth))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Category band scale configuration is invalid.", "categoryBandScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateCategoryPointScale(ChartCategoryPointScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("categoryPointScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!AreCategoriesValid(scale.Categories) || !IsFinite(scale.RangeMinimum) || !IsFinite(scale.RangeMaximum) || !IsPaddingValid(scale.Padding) || !IsFinite(scale.Step))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Category point scale configuration is invalid.", "categoryPointScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateColorScale(ChartColorScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("colorScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum == scale.DomainMaximum || scale.StartColor == null || scale.EndColor == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Color scale domain or colors are invalid.", "colorScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateOrdinalScale(ChartOrdinalScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("ordinalScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!AreCategoriesValid(scale.Categories) || !AreFiniteValues(scale.Values) || scale.Values.Length != scale.Categories.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Ordinal scale categories or values are invalid.", "ordinalScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateSizeScale(ChartSizeScale? scale)
    {
        if (scale == null)
        {
            return MissingResult("sizeScale");
        }
        ChartValidationResult result = new ChartValidationResult();
        if (!IsFinite(scale.DomainMinimum) || !IsFinite(scale.DomainMaximum) || scale.DomainMinimum == scale.DomainMaximum || !IsNonnegativeFinite(scale.SizeMinimum) || !IsNonnegativeFinite(scale.SizeMaximum))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidScaleCode, "Size scale domain or range is invalid.", "sizeScale", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateAxis(ChartAxis? axis)
    {
        if (axis == null)
        {
            return MissingResult("axis");
        }

        ChartValidationResult result = new ChartValidationResult();
        int kind = (int)axis.Kind;
        int placement = (int)axis.Placement;
        if (kind < 0 || kind > 4 || placement < 0 || placement > 5)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis kind or placement is invalid.", "axis.kind", -1);
        }
        if (axis.Id == null || axis.Id.Length == 0 || axis.Title == null || axis.Subtitle == null || axis.Unit == null || axis.LinkedAxisId == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis identifiers and text values cannot be missing.", "axis.text", -1);
        }
        if (!IsRangeValid(axis.Domain))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis domain must be finite and non-zero.", "axis.domain", -1);
        }
        if (!IsRectValid(axis.PlotArea))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis plot area must be finite and positive.", "axis.plotArea", -1);
        }
        if (!IsPointFinite(axis.Center) || !IsPositiveFinite(axis.Radius))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis polar geometry must be finite and positive.", "axis.polar", -1);
        }
        if (!IsNonnegativeFinite(axis.TickLength) || !IsNonnegativeFinite(axis.MinorTickLength) || axis.MinorTickCount < 0 || !IsNonnegativeFinite(axis.LabelOffset) || !IsNonnegativeFinite(axis.TitleOffset) || !IsNonnegativeFinite(axis.GridLength) || !IsNonnegativeFinite(axis.MinimumLabelSpacing))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis lengths and counts must be finite and nonnegative.", "axis.metrics", -1);
        }
        if (!IsPositiveFinite(axis.StrokeWidth) || !IsPositiveFinite(axis.GridStrokeWidth) || !IsPositiveFinite(axis.ZeroLineStrokeWidth) || !IsPositiveFinite(axis.TickStrokeWidth) || !IsPositiveFinite(axis.MinorTickStrokeWidth) || axis.FontSize <= 0 || axis.TickLabelMaximumCharacters < 0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis stroke widths and font sizes must be positive.", "axis.style", -1);
        }
        if (axis.StrokeColor == null || axis.GridColor == null || axis.ZeroLineColor == null || axis.BandColor == null || axis.TextColor == null || axis.TickColor == null || axis.MinorTickColor == null || axis.FontFamily == null || axis.TickFormatter == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "Axis style contains a missing required value.", "axis.style", -1);
        }

        CheckTicks(axis, result);
        CheckAxisThresholds(axis, result);
        CheckAxisBreaks(axis, result);
        return result;
    }

    internal static ChartValidationResult ValidateGrid(ChartGrid? grid)
    {
        if (grid == null)
        {
            return MissingResult("grid");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (grid.Rows <= 0 || grid.Columns <= 0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidLayoutCode, "Grid dimensions must be positive.", "grid.dimensions", -1);
        }
        if (!IsNonnegativeFinite(grid.RowGap) || !IsNonnegativeFinite(grid.ColumnGap))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidLayoutCode, "Grid gaps must be finite and nonnegative.", "grid.gaps", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateLayout(ChartLayoutResult? layout)
    {
        if (layout == null)
        {
            return MissingResult("layout");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (!IsRectValid(layout.FigureRect) || !IsRectValid(layout.ContentRect) || !IsRectValid(layout.PlotArea))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidLayoutCode, "Layout rectangles must be finite and positive.", "layout.bounds", -1);
        }
        ChartLayoutItem[]? items = layout.Items;
        if (items == null || layout.ItemCount < 0 || layout.ItemCount > items.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidLayoutCode, "Layout item storage is invalid.", "layout.items", -1);
            return result;
        }

        int index = 0;
        while (index < layout.ItemCount)
        {
            ChartLayoutItem item = items[index];
            if (item == null || item.Id == null || item.Id.Length == 0 || !IsRectValid(item.Bounds) || (int)item.Kind < 0 || (int)item.Kind > 9 || (int)item.Side < 0 || (int)item.Side > 3)
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidLayoutCode, "Layout item is invalid.", "layout.items", index);
            }
            else if (HasEarlierLayoutId(layout, index, item.Id))
            {
                result.AddError(ChartConfigurationValidatorConstants.DuplicateIdentifierCode, "Layout item identifier is duplicated.", item.Id, index);
            }
            index = index + 1;
        }
        return result;
    }

    internal static ChartValidationResult ValidateStyle(ChartStyle? style)
    {
        if (style == null)
        {
            return MissingResult("style");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (style.FillColor == null || style.StrokeColor == null || style.BorderColor == null || style.TextOutlineColor == null || style.TextShadowColor == null || style.FontFamily == null || style.CustomMarkerPath == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "Chart style contains a missing required value.", "style", -1);
        }
        if ((style.HasStrokeWidth && !IsNonnegativeFinite(style.StrokeWidth)) || (style.HasOpacity && !IsUnit(style.Opacity)) || (style.HasMarkerSize && !IsNonnegativeFinite(style.MarkerSize)) || (style.HasMiterLimit && !IsPositiveFinite(style.MiterLimit)) || (style.HasFillOpacity && !IsUnit(style.FillOpacity)) || (style.HasStrokeOpacity && !IsUnit(style.StrokeOpacity)) || (style.HasBorder && !IsPositiveFinite(style.BorderWidth)) || (style.HasTextOutline && !IsPositiveFinite(style.TextOutlineWidth)) || (style.HasTextShadow && (!IsFinite(style.TextShadowOffsetX) || !IsFinite(style.TextShadowOffsetY) || !IsNonnegativeFinite(style.TextShadowBlur))))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Chart style numeric settings are invalid.", "style.metrics", -1);
        }
        if ((style.HasFontSize && style.FontSize <= 0) || (style.HasFontWeight && ((int)style.FontWeight < 0 || (int)style.FontWeight > 1)) || (style.HasFontStyle && ((int)style.FontStyle < 0 || (int)style.FontStyle > 2)))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Chart text style is invalid.", "style.text", -1);
        }
        if (style.HasDashArray && !IsDashValid(style.DashArray))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Chart dash array is invalid.", "style.dash", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateRenderStyle(ChartRenderStyle? style)
    {
        if (style == null)
        {
            return MissingResult("renderStyle");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (style.FillColor == null || style.StrokeColor == null || style.BorderColor == null || style.TextOutlineColor == null || style.TextShadowColor == null || style.FontFamily == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "Render style contains a missing required value.", "renderStyle", -1);
        }
        if (!IsPositiveFinite(style.StrokeWidth) || !IsUnit(style.Opacity) || style.FontSize <= 0 || style.TextScale <= 0 || !IsPositiveFinite(style.MiterLimit) || !IsUnit(style.FillOpacity) || !IsUnit(style.StrokeOpacity) || (style.HasBorder && !IsPositiveFinite(style.BorderWidth)) || (style.HasTextOutline && !IsPositiveFinite(style.TextOutlineWidth)) || (style.HasTextShadow && (!IsFinite(style.TextShadowOffsetX) || !IsFinite(style.TextShadowOffsetY) || !IsNonnegativeFinite(style.TextShadowBlur))))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Render style numeric settings are invalid.", "renderStyle.metrics", -1);
        }
        if ((int)style.LineCap < 0 || (int)style.LineCap > 2 || (int)style.LineJoin < 0 || (int)style.LineJoin > 2 || (int)style.FillRule < 0 || (int)style.FillRule > 1 || (int)style.Quality < 0 || (int)style.Quality > 1 || (int)style.BlendMode < 0 || (int)style.BlendMode > 6 || (int)style.FontWeight < 0 || (int)style.FontWeight > 1 || (int)style.FontStyle < 0 || (int)style.FontStyle > 2)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Render style enum setting is invalid.", "renderStyle.enums", -1);
        }
        if (!IsDashValid(style.DashArray))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Render dash array is invalid.", "renderStyle.dash", -1);
        }
        if (style.HasFillGradient && !IsGradientValid(style.FillGradientStart, style.FillGradientEnd, style.FillGradientColors, style.FillGradientOffsets))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Fill gradient is invalid.", "renderStyle.fillGradient", -1);
        }
        if (style.HasStrokeGradient && !IsGradientValid(style.StrokeGradientStart, style.StrokeGradientEnd, style.StrokeGradientColors, style.StrokeGradientOffsets))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Stroke gradient is invalid.", "renderStyle.strokeGradient", -1);
        }
        if ((style.HasFillPattern && style.FillPattern == null) || (style.HasStrokePattern && style.StrokePattern == null))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidStyleCode, "Render pattern bitmap is missing.", "renderStyle.pattern", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateRenderCommand(ChartRenderCommand? command)
    {
        if (command == null)
        {
            return MissingResult("renderCommand");
        }

        ChartValidationResult result = new ChartValidationResult();
        ChartRenderValidatorOperations.ValidateCommandInto(command, ChartConfigurationValidatorConstants.InvalidRenderCommandCode, "renderCommand", -1, result);
        if (command.Style != null)
        {
            result.AddResult(ValidateRenderStyle(command.Style));
        }
        return result;
    }

    internal static ChartValidationResult ValidateBmpOptions(ChartBmpOptions? options)
    {
        if (options == null)
        {
            return MissingResult("bmp");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (options.BitDepth != ChartBmpBitDepth.Indexed8 && options.BitDepth != ChartBmpBitDepth.Rgb24 && options.BitDepth != ChartBmpBitDepth.Rgba32)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, "BMP bit depth is invalid.", "bmp.bitDepth", -1);
        }
        if (options.RowOrientation != ChartBmpRowOrientation.TopDown && options.RowOrientation != ChartBmpRowOrientation.BottomUp)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, "BMP row orientation is invalid.", "bmp.rowOrientation", -1);
        }
        if (options.HorizontalPixelsPerMeter < 0 || options.VerticalPixelsPerMeter < 0)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, "BMP resolution must be nonnegative.", "bmp.resolution", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidatePngOptions(ChartPngOptions? options)
    {
        if (options == null)
        {
            return MissingResult("png");
        }

        ChartValidationResult result = new ChartValidationResult();
        int colorType = (int)options.ColorType;
        if (colorType != -1 && colorType != 0 && colorType != 2 && colorType != 3 && colorType != 4 && colorType != 6)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, "PNG color type is invalid.", "png.colorType", -1);
        }
        int filter = (int)options.Filter;
        if (filter < -1 || filter > 4)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, "PNG filter is invalid.", "png.filter", -1);
        }
        int compression = (int)options.Compression;
        if (compression < 0 || compression > 3)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, "PNG compression is invalid.", "png.compression", -1);
        }
        if (options.Metadata == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "PNG metadata is missing.", "png.metadata", -1);
        }
        else if (!IsPngMetadataValid(options.Metadata))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, "PNG metadata contains a missing value or null character.", "png.metadata", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateSvgOptions(ChartSvgOptions? options)
    {
        if (options == null)
        {
            return MissingResult("svg");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (options.IdPrefix == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, "SVG identifier prefix cannot be missing.", "svg.idPrefix", -1);
        }
        return result;
    }

    internal static ChartValidationResult ValidateAccessibility(ChartAccessibilityMetadata? metadata)
    {
        if (metadata == null)
        {
            return MissingResult("accessibility");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (metadata.Title == null || metadata.Description == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Accessibility title and description cannot be missing.", "accessibility.text", -1);
        }
        ChartSeriesAccessibility[]? seriesValues = metadata.Series;
        if (seriesValues == null || metadata.SeriesCount < 0 || metadata.SeriesCount > seriesValues.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Accessibility series storage is invalid.", "accessibility.series", -1);
            return result;
        }

        int index = 0;
        while (index < metadata.SeriesCount)
        {
            ChartSeriesAccessibility series = seriesValues[index];
            CheckAccessibilitySeries(metadata, series, index, result);
            index = index + 1;
        }
        if (metadata.DataTableSidecar != null)
        {
            CheckDataSidecar(metadata.DataTableSidecar, result);
        }
        if (metadata.SelectionSidecar != null)
        {
            CheckSelectionSidecar(metadata.SelectionSidecar, result);
        }
        return result;
    }

    internal static ChartValidationResult ValidateExportOptions(ChartExportOptions? options)
    {
        if (options == null)
        {
            return MissingResult("export");
        }

        ChartValidationResult result = new ChartValidationResult();
        if ((int)options.Format < 0 || (int)options.Format > 3 || (int)options.BackgroundMode < 0 || (int)options.BackgroundMode > 2)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidExportOptionCode, "Export format or background mode is invalid.", "export.format", -1);
        }
        if (options.BackgroundColor == null || options.Scale == null || options.Accessibility == null || options.Bmp == null || options.Png == null || options.Svg == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "Export options contain a missing nested value.", "export", -1);
        }
        if (options.Scale != null && (!IsPositiveFinite(options.Scale.DevicePixelRatio) || !IsPositiveFinite(options.Scale.ExportScale) || !IsPositiveFinite(options.Scale.DevicePixelRatio * options.Scale.ExportScale)))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidExportOptionCode, "Export scales must be finite and positive.", "export.scale", -1);
        }
        if (options.Accessibility != null)
        {
            result.AddResult(ValidateAccessibility(options.Accessibility));
        }
        if (options.Bmp != null)
        {
            result.AddResult(ValidateBmpOptions(options.Bmp));
        }
        if (options.Png != null)
        {
            result.AddResult(ValidatePngOptions(options.Png));
        }
        if (options.Svg != null)
        {
            result.AddResult(ValidateSvgOptions(options.Svg));
        }
        return result;
    }

    internal static ChartValidationResult ValidateTimeline(ChartTimeline? timeline)
    {
        if (timeline == null)
        {
            return MissingResult("timeline");
        }

        ChartValidationResult result = new ChartValidationResult();
        ChartTimelineFrame[]? frames = timeline.Frames;
        if (frames == null || timeline.FrameCount < 0 || timeline.FrameCount > frames.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidTimelineCode, "Timeline frame storage is invalid.", "timeline.frames", -1);
            return result;
        }
        if (timeline.FrameCount == 0)
        {
            result.AddWarning(ChartConfigurationValidatorConstants.InvalidTimelineCode, "Timeline has no frames.", "timeline.frames", -1);
            return result;
        }

        int width = 0;
        int height = 0;
        int index = 0;
        while (index < timeline.FrameCount)
        {
            ChartTimelineFrame frame = frames[index];
            if (frame == null || frame.FrameId == null || frame.FrameId.Length == 0 || frame.Scene == null || frame.DurationMilliseconds <= 0)
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidTimelineCode, "Timeline frame is invalid.", "timeline.frames", index);
            }
            else
            {
                if (index == 0)
                {
                    width = frame.Scene.Width;
                    height = frame.Scene.Height;
                }
                else if (frame.Scene.Width != width || frame.Scene.Height != height)
                {
                    result.AddError(ChartConfigurationValidatorConstants.InvalidTimelineCode, "Timeline frame dimensions differ.", "timeline.frames", index);
                }
                if (HasEarlierFrameId(timeline, index, frame.FrameId))
                {
                    result.AddError(ChartConfigurationValidatorConstants.DuplicateIdentifierCode, "Timeline frame identifier is duplicated.", frame.FrameId, index);
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal static ChartValidationResult MissingResult(string path)
    {
        ChartValidationResult result = new ChartValidationResult();
        result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "Required value is missing.", path, -1);
        return result;
    }

    internal static int CheckNumberChannel(double[] values, int rowCount, string path, ChartValidationResult result)
    {
        if (values == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "Data channel storage is missing.", path, -1);
            return 0;
        }
        if (values.Length == 0)
        {
            return 0;
        }
        if (values.Length != rowCount)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidDataCode, "Data channel length does not match row count.", path, -1);
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!IsFinite(values[index]))
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidDataCode, "Numeric data value must be finite.", path, index);
            }
            index = index + 1;
        }
        return 1;
    }

    internal static int CheckTextChannel(string? [] values, int rowCount, string path, ChartValidationResult result)
    {
        if (values == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "Text channel storage is missing.", path, -1);
            return 0;
        }
        if (values.Length == 0)
        {
            return 0;
        }
        if (values.Length != rowCount)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidDataCode, "Text channel length does not match row count.", path, -1);
        }
        return 1;
    }

    internal static void CheckTicks(ChartAxis axis, ChartValidationResult result)
    {
        if (axis.Ticks == null)
        {
            result.AddError(ChartConfigurationValidatorConstants.MissingRequiredValueCode, "Axis ticks are missing.", "axis.ticks", -1);
            return;
        }
        int index = 0;
        while (index < axis.Ticks.Length)
        {
            ChartTick tick = axis.Ticks[index];
            if (tick == null || tick.Label == null || tick.Index < 0 || (int)tick.Kind < 0 || (int)tick.Kind > 7 || !IsFinite(tick.Value))
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis tick is invalid.", "axis.ticks", index);
            }
            index = index + 1;
        }
    }

    internal static void CheckAxisThresholds(ChartAxis axis, ChartValidationResult result)
    {
        double[]? values = axis.ThresholdValues;
        string[]? labels = axis.ThresholdLabels;
        ChartColor[]? colors = axis.ThresholdColors;
        double[]? widths = axis.ThresholdWidths;
        if (axis.ThresholdCount < 0 || values == null || labels == null || colors == null || widths == null || axis.ThresholdCount > values.Length || axis.ThresholdCount > labels.Length || axis.ThresholdCount > colors.Length || axis.ThresholdCount > widths.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis threshold storage is invalid.", "axis.thresholds", -1);
            return;
        }
        int index = 0;
        while (index < axis.ThresholdCount)
        {
            if (!IsFinite(values[index]) || labels[index] == null || colors[index] == null || !IsPositiveFinite(widths[index]))
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis threshold is invalid.", "axis.thresholds", index);
            }
            index = index + 1;
        }
    }

    internal static void CheckAxisBreaks(ChartAxis axis, ChartValidationResult result)
    {
        double[]? minimums = axis.BreakMinimums;
        double[]? maximums = axis.BreakMaximums;
        if (axis.BreakCount < 0 || minimums == null || maximums == null || axis.BreakCount > minimums.Length || axis.BreakCount > maximums.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis break storage is invalid.", "axis.breaks", -1);
            return;
        }
        int index = 0;
        while (index < axis.BreakCount)
        {
            if (!IsFinite(minimums[index]) || !IsFinite(maximums[index]) || minimums[index] >= maximums[index])
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidAxisCode, "Axis break is invalid.", "axis.breaks", index);
            }
            index = index + 1;
        }
    }

    internal static bool HasEarlierLayoutId(ChartLayoutResult layout, int end, string id)
    {
        int index = 0;
        while (index < end)
        {
            ChartLayoutItem item = layout.Items[index];
            if (item != null && item.Id == id)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsDashValid(double[] values)
    {
        if (values == null)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!IsPositiveFinite(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsGradientValid(ChartPoint start, ChartPoint end, ChartColor[] colors, double[] offsets)
    {
        if (!IsPointFinite(start) || !IsPointFinite(end) || colors == null || offsets == null || colors.Length < 2 || colors.Length != offsets.Length)
        {
            return false;
        }
        int index = 0;
        double previous = -1.0;
        while (index < colors.Length)
        {
            if (colors[index] == null || !IsUnit(offsets[index]) || offsets[index] < previous)
            {
                return false;
            }
            previous = offsets[index];
            index = index + 1;
        }
        return true;
    }

    internal static bool IsPngMetadataValid(ChartPngMetadata metadata)
    {
        return IsMetadataValueValid(metadata.Title) && IsMetadataValueValid(metadata.Author) && IsMetadataValueValid(metadata.Description) && IsMetadataValueValid(metadata.Copyright) && IsMetadataValueValid(metadata.Software);
    }

    internal static bool IsMetadataValueValid(string value)
    {
        return value != null && value.IndexOf('\0') < 0;
    }

    internal static void CheckAccessibilitySeries(ChartAccessibilityMetadata metadata, ChartSeriesAccessibility series, int seriesIndex, ChartValidationResult result)
    {
        ChartPointAccessibility[]? points = series == null ? null : series.Points;
        if (series == null || series.SeriesId == null || series.SeriesId.Length == 0 || series.Description == null || points == null || series.PointCount < 0 || series.PointCount > points.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Accessibility series is invalid.", "accessibility.series", seriesIndex);
            return;
        }
        int earlier = 0;
        while (earlier < seriesIndex)
        {
            ChartSeriesAccessibility prior = metadata.Series[earlier];
            if (prior != null && prior.SeriesId == series.SeriesId)
            {
                result.AddError(ChartConfigurationValidatorConstants.DuplicateIdentifierCode, "Accessibility series identifier is duplicated.", series.SeriesId, seriesIndex);
            }
            earlier = earlier + 1;
        }
        if (series.HasNonColorCues && ((int)series.CueShape < 0 || (int)series.CueShape > 8 || !IsDashValid(series.CueDashArray) || series.CueLabel == null || series.CuePatternDescription == null))
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Non-color cue metadata is invalid.", "accessibility.series", seriesIndex);
        }
        int pointIndex = 0;
        while (pointIndex < series.PointCount)
        {
            ChartPointAccessibility point = points[pointIndex];
            if (point == null || point.PointId == null || point.PointId.Length == 0 || point.Description == null || point.RowIndex < 0 || HasEarlierPointId(series, pointIndex, point == null ? "" : point.PointId))
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Accessibility point is invalid or duplicated.", series.SeriesId, pointIndex);
            }
            pointIndex = pointIndex + 1;
        }
    }

    internal static bool HasEarlierPointId(ChartSeriesAccessibility series, int end, string id)
    {
        ChartPointAccessibility[] points = series.Points;
        int index = 0;
        while (index < end)
        {
            ChartPointAccessibility point = points[index];
            if (point != null && point.PointId == id)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void CheckDataSidecar(ChartDataTableSidecarPlan plan, ChartValidationResult result)
    {
        string[]? names = plan.ColumnNames;
        ChartDataKind[]? kinds = plan.ColumnKinds;
        if (plan.TableId == null || plan.TableId.Length == 0 || plan.Caption == null || plan.RowCount < 0 || plan.ColumnCount < 0 || names == null || kinds == null || plan.ColumnCount > names.Length || plan.ColumnCount > kinds.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Data-table sidecar is invalid.", "accessibility.dataTable", -1);
            return;
        }
        int index = 0;
        while (index < plan.ColumnCount)
        {
            if (names[index] == null || names[index].Length == 0 || (int)kinds[index] < 0 || (int)kinds[index] > 4)
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Data-table sidecar column is invalid.", "accessibility.dataTable", index);
            }
            index = index + 1;
        }
    }

    internal static void CheckSelectionSidecar(ChartSelectionSidecar sidecar, ChartValidationResult result)
    {
        ChartSelection[]? selections = sidecar.Selections;
        ChartBrush[]? brushes = sidecar.Brushes;
        if (selections == null || sidecar.SelectionCount < 0 || sidecar.SelectionCount > selections.Length || brushes == null || sidecar.BrushCount < 0 || sidecar.BrushCount > brushes.Length)
        {
            result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Selection sidecar storage is invalid.", "accessibility.selection", -1);
            return;
        }
        int index = 0;
        while (index < sidecar.SelectionCount)
        {
            ChartSelection selection = selections[index];
            if (selection == null || selection.SelectionId == null || selection.SelectionId.Length == 0 || selection.LinkGroup == null || selection.LinkGroup.Length == 0 || selection.SelectedRows == null || selection.HighlightedRows == null)
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Selection snapshot is invalid.", "accessibility.selection", index);
            }
            index = index + 1;
        }
        index = 0;
        while (index < sidecar.BrushCount)
        {
            ChartBrush brush = brushes[index];
            if (brush == null || brush.BrushId == null || brush.BrushId.Length == 0 || brush.SourceId == null || brush.SourceId.Length == 0 || brush.LinkGroup == null || brush.LinkGroup.Length == 0 || !IsRectValid(brush.Bounds) || brush.Rows == null)
            {
                result.AddError(ChartConfigurationValidatorConstants.InvalidAccessibilityCode, "Linked brush snapshot is invalid.", "accessibility.brush", index);
            }
            index = index + 1;
        }
    }

    internal static bool HasEarlierFrameId(ChartTimeline timeline, int end, string id)
    {
        int index = 0;
        while (index < end)
        {
            ChartTimelineFrame frame = timeline.Frames[index];
            if (frame != null && frame.FrameId == id)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsRangeValid(ChartRange range)
    {
        return range != null && IsFinite(range.Minimum) && IsFinite(range.Maximum) && range.Minimum != range.Maximum;
    }

    internal static bool IsRectValid(ChartRect rect)
    {
        return rect != null && IsFinite(rect.X) && IsFinite(rect.Y) && IsPositiveFinite(rect.Width) && IsPositiveFinite(rect.Height);
    }

    internal static bool IsPointFinite(ChartPoint point)
    {
        return point != null && IsFinite(point.X) && IsFinite(point.Y);
    }

    internal static bool AreFiniteValues(double[] values)
    {
        if (values == null)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!IsFinite(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool AreThresholdValuesValid(double[] values)
    {
        if (!AreFiniteValues(values))
        {
            return false;
        }
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] <= values[index - 1])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool AreOrderedValuesValid(double[] values)
    {
        if (!AreFiniteValues(values))
        {
            return false;
        }
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] < values[index - 1])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool AreCategoriesValid(string[] categories)
    {
        if (categories == null || categories.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < categories.Length)
        {
            if (categories[index] == null || categories[index].Length == 0)
            {
                return false;
            }
            int previous = 0;
            while (previous < index)
            {
                if (categories[previous] == categories[index])
                {
                    return false;
                }
                previous = previous + 1;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool AreMarkerShapesValid(ChartMarkerShape[] shapes)
    {
        if (shapes == null)
        {
            return false;
        }
        int index = 0;
        while (index < shapes.Length)
        {
            if ((int)shapes[index] < 0 || (int)shapes[index] > 8)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsStrictTriple(double first, double middle, double last)
    {
        return IsFinite(first) && IsFinite(middle) && IsFinite(last) && first < middle && middle < last;
    }

    internal static bool IsMonotonicTriple(double first, double middle, double last)
    {
        if (!IsFinite(first) || !IsFinite(middle) || !IsFinite(last))
        {
            return false;
        }
        return (first < middle && middle < last) || (first > middle && middle > last);
    }

    internal static bool IsPaddingValid(double value)
    {
        return IsFinite(value) && value >= 0.0 && value < 1.0;
    }

    internal static bool IsUnit(double value)
    {
        return IsFinite(value) && value >= 0.0 && value <= 1.0;
    }

    internal static bool IsNonnegativeFinite(double value)
    {
        return IsFinite(value) && value >= 0.0;
    }

    internal static bool IsPositiveFinite(double value)
    {
        return IsFinite(value) && value > 0.0;
    }

    internal static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
