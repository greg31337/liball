namespace chart.Tests;

[TestFixture]
public class ChartEncodingDeclarativePublicOperationsTests
{
    [Test]
    public void EncodingBindingAccessors_ExposeAndCopyEveryValueKind()
    {
        ChartEncodingBinding binding = new ChartEncodingBinding(ChartEncodingChannel.X, "x", ChartDataKind.Number);
        ChartColor color = ChartColor.FromRgb(10, 20, 30);
        binding.SetChannel(ChartEncodingChannel.FillColor);
        binding.SetFieldName("fill");
        binding.SetFieldKind(ChartDataKind.Category);
        binding.SetIsConstant(true);
        binding.SetNumberValue(4.5);
        binding.SetTextValue("fixed");
        binding.SetColorValue(color);
        binding.SetShapeValue(ChartMarkerShape.Diamond);

        Assert.That(binding.GetChannel(), Is.EqualTo(ChartEncodingChannel.FillColor));
        Assert.That(binding.GetFieldName(), Is.EqualTo("fill"));
        Assert.That(binding.GetFieldKind(), Is.EqualTo(ChartDataKind.Category));
        Assert.That(binding.GetIsConstant(), Is.True);
        Assert.That(binding.GetNumberValue(), Is.EqualTo(4.5));
        Assert.That(binding.GetTextValue(), Is.EqualTo("fixed"));
        Assert.That(binding.GetColorValue(), Is.SameAs(color));
        Assert.That(binding.GetShapeValue(), Is.EqualTo(ChartMarkerShape.Diamond));

        ChartEncodingBinding copy = binding.Copy();
        color.SetR(200);
        Assert.That(copy.GetColorValue().GetR(), Is.EqualTo(10));
    }

    [Test]
    public void VisualEncoding_PublicCollectionAndConditionalOperations_AreObservable()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        ChartEncodingBinding first = encoding.SetField(ChartEncodingChannel.X, "x", ChartDataKind.Number);
        ChartEncodingBinding second = new ChartEncodingBinding(ChartEncodingChannel.Y, "y", ChartDataKind.Number);
        encoding.SetBinding(second);

        Assert.That(encoding.BindingIndex(ChartEncodingChannel.X), Is.EqualTo(0));
        Assert.That(encoding.BindingIndex(ChartEncodingChannel.Y), Is.EqualTo(1));
        Assert.That(encoding.GetBindings()[0], Is.SameAs(first));
        Assert.That(encoding.GetBindingCount(), Is.EqualTo(2));

        ChartEncodingBinding replacement = new ChartEncodingBinding(ChartEncodingChannel.Text, "label", ChartDataKind.Text);
        encoding.SetBindings(new ChartEncodingBinding[] { replacement });
        encoding.SetBindingCount(1);
        Assert.That(encoding.BindingAt(0), Is.SameAs(replacement));
        Assert.That(encoding.HasChannel(ChartEncodingChannel.Text), Is.True);

        ChartConditionalEncoding condition = new ChartConditionalEncoding(ChartEncodingChannel.FillColor);
        ChartEncodingBindingFactory factory = new ChartEncodingBindingFactory();
        condition.SetSelectedBinding(factory.CreateColorConstant(ChartEncodingChannel.FillColor, ChartColor.White()));
        encoding.SetConditionalEncoding(condition);
        Assert.That(encoding.HasConditionalEncoding(ChartEncodingChannel.FillColor), Is.True);
        Assert.That(encoding.ConditionalEncodingFor(ChartEncodingChannel.FillColor).GetChannel(), Is.EqualTo(ChartEncodingChannel.FillColor));

        ChartSelection selection = new ChartSelection("selection", "shared");
        encoding.SetSelection(selection);
        encoding.ClearSelection();
        Assert.That(encoding.Copy().Selection, Is.Null);
        Assert.Throws<System.ArgumentException>(() => encoding.ConditionalEncodingFor(ChartEncodingChannel.StrokeColor));
    }

    [Test]
    public void VisualEncoding_AllChannelChoicesCanBeBoundAndValidated()
    {
        ChartEncodingChannel[] channels = new ChartEncodingChannel[]
        {
            ChartEncodingChannel.X, ChartEncodingChannel.Y, ChartEncodingChannel.X2, ChartEncodingChannel.Y2,
            ChartEncodingChannel.Color, ChartEncodingChannel.Size, ChartEncodingChannel.Shape, ChartEncodingChannel.Opacity,
            ChartEncodingChannel.Text, ChartEncodingChannel.Tooltip, ChartEncodingChannel.Order, ChartEncodingChannel.Detail,
            ChartEncodingChannel.Row, ChartEncodingChannel.Column, ChartEncodingChannel.Radius, ChartEncodingChannel.Angle,
            ChartEncodingChannel.Latitude, ChartEncodingChannel.Longitude, ChartEncodingChannel.StrokeColor,
            ChartEncodingChannel.FillColor, ChartEncodingChannel.StrokeWidth, ChartEncodingChannel.Dash,
            ChartEncodingChannel.ValueLabel, ChartEncodingChannel.ZIndex, ChartEncodingChannel.Wrap
        };
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        int index = 0;
        while (index < channels.Length)
        {
            string field = "field" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            encoding.SetField(channels[index], field, ChartDataKind.Number);
            Assert.That(encoding.BindingFor(channels[index]).GetFieldName(), Is.EqualTo(field));
            index = index + 1;
        }

        Assert.That(encoding.GetBindingCount(), Is.EqualTo(channels.Length));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => encoding.SetField((ChartEncodingChannel)99, "bad", ChartDataKind.Number));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => encoding.BindingIndex((ChartEncodingChannel)99));
    }

    [Test]
    public void VisualMapAccessors_ConfigureEveryMapKindAndVisualChannel()
    {
        ChartColorMap grayscale = new ChartColorMap("gray", new ChartColor[] { ChartColor.Black(), ChartColor.White() });
        ChartPalette palette = new ChartPalette("palette", new ChartColor[] { ChartColor.FromRgb(255, 0, 0), ChartColor.FromRgb(0, 0, 255) });

        ChartVisualMap continuous = new ChartVisualMap(ChartVisualMapKind.Continuous, ChartEncodingChannel.Color);
        continuous.SetDomainMinimum(-1.0);
        continuous.SetDomainMaximum(1.0);
        continuous.SetColorMap(grayscale);
        continuous.SetTitle("Continuous");
        Assert.That(continuous.GetKind(), Is.EqualTo(ChartVisualMapKind.Continuous));
        Assert.That(continuous.GetChannel(), Is.EqualTo(ChartEncodingChannel.Color));
        Assert.That(continuous.GetDomainMinimum(), Is.EqualTo(-1.0));
        Assert.That(continuous.GetDomainMaximum(), Is.EqualTo(1.0));
        Assert.That(continuous.GetColorMap(), Is.SameAs(grayscale));
        Assert.That(continuous.GetTitle(), Is.EqualTo("Continuous"));
        Assert.That(continuous.MapNumber(0.0).GetR(), Is.EqualTo(128));

        ChartVisualMap binned = new ChartVisualMap(ChartVisualMapKind.Binned, ChartEncodingChannel.Size);
        ChartVisualMapBin[] bins = new ChartVisualMapBin[]
        {
            new ChartVisualMapBin(0.0, 5.0, ChartColor.Black(), "low"),
            new ChartVisualMapBin(5.0, 10.0, ChartColor.White(), "high")
        };
        binned.SetDomainMinimum(0.0);
        binned.SetDomainMaximum(10.0);
        binned.SetBins(bins);
        binned.SetBinCount(2);
        Assert.That(binned.GetBins(), Is.SameAs(bins));
        Assert.That(binned.GetBinCount(), Is.EqualTo(2));
        Assert.That(binned.MapNumber(8.0).GetR(), Is.EqualTo(255));

        ChartVisualMap threshold = new ChartVisualMap(ChartVisualMapKind.Threshold, ChartEncodingChannel.Opacity);
        double[] thresholds = new double[] { 10.0 };
        ChartColor[] thresholdColors = new ChartColor[] { ChartColor.Black(), ChartColor.White() };
        threshold.SetThresholds(thresholds);
        threshold.SetThresholdColors(thresholdColors);
        Assert.That(threshold.GetThresholds(), Is.SameAs(thresholds));
        Assert.That(threshold.GetThresholdColors(), Is.SameAs(thresholdColors));
        Assert.That(threshold.MapNumber(10.0).GetR(), Is.EqualTo(255));

        ChartVisualMap categorical = new ChartVisualMap(ChartVisualMapKind.Categorical, ChartEncodingChannel.Color);
        ChartVisualMapCategory[] categories = new ChartVisualMapCategory[]
        {
            new ChartVisualMapCategory("A", ChartColor.Black()),
            new ChartVisualMapCategory("B", ChartColor.White())
        };
        categorical.SetPalette(palette);
        categorical.SetCategories(categories);
        categorical.SetCategoryCount(2);
        Assert.That(categorical.GetPalette(), Is.SameAs(palette));
        Assert.That(categorical.GetCategories(), Is.SameAs(categories));
        Assert.That(categorical.GetCategoryCount(), Is.EqualTo(2));
        Assert.That(categorical.MapCategory("B").GetR(), Is.EqualTo(255));

        categorical.SetChannel(ChartEncodingChannel.Size);
        categorical.SetKind(ChartVisualMapKind.Categorical);
        Assert.That(categorical.GetChannel(), Is.EqualTo(ChartEncodingChannel.Size));
        Assert.That(categorical.GetKind(), Is.EqualTo(ChartVisualMapKind.Categorical));
    }

    [Test]
    public void DeclarativeFacade_ExposesEveryDirectSeriesOperation()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable table = CreateTable();
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartVisualEncoding xy = XYEncoding();
        ChartVisualEncoding area = XYEncoding();
        area.SetY2("low", ChartDataKind.Number);
        ChartVisualEncoding bubble = XYEncoding();
        bubble.SetSize("size", ChartDataKind.Number);
        ChartVisualEncoding bars = XYEncoding();
        bars.SetConstantNumber(ChartEncodingChannel.Size, 0.5);
        ChartVisualEncoding range = XYEncoding();
        range.SetY2("high", ChartDataKind.Number);
        range.SetConstantNumber(ChartEncodingChannel.Size, 0.5);

        Assert.That(charts.CreateLineSeries("line", table, xy, stroke).GetCommandCount(), Is.EqualTo(1));
        Assert.That(charts.CreateAreaSeries("area", table, area, fill).GetCommandCount(), Is.EqualTo(1));
        Assert.That(charts.CreateScatterSeries("scatter", table, xy, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateBubbleSeries("bubble", table, bubble, 2.0, 8.0, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateVerticalBarSeries("vertical", table, bars, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateHorizontalBarSeries("horizontal", table, bars, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateRangeBarSeries("range", table, range, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateSeries("line-mark", ChartDeclarativeMark.Line, table, xy, stroke).GetCommandCount(), Is.EqualTo(1));
        Assert.That(charts.CreateSeries("area-mark", ChartDeclarativeMark.Area, table, area, fill).GetCommandCount(), Is.EqualTo(1));
        Assert.That(charts.CreateSeries("scatter-mark", ChartDeclarativeMark.Scatter, table, xy, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateSeries("bubble-mark", ChartDeclarativeMark.Bubble, table, bubble, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateSeries("vertical-mark", ChartDeclarativeMark.VerticalBar, table, bars, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateSeries("horizontal-mark", ChartDeclarativeMark.HorizontalBar, table, bars, fill).GetCommandCount(), Is.EqualTo(3));
        Assert.That(charts.CreateSeries("range-mark", ChartDeclarativeMark.RangeBar, table, range, fill).GetCommandCount(), Is.EqualTo(3));
    }

    [Test]
    public void DeclarativeFacade_RejectsInvalidConfiguration()
    {
        ChartDeclarativeCharts charts = new ChartDeclarativeCharts();
        ChartDataTable table = CreateTable();
        ChartVisualEncoding xy = XYEncoding();
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());

        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreateSeries("bad", (ChartDeclarativeMark)99, table, xy, fill));
        Assert.Throws<System.ArgumentNullException>(() => charts.CreateLineSeries("bad", null!, xy, fill));
        Assert.Throws<System.ArgumentException>(() => charts.CreateBubbleSeries("bad", table, xy, 2.0, 8.0, fill));
        Assert.Throws<System.ArgumentException>(() => charts.CreateRangeBarSeries("bad", table, xy, fill));
    }

    static ChartDataTable CreateTable()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 0.0, 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 2.0, 4.0, 3.0 });
        table.AddNumberColumn("low", new double[] { 1.0, 2.0, 1.5 });
        table.AddNumberColumn("high", new double[] { 3.0, 5.0, 4.0 });
        table.AddNumberColumn("size", new double[] { 1.0, 4.0, 9.0 });
        return table;
    }

    static ChartVisualEncoding XYEncoding()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        return encoding;
    }
}
