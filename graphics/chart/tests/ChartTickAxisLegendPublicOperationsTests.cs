namespace chart.Tests;

[TestFixture]
public class ChartTickAxisLegendPublicOperationsTests
{
    [Test]
    public void TickGeneratorFacade_ExposesEveryGenerationOperation()
    {
        ChartTickGenerator generator = new ChartTickGenerator();

        Assert.That(generator.GenerateNumericTicks(0.0, 10.0, 3).Length, Is.GreaterThanOrEqualTo(3));
        Assert.That(generator.GenerateExplicitTicks(new double[] { 3.0, 1.0 })[0].GetValue(), Is.EqualTo(3.0));
        Assert.That(generator.GenerateLogTicks(1.0, 100.0, 10.0).Length, Is.EqualTo(3));
        Assert.That(generator.GenerateMinorLogTicks(1.0, 10.0, 10.0).Length, Is.EqualTo(8));
        Assert.That(generator.GenerateTimeTicks(new System.DateTime(2024, 1, 1), new System.DateTime(2024, 1, 2), 3).Length, Is.EqualTo(3));
        Assert.That(generator.GenerateCategoryTicks(new string[] { "A", "B" })[1].GetLabel(), Is.EqualTo("B"));
        Assert.That(generator.GenerateRadialTicks(10.0, 3)[0].GetValue(), Is.EqualTo(0.0));
        Assert.That(generator.GenerateAngularTicks(4).Length, Is.EqualTo(4));
    }

    [Test]
    public void CalendarTickFacade_SupportsEveryIntervalChoice()
    {
        ChartTickGenerator generator = new ChartTickGenerator();
        ChartTimeInterval[] intervals = new ChartTimeInterval[]
        {
            ChartTimeInterval.Millisecond,
            ChartTimeInterval.Second,
            ChartTimeInterval.Minute,
            ChartTimeInterval.Hour,
            ChartTimeInterval.Day,
            ChartTimeInterval.Week,
            ChartTimeInterval.Month,
            ChartTimeInterval.Quarter,
            ChartTimeInterval.Year
        };
        System.DateTime start = new System.DateTime(2024, 1, 1);
        System.DateTime[] ends = new System.DateTime[]
        {
            start.AddMilliseconds(2.0),
            start.AddSeconds(2.0),
            start.AddMinutes(2.0),
            start.AddHours(2.0),
            start.AddDays(2.0),
            start.AddDays(14.0),
            start.AddMonths(2),
            start.AddMonths(6),
            start.AddYears(2)
        };
        int index = 0;
        while (index < intervals.Length)
        {
            ChartTick[] ticks = generator.GenerateCalendarTicks(start, ends[index], intervals[index], 1);
            Assert.That(ticks.Length, Is.GreaterThan(0), intervals[index].ToString());
            Assert.That(ticks[0].GetKind(), Is.EqualTo(ChartTickKind.Time));
            index = index + 1;
        }
    }

    [Test]
    public void TickFormatterFacade_ExposesEveryFormattingOperation()
    {
        ChartTickFormatter formatter = new ChartTickFormatter();
        ChartTick[] ticks = new ChartTick[]
        {
            new ChartTick(ChartTickKind.Numeric, 12.5, "", 0),
            new ChartTick(ChartTickKind.Category, 1.0, "North", 1)
        };

        Assert.That(formatter.FormatNumber(12.5, 1), Is.EqualTo("12.5"));
        Assert.That(formatter.FormatNumberWithCulture(1234.5, 1, "de-DE"), Is.EqualTo("1234,5"));
        Assert.That(formatter.FormatCurrency(12.5, 2, "en-US"), Is.EqualTo("$12.50"));
        Assert.That(formatter.FormatPercent(0.125, 1), Is.EqualTo("12.5%"));
        Assert.That(formatter.FormatScientific(1200.0, 1), Does.Contain("E"));
        Assert.That(formatter.FormatSiPrefix(1200.0, 1), Does.EndWith("k"));
        Assert.That(formatter.FormatDate(new System.DateTime(2024, 3, 5)), Is.Not.Empty);
        Assert.That(formatter.FormatDateWithPattern(new System.DateTime(2024, 3, 5), "yyyy/MM/dd"), Is.EqualTo("2024/03/05"));
        Assert.That(formatter.FormatDateWithCulture(new System.DateTime(2024, 3, 5), "MMMM", "fr-FR"), Is.EqualTo("mars"));
        Assert.That(formatter.FormatDuration(System.TimeSpan.FromMinutes(90.0)), Does.Contain("h"));
        Assert.That(formatter.FormatCategory("North"), Is.EqualTo("North"));
        Assert.That(formatter.FormatTick(new ChartTick(ChartTickKind.Automatic, 2.5, "", 0)), Is.EqualTo("2.5"));
        Assert.That(formatter.FormatTick(ticks[0]), Is.EqualTo("12.5"));
        Assert.That(formatter.FormatTicks(ticks), Is.EqualTo(new string[] { "12.5", "North" }));
    }

    [Test]
    public void TickFacades_RejectInvalidConfiguration()
    {
        ChartTickGenerator generator = new ChartTickGenerator();
        ChartTickFormatter formatter = new ChartTickFormatter();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => generator.GenerateNumericTicks(0.0, 1.0, 0));
        Assert.Throws<System.ArgumentException>(() => generator.GenerateLogTicks(0.0, 10.0, 10.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => generator.GenerateCalendarTicks(new System.DateTime(2024, 1, 1), new System.DateTime(2024, 1, 2), (ChartTimeInterval)99, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => formatter.FormatNumber(1.0, -1));
        Assert.Throws<System.ArgumentException>(() => formatter.FormatDateWithPattern(new System.DateTime(2024, 1, 1), ""));
        Assert.Throws<System.ArgumentNullException>(() => formatter.FormatTicks(null!));
    }

    [Test]
    public void AxisAccessors_ExposeAllMutableConfiguration()
    {
        ChartTick[] ticks = new ChartTick[] { new ChartTick(ChartTickKind.Numeric, 0.0, "zero", 0) };
        ChartAxis axis = new ChartAxis(ChartAxisKind.X, ChartAxisPlacement.Bottom, ticks, new ChartRange(0.0, 1.0), new ChartRect(0.0, 0.0, 10.0, 10.0));
        ChartColor grid = ChartColor.FromRgb(1, 2, 3);
        ChartColor zero = ChartColor.FromRgb(4, 5, 6);
        ChartColor band = ChartColor.FromRgb(7, 8, 9);
        ChartColor text = ChartColor.FromRgb(10, 11, 12);

        axis.SetPlacement(ChartAxisPlacement.Top);
        axis.SetPlotArea(new ChartRect(1.0, 2.0, 20.0, 30.0));
        axis.SetCenter(new ChartPoint(11.0, 12.0));
        axis.SetRadius(13.0);
        axis.SetTickLength(14.0);
        axis.SetMinorTickLength(5.0);
        axis.SetMinorTickCount(2);
        axis.SetLabelOffset(15.0);
        axis.SetTitleOffset(16.0);
        axis.SetGridLength(17.0);
        axis.SetMinimumLabelSpacing(18.0);
        axis.SetGridColor(grid);
        axis.SetZeroLineColor(zero);
        axis.SetBandColor(band);
        axis.SetTextColor(text);
        axis.SetGridStrokeWidth(2.0);
        axis.SetZeroLineStrokeWidth(3.0);
        axis.SetTitle("Title");
        axis.SetFontFamily("Inter");
        axis.SetShowTickLabels(true);
        axis.SetShowMinorTicks(true);
        axis.SetShowGridLines(true);
        axis.SetShowZeroLine(true);
        axis.SetShowGridBands(true);
        axis.SetAutoSkipTickLabels(true);

        Assert.That(axis.GetPlacement(), Is.EqualTo(ChartAxisPlacement.Top));
        Assert.That(axis.GetPlotArea().GetWidth(), Is.EqualTo(20.0));
        Assert.That(axis.GetCenter().GetX(), Is.EqualTo(11.0));
        Assert.That(axis.GetRadius(), Is.EqualTo(13.0));
        Assert.That(axis.GetTickLength(), Is.EqualTo(14.0));
        Assert.That(axis.GetMinorTickLength(), Is.EqualTo(5.0));
        Assert.That(axis.GetMinorTickCount(), Is.EqualTo(2));
        Assert.That(axis.GetLabelOffset(), Is.EqualTo(15.0));
        Assert.That(axis.GetTitleOffset(), Is.EqualTo(16.0));
        Assert.That(axis.GetGridLength(), Is.EqualTo(17.0));
        Assert.That(axis.GetMinimumLabelSpacing(), Is.EqualTo(18.0));
        Assert.That(axis.GetGridColor(), Is.SameAs(grid));
        Assert.That(axis.GetZeroLineColor(), Is.SameAs(zero));
        Assert.That(axis.GetBandColor(), Is.SameAs(band));
        Assert.That(axis.GetTextColor(), Is.SameAs(text));
        Assert.That(axis.GetGridStrokeWidth(), Is.EqualTo(2.0));
        Assert.That(axis.GetZeroLineStrokeWidth(), Is.EqualTo(3.0));
        Assert.That(axis.GetTitle(), Is.EqualTo("Title"));
        Assert.That(axis.GetFontFamily(), Is.EqualTo("Inter"));
        Assert.That(axis.GetShowTickLabels(), Is.True);
        Assert.That(axis.GetShowMinorTicks(), Is.True);
        Assert.That(axis.GetShowGridLines(), Is.True);
        Assert.That(axis.GetShowZeroLine(), Is.True);
        Assert.That(axis.GetShowGridBands(), Is.True);
        Assert.That(axis.GetAutoSkipTickLabels(), Is.True);
        Assert.That(axis.GetTickFormatter(), Is.Not.Null);
    }

    [Test]
    public void AxisSupportsEveryKindPlacementAndClearOperation()
    {
        ChartTick[] ticks = new ChartTick[] { new ChartTick(ChartTickKind.Numeric, 0.0, "", 0) };
        ChartRange domain = new ChartRange(0.0, 1.0);
        ChartRect area = new ChartRect(0.0, 0.0, 10.0, 10.0);
        ChartAxis[] axes = new ChartAxis[]
        {
            new ChartAxis(ChartAxisKind.X, ChartAxisPlacement.Bottom, ticks, domain, area),
            new ChartAxis(ChartAxisKind.X, ChartAxisPlacement.Top, ticks, domain, area),
            new ChartAxis(ChartAxisKind.Y, ChartAxisPlacement.Left, ticks, domain, area),
            new ChartAxis(ChartAxisKind.Y, ChartAxisPlacement.Right, ticks, domain, area),
            new ChartAxis(ChartAxisKind.Radial, ChartAxisPlacement.Inside, ticks, domain, area),
            new ChartAxis(ChartAxisKind.Angular, ChartAxisPlacement.Outside, ticks, domain, area),
            new ChartAxis(ChartAxisKind.Ternary, ChartAxisPlacement.Outside, ticks, domain, area)
        };
        Assert.That(axes.Length, Is.EqualTo(7));

        ChartAxis axis = axes[0];
        axis.AddThresholdLine(0.5, "middle", ChartColor.Black(), 1.0);
        axis.AddBreak(0.2, 0.4);
        Assert.That(axis.ThresholdCount, Is.EqualTo(1));
        Assert.That(axis.BreakCount, Is.EqualTo(1));
        axis.ClearThresholdLines();
        axis.ClearBreaks();
        Assert.That(axis.ThresholdCount, Is.EqualTo(0));
        Assert.That(axis.BreakCount, Is.EqualTo(0));
        Assert.Throws<System.ArgumentException>(() => new ChartAxis(ChartAxisKind.X, ChartAxisPlacement.Left, ticks, domain, area));
        Assert.Throws<System.ArgumentException>(() => new ChartAxis((ChartAxisKind)99, ChartAxisPlacement.Outside, ticks, domain, area));
    }

    [Test]
    public void LegendAccessors_ExposeEntriesAndPresentationState()
    {
        ChartLegend legend = new ChartLegend("legend");
        ChartStyle style = new ChartStyle();
        style.SetFill(ChartColor.Black());
        ChartLegendEntry entry = ChartLegendEntry.CreateSymbol("entry", "Entry", ChartMarkerShape.Circle, style);
        ChartColor text = ChartColor.FromRgb(1, 2, 3);
        ChartColor background = ChartColor.FromRgb(4, 5, 6);
        ChartColor border = ChartColor.FromRgb(7, 8, 9);

        legend.SetTitle("Legend");
        legend.SetEntries(new ChartLegendEntry[] { entry });
        legend.SetEntryCount(1);
        legend.SetPadding(2.0);
        legend.SetEntrySpacing(3.0);
        legend.SetSymbolSize(4.0);
        legend.SetGradientWidth(5.0);
        legend.SetGradientHeight(6.0);
        legend.SetFontFamily("Inter");
        legend.SetFontSize(12);
        legend.SetTextColor(text);
        legend.SetBackgroundColor(background);
        legend.SetHasBackground(true);
        legend.SetBorderColor(border);
        legend.SetBorderWidth(2.0);
        legend.SetShowBorder(true);

        ChartLegendEntry gradient = ChartLegendEntry.CreateGradient("gradient", "Gradient", new ChartColor[] { ChartColor.Black(), ChartColor.White() }, new double[] { 0.0, 1.0 });
        ChartLegendEntry custom = ChartLegendEntry.CreateCustom("custom", "Custom", "<path/>");

        Assert.That(legend.GetTitle(), Is.EqualTo("Legend"));
        Assert.That(legend.GetEntries()[0], Is.SameAs(entry));
        Assert.That(legend.GetEntryCount(), Is.EqualTo(1));
        Assert.That(legend.GetPadding(), Is.EqualTo(2.0));
        Assert.That(legend.GetEntrySpacing(), Is.EqualTo(3.0));
        Assert.That(legend.GetSymbolSize(), Is.EqualTo(4.0));
        Assert.That(legend.GetGradientWidth(), Is.EqualTo(5.0));
        Assert.That(legend.GetGradientHeight(), Is.EqualTo(6.0));
        Assert.That(legend.GetFontFamily(), Is.EqualTo("Inter"));
        Assert.That(legend.GetFontSize(), Is.EqualTo(12));
        Assert.That(legend.GetTextColor(), Is.SameAs(text));
        Assert.That(legend.GetBackgroundColor(), Is.SameAs(background));
        Assert.That(legend.GetHasBackground(), Is.True);
        Assert.That(legend.GetBorderColor(), Is.SameAs(border));
        Assert.That(legend.GetBorderWidth(), Is.EqualTo(2.0));
        Assert.That(legend.GetShowBorder(), Is.True);
        Assert.That(entry.GetKind(), Is.EqualTo(ChartLegendEntryKind.Symbol));
        Assert.That(gradient.GetKind(), Is.EqualTo(ChartLegendEntryKind.Gradient));
        Assert.That(custom.GetKind(), Is.EqualTo(ChartLegendEntryKind.Custom));
    }
}
