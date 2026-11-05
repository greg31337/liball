namespace chart.Tests;

[TestFixture]
public class ChartTickPoliciesTests
{
    [Test]
    public void GenerateCalendarTicks_AlignsMonthsAndQuarters()
    {
        ChartTickGenerator generator = new ChartTickGenerator();

        ChartTick[] months = generator.GenerateCalendarTicks(
            new System.DateTime(2024, 1, 15),
            new System.DateTime(2024, 5, 20),
            ChartTimeInterval.Month,
            1);
        ChartTick[] quarters = generator.GenerateCalendarTicks(
            new System.DateTime(2024, 2, 15),
            new System.DateTime(2024, 11, 1),
            ChartTimeInterval.Quarter,
            1);

        Assert.That(CopyDates(months), Is.EqualTo(new System.DateTime[]
        {
            new System.DateTime(2024, 2, 1),
            new System.DateTime(2024, 3, 1),
            new System.DateTime(2024, 4, 1),
            new System.DateTime(2024, 5, 1)
        }));
        Assert.That(CopyDates(quarters), Is.EqualTo(new System.DateTime[]
        {
            new System.DateTime(2024, 4, 1),
            new System.DateTime(2024, 7, 1),
            new System.DateTime(2024, 10, 1)
        }));
    }

    [Test]
    public void GenerateCalendarTicks_AlignsWeeksAndPreservesUtcKind()
    {
        ChartTickGenerator generator = new ChartTickGenerator();
        System.DateTime start = new System.DateTime(2024, 1, 3, 12, 0, 0, System.DateTimeKind.Utc);
        System.DateTime end = new System.DateTime(2024, 1, 22, 0, 0, 0, System.DateTimeKind.Utc);

        ChartTick[] ticks = generator.GenerateCalendarTicks(start, end, ChartTimeInterval.Week, 1);

        Assert.That(CopyDates(ticks), Is.EqualTo(new System.DateTime[]
        {
            new System.DateTime(2024, 1, 8, 0, 0, 0, System.DateTimeKind.Utc),
            new System.DateTime(2024, 1, 15, 0, 0, 0, System.DateTimeKind.Utc),
            new System.DateTime(2024, 1, 22, 0, 0, 0, System.DateTimeKind.Utc)
        }));
        Assert.That(ticks[0].GetDateTimeValue().Kind, Is.EqualTo(System.DateTimeKind.Utc));
    }

    [Test]
    public void GenerateCalendarTicks_SupportsFixedCalendarIntervalsAndSteps()
    {
        ChartTickGenerator generator = new ChartTickGenerator();
        System.DateTime start = new System.DateTime(2024, 1, 1, 0, 0, 0, 250);
        System.DateTime end = new System.DateTime(2024, 1, 1, 0, 0, 1, 0);

        ChartTick[] milliseconds = generator.GenerateCalendarTicks(start, end, ChartTimeInterval.Millisecond, 250);
        ChartTick[] hours = generator.GenerateCalendarTicks(
            new System.DateTime(2024, 1, 1, 0, 30, 0),
            new System.DateTime(2024, 1, 1, 6, 0, 0),
            ChartTimeInterval.Hour,
            2);

        Assert.That(milliseconds.Length, Is.EqualTo(4));
        Assert.That(milliseconds[0].GetDateTimeValue().Millisecond, Is.EqualTo(250));
        Assert.That(milliseconds[3].GetDateTimeValue().Millisecond, Is.EqualTo(0));
        Assert.That(milliseconds[3].GetDateTimeValue().Second, Is.EqualTo(1));
        Assert.That(CopyDates(hours), Is.EqualTo(new System.DateTime[]
        {
            new System.DateTime(2024, 1, 1, 2, 0, 0),
            new System.DateTime(2024, 1, 1, 4, 0, 0),
            new System.DateTime(2024, 1, 1, 6, 0, 0)
        }));
    }

    [Test]
    public void GenerateMinorLogTicks_ReturnsNonPowerTicksWithinDomain()
    {
        ChartTickGenerator generator = new ChartTickGenerator();

        ChartTick[] ticks = generator.GenerateMinorLogTicks(1.0, 100.0, 10.0);
        double[] values = new double[ticks.Length];
        int index = 0;
        while (index < ticks.Length)
        {
            values[index] = ticks[index].GetValue();
            index = index + 1;
        }

        Assert.That(values, Is.EqualTo(new double[]
        {
            2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0,
            20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0
        }));
    }

    [Test]
    public void TickFormatter_FormatsCurrencyNumbersAndDatesWithExplicitCultures()
    {
        ChartTickFormatter formatter = new ChartTickFormatter();

        Assert.That(formatter.FormatCurrency(1234.5, 2, "en-US"), Is.EqualTo("$1,234.50"));
        Assert.That(formatter.FormatNumberWithCulture(1234.5, 2, "de-DE"), Is.EqualTo("1234,5"));
        Assert.That(formatter.FormatDateWithCulture(new System.DateTime(2024, 3, 5), "dd MMMM yyyy", "fr-FR"), Is.EqualTo("05 mars 2024"));
        Assert.That(formatter.FormatNumber(1234.5, 1), Is.EqualTo("1234.5"));
    }

    [Test]
    public void TickLabelPreparation_TrimsEllipsizesAndControlsLineBreaks()
    {
        string multiline = ChartTickLabelOperations.Prepare("  North America  \r\n South ", true, true, 8, true);
        string flattened = ChartTickLabelOperations.Prepare("  North\nSouth  ", false, true, 0, false);
        string clipped = ChartTickLabelOperations.Prepare("ABCDEFGHI", false, false, 4, false);

        Assert.That(multiline, Is.EqualTo("North A…\nSouth"));
        Assert.That(flattened, Is.EqualTo("North South"));
        Assert.That(clipped, Is.EqualTo("ABCD"));
    }

    [Test]
    public void AxisRenderer_RendersMultilineRotatedTrimmedLabels()
    {
        ChartTick tick = new ChartTick(ChartTickKind.Explicit, 0.0, "  North America\n Region  ", 0);
        ChartAxis axis = ChartAxis.CreateBottom(
            new ChartTick[] { tick },
            new ChartRange(0.0, 1.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));
        axis.SetTickLabels(true, false, 0.0);
        axis.SetTickLabelLayout(45.0, 8, true, true, true);
        SvgWriter writer = new SvgWriter(140, 130);

        ChartAxisRenderer.RenderSvg(writer, axis);
        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<text id=\"axis-bottom-label-0\" x=\"10\" y=\"98\" transform=\"rotate(45 10 98)\""));
        Assert.That(svg, Does.Contain("<tspan x=\"10\" dy=\"0\">North A…</tspan>"));
        Assert.That(svg, Does.Contain("<tspan x=\"10\" dy=\"12\">Region</tspan>"));
    }

    [Test]
    public void AxisRenderer_CollisionSkippingUsesRotatedLabelBounds()
    {
        ChartTick[] ticks = new ChartTick[]
        {
            new ChartTick(ChartTickKind.Explicit, 0.0, "ABCDEFGHIJKLMNOPQRST", 0),
            new ChartTick(ChartTickKind.Explicit, 5.0, "ABCDEFGHIJKLMNOPQRST", 1),
            new ChartTick(ChartTickKind.Explicit, 10.0, "ABCDEFGHIJKLMNOPQRST", 2)
        };
        ChartAxis horizontal = ChartAxis.CreateBottom(ticks, new ChartRange(0.0, 10.0), new ChartRect(10.0, 20.0, 100.0, 60.0));
        horizontal.SetTickLabels(true, true, 0.0);
        SvgWriter horizontalWriter = new SvgWriter(140, 130);
        ChartAxisRenderer.RenderSvg(horizontalWriter, horizontal);

        ChartAxis rotated = ChartAxis.CreateBottom(ticks, new ChartRange(0.0, 10.0), new ChartRect(10.0, 20.0, 100.0, 60.0));
        rotated.SetTickLabels(true, true, 0.0);
        rotated.SetTickLabelLayout(90.0, 0, false, false, false);
        SvgWriter rotatedWriter = new SvgWriter(140, 130);
        ChartAxisRenderer.RenderSvg(rotatedWriter, rotated);

        string horizontalSvg = horizontalWriter.ToSvgString();
        string rotatedSvg = rotatedWriter.ToSvgString();
        Assert.That(horizontalSvg, Does.Contain("axis-bottom-label-0"));
        Assert.That(horizontalSvg, Does.Not.Contain("axis-bottom-label-1"));
        Assert.That(horizontalSvg, Does.Not.Contain("axis-bottom-label-2"));
        Assert.That(rotatedSvg, Does.Contain("axis-bottom-label-0"));
        Assert.That(rotatedSvg, Does.Contain("axis-bottom-label-1"));
        Assert.That(rotatedSvg, Does.Contain("axis-bottom-label-2"));
    }

    [Test]
    public void TickPolicies_ValidateUnsupportedInputs()
    {
        ChartTickGenerator generator = new ChartTickGenerator();
        ChartTickFormatter formatter = new ChartTickFormatter();
        ChartAxis axis = ChartAxis.CreateBottom(
            new ChartTick[] { new ChartTick(ChartTickKind.Numeric, 0.0, "", 0) },
            new ChartRange(0.0, 1.0),
            new ChartRect(0.0, 0.0, 10.0, 10.0));

        Assert.Throws<System.ArgumentException>(() => generator.GenerateMinorLogTicks(1.0, 10.0, 2.5));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => generator.GenerateCalendarTicks(new System.DateTime(2024, 1, 1), new System.DateTime(2024, 2, 1), ChartTimeInterval.Month, 0));
        Assert.Throws<System.ArgumentException>(() => generator.GenerateCalendarTicks(new System.DateTime(2024, 2, 1), new System.DateTime(2024, 1, 1), ChartTimeInterval.Month, 1));
        Assert.Throws<System.ArgumentException>(() => formatter.FormatCurrency(1.0, 2, ""));
        Assert.Throws<System.ArgumentNullException>(() => formatter.FormatNumberWithCulture(1.0, 2, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => axis.SetTickLabelLayout(0.0, -1, false, false, false));
    }

    static System.DateTime[] CopyDates(ChartTick[] ticks)
    {
        System.DateTime[] dates = new System.DateTime[ticks.Length];
        int index = 0;
        while (index < ticks.Length)
        {
            dates[index] = ticks[index].GetDateTimeValue();
            index = index + 1;
        }

        return dates;
    }
}
