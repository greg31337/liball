namespace chart;
/// <summary>
/// Represents a visual map used by the chart model or rendering pipeline.
/// </summary>
public class ChartVisualMap : IChartVisualMap
{
    internal ChartVisualMapKind Kind;
    internal ChartEncodingChannel Channel;
    internal double DomainMinimum;
    internal double DomainMaximum;
    internal ChartColorMap ColorMap;
    internal ChartPalette Palette;
    internal ChartVisualMapBin[] Bins;
    internal int BinCount;
    internal double[] Thresholds;
    internal ChartColor[] ThresholdColors;
    internal ChartVisualMapCategory[] Categories;
    internal int CategoryCount;
    internal string Title;
    /// <summary>
    /// Initializes a visual map from the supplied kind and channel.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "channel">The channel.</param>
    public ChartVisualMap(ChartVisualMapKind kind, ChartEncodingChannel channel)
    {
        ValidateKind(kind);
        ValidateVisualChannel(channel);
        Kind = kind;
        Channel = channel;
        DomainMinimum = 0.0;
        DomainMaximum = 1.0;
        ColorMap = ChartPalettesOperations.GrayscaleMap();
        Palette = ChartPalettesOperations.Categorical();
        Bins = new ChartVisualMapBin[0];
        BinCount = 0;
        Thresholds = new double[0];
        ThresholdColors = new ChartColor[0];
        Categories = new ChartVisualMapCategory[0];
        CategoryCount = 0;
        Title = "";
    }

    internal static ChartVisualMap CreateContinuous(ChartEncodingChannel channel, double minimum, double maximum, ChartColorMap colorMap)
    {
        ValidateVisualChannel(channel);
        ChartLinearScale.ValidateDomain(minimum, maximum);
        ValidateColorMap(colorMap);
        ChartVisualMap map = new ChartVisualMap(ChartVisualMapKind.Continuous, channel);
        map.DomainMinimum = minimum;
        map.DomainMaximum = maximum;
        map.ColorMap = CopyColorMap(colorMap);
        return map;
    }

    internal static ChartVisualMap CreateBinned(ChartEncodingChannel channel, double minimum, double maximum, int binCount, ChartColorMap colorMap)
    {
        ValidateVisualChannel(channel);
        ChartLinearScale.ValidateDomain(minimum, maximum);
        ValidateBinCount(binCount);
        ValidateColorMap(colorMap);
        ChartVisualMap map = new ChartVisualMap(ChartVisualMapKind.Binned, channel);
        map.DomainMinimum = minimum;
        map.DomainMaximum = maximum;
        map.ColorMap = CopyColorMap(colorMap);
        map.BinCount = binCount;
        map.Bins = new ChartVisualMapBin[binCount];
        int index = 0;
        while (index < binCount)
        {
            double binMinimum = minimum + (maximum - minimum) * index / binCount;
            double binMaximum = minimum + (maximum - minimum) * (index + 1) / binCount;
            double amount = (index + 0.5) / binCount;
            string label = FormatRangeLabel(binMinimum, binMaximum);
            map.Bins[index] = new ChartVisualMapBin(binMinimum, binMaximum, colorMap.GetColor(amount), label);
            index = index + 1;
        }

        return map;
    }

    internal static ChartVisualMap CreateThreshold(ChartEncodingChannel channel, double[] thresholds, ChartColor[] colors)
    {
        ValidateVisualChannel(channel);
        ValidateThresholds(thresholds, colors);
        ChartVisualMap map = new ChartVisualMap(ChartVisualMapKind.Threshold, channel);
        map.Thresholds = CopyDoubleArray(thresholds);
        map.ThresholdColors = ChartPalette.CopyColorArray(colors);
        return map;
    }

    internal static ChartVisualMap CreateCategorical(ChartEncodingChannel channel, string[] categories, ChartPalette palette)
    {
        ValidateVisualChannel(channel);
        ChartCategoryBandScale.ValidateCategories(categories);
        ValidatePalette(palette);
        ChartVisualMap map = new ChartVisualMap(ChartVisualMapKind.Categorical, channel);
        map.Palette = CopyPalette(palette);
        map.CategoryCount = categories.Length;
        map.Categories = new ChartVisualMapCategory[categories.Length];
        int index = 0;
        while (index < categories.Length)
        {
            map.Categories[index] = new ChartVisualMapCategory(categories[index], palette.GetColor(index));
            index = index + 1;
        }

        return map;
    }

    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    public void SetTitle(string title)
    {
        ChartTextBlock.ValidateText(title, "title");
        Title = title;
    }

    /// <summary>
    /// Maps number through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting color.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public ChartColor MapNumber(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (Kind == ChartVisualMapKind.Categorical)
        {
            throw new System.InvalidOperationException("Categorical visual maps require category values.");
        }

        if (Kind == ChartVisualMapKind.Continuous)
        {
            double amount = (value - DomainMinimum) / (DomainMaximum - DomainMinimum);
            return ColorMap.GetColor(amount);
        }

        if (Kind == ChartVisualMapKind.Binned)
        {
            int binIndex = BinIndex(value);
            return Bins[binIndex].Color.Copy();
        }

        int thresholdIndex = ThresholdIndex(value);
        return ThresholdColors[thresholdIndex].Copy();
    }

    /// <summary>
    /// Maps category through the configured input and output domains.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting color.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public ChartColor MapCategory(string category)
    {
        ChartVisualMapCategory.ValidateCategory(category, "category");
        if (Kind != ChartVisualMapKind.Categorical)
        {
            throw new System.InvalidOperationException("Numeric visual maps require numeric values.");
        }

        int index = CategoryIndex(category);
        return Categories[index].Color.Copy();
    }

    /// <summary>
    /// Applies the build legend operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting legend.</returns>
    public ChartLegend BuildLegend(string id)
    {
        ChartLegend legend = new ChartLegend(id);
        legend.SetTitle(Title);
        if (Kind == ChartVisualMapKind.Continuous)
        {
            legend.AddGradientEntry(id + "-gradient", FormatRangeLabel(DomainMinimum, DomainMaximum), ColorMapStops(ColorMap), ColorMapOffsets(ColorMap));
            return legend;
        }

        if (Kind == ChartVisualMapKind.Binned)
        {
            AddBinLegendEntries(legend, id);
            return legend;
        }

        if (Kind == ChartVisualMapKind.Threshold)
        {
            AddThresholdLegendEntries(legend, id);
            return legend;
        }

        AddCategoryLegendEntries(legend, id);
        return legend;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartVisualMap Copy()
    {
        ChartVisualMap map = new ChartVisualMap(Kind, Channel);
        map.DomainMinimum = DomainMinimum;
        map.DomainMaximum = DomainMaximum;
        map.ColorMap = CopyColorMap(ColorMap);
        map.Palette = CopyPalette(Palette);
        map.Bins = CopyBins(Bins);
        map.BinCount = BinCount;
        map.Thresholds = CopyDoubleArray(Thresholds);
        map.ThresholdColors = ChartPalette.CopyColorArray(ThresholdColors);
        map.Categories = CopyCategories(Categories);
        map.CategoryCount = CategoryCount;
        map.Title = Title;
        return map;
    }

    /// <summary>
    /// Groups values into index using the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public int BinIndex(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (Kind != ChartVisualMapKind.Binned)
        {
            throw new System.InvalidOperationException("Bin index is available only for binned visual maps.");
        }

        if (value <= DomainMinimum)
        {
            return 0;
        }

        if (value >= DomainMaximum)
        {
            return BinCount - 1;
        }

        double amount = (value - DomainMinimum) / (DomainMaximum - DomainMinimum);
        int index = (int)System.Math.Floor(amount * BinCount);
        if (index < 0)
        {
            return 0;
        }

        if (index >= BinCount)
        {
            return BinCount - 1;
        }

        return index;
    }

    /// <summary>
    /// Returns the zero-based threshold index.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public int ThresholdIndex(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (Kind != ChartVisualMapKind.Threshold)
        {
            throw new System.InvalidOperationException("Threshold index is available only for threshold visual maps.");
        }

        int index = 0;
        while (index < Thresholds.Length)
        {
            if (value < Thresholds[index])
            {
                return index;
            }

            index = index + 1;
        }

        return Thresholds.Length;
    }

    /// <summary>
    /// Returns the zero-based category index.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public int CategoryIndex(string category)
    {
        ChartVisualMapCategory.ValidateCategory(category, "category");
        if (Kind != ChartVisualMapKind.Categorical)
        {
            throw new System.InvalidOperationException("Category index is available only for categorical visual maps.");
        }

        int index = 0;
        while (index < CategoryCount)
        {
            if (Categories[index].Category == category)
            {
                return index;
            }

            index = index + 1;
        }

        throw new System.ArgumentException("Category was not found.");
    }

    internal void AddBinLegendEntries(ChartLegend legend, string id)
    {
        if (legend == null)
        {
            throw new System.ArgumentNullException("legend");
        }

        ChartPlot.ValidateId(id);
        int index = 0;
        while (index < BinCount)
        {
            legend.AddSymbolEntry(id + "-bin-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), Bins[index].Label, ChartMarkerShape.Square, CreateColorStyle(Bins[index].Color));
            index = index + 1;
        }
    }

    internal void AddThresholdLegendEntries(ChartLegend legend, string id)
    {
        if (legend == null)
        {
            throw new System.ArgumentNullException("legend");
        }

        ChartPlot.ValidateId(id);
        int index = 0;
        while (index < ThresholdColors.Length)
        {
            legend.AddSymbolEntry(id + "-threshold-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), ThresholdLabel(index), ChartMarkerShape.Square, CreateColorStyle(ThresholdColors[index]));
            index = index + 1;
        }
    }

    internal void AddCategoryLegendEntries(ChartLegend legend, string id)
    {
        if (legend == null)
        {
            throw new System.ArgumentNullException("legend");
        }

        ChartPlot.ValidateId(id);
        int index = 0;
        while (index < CategoryCount)
        {
            legend.AddSymbolEntry(id + "-category-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), Categories[index].Category, ChartMarkerShape.Square, CreateColorStyle(Categories[index].Color));
            index = index + 1;
        }
    }

    internal string ThresholdLabel(int index)
    {
        if (index < 0 || index >= ThresholdColors.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        if (Thresholds.Length == 0)
        {
            return "all";
        }

        if (index == 0)
        {
            return "< " + ChartTickFormatterOperations.FormatNumber(Thresholds[0], 3);
        }

        if (index == Thresholds.Length)
        {
            return ">= " + ChartTickFormatterOperations.FormatNumber(Thresholds[Thresholds.Length - 1], 3);
        }

        return FormatRangeLabel(Thresholds[index - 1], Thresholds[index]);
    }

    internal static ChartStyle CreateColorStyle(ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartStyle style = new ChartStyle();
        style.SetFill(color);
        style.SetNoStroke();
        return style;
    }

    internal static string FormatRangeLabel(double minimum, double maximum)
    {
        ChartLinearScale.ValidateFinite(minimum, "minimum");
        ChartLinearScale.ValidateFinite(maximum, "maximum");
        return ChartTickFormatterOperations.FormatNumber(minimum, 3) + " - " + ChartTickFormatterOperations.FormatNumber(maximum, 3);
    }

    internal static ChartColor[] ColorMapStops(ChartColorMap colorMap)
    {
        ValidateColorMap(colorMap);
        return ChartPalette.CopyColorArray(colorMap.Stops);
    }

    internal static double[] ColorMapOffsets(ChartColorMap colorMap)
    {
        ValidateColorMap(colorMap);
        double[] offsets = new double[colorMap.Stops.Length];
        if (colorMap.Stops.Length == 1)
        {
            offsets[0] = 0.0;
            return offsets;
        }

        int index = 0;
        while (index < colorMap.Stops.Length)
        {
            offsets[index] = index / (double)(colorMap.Stops.Length - 1);
            index = index + 1;
        }

        return offsets;
    }

    internal static ChartVisualMapBin[] CopyBins(ChartVisualMapBin[] bins)
    {
        if (bins == null)
        {
            throw new System.ArgumentNullException("bins");
        }

        ChartVisualMapBin[] copy = new ChartVisualMapBin[bins.Length];
        int index = 0;
        while (index < bins.Length)
        {
            if (bins[index] == null)
            {
                throw new System.ArgumentException("Visual map bins cannot contain null values.");
            }

            copy[index] = bins[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static ChartVisualMapCategory[] CopyCategories(ChartVisualMapCategory[] categories)
    {
        if (categories == null)
        {
            throw new System.ArgumentNullException("categories");
        }

        ChartVisualMapCategory[] copy = new ChartVisualMapCategory[categories.Length];
        int index = 0;
        while (index < categories.Length)
        {
            if (categories[index] == null)
            {
                throw new System.ArgumentException("Visual map categories cannot contain null values.");
            }

            copy[index] = categories[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static double[] CopyDoubleArray(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        double[] copy = new double[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static ChartColorMap CopyColorMap(ChartColorMap colorMap)
    {
        ValidateColorMap(colorMap);
        return new ChartColorMap(colorMap.Name, colorMap.Stops);
    }

    internal static ChartPalette CopyPalette(ChartPalette palette)
    {
        ValidatePalette(palette);
        return new ChartPalette(palette.Name, palette.Colors);
    }

    internal static void ValidateKind(ChartVisualMapKind kind)
    {
        int value = (int)kind;
        if (value < 0 || value > 3)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }

    internal static void ValidateVisualChannel(ChartEncodingChannel channel)
    {
        ChartEncodingBinding.ValidateChannel(channel);
        if (channel != ChartEncodingChannel.Color && channel != ChartEncodingChannel.Size && channel != ChartEncodingChannel.Opacity)
        {
            throw new System.ArgumentException("Visual maps can target color, size, or opacity channels.");
        }
    }

    internal static void ValidateBinCount(int binCount)
    {
        if (binCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("binCount");
        }
    }

    internal static void ValidateColorMap(ChartColorMap colorMap)
    {
        if (colorMap == null)
        {
            throw new System.ArgumentNullException("colorMap");
        }

        if (colorMap.Stops == null || colorMap.Stops.Length == 0)
        {
            throw new System.ArgumentException("Visual map color map requires at least one stop.");
        }
    }

    internal static void ValidatePalette(ChartPalette palette)
    {
        if (palette == null)
        {
            throw new System.ArgumentNullException("palette");
        }

        if (palette.Colors == null || palette.Colors.Length == 0)
        {
            throw new System.ArgumentException("Visual map palette requires at least one color.");
        }
    }

    internal static void ValidateThresholds(double[] thresholds, ChartColor[] colors)
    {
        if (thresholds == null)
        {
            throw new System.ArgumentNullException("thresholds");
        }

        if (colors == null)
        {
            throw new System.ArgumentNullException("colors");
        }

        if (colors.Length != thresholds.Length + 1)
        {
            throw new System.ArgumentException("Threshold visual maps require one more color than threshold.");
        }

        int index = 0;
        while (index < thresholds.Length)
        {
            ChartLinearScale.ValidateFinite(thresholds[index], "thresholds");
            if (index > 0 && thresholds[index] <= thresholds[index - 1])
            {
                throw new System.ArgumentException("Thresholds must be strictly increasing.");
            }

            index = index + 1;
        }

        ChartPalette.CopyColorArray(colors);
    }

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartVisualMapKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartVisualMapKind value)
    {
        Kind = value;
    }

    /// <summary>Returns the current channel.</summary>
    /// <returns>The current field value.</returns>
    public ChartEncodingChannel GetChannel()
    {
        return Channel;
    }

    /// <summary>Replaces the current channel.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetChannel(ChartEncodingChannel value)
    {
        Channel = value;
    }

    /// <summary>Returns the current domain minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetDomainMinimum()
    {
        return DomainMinimum;
    }

    /// <summary>Replaces the current domain minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomainMinimum(double value)
    {
        DomainMinimum = value;
    }

    /// <summary>Returns the current domain maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetDomainMaximum()
    {
        return DomainMaximum;
    }

    /// <summary>Replaces the current domain maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDomainMaximum(double value)
    {
        DomainMaximum = value;
    }

    /// <summary>Returns the current color map.</summary>
    /// <returns>The current field value.</returns>
    public ChartColorMap GetColorMap()
    {
        return ColorMap;
    }

    /// <summary>Replaces the current color map.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColorMap(ChartColorMap value)
    {
        ColorMap = value;
    }

    /// <summary>Returns the current palette.</summary>
    /// <returns>The current field value.</returns>
    public ChartPalette GetPalette()
    {
        return Palette;
    }

    /// <summary>Replaces the current palette.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPalette(ChartPalette value)
    {
        Palette = value;
    }

    /// <summary>Returns the current bins.</summary>
    /// <returns>The current field value.</returns>
    public ChartVisualMapBin[] GetBins()
    {
        return Bins;
    }

    /// <summary>Replaces the current bins.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBins(ChartVisualMapBin[] value)
    {
        Bins = value;
    }

    /// <summary>Returns the current bin count.</summary>
    /// <returns>The current field value.</returns>
    public int GetBinCount()
    {
        return BinCount;
    }

    /// <summary>Replaces the current bin count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBinCount(int value)
    {
        BinCount = value;
    }

    /// <summary>Returns the current thresholds.</summary>
    /// <returns>The current field value.</returns>
    public double[] GetThresholds()
    {
        return Thresholds;
    }

    /// <summary>Replaces the current thresholds.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetThresholds(double[] value)
    {
        Thresholds = value;
    }

    /// <summary>Returns the current threshold colors.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor[] GetThresholdColors()
    {
        return ThresholdColors;
    }

    /// <summary>Replaces the current threshold colors.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetThresholdColors(ChartColor[] value)
    {
        ThresholdColors = value;
    }

    /// <summary>Returns the current categories.</summary>
    /// <returns>The current field value.</returns>
    public ChartVisualMapCategory[] GetCategories()
    {
        return Categories;
    }

    /// <summary>Replaces the current categories.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCategories(ChartVisualMapCategory[] value)
    {
        Categories = value;
    }

    /// <summary>Returns the current category count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCategoryCount()
    {
        return CategoryCount;
    }

    /// <summary>Replaces the current category count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCategoryCount(int value)
    {
        CategoryCount = value;
    }

    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    public string GetTitle()
    {
        return Title;
    }
}
