namespace chart.Tests;

[TestFixture]
public class ChartTickGeneratorTests
{
    [Test]
    public void GenerateNumericTicks_UsesNiceDomain()
    {
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateNumericTicks(3.2, 9.7, 5);

        AssertTick(ticks[0], ChartTickKind.Numeric, 2.0, "", 0);
        AssertTick(ticks[1], ChartTickKind.Numeric, 4.0, "", 1);
        AssertTick(ticks[2], ChartTickKind.Numeric, 6.0, "", 2);
        AssertTick(ticks[3], ChartTickKind.Numeric, 8.0, "", 3);
        AssertTick(ticks[4], ChartTickKind.Numeric, 10.0, "", 4);
    }

    [Test]
    public void GenerateExplicitTicks_PreservesFiniteValuesInOrder()
    {
        double[] values = new double[] { 5.0, 1.0, 3.0 };
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateExplicitTicks(values);

        values[0] = 99.0;

        AssertTick(ticks[0], ChartTickKind.Explicit, 5.0, "", 0);
        AssertTick(ticks[1], ChartTickKind.Explicit, 1.0, "", 1);
        AssertTick(ticks[2], ChartTickKind.Explicit, 3.0, "", 2);
    }

    [Test]
    public void GenerateLogTicks_ReturnsPowersWithinDomain()
    {
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateLogTicks(1.0, 1000.0, 10.0);

        AssertTick(ticks[0], ChartTickKind.Log, 1.0, "", 0);
        AssertTick(ticks[1], ChartTickKind.Log, 10.0, "", 1);
        AssertTick(ticks[2], ChartTickKind.Log, 100.0, "", 2);
        AssertTick(ticks[3], ChartTickKind.Log, 1000.0, "", 3);
    }

    [Test]
    public void GenerateTimeTicks_InterpolatesDateTimes()
    {
        System.DateTime start = new System.DateTime(2024, 1, 1);
        System.DateTime end = new System.DateTime(2024, 1, 5);

        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateTimeTicks(start, end, 5);

        AssertTimeTick(ticks[0], new System.DateTime(2024, 1, 1), 0);
        AssertTimeTick(ticks[1], new System.DateTime(2024, 1, 2), 1);
        AssertTimeTick(ticks[2], new System.DateTime(2024, 1, 3), 2);
        AssertTimeTick(ticks[3], new System.DateTime(2024, 1, 4), 3);
        AssertTimeTick(ticks[4], new System.DateTime(2024, 1, 5), 4);
    }

    [Test]
    public void GenerateCategoryTicks_UsesCategoryLabelsAndIndexes()
    {
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateCategoryTicks(new string[] { "A", "B", "C" });

        AssertTick(ticks[0], ChartTickKind.Category, 0.0, "A", 0);
        AssertTick(ticks[1], ChartTickKind.Category, 1.0, "B", 1);
        AssertTick(ticks[2], ChartTickKind.Category, 2.0, "C", 2);
    }

    [Test]
    public void GenerateRadialTicks_IncludesZeroAndMaximum()
    {
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateRadialTicks(10.0, 3);

        AssertTick(ticks[0], ChartTickKind.Radial, 0.0, "", 0);
        AssertTick(ticks[1], ChartTickKind.Radial, 5.0, "", 1);
        AssertTick(ticks[2], ChartTickKind.Radial, 10.0, "", 2);
    }

    [Test]
    public void GenerateAngularTicks_CoversCircleWithoutDuplicateEndpoint()
    {
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateAngularTicks(4);

        AssertTick(ticks[0], ChartTickKind.Angular, 0.0, "", 0);
        AssertTick(ticks[1], ChartTickKind.Angular, 90.0, "", 1);
        AssertTick(ticks[2], ChartTickKind.Angular, 180.0, "", 2);
        AssertTick(ticks[3], ChartTickKind.Angular, 270.0, "", 3);
    }

    [Test]
    public void CopyTicks_ReturnsIndependentTickCopies()
    {
        ChartTick[] ticks = ChartTickGeneratorOperations.GenerateCategoryTicks(new string[] { "A", "B" });
        ChartTick[] copy = ChartTickGeneratorOperations.CopyTicks(ticks);

        ticks[0].Label = "Changed";
        ticks[1].Value = 99.0;

        AssertTick(copy[0], ChartTickKind.Category, 0.0, "A", 0);
        AssertTick(copy[1], ChartTickKind.Category, 1.0, "B", 1);
    }

    [Test]
    public void TickGenerator_ValidatesInputs()
    {
        Assert.Throws<System.ArgumentException>(() => ChartTickGeneratorOperations.GenerateNumericTicks(1.0, 1.0, 5));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickGeneratorOperations.GenerateNumericTicks(0.0, 1.0, 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartTickGeneratorOperations.GenerateExplicitTicks(null!));
        Assert.Throws<System.ArgumentException>(() => ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { }));
        Assert.Throws<System.ArgumentException>(() => ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { double.NaN }));
        Assert.Throws<System.ArgumentException>(() => ChartTickGeneratorOperations.GenerateLogTicks(0.0, 10.0, 10.0));
        Assert.Throws<System.ArgumentException>(() => ChartTickGeneratorOperations.GenerateLogTicks(1.0, 10.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickGeneratorOperations.GenerateLogTicks(double.Epsilon, double.MaxValue, 1.0000000000000002));
        Assert.Throws<System.ArgumentException>(() => ChartTickGeneratorOperations.GenerateTimeTicks(new System.DateTime(2024, 1, 1), new System.DateTime(2024, 1, 1), 3));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickGeneratorOperations.GenerateTimeTicks(new System.DateTime(2024, 1, 1), new System.DateTime(2024, 1, 2), 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartTickGeneratorOperations.GenerateCategoryTicks(null!));
        Assert.Throws<System.ArgumentException>(() => ChartTickGeneratorOperations.GenerateCategoryTicks(new string[] { "" }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickGeneratorOperations.GenerateRadialTicks(0.0, 3));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickGeneratorOperations.GenerateRadialTicks(1.0, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartTickGeneratorOperations.GenerateAngularTicks(0));
        Assert.Throws<System.ArgumentNullException>(() => new ChartTick(ChartTickKind.Numeric, 1.0, null!, 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartTickGeneratorOperations.CopyTicks(null!));
        Assert.Throws<System.ArgumentException>(() => ChartTickGeneratorOperations.CopyTicks(new ChartTick[] { null! }));
    }

    static void AssertTick(ChartTick tick, ChartTickKind kind, double value, string label, int index)
    {
        Assert.That(tick.Kind, Is.EqualTo(kind));
        Assert.That(tick.Value, Is.EqualTo(value).Within(0.0000001));
        Assert.That(tick.Label, Is.EqualTo(label));
        Assert.That(tick.Index, Is.EqualTo(index));
    }

    static void AssertTimeTick(ChartTick tick, System.DateTime value, int index)
    {
        Assert.That(tick.Kind, Is.EqualTo(ChartTickKind.Time));
        Assert.That(tick.DateTimeValue, Is.EqualTo(value));
        Assert.That(tick.Value, Is.EqualTo((double)value.Ticks).Within(1.0));
        Assert.That(tick.Index, Is.EqualTo(index));
    }
}
