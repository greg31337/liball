namespace chart.Tests;

[TestFixture]
public class ChartTickFormatterTests
{
    [Test]
    public void FormatNumber_UsesInvariantCompactDecimals()
    {
        Assert.That(ChartTickFormatterOperations.FormatNumber(12.3456, 2), Is.EqualTo("12.35"));
        Assert.That(ChartTickFormatterOperations.FormatNumber(12.0, 3), Is.EqualTo("12"));
        Assert.That(ChartTickFormatterOperations.FormatNumber(-0.0001, 2), Is.EqualTo("0"));
    }

    [Test]
    public void FormatPercent_FormatsFractionAsPercent()
    {
        Assert.That(ChartTickFormatterOperations.FormatPercent(0.1234, 1), Is.EqualTo("12.3%"));
        Assert.That(ChartTickFormatterOperations.FormatPercent(1.0, 0), Is.EqualTo("100%"));
    }

    [Test]
    public void FormatScientific_UsesExplicitExponentNotation()
    {
        Assert.That(ChartTickFormatterOperations.FormatScientific(12345.0, 2), Is.EqualTo("1.23E+4"));
        Assert.That(ChartTickFormatterOperations.FormatScientific(0.0123, 1), Is.EqualTo("1.2E-2"));
        Assert.That(ChartTickFormatterOperations.FormatScientific(0.0, 2), Is.EqualTo("0E+0"));
    }

    [Test]
    public void FormatSiPrefix_ChoosesScaledSuffixes()
    {
        Assert.That(ChartTickFormatterOperations.FormatSiPrefix(1500.0, 1), Is.EqualTo("1.5k"));
        Assert.That(ChartTickFormatterOperations.FormatSiPrefix(2500000.0, 1), Is.EqualTo("2.5M"));
        Assert.That(ChartTickFormatterOperations.FormatSiPrefix(0.0025, 1), Is.EqualTo("2.5m"));
        Assert.That(ChartTickFormatterOperations.FormatSiPrefix(0.0000012, 1), Is.EqualTo("1.2u"));
        Assert.That(ChartTickFormatterOperations.FormatSiPrefix(0.0, 2), Is.EqualTo("0"));
    }

    [Test]
    public void FormatDate_UsesInvariantDatePatterns()
    {
        System.DateTime value = new System.DateTime(2024, 3, 5, 14, 30, 0);

        Assert.That(ChartTickFormatterOperations.FormatDate(value), Is.EqualTo("2024-03-05"));
        Assert.That(ChartTickFormatterOperations.FormatDate(value, "MMM dd HH:mm"), Is.EqualTo("Mar 05 14:30"));
    }

    [Test]
    public void FormatDuration_UsesReadableLargestUnits()
    {
        Assert.That(ChartTickFormatterOperations.FormatDuration(new System.TimeSpan(1, 2, 3, 4)), Is.EqualTo("1d 2h"));
        Assert.That(ChartTickFormatterOperations.FormatDuration(new System.TimeSpan(0, 2, 3, 4)), Is.EqualTo("2h 3m"));
        Assert.That(ChartTickFormatterOperations.FormatDuration(new System.TimeSpan(0, 0, 3, 4)), Is.EqualTo("3m 4s"));
        Assert.That(ChartTickFormatterOperations.FormatDuration(System.TimeSpan.FromSeconds(1.25)), Is.EqualTo("1.25s"));
        Assert.That(ChartTickFormatterOperations.FormatDuration(System.TimeSpan.FromMilliseconds(-2.5)), Is.EqualTo("-2.5ms"));
    }

    [Test]
    public void FormatCategory_ReturnsCategoryLabel()
    {
        Assert.That(ChartTickFormatterOperations.FormatCategory("North"), Is.EqualTo("North"));
    }

    [Test]
    public void FormatTick_UsesNaturalFormattingForTickKind()
    {
        ChartTick[] ticks = new ChartTick[]
        {
            new ChartTick(ChartTickKind.Numeric, 12.3456, "", 0),
            ChartTick.CreateTime(new System.DateTime(2024, 3, 5), 1),
            new ChartTick(ChartTickKind.Category, 0.0, "North", 2),
            new ChartTick(ChartTickKind.Explicit, 1.0, "Custom", 3)
        };

        Assert.That(ChartTickFormatterOperations.FormatTick(ticks[0]), Is.EqualTo("12.346"));
        Assert.That(ChartTickFormatterOperations.FormatTick(ticks[1]), Is.EqualTo("2024-03-05"));
        Assert.That(ChartTickFormatterOperations.FormatTick(ticks[2]), Is.EqualTo("North"));
        Assert.That(ChartTickFormatterOperations.FormatTick(ticks[3]), Is.EqualTo("Custom"));
        Assert.That(ChartTickFormatterOperations.FormatTicks(ticks), Is.EqualTo(new string[] { "12.346", "2024-03-05", "North", "Custom" }));
    }

    [Test]
    public void TickFormatter_ValidatesInputs()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickFormatterOperations.FormatNumber(double.NaN, 2));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickFormatterOperations.FormatNumber(1.0, -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickFormatterOperations.FormatNumber(1.0, 16));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickFormatterOperations.FormatPercent(double.PositiveInfinity, 1));
        Assert.Throws<System.ArgumentNullException>(() => ChartTickFormatterOperations.FormatDate(new System.DateTime(2024, 1, 1), null!));
        Assert.Throws<System.ArgumentException>(() => ChartTickFormatterOperations.FormatDate(new System.DateTime(2024, 1, 1), ""));
        Assert.Throws<System.ArgumentNullException>(() => ChartTickFormatterOperations.FormatCategory(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartTickFormatterOperations.FormatTick(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartTickFormatterOperations.FormatTicks(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartTickFormatterOperations.FormatTicks(new ChartTick[] { null! }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickFormatterOperations.SiSuffix(2));
    }
}
